using Autofac;
using EasyNetQ.Management.Client;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using IContainer = Autofac.IContainer;

namespace Uptick.Platform.PubSub.Sdk.ComponentTests
{
    /// <summary>
    /// Temporary queue tests
    /// </summary>
    /// <remarks>
    /// run separately from other tests, because disconnection influence other test progress
    /// for Aggregator disconnection cause loosing await lock on event and processing same events multiple times
    /// </remarks>
    [Collection("Non-Parallel Collection")]
    public class TemporaryQueueTests
    {
        private readonly ILogger _logger;

        public TemporaryQueueTests(ITestOutputHelper output)
        {
            _logger = new LoggerConfiguration().WriteTo.TestOutput(output).CreateLogger().ForContext<TemporaryQueueTests>();
        }

        /// <summary>
        /// Check that callback is executed after rabbitmq disconnect
        /// </summary>
        /// <returns>system task object</returns>
        [Fact]
        public async Task Subscriber_NetworkDisconnected_CallbackExecuted()
        {
            //prepare
            IConfiguration configuration = InitializeConfiguration();

            const int subscriberNum = 5;

            var disconnectedEventFired = new bool[subscriberNum];

            const int maxRetryCount = 1;
            const int maxPrefetchCount = 1;

            using (IContainer container = ConfigurationHelper.ConfigureContainer(configuration))
            {
                //create Subscriber
                var testEntitySubscriberFactory = container.Resolve<ISubscriberFactory>();

                for (int i = 0; i < subscriberNum; i++)
                {
                    var index = i;

                    ISubscriber subscriber =
                        await testEntitySubscriberFactory
                            .CreateSubscriberAsync($"Publiser_TemporaryQueue.testSubscriber_{Guid.NewGuid()}",
                                () => { disconnectedEventFired[index] = true; },
                                new List<string>
                                    {
                                        "*.entity.create.testEntity",
                                    }, maxRetryCount, maxPrefetchCount);

                    // create Subscriber
                    subscriber.Subscribe(SubscriptionBuilder.Create()
                        .AddSubscription("testEntityCreated", () => (IConsumer<string>)null)
                        .Build());
                }

                var managementClient = InitializeManagementClient(configuration);
                await CloseRabbitConnection(managementClient);

                await Task.Delay(100);

                disconnectedEventFired.ShouldAllBeEquivalentTo(true);
            }
        }

        /// <summary>
        /// Check that temporary queue is deleted after disconnect
        /// </summary>
        /// <returns>system task object</returns>
        [Fact]
        public async Task Subscriber_NetworkDisconnected_TemporaryQueueIsDeleted()
        {
            //prepare
            IConfiguration configuration = InitializeConfiguration();

            const int maxRetryCount = 1;
            const int maxPrefetchCount = 1;

            using (IContainer container = ConfigurationHelper.ConfigureContainer(configuration))
            {
                //create Subscriber
                var testEntitySubscriberFactory = container.Resolve<ISubscriberFactory>();

                var subscriberName = $"Publiser_TemporaryQueue.testSubscriber_{Guid.NewGuid()}";

                ISubscriber subscriber =
                    await testEntitySubscriberFactory
                        .CreateSubscriberAsync(subscriberName,
                            () => { },
                            new List<string>
                                {
                                    "*.entity.create.testEntity",
                                }, maxRetryCount, maxPrefetchCount);

                // create Subscriber
                subscriber.Subscribe(SubscriptionBuilder.Create()
                    .AddSubscription("testEntityCreated", () => (IConsumer<string>)null)
                    .Build());

                var managementClient = InitializeManagementClient(configuration);
                await CloseRabbitConnection(managementClient);

                await Task.Delay(100);

                var queuExists = await CheckQueueExists(managementClient, subscriberName);
                queuExists.Should().BeFalse();
            }
        }

        /// <summary>
        /// Check that disconnect event fires only one time
        /// </summary>
        /// <returns>system task object</returns>
        [Fact]
        public async Task Subscriber_SeveralNetworkDisconnected_CallbackDelegateRunOnce()
        {
            //prepare
            IConfiguration configuration = InitializeConfiguration();

            const int maxRetryCount = 1;
            const int maxPrefetchCount = 1;

            using (IContainer container = ConfigurationHelper.ConfigureContainer(configuration))
            {
                //create Subscriber
                var testEntitySubscriberFactory = container.Resolve<ISubscriberFactory>();

                var subscriberName = $"Publiser_TemporaryQueue.testSubscriber_{Guid.NewGuid()}";

                var numberOfRuns = 0;
                ISubscriber subscriber =
                    await testEntitySubscriberFactory
                        .CreateSubscriberAsync(subscriberName,
                            () => { numberOfRuns++; },
                            new List<string>
                                {
                                    "*.entity.create.testEntity",
                                }, maxRetryCount, maxPrefetchCount);

                // create Subscriber
                subscriber.Subscribe(SubscriptionBuilder.Create()
                    .AddSubscription("testEntityCreated", () => (IConsumer<string>)null)
                    .Build());

                var managementClient = InitializeManagementClient(configuration);

                const int disconnectedNum = 5;

                for (int i = 0; i < disconnectedNum; i++)
                {
                    await CloseRabbitConnection(managementClient);

                    await Task.Delay(500);
                }

                numberOfRuns.Should().Be(1);
            }
        }

