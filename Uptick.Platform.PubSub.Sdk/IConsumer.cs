using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk
{
    public interface IConsumer<T>
    {
        Task ConsumeAsync(IntegrationEvent<T> integrationEvent);
    }
}
