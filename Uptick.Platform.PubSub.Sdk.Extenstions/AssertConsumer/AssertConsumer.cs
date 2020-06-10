using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Polly;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.AssertConsumer
{
    internal class AssertConsumer<T, TException> : IConsumer<T>, IAssertConsumer<T>
        where T : class
        where TException : Exception
    {
        private readonly IEnumerable<string> _eventTypes;
        private readonly string _routingKey;

        private readonly ISubscriberFactory _subscriberFactory;

        public AssertConsumer(ISubscriberFactory subscriberFactory, string routingKey, IEnumerable<string> eventTypes)
        {
            _subscriberFactory = subscriberFactory;
            _routingKey = routingKey;
            _eventTypes = eventTypes;
        }

        private ConcurrentBag<T> ReceivedEvents { get; } = new ConcurrentBag<T>();

        private ISubscriber Subscriber { get; set; }

        public Task ConsumeAsync(IntegrationEvent<T> integrationEvent)
        {
            ReceivedEvents.Add(integrationEvent.Content);
            return Task.CompletedTask;
        }

        public void ClearEvents()
        {
            while (ReceivedEvents.TryTake(out _)) { }
        }

        public async Task WaitAssertAsync(Func<T[], Task> assertionFunc, int retryCount, int retryTimeoutRatioMs)
        {
            var policy = Policy
                .Handle<TException>()
                .WaitAndRetryAsync(retryCount, i => TimeSpan.FromMilliseconds(i * retryTimeoutRatioMs));

            await policy.ExecuteAsync(async () => await assertionFunc(ReceivedEvents.ToArray()));
        }
        
        public void WaitAssert(Action<T[]> assertionFunc, int retryCount, int retryTimeoutRatioMs)
        {
            var policy = Policy
                .Handle<TException>()
                .WaitAndRetry(retryCount, i => TimeSpan.FromMilliseconds(i * retryTimeoutRatioMs));

            policy.Execute(() => assertionFunc(ReceivedEvents.ToArray()));
        }

        public void Dispose()
        {
            Subscriber?.Dispose();
            Subscriber = null;
        }

        public async Task SubscribeAsync()
        {
            Subscriber = await _subscriberFactory.CreateSubscriberAsync(
                $"assert_queue_{Guid.NewGuid().ToString().Replace("-", "_")}",
                _routingKey, 1);

            var subscriptionBuilder = SubscriptionBuilder.Create();

            foreach (var eventType in _eventTypes.Distinct())
                subscriptionBuilder.AddSubscription(eventType, () => this);
            Subscriber.Subscribe(subscriptionBuilder.Build());
        }
    }
}