using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tracker
{
    public interface ITrackerFactory
    {
        Task<ITracker> CreateTrackerAsync(string name, bool temporary, IEnumerable<string> targetSubscriberNames, int retrycount, int prefetchcount);
    }
}
