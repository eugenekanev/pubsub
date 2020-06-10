using System;
using EasyNetQ;
using EasyNetQ.Topology;
using FluentAssertions;
using Moq;
using Uptick.Platform.PubSub.Sdk.RabbitMQ;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Management.RabbitMQ.Tests
{
    public class EasyNetQQueueInfoProviderTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetQueueInfo_queuealreadyexists_returnPredifinedMessageCount(bool temporary)
        {
            //prepare

            var mockBus = new Mock<IAdvancedBus>();
            var mockEnvironmentNamingConventionController = new Mock<IEnvironmentNamingConventionController>();
            var mockBroker = new Mock<IBroker>();
            var mockQueue = new Mock<IQueue>();
            var subscriberName = "activitytagger";
            var envName = "staging";
            uint expectedCountOfMessages = 10;

            mockBroker.SetupGet(x => x.Bus).Returns(mockBus.Object);
            mockEnvironmentNamingConventionController.Setup(x => x.GetQueueName(subscriberName))
                .Returns((string subscriber) => string.Concat(envName, ".", subscriber));

            mockBus.Setup(x => x.QueueDeclare(string.Concat(envName, ".", subscriberName), true, true,
                    temporary, false, null, null, null, null, null, null, null))
                .Returns((string name, bool passive, bool durable, bool exclusive,
                        bool autoDelete, int perQueueMessageTtl, int expires, int maxPriority,
                        string deadLetterExchange,
                        string deadLetterRoutingKey, int maxLength, int maxLengthBytes) => mockQueue.Object);

            mockBus.Setup(x => x.MessageCount(mockQueue.Object))
                .Returns((IQueue targetQueue) => expectedCountOfMessages);

            //act
            EasyNetQQueueInfoProvider easyNetQQueueInfoProvider = new EasyNetQQueueInfoProvider(mockEnvironmentNamingConventionController.Object, mockBroker.Object);
            QueueInfoSnapshot queueInfoSnapshot = easyNetQQueueInfoProvider.GetQueueInfo(subscriberName, temporary);

            //check
            queueInfoSnapshot.CountOfMessages.Should().Be(expectedCountOfMessages);
        }
    }
}
