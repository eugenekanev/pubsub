using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tracker
{
    public class DefaultTrackerFactory : ITrackerFactory
    {
        private readonly ISubscriberFactory _subscriberFactory;
        private readonly ITrackerNamingConventionController _trackerNamingConventionController;
        private readonly IModelNamingConventionController _modelNamingConventionController;
        public DefaultTrackerFactory(ISubscriberFactory subscriberFactory,
            ITrackerNamingConventionController trackerNamingConventionController,
            IModelNamingConventionController modelNamingConventionController)
        {
            _subscriberFactory = subscriberFactory;
            _trackerNamingConventionController = trackerNamingConventionController;
            _modelNamingConventionController = modelNamingConventionController;
        }


        public async Task<ITracker> CreateTrackerAsync(string name, bool temporary, IEnumerable<string> targetSubscriberNames, int retrycount, int prefetchcount)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name argument can not be null or empty");
            }

            if (targetSubscriberNames == null || !targetSubscriberNames.Any())
            {
                throw new ArgumentException("targetSubscriberNames argument can not be null or empty");
            }

            string trackingQueueName = _trackerNamingConventionController.GetTrackerQueueName(name);
            IEnumerable<string> trackerQueueRoutingKeys = _trackerNamingConventionController.GetTrackerQueueRoutingKeys(targetSubscriberNames);

            ISubscriber trackerQueueSubscriber =
                await _subscriberFactory.CreateSubscriberAsync(trackingQueueName, temporary, trackerQueueRoutingKeys,
                    retrycount, prefetchcount, new TrackingEventsSubscriptionSelector(_modelNamingConventionController));

            return new DefaultTracker(trackerQueueSubscriber, _modelNamingConventionController);
        }
    }
}
