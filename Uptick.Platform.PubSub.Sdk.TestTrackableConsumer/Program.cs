using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Uptick.Platform.PubSub.Sdk.Autofac;
using Uptick.Platform.PubSub.Sdk.Extensions.Autofac;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;
using Uptick.Platform.PubSub.Sdk.RabbitMQ;

namespace Uptick.Platform.PubSub.Sdk.TestTrackableConsumer
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Test trackable Subscriber Host started to work.");
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true);
            var configuration = configurationBuilder.Build();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new PubSubRabbitmqModule(configuration));
            containerBuilder.RegisterModule(new PubSubExtensionModule());
            var container = containerBuilder.Build();

            SerilogInitializer.Initialize();

            var trackerFactory =
                container.Resolve<ITrackerFactory>();

            //constants
            string trackerName = "servicecontroller";
            string activityGeneratorName = "activitygenerator";
            string emailsignatureName = "emailsignature";
            string taggerName = "tagger";
            string activitygeneratorroutingkey = "exchangecollector.create.email";
            string emailsignatureroutingkey = "changetracker.table.create.activity";
            string taggeroutingkey = "changetracker.table.create.activity";
            string exchangeeventtype = "email.created";
            string changetrackereventtype = "activity.create";

            //Let's create tracking subscriber that tracks processing events from activity generator and email signature
            var serviceController = await trackerFactory.CreateTrackerAsync(trackerName, true,
                new List<string>() {activityGeneratorName, emailsignatureName}, 0, 1);

            var subscriberFactory = container.Resolve<ISubscriberFactory>();

            //Let's create activity generator and email signature parser whose messages' processing we want to track
            var activitygenerator = await subscriberFactory.CreateSubscriberAsync(activityGeneratorName, true,
                new List<string> {activitygeneratorroutingkey}, 0, 1, true);

            var emailsignatureparser = await subscriberFactory.CreateSubscriberAsync(emailsignatureName, true,
                new List<string> { emailsignatureroutingkey }, 0, 1, true);

            var activityGeneratorConsumer =
                new ActivityGeneratorConsumer();
            var emailSignatureParserConsumer =
                new EmailSignatureParserConsumer();

            activitygenerator.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription(exchangeeventtype,
                        () => activityGeneratorConsumer)
                    .Build());

            emailsignatureparser.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription(changetrackereventtype,
                        () => emailSignatureParserConsumer)
                    .Build());

            //Let's create tagger. We are not going to track its message processing. Tagger will recieve the same messages as Email Signature Parser will do.
            var taggerConsumer =
                new TaggerConsumer();

            var tagger = await subscriberFactory.CreateSubscriberAsync(taggerName,
                new List<string> { taggeroutingkey }, 0, 1, false);
            tagger.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription(changetrackereventtype,
                        () => taggerConsumer)
                    .Build());

            //Let's process events sent to the tracking queue
            var trackerConsumer =
                new TrackerConsumer();


            serviceController.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription<string>(exchangeeventtype,
                        () => trackerConsumer)
                    .AddSubscription<string>(changetrackereventtype,
                        () => trackerConsumer)
                    .Build(),
                SubscriptionBuilder
                    .Create()
                    .AddSubscription<DeadLetterEventDescriptor<EmailCreated>>(exchangeeventtype,
                        () => trackerConsumer)
                    .AddSubscription<DeadLetterEventDescriptor<ActivityCreated>>(changetrackereventtype,
                        () => trackerConsumer)
                    .Build());

            //Let's publish something
            var publisher = container.Resolve<IPublisher>();
            while (true)
            {
                Console.WriteLine("Press the appropriate button to publish the event: (e) - email created, (a) - activity created");
                string input = Console.ReadLine();
                if (input == "e")
                {

                    EmailCreated emailCreated =
                        new EmailCreated() { email = "eugene@uptick.com" + DateTime.Now.Minute, pathtos3 = "emails\\"+ Guid.NewGuid()};

                    var emailCreatedIntegrationEvent = new IntegrationEvent<EmailCreated>()
                    {
                        CorrelationId = Guid.NewGuid(),
                        Content = emailCreated,
                        EventType = exchangeeventtype
                    };

                    Console.WriteLine($"The email created event with correlation id {emailCreatedIntegrationEvent.CorrelationId} is going to be published");

                    await publisher.PublishEventAsync(emailCreatedIntegrationEvent,
                        activitygeneratorroutingkey);
                }

                if (input == "a")
                {

                   ActivityCreated activityCreated =
                        new ActivityCreated() { subject = "email subject"+DateTime.Now.Minute,accountid = Guid.NewGuid().ToString(),
                            id = Guid.NewGuid().ToString(), type = "email"};

                    var acivtityCreatedIntegrationEvent = new IntegrationEvent<ActivityCreated>()
                    {
                        Content = activityCreated,
                        EventType = changetrackereventtype
                    };

                    Console.WriteLine($"The activity created event with correlation id {acivtityCreatedIntegrationEvent.CorrelationId} is going to be published");

                    await publisher.PublishEventAsync(acivtityCreatedIntegrationEvent,
                        emailsignatureroutingkey);
                }
            }
        }
    }
}
