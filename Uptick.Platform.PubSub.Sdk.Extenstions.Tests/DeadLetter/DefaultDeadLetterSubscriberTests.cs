using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.DeadLetter
{
    public class DefaultDeadLetterSubscriberTests
    {
        [Fact]
        public void Dispose_Decorator_InternalSubscriberDisposed()
        {
            //prepare
            var subscriberMock = new Mock<ISubscriber>();

            //act
            DefaultDeadLetterSubscriber defaultDeadLetterSubscriber = new DefaultDeadLetterSubscriber(subscriberMock.Object, null, null, null);
            defaultDeadLetterSubscriber.Dispose();


            //check
            subscriberMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void UnSubscribe_Decorator_InternalSubscriberUnSubscribed()
        {
            //prepare
            var subscriberMock = new Mock<ISubscriber>();

            //act
            DefaultDeadLetterSubscriber defaultDeadLetterSubscriber = new DefaultDeadLetterSubscriber(subscriberMock.Object, null, null, null);
            defaultDeadLetterSubscriber.UnSubscribe();


            //check
            subscriberMock.Verify(x => x.UnSubscribe(), Times.Once);
        }

        [Fact]
        public void Subscribe_SubscriptionAreProvided_EventTypesAreChangedAndSubscriptionAreWrapped()
        {
            //prepare
            var subscriberMock = new Mock<ISubscriber>();
            var modelNamingConventionControllerMock = new Mock<IModelNamingConventionController>();
            var publisherMock = new Mock<IPublisher>();
            var targetSubscriberDeadLetterQueueName = "deadletter.subscriber";

            IDictionary<string, ISubscription> originalSubscriptions = new Dictionary<string, ISubscription>();
            var activityCreatedSubscription = new Mock<ISubscription>();
            var bookingCreatedSubscription = new Mock<ISubscription>();
            var activityCreatedEventType = "activity.created";
            var bookingCreatedEventType = "booking.created";
            originalSubscriptions.Add(activityCreatedEventType, activityCreatedSubscription.Object);
            originalSubscriptions.Add(bookingCreatedEventType, bookingCreatedSubscription.Object);

            IDictionary<string, ISubscription> actualSubscriptions = null;
            
            subscriberMock.Setup(x => x.Subscribe(It.IsAny<IDictionary<string, ISubscription >>()))
                .Callback<IDictionary<string, ISubscription>>(
                    (subscriptions) => { actualSubscriptions = subscriptions; });

            modelNamingConventionControllerMock.Setup(x => x.GetDeadLetterQueueMessageEventType(It.IsAny<string>()))
                .Returns<string> (
                (eventType) =>
                {
                    return string.Concat("deadletter.", eventType);
                });


            //act
            DefaultDeadLetterSubscriber defaultDeadLetterSubscriber = new DefaultDeadLetterSubscriber(subscriberMock.Object, 
                modelNamingConventionControllerMock.Object, publisherMock.Object, targetSubscriberDeadLetterQueueName);
            defaultDeadLetterSubscriber.Subscribe(originalSubscriptions);


            //check
            foreach (var originalSubscription in originalSubscriptions)
            {
               DeadLetterSubscriptionWrapper wrapper = 
                   (DeadLetterSubscriptionWrapper)actualSubscriptions[string.Concat("deadletter.", originalSubscription.Key)];
                wrapper.Should().NotBeNull();

            }
        }
    }
}
