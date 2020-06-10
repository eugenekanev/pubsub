using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions
{
    public class DoNothingConsumer<T> : IConsumer<T>
    {
        public Task ConsumeAsync(IntegrationEvent<T> integrationEvent)
        {
            return Task.CompletedTask;
        }
    }
}