using System;
using EasyNetQ;
using Uptick.Platform.PubSub.Sdk.RabbitMQ;

namespace Uptick.Platform.PubSub.Sdk.Management.RabbitMQ
{
    public class EasyNetQQueueInfoProvider : IQueueInfoProvider
    {
        private readonly IEnvironmentNamingConventionController _environmentNamingConventionController;
        private readonly IAdvancedBus _bus;
        public EasyNetQQueueInfoProvider(IEnvironmentNamingConventionController environmentNamingConventionController, IBroker broker)
        {
            _environmentNamingConventionController = environmentNamingConventionController;
            _bus = broker.Bus;
        }

        public QueueInfoSnapshot GetQueueInfo(string subscriberName, bool temporary)
        {
            var queueName = _environmentNamingConventionController.GetQueueName(subscriberName);
            var queue = _bus.QueueDeclare(queueName, true, true, temporary);
            var countOfMessages = _bus.MessageCount(queue);
            return new QueueInfoSnapshot{ CountOfMessages = countOfMessages};
        }
    }
}
