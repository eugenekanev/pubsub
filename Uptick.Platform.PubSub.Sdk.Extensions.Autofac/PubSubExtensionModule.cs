using System;
using Autofac;
using Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;

namespace Uptick.Platform.PubSub.Sdk.Extensions.Autofac
{
    public class PubSubExtensionModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<DefaultTrackerFactory>()
                .As<ITrackerFactory>().SingleInstance();

            builder.RegisterType<DefaultTrackerNamingConventionController>()
                .As<ITrackerNamingConventionController>().SingleInstance();

            builder.RegisterType<DefaultDeadLetterSubscriberFactory>()
                .As<IDeadLetterSubscriberFactory>().SingleInstance();
        }
    }
}
