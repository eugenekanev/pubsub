using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;
using Uptick.Platform.PubSub.Sdk.RabbitMQ;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.ComponentTests
{
    public class TrackerTests
    {
        [Fact]
        public async Task Tracker_3DifferentSubscribers_OnlyAppropriateMessagesMustBeReceived()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Tracker_3DifferentSubscribers_OnlyAppropriateMessagesMustBeReceived.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);


            //Let's create Tracker to follow activitygenerator and emailsignature subscribers
            var maxretrycount = 0;
            string trackerName = "servicecontroller";
            string activityGeneratorName = "activitygenerator";
            string emailSignatureName = "emailsignature";
            string taggerName = "tagger";

            var trackerFactory =
                container.Resolve<ITrackerFactory>();
            var serviceController = await trackerFactory.CreateTrackerAsync(trackerName, false,
                new List<string>() { activityGeneratorName, emailSignatureName}, 0, 1);

            //Let's create Subscribers. We want to follow some of them (activitygenerator and emailsignature)
            //emailsignature and tagger subscribe to the same routing key

            var subscriberFactory = container.Resolve<ISubscriberFactory>();

            string activitygeneratorroutingkey = "exchangecollector.create.email";
            string emailsignatureroutingkey = "changetracker.table.create.activity";
            string taggeroutingkey = "changetracker.table.create.activity";

            ISubscriber activityGeneratorSubscriber =
                await subscriberFactory
                    .CreateSubscriberAsync(activityGeneratorName, new List<string>
                    {
                        activitygeneratorroutingkey,
                    }, maxretrycount, 20, true);

            ISubscriber emailSignatureSubscriber =
                await subscriberFactory
                    .CreateSubscriberAsync(emailSignatureName, new List<string>
                    {
                        emailsignatureroutingkey,
                    }, maxretrycount, 20, false); //oops! we forgot to specify that we want follow this subscriber

            ISubscriber taggerSubscriber =
                await subscriberFactory
                    .CreateSubscriberAsync(taggerName, new List<string>
                    {
                        taggeroutingkey,
                    }, maxretrycount, 20, true);

            //Let's specify Consumers
            string emailCreatedEventType = "email.created";
            string activityCreatedEventType = "activity.create";


            var activityGeneratorConsumer =
                new ActivityGeneratorConsumer();

            activityGeneratorSubscriber.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription(emailCreatedEventType,
                        () => activityGeneratorConsumer)
                    .Build());

            var emailSignatureConsumer =
                new EmailSignatureConsumer();

            emailSignatureSubscriber.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription(activityCreatedEventType,
                        () => emailSignatureConsumer)
                    .Build());

            var taggerConsumer =
                new TaggerConsumer();

            taggerSubscriber.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription(activityCreatedEventType,
                        () => taggerConsumer)
                    .Build());

            //Let's process events sent to the tracking queue
            var trackerConsumer =
                new TrackerConsumer();


            serviceController.Subscribe(
                SubscriptionBuilder
                    .Create()
                    .AddSubscription<string>(emailCreatedEventType,
                        () => trackerConsumer)
                    .AddSubscription<string>(activityCreatedEventType,
                        () => trackerConsumer)
                    .Build(),
                SubscriptionBuilder
                    .Create()
                    .AddSubscription<DeadLetterEventDescriptor<JObject>>(emailCreatedEventType,
                        () => trackerConsumer)
                    .AddSubscription<DeadLetterEventDescriptor<JObject>>(activityCreatedEventType,
                        () => trackerConsumer)
                    .Build());

            //Let's create Publisher
            var publisher = container.Resolve<IPublisher>();

            //Let's create and publish EmailCreated events. All of them must be catched by ActivityGenerator. The result of processing must be catched by tracker.
            var emailCreatedIntegrationEvent1 = new IntegrationEvent<EmailCreated>()
            {
                CorrelationId = Guid.NewGuid(),
                Content = new EmailCreated { Email = "boss@london.com" },
                EventType = emailCreatedEventType
            };

            var emailCreatedIntegrationEvent2 = new IntegrationEvent<EmailCreated>()
            {
                CorrelationId = Guid.NewGuid(),
                Content = new EmailCreated { Email = "" },
                EventType = emailCreatedEventType
            };

            var emailCreatedIntegrationEvent3 = new IntegrationEvent<EmailCreated>()
            {
                CorrelationId = Guid.NewGuid(),
                Content = new EmailCreated { Email = "boss@paris.com" },
                EventType = emailCreatedEventType
            };

            await publisher.PublishEventAsync(emailCreatedIntegrationEvent1,
                activitygeneratorroutingkey);
            await publisher.PublishEventAsync(emailCreatedIntegrationEvent2,
                activitygeneratorroutingkey);
            await publisher.PublishEventAsync(emailCreatedIntegrationEvent3,
                activitygeneratorroutingkey);

            //Let's create and publish ActivityCreated events. All of them must be catched by EmailSignature and Tagger as they have the same binding key.
            //The result of processing should be catched by tracker. EmailSignature forgot to specify that after consuming
            //the explicit acknowledge message must be sent. The Tagger has not been specified as the subscriber whose message
            //processing results the Tracker is interested in.
            var activityCreatedIntegrationEvent1 = new IntegrationEvent<ActivityCreated>()
            {
                CorrelationId = Guid.NewGuid(),
                Content = new ActivityCreated() { Id = Guid.NewGuid().ToString() },
                EventType = activityCreatedEventType
            };

            var activityCreatedIntegrationEvent2 = new IntegrationEvent<ActivityCreated>()
            {
                CorrelationId = Guid.NewGuid(),
                Content = new ActivityCreated() { Id = Guid.NewGuid().ToString() },
                EventType = activityCreatedEventType
            };

            await publisher.PublishEventAsync(activityCreatedIntegrationEvent1,
                emailsignatureroutingkey);
            await publisher.PublishEventAsync(activityCreatedIntegrationEvent2,
                emailsignatureroutingkey);

            //wait 10 seconds
            await Task.Delay(10000);

            //check
            var actualFailedresult = new List<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>();
            var actualSuccessulresult = new List<IntegrationEvent<string>>();
            foreach (var processedIntegrationEvent in trackerConsumer.FailedProcessedIntegrationEvents)
            {
                actualFailedresult.Add(processedIntegrationEvent);
            }
            foreach (var processedIntegrationEvent in trackerConsumer.SuccessfullyProcessedIntegrationEvents)
            {
                actualSuccessulresult.Add(processedIntegrationEvent);
            }

            actualFailedresult.Count.Should().Be(1);
            actualFailedresult[0].CorrelationId.Should().Be(emailCreatedIntegrationEvent2.CorrelationId);
            actualSuccessulresult.Count.Should().Be(2);
            actualSuccessulresult.Select(se=>se.CorrelationId)
                .ShouldBeEquivalentTo(new List<Guid?>{ emailCreatedIntegrationEvent1.CorrelationId , emailCreatedIntegrationEvent3.CorrelationId});

        }

        #region Helpers
        public class EmailCreated
        {
            public string Email { get; set; }
        }
        public class ActivityGeneratorConsumer : IConsumer<EmailCreated>
        {
            public Task ConsumeAsync(IntegrationEvent<EmailCreated> messagEvent)
            {
                if (messagEvent.Content.Email == string.Empty)
                {
                    throw new Exception($"ACTIVITY GENERATOR. Exception. correlationId is {messagEvent.CorrelationId}");
                }
                else
                {
                    return Task.CompletedTask;
                }
            }
        }


        public class ActivityCreated
        {
            public string Id { get; set; }
            public string AccountId { get; set; }

            public string Type { get; set; }

            public string Subject { get; set; }
        }

        public class EmailSignatureConsumer : IConsumer<ActivityCreated>
        {
            public Task ConsumeAsync(IntegrationEvent<ActivityCreated> messagEvent)
            {
                return Task.CompletedTask;
            }
        }

        public class TaggerConsumer : IConsumer<ActivityCreated>
        {
            public Task ConsumeAsync(IntegrationEvent<ActivityCreated> messagEvent)
            {
                return Task.CompletedTask;
            }
        }

        public class TrackerConsumer : IConsumer<DeadLetterEventDescriptor<JObject>>, IConsumer<string>
        {

            public List<IntegrationEvent<DeadLetterEventDescriptor<JObject>>> FailedProcessedIntegrationEvents { get; set; }
            public List<IntegrationEvent<string>> SuccessfullyProcessedIntegrationEvents { get; set; }
            public TrackerConsumer()
            {
                FailedProcessedIntegrationEvents = new List<IntegrationEvent<DeadLetterEventDescriptor<JObject>>>();
                SuccessfullyProcessedIntegrationEvents = new List<IntegrationEvent<string>>();
            }
            public Task ConsumeAsync(IntegrationEvent<DeadLetterEventDescriptor<JObject>> integrationEvent)
            {
                FailedProcessedIntegrationEvents.Add(integrationEvent);
                return Task.CompletedTask;
            }

            public Task ConsumeAsync(IntegrationEvent<string> integrationEvent)
            {
                SuccessfullyProcessedIntegrationEvents.Add(integrationEvent);
                return Task.CompletedTask;
            }
        }
        #endregion
    }
}
