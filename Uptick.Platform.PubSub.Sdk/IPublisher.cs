using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk
{
    public interface IPublisher
    {
        Task PublishEventAsync<T>(IntegrationEvent<T> integrationEvent, string routing, bool persistant = true);

        Task PublishDirectEventAsync<T>(IntegrationEvent<T> integrationEvent, string subscriberName, bool persistant = true);
    }
}
