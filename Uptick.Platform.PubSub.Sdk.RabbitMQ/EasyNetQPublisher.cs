using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using Newtonsoft.Json;
using Uptick.Utils;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public class EasyNetQPublisher : IPublisher
    {
        private readonly IAdvancedBus _bus;
        private readonly AsyncLazy<IExchange> _exchange;
        private readonly IModelNamingConventionController _modelNamingConventionController;
        const byte PersistentDeliveryMode = 2;
        const byte NonPersistentDeliveryMode = 1;


        public EasyNetQPublisher(IAdvancedBus bus, string exchangename, IModelNamingConventionController modelNamingConventionController)
        {
            _bus = bus;
            _exchange = new AsyncLazy<IExchange>(async () =>
                await _bus.ExchangeDeclareAsync(exchangename, ExchangeType.Topic));
            _modelNamingConventionController = modelNamingConventionController;
        }

        public async Task PublishEventAsync<T>(
            IntegrationEvent<T> integrationEvent, string routing, bool persistant)
        {
            integrationEvent.CorrelationId = integrationEvent.CorrelationId ?? CorrelationHelper.GetCorrelationId();
            
            var message = JsonConvert.SerializeObject(integrationEvent);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new MessageProperties()
            {
                DeliveryMode = persistant ? PersistentDeliveryMode : NonPersistentDeliveryMode
            };

            await PublishEventAsync(body, properties, routing);
        }

        public async Task PublishEventAsync(byte[] body, MessageProperties messageProperties, string routing)
        {
            await _bus.PublishAsync(await _exchange, routing, false, messageProperties, body);
        }

        public async Task PublishDirectEventAsync<T>(IntegrationEvent<T> integrationEvent, 
            string subscriberName, bool persistant = true)
        {
            await PublishEventAsync(integrationEvent, _modelNamingConventionController.GetDirectRoutingKey(subscriberName),
                persistant);
        }

        public IExchange Exchange => _exchange.Value.ConfigureAwait(false).GetAwaiter().GetResult();
    }
}