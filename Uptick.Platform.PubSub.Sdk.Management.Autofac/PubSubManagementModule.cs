using System;
using Autofac;
using Uptick.Platform.PubSub.Sdk.Management.RabbitMQ;

namespace Uptick.Platform.PubSub.Sdk.Management.Autofac
{
    public class PubSubManagementModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<EasyNetQQueueInfoProvider>()
                .As<IQueueInfoProvider>().SingleInstance();

        }
    }
}
