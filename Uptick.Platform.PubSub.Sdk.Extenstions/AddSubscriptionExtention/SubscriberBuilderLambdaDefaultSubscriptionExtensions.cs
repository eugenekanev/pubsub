using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions
{
    public static class SubscriberBuilderLambdaDefaultSubscriptionExtensions
    {
        public static SubscriptionBuilder AddDefaultSubscription<TEvent>(
            this SubscriptionBuilder subscriptionBuilder,
            Func<IntegrationEvent<TEvent>, Task> consumer,
            Func<IntegrationEvent<TEvent>, Exception, Task> deadLetterCallback = null) where TEvent : class
        {
            var lambdaConsumer = new LambdaConsumer<TEvent>(consumer);
            subscriptionBuilder.AddDefaultSubscription(() => lambdaConsumer, deadLetterCallback);
            return subscriptionBuilder;
        }
    }
}
