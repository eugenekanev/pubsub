using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Exceptions;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter
{
    public class DeadLetterSubscriptionWrapper : ISubscription
    {
        private readonly ISubscription _originalSubscription;
        private readonly IPublisher _publisher;
        private readonly string _subscriberDeadLetterQueueName;
        public DeadLetterSubscriptionWrapper(ISubscription originalSubscription, 
            IPublisher publisher, string subscriberDeadLetterQueueName)
        {
            _originalSubscription = originalSubscription;
            _publisher = publisher;
            _subscriberDeadLetterQueueName = subscriberDeadLetterQueueName;
        }
        public async Task InvokeAsync(string serializedMessage)
        {
            try
            {
                await _originalSubscription.InvokeAsync(serializedMessage);
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case MalformedEventException ex:
                        throw ex;
                    case DiscardEventException ex:
                        throw ex;
                    default:
                        IntegrationEvent<DeadLetterEventDescriptor<JObject>> integrationEvent =
                            JsonConvert.DeserializeObject<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>(serializedMessage);
                        await _publisher.PublishDirectEventAsync(integrationEvent, _subscriberDeadLetterQueueName);
                        break;
                }
            }
        }

        public async Task NotifyAboutDeadLetterAsync(string serializedMessage, Exception exception)
        {
            await _originalSubscription.NotifyAboutDeadLetterAsync(serializedMessage, exception);
        }
    }
}
