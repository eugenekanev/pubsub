using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.Management.Client;
using EasyNetQ.Topology;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Autofac;
using Uptick.Platform.PubSub.Sdk.Exceptions;
using Uptick.Platform.PubSub.Sdk.Extenstions.Aggregator;
using Uptick.Platform.PubSub.Sdk.RabbitMQ;
using Xunit;
using ExchangeType = RabbitMQ.Client.ExchangeType;
using IContainer = Autofac.IContainer;

namespace Uptick.Platform.PubSub.Sdk.ComponentTests
{
    public class PublisherSubscriberPatternTests
    {
        [Fact]
        public async Task Publisher_Publish_MessageMustBeSerializedAndCorrectlyRouted()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName = "Publisher_Publish_MessageMustBeSerializedAndCorrectlyRouted.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            var publisher = container.Resolve<IPublisher>();

            string routingKey = "changetracker.entity.create.booking";

            var bus = container.Resolve<IAdvancedBus>();
            IExchange exchange = bus.ExchangeDeclare(exchangeName, ExchangeType.Topic);

            var queue = bus.QueueDeclare("Publisher_Publish_MessageMustBeSentAndCorrectlyRouted.subscriberqueue");
            var binding = bus.Bind(exchange, queue, "changetracker.entity.create.booking");

            //act
            BookingCreated bookingCreated =
              new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };

