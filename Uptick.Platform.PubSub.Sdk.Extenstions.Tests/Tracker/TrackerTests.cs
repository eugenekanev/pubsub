using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Uptick.Platform.PubSub.Sdk.Extenstions.Aggregator;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.Tracker
{
    public class TrackerTests
    {
        [Fact]
        public void Subscribe_SubscriptionsAreProvided_EventTypesAreAdjustedAndSubscriptionsAreApplied()
        {
            //prepare

            var mockSubscriber = new Mock<ISubscriber>();

            IDictionary<string, ISubscription> actualSubscriptions = null;
            mockSubscriber.Setup(x => x.Subscribe(It.IsAny<IDictionary<string, ISubscription>>()))
                .Callback<IDictionary<string, ISubscription>> (
                    (subscriptions) => { actualSubscriptions = subscriptions; });

            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();
            mockModelNamingConventionController.Setup(x => x.GetDeadLetterQueueMessageEventType(It.IsAny<string>()))
                .Returns((string subscriberName) => string.Concat("deadletter.", subscriberName));
            mockModelNamingConventionController.Setup(x => x.GetTrackingMessageEventType(It.IsAny<string>()))
                .Returns((string originalEventType) => string.Concat("ok.", originalEventType));
            IModelNamingConventionController defaultNamingConvensionController = mockModelNamingConventionController.Object;

            DefaultTracker tracker = new DefaultTracker(mockSubscriber.Object, defaultNamingConvensionController);

            //act
            IDictionary<string, ISubscription> successfulProcessingSubscriptions = new Dictionary<string, ISubscription>();
            IDictionary<string, ISubscription> failedProcessingSubscriptions = new Dictionary<string, ISubscription>();

            string eventType1 = "eventType1";
            string eventType2 = "eventType2";

            var mockEventType1SuccessfulSubscription = new Mock<ISubscription>();
            var mockEventType2SuccessfulSubscription = new Mock<ISubscription>();
            var mockEventType1FailedSubscription = new Mock<ISubscription>();
            var mockEventType2FailedfulSubscription = new Mock<ISubscription>();

            successfulProcessingSubscriptions.Add(eventType1, mockEventType1SuccessfulSubscription.Object);
            successfulProcessingSubscriptions.Add(eventType2, mockEventType2SuccessfulSubscription.Object);

            failedProcessingSubscriptions.Add(eventType1, mockEventType1FailedSubscription.Object);
            failedProcessingSubscriptions.Add(eventType2, mockEventType2FailedfulSubscription.Object);

            tracker.Subscribe(successfulProcessingSubscriptions, failedProcessingSubscriptions);

            //check
            IDictionary<string, ISubscription> expecteEffectiveSubscriptions= new Dictionary<string, ISubscription>();

            expecteEffectiveSubscriptions.Add(defaultNamingConvensionController.GetTrackingMessageEventType(eventType1),
                mockEventType1SuccessfulSubscription.Object);
            expecteEffectiveSubscriptions.Add(defaultNamingConvensionController.GetTrackingMessageEventType(eventType2),
                mockEventType2SuccessfulSubscription.Object);

            expecteEffectiveSubscriptions.Add(defaultNamingConvensionController.GetDeadLetterQueueMessageEventType(eventType1),
                mockEventType1FailedSubscription.Object);
            expecteEffectiveSubscriptions.Add(defaultNamingConvensionController.GetDeadLetterQueueMessageEventType(eventType2),
                mockEventType2FailedfulSubscription.Object);

            expecteEffectiveSubscriptions.ShouldBeEquivalentTo(actualSubscriptions);
        }

        [Fact]
        public void UnSubscribe_UnSubscribeFromInternalSubscriber()
        {
            //prepare

            var mockSubscriber = new Mock<ISubscriber>();

            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();
            IModelNamingConventionController defaultModelNamingConventionController = mockModelNamingConventionController.Object;

            DefaultTracker tracker = new DefaultTracker(mockSubscriber.Object, defaultModelNamingConventionController);

            //act

            tracker.Dispose();

            //check
            mockSubscriber.Verify(v => v.Dispose(), Times.Once);
        }
    }
}
