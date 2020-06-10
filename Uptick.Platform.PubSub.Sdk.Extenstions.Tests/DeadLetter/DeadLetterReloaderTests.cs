using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.DeadLetter
{
    public class DeadLetterReloaderTests
    {
        private readonly string _serializedMessage = "{'EventId': '37dca264-fa1e-11e8-84d2-0a586460090e', 'CorrelationId': '1812ec24-9e57-4c97-85f3-a9318d340326', 'EventCreationDate': '2018-12-07T12:47:12.823640Z', " +
                                                     "'EventType': 'deadletter.ActivityParticipants.create', 'Version': '0.1', 'Content': {'CountOfAttempts': 10, \"LastAttemptError\": \"'NoneType' object is not subscriptable\", " +
                                                     "'Original': {'CreatedDate': '2018-12-07T12:44:26.119113', 'Entity': 'ActivityParticipants', 'Operation': 'create', 'Fields': {'new': {'sys_period': '[\"2018-12-07 00:00:00\",)', " +
                                                     "'id': 'c2a902b1-8058-4310-bf44-01a70b5f22f5', 'role': 'from', 'status': 'Ramp', 'ownerId': 'f23e153a-74ce-4f85-a9fc-8b5d9abcc135', 'createdById': '3890075d-6d00-47b7-bbf8-9196dea6c5d8'," +
                                                     " 'activityCreatedDate': '2018-06-12T16:03:55', 'lastModifiedById': '05b1ec71-d878-40ec-bcc5-2b5ee4dfbc77', 'createdDate': '2018-01-02T00:00:00', 'lastModifiedDate': '2018-12-07T00:00:00', " +
                                                     "'isDeleted': false, 'activityId': 'f7847c46-8078-438e-af52-63e9b306d5c6', 'personId': '1f59ca1d-e57d-4dbd-bb7e-05b565f4886a'}}}}}";
        [Fact]
        public async Task ConsumeAsync_DeadLetterMesssageIsProvided_OriginalMessageIsSentToSubscriberQueue()
        {
            //prepare
            var publisherMock = new Mock<IPublisher>();
            string subscriberQueueName = "subscriber";

            IntegrationEvent<DeadLetterEventDescriptor<JObject>> deadLetterIntegrationEvent =
                JsonConvert.DeserializeObject<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(_serializedMessage);

            IntegrationEvent<JObject> originalMessage = null;
            publisherMock.Setup(x => x.PublishDirectEventAsync(
                    It.IsAny<IntegrationEvent<JObject>>(), subscriberQueueName, true))
                .Returns<IntegrationEvent<JObject>, string, bool>(
                    (integrationEvent, subscriberName, persistant) =>
                    {
                        originalMessage = integrationEvent;
                        return Task.CompletedTask;
                    });

            var deadLetterReloader =
                new DeadLetterReloader<JObject>(publisherMock.Object, subscriberQueueName);
            //act
            await deadLetterReloader.ConsumeAsync(deadLetterIntegrationEvent);

            //check
            originalMessage.ShouldBeEquivalentTo(deadLetterIntegrationEvent.Content.Original);
        }
    }
}
