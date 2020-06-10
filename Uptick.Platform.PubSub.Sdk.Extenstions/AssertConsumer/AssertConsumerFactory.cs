using System;
using System.Collections.Generic;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.AssertConsumer
{    

    public class AssertConsumerFactory : IDisposable
    {
        private readonly ISubscriberFactory _subscriberFactory;
        private readonly List<IDisposable> _consumers;

        public AssertConsumerFactory(ISubscriberFactory subscriberFactory)
        {
            _subscriberFactory = subscriberFactory;
            _consumers = new List<IDisposable>();
        }

        public IAssertConsumer<T> GetConsumer<T, TException>(string routingKey, IEnumerable<string> eventTypes)
            where T : class
            where TException : Exception
        {
            var consumer = new AssertConsumer<T, TException>(_subscriberFactory, routingKey, eventTypes);
            _consumers.Add(consumer);
            return consumer;
        }

        public IAssertConsumer<T> GetConsumer<T, TException>(string routingKey)
            where T : class
            where TException : Exception
        {
            return GetConsumer<T, TException>(routingKey, new[] {"*"});
        }

        public void Dispose()
        {
            foreach (var consumer in _consumers)
            {
                consumer.Dispose();
            }
        }
        
    }
}