            await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, routingKey);

            await Task.Delay(300);

            //check

            IBasicGetResult result = bus.Get(queue);

            var message = Encoding.UTF8.GetString(result.Body);
            IntegrationEvent<BookingCreated> eventToCheck = JsonConvert.DeserializeObject<IntegrationEvent<BookingCreated>>(message);

            eventToCheck.ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);
            container.Dispose();

        }

        [Fact]
        public async Task Publisher_PublishDirectEventAsync_MessageMustBeSerializedAndCorrectlyRoutedToSpecificSubscriber()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Publisher_PublishDirectEventAsync_MessageMustBeSerializedAndCorrectlyRoutedToSpecificSubscriber.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");


            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            string subscriberName = "Publisher_PublishDirectEventAsync_MessageMustBeSerializedAndCorrectlyRoutedToSpecificSubscriber.typedbookingcreatedconsumer";
            ISubscriber typedSubscriber =
                await bookingCreatedSubscriberFactory
                    .CreateSubscriberAsync(subscriberName, (List<string>)null, maxretrycount);
            BookingTypedConsumer typedConsumer = new BookingTypedConsumer(0);
            typedSubscriber.Subscribe(SubscriptionBuilder.Create().AddDefaultSubscription(() => typedConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };
            await publisher.PublishDirectEventAsync(bookingCreatedIntegrationEvent, subscriberName);

            await Task.Delay(200);

            //check

            typedConsumer.ProcessedIntegrationEvent.ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);
            typedConsumer.CountOfAttempts.Should().Be(1);
            container.Dispose();
        }

        [Fact]
        public async Task Subscriber_ProcessTheMessageTheFirstTime_MessageMustBeRemovedFromTheRabbitMq()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscriber_ProcessTheMessageTheFirstTime_MessageMustBeRemovedFromTheRabbitMq.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");


            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber typedSubscriber =
                await bookingCreatedSubscriberFactory
                .CreateSubscriberAsync("Subscriber_ProcessTheMessageTheFirstTime_MessageMustBeRemovedFromTheRabbitMq" +
                                       ".typedbookingcreatedconsumer", new List<string> { "*.entity.create.booking" }, maxretrycount);
            BookingTypedConsumer typedConsumer = new BookingTypedConsumer(0);
            typedSubscriber.Subscribe(SubscriptionBuilder.Create().AddDefaultSubscription(() => typedConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string routingKey = "changetracker.entity.create.booking";

            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };
            await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, routingKey);

            await Task.Delay(200);

            //check

            typedConsumer.ProcessedIntegrationEvent.ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);
            typedConsumer.CountOfAttempts.Should().Be(1);
            container.Dispose();

        }

        [Fact]
        public async Task Subscriber_ProcessTheMessageTheSecondTime_MessageMustBeRemovedFromTheRabbitMq()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscriber_ProcessTheMessageTheSecondTime_MessageMustBeRemovedFromTheRabbitMq.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");
            configuration["rabbitmq:retryfactor"] = 100.ToString();


            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber typedSubscriber =
                await bookingCreatedSubscriberFactory
                .CreateSubscriberAsync("Subscriber_ProcessTheMessageTheSecondTime_MessageMustBeRemovedFromTheRabbitMq" +
                                       ".typedbookingcreatedconsumer", new List<string> { "*.entity.create.booking" },
                    maxretrycount);
            BookingTypedConsumer typedConsumer = new BookingTypedConsumer(1);
            typedSubscriber.Subscribe(SubscriptionBuilder.Create().AddDefaultSubscription(() => typedConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string routingKey = "changetracker.entity.create.booking";

            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };
            await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, routingKey);

            await Task.Delay(500);

            //check

            typedConsumer.ProcessedIntegrationEvent.ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);
            typedConsumer.CountOfAttempts.Should().Be(2);
            container.Dispose();

        }

        [Fact]
        public async Task Subscriber_ExceededCountOfAttempts_MessageMustBeMovedToSpecificDeadLetterQueue()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscriber_ExceededCountOfAttempts_MessageMustBeMovedToDeadLetterQueue.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");
            configuration["rabbitmq:retryfactor"] = 100.ToString();

            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber typedSubscriber =
                await bookingCreatedSubscriberFactory
                .CreateSubscriberAsync("Subscriber_ExceededCountOfAttempts_MessageMustBeMovedToDeadLetterQueue" +
                                       ".typedbookingcreatedconsumer", new List<string> { "*.entity.create.booking" },
                    maxretrycount);
            BookingTypedConsumer typedConsumer = new BookingTypedConsumer(3);

            IntegrationEvent<BookingCreated> actualDeadLetterCallbackIntegrationEvent = null;
            Exception actualDeadLetterCallbackException = null;
            typedSubscriber.Subscribe(SubscriptionBuilder.Create().AddDefaultSubscription(() => typedConsumer, (integrationEvent, exception) =>
            {
                actualDeadLetterCallbackIntegrationEvent = integrationEvent;
                actualDeadLetterCallbackException = exception;
                return Task.CompletedTask;
            }).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string routingKey = "changetracker.entity.create.booking";

            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated",
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, routingKey);

            //wait 1 second
            await Task.Delay(1000);

            //check
            var bus = container.Resolve<IAdvancedBus>();
            var deadleterqueue = bus.QueueDeclare($"{exchangeName}.deadletter" +
             ".Subscriber_ExceededCountOfAttempts_MessageMustBeMovedToDeadLetterQueue." +
                                                  "typedbookingcreatedconsumer");
            IBasicGetResult result = bus.Get(deadleterqueue);

            var message = Encoding.UTF8.GetString(result.Body);
            var actualDeadLetterQueueIntergrationEvent = JsonConvert.
                DeserializeObject<IntegrationEvent<DeadLetterEventDescriptor<BookingCreated>>>(message);

            result.Info.RoutingKey.Should().Be("deadletter" +
            ".Subscriber_ExceededCountOfAttempts_MessageMustBeMovedToDeadLetterQueue" +
            ".typedbookingcreatedconsumer");

            actualDeadLetterQueueIntergrationEvent.Content.Original
                .ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);
            actualDeadLetterQueueIntergrationEvent.CorrelationId
                .ShouldBeEquivalentTo(bookingCreatedIntegrationEvent.CorrelationId);
            actualDeadLetterQueueIntergrationEvent.EventType.Should()
                .BeEquivalentTo(string.Concat("deadletter.", bookingCreatedIntegrationEvent.EventType));

            actualDeadLetterCallbackException.Message.Should().Be("ExceptionHasBeenThrown");
            actualDeadLetterCallbackException.Should().BeOfType<Exception>();
            actualDeadLetterCallbackIntegrationEvent.ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);


            typedConsumer.ProcessedIntegrationEvent.Should().BeNull();
            typedConsumer.CountOfAttempts.Should().Be(3);
            container.Dispose();

        }

        [Fact]
        public async Task Subscriber_DiscardException_MessageMustBeMovedToGlobalDeadLetterQueue()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscriber_DiscardException_MessageMustBeMovedToGlobalDeadLetterQueue.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber typedSubscriber =
                await bookingCreatedSubscriberFactory
                    .CreateSubscriberAsync("Subscriber_DiscardException_MessageMustBeMovedToGlobalDeadLetterQueue" +
                                           ".typedbookingcreatedconsumer", new List<string> { "*.entity.create.booking" },
                        maxretrycount);
            DiscardConsumer discardConsumer = new DiscardConsumer();
            typedSubscriber.Subscribe(SubscriptionBuilder.Create().AddDefaultSubscription(() => discardConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string routingKey = "changetracker.entity.create.booking";

            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated",
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, routingKey);

            await Task.Delay(3000);

            //check
            var bus = container.Resolve<IAdvancedBus>();
            var deadleterqueue = bus.QueueDeclare($"{exchangeName}.defaultTombQueue");
            IBasicGetResult result = bus.Get(deadleterqueue);

            var message = Encoding.UTF8.GetString(result.Body);
            JObject errorEvent = JObject.Parse(message);
            errorEvent["RoutingKey"].Value<string>().Should().Be("changetracker.entity.create.booking");
            errorEvent["Exchange"]
                .Value<string>().Should().Be("Subscriber_DiscardException_MessageMustBeMovedToGlobalDeadLetterQueue.exchangename");
            errorEvent["Queue"]
                .Value<string>().Should().Be($"{exchangeName}.Subscriber_DiscardException_MessageMustBeMovedToGlobalDeadLetterQueue.typedbookingcreatedconsumer");

            //check bindings 
            var managementClient = new ManagementClient(configuration["rabbitmqmanagement:hostUrl"], configuration["rabbitmqmanagement:username"], configuration["rabbitmqmanagement:password"], configuration.GetValue<int>("rabbitmqmanagement:portNumber"));
            var virtualHostName = new ConnectionStringParser().Parse(configuration["rabbitmq:connectionstring"]).VirtualHost;
            var virtualhost = await managementClient.GetVhostAsync(virtualHostName);
            var deadleterq = await managementClient.GetQueueAsync(deadleterqueue.Name, virtualhost);
            var deadleterqbindings = (await managementClient.GetBindingsForQueueAsync(deadleterq)).ToList();
            deadleterqbindings.Should().HaveCount(2);//one is default
            deadleterqbindings.Where(x => x.Source == $"{exchangeName}_error" && x.RoutingKey == routingKey).Should().HaveCount(1);

            container.Dispose();
        }

        [Theory]
        [InlineData("not_valid_json", "X%@")]
        [InlineData("EventId_is_missing", "{\"Content\":{},\"CorrelationId\":\"efb250d0-3c7d-4603-9775-b6d8c5518f6e\",\"EventCreationDate\":\"2018-09-13T12:16:36.8929765Z\",\"EventType\":\"bookingcreated\",\"Version\":\"0.1\"}")]
        [InlineData("CorrelationId_is_missing", "{\"Content\":{},\"EventId\":\"e29d5af9-8d27-4385-85af-360e2239a74e\",\"EventCreationDate\":\"2018-09-13T12:16:36.8929765Z\",\"EventType\":\"bookingcreated\",\"Version\":\"0.1\"}")]
        [InlineData("EventCreationDate_is_missing", "{\"Content\":{},\"EventId\":\"e29d5af9-8d27-4385-85af-360e2239a74e\",\"CorrelationId\":\"efb250d0-3c7d-4603-9775-b6d8c5518f6e\",\"EventType\":\"bookingcreated\",\"Version\":\"0.1\"}")]
        [InlineData("EventType_is_missing", "{\"Content\":{},\"EventId\":\"e29d5af9-8d27-4385-85af-360e2239a74e\",\"CorrelationId\":\"efb250d0-3c7d-4603-9775-b6d8c5518f6e\",\"EventCreationDate\":\"2018-09-13T12:16:36.8929765Z\",\"Version\":\"0.1\"}")]
        [InlineData("Version_is_missing", "{\"Content\":{},\"EventId\":\"e29d5af9-8d27-4385-85af-360e2239a74e\",\"CorrelationId\":\"efb250d0-3c7d-4603-9775-b6d8c5518f6e\",\"EventCreationDate\":\"2018-09-13T12:16:36.8929765Z\",\"EventType\":\"bookingcreated\"}")]
        public async Task Subscriber_MalformedMessage_MessageMustBeMovedToGlobalDeadLetterQueue(string caseName, string messageBody)
        {
            var testName = $"{nameof(Subscriber_MalformedMessage_MessageMustBeMovedToGlobalDeadLetterQueue)}_{caseName}";
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                $"{testName}.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber typedSubscriber =
                await bookingCreatedSubscriberFactory
                    .CreateSubscriberAsync(testName +
                                           ".stubconsumer", new List<string> { "*.entity.create.booking" },
                        maxretrycount);
            var consumer = new StubConsumer();
            typedSubscriber.Subscribe(SubscriptionBuilder.Create().AddDefaultSubscription(() => consumer).Build());

            //create Bus
            var bus = container.Resolve<IAdvancedBus>();
            string routingKey = "changetracker.entity.create.booking";

            //act
            var body = Encoding.UTF8.GetBytes(messageBody);
            var properties = new MessageProperties
            {
                DeliveryMode = 2//persistent
            };
            await bus.PublishAsync(await bus.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic), routingKey, false, properties, body);

            await Task.Delay(3000);

            //check
            var deadleterqueue = bus.QueueDeclare($"{exchangeName}.defaultTombQueue");
            IBasicGetResult result = bus.Get(deadleterqueue);

            var message = Encoding.UTF8.GetString(result.Body);
            JObject errorEvent = JObject.Parse(message);
            errorEvent["RoutingKey"].Value<string>().Should().Be("changetracker.entity.create.booking");
            errorEvent["Exchange"]
                .Value<string>().Should().Be(exchangeName);
            errorEvent["Queue"]
                .Value<string>().Should().Be($"{exchangeName}.{testName}.stubconsumer");

            //check bindings 
            var managementClient = new ManagementClient(configuration["rabbitmqmanagement:hostUrl"], configuration["rabbitmqmanagement:username"], configuration["rabbitmqmanagement:password"], configuration.GetValue<int>("rabbitmqmanagement:portNumber"));
            var virtualHostName = new ConnectionStringParser().Parse(configuration["rabbitmq:connectionstring"]).VirtualHost;
            var virtualhost = await managementClient.GetVhostAsync(virtualHostName);
            var deadleterq = await managementClient.GetQueueAsync(deadleterqueue.Name, virtualhost);
            var deadleterqbindings = (await managementClient.GetBindingsForQueueAsync(deadleterq)).ToList();
            deadleterqbindings.Should().HaveCount(2);//one is default
            deadleterqbindings.Where(x => x.Source == $"{exchangeName}_error" && x.RoutingKey == routingKey).Should().HaveCount(1);

            container.Dispose();
        }

        [Fact]
        public async Task Subscriber_ProcessThe2MessageTheFirstTimeWith2DifferentRoutingKeys_MessagesMustBeRemovedFromTheRabbitMq()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscriber_ProcessTheMessageTheFirstTime_MessageMustBeRemovedFromTheRabbitMq.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");


            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber typedSubscriber =
                await bookingCreatedSubscriberFactory
                .CreateSubscriberAsync("Subscriber_ProcessTheMessageTheFirstTime_MessageMustBeRemovedFromTheRabbitMq" +
                                       ".typedbookingcreatedconsumer", new List<string>
                    {
                        "*.entity.create.booking",
                        "*.entity.create.account"
                    }, maxretrycount);
            BookingTypedConsumer bookingTypedConsumer = new BookingTypedConsumer(0);
            AccountTypedConsumer accountTypedConsumer = new AccountTypedConsumer(0);
            typedSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingcreated", () => bookingTypedConsumer)
                .AddSubscription("accountcreated", () => accountTypedConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string bookingRoutingKey = "changetracker.entity.create.booking";

            string accountRoutingKey = "changetracker.entity.create.account";

            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };

            AccountCreated accountCreated =
                new AccountCreated() { AccountLevel = 100 };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };

            var accountCreatedIntegrationEvent = new IntegrationEvent<AccountCreated>()
            {
                Content = accountCreated,
                EventType = "accountcreated"
            };

            await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, bookingRoutingKey);

            await publisher.PublishEventAsync(accountCreatedIntegrationEvent, accountRoutingKey);

            await Task.Delay(200);

            //check

            bookingTypedConsumer.ProcessedIntegrationEvent.ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);
            bookingTypedConsumer.CountOfAttempts.Should().Be(1);

            accountTypedConsumer.ProcessedIntegrationEvent.ShouldBeEquivalentTo(accountCreatedIntegrationEvent);
            accountTypedConsumer.CountOfAttempts.Should().Be(1);
            container.Dispose();

        }

        [Fact]
        public async Task Subscriber_2DifferentSubscribersWithdifferentPrefetchCount_SubscribersShouldProcessEventsWithDifferentSpeed()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscriber_2DifferentSubscribersWithdifferentPrefetchCount_SubscribersShouldProcessEventsWithDifferentSpeed.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");


            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber fastSubscriber =
                await bookingCreatedSubscriberFactory
                .CreateSubscriberAsync("Subscriber_2DifferentSubscribersWithdifferentPrefetchCount_SubscribersShouldProcessEventsWithDifferentSpeed" +
                                       ".fast", new List<string>
                    {
                        "*.entity.create.booking",
                    }, maxretrycount, 5);

            ISubscriber slowSubscriber =
                await bookingCreatedSubscriberFactory
                    .CreateSubscriberAsync("Subscriber_2DifferentSubscribersWithdifferentPrefetchCount_SubscribersShouldProcessEventsWithDifferentSpeed" +
                                           ".slow", new List<string>
                    {
                        "*.entity.create.booking",
                    }, maxretrycount, 1);

            var fastBookingTypedSubscriberConsumer = new BookingTypedParallellismCounterConsumer(3000);
            var slowBookingTypedSubscriberConsumer = new BookingTypedParallellismCounterConsumer(3000);
            fastSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingcreated", () => fastBookingTypedSubscriberConsumer)
               .Build());

            slowSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingcreated", () => slowBookingTypedSubscriberConsumer)
                .Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string bookingRoutingKey = "changetracker.entity.create.booking";


            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };


            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };

            for (int i = 1; i <= 8; i++)
            {
                await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, bookingRoutingKey);
            }


            //wait 30 seconds
            await Task.Delay(30000);

            //check

            fastBookingTypedSubscriberConsumer.MaxParrelelEvents.Should().Be(5);
            fastBookingTypedSubscriberConsumer.CountOfProcessedEvents.Should().Be(8);

            slowBookingTypedSubscriberConsumer.MaxParrelelEvents.Should().Be(1);
            slowBookingTypedSubscriberConsumer.CountOfProcessedEvents.Should().Be(8);

            container.Dispose();

        }

        [Fact]
        public async Task Subscriber_2DifferentSubscribersWithEqualPrefetchCount_SubscribersShouldProcessEventsInParallelWithEqualSpeed()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscriber_2DifferentSubscribersWithEqualPrefetchCount_SubscribersShouldProcessEventsInParallelWithEqualSpeed.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");


            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber fastSubscriber =
                await bookingCreatedSubscriberFactory
                .CreateSubscriberAsync("Subscriber_2DifferentSubscribersWithEqualPrefetchCount_SubscribersShouldProcessEventsInParallelWithEqualSpeed" +
                                       ".first", new List<string>
                    {
                        "*.entity.create.booking",
                    }, maxretrycount, 1);

            ISubscriber slowSubscriber =
                await bookingCreatedSubscriberFactory
                    .CreateSubscriberAsync("Subscriber_2DifferentSubscribersWithEqualPrefetchCount_SubscribersShouldProcessEventsInParallelWithEqualSpeed" +
                                           ".second", new List<string>
                    {
                        "*.entity.create.booking",
                    }, maxretrycount, 1);

            var firstBookingTypedSubscriberConsumer = new BookingTypedParallellismCounterConsumer(3000);
            var secondBookingTypedSubscriberConsumer = new BookingTypedParallellismCounterConsumer(3000);
            fastSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingcreated", () => firstBookingTypedSubscriberConsumer)
               .Build());

            slowSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingcreated", () => secondBookingTypedSubscriberConsumer)
                .Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string bookingRoutingKey = "changetracker.entity.create.booking";


            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };


            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };

            for (int i = 1; i <= 8; i++)
            {
                await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, bookingRoutingKey);
            }


            //wait 30 seconds
            await Task.Delay(30000);

            //check

            firstBookingTypedSubscriberConsumer.MaxParrelelEvents.Should().Be(1);
            firstBookingTypedSubscriberConsumer.CountOfProcessedEvents.Should().Be(8);

            secondBookingTypedSubscriberConsumer.MaxParrelelEvents.Should().Be(1);
            secondBookingTypedSubscriberConsumer.CountOfProcessedEvents.Should().Be(8);

            container.Dispose();

        }


        #region Helpers


        public class BookingCreated
        {
            public string BookingName { get; set; }
        }

        public class AccountCreated
        {
            public int AccountLevel { get; set; }
        }

        public class BookingTypedConsumer : IConsumer<BookingCreated>
        {
            private readonly int _countOfAttemptsBeforeSuccess;
            public int CountOfAttempts { get; set; }

            public IntegrationEvent<BookingCreated> ProcessedIntegrationEvent { get; set; }
            public BookingTypedConsumer(int countOfAttemptsBeforeSuccess)
            {
                _countOfAttemptsBeforeSuccess = countOfAttemptsBeforeSuccess;
                CountOfAttempts = 0;
            }
            public async Task ConsumeAsync(IntegrationEvent<BookingCreated> integrationEvent)
            {
                await Task.Delay(50);
                CountOfAttempts = CountOfAttempts + 1;
                if (CountOfAttempts <= _countOfAttemptsBeforeSuccess)
                {
                    throw new Exception("ExceptionHasBeenThrown");
                }

                ProcessedIntegrationEvent = integrationEvent;
            }
        }

        public class AccountTypedConsumer : IConsumer<AccountCreated>
        {
            private readonly int _countOfAttemptsBeforeSuccess;
            public int CountOfAttempts { get; set; }

            public IntegrationEvent<AccountCreated> ProcessedIntegrationEvent { get; set; }
            public AccountTypedConsumer(int countOfAttemptsBeforeSuccess)
            {
                _countOfAttemptsBeforeSuccess = countOfAttemptsBeforeSuccess;
                CountOfAttempts = 0;
            }
            public async Task ConsumeAsync(IntegrationEvent<AccountCreated> integrationEvent)
            {
                await Task.Delay(50);
                CountOfAttempts = CountOfAttempts + 1;
                if (CountOfAttempts <= _countOfAttemptsBeforeSuccess)
                {
                    throw new Exception();
                }

                ProcessedIntegrationEvent = integrationEvent;
            }
        }

        public class DiscardConsumer : IConsumer<BookingCreated>
        {

            public int CountOfAttempts { get; set; }


            public async Task ConsumeAsync(IntegrationEvent<BookingCreated> integrationEvent)
            {
                await Task.Delay(50);
                throw new DiscardEventException();
            }
        }

        public class StubConsumer : IConsumer<object>
        {
            public Task ConsumeAsync(IntegrationEvent<object> integrationEvent)
            {
                return Task.CompletedTask;
            }
        }

        public class BookingTypedParallellismCounterConsumer : IConsumer<BookingCreated>
        {
            private readonly int _delayForConsuming;
            private object _guard = new object();

            public int MaxParrelelEvents = 0;
            public int CountOfProcessedEvents = 0;

            private int _currentParrelelEvents = 0;

            public BookingTypedParallellismCounterConsumer(int delayForConsuming)
            {
                _delayForConsuming = delayForConsuming;
            }
            public async Task ConsumeAsync(IntegrationEvent<BookingCreated> integrationEvent)
            {
                lock (_guard)
                {
                    _currentParrelelEvents = _currentParrelelEvents + 1;
                    if (_currentParrelelEvents > MaxParrelelEvents)
                    {
                        MaxParrelelEvents = _currentParrelelEvents;
                    }
                }

                await Task.Delay(_delayForConsuming);

                lock (_guard)
                {
                    _currentParrelelEvents = _currentParrelelEvents - 1;
                    CountOfProcessedEvents = CountOfProcessedEvents + 1;
                }
            }
        }

        #endregion
    }
}
