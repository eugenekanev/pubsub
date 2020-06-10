using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter
{
    public class DefaultDeadLetterSubscriberFactory : IDeadLetterSubscriberFactory
    {
        private readonly ISubscriberFactory _subscriberFactory;
        private readonly IModelNamingConventionController _modelNamingConventionController;
        private readonly IPublisher _publisher;

        public DefaultDeadLetterSubscriberFactory(ISubscriberFactory subscriberFactory, 
            IModelNamingConventionController modelNamingConventionController,
            IPublisher publisher)
        {
            _subscriberFactory = subscriberFactory;
            _modelNamingConventionController = modelNamingConventionController;
            _publisher = publisher;
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string targetSubscriber, int prefetchcount, bool temporary)
        {
            if (string.IsNullOrEmpty(targetSubscriber))
            {
                throw new ArgumentException("targetSubscriber argument can not be null or empty");
            }

            string targetSubscriberDeadLetterQueueName = _modelNamingConventionController.GetDeadLetterQueueName(targetSubscriber);

            ISubscriber deadLetterSubscriber =
                await _subscriberFactory.CreateSubscriberAsync(targetSubscriberDeadLetterQueueName, temporary, null,
                    0, prefetchcount, false, new DeadLetterEventsSubscriptionSelector(_modelNamingConventionController), false);

            return new DefaultDeadLetterSubscriber(deadLetterSubscriber, _modelNamingConventionController,
                _publisher, targetSubscriberDeadLetterQueueName);
        }

    }
}
