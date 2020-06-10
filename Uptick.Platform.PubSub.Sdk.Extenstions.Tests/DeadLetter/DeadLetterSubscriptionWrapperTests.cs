using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Exceptions;
using Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.DeadLetter
{
    public class DeadLetterSubscriptionWrapperTests
    {
        private readonly string _serializedMessage = "{'EventId': '37dca264-fa1e-11e8-84d2-0a586460090e', 'CorrelationId': '1812ec24-9e57-4c97-85f3-a9318d340326', 'EventCreationDate': '2018-12-07T12:47:12.823640Z', " +
                                                     "'EventType': 'deadletter.ActivityParticipants.create', 'Version': '0.1', 'Content': {'CountOfAttempts': 10, \"LastAttemptError\": \"'NoneType' object is not subscriptable\", " +
                                                     "'Original': {'CreatedDate': '2018-12-07T12:44:26.119113', 'Entity': 'ActivityParticipants', 'Operation': 'create', 'Fields': {'new': {'sys_period': '[\"2018-12-07 00:00:00\",)', " +
                                                     "'id': 'c2a902b1-8058-4310-bf44-01a70b5f22f5', 'role': 'from', 'status': 'Ramp', 'ownerId': 'f23e153a-74ce-4f85-a9fc-8b5d9abcc135', 'createdById': '3890075d-6d00-47b7-bbf8-9196dea6c5d8'," +
                                                     " 'activityCreatedDate': '2018-06-12T16:03:55', 'lastModifiedById': '05b1ec71-d878-40ec-bcc5-2b5ee4dfbc77', 'createdDate': '2018-01-02T00:00:00', 'lastModifiedDate': '2018-12-07T00:00:00', " +
                                                     "'isDeleted': false, 'activityId': 'f7847c46-8078-438e-af52-63e9b306d5c6', 'personId': '1f59ca1d-e57d-4dbd-bb7e-05b565f4886a'}}}}}";
        [Fact]
        public async Task NotifyAboutDeadLetterAsync_DecoratorMethod_OriginalSubscriptionNotifyIsCalled()
        {
            //prepare
            var originalSubscriptionMock = new Mock<ISubscription>();
            var publisherMock = new Mock<IPublisher>();
            Exception argException = new ArgumentException();
            string message = "justMessage";

            //Act
            DeadLetterSubscriptionWrapper actor = new DeadLetterSubscriptionWrapper(originalSubscriptionMock.Object, publisherMock.Object, null);
            await actor.NotifyAboutDeadLetterAsync(message, argException);

            //check
            originalSubscriptionMock.Verify(x => x.NotifyAboutDeadLetterAsync(message, argException), Times.Once);
            publisherMock.Verify(x => x.PublishDirectEventAsync(It.IsAny<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(), 
                It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

        }


        [Theory]
        [InlineData(typeof(MalformedEventException))]
        [InlineData(typeof(DiscardEventException))]
        public void InvokeAsync_SpecialExceptionIsThrown_ExceptionShouldBePropogated(Type exceptionType)
        {
            //prepare
            var originalSubscriptionMock = new Mock<ISubscription>();
            var publisherMock = new Mock<IPublisher>();
            string message = "justMessage";

            originalSubscriptionMock.Setup(x=>x.InvokeAsync(message)).Returns<string>(
                (msg) =>
                {
                    return Task.FromException((Exception)Activator.CreateInstance(exceptionType));
                });

            //act
            DeadLetterSubscriptionWrapper actor = new DeadLetterSubscriptionWrapper(originalSubscriptionMock.Object, publisherMock.Object, null);
            Func<Task> f = async () =>
            {
                await actor.InvokeAsync(message);
            };

            //check
            f.ShouldThrow<Exception>();
            originalSubscriptionMock.Verify(x => x.NotifyAboutDeadLetterAsync(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
            publisherMock.Verify(x => x.PublishDirectEventAsync(It.IsAny<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(),
                It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

        }

        [Fact]
        public async Task InvokeAsync_NonSpecialExceptionIsThrown_OriginalMessageShouldBeSentToTheDeadLetterQueue()
        {
            //prepare
            var originalSubscriptionMock = new Mock<ISubscription>();
            var publisherMock = new Mock<IPublisher>();
            string subscriberDeadLetterQueueName = "deadletter.subscriber";
            var argumentException  = new ArgumentException();

            originalSubscriptionMock.Setup(x => x.InvokeAsync(_serializedMessage)).Returns<string>(
                (msg) =>
                {
                    return Task.FromException(argumentException);
                });

            //act
            DeadLetterSubscriptionWrapper actor = new DeadLetterSubscriptionWrapper(originalSubscriptionMock.Object, 
                publisherMock.Object, subscriberDeadLetterQueueName);

            await actor.InvokeAsync(_serializedMessage);

            //check
            originalSubscriptionMock.Verify(x => x.NotifyAboutDeadLetterAsync(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
            IntegrationEvent<DeadLetterEventDescriptor<JObject>> integrationEvent =
                JsonConvert.DeserializeObject<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(_serializedMessage);

            publisherMock.Verify(x => x.PublishDirectEventAsync(It.Is<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(actualevent=>actualevent.CorrelationId == integrationEvent.CorrelationId) ,
                subscriberDeadLetterQueueName, true), Times.Once);
        }
    }
}
