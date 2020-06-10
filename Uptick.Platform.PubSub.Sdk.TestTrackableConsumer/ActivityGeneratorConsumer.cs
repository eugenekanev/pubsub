using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Uptick.Platform.PubSub.Sdk.TestTrackableConsumer
{
    public class ActivityGeneratorConsumer : IConsumer<EmailCreated>
    {
        ILogger logger = Log.ForContext<ActivityGeneratorConsumer>();
        public async Task ConsumeAsync(IntegrationEvent<EmailCreated> messagEvent)
        {
            logger.Debug($"ACTIVITY GENERATOR. New Message of type {messagEvent.EventType} has been recieved");
            logger.Debug($"ACTIVITY GENERATOR. Email is {messagEvent.Content.email}");
            Random rnd = new Random();
            if (rnd.Next(0, 101) % 3 != 0)
            {
                logger.Debug($"ACTIVITY GENERATOR. Success of processing event of type {messagEvent.EventType} with" +
                             $" correlation id is {messagEvent.CorrelationId}");
            }
            else
            {
                logger.Error(new Exception($"ACTIVITY GENERATOR. Exception. correlationId is {messagEvent.CorrelationId}"),
                    $"ACTIVITY GENERATOR. Exception. correlationId is {messagEvent.CorrelationId}");
                throw new Exception($"ACTIVITY GENERATOR. Exception. correlationId is {messagEvent.CorrelationId}");
            }
            await Task.Delay(5000);
        }
    }
}
