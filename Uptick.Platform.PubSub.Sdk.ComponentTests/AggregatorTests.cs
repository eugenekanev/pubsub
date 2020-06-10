using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Uptick.Platform.PubSub.Sdk.Extenstions.Aggregator;
using Xunit;
using ExchangeType = RabbitMQ.Client.ExchangeType;
using IContainer = Autofac.IContainer;

namespace Uptick.Platform.PubSub.Sdk.ComponentTests
{
    public class AggregatorTests
    {
        #region Aggregator Test
        [Fact]
        public async Task Aggregator_Consume40EventsWith5BucketSize_8BucketsMustBeCreated()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Aggregator_Consume40EventsWith5BucketSize_8BucketsMustBeCreated.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            var maxretrycount = 2;
            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingactivityCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber aggregatorSubscriber =
                await bookingactivityCreatedSubscriberFactory
                    .CreateSubscriberAsync("Aggregator_Consume40EventsWith5BucketSize_8BucketsMustBeCreated" +
                                           ".bookingactivitycreatedclusterizator", new List<string>
                    {
                        "*.entity.create.bookingactivity",
                    }, maxretrycount, 20);


            var activityBatchSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber activityBatchSubscriber =
                await activityBatchSubscriberFactory
                    .CreateSubscriberAsync("Aggregator_Consume40EventsWith5BucketSize_8BucketsMustBeCreated" +
                                           ".activityclusterizator", new List<string>
                    {
                        "*.activitytagging",
                    }, maxretrycount, 1);


            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string bookingactivityRoutingKey = "changetracker.entity.create.bookingactivity";
            string activitytaggingRoutingKey = "activityclusterizator.activitytagging";

            //create aggregator
            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity, Guid>(
                intergrationevent => Task.FromResult(intergrationevent.Content.ActivityId), 5,
                activitiesIds =>
                {
                    var activitytaggingintergrationevnt = new IntegrationEvent<ActivityTags>
                    {
                        EventId = Guid.NewGuid(),
                        CorrelationId = Guid.NewGuid(),
                        EventCreationDate = DateTime.UtcNow,
                        EventType = "activitytagging",
                        Version = "0.1",
                        Content = new ActivityTags() {ActivityIds = activitiesIds.ToList()}
                    };

                    return publisher.PublishEventAsync(activitytaggingintergrationevnt, activitytaggingRoutingKey);

                }, TimeSpan.Zero);

            aggregatorSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingactivityCreated", () => bookingActivitiesChangeTrackerEventAggregator)
                .Build());

