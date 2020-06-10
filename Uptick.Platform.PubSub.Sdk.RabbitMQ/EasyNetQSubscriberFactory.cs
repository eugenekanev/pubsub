using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using Uptick.Platform.PubSub.Sdk.Exceptions;
using Uptick.Utils;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public class EasyNetQSubscriberFactory : ISubscriberFactory
    {
        private readonly IAdvancedBus _bus;

        protected readonly AsyncLazy<IExchange> _exchange;

        private readonly EasyNetQPublisher _easyNetQPublisherToWaitExchange;

        private readonly ISubscriberController _subscriberController;

        private readonly IEnvironmentNamingConventionController _environmentNamingConventionController;

        private readonly int _retryfactor;


        private readonly IPublisher _publisher;

        private readonly IBroker _broker;

        public EasyNetQSubscriberFactory(IBroker broker, string exchangename,
            string waitexchangename,
            ISubscriberController subscriberController,
            IEnvironmentNamingConventionController environmentNamingConventionController,
            int retryfactor, IPublisher publisher)
        {
            _broker = broker;
            _bus = broker.Bus;
            _subscriberController = subscriberController;
            _exchange = new AsyncLazy<IExchange>(async () => await _bus.ExchangeDeclareAsync(exchangename, ExchangeType.Topic));
            _easyNetQPublisherToWaitExchange = new EasyNetQPublisher(_bus, waitexchangename, environmentNamingConventionController);
            _environmentNamingConventionController = environmentNamingConventionController;
            _retryfactor = retryfactor;
            _publisher = publisher;
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, string routing, int retrycount, int prefetchcount)
        {
            return await CreateSubscriberAsync(name, new List<string>() { routing }, retrycount, prefetchcount, new DefaultSubscriptionSelector());
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount, int prefetchcount)
        {
            return await CreateSubscriberAsync(name, false, routings, retrycount, prefetchcount, new DefaultSubscriptionSelector());
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, string routing, int retrycount)
        {
            return await CreateSubscriberAsync(name, new List<string>() { routing }, retrycount, 50, new DefaultSubscriptionSelector());
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount)
        {
            return await CreateSubscriberAsync(name, routings, retrycount, 50, new DefaultSubscriptionSelector());
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings,
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector)
        {
            return await CreateSubscriberAsync(name, false, null, routings,
                retrycount, prefetchcount, false, new DefaultSubscriptionSelector(), true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, 
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector, bool storedeadletter)
        {
            return await CreateSubscriberAsync(name, false, null, routings, 
                retrycount, prefetchcount, false, new DefaultSubscriptionSelector(), storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount, int prefetchcount,
            bool explicitAcknowledgments)
        {
            return await CreateSubscriberAsync(name, false, null, routings,
                retrycount, prefetchcount, explicitAcknowledgments, new DefaultSubscriptionSelector(), true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount, int prefetchcount, 
            bool explicitAcknowledgments, bool storedeadletter)
        {
            return await CreateSubscriberAsync(name, false, null, routings, 
                retrycount, prefetchcount, explicitAcknowledgments, new DefaultSubscriptionSelector(), storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount, int prefetchcount,
            bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector)
        {
            return await CreateSubscriberAsync(name, false, null, routings, retrycount, prefetchcount,
                explicitAcknowledgments, subscriptionSelector, true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount, int prefetchcount, 
            bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector, bool storedeadletter)
        {
            return await CreateSubscriberAsync(name, false, null, routings, retrycount, prefetchcount, explicitAcknowledgments, subscriptionSelector, storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings, int retrycount, int prefetchcount)
        {
            return await CreateSubscriberAsync(name, temporary, routings, retrycount, prefetchcount, new DefaultSubscriptionSelector());
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector)
        {
            return await CreateSubscriberAsync(name, temporary, null, routings,
                retrycount, prefetchcount, false, new DefaultSubscriptionSelector(), true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector, bool storedeadletter)
        {
            return await CreateSubscriberAsync(name, temporary, null, routings,
                retrycount, prefetchcount, false, new DefaultSubscriptionSelector(), storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments)
        {
            return await CreateSubscriberAsync(name, temporary, null, routings,
                retrycount, prefetchcount, explicitAcknowledgments, new DefaultSubscriptionSelector(), true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, bool storedeadletter)
        {
            return await CreateSubscriberAsync(name, temporary, null, routings,
                retrycount, prefetchcount, explicitAcknowledgments, new DefaultSubscriptionSelector(), storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector)
        {
            return await CreateSubscriberAsync(name, temporary, null, routings,
                retrycount, prefetchcount, explicitAcknowledgments, subscriptionSelector, true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector, bool storedeadletter)
        {
            return await CreateSubscriberAsync(name, temporary, null, routings,
                retrycount, prefetchcount, explicitAcknowledgments, subscriptionSelector, storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount)
        {
            return await CreateSubscriberAsync(name, true, temporaryQueueDisconnected, routings,
                retrycount, prefetchcount, false, new DefaultSubscriptionSelector(), true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings, 
            int retrycount, bool storedeadletter, int prefetchcount)
        {
            return await CreateSubscriberAsync(name, true, temporaryQueueDisconnected, routings,
                retrycount, prefetchcount, false, new DefaultSubscriptionSelector(), storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector)
        {
            return await CreateSubscriberAsync(name, true, temporaryQueueDisconnected, routings,
                retrycount, prefetchcount, false, new DefaultSubscriptionSelector(), true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings, 
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector, bool storedeadletter)
        {
            return await CreateSubscriberAsync(name, true, temporaryQueueDisconnected, routings,
                retrycount, prefetchcount, false, new DefaultSubscriptionSelector(), storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments)
        {
            return await CreateSubscriberAsync(name, true, temporaryQueueDisconnected, routings,
                retrycount, prefetchcount, explicitAcknowledgments, new DefaultSubscriptionSelector(), true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, bool storedeadletter, int prefetchcount, bool explicitAcknowledgments)
        {
            return await CreateSubscriberAsync(name, true, temporaryQueueDisconnected, routings,
                retrycount, prefetchcount, explicitAcknowledgments, new DefaultSubscriptionSelector(), storedeadletter);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector)
        {
            return await CreateSubscriberAsync(name, true, temporaryQueueDisconnected, routings,
                retrycount, prefetchcount, explicitAcknowledgments, new DefaultSubscriptionSelector(), true);
        }

        public async Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings, 
            int retrycount, bool storedeadletter, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector)
        {
            return await CreateSubscriberAsync(name, true, temporaryQueueDisconnected, routings, 
                retrycount, prefetchcount, explicitAcknowledgments, new DefaultSubscriptionSelector(), storedeadletter);
        }

        private async Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, Action temporaryQueueDisconnected,
            IEnumerable<string> routings, int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector, bool storedeadletter)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name argument can not be null or empty");
            }

            if ((routings != null) && (routings.Any(routing => string.IsNullOrEmpty(routing)) || !routings.Any()))
            {
                throw new ArgumentException("routings argument can not be empty or contain empty strings");
            }

            if (retrycount < 0)
            {
                throw new ArgumentException("retrycount argument can not be less than zero");
            }

            if (temporaryQueueDisconnected != null)
            {
                _broker.RegisterDisconnectedAction(temporaryQueueDisconnected);
            }

            //Main subscriber queue and bindings
            var queue = await _bus.QueueDeclareAsync(
                _environmentNamingConventionController.GetQueueName(name), false, true, temporary);

            if (routings != null)
            {
                foreach (var routing in routings)
                {
                    await _bus.BindAsync(await _exchange, queue, routing);
                }
            }

            //Retry subscriber queue and bindings
            for (int i = 1; i <= retrycount; i++)
            {
                var retryqueueroutingkey = _environmentNamingConventionController.GetRetryRoutingKey(name, i);
                var retryqueuequeuename = _environmentNamingConventionController.GetRetryQueueName(name, i);
                var queueretrybinding = await _bus.BindAsync(await _exchange, queue,
                    retryqueueroutingkey);

                var retryqueue = await _bus.QueueDeclareAsync(retryqueuequeuename,
                    false, true, temporary, false, _retryfactor * i, null, null, (await _exchange).Name);

                var retryqueuebinding =
                    await _bus.BindAsync(_easyNetQPublisherToWaitExchange.Exchange,
                        retryqueue, retryqueueroutingkey);
            }



            //Dead letter subscriber queue and bindings
            if (storedeadletter)
            {
                var deadletterqueueroutingkey = _environmentNamingConventionController
                    .GetDeadLetterQueueRoutingKey(name);
                var deadletterqueuequeuename = _environmentNamingConventionController.
                    GetDeadLetterQueueName(name);
                var deadletterqueue = await _bus.QueueDeclareAsync(deadletterqueuequeuename, false, true, temporary);
                var deadletterqueuebinding = await
                    _bus.BindAsync(_easyNetQPublisherToWaitExchange.Exchange, deadletterqueue,
                        deadletterqueueroutingkey);
            }

            //Direct Routing Key binding
            var directroutingkey = _environmentNamingConventionController.GetDirectRoutingKey(name);
            var queuedirectroutingkeybinding = await _bus.BindAsync(await _exchange, queue,
                directroutingkey);


            if (_subscriberController.RegisterSubscriberName(name, out IDisposable nameHandle))
            {
                var executionHandler = new ExecutionHandlingStrategy(name, _easyNetQPublisherToWaitExchange,
                    retrycount, _environmentNamingConventionController, explicitAcknowledgments, _publisher, storedeadletter);
                return new EasyNetQSubscriber(executionHandler, _bus, queue, (ushort)prefetchcount,
                    subscriptionSelector, executionHandler, nameHandle);
            }
            else
            {
                throw new SubscriberAlreadyExistsException($"The subscriber " +
                                                           $"with name {name}" +
                                                           $" already exists");
            }
        }
    }
}