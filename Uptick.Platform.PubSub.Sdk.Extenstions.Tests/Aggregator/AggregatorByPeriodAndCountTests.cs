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
    public class AggregatorByPeriodAndCountTests
    {
        [Fact]
        public async Task Consume_4EventsAreMappedAndReducedSuccessfully_2BucketMustBeClosedBecauseOfCountAndPeriod()
        {
            //prepare

            IEnumerable<Guid>[] groupedActivityIds = new IEnumerable<Guid>[2];
            int groupedActivityIdsIndex = -1;


            var bookingActivitiesChangeTrackerEventAggregator = new Aggregator<BookingActivity,Guid>(
                intergrationevent => Task.FromResult(intergrationevent.Content.ActivityId),3,
                activitiesIds =>
                {
                   int bucketIndex = Interlocked.Increment(ref groupedActivityIdsIndex);
                    groupedActivityIds[bucketIndex] = new List<Guid>(activitiesIds);
                    return Task.FromResult(true);
                }, TimeSpan.FromSeconds(5));


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
            var parallelqueryoftasks = originalBookingActivityCreatedEvents.AsParallel().Select(async (integrationEvent) =>
                await bookingActivitiesChangeTrackerEventAggregator.ConsumeAsync(integrationEvent));
            var realtasks = parallelqueryoftasks.ToArray();
            await Task.WhenAll(realtasks);

            //act 

            //check
            await Task.Delay(7000);

            //first bucket is closed because of max count is reached
            groupedActivityIds[0].Count().Should().Be(3);
            //second bucket is closed because of max waiting time is reached.
            groupedActivityIds[1].Count().Should().Be(1);

            List<Guid> result = groupedActivityIds[0].ToList();
            result.AddRange(groupedActivityIds[1]);

            result.ShouldBeEquivalentTo(originalBookingActivityCreatedEvents.Select(ob=>ob.Content.ActivityId));

            realtasks.Count(t => t.IsCompleted && t.IsFaulted).Should().Be(0);
            realtasks.Count(t => t.IsCompleted && t.IsCompletedSuccessfully).Should().Be(4);

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
