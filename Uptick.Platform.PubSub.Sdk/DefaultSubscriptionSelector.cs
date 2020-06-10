using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk
{
    public class DefaultSubscriptionSelector : ISubscriptionSelector
    {
        public ISubscription Select(IDictionary<string, ISubscription> registeredSubscriptions, string messageEventType)
        {
            if (registeredSubscriptions.TryGetValue(messageEventType,
                out var subscription))
            {
                return subscription;
            }
            else if (registeredSubscriptions.TryGetValue("*", out var defaultSubscription))
            {
                return defaultSubscription;
            }

            return null;
        }
    }
}
