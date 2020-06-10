using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk
{
    public interface ISubscriptionSelector
    {
        ISubscription Select(IDictionary<string, ISubscription> registeredSubscriptions, string messageEventType);
    }
}
