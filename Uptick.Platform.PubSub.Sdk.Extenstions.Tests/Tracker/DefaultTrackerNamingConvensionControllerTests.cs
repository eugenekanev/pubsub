using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Moq;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.Tracker
{
    public class DefaultTrackerNamingConvensionControllerTests
    {
        [Fact]
        public void GetTrackerQueueName_trackerNameIsProvided_AdjustedTrackerNameIsReturned()
        {
            //prepare

            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();
            var defaultTrackerNamingConvensionController = new DefaultTrackerNamingConventionController(mockModelNamingConventionController.Object);

            //act
            string trackerName = "trackerName";
            string adjustedTrackerName = defaultTrackerNamingConvensionController.GetTrackerQueueName(trackerName);

            //check
            adjustedTrackerName.Should().Be(string.Concat("tracking.", trackerName));
        }

        [Fact]
        public void GetTrackerQueueRoutingKeys_targetSubscriberNamesAreProvided_EffectiveRoutingKeysAreReturned()
        {
            //prepare

            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();
            var defaultTrackerNamingConvensionController = new DefaultTrackerNamingConventionController(mockModelNamingConventionController.Object);

            string subscriberName1 = "subscriberName1";
            string subscriberName2 = "subscriberName2";

            mockModelNamingConventionController.Setup(x => x.GetDeadLetterQueueRoutingKey(It.IsAny<string>()))
                .Returns((string subscriberName) => string.Concat("deadletter.", subscriberName));

            mockModelNamingConventionController.Setup(x => x.GetTrackingRoutingKey(It.IsAny<string>()))
                .Returns((string subscriberName) => string.Concat("ok.", subscriberName));

            //act

            IEnumerable<string> routingKeys = 
                defaultTrackerNamingConvensionController.GetTrackerQueueRoutingKeys(new List<string>{ subscriberName1 , subscriberName2});

            //check
            routingKeys.ShouldBeEquivalentTo(new List<string>
            {
                string.Concat("ok.", subscriberName1),
                string.Concat("ok.", subscriberName2),
                string.Concat("deadletter.", subscriberName1),
                string.Concat("deadletter.", subscriberName2)
            });
        }
    }
}
