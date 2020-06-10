using Autofac;
using EasyNetQ;
using Microsoft.Extensions.Configuration;
using System;
using Uptick.Platform.PubSub.Sdk.RabbitMQ;

namespace Uptick.Platform.PubSub.Sdk.Autofac
{
    public class PubSubRabbitmqModule : Module
    {
        private readonly IConfiguration _configuration;

        public PubSubRabbitmqModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<ExchangePrefixEnvironmentNamingConventionController>()
                .WithParameter("exchangeName", _configuration["rabbitmq:exchangename"])
                .As<IModelNamingConventionController>()
                .As<IEnvironmentNamingConventionController>().SingleInstance();

            builder.Register<Func<string, EventHandler, EventHandler, Action<IServiceRegister>, IAdvancedBus>>(
                c => (connectionString, connected, disconnected, registerServices) => RabbitHutch.CreateBus(
                    connectionString, new AdvancedBusEventHandlers(connected, disconnected), registerServices).Advanced);

            builder.RegisterType<EasyNetQBroker>()
                .WithParameter("tombQueue", _configuration.GetValue("rabbitmq:tombQueue", "defaultTombQueue"))
                .WithParameter("connectionString", _configuration["rabbitmq:connectionstring"])
                .WithParameter("exchangeName", _configuration["rabbitmq:exchangename"])
                .As<IBroker>().SingleInstance();

            builder.Register(c => c.Resolve<IBroker>().Bus).As<IAdvancedBus>().SingleInstance();

            builder.RegisterType<EasyNetQPublisher>()
                .WithParameter("exchangename", _configuration["rabbitmq:exchangename"])
                .As<IPublisher>().SingleInstance();

            builder
                .RegisterType(typeof(EasyNetQSubscriberFactory))
                .WithParameter("exchangename", _configuration["rabbitmq:exchangename"])
                .WithParameter("waitexchangename", _configuration["rabbitmq:waitexchangename"])
                .WithParameter("retryfactor", int.Parse(_configuration["rabbitmq:retryfactor"]))
                .As(typeof(ISubscriberFactory)).SingleInstance();

            builder.RegisterType<OnlyOnceRegistrationSubscriberController>()
                .As<ISubscriberController>()
                .SingleInstance();
        }
    }
}
