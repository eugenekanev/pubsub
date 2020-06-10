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
    public class AggregatorByCountTests
    {
        [Fact]
        public async Task Consume_10EventsAreMappedAndReducedSuccessfully_2BucketMustBeClosed()
        {
            //prepare

            IEnumerable<Guid>[] groupedActivityIds = new IEnumerable<Guid>[2];
            int groupedActivityIdsIndex = -1;


            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity,Guid>(
                intergrationevent => Task.FromResult(intergrationevent.Content.ActivityId),5,
                activitiesIds =>
                {
                   int bucketIndex = Interlocked.Increment(ref groupedActivityIdsIndex);
                    groupedActivityIds[bucketIndex] = new List<Guid>(activitiesIds);
                    return Task.FromResult(true);
                }, TimeSpan.Zero);


            List<IntegrationEvent<BookingActivity>> originalBookingActivityCreatedEvents = new List<IntegrationEvent<BookingActivity>>();

            for (int i = 1; i <= 10; i++)
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
            var parallelqueryoftasks = originalBookingActivityCreatedEvents.AsParallel().Select(async (integrationEvent) =>
                await bookingActivitiesChangeTrackerEventAggregator.ConsumeAsync(integrationEvent));
            var realtasks = parallelqueryoftasks.ToArray();
            await Task.WhenAll(realtasks);


            //act 

            //check
            groupedActivityIds[0].Count().Should().Be(5);
            groupedActivityIds[1].Count().Should().Be(5);

            List<Guid> result = groupedActivityIds[0].ToList();
            result.AddRange(groupedActivityIds[1]);

            result.ShouldBeEquivalentTo(originalBookingActivityCreatedEvents.Select(ob=>ob.Content.ActivityId));

            realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(0);
            realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(10);

        }

        [Fact]
        public async Task Consume_13EventsAreMappedSuccessfully_2BucketMustBeClosed()
        {
            //prepare

            IEnumerable<Guid>[] groupedActivityIds = new IEnumerable<Guid>[3];
            int groupedActivityIdsIndex = -1;
            int countOfMapCalls = 0;


            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity, Guid>(
                intergrationevent =>
                {
                    Interlocked.Increment(ref countOfMapCalls);
                    return Task.FromResult(intergrationevent.Content.ActivityId);
                }, 5,
                activitiesIds =>
                {
                    int bucketIndex = Interlocked.Increment(ref groupedActivityIdsIndex);
                    groupedActivityIds[bucketIndex] = new List<Guid>(activitiesIds);
                    return Task.FromResult(true);
                }, TimeSpan.Zero);


            List<IntegrationEvent<BookingActivity>> originalBookingActivityCreatedEvents = new List<IntegrationEvent<BookingActivity>>();

            for (int i = 1; i <= 13; i++)
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
            var parallelqueryoftasks = originalBookingActivityCreatedEvents.AsParallel().Select((integrationEvent) => bookingActivitiesChangeTrackerEventAggregator.ConsumeAsync(integrationEvent));

            var realtasks = parallelqueryoftasks.ToArray();

            await Task.WhenAny(realtasks); // this code is required to have test runner to start tasks assigned to variable tasks

            await Task.Delay(5000);

            //check
            groupedActivityIds[0].Count().Should().Be(5);
            groupedActivityIds[1].Count().Should().Be(5);
            groupedActivityIds[2].Should().BeNull();

            List<Guid> result = groupedActivityIds[0].ToList();
            result.AddRange(groupedActivityIds[1]);

            result.Distinct().Count().Should().Be(10);

            foreach (var bucketsitems in result)
            {
              Assert.True(originalBookingActivityCreatedEvents.Select(ob => ob.Content.ActivityId).Contains(bucketsitems));
            }

            realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(0);
            realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(10);

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
                , 5,
                activitiesIds =>
                {
                    int bucketIndex = Interlocked.Increment(ref groupedActivityIdsIndex);
                    groupedActivityIds[bucketIndex] = new List<Guid>(activitiesIds);
                    return Task.FromResult(true);
                }, TimeSpan.Zero);


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

            await Task.Delay(5000);


            //act 

            //check
            groupedActivityIds[0].Count().Should().Be(5);
            groupedActivityIds[1].Should().BeNull();

            List<Guid> result = groupedActivityIds[0].ToList();


            foreach (var bucketsitems in result)
            {
                Assert.False(originalBookingActivityCreatedEvents.Where(ev=>ev.Version == "2" || ev.Version == "5" || ev.Version == "8"
                ).Select(ob => ob.Content.ActivityId).Contains(bucketsitems));
            }

            realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(3);
            realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(5);

        }


        [Fact]
        public async Task Consume_13EventsAreMappedSuccessfully1BucketFailedDuringReduce_1BucketMustBeClosed()
        {
            //prepare

            IEnumerable<Guid>[] groupedActivityIds = new IEnumerable<Guid>[2];
            int groupedActivityIdsIndex = -1;

            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity, Guid>(
                intergrationevent =>Task.FromResult(intergrationevent.Content.ActivityId), 5,
                activitiesIds =>
                {
                    int bucketIndex = Interlocked.Increment(ref groupedActivityIdsIndex);
                    if (bucketIndex ==1) throw new ArgumentException();

                    groupedActivityIds[bucketIndex] = new List<Guid>(activitiesIds);
                    return Task.FromResult(true);
                }, TimeSpan.Zero);


            List<IntegrationEvent<BookingActivity>> originalBookingActivityCreatedEvents = new List<IntegrationEvent<BookingActivity>>();

            for (int i = 1; i <= 13; i++)
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
            var parallelqueryoftasks = originalBookingActivityCreatedEvents.AsParallel().Select((integrationEvent) => bookingActivitiesChangeTrackerEventAggregator.ConsumeAsync(integrationEvent));
            var realtasks = parallelqueryoftasks.ToArray();
            await Task.WhenAny(realtasks); // this code is required to have test runner to start tasks assigned to variable tasks

            await Task.Delay(5000);


            //act 

            //check
            groupedActivityIds[0].Count().Should().Be(5);
            groupedActivityIds[1].Should().BeNull();

            List<Guid> result = groupedActivityIds[0].ToList();


            foreach (var bucketsitems in result)
            {
                Assert.True(originalBookingActivityCreatedEvents.Select(ob => ob.Content.ActivityId).Contains(bucketsitems));
            }

            realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(5);
            realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(5);

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
