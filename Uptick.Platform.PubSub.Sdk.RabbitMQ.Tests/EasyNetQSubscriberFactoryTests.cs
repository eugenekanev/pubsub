using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using FluentAssertions;
using Moq;
using Uptick.Platform.PubSub.Sdk.Exceptions;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ.Tests
{
    public class EasyNetQSubscriberFactoryTests
    {
        private readonly Mock<IAdvancedBus> _mockBus;
        private readonly Mock<IBroker> _mockBroker;
        private readonly Mock<IExchange> _mockExchange;
        private readonly Mock<ISubscriberController> _queueController;
        private readonly Mock<IEnvironmentNamingConventionController> _environmentNamingConventionController;
        private readonly Mock<IPublisher> _publisher;
        private readonly int _retryfactor;
        private readonly Func<EasyNetQSubscriberFactory> _createEasyNetQSubscriberFactoryFunc;
        private string _defaultExchangename = "exchangename";
        private string _defaultWaitExchangename = "waitexchangename";

        public EasyNetQSubscriberFactoryTests()
        {
            _mockBus = new Mock<IAdvancedBus>();

            _mockBroker = new Mock<IBroker>();
            _mockBroker.Setup(x => x.Bus).Returns(_mockBus.Object);

            _mockExchange = new Mock<IExchange>();
            _queueController = new Mock<ISubscriberController>();
            _environmentNamingConventionController = new Mock<IEnvironmentNamingConventionController>();
            _publisher = new Mock<IPublisher>();
            _retryfactor = 3000;
            _createEasyNetQSubscriberFactoryFunc = () => new EasyNetQSubscriberFactory(_mockBroker.Object, _defaultExchangename, _defaultWaitExchangename,
                _queueController.Object, _environmentNamingConventionController.Object, _retryfactor, _publisher.Object);
        }

       
        [Fact]
        public void CreateSubscriberAsync_NameIsInvalid_Should_throw_exception()
        {
            //prepare
            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(_mockExchange.Object); });

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(_mockExchange.Object); });


            //act
            Func<Task> f = async () =>
            {
                await _createEasyNetQSubscriberFactoryFunc().CreateSubscriberAsync(null, new List<string>{"routingkey"}, 10);
            };

            //check
            f.ShouldThrow<ArgumentException>("name argument can not be null or empty");
        }

        [Fact]
        public void CreateSubscriberAsync_RoutingIsInvalid_Should_throw_exception()
        {
            //prepare
            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(_mockExchange.Object); });

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(_mockExchange.Object); });


            //act
            Func<Task> f = async () =>
            {
                await _createEasyNetQSubscriberFactoryFunc().CreateSubscriberAsync("justname", new List<string> { ""}, 10);
            };

            //check
            f.ShouldThrow<ArgumentException>("routings argument can not be null or empty");
        }


        [Fact]
        public void CreateSubscriberAsync_RoutingsHasNoItems_Should_throw_exception()
        {
            //prepare
            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                        bool passive, bool durable, bool autoDelete, bool internalflag,
                        string alternateExchange, bool delayed) =>
                    { return Task.FromResult(_mockExchange.Object); });

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                        bool passive, bool durable, bool autoDelete, bool internalflag,
                        string alternateExchange, bool delayed) =>
                    { return Task.FromResult(_mockExchange.Object); });


            //act
            Func<Task> f = async () =>
            {
                await _createEasyNetQSubscriberFactoryFunc().CreateSubscriberAsync("justname", new List<string>(), 10);
            };

            //check
            f.ShouldThrow<ArgumentException>("routings argument can not be empty or contain empty strings");
        }

        [Fact]
        public void CreateSubscriberAsync_RoutingsOneItemIsEmpty_Should_throw_exception()
        {
            //prepare
            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                        bool passive, bool durable, bool autoDelete, bool internalflag,
                        string alternateExchange, bool delayed) =>
                    { return Task.FromResult(_mockExchange.Object); });

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                        bool passive, bool durable, bool autoDelete, bool internalflag,
                        string alternateExchange, bool delayed) =>
                    { return Task.FromResult(_mockExchange.Object); });

            //act
            Func<Task> f = async () =>
            {
                await _createEasyNetQSubscriberFactoryFunc().
                    CreateSubscriberAsync("justname", new List<string> { "boocking.created", "" }, 10);
            };

            //check
            f.ShouldThrow<ArgumentException>("routings argument can not be empty or contain empty strings");
        }


        [Fact]
        public void CreateSubscriberAsync_Retrycount_less_zero_Should_throw_exception()
        {
            //prepare
            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(_mockExchange.Object); });

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(_mockExchange.Object); });


            //act
            Func<Task> f = async () =>
            {
                await _createEasyNetQSubscriberFactoryFunc().CreateSubscriberAsync("justname", new List<string> { "justkey"}, -10);
            };

            //check
            f.ShouldThrow<ArgumentException>("retrycount argument can not be less than zero");
        }

        [Fact]
        public void CreateSubscriberAsync_SubscriberAlreadyExists_Should_throw_exception()
        {
            //prepare

            var exchangeName = _defaultExchangename;
            var subscribername = "subscribername";
            var subscriberqueue = new Mock<IQueue>();
            var deadlettersubscriberqueue = new Mock<IQueue>();
            var routingKey = "routingKey";
            var retrycount = 10;

            _environmentNamingConventionController.Setup(x => x.GetQueueName(subscribername))
                .Returns((string queuename) => string.Concat(exchangeName, ".", queuename));
            _environmentNamingConventionController.Setup(x => x.GetDeadLetterQueueName(subscribername))
                .Returns((string queuename) => string.Concat(exchangeName, ".", "deadletter.", queuename));
            _environmentNamingConventionController.Setup(x => x.GetDeadLetterQueueRoutingKey(subscribername))
                .Returns((string queuename) => string.Concat("deadletter.", queuename));
            _environmentNamingConventionController.Setup(x => x.GetRetryQueueName(subscribername, It.IsAny<int>()))
                .Returns((string queuename, int retryindex) => string.Concat(exchangeName, ".", "retry.", retryindex, ".", queuename));
            _environmentNamingConventionController.Setup(x => x.GetRetryRoutingKey(subscribername, It.IsAny<int>()))
                .Returns((string queuename, int retryindex) => string.Concat("retry.", retryindex, ".", queuename));

            var mockWaitExchange = new Mock<IExchange>();
            _mockExchange.Setup(x => x.Name).Returns(exchangeName);

            IDisposable subscribernameHandle = null;
            _queueController.Setup(x => x.RegisterSubscriberName(subscribername, out subscribernameHandle)).Returns(false);

            _mockBus.Setup(x => x.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(_mockExchange.Object); });

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(mockWaitExchange.Object); });


            _mockBus.Setup(x => x.QueueDeclareAsync(string.Concat(exchangeName, ".", subscribername), false, true, false, false, null, null,
                null, null, null, null, null))
                .Returns((string name, bool passive, bool durable, bool exclusive,
                bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority, string deadLetterExchange,
                string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                { return Task.FromResult(subscriberqueue.Object); });

            Dictionary<int, Mock<IQueue>> retryqueues = new Dictionary<int, Mock<IQueue>>();

            for (int i = 1; i <= retrycount; i++)
            {
                int b = i;
                var currentRetryQueue = new Mock<IQueue>();
                retryqueues.Add(b, currentRetryQueue);
                _mockBus.Setup(x => x.QueueDeclareAsync(string.Concat(exchangeName, ".", "retry.", b, ".", subscribername), false,
                        true, false, false, _retryfactor * b, null, null, exchangeName, null, null, null))
                    .Returns((string name, bool passive, bool durable, bool exclusive,
                            bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                            string deadLetterExchange,
                            string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                        { return Task.FromResult(currentRetryQueue.Object); });
            }


            _mockBus.Setup(x => x.QueueDeclareAsync(String.Concat(exchangeName, ".", "deadletter.", subscribername), false, true,
                false, false, null, null, null, null, null, null, null))
                .Returns((string name, bool passive, bool durable, bool exclusive,
                        bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                        string deadLetterExchange,
                        string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                    { return Task.FromResult(deadlettersubscriberqueue.Object); });

           
            //act
            Func<Task> f = async () =>
            {
                await _createEasyNetQSubscriberFactoryFunc().CreateSubscriberAsync(subscribername, new List<string> { routingKey}, 10);
            };

            //check

            f.ShouldThrow<SubscriberAlreadyExistsException>($"The subscriber " +
                                                            $"with name {subscribername}" +
                                                            $" already exists");

            _mockBus.Verify(m => m.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic,
                false, true, false, false, null, false));

            _mockBus.Verify(m => m.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                false, true, false, false, null, false));

            _mockBus.Verify(x => x.QueueDeclareAsync(string.Concat(exchangeName, ".", subscribername), false, true, false, false, null, null,
                null, null, null, null, null));

            for (int i = 1; i <= retrycount; i++)
            {
                int b = i;
                _mockBus.Verify(x => x.QueueDeclareAsync(string.Concat(exchangeName, ".", "retry.", b, ".", subscribername), false,
                    true, false, false, _retryfactor * b, null, null, exchangeName, null, null, null));

                _mockBus.Verify(x => x.BindAsync(_mockExchange.Object, subscriberqueue.Object,
                    string.Concat("retry.", b, ".", subscribername)));
                _mockBus.Verify(x => x.BindAsync(mockWaitExchange.Object, retryqueues[b].Object,
                    string.Concat("retry.", b, ".", subscribername)));

                _environmentNamingConventionController.Verify(x => x.GetRetryQueueName(subscribername, b));
                _environmentNamingConventionController.Verify(x => x.GetRetryRoutingKey(subscribername, b));
            }


            _mockBus.Verify(x => x.QueueDeclareAsync(String.Concat(exchangeName, ".", "deadletter.", subscribername),
                false, true, false, false, null, null,
                null, null, null, null, null));

            _mockBus.Verify(x => x.BindAsync(_mockExchange.Object, subscriberqueue.Object, routingKey));
            _mockBus.Verify(x => x.BindAsync(mockWaitExchange.Object, deadlettersubscriberqueue.Object,
                string.Concat("deadletter.", subscribername)));

            _environmentNamingConventionController.Verify(x => x.GetQueueName(subscribername));
            _environmentNamingConventionController.Verify(x => x.GetDeadLetterQueueName(subscribername));
            _environmentNamingConventionController.Verify(x => x.GetDeadLetterQueueRoutingKey(subscribername));

            _queueController.Verify(x => x.RegisterSubscriberName(subscribername, out subscribernameHandle));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void CreateSubscriberAsync_CorrectParameters_SubscriberMustBeCreated(bool temporary)
        {
            //prepare
            var subscribername = "subscribername";
            var subscriberqueue = new Mock<IQueue>();
            var deadlettersubscriberqueue = new Mock<IQueue>();
            var routingKey1 = "routingKey1";
            var routingKey2 = "routingKey2";
            var retrycount = 10;

            _environmentNamingConventionController.Setup(x => x.GetQueueName(subscribername))
                .Returns((string queuename) => string.Concat(_defaultExchangename, ".", queuename));
            _environmentNamingConventionController.Setup(x => x.GetDeadLetterQueueName(subscribername))
                .Returns((string queuename) => string.Concat(_defaultExchangename, ".", "deadletter.", queuename));
            _environmentNamingConventionController.Setup(x => x.GetDeadLetterQueueRoutingKey(subscribername))
                .Returns((string queuename) => string.Concat("deadletter.", queuename));
            _environmentNamingConventionController.Setup(x => x.GetRetryQueueName(subscribername, It.IsAny<int>()))
                .Returns((string queuename, int retryindex) => string.Concat(_defaultExchangename, ".", "retry.", retryindex, ".", queuename));
            _environmentNamingConventionController.Setup(x => x.GetRetryRoutingKey(subscribername, It.IsAny<int>()))
                .Returns((string queuename, int retryindex) => string.Concat("retry.", retryindex, ".", queuename));

            var mockExchange = new Mock<IExchange>();
            var mockWaitExchange = new Mock<IExchange>();
            mockExchange.Setup(x => x.Name).Returns(_defaultExchangename);

            IDisposable subscribernameHandleStub;
            Mock<IDisposable> mocksubscribernameHandle = new Mock<IDisposable>();
            IDisposable actualSubscriberNameHandle = null;
            _queueController.Setup(x => x.RegisterSubscriberName(subscribername, out subscribernameHandleStub))
                .Callback(new RegisterSubscriberNameDelegate((string subscriber, out IDisposable nameHandle) =>
                {
                    nameHandle = mocksubscribernameHandle.Object;
                    actualSubscriberNameHandle = mocksubscribernameHandle.Object;
                })).Returns(true);

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(mockExchange.Object); });

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(mockWaitExchange.Object); });


            _mockBus.Setup(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", subscribername), false, true, temporary, false, null, null,
                    null, null, null, null, null))
                .Returns((string name, bool passive, bool durable, bool exclusive,
                        bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority, string deadLetterExchange,
                        string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                    { return Task.FromResult(subscriberqueue.Object); });

            Dictionary<int, Mock<IQueue>> retryqueues = new Dictionary<int, Mock<IQueue>>();

            for (int i = 1; i <= retrycount; i++)
            {
                int b = i;
                var currentRetryQueue = new Mock<IQueue>();
                retryqueues.Add(b, currentRetryQueue);
                _mockBus.Setup(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", "retry.", b, ".", subscribername), false,
                        true, temporary, false, _retryfactor * b, null, null, _defaultExchangename, null, null, null))
                    .Returns((string name, bool passive, bool durable, bool exclusive,
                            bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                            string deadLetterExchange,
                            string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                        { return Task.FromResult(currentRetryQueue.Object); });
            }

            _mockBus.Setup(x => x.QueueDeclareAsync(String.Concat(_defaultExchangename, ".", "deadletter.", subscribername), false, true,
                    temporary, false, null, null, null, null, null, null, null))
                .Returns((string name, bool passive, bool durable, bool exclusive,
                        bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                        string deadLetterExchange,
                        string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                    { return Task.FromResult(deadlettersubscriberqueue.Object); });


            //act

            ISubscriber subcriber =
                await _createEasyNetQSubscriberFactoryFunc()
                    .CreateSubscriberAsync(subscribername, temporary, new List<string> { routingKey1, routingKey2 }, 10, 1);


            //check

            _mockBus.Verify(m => m.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                false, true, false, false, null, false));

            _mockBus.Verify(m => m.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                false, true, false, false, null, false));

            _mockBus.Verify(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", subscribername), false, true, temporary, false, null, null,
                null, null, null, null, null));

            for (int i = 1; i <= retrycount; i++)
            {
                int b = i;
                _mockBus.Verify(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", "retry.", b, ".", subscribername), false,
                    true, temporary, false, _retryfactor * b, null, null,_defaultExchangename, null, null, null));

                _mockBus.Verify(x => x.BindAsync(mockExchange.Object, subscriberqueue.Object,
                    string.Concat("retry.", b, ".", subscribername)));
                _mockBus.Verify(x => x.BindAsync(mockWaitExchange.Object, retryqueues[b].Object,
                    string.Concat("retry.", b, ".", subscribername)));

                _environmentNamingConventionController.Verify(x => x.GetQueueName(subscribername));
                _environmentNamingConventionController.Verify(x => x.GetRetryQueueName(subscribername, b));
                _environmentNamingConventionController.Verify(x => x.GetRetryRoutingKey(subscribername, b));
            }

            _mockBus.Verify(x => x.QueueDeclareAsync(String.Concat(_defaultExchangename, ".", "deadletter.", subscribername),
                false, true, temporary, false, null, null,
                null, null, null, null, null));

            _mockBus.Verify(x => x.BindAsync(mockExchange.Object, subscriberqueue.Object, routingKey1));
            _mockBus.Verify(x => x.BindAsync(mockExchange.Object, subscriberqueue.Object, routingKey2));
            _mockBus.Verify(x => x.BindAsync(mockWaitExchange.Object, deadlettersubscriberqueue.Object,
                string.Concat("deadletter.", subscribername)));

            _environmentNamingConventionController.Verify(x => x.GetDeadLetterQueueName(subscribername));
            _environmentNamingConventionController.Verify(x => x.GetDeadLetterQueueRoutingKey(subscribername));


            actualSubscriberNameHandle.Should().Be(mocksubscribernameHandle.Object);

            subcriber.Should().NotBeNull();
        }

        [Fact]
        public async void CreateSubscriberAsyncWithDisconnectCallback_CorrectParameters_SubscriberMustBeCreated()
        {
            //prepare
            var subscribername = "subscribername";
            var subscriberqueue = new Mock<IQueue>();
            var deadlettersubscriberqueue = new Mock<IQueue>();
            var routingKey1 = "routingKey1";
            var routingKey2 = "routingKey2";
            var retrycount = 10;
            var temporary = true;
            Action disconnectCallback = () => { };

            _environmentNamingConventionController.Setup(x => x.GetQueueName(subscribername))
                .Returns((string queuename) => string.Concat(_defaultExchangename, ".", queuename));
            _environmentNamingConventionController.Setup(x => x.GetDeadLetterQueueName(subscribername))
                .Returns((string queuename) => string.Concat(_defaultExchangename, ".", "deadletter.", queuename));
            _environmentNamingConventionController.Setup(x => x.GetDeadLetterQueueRoutingKey(subscribername))
                .Returns((string queuename) => string.Concat("deadletter.", queuename));
            _environmentNamingConventionController.Setup(x => x.GetRetryQueueName(subscribername, It.IsAny<int>()))
                .Returns((string queuename, int retryindex) => string.Concat(_defaultExchangename, ".", "retry.", retryindex, ".", queuename));
            _environmentNamingConventionController.Setup(x => x.GetRetryRoutingKey(subscribername, It.IsAny<int>()))
                .Returns((string queuename, int retryindex) => string.Concat("retry.", retryindex, ".", queuename));

            var mockExchange = new Mock<IExchange>();
            var mockWaitExchange = new Mock<IExchange>();
            mockExchange.Setup(x => x.Name).Returns(_defaultExchangename);

            IDisposable subscribernameHandleStub;
            Mock<IDisposable> mocksubscribernameHandle = new Mock<IDisposable>();
            IDisposable actualSubscriberNameHandle = null;
            _queueController.Setup(x => x.RegisterSubscriberName(subscribername, out subscribernameHandleStub))
                .Callback(new RegisterSubscriberNameDelegate((string subscriber, out IDisposable nameHandle) =>
                {
                    nameHandle = mocksubscribernameHandle.Object;
                    actualSubscriberNameHandle = mocksubscribernameHandle.Object;
                })).Returns(true); ;

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                    false, temporary, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(mockExchange.Object); });

            _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                    false, temporary, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                    bool passive, bool durable, bool autoDelete, bool internalflag,
                    string alternateExchange, bool delayed) =>
                { return Task.FromResult(mockWaitExchange.Object); });


            _mockBus.Setup(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", subscribername), false, true, temporary, false, null, null,
                    null, null, null, null, null))
                .Returns((string name, bool passive, bool durable, bool exclusive,
                        bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority, string deadLetterExchange,
                        string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                { return Task.FromResult(subscriberqueue.Object); });

            Dictionary<int, Mock<IQueue>> retryqueues = new Dictionary<int, Mock<IQueue>>();

            for (int i = 1; i <= retrycount; i++)
            {
                int b = i;
                var currentRetryQueue = new Mock<IQueue>();
                retryqueues.Add(b, currentRetryQueue);
                _mockBus.Setup(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", "retry.", b, ".", subscribername), false,
                        true, temporary, false, _retryfactor * b, null, null, _defaultExchangename, null, null, null))
                    .Returns((string name, bool passive, bool durable, bool exclusive,
                            bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                            string deadLetterExchange,
                            string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                    { return Task.FromResult(currentRetryQueue.Object); });
            }

            _mockBus.Setup(x => x.QueueDeclareAsync(String.Concat(_defaultExchangename, ".", "deadletter.", subscribername), false, true,
                    temporary, false, null, null, null, null, null, null, null))
                .Returns((string name, bool passive, bool durable, bool exclusive,
                        bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                        string deadLetterExchange,
                        string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                { return Task.FromResult(deadlettersubscriberqueue.Object); });


            //act

            ISubscriber subcriber =
                await _createEasyNetQSubscriberFactoryFunc()
                    .CreateSubscriberAsync(subscribername, disconnectCallback, new List<string> { routingKey1, routingKey2 }, 10, 1);


            //check

            _mockBus.Verify(m => m.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                false, true, false, false, null, false));

            _mockBus.Verify(m => m.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                false, true, false, false, null, false));

            _mockBus.Verify(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", subscribername), false, true, temporary, false, null, null,
                null, null, null, null, null));

            for (int i = 1; i <= retrycount; i++)
            {
                int b = i;
                _mockBus.Verify(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", "retry.", b, ".", subscribername), false,
                    true, temporary, false, _retryfactor * b, null, null, _defaultExchangename, null, null, null));

                _mockBus.Verify(x => x.BindAsync(mockExchange.Object, subscriberqueue.Object,
                    string.Concat("retry.", b, ".", subscribername)));
                _mockBus.Verify(x => x.BindAsync(mockWaitExchange.Object, retryqueues[b].Object,
                    string.Concat("retry.", b, ".", subscribername)));

                _environmentNamingConventionController.Verify(x => x.GetQueueName(subscribername));
                _environmentNamingConventionController.Verify(x => x.GetRetryQueueName(subscribername, b));
                _environmentNamingConventionController.Verify(x => x.GetRetryRoutingKey(subscribername, b));
            }

            _mockBus.Verify(x => x.QueueDeclareAsync(String.Concat(_defaultExchangename, ".", "deadletter.", subscribername),
                false, true, temporary, false, null, null,
                null, null, null, null, null));

            _mockBus.Verify(x => x.BindAsync(mockExchange.Object, subscriberqueue.Object, routingKey1));
            _mockBus.Verify(x => x.BindAsync(mockExchange.Object, subscriberqueue.Object, routingKey2));
            _mockBus.Verify(x => x.BindAsync(mockWaitExchange.Object, deadlettersubscriberqueue.Object,
                string.Concat("deadletter.", subscribername)));

            // check delegate registration
            _mockBroker.Verify(m => m.RegisterDisconnectedAction(disconnectCallback));

            _environmentNamingConventionController.Verify(x => x.GetDeadLetterQueueName(subscribername));
            _environmentNamingConventionController.Verify(x => x.GetDeadLetterQueueRoutingKey(subscribername));

            actualSubscriberNameHandle.Should().Be(mocksubscribernameHandle.Object);

            subcriber.Should().NotBeNull();
        }

        [Fact]
        public async void Create3SubscriberWith2defferentPrefetchCountAsync_CorrectParameters_1ConnectionToRabbitMustBeCreated()
        {
            //prepare
            var subscriber1PrefetchCount10Name = "subscriber1PrefetchCount10Name";
            var subscriber1PrefetchCount10queue = new Mock<IQueue>();
            var deadlettersubscriber1PrefetchCount10queue = new Mock<IQueue>();
            var subscriber1PrefetchCount10routingKey1 = "subscriber1PrefetchCount10routingKey1";
            var subscriber1PrefetchCount10Retrycount = 10;

            var subscriber2PrefetchCount10Name = "subscriber2PrefetchCount10Name";
            var subscriber2PrefetchCount10queue = new Mock<IQueue>();
            var deadlettersubscriber2PrefetchCount10queue = new Mock<IQueue>();
            var subscriber2PrefetchCount10routingKey1 = "subscriber2PrefetchCount10routingKey1";
            var subscriber2PrefetchCount10Retrycount = 5;

            var subscriber3PrefetchCount1Name = "subscriber1PrefetchCount1Name";
            var subscriber3PrefetchCount1queue = new Mock<IQueue>();
            var deadlettersubscriber3PrefetchCount1queue = new Mock<IQueue>();
            var subscriber3PrefetchCount1routingKey1 = "subscriber3PrefetchCount1routingKey1";
            var subscriber3PrefetchCount1Retrycount = 8;

            IEnumerable<string> subscribersQueueNames = new List<string>
            {
                subscriber1PrefetchCount10Name , subscriber2PrefetchCount10Name 
                ,subscriber3PrefetchCount1Name
            };

            foreach (var subscribername in subscribersQueueNames)
            {
                _environmentNamingConventionController.Setup(x => x.GetQueueName(subscribername))
                    .Returns((string queuename) => string.Concat(_defaultExchangename, ".", queuename));
                _environmentNamingConventionController.Setup(x => x.GetDeadLetterQueueName(subscribername))
                    .Returns((string queuename) => string.Concat(_defaultExchangename, ".", "deadletter.", queuename));
                _environmentNamingConventionController.Setup(x => x.GetDeadLetterQueueRoutingKey(subscribername))
                    .Returns((string queuename) => string.Concat("deadletter.", queuename));
                _environmentNamingConventionController.Setup(x => x.GetRetryQueueName(subscribername, It.IsAny<int>()))
                    .Returns((string queuename, int retryindex) => string.Concat(_defaultExchangename, ".", "retry.", retryindex, ".", queuename));
                _environmentNamingConventionController.Setup(x => x.GetRetryRoutingKey(subscribername, It.IsAny<int>()))
                    .Returns((string queuename, int retryindex) => string.Concat("retry.", retryindex, ".", queuename));
            }

            var mockExchange = new Mock<IExchange>();
            var mockWaitExchange = new Mock<IExchange>();
            mockExchange.Setup(x => x.Name).Returns(_defaultExchangename);

            Dictionary<string, IDisposable> nameHandles = new Dictionary<string, IDisposable>();
            Dictionary<string, IDisposable> actualNameHandles = new Dictionary<string, IDisposable>();
            foreach (var subscribername in subscribersQueueNames)
            {

                IDisposable subscribernameHandleStub;
                Mock<IDisposable> mocksubscribernameHandle = new Mock<IDisposable>();
                _queueController.Setup(x => x.RegisterSubscriberName(subscribername, out subscribernameHandleStub))
                    .Callback(new RegisterSubscriberNameDelegate((string subscriber, out IDisposable nameHandle) =>
                    {
                        nameHandle = mocksubscribernameHandle.Object;
                        actualNameHandles.Add(subscriber, mocksubscribernameHandle.Object);
                    })).Returns(true);

                nameHandles.Add(subscribername, mocksubscribernameHandle.Object);
            }

            
             _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                        false, true, false, false, null, false))
                    .Returns((string exchangename, string exchanegType,
                            bool passive, bool durable, bool autoDelete, bool internalflag,
                            string alternateExchange, bool delayed) =>
                        { return Task.FromResult(mockExchange.Object); });

             _mockBus.Setup(x => x.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                        false, true, false, false, null, false))
                    .Returns((string exchangename, string exchanegType,
                            bool passive, bool durable, bool autoDelete, bool internalflag,
                            string alternateExchange, bool delayed) =>
                        { return Task.FromResult(mockWaitExchange.Object); });
            

            var subscribenamesInfo = new List<SubscriberContext>
            {
                new SubscriberContext{SubscriberName = subscriber1PrefetchCount10Name,
                    RetryQueues = new Dictionary<int, Mock<IQueue>>(), RetryCount = subscriber1PrefetchCount10Retrycount,
                    DeadLetterQueue = deadlettersubscriber1PrefetchCount10queue, ExpectedAdvancedBus = _mockBus,
                    MainQueue = subscriber1PrefetchCount10queue,RoutingKey = subscriber1PrefetchCount10routingKey1},
                new SubscriberContext{SubscriberName = subscriber2PrefetchCount10Name,
                    RetryQueues = new Dictionary<int, Mock<IQueue>>(), RetryCount = subscriber2PrefetchCount10Retrycount,
                    DeadLetterQueue = deadlettersubscriber2PrefetchCount10queue, ExpectedAdvancedBus = _mockBus,
                    MainQueue = subscriber2PrefetchCount10queue,RoutingKey = subscriber2PrefetchCount10routingKey1},
                new SubscriberContext{SubscriberName = subscriber3PrefetchCount1Name,
                    RetryQueues = new Dictionary<int, Mock<IQueue>>(), RetryCount = subscriber3PrefetchCount1Retrycount,
                    DeadLetterQueue = deadlettersubscriber3PrefetchCount1queue, ExpectedAdvancedBus = _mockBus,
                    MainQueue = subscriber3PrefetchCount1queue,RoutingKey = subscriber3PrefetchCount1routingKey1},
            };

            foreach (var subscribenameInfo in subscribenamesInfo)
            {
                subscribenameInfo.ExpectedAdvancedBus.Setup(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", subscribenameInfo.SubscriberName), false, true, false, false, null, null,
                        null, null, null, null, null))
                    .Returns((string name, bool passive, bool durable, bool exclusive,
                            bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority, string deadLetterExchange,
                            string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                        { return Task.FromResult(subscribenameInfo.MainQueue.Object); });


                for (int i = 1; i <= subscribenameInfo.RetryCount; i++)
                {
                    int b = i;
                    var currentRetryQueue = new Mock<IQueue>();
                    subscribenameInfo.RetryQueues.Add(b, currentRetryQueue);
                    subscribenameInfo.ExpectedAdvancedBus.Setup(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", "retry.", b, ".", subscribenameInfo.SubscriberName), false,
                            true, false, false, _retryfactor * b, null, null, _defaultExchangename, null, null, null))
                        .Returns((string name, bool passive, bool durable, bool exclusive,
                                bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                                string deadLetterExchange,
                                string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                            { return Task.FromResult(currentRetryQueue.Object); });
                }

                subscribenameInfo.ExpectedAdvancedBus.Setup(x => x.QueueDeclareAsync(String.Concat(_defaultExchangename, ".", "deadletter.", subscribenameInfo.SubscriberName), false, true,
                        false, false, null, null, null, null, null, null, null))
                    .Returns((string name, bool passive, bool durable, bool exclusive,
                            bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                            string deadLetterExchange,
                            string deadLetterRoutingKey, int maxLength, int maxLengthBytes) =>
                        { return Task.FromResult(subscribenameInfo.DeadLetterQueue.Object); });
            }


            //act

            ISubscriber subscriber1 =
                await _createEasyNetQSubscriberFactoryFunc()
                    .CreateSubscriberAsync(subscriber1PrefetchCount10Name,
                        new List<string> { subscriber1PrefetchCount10routingKey1}, subscriber1PrefetchCount10Retrycount, 10);

            ISubscriber subscriber2 =
                await _createEasyNetQSubscriberFactoryFunc()
                    .CreateSubscriberAsync(subscriber2PrefetchCount10Name,
                        new List<string> { subscriber2PrefetchCount10routingKey1 }, subscriber2PrefetchCount10Retrycount, 10);

            ISubscriber subscriber3 =
                await _createEasyNetQSubscriberFactoryFunc()
                    .CreateSubscriberAsync(subscriber3PrefetchCount1Name,
                        new List<string> { subscriber3PrefetchCount1routingKey1 }, subscriber3PrefetchCount1Retrycount, 1);


            //check

            foreach (var subscribenameInfo in subscribenamesInfo)
            {
                subscribenameInfo.ExpectedAdvancedBus.Verify(m => m.ExchangeDeclareAsync(_defaultExchangename, ExchangeType.Topic,
                    false, true, false, false, null, false));

                subscribenameInfo.ExpectedAdvancedBus.Verify(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", subscribenameInfo.SubscriberName), false, true, false, false, null, null,
                    null, null, null, null, null));

                for (int i = 1; i <= subscribenameInfo.RetryCount; i++)
                {
                    int b = i;
                    subscribenameInfo.ExpectedAdvancedBus.Verify(x => x.QueueDeclareAsync(string.Concat(_defaultExchangename, ".", "retry.", b, ".", subscribenameInfo.SubscriberName), false,
                        true, false, false, _retryfactor * b, null, null, _defaultExchangename, null, null, null));

                    subscribenameInfo.ExpectedAdvancedBus.Verify(x => x.BindAsync(mockExchange.Object, subscribenameInfo.MainQueue.Object,
                        string.Concat("retry.", b, ".", subscribenameInfo.SubscriberName)));
                    subscribenameInfo.ExpectedAdvancedBus.Verify(x => x.BindAsync(mockWaitExchange.Object, subscribenameInfo.RetryQueues[b].Object,
                        string.Concat("retry.", b, ".", subscribenameInfo.SubscriberName)));

                    _environmentNamingConventionController.Verify(x => x.GetRetryQueueName(subscribenameInfo.SubscriberName, b));
                    _environmentNamingConventionController.Verify(x => x.GetRetryRoutingKey(subscribenameInfo.SubscriberName, b));
                }

                subscribenameInfo.ExpectedAdvancedBus.Verify(x => x.QueueDeclareAsync(String.Concat(_defaultExchangename, ".", "deadletter.", subscribenameInfo.SubscriberName),
                    false, true, false, false, null, null,
                    null, null, null, null, null));

                subscribenameInfo.ExpectedAdvancedBus.Verify(x => x.BindAsync(mockExchange.Object, subscribenameInfo.MainQueue.Object, subscribenameInfo.RoutingKey));

                subscribenameInfo.ExpectedAdvancedBus.Verify(x => x.BindAsync(mockWaitExchange.Object, subscribenameInfo.DeadLetterQueue.Object,
                    string.Concat("deadletter.", subscribenameInfo.SubscriberName)));

                _environmentNamingConventionController.Verify(x => x.GetQueueName(subscribenameInfo.SubscriberName));
                _environmentNamingConventionController.Verify(x => x.GetDeadLetterQueueName(subscribenameInfo.SubscriberName));
                _environmentNamingConventionController.Verify(x => x.GetDeadLetterQueueRoutingKey(subscribenameInfo.SubscriberName));

                actualNameHandles[subscribenameInfo.SubscriberName].ShouldBeEquivalentTo(nameHandles[subscribenameInfo.SubscriberName]);


            }

            _mockBus.Verify(m => m.ExchangeDeclareAsync(_defaultWaitExchangename, ExchangeType.Topic,
                false, true, false, false, null, false));

            subscriber1.Should().NotBeNull();

            subscriber2.Should().NotBeNull();

            subscriber3.Should().NotBeNull();

        }


        #region Helpers
        public class BookingCreated
        {
            public string BookingName { get; set; }
        }

        public class SubscriberContext
        {
            public string SubscriberName { get; set; }
            public Dictionary<int, Mock<IQueue>> RetryQueues { get; set; }

            public int RetryCount { get; set; }

            public Mock<IQueue> DeadLetterQueue { get; set; }

            public Mock<IAdvancedBus> ExpectedAdvancedBus { get; set; }

            public Mock<IQueue> MainQueue { get; set; }
            public string RoutingKey { get; set; }
        }
        #endregion
    }

    #region helpers
    public delegate void RegisterSubscriberNameDelegate(string subscriberName, out IDisposable nameHandle);
    #endregion
}
