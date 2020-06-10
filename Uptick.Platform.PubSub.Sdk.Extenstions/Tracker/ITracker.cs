using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tracker
{
    public interface ITracker : IDisposable
    {
        void Subscribe(IDictionary<string, ISubscription> succesfulMessagesSubscriptions,
            IDictionary<string, ISubscription> failedMessagesSubscriptions);

        void UnSubscribe();

    }
}