            var activityBatchProcessor = new ActivityTagsConsumer();
            activityBatchSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("activitytagging", () => activityBatchProcessor)
                .Build());

            List<IntegrationEvent<BookingActivity>> originalBookingActivityCreatedEvents = new List<IntegrationEvent<BookingActivity>>();

            for (int i = 1; i <= 40; i++)
            {
                originalBookingActivityCreatedEvents.Add(new IntegrationEvent<BookingActivity>
                {
                    EventId = Guid.NewGuid(),
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventType = "bookingactivityCreated",
                    Version = "0.1",
                    Content = new BookingActivity() { ActivityId = Guid.NewGuid(), BookingId = Guid.NewGuid() }
                });
            }

            //act
            foreach (var bookingActivityCreatedEvent in originalBookingActivityCreatedEvents)
            {
                await publisher.PublishEventAsync(bookingActivityCreatedEvent, bookingactivityRoutingKey);
            }

            //wait 10 seconds
            await Task.Delay(10000);

            //check
            List<Guid> expectedresult = new List<Guid>();
            foreach (var processedIntegrationEvent in activityBatchProcessor.ProcessedIntegrationEvents)
            {
                processedIntegrationEvent.Content.ActivityIds.Count().Should().Be(5);
                expectedresult.AddRange(processedIntegrationEvent.Content.ActivityIds);
            }

            expectedresult.ShouldBeEquivalentTo(originalBookingActivityCreatedEvents.Select(ob => ob.Content.ActivityId));
            container.Dispose();


        }

        [Fact]
        public async Task Aggregator_Consume40EventsWith5BucketSize_2BucketsMustBeCreatedBecauseOfSizeAndPeriod()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Aggregator_Consume40EventsWith5BucketSize_2BucketsMustBeCreatedBecauseOfSizeAndPeriod.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            var maxretrycount = 2;
            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingactivityCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber aggregatorSubscriber =
                await bookingactivityCreatedSubscriberFactory
                    .CreateSubscriberAsync("Aggregator_Consume40EventsWith5BucketSize_2BucketsMustBeCreatedBecauseOfSizeAndPeriod" +
                                           ".bookingactivitycreatedclusterizator", new List<string>
                    {
                        "*.entity.create.bookingactivity",
                    }, maxretrycount, 40);


            var activityBatchSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber activityBatchSubscriber =
                await activityBatchSubscriberFactory
                    .CreateSubscriberAsync("Aggregator_Consume40EventsWith5BucketSize_2BucketsMustBeCreatedBecauseOfSizeAndPeriod" +
                                           ".activityclusterizator", new List<string>
                    {
                        "*.activitytagging",
                    }, maxretrycount, 1);


            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string bookingactivityRoutingKey = "changetracker.entity.create.bookingactivity";
            string activitytaggingRoutingKey = "activityclusterizator.activitytagging";

            //create aggregator
            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity, Guid>(
                intergrationevent => Task.FromResult(intergrationevent.Content.ActivityId), 30,
                activitiesIds =>
                {
                    var activitytaggingintergrationevnt = new IntegrationEvent<ActivityTags>
                    {
                        EventId = Guid.NewGuid(),
                        CorrelationId = Guid.NewGuid(),
                        EventCreationDate = DateTime.UtcNow,
                        EventType = "activitytagging",
                        Version = "0.1",
                        Content = new ActivityTags() { ActivityIds = activitiesIds.ToList() }
                    };

                    return publisher.PublishEventAsync(activitytaggingintergrationevnt, activitytaggingRoutingKey);

                }, TimeSpan.FromSeconds(10));

            aggregatorSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingactivityCreated", () => bookingActivitiesChangeTrackerEventAggregator)
                .Build());

            var activityBatchProcessor = new ActivityTagsConsumer();
            activityBatchSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("activitytagging", () => activityBatchProcessor)
                .Build());

            List<IntegrationEvent<BookingActivity>> originalBookingActivityCreatedEvents = new List<IntegrationEvent<BookingActivity>>();

            for (int i = 1; i <= 40; i++)
            {
                originalBookingActivityCreatedEvents.Add(new IntegrationEvent<BookingActivity>
                {
                    EventId = Guid.NewGuid(),
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventType = "bookingactivityCreated",
                    Version = "0.1",
                    Content = new BookingActivity() { ActivityId = Guid.NewGuid(), BookingId = Guid.NewGuid() }
                });
            }

            //act
            foreach (var bookingActivityCreatedEvent in originalBookingActivityCreatedEvents)
            {
                await publisher.PublishEventAsync(bookingActivityCreatedEvent, bookingactivityRoutingKey);
            }

            //wait 12 seconds
            await Task.Delay(12000);

            //check
            List<Guid> expectedresult = new List<Guid>();

            IntegrationEvent<ActivityTags> countBasedBucket =  activityBatchProcessor.ProcessedIntegrationEvents[0];
            countBasedBucket.Content.ActivityIds.Count().Should().Be(30);
            expectedresult.AddRange(countBasedBucket.Content.ActivityIds);

            IntegrationEvent<ActivityTags> maxWaitingTimeBasedBucket = activityBatchProcessor.ProcessedIntegrationEvents[1];
            maxWaitingTimeBasedBucket.Content.ActivityIds.Count().Should().Be(10);
            expectedresult.AddRange(maxWaitingTimeBasedBucket.Content.ActivityIds);

            expectedresult.ShouldBeEquivalentTo(originalBookingActivityCreatedEvents.Select(ob => ob.Content.ActivityId));
            container.Dispose();


        }
        #endregion

        #region Helpers

        public class BookingActivity
        {
            public Guid ActivityId { get; set; }

            public Guid BookingId { get; set; }

        }

        public class ActivityTags
        {
            public List<Guid> ActivityIds { get; set; }

        }

        public class ActivityTagsConsumer : IConsumer<ActivityTags>
        {

            public List<IntegrationEvent<ActivityTags>> ProcessedIntegrationEvents { get; set; }
            public ActivityTagsConsumer()
            {
                ProcessedIntegrationEvents = new List<IntegrationEvent<ActivityTags>>();
            }
            public async Task ConsumeAsync(IntegrationEvent<ActivityTags> integrationEvent)
            {
                await Task.Delay(50);
                ProcessedIntegrationEvents.Add(integrationEvent);
            }
        }
        #endregion
    }
}
