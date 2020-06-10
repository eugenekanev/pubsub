using System;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions
{
    public static class AddSubscriptionExtention
    {
        public static async Task<Tuple<ISubscriber,SubscriptionBuilder>> AddSubscription<TEvent>(
            this Task<ISubscriber> subscriberTask, 
            string eventType, 
            IConsumer<TEvent> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent: class
        {
            var subscriber = await subscriberTask;
            return AddSubscription(subscriber, eventType, consumer,deadLetterCallback);
        }


        public static Tuple<ISubscriber,SubscriptionBuilder> AddSubscription<TEvent>(
            this ISubscriber subscriber, 
            string eventType, 
            IConsumer<TEvent> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent: class
        {
            var builder = SubscriptionBuilder.Create().AddSubscription(eventType,() => consumer,deadLetterCallback);
            return new Tuple<ISubscriber, SubscriptionBuilder>(subscriber,builder);
        }

        public static Tuple<ISubscriber, SubscriptionBuilder> AddSubscription<TEvent>(
            this Tuple<ISubscriber, SubscriptionBuilder> subscriber,
            string eventType,
            IConsumer<TEvent> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent : class
        {
            subscriber.Item2.AddSubscription(eventType,() => consumer,deadLetterCallback);
            return subscriber;
        }
        
        public static async  Task<Tuple<ISubscriber, SubscriptionBuilder>> AddSubscription<TEvent>(
            this Task<Tuple<ISubscriber, SubscriptionBuilder>> subscriberTask,
            string eventType,
            IConsumer<TEvent> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent : class
        {
            var subscriber = await subscriberTask;
            return AddSubscription(subscriber, eventType, consumer,deadLetterCallback);
        }
        
        public static async Task<ISubscriber> Build(this Task<Tuple<ISubscriber, SubscriptionBuilder>> subscriberTask)
        {
            var subscriber = await subscriberTask;
            return Build(subscriber);
        }
        
        public static ISubscriber Build(this Tuple<ISubscriber, SubscriptionBuilder> subscriber)
        {
            subscriber.Item1.Subscribe(subscriber.Item2.Build());
            return subscriber.Item1;
        }
    }
}