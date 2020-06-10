using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Uptick.Platform.PubSub.Sdk.TestConsumer
{
    public class ChangeTrackerTableCreateConsumer : IConsumer<JObject>
    {
        ILogger logger = Log.ForContext<ChangeTrackerTableCreateConsumer>();
        public async Task ConsumeAsync(IntegrationEvent<JObject> messagEvent)
        {
            logger.Debug("New Message has been recieved");
            logger.Debug($"EventType is {messagEvent}");
            Random rnd = new Random();
            if (rnd.Next(0, 101) % 3 != 0)
            {
                logger.Debug("Success");
            }
            else
            {
                logger.Error(new Exception("Some Exception"),"Some Exception");
                throw new Exception("Some Exception");
            }
            await Task.Delay(5000);
        }
    }
}
