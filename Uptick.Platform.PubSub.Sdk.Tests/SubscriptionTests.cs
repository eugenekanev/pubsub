using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using SemanticComparison.Fluent;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Tests
{
    public class SubscriptionTests
    {
        [Fact]
        public async void InvokeAsync_CorrectSerializedMessageIsProvided_NewConsumerInstanceIsCreatedAndUseed()
        {

            //prepare
            var mockConsumer1 = new Mock<IConsumer<BookingCreated>>();
            var mockConsumer2 = new Mock<IConsumer<BookingCreated>>();

            int count = 0;
            Func<IConsumer<BookingCreated>> consumerFactory = () =>
            {
                if (count == 0)
                {
                    count = count + 1;
                    return mockConsumer1.Object;
                }
                else
                {
                    return mockConsumer2.Object;
                }
            };

            ISubscription subscription = new Subscription<BookingCreated>(consumerFactory,null);

            IntegrationEvent< BookingCreated> integrationEvent = new IntegrationEvent<BookingCreated>(new BookingCreated{BookingName = "bookingName"}, "bookingcreatedeventtype");

            string serializedMessage = JsonConvert.SerializeObject(integrationEvent);

            //act
            await subscription.InvokeAsync(serializedMessage);
            await subscription.InvokeAsync(serializedMessage);

            //check
            var expected = integrationEvent.AsSource().OfLikeness<IntegrationEvent<BookingCreated>>()
                .With(a => a.Content)
                .EqualsWhen((p, m) => { return m.Content.BookingName == p.Content.BookingName; }).CreateProxy();
              
            mockConsumer1.Verify(x => x.ConsumeAsync(expected), Times.Once);
            mockConsumer2.Verify(x => x.ConsumeAsync(expected), Times.Once);

        }

        [Fact]
        public async void NotifyAboutDeadLetterAsync_NotNullDeadLetterCallbackFuncIsProvided_CallbackIsInvoked()
        {

            //prepare

            IntegrationEvent<BookingCreated> expectedFirstIntegrationEvent = null;
            Exception expectedFirstException = null;
            IntegrationEvent<BookingCreated> expectedSecondIntegrationEvent = null;
            Exception expectedSecondException = null;
            int count = 0;

            Func<IntegrationEvent<BookingCreated>, Exception, Task> deadLetterCallbackFunc = (bookingCreatedEvent, exception) =>
            {
                if (count == 0)
                {
                    expectedFirstIntegrationEvent = bookingCreatedEvent;
                    expectedFirstException = exception;
                    count = count + 1;
                }
                else
                {
                    expectedSecondIntegrationEvent = bookingCreatedEvent;
                    expectedSecondException = exception;
                }

                return Task.CompletedTask;
            };

            ISubscription subscription = new Subscription<BookingCreated>(null, deadLetterCallbackFunc);

            IntegrationEvent<BookingCreated> shouldRaiseArgumentNullExceptionIntegrationEvent =
                new IntegrationEvent<BookingCreated>(new BookingCreated { BookingName = "Coca Cola" }, "bookingcreatedeventtype");

            IntegrationEvent<BookingCreated> shouldRaiseInvalidOperationExceptionIntegrationEvent =
                new IntegrationEvent<BookingCreated>(new BookingCreated { BookingName = "Apple" }, "bookingcreatedeventtype");

            string serializedShouldRaiseArgumentNullExceptionIntegrationEvent = JsonConvert.SerializeObject(shouldRaiseArgumentNullExceptionIntegrationEvent);
            string serializedShouldRaiseInvalidOperationExceptionIntegrationEvent = JsonConvert.SerializeObject(shouldRaiseInvalidOperationExceptionIntegrationEvent);

            ArgumentNullException argumentNullException = new ArgumentNullException("ArgumentNullException");
            InvalidOperationException invalidOperationException = new InvalidOperationException("InvalidOperationException");

            //act
            await subscription.NotifyAboutDeadLetterAsync(serializedShouldRaiseArgumentNullExceptionIntegrationEvent, argumentNullException);
            await subscription.NotifyAboutDeadLetterAsync(serializedShouldRaiseInvalidOperationExceptionIntegrationEvent, invalidOperationException);

            //check
            expectedFirstException.Should().Be(argumentNullException);
            expectedSecondException.Should().Be(invalidOperationException);
            expectedFirstIntegrationEvent.ShouldBeEquivalentTo(shouldRaiseArgumentNullExceptionIntegrationEvent);
            expectedSecondIntegrationEvent.ShouldBeEquivalentTo(shouldRaiseInvalidOperationExceptionIntegrationEvent);
        }


        #region helper

        public class BookingCreated
        {
            public string BookingName { get; set; }
        }
        #endregion
    }
}
