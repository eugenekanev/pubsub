using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using EasyNetQ;
using EasyNetQ.Topology;
using Serilog;
using Serilog.Context;
using Uptick.Platform.PubSub.Sdk.Exceptions;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public class EasyNetQSubscriber : ISubscriber
    {
        private readonly IErrorHandlingStrategy _errorHandlingStrategy;
        private readonly IAdvancedBus _bus;
        private readonly IQueue _queue;
        private readonly object _threadguard = new object();
        private bool _subscribed;
        private IDisposable _consumerDisposable;
        private readonly ILogger _logger = Log.ForContext<EasyNetQSubscriber>();
        private readonly ushort _prefetchcount;
        private readonly ISubscriptionSelector _subscriptionSelector;
        private readonly ISuccessHandlingStrategy _successHandlingStrategy;
        private readonly IDisposable _nameHandle;
        private bool _alreadyDisposed = false;
        private ConcurrentDictionary<string, ISubscription> _subscriptions;

        public EasyNetQSubscriber(IErrorHandlingStrategy errorHandlingStrategy, IAdvancedBus bus, IQueue queue, ushort prefetchcount,
            ISubscriptionSelector subscriptionSelector, ISuccessHandlingStrategy successHandlingStrategy, IDisposable nameHandle)
        {
            _errorHandlingStrategy = errorHandlingStrategy;
            _successHandlingStrategy = successHandlingStrategy;
            _bus = bus;
            _queue = queue;
            _prefetchcount = prefetchcount;
            _subscriptionSelector = subscriptionSelector;
            _nameHandle = nameHandle;
        }

        public void Subscribe(IDictionary<string, ISubscription> subscriptions)
        {
            lock (_threadguard)
            {
                if (_alreadyDisposed)
                    throw new
                        ObjectDisposedException("EasyNetQSubscriber",
                            "Called Subscribe Method on Disposed object");

                if (_subscribed == false)
                {
                    _subscriptions = new ConcurrentDictionary<string, ISubscription>(subscriptions);
                    _consumerDisposable = _bus.Consume(_queue, async (body, properties, info) =>
                    {
                        MessageExecutionContext messageExecutionContext = new MessageExecutionContext(body, properties, info);
                        try
                        {
                            messageExecutionContext.IntergrationEventParsingResult = DefaultIntergrationEventParser.Parse(body);
                            if (messageExecutionContext.IntergrationEventParsingResult.Success)
                            {
                                using (LogContext.PushProperty("CorrelationId", messageExecutionContext.IntergrationEventParsingResult.IntegrationEvent.CorrelationId))
                                {
                                    messageExecutionContext.Subscription = _subscriptionSelector.Select(_subscriptions,
                                        messageExecutionContext.IntergrationEventParsingResult.IntegrationEvent.EventType);

                                    if (messageExecutionContext.Subscription != null)
                                    {
                                        messageExecutionContext.SerializedMessage = Encoding.UTF8.GetString(body);
                                        _logger.Debug("Subscriber received message: {@body}", messageExecutionContext.SerializedMessage);
                                        await messageExecutionContext.Subscription.InvokeAsync(messageExecutionContext.SerializedMessage);
                                        await _successHandlingStrategy.HandleSuccessAsync(
                                            messageExecutionContext.IntergrationEventParsingResult.IntegrationEvent);
                                    }
                                    else
                                    {
                                        throw new DiscardEventException(
                                            $"The consumer for {messageExecutionContext.IntergrationEventParsingResult.IntegrationEvent.EventType} event type is not matched");
                                    }
                                }
                            }
                            else
                            {
                                throw new MalformedEventException(messageExecutionContext.IntergrationEventParsingResult.Errors);
                            }
                        }
                        catch (Exception e)
                        {
                            messageExecutionContext.Exception = e;
                            await _errorHandlingStrategy.HandleErrorAsync(messageExecutionContext);
                        }
                    }, configuration => configuration.WithPrefetchCount(_prefetchcount));
                    _subscribed = true;
                }
                else
                {
                    foreach (var subscription in subscriptions)
                        _subscriptions.AddOrUpdate(
                            subscription.Key, subscription.Value,
                            (key, oldValue) => subscription.Value);
                }
            }
        }

        public void UnSubscribe()
        {
            lock (_threadguard)
            {
                if (_alreadyDisposed)
                    throw new
                        ObjectDisposedException("EasyNetQSubscriber",
                            "Called UnSubscribe Method on Disposed object");

                if (_subscribed)
                {
                    _consumerDisposable.Dispose();
                    _subscribed = false;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_alreadyDisposed)
                return;

            if (isDisposing)
            {
                _consumerDisposable?.Dispose();
                _nameHandle.Dispose();
            }

            _alreadyDisposed = true;
        }
    }
}
