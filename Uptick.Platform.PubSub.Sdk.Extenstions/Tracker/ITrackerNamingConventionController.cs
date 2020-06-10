using System.Collections.Generic;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tracker
{
    public interface ITrackerNamingConventionController
    {
        string GetTrackerQueueName(string trackername);

        IEnumerable<string> GetTrackerQueueRoutingKeys(IEnumerable<string> targetSubscriberNames);
    }
}
