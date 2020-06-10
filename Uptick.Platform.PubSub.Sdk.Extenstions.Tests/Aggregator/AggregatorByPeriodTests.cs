using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Uptick.Platform.PubSub.Sdk.Extenstions.Aggregator;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests
{
    public class AggregatorByPeriodTests
    {
        [Fact]
        public async Task Consume_4EventsAreMappedAndReducedSuccessfully_2BucketMustBeClosed()
        {
            //prepare

            IEnumerable<Guid>[] groupedActivityIds = new IEnumerable<Guid>[2];
            int groupedActivityIdsIndex = -1;


            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity,Guid>(
                intergrationevent => Task.FromResult(intergrationevent.Content.ActivityId),1000,
                activitiesIds =>
                {
                   int bucketIndex = Interlocked.Increment(ref groupedActivityIdsIndex);
                    groupedActivityIds[bucketIndex] = new List<Guid>(activitiesIds);
                    return Task.FromResult(true);
                }, TimeSpan.FromSeconds(7));


            List<IntegrationEvent<BookingActivity>> originalBookingActivityCreatedEvents = new List<IntegrationEvent<BookingActivity>>();

            for (int i = 1; i <= 4; i++)
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
            var realtasks = new List<Task>();
            foreach (var integrationEvent in originalBookingActivityCreatedEvents)
            {
                realtasks.Add(bookingActivitiesChangeTrackerEventAggregator.ConsumeAsync(integrationEvent));
                await Task.Delay(3000);
            }

            //act 

            //check
            await Task.Delay(7000);

            //As timing is very different on the build-server there might be several path of the execution

            List<Guid> result = groupedActivityIds[0].ToList();
            if (groupedActivityIds[0].Count() == 3)
            {
                groupedActivityIds[1].Count().Should().Be(1);
                result.AddRange(groupedActivityIds[1]);
            }
            else
            {
                groupedActivityIds[0].Count().Should().Be(4);
                groupedActivityIds[1].Should().BeNull();
            }

            result.ShouldBeEquivalentTo(originalBookingActivityCreatedEvents.Select(ob=>ob.Content.ActivityId));

            realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(0);
            realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(4);

        }

        [Fact]
        public async Task Consume_7EventsAreMappedSuccessfully3AreFailedDuringMap_1BucketMustBeClosed()
        {
            //prepare

            IEnumerable<Guid>[] groupedActivityIds = new IEnumerable<Guid>[2];
            int groupedActivityIdsIndex = -1;

            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity, Guid>(
                intergrationevent =>
                {
                    string eventVersion = intergrationevent.Version;
                    if (eventVersion == "2" || eventVersion == "5" ||
                       eventVersion == "8")
                    {
                        throw new ArgumentException();
                    }
                    return Task.FromResult(intergrationevent.Content.ActivityId);
                }
                , 1000,
                activitiesIds =>
                {
                    int bucketIndex = Interlocked.Increment(ref groupedActivityIdsIndex);
                    groupedActivityIds[bucketIndex] = new List<Guid>(activitiesIds);
                    return Task.FromResult(true);
                }, TimeSpan.FromSeconds(7));


            List<IntegrationEvent<BookingActivity>> originalBookingActivityCreatedEvents = new List<IntegrationEvent<BookingActivity>>();

            for (int i = 1; i <= 10; i++)
            {
                originalBookingActivityCreatedEvents.Add(new IntegrationEvent<BookingActivity>
                {
                    EventId = Guid.NewGuid(),
                    CorrelationId = Guid.NewGuid(),
                    EventCreationDate = DateTime.UtcNow,
                    EventType = "bookingactivityCreated",
                    Version = i.ToString(),
                    Content = new BookingActivity() { ActivityId = Guid.NewGuid(), BookingId = Guid.NewGuid() }
                });
            }

            //act
            var parallelqueryoftasks = originalBookingActivityCreatedEvents.AsParallel().Select((integrationEvent) => bookingActivitiesChangeTrackerEventAggregator.ConsumeAsync(integrationEvent));
            var realtasks = parallelqueryoftasks.ToArray();
            await Task.WhenAny(realtasks); // this code is required to have test runner to start tasks assigned to variable tasks

            await Task.Delay(8000);


            //act 

            //check
            groupedActivityIds[0].Count().Should().Be(7);
            groupedActivityIds[1].Should().BeNull();

            List<Guid> result = groupedActivityIds[0].ToList();


            foreach (var bucketsitems in result)
            {
                Assert.False(originalBookingActivityCreatedEvents.Where(ev=>ev.Version == "2" || ev.Version == "5" || ev.Version == "8"
                ).Select(ob => ob.Content.ActivityId).Contains(bucketsitems));
            }

            realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(3);
            realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(7);

        }


        [Fact]
        public async Task Consume_5EventsAreMappedSuccessfully1BucketFailedDuringReduce_1BucketMustBeClosed()
        {
            //prepare

            IEnumerable<Guid>[] groupedActivityIds = new IEnumerable<Guid>[2];
            int groupedActivityIdsIndex = -1;

            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity, Guid>(
                intergrationevent =>Task.FromResult(intergrationevent.Content.ActivityId), 1000,
                activitiesIds =>
                {
                    int bucketIndex = Interlocked.Increment(ref groupedActivityIdsIndex);
                    if (bucketIndex ==1) throw new ArgumentException();

                    groupedActivityIds[bucketIndex] = new List<Guid>(activitiesIds);
                    return Task.FromResult(true);
                }, TimeSpan.FromSeconds(7));


            List<IntegrationEvent<BookingActivity>> originalBookingActivityCreatedEvents = new List<IntegrationEvent<BookingActivity>>();

            for (int i = 1; i <= 5; i++)
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
            var realtasks = new List<Task>();
            foreach (var integrationEvent in originalBookingActivityCreatedEvents)
            {
                realtasks.Add(bookingActivitiesChangeTrackerEventAggregator.ConsumeAsync(integrationEvent));
                await Task.Delay(2000);
            }

            await Task.Delay(7000);


            //act 

            //As timing is very different on the build-server there might be several path of the execution

            groupedActivityIds[1].Should().BeNull();
            if (groupedActivityIds[0].Count() == 4)
            {
                realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(1);
                realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(4);
            }
            else
            {
                groupedActivityIds[0].Count().Should().Be(5);
                realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(0);
                realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(5);
            }


            List<Guid> result = groupedActivityIds[0].ToList();


            foreach (var bucketsitems in result)
            {
                Assert.True(originalBookingActivityCreatedEvents.Select(ob => ob.Content.ActivityId).Contains(bucketsitems));
            }

           

        }


        #region helpers

        public class BookingActivity
        {
            public Guid ActivityId { get; set; }

            public Guid BookingId { get; set; }

        }

        #endregion
    }
}
