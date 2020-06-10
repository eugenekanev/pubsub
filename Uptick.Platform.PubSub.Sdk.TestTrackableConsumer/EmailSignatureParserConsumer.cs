using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Uptick.Platform.PubSub.Sdk.TestTrackableConsumer
{
    public class EmailSignatureParserConsumer : IConsumer<ActivityCreated>
    {
        ILogger logger = Log.ForContext<ActivityGeneratorConsumer>();
        public async Task ConsumeAsync(IntegrationEvent<ActivityCreated> messagEvent)
        {
            logger.Debug($"EMAIL SIGNATURE PARSER. New Message of type {messagEvent.EventType} has been recieved");
            logger.Debug($"EMAIL SIGNATURE PARSER. Subject is {messagEvent.Content.subject}");
            Random rnd = new Random();
            if (rnd.Next(0, 101) % 3 != 0)
            {
                logger.Debug($"EMAIL SIGNATURE PARSER. Success of processing event of type {messagEvent.EventType} " +
                             $"with correlation id is {messagEvent.CorrelationId}");
            }
            else
            {
                logger.Error(new Exception($"EMAIL SIGNATURE PARSER. Exception. correlationId is {messagEvent.CorrelationId}"),
                    $"EMAIL SIGNATURE PARSER. Exception. correlationId is {messagEvent.CorrelationId}");
                throw new Exception($"EMAIL SIGNATURE PARSER. Exception. correlationId is {messagEvent.CorrelationId}");
            }
            await Task.Delay(5000);
        }
    }
}
