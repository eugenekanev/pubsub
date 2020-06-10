using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk
{
    public interface ISubscriberFactory
    {
        Task<ISubscriber> CreateSubscriberAsync(string name, string routing, int retrycount, int prefetchcount);

        Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount, int prefetchcount);

        Task<ISubscriber> CreateSubscriberAsync(string name, string routing, int retrycount);

        Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount);

        Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount, int prefetchcount, 
            ISubscriptionSelector subscriptionSelector);

        Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings, int retrycount, int prefetchcount,
            ISubscriptionSelector subscriptionSelector, bool storedeadletter);

        Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments);

        Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, bool storedeadletter);

        Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector);

        Task<ISubscriber> CreateSubscriberAsync(string name, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector, bool storedeadletter);

        Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings, int retrycount, int prefetchcount);

        Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector);

        Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector, bool storedeadletter);

        Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments);

        Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, bool storedeadletter);

        Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector);

        Task<ISubscriber> CreateSubscriberAsync(string name, bool temporary, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector, bool storedeadletter);

        Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount);

        Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, bool storedeadletter, int prefetchcount);

        Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector);

        Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount, ISubscriptionSelector subscriptionSelector, bool storedeadletter);

        Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments);

        Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, bool storedeadletter, int prefetchcount, bool explicitAcknowledgments);

        Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector);

        Task<ISubscriber> CreateSubscriberAsync(string name, Action temporaryQueueDisconnected, IEnumerable<string> routings,
            int retrycount, bool storedeadletter, int prefetchcount, bool explicitAcknowledgments, ISubscriptionSelector subscriptionSelector);
    }
}