using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Exceptions;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ.Tests
{
    public class ExecutionHandlingStrategyTests
    {
        private readonly Mock<IPublisher> _publisher;
        private readonly Mock<IModelNamingConventionController> _modelNamingConventionController;
        private readonly Mock<IAdvancedBus> _mockBus;
        private readonly Mock<IExchange> _mockExchange;
        private readonly EasyNetQPublisher _easyNetQPublisher;
        private readonly string _exchangeName = "exchangeName";
        private readonly string _subscriberName = "subscriberName";
        private readonly int _maxRetry = 5;
        public ExecutionHandlingStrategyTests()
        {
            _publisher = new Mock<IPublisher>();
            _mockBus = new Mock<IAdvancedBus>();
            _modelNamingConventionController = new Mock<IModelNamingConventionController>();
            _easyNetQPublisher = new EasyNetQPublisher(_mockBus.Object, _exchangeName, _modelNamingConventionController.Object);
            _mockExchange = new Mock<IExchange>();
        }

        [Theory]
        [InlineData(true, typeof(MalformedEventException), true)]
        [InlineData(false, typeof(MalformedEventException), true)]
        [InlineData(true, typeof(DiscardEventException), true)]
        [InlineData(false, typeof(DiscardEventException), true)]
        [InlineData(true, typeof(MalformedEventException), false)]
        [InlineData(false, typeof(MalformedEventException), false)]
        [InlineData(true, typeof(DiscardEventException), false)]
        [InlineData(false, typeof(DiscardEventException), false)]
        public void HandleErrorAsync_ExceptionThatShouldNotBeCatched_ShouldThrowException(bool explicitAcknowledgments, Type exceptionType, bool storedeadletter)
        {
            //prepare
            ExecutionHandlingStrategy executionHandlingStrategy = new ExecutionHandlingStrategy(_subscriberName, _easyNetQPublisher,
                _maxRetry, _modelNamingConventionController.Object, explicitAcknowledgments, _publisher.Object, storedeadletter);

            //act
            Func<Task> f = async () =>
                {
                    await executionHandlingStrategy.HandleErrorAsync(new MessageExecutionContext(null,null,null){Exception = (Exception)Activator.CreateInstance(exceptionType) });
                };

            //check
            f.ShouldThrow<Exception>();

            _publisher.Verify(m => m.PublishEventAsync(It.IsAny<IntegrationEvent<string>>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            _mockBus.Verify(x=> x.PublishAsync(It.IsAny<IExchange>(), It.IsAny<string>(), 
                It.IsAny<bool>(), It.IsAny<MessageProperties>(), It.IsAny<byte[]>()), Times.Never);

        }

        [Theory]
        [InlineData(true,0, true)]
        [InlineData(false,0, true)]
        [InlineData(true, 1, true)]
        [InlineData(false, 1, true)]
        [InlineData(true, 0, false)]
        [InlineData(false, 0, false)]
        [InlineData(true, 1, false)]
        [InlineData(false, 1, false)]
        public async void HandleErrorAsync_ExceptionThatShouldBeCatchedANDAttemptNumberN_MessageShouldBeSentToAppropriateRetryQueue(bool explicitAcknowledgments, 
            int numberOfAttempt, bool storedeadletter)
        {
            //prepare
            MessageProperties messageProperties = new MessageProperties(); 
            if (numberOfAttempt != 0)
            {
                messageProperties.Headers = new Dictionary<string, object>();
                messageProperties.Headers["retrycount"] = numberOfAttempt;
            }
         

            BookingCreated bookingCreated = new BookingCreated{BookingName = "test"};
            string bookingCreatedEventType = "bookingCreatedEventType";
            IntegrationEvent<BookingCreated> integrationEvent = new IntegrationEvent<BookingCreated>(bookingCreated, bookingCreatedEventType);
            var message = JsonConvert.SerializeObject(integrationEvent);
            var body = Encoding.UTF8.GetBytes(message);
            Exception exception = new IOException("IOException");

            ExecutionHandlingStrategy executionHandlingStrategy = new ExecutionHandlingStrategy(_subscriberName, _easyNetQPublisher,
                _maxRetry, _modelNamingConventionController.Object, explicitAcknowledgments, _publisher.Object, storedeadletter);

            string retryRoutingKey = string.Concat("retry.", numberOfAttempt+1, ".", _subscriberName);
            _modelNamingConventionController.Setup(x => x.GetRetryRoutingKey(_subscriberName, numberOfAttempt+1))
                .Returns((string subscribername, int retryindex) => retryRoutingKey);



            string actualRouting = string.Empty;
            MessageProperties actualProperties = null;
            byte[] actualMsg = null;
            IExchange actualExchange = null;

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                        bool passive, bool durable, bool autoDelete, bool internalflag,
                        string alternateExchange, bool delayed) =>
                    { return Task.FromResult(_mockExchange.Object); });

            _mockBus.Setup(x => x.PublishAsync(It.IsAny<IExchange>(), It.IsAny<string>(), false, It.IsAny<MessageProperties>(), It.IsAny<byte[]>()))
                .Returns((IExchange exchange, string routing, bool mandatory,
                        MessageProperties properties, byte[] msg) =>
                    { return Task.FromResult(true); })
                .Callback<IExchange, string, bool, MessageProperties, byte[]>(
                    (exchange, routing, mandatory, properties, msg) =>
                    {
                        actualExchange = exchange;
                        actualRouting = routing;
                        actualProperties = properties;
                        actualMsg = msg;
                    });


            //act
            Mock<ISubscription> mockedSubscription = new Mock<ISubscription>();
            MessageExecutionContext messageExecutionContext =
                new MessageExecutionContext(body, messageProperties, null)
                {
                    Exception = exception,
                    SerializedMessage = message,
                    Subscription = mockedSubscription.Object,
                    IntergrationEventParsingResult = null,
                    DeadLetterIntegrationEvent = null
                };
            await executionHandlingStrategy.HandleErrorAsync(messageExecutionContext);


            //check
            actualRouting.Should().Be(retryRoutingKey);
            actualMsg.Should().BeEquivalentTo(body);
            (int.Parse(actualProperties.Headers["retrycount"].ToString())).Should().Be(numberOfAttempt+1);
            actualExchange.Should().Be(_mockExchange.Object);

            _publisher.Verify(m => m.PublishEventAsync(It.IsAny<IntegrationEvent<string>>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            mockedSubscription.Verify(s=>s.NotifyAboutDeadLetterAsync(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
            mockedSubscription.Verify(s => s.InvokeAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public async void HandleErrorAsync_ExceptionThatShouldBeCatchedANDLastAttempt_MessageShouldBeSentToAppropriateDeadLetterQueue(bool explicitAcknowledgments, 
            bool storedeadletter)
        {
            //prepare
            MessageProperties messageProperties = new MessageProperties();
            messageProperties.Headers = new Dictionary<string, object>();
            messageProperties.Headers["retrycount"] = _maxRetry;

            BookingCreated bookingCreated = new BookingCreated { BookingName = "test" };
            string bookingCreatedEventType = "bookingCreatedEventType";
            IntegrationEvent<BookingCreated> integrationEvent = new IntegrationEvent<BookingCreated>(bookingCreated, bookingCreatedEventType);
            var message = JsonConvert.SerializeObject(integrationEvent);
            var body = Encoding.UTF8.GetBytes(message);
            Exception exception = new IOException("IOException");

            ExecutionHandlingStrategy executionHandlingStrategy = new ExecutionHandlingStrategy(_subscriberName, _easyNetQPublisher,
                _maxRetry, _modelNamingConventionController.Object, explicitAcknowledgments, _publisher.Object, storedeadletter);

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                        bool passive, bool durable, bool autoDelete, bool internalflag,
                        string alternateExchange, bool delayed) =>
                    { return Task.FromResult(_mockExchange.Object); });


            _modelNamingConventionController.Setup(x => x.GetDeadLetterQueueMessageEventType(bookingCreatedEventType))
                .Returns((string eventType) => string.Concat("deadletter.", eventType));

            _modelNamingConventionController.Setup(x => x.GetDeadLetterQueueRoutingKey(_subscriberName))
                .Returns((string subscriberName) => string.Concat("deadletter.", subscriberName));

            //Main flow
            string actualRouting = string.Empty;
            MessageProperties actualProperties = null;
            byte[] actualMsg = null;
            IExchange actualExchange = null;

            _mockBus.Setup(x => x.PublishAsync(It.IsAny<IExchange>(), It.IsAny<string>(), false, It.IsAny<MessageProperties>(), It.IsAny<byte[]>()))
                .Returns((IExchange exchange, string routing, bool mandatory,
                        MessageProperties properties, byte[] msg) =>
                    { return Task.FromResult(true); })
                .Callback<IExchange, string, bool, MessageProperties, byte[]>(
                    (exchange, routing, mandatory, properties, msg) =>
                    {
                        actualExchange = exchange;
                        actualRouting = routing;
                        actualProperties = properties;
                        actualMsg = msg;
                    });
            //END Main flow

            //Tracking flow
            IntegrationEvent<DeadLetterEventDescriptor<JObject>> actualTrackingMessage = null;
            string actualTrackingMessageRouting = string.Empty;
            bool actualPersistantMode = false;
            _publisher.Setup(x => x.PublishEventAsync(It.IsAny<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(), It.IsAny<string>(), true))
                .Returns(Task.FromResult(true))
                .Callback<IntegrationEvent<DeadLetterEventDescriptor<JObject>>, string, bool>(
                    (trackingMessage, routing, persistantMode) =>
                    {
                        actualTrackingMessage = trackingMessage;
                        actualTrackingMessageRouting = routing;
                        actualPersistantMode = persistantMode;
                    });

            //END Tracking flow


            //act
            Mock<ISubscription> mockedSubscription = new Mock<ISubscription>();
            MessageExecutionContext messageExecutionContext =
                new MessageExecutionContext(body, messageProperties, null)
                {
                    Exception = exception,
                    SerializedMessage = message,
                    Subscription = mockedSubscription.Object,
                    IntergrationEventParsingResult = null,
                    DeadLetterIntegrationEvent = null
                };

            await executionHandlingStrategy.HandleErrorAsync(messageExecutionContext);

            //check
            var expectedDeadLetterEventDescriptor =
                new DeadLetterEventDescriptor<JObject>
                {
                    CountOfAttempts = _maxRetry,
                    LastAttemptError = exception.ToString(),
                    Original = JsonConvert.DeserializeObject<IntegrationEvent<JObject>>(message)
                };

            var expectedDeadletterqueueevent = new IntegrationEvent<DeadLetterEventDescriptor<JObject>>
            {
                CorrelationId = integrationEvent.CorrelationId,
                EventType = string.Concat("deadletter.", bookingCreatedEventType),
                Content = expectedDeadLetterEventDescriptor
            };

            //Check publishing dead letter message
            if (storedeadletter)
            {
                var actuaDeadLetterQueuelSerializedMessage = Encoding.UTF8.GetString(actualMsg);
                IntegrationEvent<DeadLetterEventDescriptor<JObject>> actualDeadLetterQueueIntegrationEvent =
                    JsonConvert.DeserializeObject<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(actuaDeadLetterQueuelSerializedMessage);


                actualDeadLetterQueueIntegrationEvent.CorrelationId.Should().Be(expectedDeadletterqueueevent.CorrelationId);
                actualDeadLetterQueueIntegrationEvent.Content.ShouldBeEquivalentTo(expectedDeadletterqueueevent.Content);
                actualDeadLetterQueueIntegrationEvent.EventType.Should()
                    .Be(expectedDeadletterqueueevent.EventType);

                actualRouting.Should().Be(string.Concat("deadletter.", _subscriberName));
                actualExchange.Should().Be(_mockExchange.Object);
                actualProperties.DeliveryMode.Should().Be(2);//persistant delivery mode
            }
            else
            {
                _mockBus.Verify(v => v.PublishAsync(It.IsAny<IExchange>(), It.IsAny<string>(), false, It.IsAny<MessageProperties>(), It.IsAny<byte[]>()), Times.Never);
            }

            //Check Subsciption notification
            mockedSubscription.Verify(s => s.NotifyAboutDeadLetterAsync(messageExecutionContext.SerializedMessage, messageExecutionContext.Exception), Times.Once);
            mockedSubscription.Verify(s => s.InvokeAsync(It.IsAny<string>()), Times.Never);

            //Check Tracking flow
            if (explicitAcknowledgments == false)
            {
                //check, that No deadletter message for tracking is sent.
                _publisher.Verify(v => v.PublishEventAsync(It.IsAny<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            }
            else
            {
                //check, that deadltter message for tracking is equal to the one sent to the deadletter queue. Routing key must be the same
                _publisher.Verify(v => v.PublishEventAsync(It.IsAny<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);

                actualTrackingMessageRouting.Should().Be(string.Concat("deadletter.", _subscriberName));
                actualPersistantMode.Should().BeTrue();
                actualTrackingMessage.CorrelationId.Should().Be(expectedDeadletterqueueevent.CorrelationId);
                actualTrackingMessage.Content.ShouldBeEquivalentTo(expectedDeadletterqueueevent.Content);
                actualTrackingMessage.EventType.Should()
                    .Be(expectedDeadletterqueueevent.EventType);
            }

        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async void HandleSuccessAsync(bool explicitAcknowledgments,
            bool storedeadletter)
        {
            //prepare
            MessageProperties messageProperties = new MessageProperties();

            BookingCreated bookingCreated = new BookingCreated { BookingName = "test" };
            string bookingCreatedEventType = "bookingCreatedEventType";
            IntegrationEvent<BookingCreated> integrationEvent = new IntegrationEvent<BookingCreated>(bookingCreated, bookingCreatedEventType);
            var message = JsonConvert.SerializeObject(integrationEvent);
            var body = Encoding.UTF8.GetBytes(message);

            ExecutionHandlingStrategy executionHandlingStrategy = new ExecutionHandlingStrategy(_subscriberName, _easyNetQPublisher,
                _maxRetry, _modelNamingConventionController.Object, explicitAcknowledgments, _publisher.Object, storedeadletter);

            _modelNamingConventionController.Setup(x => x.GetTrackingMessageEventType(bookingCreatedEventType))
                .Returns((string eventType) => string.Concat("ok.", eventType));

            _modelNamingConventionController.Setup(x => x.GetTrackingRoutingKey(_subscriberName))
                .Returns((string subscriberName) => string.Concat("ok.", subscriberName));

            //Tracking flow
            IntegrationEvent<string> actualTrackingMessage = null;
            string actualTrackingMessageRouting = string.Empty;
            bool actualPersistantMode = false;
            _publisher.Setup(x => x.PublishEventAsync(It.IsAny<IntegrationEvent<string>>(), It.IsAny<string>(), true))
                .Returns(Task.FromResult(true))
                .Callback<IntegrationEvent<string>, string, bool>(
                    (trackingMessage, routing, persistantMode) =>
                    {
                        actualTrackingMessage = trackingMessage;
                        actualTrackingMessageRouting = routing;
                        actualPersistantMode = persistantMode;
                    });

            //act
            await executionHandlingStrategy.HandleSuccessAsync(integrationEvent);

            //check
            if (explicitAcknowledgments == false)
            {
                //check, that No messages for tracking are sent.
                _publisher.Verify(v => v.PublishEventAsync(It.IsAny<IntegrationEvent<string>>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            }
            else
            {
                _publisher.Verify(v => v.PublishEventAsync(It.IsAny<IntegrationEvent<string>>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);

                actualTrackingMessageRouting.Should().Be(string.Concat("ok.", _subscriberName));
                actualPersistantMode.Should().BeTrue();
                actualTrackingMessage.EventType.Should()
                    .Be(string.Concat("ok.", bookingCreatedEventType));
                actualTrackingMessage.CorrelationId.Should().Be(integrationEvent.CorrelationId);
                actualTrackingMessage.Content.Should().BeEmpty();
            }
        }

        #region Helpers
        public class BookingCreated
        {
            public string BookingName { get; set; }
        }
        #endregion
    }
}
