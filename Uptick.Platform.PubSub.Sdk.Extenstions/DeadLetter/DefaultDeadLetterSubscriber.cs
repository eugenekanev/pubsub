using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter
{
    public class DefaultDeadLetterSubscriber : ISubscriber
    {
        private readonly ISubscriber _subscriber;
        private readonly IModelNamingConventionController _modelNamingConventionController;
        private readonly IPublisher _publisher;
        private readonly string _targetSubscriberDeadLetterQueueName;
        public DefaultDeadLetterSubscriber(ISubscriber subscriber,
            IModelNamingConventionController modelNamingConventionController,
            IPublisher publisher, string targetSubscriberDeadLetterQueueName)
        {
            _subscriber = subscriber;
            _modelNamingConventionController = modelNamingConventionController;
            _publisher = publisher;
            _targetSubscriberDeadLetterQueueName = targetSubscriberDeadLetterQueueName;
        }

        public void Dispose()
        {
            _subscriber.Dispose();
        }

        public void Subscribe(IDictionary<string, ISubscription> deadletterSubscriptions)
        {
            //We have to look through all the subscriptions and change the event types according the conversion
            IDictionary<string, ISubscription> finalSubscriptionDictionary = new Dictionary<string, ISubscription>();

            foreach (var deadletterSubscription in deadletterSubscriptions)
            {
                finalSubscriptionDictionary.Add(
                    _modelNamingConventionController.GetDeadLetterQueueMessageEventType(deadletterSubscription.Key), 
                    new DeadLetterSubscriptionWrapper(deadletterSubscription.Value, _publisher, _targetSubscriberDeadLetterQueueName));
            }

            _subscriber.Subscribe(finalSubscriptionDictionary);
        }

        public void UnSubscribe()
        {
            _subscriber.UnSubscribe();
        }


    }
}
