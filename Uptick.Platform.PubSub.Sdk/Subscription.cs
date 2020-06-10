using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Uptick.Platform.PubSub.Sdk
{
        public interface ISubscription
        {
            Task InvokeAsync(string serializedMessage);

            Task NotifyAboutDeadLetterAsync(string serializedMessage, Exception exception);
    }

        public class Subscription<TEvent> : ISubscription
            where TEvent : class
        {
            private readonly Func<IConsumer<TEvent>> _consumerFactory;
            private readonly Func<IntegrationEvent<TEvent>, Exception, Task> _deadLetterCallbackFunc;

        public Subscription(Func<IConsumer<TEvent>> consumerFactory, Func<IntegrationEvent<TEvent>, Exception, Task> deadLetterCallbackFunc)
            {
                _consumerFactory = consumerFactory;
                _deadLetterCallbackFunc = deadLetterCallbackFunc;
            }

        public async Task NotifyAboutDeadLetterAsync(string serializedMessage, Exception exception)
        {
            if (_deadLetterCallbackFunc != null)
            {
                IntegrationEvent<TEvent> integrationEvent = JsonConvert.DeserializeObject<IntegrationEvent<TEvent>>(serializedMessage);
                await _deadLetterCallbackFunc(integrationEvent, exception);
            }
           
        }

        async Task ISubscription.InvokeAsync(string serializedMessage)
            {
                IConsumer<TEvent> consumer = _consumerFactory();
                IntegrationEvent<TEvent> integrationEvent = JsonConvert.DeserializeObject<IntegrationEvent<TEvent>>(serializedMessage);
                await consumer.ConsumeAsync(integrationEvent);
            }

 
        }

        public class SubscriptionBuilder
        {
            private readonly Dictionary<string, ISubscription> _subscriptions = new Dictionary<string, ISubscription>();

            private SubscriptionBuilder()
            {
            }

            public static SubscriptionBuilder Create()
            {
                return new SubscriptionBuilder();
            }

            public SubscriptionBuilder AddSubscription<TEvent>(string eventType, Func<IConsumer<TEvent>> consumerFactory, 
                Func<IntegrationEvent<TEvent>, Exception, Task> deadLetterCallbackFunc =null) 
                where TEvent : class
            {
                _subscriptions.Add(eventType, new Subscription<TEvent>(consumerFactory, deadLetterCallbackFunc));
                return this;
            }


            public SubscriptionBuilder AddDefaultSubscription<TEvent>(Func<IConsumer<TEvent>> consumerFactory, Func<IntegrationEvent<TEvent>,
                Exception, Task> deadLetterCallbackFunc = null)
                where TEvent : class
            {
                _subscriptions.Add("*", new Subscription<TEvent>(consumerFactory, deadLetterCallbackFunc));
                return this;
            }

            public IDictionary<string, ISubscription> Build()
            {
                return _subscriptions;
            }
        }
}