using System;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions
{
    public static class AddDefaultSubscriptionExtention
    {
        public static Tuple<ISubscriber, SubscriptionBuilder> AddDefaultSubscribtion<TEvent>(
            this ISubscriber subscriber, 
            IConsumer<TEvent> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent: class
        {
            var builder = SubscriptionBuilder.Create();
            builder.AddDefaultSubscription(() => consumer,deadLetterCallback);
            return new Tuple<ISubscriber, SubscriptionBuilder>(subscriber,builder);
        }
        
        
        public static Tuple<ISubscriber, SubscriptionBuilder> AddDefaultSubscribtion<TEvent>(
            this Tuple<ISubscriber, SubscriptionBuilder> subscriber, 
            IConsumer<TEvent> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent: class
        {
            var builder = subscriber.Item2;
            builder.AddDefaultSubscription(() => consumer,deadLetterCallback);
            return subscriber;
        }
        
        
        public static async Task <Tuple<ISubscriber, SubscriptionBuilder>> AddDefaultSubscribtion<TEvent>(
            this Task<ISubscriber> subscriberTask, 
            IConsumer<TEvent> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent: class
        {
            var subscriber = await subscriberTask;
            return AddDefaultSubscribtion(subscriber, consumer,deadLetterCallback);
        }
        
        
        public static async Task <Tuple<ISubscriber, SubscriptionBuilder>> AddDefaultSubscribtion<TEvent>(
            this Task<Tuple<ISubscriber, SubscriptionBuilder>> subscriberTask, 
            IConsumer<TEvent> consumer,
            Func<IntegrationEvent<TEvent>,Exception,Task> deadLetterCallback = null) where TEvent: class
        {
            var subscriber = await subscriberTask;
            return AddDefaultSubscribtion(subscriber, consumer,deadLetterCallback);
        }

    }
}