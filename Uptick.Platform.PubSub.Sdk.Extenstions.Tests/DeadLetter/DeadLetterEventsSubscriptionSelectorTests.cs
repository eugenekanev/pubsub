using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Moq;
using Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.DeadLetter
{
    public class DeadLetterEventsSubscriptionSelectorTests
    {
        [Fact]
        public void Select_DefaultSubscriptionsAreRegisteredDifferentScenatios_AppropriateSubscriptionOrNullIsReturned()
        {
            //prepare
            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();

            mockModelNamingConventionController.Setup(x => x.GetDeadLetterQueueMessageEventType("*"))
                .Returns((string eventType) => string.Concat("deadletter.", eventType));

            mockModelNamingConventionController.Setup(x => x.IsDeadLetterQueueMessageEventType(It.IsAny<string>()))
                .Returns((string eventType) => eventType.StartsWith("deadletter."));

            string deadletterSpecificEventType1 = "deadletter.eventType1";
            string deadletterSpecificEventType2 = "deadletter.eventType2";
            string deadletterCommonEventType = "deadletter.*";

            IDictionary<string, ISubscription> registeredSubscriptions = new Dictionary<string, ISubscription>();

            var mockEventType1FailedSubscription = new Mock<ISubscription>();
            var mockEventType2FailedSubscription = new Mock<ISubscription>();
            var mockEventTypeCommonFailedSubscription = new Mock<ISubscription>();

            registeredSubscriptions.Add(deadletterSpecificEventType1, mockEventType1FailedSubscription.Object);
            registeredSubscriptions.Add(deadletterSpecificEventType2, mockEventType2FailedSubscription.Object);

            registeredSubscriptions.Add(deadletterCommonEventType, mockEventTypeCommonFailedSubscription.Object);

            //act
            ISubscriptionSelector subscriptionSelector = new DeadLetterEventsSubscriptionSelector(mockModelNamingConventionController.Object);
            ISubscription expectedEventType2FailedSubscription = subscriptionSelector.Select(registeredSubscriptions, deadletterSpecificEventType2);
            ISubscription expectedFailedCommonSubscription = subscriptionSelector.Select(registeredSubscriptions, "deadletter.uknownsuccessultype");
            ISubscription expectedUnknownTypeSubscription = subscriptionSelector.Select(registeredSubscriptions, "unknowneventtype");

            expectedEventType2FailedSubscription.Should().Be(mockEventType2FailedSubscription.Object);
            expectedFailedCommonSubscription.Should().Be(mockEventTypeCommonFailedSubscription.Object);
            expectedUnknownTypeSubscription.Should().BeNull();

        }

        [Fact]
        public void Select_DefaultSubscriptionsAreNotRegisteredDifferentScenatios_AppropriateSubscriptionOrNullIsReturned()
        {
            //prepare
            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();

            mockModelNamingConventionController.Setup(x => x.GetDeadLetterQueueMessageEventType("*"))
                .Returns((string eventType) => string.Concat("deadletter.", eventType));

            mockModelNamingConventionController.Setup(x => x.IsDeadLetterQueueMessageEventType(It.IsAny<string>()))
                .Returns((string eventType) => eventType.StartsWith("deadletter."));


            string deadletterSpecificEventType1 = "deadletter.eventType1";
            string deadletterSpecificEventType2 = "deadletter.eventType2";

            IDictionary<string, ISubscription> registeredSubscriptions = new Dictionary<string, ISubscription>();

            var mockEventType1FailedSubscription = new Mock<ISubscription>();
            var mockEventType2FailedSubscription = new Mock<ISubscription>();

            registeredSubscriptions.Add(deadletterSpecificEventType1, mockEventType1FailedSubscription.Object);
            registeredSubscriptions.Add(deadletterSpecificEventType2, mockEventType2FailedSubscription.Object);

            //act
            ISubscriptionSelector subscriptionSelector = new DeadLetterEventsSubscriptionSelector(mockModelNamingConventionController.Object);
            ISubscription expectedEventType2FailedSubscription = subscriptionSelector.Select(registeredSubscriptions, deadletterSpecificEventType2);
            ISubscription expectedFailedCommonSubscription = subscriptionSelector.Select(registeredSubscriptions, "deadletter.uknownsuccessultype");
            ISubscription expectedUnknownTypeSubscription = subscriptionSelector.Select(registeredSubscriptions, "unknowneventtype");

            expectedEventType2FailedSubscription.Should().Be(mockEventType2FailedSubscription.Object);
            expectedFailedCommonSubscription.Should().BeNull();
            expectedUnknownTypeSubscription.Should().BeNull();

        }
    }
}
