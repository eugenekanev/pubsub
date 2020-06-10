using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter
{
    public interface IDeadLetterSubscriberFactory
    {
        Task<ISubscriber> CreateSubscriberAsync(string targetSubscriber, int prefetchcount, bool temporary = false);
    }
}
