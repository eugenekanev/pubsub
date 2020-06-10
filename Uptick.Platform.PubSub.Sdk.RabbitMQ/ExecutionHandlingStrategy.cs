using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Uptick.Platform.PubSub.Sdk.Exceptions;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public class ExecutionHandlingStrategy : IErrorHandlingStrategy, ISuccessHandlingStrategy
    {
        private readonly string _name;
        private readonly EasyNetQPublisher _waitEasyNetQPublisher;
        private readonly int _maxRetry;
        private readonly IModelNamingConventionController _modelNamingConventionController;
        private readonly ILogger _logger = Log.ForContext<ExecutionHandlingStrategy>();
        private readonly Func<IntegrationEvent, Task> _successExecutionHandler = (integrationEvent) => Task.CompletedTask;

        private readonly Func<MessageExecutionContext, Task> _failedExecutionHandler;
        private readonly IPublisher _publisher;
        private readonly bool _storedeadletter;

        public ExecutionHandlingStrategy(string name, EasyNetQPublisher waitEasyNetQPublisher, int maxRetry,
            IModelNamingConventionController modelNamingConventionController, bool explicitAcknowledgments, IPublisher publisher, bool storedeadletter)
        {
            _name = name;
            _waitEasyNetQPublisher = waitEasyNetQPublisher;
            _maxRetry = maxRetry;
            _modelNamingConventionController = modelNamingConventionController;
            _publisher = publisher;
            _storedeadletter = storedeadletter;
            if (explicitAcknowledgments)
            {
                _successExecutionHandler = HandleTrackingAsync;
                _failedExecutionHandler = HandleDeadletterQueueAndTrackingAsync;
            }
            else
            {
                _failedExecutionHandler = HandleDeadletterQueueWithoutTrackingAsync;
            }
        }

        public async Task HandleErrorAsync(MessageExecutionContext messageExecutionContext)
        {
            switch (messageExecutionContext.Exception)
            {
                case MalformedEventException ex:
                    throw ex;
                case DiscardEventException ex:
                    throw ex;
                default:
                    await TrySendToRetryQueue(messageExecutionContext);
                    break;
            }
        }

        private async Task TrySendToRetryQueue(MessageExecutionContext messageExecutionContext)
        {
            if (!messageExecutionContext.MessageProperties.HeadersPresent)
            {
                messageExecutionContext.MessageProperties.Headers = new Dictionary<string, object>();
            }
            int retrycount;
            if (messageExecutionContext.MessageProperties.Headers.ContainsKey("retrycount"))
            {
                retrycount = int.Parse(messageExecutionContext.MessageProperties.Headers["retrycount"].ToString());
            }
            else
            {
                retrycount = 0;
            }

            if (retrycount < _maxRetry)
            {
                _logger.Warning(messageExecutionContext.Exception,
                    $"Failed to process the event({retrycount}/{_maxRetry}): {messageExecutionContext.SerializedMessage}");
                messageExecutionContext.MessageProperties.Headers["retrycount"] = retrycount + 1;
                await _waitEasyNetQPublisher.PublishEventAsync(messageExecutionContext.BodyBytes, messageExecutionContext.MessageProperties,
                    _modelNamingConventionController.GetRetryRoutingKey(_name, retrycount + 1));
            }
            else 
            {
                _logger.Error(messageExecutionContext.Exception, $"Failed to process the event({retrycount}/{_maxRetry}): {messageExecutionContext.SerializedMessage}");
                await SendMessageToDeadLetterQueueAsync(messageExecutionContext, retrycount);
            }
        }

        private async Task SendMessageToDeadLetterQueueAsync(MessageExecutionContext messageExecutionContext,
            int retrycount)
        {
            IntegrationEvent<JObject> integrationEvent = null;

            if (!string.IsNullOrEmpty(messageExecutionContext.SerializedMessage))
            {
                integrationEvent = JsonConvert.DeserializeObject<IntegrationEvent<JObject>>(messageExecutionContext.SerializedMessage);
            }
            
            var deadLetterEventDescriptor =
                new DeadLetterEventDescriptor<JObject>
                {
                    CountOfAttempts = retrycount,
                    LastAttemptError = messageExecutionContext.Exception.ToString(),
                    Original = integrationEvent
                };

            messageExecutionContext.DeadLetterIntegrationEvent = new IntegrationEvent<DeadLetterEventDescriptor<JObject>>
            {
                EventType = _modelNamingConventionController.GetDeadLetterQueueMessageEventType(
                    integrationEvent?.EventType),
                Content = deadLetterEventDescriptor
            };

            await _failedExecutionHandler(messageExecutionContext);

        }

        public async Task HandleSuccessAsync(IntegrationEvent integrationEvent)
        {
            await _successExecutionHandler(integrationEvent);
        }

        private async Task HandleTrackingAsync(IntegrationEvent integrationEvent)
        {
            await _publisher.PublishEventAsync(
                new IntegrationEvent<string>(string.Empty, 
                    _modelNamingConventionController.GetTrackingMessageEventType(integrationEvent.EventType)),
                _modelNamingConventionController.GetTrackingRoutingKey(_name));
        }

        private async Task HandleDeadletterQueueAndTrackingAsync(MessageExecutionContext messageExecutionContext)
        {
            await HandleDeadletterQueueWithoutTrackingAsync(messageExecutionContext);

            await _publisher.PublishEventAsync(
                messageExecutionContext.DeadLetterIntegrationEvent,
                _modelNamingConventionController.GetDeadLetterQueueRoutingKey(_name));
        }

        private async Task HandleDeadletterQueueWithoutTrackingAsync(MessageExecutionContext messageExecutionContext)
        {
            if (_storedeadletter)
            {
                await _waitEasyNetQPublisher.PublishEventAsync(
                    messageExecutionContext.DeadLetterIntegrationEvent,
                    _modelNamingConventionController.GetDeadLetterQueueRoutingKey(_name), true);
            }

            await messageExecutionContext.Subscription.NotifyAboutDeadLetterAsync(messageExecutionContext.SerializedMessage,
                messageExecutionContext.Exception);
        }
    }
}