using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Uptick.Platform.PubSub.Sdk.TestTrackableConsumer
{
    public class TaggerConsumer : IConsumer<ActivityCreated>
    {
        ILogger logger = Log.ForContext<ActivityGeneratorConsumer>();
        public async Task ConsumeAsync(IntegrationEvent<ActivityCreated> messagEvent)
        {
            logger.Debug("");
            logger.Debug($"TAGGER. New Message of type {messagEvent.EventType} has been recieved");
            logger.Debug($"TAGGER.Subject is {messagEvent.Content.subject}");
            Random rnd = new Random();
            if (rnd.Next(0, 101) % 3 != 0)
            {
                logger.Debug($"TAGGER.Success of processing event of type {messagEvent.EventType} with correlation id is {messagEvent.CorrelationId}");
            }
            else
            {
                logger.Error(new Exception($"TAGGER. Exception. correlationId is {messagEvent.CorrelationId}"),
                    $"TAGGER. Exception.correlationId is {messagEvent.CorrelationId}");
                throw new Exception($"TAGGER. Exception. correlationId is {messagEvent.CorrelationId}");
            }
            await Task.Delay(5000);
        }
    }
}
