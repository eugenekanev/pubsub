using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tracker
{
    public class DefaultTrackerNamingConventionController : ITrackerNamingConventionController
    {

        private readonly IModelNamingConventionController _trackerNamingConventionController;
        public DefaultTrackerNamingConventionController(IModelNamingConventionController modelNamingConventionController)
        {
            _trackerNamingConventionController = modelNamingConventionController;
        }

        public string GetTrackerQueueName(string trackername)
        {
            return string.Concat("tracking.", trackername);
        }

        public IEnumerable<string> GetTrackerQueueRoutingKeys(IEnumerable<string> targetSubscriberNames)
        {
            List<string> result = new List<string>();

            foreach (var subscriber in targetSubscriberNames)
            {
                result.Add(_trackerNamingConventionController.GetDeadLetterQueueRoutingKey(subscriber));
                result.Add(_trackerNamingConventionController.GetTrackingRoutingKey(subscriber));
            }

            return result;
        }
    }
}
