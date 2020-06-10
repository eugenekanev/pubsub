using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Autofac;

namespace Uptick.Platform.PubSub.Sdk.TestConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Subscriber started to work.");
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true);
            var configuration = configurationBuilder.Build();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new PubSubRabbitmqModule(configuration));
            var container = containerBuilder.Build();

            SerilogInitializer.Initialize();

            var maxretrycount = int.Parse(configuration["maxretrycount"]);

            //all the entitites creation
            var dynamicObjectSubscriberFactory = 
                container.Resolve<ISubscriberFactory>();
            ISubscriber sharingHandlerSubscriber = 
                dynamicObjectSubscriberFactory.CreateSubscriberAsync("sharingruleshandler"
                , new List<string>{ "*.table.create.*"},
               maxretrycount).Result;

            IConsumer<JObject> sharingHandlerConsumer = 
                new ChangeTrackerTableCreateConsumer();
            sharingHandlerSubscriber.Subscribe(SubscriptionBuilder.Create().AddDefaultSubscription(() => sharingHandlerConsumer).Build());

            //typed consumer
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber typedSubscriber = 
                bookingCreatedSubscriberFactory.CreateSubscriberAsync("typedbookingcreatedconsumer", new List<string>{"*.table.create.*"},
                maxretrycount).Result;
            IConsumer<BookingCreated> typedConsumer = new TypedConsumer();
            typedSubscriber.Subscribe(SubscriptionBuilder.Create().AddDefaultSubscription(() => typedConsumer).Build());

            //relationship creation entitites 
            var relationshipCreatedSubscriberFactory = 
                container.Resolve<ISubscriberFactory>();
            ISubscriber bpmRelationshipCreateSubscriber = 
                relationshipCreatedSubscriberFactory.CreateSubscriberAsync("bpmRelationshipCreateHandler",
                    new List<string> { "*.table.create.relationship"}, maxretrycount).Result;

            IConsumer<RelationshipCreated> bpmRelationshipCreateConsumer = new ChangeTrackerTableCreateRelationshipBpmConsumer();

            bpmRelationshipCreateSubscriber.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddDefaultSubscription(() => bpmRelationshipCreateConsumer)
                    .Build());

            //all the entities creation. 2 different handlers for 2 different event types.
            ISubscriber forDemultiplexerConsumerSubscriber = 
                dynamicObjectSubscriberFactory
                .CreateSubscriberAsync("DemultiplexerRelationshipAndBookingHandler",
                        new List<string> { "*.table.create.*"},maxretrycount).Result;

            var changeTrackerTableCreateRelationshipBpmConsumer = 
                new ChangeTrackerTableCreateRelationshipBpmConsumer();
            var changeTrackerTableCreateConsumer = 
                new ChangeTrackerTableCreateConsumer();


            forDemultiplexerConsumerSubscriber.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription<RelationshipCreated>("relationshipcreated",
                        () => changeTrackerTableCreateRelationshipBpmConsumer)
                    .AddSubscription("bookingcreated", () => changeTrackerTableCreateConsumer)
                    .AddDefaultSubscription(() => new UnknownEventTypeConsumer())
                    .Build());


            Console.WriteLine("Press button to exit");
            string exit = Console.ReadLine();
            container.Dispose();


          
        }
    }
}