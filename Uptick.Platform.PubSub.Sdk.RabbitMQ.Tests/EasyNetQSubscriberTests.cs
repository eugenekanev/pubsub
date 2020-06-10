using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using SemanticComparison.Fluent;
using Uptick.Platform.PubSub.Sdk.Exceptions;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ.Tests
{
    public class EasyNetQSubscriberTests
    {
        private readonly Mock<IAdvancedBus> _mockBus;
        private readonly Mock<IBus> _mockMainBus;

        private readonly Mock<IQueue> _mockQueue;
        private readonly EasyNetQPublisher _easyNetQPublisher;
        private readonly Mock<ISuccessHandlingStrategy> _successHandlingStrategy;
        private readonly Mock<IErrorHandlingStrategy> _errorHandlingStrategy;
        private readonly Mock<IDisposable> _consumerDisposable;
        private readonly Mock<IModelNamingConventionController> _modelNamingConventionController;
        private readonly Mock<ISubscriptionSelector> _subsciptionSelector;
        private readonly Mock<IDisposable> _nameHandle;
        private readonly EasyNetQSubscriber _easyNetQSubscriber;
        private readonly EasyNetQBroker _easyNetQBroker;
        private readonly string _bookingEventType;
        private readonly string _version;
        private readonly string _bookingName;
        private readonly ushort _prefetchcount;

        public interface IDisconnectHandler
        {
            void HandleEvent();
        }

        public EasyNetQSubscriberTests()
        {
            var exchangename = "exchangeName";

            _bookingEventType = "bookingcreated";
            _version = "0.1";
            _bookingName = "bookingName";

            _mockBus = new Mock<IAdvancedBus>();
            var mockContainer = new Mock<IContainer>();
            _mockBus.SetupGet(x => x.Container).Returns(mockContainer.Object);
            _mockMainBus = new Mock<IBus>();
            _mockMainBus.SetupGet(x => x.Advanced).Returns(_mockBus.Object);

            _mockQueue = new Mock<IQueue>();

            _prefetchcount = 5;

            _modelNamingConventionController = new Mock<IModelNamingConventionController>();
            _easyNetQPublisher = new EasyNetQPublisher(_mockBus.Object, exchangename, _modelNamingConventionController.Object);
            _consumerDisposable = new Mock<IDisposable>();
          

            _successHandlingStrategy = new Mock<ISuccessHandlingStrategy>();
            _successHandlingStrategy.Setup(x => x.HandleSuccessAsync(It.IsAny<IntegrationEvent>()))
                .Returns(Task.FromResult(true));
            _errorHandlingStrategy = new Mock<IErrorHandlingStrategy>();
            _errorHandlingStrategy.Setup(x => x.HandleErrorAsync(It.IsAny<MessageExecutionContext>()))
                .Returns(Task.FromResult(true));

            _subsciptionSelector = new Mock<ISubscriptionSelector>();
            _nameHandle = new Mock<IDisposable>();

            var mockEnvironmentNamingConventionController = new Mock<IEnvironmentNamingConventionController>();
            _easyNetQBroker = new EasyNetQBroker(
                (connectionString, connected, disconnected, registerServices) =>
                {
                    _mockMainBus.Object.Advanced.Disconnected += disconnected;
                    _mockMainBus.Object.Advanced.Connected += connected;
                    return _mockBus.Object;
                }, string.Empty, string.Empty, string.Empty, mockEnvironmentNamingConventionController.Object);

            _easyNetQSubscriber = new EasyNetQSubscriber(_errorHandlingStrategy.Object, _mockBus.Object,
                _mockQueue.Object, _prefetchcount, _subsciptionSelector.Object, _successHandlingStrategy.Object, _nameHandle.Object);
        }

        [Fact]
        public async void Subscribe_CorrectlyFormedEvent_ProcessingWithoutErrors_NoExceptionsShouldBeThrown()
        {
            //prepare
            IntegrationEvent<BookingCreated> integrationEvent =
                new IntegrationEvent<BookingCreated>()
                {
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    EventType = _bookingEventType,
                    Version = _version,
                    Content = new BookingCreated
                    {
                        BookingName = _bookingName
                    }
                };

            var messagebytes = SerializeEvent(integrationEvent);

            EventEmulator eventEmulator = null;
            _mockBus.Setup(x => x.Consume(_mockQueue.Object,
                It.IsAny<Func<byte[], MessageProperties, MessageReceivedInfo, Task>>(), It.IsAny<Action<IConsumerConfiguration>>()))
                .Returns((IQueue queue, Func<byte[],
                    MessageProperties, MessageReceivedInfo, Task> func, Action<IConsumerConfiguration> configure) =>
                {
                    eventEmulator = new EventEmulator(func);
                    return _consumerDisposable.Object;
                });

            var mockSubscription = new Mock<ISubscription>();
            mockSubscription.Setup(x=>x.InvokeAsync(Encoding.UTF8.GetString(messagebytes))).Returns(Task.FromResult(true));


            Dictionary<string, ISubscription> subscriptions = new Dictionary<string, ISubscription> { { "bookingcreated", mockSubscription.Object } };
            _subsciptionSelector.Setup(x => x.Select(subscriptions, "bookingcreated"))
                .Returns((IDictionary<string, ISubscription> subs,string eventType) => mockSubscription.Object);

            _easyNetQSubscriber.Subscribe(subscriptions);

            //Act
            MessageProperties messageProperties = new MessageProperties();
            await eventEmulator.Execute(messagebytes, messageProperties, null);

            //check
            eventEmulator.RaisedException.Should().BeNull();
            mockSubscription.Verify(x => x.InvokeAsync(Encoding.UTF8.GetString(messagebytes)),Times.Once);
            mockSubscription.Verify(x => x.NotifyAboutDeadLetterAsync(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);

            var parsedMessage = DefaultIntergrationEventParser.Parse(messagebytes);

            var expectedEvent = parsedMessage.IntegrationEvent
                .AsSource()
                .OfLikeness<IntegrationEvent>()
                .CreateProxy();

            _successHandlingStrategy.Verify(x => x.HandleSuccessAsync(expectedEvent));

        }

        [Fact]
        public void Subscribe_DisconnectEventReseived_CallbackHandlerMustExecuted()
        {
            var disconnectHandler = new Mock<IDisconnectHandler>();
            _easyNetQBroker.RegisterDisconnectedAction(disconnectHandler.Object.HandleEvent);

            // raise event
            _mockBus.Raise(x => x.Disconnected += null, EventArgs.Empty);
            
            // check
            disconnectHandler.Verify(x => x.HandleEvent(), Times.Once);
        }

        [Fact]
        public async void Subscribe_CorrectlyFormedEvent_ProcessingWithoutErrors_MultipeConsumers_WithDefaultConsumer()
        {
            //prepare
            IntegrationEvent<BookingCreated> bookingEvent =
                new IntegrationEvent<BookingCreated>
                {
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    EventType = _bookingEventType,
                    Version = _version,
                    Content = new BookingCreated
                    {
                        BookingName = _bookingName
                    }
                };

            IntegrationEvent<RelationshipCreated> relEvent =
                new IntegrationEvent<RelationshipCreated>
                {
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    EventType = "relationshipcreated",
                    Version = _version,
                    Content = new RelationshipCreated
                    {
                        Relationship = "rel1"
                    }
                };

            IntegrationEvent<PersonCreated> personEvent =
                new IntegrationEvent<PersonCreated>
                {
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    EventType = "personcreated",
                    Version = _version,
                    Content = new PersonCreated
                    {
                        FirstName = "John"
                    }
                };

            var bookingMessagebytes = SerializeEvent(bookingEvent);
            var relMessagebytes = SerializeEvent(relEvent);
            var personMessagebytes = SerializeEvent(personEvent);

            EventEmulator eventEmulator = null;
            _mockBus.Setup(x => x.Consume(_mockQueue.Object,
                It.IsAny<Func<byte[], MessageProperties, MessageReceivedInfo, Task>>(), It.IsAny<Action<IConsumerConfiguration>>()))
                .Returns((IQueue queue, Func<byte[],
                    MessageProperties, MessageReceivedInfo, Task> func, Action<IConsumerConfiguration> configure) =>
                {
                    eventEmulator = new EventEmulator(func);
                    return _consumerDisposable.Object;
                });


            var mockBookingProcessingSubscription = new Mock<ISubscription>();
            mockBookingProcessingSubscription.Setup(x => x.InvokeAsync(Encoding.UTF8.GetString(bookingMessagebytes))).Returns(Task.FromResult(true));
            var mockRelationshipProcessingSubscription = new Mock<ISubscription>();
            mockRelationshipProcessingSubscription.Setup(x => x.InvokeAsync(Encoding.UTF8.GetString(relMessagebytes))).Returns(Task.FromResult(true));
            var mockPersonProcessingSubscription = new Mock<ISubscription>();
            mockPersonProcessingSubscription.Setup(x => x.InvokeAsync(Encoding.UTF8.GetString(personMessagebytes))).Returns(Task.FromResult(true));

            ConcurrentDictionary<string, ISubscription> subscriptions = new ConcurrentDictionary<string, ISubscription>
            {
                { "bookingcreated", mockBookingProcessingSubscription.Object },
                { "relationshipcreated", mockRelationshipProcessingSubscription.Object },
                { "*", mockPersonProcessingSubscription.Object }
            };
            _subsciptionSelector.Setup(x => x.Select(subscriptions, "bookingcreated"))
                .Returns((IDictionary<string, ISubscription> subs, string eventType) => mockBookingProcessingSubscription.Object);
            _subsciptionSelector.Setup(x => x.Select(subscriptions, "relationshipcreated"))
                .Returns((IDictionary<string, ISubscription> subs, string eventType) => mockRelationshipProcessingSubscription.Object);
            _subsciptionSelector.Setup(x => x.Select(subscriptions, It.Is<string>(str => str!= "bookingcreated" && str != "relationshipcreated")))
                .Returns((IDictionary<string, ISubscription> subs, string eventType) => mockPersonProcessingSubscription.Object);

            _easyNetQSubscriber.Subscribe(subscriptions);


            //Act
            MessageProperties messageProperties = new MessageProperties();
            await eventEmulator.Execute(bookingMessagebytes, messageProperties, null);
            await eventEmulator.Execute(relMessagebytes, messageProperties, null);
            await eventEmulator.Execute(personMessagebytes, messageProperties, null);

            //check

            eventEmulator.RaisedException.Should().BeNull();
            mockBookingProcessingSubscription.Verify(x => x.NotifyAboutDeadLetterAsync(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
            mockRelationshipProcessingSubscription.Verify(x => x.NotifyAboutDeadLetterAsync(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
            mockPersonProcessingSubscription.Verify(x => x.NotifyAboutDeadLetterAsync(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);

            mockBookingProcessingSubscription.Verify(x => x.InvokeAsync(Encoding.UTF8.GetString(bookingMessagebytes)), Times.Once);
            mockRelationshipProcessingSubscription.Verify(x => x.InvokeAsync(Encoding.UTF8.GetString(relMessagebytes)), Times.Once);
            mockPersonProcessingSubscription.Verify(x => x.InvokeAsync(Encoding.UTF8.GetString(personMessagebytes)), Times.Once);

            var parsedBookingsMessage = DefaultIntergrationEventParser.Parse(bookingMessagebytes);
            var expectedEvent = parsedBookingsMessage.IntegrationEvent
                .AsSource()
                .OfLikeness<IntegrationEvent>()
                .CreateProxy();
            _successHandlingStrategy.Verify(x => x.HandleSuccessAsync(expectedEvent));

            var parsedRelationshipMessage = DefaultIntergrationEventParser.Parse(relMessagebytes);
            expectedEvent = parsedRelationshipMessage.IntegrationEvent
                .AsSource()
                .OfLikeness<IntegrationEvent>()
                .CreateProxy();
            _successHandlingStrategy.Verify(x => x.HandleSuccessAsync(expectedEvent));

            var parsedPersonMessage = DefaultIntergrationEventParser.Parse(personMessagebytes);
            expectedEvent = parsedPersonMessage.IntegrationEvent
                .AsSource()
                .OfLikeness<IntegrationEvent>()
                .CreateProxy();
            _successHandlingStrategy.Verify(x => x.HandleSuccessAsync(expectedEvent));
        }

        [Fact]
        public async void Subscribe_CorrectlyFormedEvent_NoConsumerForEventTypeMatched()
        {
            //prepare
            IntegrationEvent<BookingCreated> integrationEvent =
                new IntegrationEvent<BookingCreated>()
                {
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    EventType = _bookingEventType,
                    Version = _version,
                    Content = new BookingCreated
                    {
                        BookingName = _bookingName
                    }
                };

            var messagebytes = SerializeEvent(integrationEvent);

            EventEmulator eventEmulator = null;
            _mockBus.Setup(x => x.Consume(_mockQueue.Object,
                It.IsAny<Func<byte[], MessageProperties, MessageReceivedInfo, Task>>(),
                    It.IsAny<Action<IConsumerConfiguration>>()))
                .Returns((IQueue queue, Func<byte[],
                    MessageProperties, MessageReceivedInfo, Task> func, Action<IConsumerConfiguration> configure) =>
                {
                    eventEmulator = new EventEmulator(func);
                    return _consumerDisposable.Object;
                });



            Dictionary<string, ISubscription> subscriptions = new Dictionary<string, ISubscription>();
            _subsciptionSelector.Setup(x => x.Select(subscriptions, _bookingEventType))
                .Returns((IDictionary<string, ISubscription> subs, string eventType) => null);

            MessageProperties messageProperties = new MessageProperties();

            MessageExecutionContext actualMessageExecutionContext = null;
            _errorHandlingStrategy.Setup(x=>x.HandleErrorAsync(It.IsAny<MessageExecutionContext>()))
                .Returns<MessageExecutionContext>(
                    (messageExecutionContext) =>
                    {
                        actualMessageExecutionContext = messageExecutionContext;
                        return Task.FromException<DiscardEventException>(messageExecutionContext.Exception);
                    });



            _easyNetQSubscriber.Subscribe(subscriptions);


            //Act
          
            await eventEmulator.Execute(messagebytes, messageProperties, null);

            //check
            _successHandlingStrategy.Verify(x => x.HandleSuccessAsync(It.IsAny<IntegrationEvent>()), Times.Never);
            _errorHandlingStrategy.Verify(x => x.HandleErrorAsync(It.IsAny<MessageExecutionContext>()), Times.Once);
            actualMessageExecutionContext.BodyBytes.ShouldBeEquivalentTo(messagebytes);
            actualMessageExecutionContext.Exception.Should().Be(eventEmulator.RaisedException);
            actualMessageExecutionContext.MessageProperties.Should().Be(messageProperties);
            actualMessageExecutionContext.IntergrationEventParsingResult.ShouldBeEquivalentTo(DefaultIntergrationEventParser.Parse(messagebytes));
            actualMessageExecutionContext.SerializedMessage.Should().BeNull();
            actualMessageExecutionContext.DeadLetterIntegrationEvent.Should().BeNull();
            actualMessageExecutionContext.Subscription.Should().BeNull();

            eventEmulator.RaisedException.Should().BeOfType<DiscardEventException>().Which.Message
                .ShouldBeEquivalentTo("The consumer for bookingcreated event type is not matched");
        }

        [Fact]
        public async void Subscribe_ChangeTheSubscriber_AppropriateConsumerShouldBeUsed()
        {
            //prepare
            var content = new BookingCreated() { BookingName = _bookingName };
            IntegrationEvent<BookingCreated> integrationEvent =
                new IntegrationEvent<BookingCreated>()
                {
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    EventType = _bookingEventType,
                    Version = _version,
                    Content = content
                };

            var messagebytes = SerializeEvent(integrationEvent);

            EventEmulator eventEmulator = null;
            _mockBus.Setup(x => x.Consume(_mockQueue.Object,
                    It.IsAny<Func<byte[], MessageProperties, MessageReceivedInfo, Task>>(),
                    It.IsAny<Action<IConsumerConfiguration>>()))
                .Returns((IQueue queue, Func<byte[],
                    MessageProperties, MessageReceivedInfo, Task> func,
                    Action<IConsumerConfiguration> configure) =>
                {
                    eventEmulator = new EventEmulator(func);
                    return _consumerDisposable.Object;
                });


            var mockSubscription = new Mock<ISubscription>();
            mockSubscription.Setup(x => x.InvokeAsync(Encoding.UTF8.GetString(messagebytes))).Returns(Task.FromResult(true));

            Dictionary<string, ISubscription> subscriptions = new Dictionary<string, ISubscription> { { "bookingcreated", mockSubscription.Object } };
            _subsciptionSelector.Setup(x => x.Select(subscriptions, "bookingcreated"))
                .Returns((IDictionary<string, ISubscription> subs, string eventType) => mockSubscription.Object);

            _easyNetQSubscriber.Subscribe(subscriptions);


            MessageProperties messageProperties = new MessageProperties();
            await eventEmulator.Execute(messagebytes, messageProperties, null);


            //Act
            _easyNetQSubscriber.UnSubscribe();

            var mockSubscription2 = new Mock<ISubscription>();
            mockSubscription2.Setup(x => x.InvokeAsync(Encoding.UTF8.GetString(messagebytes))).Returns(Task.FromResult(true));

            Dictionary<string, ISubscription> subscriptions2 = new Dictionary<string, ISubscription> { { "bookingcreated", mockSubscription2.Object } };
            _subsciptionSelector.Setup(x => x.Select(subscriptions2, "bookingcreated"))
                .Returns((IDictionary<string, ISubscription> subs, string eventType) => mockSubscription2.Object);

            _easyNetQSubscriber.Subscribe(subscriptions2);


            await eventEmulator.Execute(messagebytes, messageProperties, null);


            //check

            eventEmulator.RaisedException.Should().BeNull();

            mockSubscription.Verify(x => x.InvokeAsync(Encoding.UTF8.GetString(messagebytes)), Times.Once);
            mockSubscription2.Verify(x => x.InvokeAsync(Encoding.UTF8.GetString(messagebytes)), Times.Once);

        }

        [Fact]
        public async void Subscribe_MalformedEvent_ExceptionShouldBeThrown()
        {
            //prepare
            var messagebytes = Encoding.UTF8.GetBytes(@"{
                                                            ""Version"": ""0.1""
                                                        }");

            EventEmulator eventEmulator = null;
            _mockBus.Setup(x => x.Consume(_mockQueue.Object,
                    It.IsAny<Func<byte[], MessageProperties, MessageReceivedInfo, Task>>(),
                    It.IsAny<Action<IConsumerConfiguration>>()))
                .Returns((IQueue queue, Func<byte[],
                    MessageProperties, MessageReceivedInfo, Task> func,
                    Action<IConsumerConfiguration> configure) =>
                {
                    eventEmulator = new EventEmulator(func);
                    return _consumerDisposable.Object;
                });


            Dictionary<string, ISubscription> subscriptions = new Dictionary<string, ISubscription>();

            MessageProperties messageProperties = new MessageProperties();

            MessageExecutionContext actualMessageExecutionContext = null;
            _errorHandlingStrategy.Setup(x => x.HandleErrorAsync(
                    It.IsAny<MessageExecutionContext>()))
                .Returns<MessageExecutionContext>(
                    (executionContext) =>
                    {
                        actualMessageExecutionContext = executionContext;
                        return Task.FromException<MalformedEventException>(executionContext.Exception);
                    });

            _easyNetQSubscriber.Subscribe(subscriptions);

            //Act
            await eventEmulator.Execute(messagebytes, messageProperties, null);

            //check
            ((MalformedEventException)eventEmulator.RaisedException).
                Errors.First().ShouldBeEquivalentTo("EventId token is missed");

            _successHandlingStrategy.Verify(x => x.HandleSuccessAsync(It.IsAny<IntegrationEvent>()), Times.Never);
            _errorHandlingStrategy.Verify(x => x.HandleErrorAsync(It.IsAny<MessageExecutionContext>()), Times.Once);
            actualMessageExecutionContext.BodyBytes.ShouldBeEquivalentTo(messagebytes);
            actualMessageExecutionContext.Exception.Should().Be(eventEmulator.RaisedException);
            actualMessageExecutionContext.MessageProperties.Should().Be(messageProperties);
            actualMessageExecutionContext.IntergrationEventParsingResult.ShouldBeEquivalentTo(DefaultIntergrationEventParser.Parse(messagebytes));
            actualMessageExecutionContext.SerializedMessage.Should().BeNull();
            actualMessageExecutionContext.DeadLetterIntegrationEvent.Should().BeNull();
            actualMessageExecutionContext.Subscription.Should().BeNull();

        }

        [Fact]
        public async void Subscribe_DiscardEventException_WhenProcessed_ExceptionShouldBeThrown()
        {
            //prepare
            var content = new BookingCreated() { BookingName = _bookingName };
            IntegrationEvent<BookingCreated> integrationEvent =
                new IntegrationEvent<BookingCreated>
                {
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventId = Guid.NewGuid(),
                    EventType = _bookingEventType,
                    Version = _version,
                    Content = content
                };

            var messagebytes = SerializeEvent(integrationEvent);

            EventEmulator eventEmulator = null;
            _mockBus.Setup(x => x.Consume(_mockQueue.Object,
                    It.IsAny<Func<byte[], MessageProperties, MessageReceivedInfo, Task>>(),
                    It.IsAny<Action<IConsumerConfiguration>>()))
                .Returns((IQueue queue, Func<byte[],
                    MessageProperties, MessageReceivedInfo, Task> func,
                    Action<IConsumerConfiguration> configure) =>
                {
                    eventEmulator = new EventEmulator(func);
                    return _consumerDisposable.Object;
                });


            DiscardEventException discardEventException = new DiscardEventException();
            var mockSubscription = new Mock<ISubscription>();
            mockSubscription.Setup(x => x.InvokeAsync(Encoding.UTF8.GetString(messagebytes)))
                .Returns<string>(
                    (serilizedmessage) =>
                    {
                        return Task.FromException<DiscardEventException>(discardEventException);
                    });


            Dictionary<string, ISubscription> subscriptions = new Dictionary<string, ISubscription> { { "bookingcreated", mockSubscription.Object } };
            _subsciptionSelector.Setup(x => x.Select(subscriptions, "bookingcreated"))
                .Returns((IDictionary<string, ISubscription> subs, string eventType) => mockSubscription.Object);

            MessageProperties messageProperties = new MessageProperties();
            MessageExecutionContext actualMessageExecutionContext = null;
            _errorHandlingStrategy.Setup(x => x.HandleErrorAsync(It.IsAny<MessageExecutionContext>()))
                .Returns<MessageExecutionContext>(
                    (messageExecutionContext) =>
                    {
                        actualMessageExecutionContext = messageExecutionContext;
                        return Task.FromException<DiscardEventException>(messageExecutionContext.Exception);
                    });

            _easyNetQSubscriber.Subscribe(subscriptions);

            //Act

            await eventEmulator.Execute(messagebytes, messageProperties, null);

            //check
            eventEmulator.RaisedException.GetType().Should().Be(typeof(DiscardEventException));

            _successHandlingStrategy.Verify(x => x.HandleSuccessAsync(It.IsAny<IntegrationEvent>()), Times.Never);
            _errorHandlingStrategy.Verify(x => x.HandleErrorAsync(It.IsAny<MessageExecutionContext>()), Times.Once);
            actualMessageExecutionContext.BodyBytes.ShouldBeEquivalentTo(messagebytes);
            actualMessageExecutionContext.Exception.Should().Be(eventEmulator.RaisedException);
            actualMessageExecutionContext.MessageProperties.Should().Be(messageProperties);
            actualMessageExecutionContext.IntergrationEventParsingResult.ShouldBeEquivalentTo(DefaultIntergrationEventParser.Parse(messagebytes));
            actualMessageExecutionContext.SerializedMessage.Should().Be(Encoding.UTF8.GetString(messagebytes));
            actualMessageExecutionContext.DeadLetterIntegrationEvent.Should().BeNull();
            actualMessageExecutionContext.Subscription.Should().Be(mockSubscription.Object);

        }

        #region Helpers

        public class EventEmulator
        {
            private Func<byte[], MessageProperties, MessageReceivedInfo, Task> _func;

            public Exception RaisedException { get; set; }
            public EventEmulator(Func<byte[], MessageProperties, MessageReceivedInfo, Task> func)
            {
                _func = func;
            }

            public async Task Execute(byte[] message, MessageProperties messageProperties,
                MessageReceivedInfo messageReceivedInfo )
            {
                try
                {
                    await _func(message, messageProperties, messageReceivedInfo);
                }
                catch (Exception e)
                {
                    RaisedException = e;
                }
              
            }
        }

        public class BookingCreated
        {
            public string BookingName { get; set; }
        }

        public class RelationshipCreated
        {
            public string Relationship { get; set; }
        }

        public class PersonCreated
        {
            public string FirstName { get; set; }
        }

        private static byte[] SerializeEvent(IntegrationEvent integrationEvent)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(integrationEvent));
        }

        #endregion
    }
}
