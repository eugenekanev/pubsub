using System;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions
{
    public static class AddEmptyDefaultSubscriptionExtention
    {
        
        public static Tuple<ISubscriber, SubscriptionBuilder> AddEmptyDefaultSubscribtion(this ISubscriber subscriber)
        {
            var builder = SubscriptionBuilder.Create();
            var emptyconsumer = new DoNothingConsumer<object>();
            builder.AddDefaultSubscription(() => emptyconsumer);
            return new Tuple<ISubscriber, SubscriptionBuilder>(subscriber,builder);
        }

        public static Tuple<ISubscriber, SubscriptionBuilder> AddEmptyDefaultSubscribtion(this Tuple<ISubscriber, SubscriptionBuilder> subscriber)
        {
            var emptyconsumer = new DoNothingConsumer<object>();
            subscriber.Item2.AddDefaultSubscription(() => emptyconsumer);
            return subscriber;
        }
        
        public static async Task<Tuple<ISubscriber, SubscriptionBuilder>> AddEmptyDefaultSubscribtion(this Task<Tuple<ISubscriber, SubscriptionBuilder>> subscriberTask)
        {
            var subscriber = await subscriberTask;
            return AddEmptyDefaultSubscribtion(subscriber);
        }
        
        
        public static async Task <Tuple<ISubscriber, SubscriptionBuilder>> AddEmptyDefaultSubscribtion(this Task<ISubscriber> subscriberTask)
        {
            
            var subscriber = await subscriberTask;
            return AddEmptyDefaultSubscribtion(subscriber);
        }
        
    }
}