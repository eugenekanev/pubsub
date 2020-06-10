using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter
{
    public interface IDeadLetterSubscriber
    {
        void Subscribe(IDictionary<string, ISubscription> deadletterSubscriptions);

        void UnSubscribe();
    }
}
