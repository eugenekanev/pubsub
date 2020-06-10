using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Uptick.Platform.PubSub.Sdk.Autofac;
using Uptick.Platform.PubSub.Sdk.Extensions.Autofac;
using Uptick.Platform.PubSub.Sdk.Management.Autofac;

namespace Uptick.Platform.PubSub.Sdk.ComponentTests
{
    public static class ConfigurationHelper
    {
        public static IConfiguration ProvideConfiguration()
        {
            var env = Environment.GetEnvironmentVariable("EnvironmentName") ?? "Development";

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddJsonFile("settings.json", false, true)
                .AddJsonFile($"settings.{env}.json", true, true).AddEnvironmentVariables();
            IConfiguration configuration = configurationBuilder.Build();

            return configuration;
        }

        public static IContainer ConfigureContainer(IConfiguration configuration)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new PubSubRabbitmqModule(configuration));
            containerBuilder.RegisterModule(new PubSubExtensionModule());
            containerBuilder.RegisterModule(new PubSubManagementModule());

            var container = containerBuilder.Build();

            return container;
        }
    }
}
