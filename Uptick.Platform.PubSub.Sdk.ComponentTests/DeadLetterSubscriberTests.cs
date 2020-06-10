using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.ComponentTests
{
    public class DeadLetterSubscriberTests
    {
        [Fact]
        public async Task Subscribe_DeadLetterMessageIsSentToDeadLetter_DeadLetterIsReadBySubscriber()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscribe_DeadLetterMessageIsSentToDeadLetter_DeadLetterIsReadBySubscriber.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //Let's create AccountBookingTypedConsumer Subscriber

            var maxretrycount = 0;
            var accountcreatedbindingkey = "*.entity.create.account";
            var bookingcreatedbindingkey = "*.entity.create.booking";
            var accountBookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            var accountBookingTypedConsumerSubscriberName = "Subscribe_DeadLetterMessagesAreSentToDeadLetter_DeadLettersAreReadByAppropriateConsumers";
            ISubscriber typedSubscriber =
                await accountBookingCreatedSubscriberFactory
                    .CreateSubscriberAsync(accountBookingTypedConsumerSubscriberName
                        , new List<string> { accountcreatedbindingkey, bookingcreatedbindingkey }, maxretrycount);

            var accountCreatedEventType = "accountcreated";
            var bookingCreatedEventType = "bookingcreated";

            AccountBookingTypedConsumer typedConsumer = new AccountBookingTypedConsumer(10, 10);
            typedSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription<AccountCreated>(accountCreatedEventType,
                    () => typedConsumer)
                .AddDefaultSubscription<BookingCreated>(() => typedConsumer).Build());

            //Let's create AccountBookingTypedConsumer dead letter queue Subscriber
            var deadLetterSubscriberFactory = container.Resolve<IDeadLetterSubscriberFactory>();
            DeadLetterConsumer deadLetterConsumer = new DeadLetterConsumer(0, 0);
            ISubscriber deadLetterSubscriber =
                await deadLetterSubscriberFactory.CreateSubscriberAsync(accountBookingTypedConsumerSubscriberName, 1);
            deadLetterSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription<DeadLetterEventDescriptor<BookingCreated>>(bookingCreatedEventType,
                    () => deadLetterConsumer)
                .AddDefaultSubscription<DeadLetterEventDescriptor<AccountCreated>>(() => deadLetterConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string accountcreatedroutingKey = "changetracker.entity.create.account";
            string bookingcreatedroutingKey = "changetracker.entity.create.booking";

            //act
            AccountCreated accountCreated =
                new AccountCreated() { AccountLevel = 10};

            var accountCreatedIntegrationEvent = new IntegrationEvent<AccountCreated>()
            {
                Content = accountCreated,
                EventType = accountCreatedEventType,
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(accountCreatedIntegrationEvent, accountcreatedroutingKey);

            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = "bookingName"};

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = bookingCreatedEventType,
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, bookingcreatedroutingKey);
            //The event must be sent to the deadletter queue.
            await Task.Delay(500);

            //Let's check that dead letter consumer got the message.
            IntegrationEvent<DeadLetterEventDescriptor<AccountCreated>> deadLetterAccountCreatedMessage = deadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.First();
            deadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.Count().Should().Be(1);
            deadLetterAccountCreatedMessage.CorrelationId.Should().Be(accountCreatedIntegrationEvent.CorrelationId);
            deadLetterAccountCreatedMessage.Content.Original.ShouldBeEquivalentTo(accountCreatedIntegrationEvent);

            IntegrationEvent<DeadLetterEventDescriptor<BookingCreated>> deadLetterBookingCreatedMessage = deadLetterConsumer.ProcessedBookingCreatedIntegrationEvents.First();
            deadLetterConsumer.ProcessedBookingCreatedIntegrationEvents.Count().Should().Be(1);
            deadLetterBookingCreatedMessage.CorrelationId.Should().Be(bookingCreatedIntegrationEvent.CorrelationId);
            deadLetterBookingCreatedMessage.Content.Original.ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);
        }

        [Fact]
        public async Task Subscribe_DeadLetterIsReadBySubscriberWithException_DeadLetterIsSentbackToDeadLetterQueue()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscribe_DeadLetterIsReadBySubscriberWithException_DeadLetterIsSentbackToDeadLetterQueue.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //Let's create AccountBookingTypedConsumer Subscriber

            var maxretrycount = 0;
            var accountcreatedbindingkey = "*.entity.create.account";
            var bookingcreatedbindingkey = "*.entity.create.booking";
            var accountBookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            var accountBookingTypedConsumerSubscriberName = "Subscribe_DeadLetterIsReadBySubscriberWithException_DeadLetterIsSentbackToDeadLetterQueue";
            ISubscriber typedSubscriber =
                await accountBookingCreatedSubscriberFactory
                    .CreateSubscriberAsync(accountBookingTypedConsumerSubscriberName
                        , new List<string> { accountcreatedbindingkey, bookingcreatedbindingkey }, maxretrycount);

            var accountCreatedEventType = "accountcreated";
            var bookingCreatedEventType = "bookingcreated";

            AccountBookingTypedConsumer typedConsumer = new AccountBookingTypedConsumer(10, 10);
            typedSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription<AccountCreated>(accountCreatedEventType,
                    () => typedConsumer)
                .AddDefaultSubscription<BookingCreated>(() => typedConsumer).Build());


            //Let's create AccountBookingTypedConsumer dead letter queue Subscriber
            var deadLetterSubscriberFactory = container.Resolve<IDeadLetterSubscriberFactory>();
            DeadLetterConsumer deadLetterConsumer = new DeadLetterConsumer(0, 1);
            ISubscriber deadLetterSubscriber =
                await deadLetterSubscriberFactory.CreateSubscriberAsync(accountBookingTypedConsumerSubscriberName, 1);
            deadLetterSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription<DeadLetterEventDescriptor<BookingCreated>>(bookingCreatedEventType,
                    () => deadLetterConsumer)
                .AddDefaultSubscription<DeadLetterEventDescriptor<AccountCreated>>(() => deadLetterConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string accountcreatedroutingKey = "changetracker.entity.create.account";
            string bookingcreatedroutingKey = "changetracker.entity.create.booking";

            //act
            AccountCreated accountCreated =
                new AccountCreated() { AccountLevel = 10 };

            var accountCreatedIntegrationEvent = new IntegrationEvent<AccountCreated>()
            {
                Content = accountCreated,
                EventType = accountCreatedEventType,
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(accountCreatedIntegrationEvent, accountcreatedroutingKey);

            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = "bookingName" };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = bookingCreatedEventType,
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, bookingcreatedroutingKey);
            //The event must be sent to the deadletter queue.
            await Task.Delay(2000);

            //Let's check that dead letter consumer processed the message with Type = AccountCreated on the first try.
            IntegrationEvent<DeadLetterEventDescriptor<AccountCreated>> deadLetterAccountCreatedMessage = deadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.First();
            deadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.Count().Should().Be(1);
            deadLetterAccountCreatedMessage.CorrelationId.Should().Be(accountCreatedIntegrationEvent.CorrelationId);
            deadLetterAccountCreatedMessage.Content.Original.ShouldBeEquivalentTo(accountCreatedIntegrationEvent);
            deadLetterConsumer.CountOfAttemptsForAccountCreated.Should().Be(1);

            //Let's check that dead letter consumer processed the message with Type = AccountCreated on the second try.
            IntegrationEvent<DeadLetterEventDescriptor<BookingCreated>> deadLetterBookingCreatedMessage = deadLetterConsumer.ProcessedBookingCreatedIntegrationEvents.First();
            deadLetterConsumer.ProcessedBookingCreatedIntegrationEvents.Count().Should().Be(1);
            deadLetterBookingCreatedMessage.CorrelationId.Should().Be(bookingCreatedIntegrationEvent.CorrelationId);
            deadLetterBookingCreatedMessage.Content.Original.ShouldBeEquivalentTo(bookingCreatedIntegrationEvent);
            deadLetterConsumer.CountOfAttemptsForBookingCreated.Should().Be(2);
        }

        [Fact]
        public async Task Subscribe_DeadLetterIsSibscribedThenDisposed_DeadLetterMessagesAreDeliveredCorrectlyToSecondSubscriber()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscribe_DeadLetterIsSibscribedThenUnsibcribed_DeadLetterMessagesAreDeliveredCorrectly.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //Let's create AccountBookingTypedConsumer Subscriber

            var maxretrycount = 0;
            var accountcreatedbindingkey = "*.entity.create.account";

            var accountCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            var accountTypedConsumerSubscriberName = "Subscribe_DeadLetterIsSibscribedThenUnsibcribed_DeadLetterMessagesAreDeliveredCorrectly";
            ISubscriber typedSubscriber =
                await accountCreatedSubscriberFactory
                    .CreateSubscriberAsync(accountTypedConsumerSubscriberName
                        , new List<string> { accountcreatedbindingkey}, maxretrycount);

            var accountCreatedEventType = "accountcreated";

            AccountBookingTypedConsumer typedConsumer = new AccountBookingTypedConsumer(10, 0);
            typedSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription<AccountCreated>(accountCreatedEventType,() => typedConsumer).Build());

            //Let's create AccountTypedConsumer for dead letter queue Subscriber
            var deadLetterSubscriberFactory = container.Resolve<IDeadLetterSubscriberFactory>();
            DeadLetterConsumer deadLetterConsumer = new DeadLetterConsumer(0, 0);
            ISubscriber deadLetterSubscriber =
                await deadLetterSubscriberFactory.CreateSubscriberAsync(accountTypedConsumerSubscriberName, 1);
            deadLetterSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddDefaultSubscription<DeadLetterEventDescriptor<AccountCreated>>(() => deadLetterConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string accountcreatedroutingKey = "changetracker.entity.create.account";

            //act
            AccountCreated accountCreated =
                new AccountCreated() { AccountLevel = 10 };

            var accountCreatedIntegrationEvent = new IntegrationEvent<AccountCreated>()
            {
                Content = accountCreated,
                EventType = accountCreatedEventType,
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(accountCreatedIntegrationEvent, accountcreatedroutingKey);

            //The event must be sent to the deadletter queue.
            await Task.Delay(1000);

            //Let's check that dead letter consumer processed the message with Type = AccountCreated on the first try.
            IntegrationEvent<DeadLetterEventDescriptor<AccountCreated>> deadLetterAccountCreatedMessage = deadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.First();
            deadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.Count().Should().Be(1);
            deadLetterAccountCreatedMessage.CorrelationId.Should().Be(accountCreatedIntegrationEvent.CorrelationId);
            deadLetterAccountCreatedMessage.Content.Original.ShouldBeEquivalentTo(accountCreatedIntegrationEvent);
            deadLetterConsumer.CountOfAttemptsForAccountCreated.Should().Be(1);

            //Let's dispose
            deadLetterSubscriber.Dispose();
            //Let's publish the new message that must be sent to the dead letter queue
            AccountCreated uptickAccountCreated =
                new AccountCreated() { AccountLevel = 20 };

            var uptickAccountCreatedIntegrationEvent = new IntegrationEvent<AccountCreated>()
            {
                Content = uptickAccountCreated,
                EventType = accountCreatedEventType,
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(uptickAccountCreatedIntegrationEvent, accountcreatedroutingKey);

            //Let's create the new Dead Letter queue Subscriber
            DeadLetterConsumer secondDeadLetterConsumer = new DeadLetterConsumer(0, 0);
            ISubscriber secondDeadLetterSubscriber =
                await deadLetterSubscriberFactory.CreateSubscriberAsync(accountTypedConsumerSubscriberName, 1);
            secondDeadLetterSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddDefaultSubscription<DeadLetterEventDescriptor<AccountCreated>>(() => secondDeadLetterConsumer).Build());

            await Task.Delay(1000);

            //check that the second dead letter qeuee message was sent to the new second consumer. The first should not have recieved new message.

            deadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.Count().Should().Be(1);
            deadLetterAccountCreatedMessage.CorrelationId.Should().Be(accountCreatedIntegrationEvent.CorrelationId);
            deadLetterAccountCreatedMessage.Content.Original.ShouldBeEquivalentTo(accountCreatedIntegrationEvent);
            deadLetterConsumer.CountOfAttemptsForAccountCreated.Should().Be(1);

            IntegrationEvent<DeadLetterEventDescriptor<AccountCreated>> uptickAccountDeadLetterAccountCreatedMessage = secondDeadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.First();
            secondDeadLetterConsumer.ProcessedAccountCreatedIntegrationEvents.Count().Should().Be(1);
            uptickAccountDeadLetterAccountCreatedMessage.CorrelationId.Should().Be(uptickAccountCreatedIntegrationEvent.CorrelationId);
            uptickAccountDeadLetterAccountCreatedMessage.Content.Original.ShouldBeEquivalentTo(uptickAccountCreatedIntegrationEvent);
            secondDeadLetterConsumer.CountOfAttemptsForAccountCreated.Should().Be(1);

        }

        [Fact]
        public async Task Subscribe_DeadLetterMessageIsSentToDeadLetter_DeadLetterIsReadByDeadLetterReloaderAndSentToTheOriginalQueue()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "Subscribe_DeadLetterMessageIsSentToDeadLetter_DeadLetterIsReadByDeadLetterReloaderAndSentToTheOriginalQueue.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //Let's create AccountBookingTypedConsumer Subscriber

            var maxretrycount = 0;
            var accountcreatedbindingkey = "*.entity.create.account";
            var bookingcreatedbindingkey = "*.entity.create.booking";
            var accountBookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            var accountBookingTypedConsumerSubscriberName = "Subscribe_DeadLetterMessageIsSentToDeadLetter_DeadLetterIsReadByDeadLetterReloaderAndSentToTheOriginalQueue";
            ISubscriber typedSubscriber =
                await accountBookingCreatedSubscriberFactory
                    .CreateSubscriberAsync(accountBookingTypedConsumerSubscriberName
                        , new List<string> { accountcreatedbindingkey, bookingcreatedbindingkey }, maxretrycount);

            var accountCreatedEventType = "accountcreated";
            var bookingCreatedEventType = "bookingcreated";

            AccountBookingTypedConsumer typedConsumer = new AccountBookingTypedConsumer(1, 1);
            typedSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription<AccountCreated>(accountCreatedEventType,
                    () => typedConsumer)
                .AddDefaultSubscription<BookingCreated>(() => typedConsumer).Build());

            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string accountcreatedroutingKey = "changetracker.entity.create.account";
            string bookingcreatedroutingKey = "changetracker.entity.create.booking";

            //Let's create AccountBookingTypedConsumer dead letter queue Subscriber and use DeadLetterReloader
            var deadLetterSubscriberFactory = container.Resolve<IDeadLetterSubscriberFactory>();

            DeadLetterReloader<AccountCreated> deadLetterreloader = new DeadLetterReloader<AccountCreated>(publisher, accountBookingTypedConsumerSubscriberName);


            ISubscriber deadLetterSubscriber =
                await deadLetterSubscriberFactory.CreateSubscriberAsync(accountBookingTypedConsumerSubscriberName, 1);
            deadLetterSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddDefaultSubscription(() => deadLetterreloader).Build());

            //act
            AccountCreated accountCreated =
                new AccountCreated() { AccountLevel = 10 };

            var accountCreatedIntegrationEvent = new IntegrationEvent<AccountCreated>()
            {
                Content = accountCreated,
                EventType = accountCreatedEventType,
                CorrelationId = Guid.NewGuid()
            };
            await publisher.PublishEventAsync(accountCreatedIntegrationEvent, accountcreatedroutingKey);

            //The event must be sent to the deadletter queue.
            await Task.Delay(2000);

            //Let's check that dead letter reloader processed the message with Type = AccountCreated and sent it to the original queue.
            //AccountBookingTypedConsumer should process the original message at the second attempt.
            IntegrationEvent<AccountCreated> accountCreatedMessage =
                typedConsumer.ProcessedAccountCreatedIntegrationEvent;

            accountCreatedMessage.CorrelationId.Should().Be(accountCreatedIntegrationEvent.CorrelationId);
            accountCreatedMessage.Content.ShouldBeEquivalentTo(accountCreatedIntegrationEvent.Content);
            typedConsumer.CountOfAttemptsForAccountCreated.Should().Be(2);
            typedConsumer.CountOfAttemptsForBookingCreated.Should().Be(0);
        }
    }

    #region Helpers
    public class AccountCreated
    {
        public int AccountLevel { get; set; }
    }

    public class BookingCreated
    {
        public string BookingName { get; set; }
    }

    public class AccountBookingTypedConsumer : IConsumer<AccountCreated>, IConsumer<BookingCreated>
    {
        private readonly int _countOfAttemptsBeforeSuccessForAccountCreated;

        private readonly int _countOfAttemptsBeforeSuccessForBookingCreated;
        public int CountOfAttemptsForAccountCreated { get; set; }
        public int CountOfAttemptsForBookingCreated { get; set; }

        public IntegrationEvent<AccountCreated> ProcessedAccountCreatedIntegrationEvent { get; set; }

        public IntegrationEvent<BookingCreated> ProcessedBookingCreatedIntegrationEvent { get; set; }
        public AccountBookingTypedConsumer(int countOfAttemptsBeforeSuccessForAccountCreated, int countOfAttemptsBeforeSuccessForBookingCreated)
        {
            _countOfAttemptsBeforeSuccessForAccountCreated = countOfAttemptsBeforeSuccessForAccountCreated;
            _countOfAttemptsBeforeSuccessForBookingCreated = countOfAttemptsBeforeSuccessForBookingCreated;
            CountOfAttemptsForAccountCreated = 0;
            CountOfAttemptsForBookingCreated = 0;
        }
        public async Task ConsumeAsync(IntegrationEvent<AccountCreated> integrationEvent)
        {
            await Task.Delay(50);
            CountOfAttemptsForAccountCreated = CountOfAttemptsForAccountCreated + 1;
            if (CountOfAttemptsForAccountCreated <= _countOfAttemptsBeforeSuccessForAccountCreated)
            {
                throw new Exception();
            }

            ProcessedAccountCreatedIntegrationEvent = integrationEvent;
        }

        public async Task ConsumeAsync(IntegrationEvent<BookingCreated> integrationEvent)
        {
            await Task.Delay(50);
            CountOfAttemptsForBookingCreated = CountOfAttemptsForBookingCreated + 1;
            if (CountOfAttemptsForBookingCreated <= _countOfAttemptsBeforeSuccessForBookingCreated)
            {
                throw new Exception();
            }

            ProcessedBookingCreatedIntegrationEvent = integrationEvent;
        }
    }

    public class DeadLetterConsumer : IConsumer<DeadLetterEventDescriptor<AccountCreated>> , IConsumer<DeadLetterEventDescriptor<BookingCreated>>
    {
        private readonly int _countOfAttemptsBeforeSuccessForAccountCreated;

        private readonly int _countOfAttemptsBeforeSuccessForBookingCreated;
        public int CountOfAttemptsForAccountCreated { get; set; }
        public int CountOfAttemptsForBookingCreated { get; set; }

        public List<IntegrationEvent<DeadLetterEventDescriptor<AccountCreated>>> ProcessedAccountCreatedIntegrationEvents { get; set; }
        public List<IntegrationEvent<DeadLetterEventDescriptor<BookingCreated>>> ProcessedBookingCreatedIntegrationEvents { get; set; }
        public DeadLetterConsumer(int countOfAttemptsBeforeSuccessForAccountCreated, int countOfAttemptsBeforeSuccessForBookingCreated)
        {
            _countOfAttemptsBeforeSuccessForAccountCreated = countOfAttemptsBeforeSuccessForAccountCreated;
            _countOfAttemptsBeforeSuccessForBookingCreated = countOfAttemptsBeforeSuccessForBookingCreated;
            CountOfAttemptsForAccountCreated = 0;
            CountOfAttemptsForBookingCreated = 0;
            ProcessedAccountCreatedIntegrationEvents = new List<IntegrationEvent<DeadLetterEventDescriptor<AccountCreated>>>();
            ProcessedBookingCreatedIntegrationEvents = new List<IntegrationEvent<DeadLetterEventDescriptor<BookingCreated>>>();
        }
        public Task ConsumeAsync(IntegrationEvent<DeadLetterEventDescriptor<AccountCreated>> integrationEvent)
        {
            CountOfAttemptsForAccountCreated = CountOfAttemptsForAccountCreated + 1;
            if (CountOfAttemptsForAccountCreated <= _countOfAttemptsBeforeSuccessForAccountCreated)
            {
                throw new Exception();
            }
            ProcessedAccountCreatedIntegrationEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }

        public Task ConsumeAsync(IntegrationEvent<DeadLetterEventDescriptor<BookingCreated>> integrationEvent)
        {
            CountOfAttemptsForBookingCreated = CountOfAttemptsForBookingCreated + 1;
            if (CountOfAttemptsForBookingCreated <= _countOfAttemptsBeforeSuccessForBookingCreated)
            {
                throw new Exception();
            }

            ProcessedBookingCreatedIntegrationEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    #endregion
}
