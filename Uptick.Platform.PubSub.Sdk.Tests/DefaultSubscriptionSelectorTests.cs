using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Moq;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Tests
{
    public class DefaultSubscriptionSelectorTests
    {
        [Fact]
        public void Select_ProvidedEventTypeIsRegistered_ReturnAppropriateSubscription()
        {
            //prepare

            var defaultSubscriptionSelector = new DefaultSubscriptionSelector();
            var eventType1Subscription = new Mock<ISubscription>();
            var eventType2Subscription = new Mock<ISubscription>();
            var eventType1 = "eventType1";
            var eventType2 = "eventType2";
            IDictionary<string,ISubscription> registeredSubscriptions = new Dictionary<string, ISubscription>();
            registeredSubscriptions.Add(eventType1, eventType1Subscription.Object);
            registeredSubscriptions.Add(eventType2, eventType2Subscription.Object);

            //act
            var actualEventType = eventType2;
            var actualISubscription = defaultSubscriptionSelector.Select(registeredSubscriptions, actualEventType);


            //check
            actualISubscription.Should().Be(eventType2Subscription.Object);
        }

        [Fact]
        public void Select_ProvidedEventTypeIsNotRegisteredButDefaultExists_ReturnDefaultSubscription()
        {
            //prepare

            var defaultSubscriptionSelector = new DefaultSubscriptionSelector();
            var eventType1Subscription = new Mock<ISubscription>();
            var eventType2Subscription = new Mock<ISubscription>();
            var defaultSubscription = new Mock<ISubscription>();
            var eventType1 = "eventType1";
            var eventType2 = "eventType2";
            var defaultEventType = "*";
            IDictionary<string, ISubscription> registeredSubscriptions = new Dictionary<string, ISubscription>();
            registeredSubscriptions.Add(eventType1, eventType1Subscription.Object);
            registeredSubscriptions.Add(eventType2, eventType2Subscription.Object);
            registeredSubscriptions.Add(defaultEventType, defaultSubscription.Object);

            //act
            var actualEventType = "eventType3";
            var actualISubscription = defaultSubscriptionSelector.Select(registeredSubscriptions, actualEventType);


            //check
            actualISubscription.Should().Be(defaultSubscription.Object);
        }

        [Fact]
        public void Select_ProvidedEventTypeIsNotRegisteredAndDefaultDoesNotExist_ReturnNull()
        {
            //prepare

            var defaultSubscriptionSelector = new DefaultSubscriptionSelector();
            var eventType1Subscription = new Mock<ISubscription>();
            var eventType2Subscription = new Mock<ISubscription>();
            var eventType1 = "eventType1";
            var eventType2 = "eventType2";
            IDictionary<string, ISubscription> registeredSubscriptions = new Dictionary<string, ISubscription>();
            registeredSubscriptions.Add(eventType1, eventType1Subscription.Object);
            registeredSubscriptions.Add(eventType2, eventType2Subscription.Object);

            //act
            var actualEventType = "eventType3";
            var actualISubscription = defaultSubscriptionSelector.Select(registeredSubscriptions, actualEventType);


            //check
            actualISubscription.Should().BeNull();
        }
    }
}
