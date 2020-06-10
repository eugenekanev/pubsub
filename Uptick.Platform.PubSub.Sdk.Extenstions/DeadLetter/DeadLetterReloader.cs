using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter
{
    public class DeadLetterReloader<T> : IConsumer<DeadLetterEventDescriptor<T>>
    {
        private readonly IPublisher _publisher;
        private readonly string _subscriberName;
        public DeadLetterReloader(IPublisher publisher, string subscriberName)
        {
            _publisher = publisher;
            _subscriberName = subscriberName;
        }

        public async Task ConsumeAsync(IntegrationEvent<DeadLetterEventDescriptor<T>> integrationEvent)
        {
            await _publisher.PublishDirectEventAsync(integrationEvent.Content.Original, _subscriberName);
        }
    }
}
