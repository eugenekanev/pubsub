using System;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions
{
    public static class AddLambdaSubscriptionExtention
    {
        public static async Task<Tuple<ISubscriber,SubscriptionBuilder>> AddSubscription<TEvent>(
            this Task<ISubscriber> subscriberTask, 
            string eventType, 
            Func<IntegrationEvent<TEvent>,Task> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent: class
        {
            var subscriber = await subscriberTask;
            return AddSubscription(subscriber, eventType, consumer,deadLetterCallback);
        }


        public static Tuple<ISubscriber,SubscriptionBuilder> AddSubscription<TEvent>(
            this ISubscriber subscriber, 
            string eventType, 
            Func<IntegrationEvent<TEvent>,Task> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent: class
        {
            var lambdaConsumer = new LambdaConsumer<TEvent>(consumer);
            var builder = SubscriptionBuilder.Create().AddSubscription(eventType, () =>lambdaConsumer, deadLetterCallback);
            return new Tuple<ISubscriber, SubscriptionBuilder>(subscriber,builder);
        }

        public static Tuple<ISubscriber, SubscriptionBuilder> AddSubscription<TEvent>(
            this Tuple<ISubscriber, SubscriptionBuilder> subscriber,
            string eventType,
            Func<IntegrationEvent<TEvent>,Task> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent : class
        {
            var lambdaConsumer = new LambdaConsumer<TEvent>(consumer);
            subscriber.Item2.AddSubscription(eventType, () =>lambdaConsumer, deadLetterCallback);
            return subscriber;
        }
        
        public static async  Task<Tuple<ISubscriber, SubscriptionBuilder>> AddSubscription<TEvent>(
            this Task<Tuple<ISubscriber, SubscriptionBuilder>> subscriberTask,
            string eventType,
            Func<IntegrationEvent<TEvent>,Task> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent : class
        {
            var subscriber = await subscriberTask;
            return AddSubscription(subscriber, eventType, consumer,deadLetterCallback);
        }
        
    }
}