using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Uptick.Platform.PubSub.Sdk.TestConsumer
{
    public class UnknownEventTypeConsumer : IConsumer<JObject>
    {
        ILogger logger = Log.ForContext<UnknownEventTypeConsumer>();
        public async Task ConsumeAsync(IntegrationEvent<JObject> messagEvent)
        {
            logger.Debug($"The Message with uknown EventType has been recieved. Message {messagEvent}");
            await Task.FromResult(true);
        }
    }
}
