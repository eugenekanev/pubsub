using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Uptick.Platform.PubSub.Sdk.TestConsumer
{
    public class ChangeTrackerTableCreateRelationshipBpmConsumer : 
        IConsumer<RelationshipCreated>, IConsumer<JObject>
    {
        ILogger logger = Log.ForContext<ChangeTrackerTableCreateRelationshipBpmConsumer>();
        public async Task ConsumeAsync(IntegrationEvent<RelationshipCreated> integrationEvent)
        {
           logger.Debug($"New Message of type {integrationEvent.EventType} " +
                             $"has been recieved through typed interface");
           logger.Debug(integrationEvent.ToString());
           logger.Debug($"RelationshipId is {integrationEvent.Content.RelationshipId}");
           await Task.Delay(10000);
        }

        public async Task ConsumeAsync(IntegrationEvent<JObject> integrationEvent)
        {
            logger.Debug($"New Message of type {integrationEvent.EventType} " +
                              $"has been recieved through dynamic interface");
            logger.Debug(integrationEvent.ToString());
            logger.Debug($"RelationshipId is { integrationEvent.Content["RelationshipId"].Value<string>()}");
            await Task.Delay(10000);
        }
    }
}
