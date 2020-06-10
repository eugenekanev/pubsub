using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter
{
    public class DeadLetterEventsSubscriptionSelector : ISubscriptionSelector
    {
        private readonly IModelNamingConventionController _modelNamingConventionController;
        private readonly string _defaultDeadLetterQueueMessageEventType;
        public DeadLetterEventsSubscriptionSelector(IModelNamingConventionController modelNamingConventionController)
        {
            _modelNamingConventionController = modelNamingConventionController;
            _defaultDeadLetterQueueMessageEventType =
                _modelNamingConventionController.GetDeadLetterQueueMessageEventType("*");
        }
        public ISubscription Select(IDictionary<string, ISubscription> registeredSubscriptions, string messageEventType)
        {
            if (registeredSubscriptions.TryGetValue(messageEventType,
                out var subscription))
            {
                return subscription;
            }

            if (registeredSubscriptions.TryGetValue(_defaultDeadLetterQueueMessageEventType, out var defaultFailedProcessingSubscription)
                && _modelNamingConventionController.IsDeadLetterQueueMessageEventType(messageEventType))
            {
                return defaultFailedProcessingSubscription;
            }

            return null;
        }
    }
}