        /// <summary>
        /// Check that new subscriber successfully created after disconnect and receive event
        /// </summary>
        /// <returns>system task object</returns>
        [Fact]
        public async Task Subscriber_NetworkDisconnectedNewSubscriberCreated_EventReceived()
        {
            //prepare
            IConfiguration configuration = InitializeConfiguration();

            const int maxRetryCount = 1;
            const int maxPrefetchCount = 1;

            using (IContainer container = ConfigurationHelper.ConfigureContainer(configuration))
            {
                //create Subscriber
                var testEntitySubscriberFactory = container.Resolve<ISubscriberFactory>();

                var subscriberName = $"Publiser_TemporaryQueue.testSubscriber_{Guid.NewGuid()}";

                TaskKeeper recreatingTask = new TaskKeeper();
                ISubscriber subscriber =
                    await testEntitySubscriberFactory
                        .CreateSubscriberAsync(subscriberName,
                            () => { recreatingTask.Task = RecreateSubscriber(); },
                            new List<string>
                            {
                                "*.entity.create.testEntity",
                            }, maxRetryCount, maxPrefetchCount);

                // create Subscriber
                subscriber.Subscribe(SubscriptionBuilder.Create()
                    .AddSubscription("testEntityCreated", () => (IConsumer<string>)null)
                    .Build());

                var managementClient = InitializeManagementClient(configuration);
                await CloseRabbitConnection(managementClient);

                while (recreatingTask.Task == null)
                {
                    await Task.Delay(100);
                }

                await recreatingTask.Task;

                async Task RecreateSubscriber()
                {
                    try
                    {
                        var subscriberNewName = $"Publiser_TemporaryQueue.testSubscriber_{Guid.NewGuid()}";
                        subscriber =
                            await testEntitySubscriberFactory
                                .CreateSubscriberAsync(subscriberNewName,
                                    true,
                                    new List<string>
                                        {
                                            "*.entity.create.testEntity",
                                        }, maxRetryCount, maxPrefetchCount);

                        // create Subscriber
                        var testConsumer = new TestEventConsumer();
                        subscriber.Subscribe(SubscriptionBuilder.Create()
                            .AddSubscription("testEntityCreated", () => testConsumer)
                            .Build());

                        var publisher = container.Resolve<IPublisher>();

                        var testEvent = new IntegrationEvent<string>
                        {
                            EventId = Guid.NewGuid(),
                            CorrelationId = Guid.NewGuid(),
                            EventCreationDate = DateTime.UtcNow,
                            EventType = "testEntityCreated",
                            Version = "0.1",
                            Content = Guid.NewGuid().ToString()
                        };

                        var routingKey = "test.entity.create.testEntity";
                        await publisher.PublishEventAsync(testEvent, routingKey);

                        await Task.Delay(3000);

                        // we successfully received event after disconnect
                        testConsumer.ConsumedEvents.Should().BeGreaterOrEqualTo(1);
                    }
                    catch (Exception e)
                    {
                        Assert.True(false, $"{e.Message}");
                    }
                }
            }
        }

        public class TaskKeeper
        {
            public Task Task { get; set; }
        }

        public class TestEventConsumer : IConsumer<string>
        {
            public int ConsumedEvents { get; set; }

            public Task ConsumeAsync(IntegrationEvent<string> integrationEvent)
            {
                ConsumedEvents++;
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Initialize configuration
        /// </summary>
        /// <returns>initialized configuration</returns>
        private IConfiguration InitializeConfiguration()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName = "Publiser_TemporaryQueue.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");
            return configuration;
        }

        /// <summary>
        /// Close rabbitmq connection
        /// </summary>
        /// <returns>management client</returns>
        private async Task CloseRabbitConnection(ManagementClient managementClient)
        {
            var rabbitConnections = await managementClient.GetConnectionsAsync(CancellationToken.None);
            foreach (var connection in rabbitConnections)
            {
                try
                {
                    await managementClient.CloseConnectionAsync(connection, CancellationToken.None);
                }
                catch (Exception exp)
                {
                    _logger.Error(exp, $"Error closing connection: {exp.Message}");
                }
            }
        }

        /// <summary>
        /// Close rabbitmq connection
        /// </summary>
        /// <returns>management client</returns>
        private async Task<bool> CheckQueueExists(ManagementClient managementClient, string queueName)
        {
            var rabbitQueues = await managementClient.GetQueuesAsync();
            return rabbitQueues.Any(x => x.Name.EndsWith(queueName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Initialize management client
        /// </summary>
        /// <returns>management client</returns>
        private ManagementClient InitializeManagementClient(IConfiguration configuration)
        {
            return new ManagementClient(
                configuration["rabbitmqmanagement:hostUrl"], configuration["rabbitmqmanagement:username"],
                configuration["rabbitmqmanagement:password"], configuration.GetValue<int>("rabbitmqmanagement:portNumber"));
        }
    }
}
