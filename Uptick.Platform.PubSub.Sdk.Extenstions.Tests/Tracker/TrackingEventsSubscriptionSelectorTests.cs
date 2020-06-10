using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Moq;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.Tracker
{
    public class TrackingEventsSubscriptionSelectorTests
    {
        [Fact]
        public void Select_DefaultSubscriptionsAreRegisteredDifferentScenatios_AppropriateSubscriptionOrNullIsReturned()
        {
            //prepare
            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();

            mockModelNamingConventionController.Setup(x => x.GetTrackingMessageEventType("*"))
                .Returns((string eventType) => string.Concat("ok.", eventType));
            mockModelNamingConventionController.Setup(x => x.GetDeadLetterQueueMessageEventType("*"))
                .Returns((string eventType) => string.Concat("deadletter.", eventType));

            mockModelNamingConventionController.Setup(x => x.IsTrackingMessageEventType(It.IsAny<string>()))
                .Returns((string eventType) => eventType.StartsWith("ok."));

            mockModelNamingConventionController.Setup(x => x.IsDeadLetterQueueMessageEventType(It.IsAny<string>()))
                .Returns((string eventType) => eventType.StartsWith("deadletter."));

            string successfulSpecificEventType1 = "ok.eventType1";
            string successfulSpecificEventType2 = "ok.eventType2";
            string successfulCommonEventType = "ok.*";

            string deadletterSpecificEventType1 = "deadletter.eventType1";
            string deadletterSpecificEventType2 = "deadletter.eventType2";
            string deadletterCommonEventType = "deadletter.*";

            IDictionary<string, ISubscription> registeredSubscriptions = new Dictionary<string, ISubscription>();

            var mockEventType1SuccessfulSubscription = new Mock<ISubscription>();
            var mockEventType2SuccessfulSubscription = new Mock<ISubscription>();
            var mockEventType1FailedSubscription = new Mock<ISubscription>();
            var mockEventType2FailedSubscription = new Mock<ISubscription>();
            var mockEventTypeCommonFailedSubscription = new Mock<ISubscription>();
            var mockEventTypeCommonSuccessfulSubscription = new Mock<ISubscription>();

            registeredSubscriptions.Add(successfulSpecificEventType1, mockEventType1SuccessfulSubscription.Object);
            registeredSubscriptions.Add(successfulSpecificEventType2, mockEventType2SuccessfulSubscription.Object);

            registeredSubscriptions.Add(deadletterSpecificEventType1, mockEventType1FailedSubscription.Object);
            registeredSubscriptions.Add(deadletterSpecificEventType2, mockEventType2FailedSubscription.Object);

            registeredSubscriptions.Add(successfulCommonEventType, mockEventTypeCommonSuccessfulSubscription.Object);
            registeredSubscriptions.Add(deadletterCommonEventType, mockEventTypeCommonFailedSubscription.Object);

            //act
            ISubscriptionSelector subscriptionSelector = new TrackingEventsSubscriptionSelector(mockModelNamingConventionController.Object);
            ISubscription expectedEventType1SuccessfulSubscription = subscriptionSelector.Select(registeredSubscriptions, successfulSpecificEventType1);
            ISubscription expectedEventType2FailedSubscription = subscriptionSelector.Select(registeredSubscriptions, deadletterSpecificEventType2);
            ISubscription expectedSuccessfulCommonSubscription = subscriptionSelector.Select(registeredSubscriptions, "ok.uknownsuccessultype");
            ISubscription expectedFailedCommonSubscription = subscriptionSelector.Select(registeredSubscriptions, "deadletter.uknownsuccessultype");
            ISubscription expectedUnknownTypeSubscription = subscriptionSelector.Select(registeredSubscriptions, "unknowneventtype");

            expectedEventType1SuccessfulSubscription.Should().Be(mockEventType1SuccessfulSubscription.Object);
            expectedEventType2FailedSubscription.Should().Be(mockEventType2FailedSubscription.Object);
            expectedSuccessfulCommonSubscription.Should().Be(mockEventTypeCommonSuccessfulSubscription.Object);
            expectedFailedCommonSubscription.Should().Be(mockEventTypeCommonFailedSubscription.Object);
            expectedUnknownTypeSubscription.Should().BeNull();

        }

        [Fact]
        public void Select_DefaultSubscriptionsAreNotRegisteredDifferentScenatios_AppropriateSubscriptionOrNullIsReturned()
        {
            //prepare
            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();

            mockModelNamingConventionController.Setup(x => x.GetTrackingMessageEventType("*"))
                .Returns((string eventType) => string.Concat("ok.", eventType));
            mockModelNamingConventionController.Setup(x => x.GetDeadLetterQueueMessageEventType("*"))
                .Returns((string eventType) => string.Concat("deadletter.", eventType));

            mockModelNamingConventionController.Setup(x => x.IsTrackingMessageEventType(It.IsAny<string>()))
                .Returns((string eventType) => eventType.StartsWith("ok."));

            mockModelNamingConventionController.Setup(x => x.IsDeadLetterQueueMessageEventType(It.IsAny<string>()))
                .Returns((string eventType) => eventType.StartsWith("deadletter."));

            string successfulSpecificEventType1 = "ok.eventType1";
            string successfulSpecificEventType2 = "ok.eventType2";

            string deadletterSpecificEventType1 = "deadletter.eventType1";
            string deadletterSpecificEventType2 = "deadletter.eventType2";

            IDictionary<string, ISubscription> registeredSubscriptions = new Dictionary<string, ISubscription>();

            var mockEventType1SuccessfulSubscription = new Mock<ISubscription>();
            var mockEventType2SuccessfulSubscription = new Mock<ISubscription>();
            var mockEventType1FailedSubscription = new Mock<ISubscription>();
            var mockEventType2FailedSubscription = new Mock<ISubscription>();

            registeredSubscriptions.Add(successfulSpecificEventType1, mockEventType1SuccessfulSubscription.Object);
            registeredSubscriptions.Add(successfulSpecificEventType2, mockEventType2SuccessfulSubscription.Object);

            registeredSubscriptions.Add(deadletterSpecificEventType1, mockEventType1FailedSubscription.Object);
            registeredSubscriptions.Add(deadletterSpecificEventType2, mockEventType2FailedSubscription.Object);

            //act
            ISubscriptionSelector subscriptionSelector = new TrackingEventsSubscriptionSelector(mockModelNamingConventionController.Object);
            ISubscription expectedEventType1SuccessfulSubscription = subscriptionSelector.Select(registeredSubscriptions, successfulSpecificEventType1);
            ISubscription expectedEventType2FailedSubscription = subscriptionSelector.Select(registeredSubscriptions, deadletterSpecificEventType2);
            ISubscription expectedSuccessfulCommonSubscription = subscriptionSelector.Select(registeredSubscriptions, "ok.uknownsuccessultype");
            ISubscription expectedFailedCommonSubscription = subscriptionSelector.Select(registeredSubscriptions, "deadletter.uknownsuccessultype");
            ISubscription expectedUnknownTypeSubscription = subscriptionSelector.Select(registeredSubscriptions, "unknowneventtype");

            expectedEventType1SuccessfulSubscription.Should().Be(mockEventType1SuccessfulSubscription.Object);
            expectedEventType2FailedSubscription.Should().Be(mockEventType2FailedSubscription.Object);
            expectedSuccessfulCommonSubscription.Should().BeNull();
            expectedFailedCommonSubscription.Should().BeNull();
            expectedUnknownTypeSubscription.Should().BeNull();

        }
    }
}
