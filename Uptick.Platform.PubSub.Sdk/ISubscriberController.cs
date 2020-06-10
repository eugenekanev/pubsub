using System;

namespace Uptick.Platform.PubSub.Sdk
{
    public interface ISubscriberController
    {
        bool RegisterSubscriberName(string name, out IDisposable nameHandle);
    }
}
