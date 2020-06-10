using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tracker
{
    public class TrackingEventsSubscriptionSelector : ISubscriptionSelector
    {
        private readonly IModelNamingConventionController _modelNamingConventionController;
        private readonly string _defaultTrackingMessageEventType;
        private readonly string _defaultDeadLetterQueueMessageEventType;
        public TrackingEventsSubscriptionSelector(IModelNamingConventionController modelNamingConventionController)
        {
            _modelNamingConventionController = modelNamingConventionController;
            _defaultTrackingMessageEventType = _modelNamingConventionController.GetTrackingMessageEventType("*");
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

            if (registeredSubscriptions.TryGetValue(_defaultTrackingMessageEventType, out var defaultSuccessfulProcessingSubscription) 
                     && _modelNamingConventionController.IsTrackingMessageEventType(messageEventType))
            {
                return defaultSuccessfulProcessingSubscription;
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
