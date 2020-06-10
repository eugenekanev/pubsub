using System;
using System.Collections.Generic;

namespace Uptick.Platform.PubSub.Sdk
{
    public interface ISubscriber : IDisposable
    {
        void Subscribe(IDictionary<string, ISubscription> subscriptions);

        void UnSubscribe();

    }
}