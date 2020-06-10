using System;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Uptick.Platform.PubSub.ExchangeBinder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("The exchange Binder has been started");

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            var configuration = configurationBuilder.Build();

            var user = configuration["user"];
            var pass = configuration["pass"];
            var hostname = configuration["hostname"];
            var port = configuration["port"];

            var destinationexchange = configuration["destinationexchange"];
            var sourceexchange = configuration["sourceexchange"];
            var routingKey = configuration["routingKey"];



            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = user;
            factory.Password = pass;
            factory.HostName = hostname;
            factory.Port = int.Parse(port);

            IConnection conn = factory.CreateConnection();
            IModel channel = conn.CreateModel();

            channel.ExchangeBind(destinationexchange, sourceexchange, routingKey);

            channel.Close();
            conn.Close();
            Console.WriteLine("The exchange Binder has been finished");


        }
    }
}
