using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Serilog;
using Uptick.Platform.PubSub.Sdk.Autofac;

namespace Uptick.Platform.PubSub.Sdk.TestPublisher
{
    class Program
    {
        static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
            var configuration = configurationBuilder.Build();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new PubSubRabbitmqModule(configuration));

            var container = containerBuilder.Build();

            SerilogInitializer.Initialize();

            ILogger logger = Log.ForContext<Program>();

            var publisher = container.Resolve<IPublisher>();

            bool continuework = true;
            
            int count = 0;
            logger.Debug("Publisher started to work.");
            while (continuework)
            {
               logger.Debug("Enter 'exit' to finish: ");
               string exit = Console.ReadLine();
               if (exit == "exit")
               {
                   continuework = false;
               }
               else
               {
                   logger.Debug("Publishing the event");
                   if (count % 2 != 0)
                   {
                       logger.Debug("Publishing RelationshipCreated event");


                       RelationshipCreated relationshipCreated =
                           new RelationshipCreated(){RelationshipId = Guid.NewGuid() };

                       var relationshipCreatedIntegrationEvent = new IntegrationEvent<RelationshipCreated>()
                       {
                           Content = relationshipCreated,
                           EventType = "relationshipcreated"
                       };

                       publisher.PublishEventAsync(relationshipCreatedIntegrationEvent, "changetracker.table.create.relationship").Wait();
                   }
                   else
                   {
                       logger.Debug("Publishing BookingCreated event");
                       BookingCreated bookingCreated =
                       new BookingCreated(){BookingName = string.Concat("Booking Name ", Guid.NewGuid().ToString()) };

                       var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
                       {
                           Content = bookingCreated,
                           EventType = "bookingcreated",
                           CorrelationId = Guid.NewGuid()
                       };

                       publisher.PublishEventAsync(bookingCreatedIntegrationEvent, "changetracker.table.create.booking").Wait();
                    }
                   count = count + 1;
                   Console.ReadLine();
                   }
            }

            container.Dispose();
        }
    }
}