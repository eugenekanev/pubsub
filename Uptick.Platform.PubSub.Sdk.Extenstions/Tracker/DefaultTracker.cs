using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tracker
{
    public class DefaultTracker : ITracker
    {
        private readonly ISubscriber _subscriber;
        private readonly IModelNamingConventionController _modelNamingConventionController;
        public DefaultTracker(ISubscriber subscriber, IModelNamingConventionController modelNamingConventionController)
        {
            _subscriber = subscriber;
            _modelNamingConventionController = modelNamingConventionController;
        }

        public void Dispose()
        {
            _subscriber.Dispose();
        }

        public void Subscribe(IDictionary<string, ISubscription> succesfulMessagesSubscriptions,
            IDictionary<string, ISubscription> failedMessagesSubscriptions)
        {
            //We have to look through all the subscriptions and change the event types according the conversion
            IDictionary<string, ISubscription> finalSubscriptionDictionary = new Dictionary<string, ISubscription>();

            foreach (var failedMessagesSubscription in failedMessagesSubscriptions)
            {
                finalSubscriptionDictionary.Add(
                    _modelNamingConventionController.GetDeadLetterQueueMessageEventType(failedMessagesSubscription.Key), failedMessagesSubscription.Value);
            }

            foreach (var succesfulMessagesSubscription in succesfulMessagesSubscriptions)
            {
                finalSubscriptionDictionary.Add(
                    _modelNamingConventionController.GetTrackingMessageEventType(succesfulMessagesSubscription.Key), succesfulMessagesSubscription.Value);
            }

            _subscriber.Subscribe(finalSubscriptionDictionary);
        }

        public void UnSubscribe()
        {
            _subscriber.UnSubscribe();
        }

    }
}
