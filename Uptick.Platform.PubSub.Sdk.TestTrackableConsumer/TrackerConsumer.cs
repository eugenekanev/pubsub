using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;
using Uptick.Platform.PubSub.Sdk.RabbitMQ;

namespace Uptick.Platform.PubSub.Sdk.TestTrackableConsumer
{
    public class TrackerConsumer : IConsumer<string>, IConsumer<DeadLetterEventDescriptor<EmailCreated>>, 
        IConsumer<DeadLetterEventDescriptor<ActivityCreated>>
    {
        ILogger logger = Log.ForContext<ActivityGeneratorConsumer>();
        public async Task ConsumeAsync(IntegrationEvent<string> messagEvent)
        {
            logger.Debug($"TRACKER. The original event with correlation id {messagEvent.CorrelationId} has been processed. Event Type is {messagEvent.EventType}");
            await Task.Delay(100);
        }

        public async Task ConsumeAsync(IntegrationEvent<DeadLetterEventDescriptor<EmailCreated>> integrationEvent)
        {
            logger.Debug($"TRACKER. EmailCreated DeadLetter Tracker. The original event with correlation id {integrationEvent.CorrelationId} has failed. Event Type is {integrationEvent.EventType}");
            await Task.Delay(100);
        }

        public async Task ConsumeAsync(IntegrationEvent<DeadLetterEventDescriptor<ActivityCreated>> integrationEvent)
        {
            logger.Debug($"TRACKER. ActivityCreated DeadLetter Tracker. The original event with correlation id {integrationEvent.CorrelationId} has failed. Event Type is {integrationEvent.EventType}");
            await Task.Delay(100);
        }
    }
}
