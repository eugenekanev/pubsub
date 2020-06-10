using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using EasyNetQ;
using EasyNetQ.Topology;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Uptick.Platform.PubSub.Sdk.Management;
using Xunit;
using ExchangeType = RabbitMQ.Client.ExchangeType;
using IContainer = Autofac.IContainer;

namespace Uptick.Platform.PubSub.Sdk.ComponentTests
{
    public class ManagementTests
    {
        [Fact]
        public async Task QueueInfoProvider_GetQueueInfo_CorrectCountOfReadyMessagesMustBeReturned()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "QueueInfoProvider_GetQueueInfo_CorrectCountOfReadyMessagesMustBeReturned.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");


            var maxretrycount = 2;

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

            //create Subscriber
            var bookingCreatedSubscriberFactory = container.Resolve<ISubscriberFactory>();
            ISubscriber fastSubscriber =
                await bookingCreatedSubscriberFactory
                .CreateSubscriberAsync("QueueInfoProvider_GetQueueInfo_CorrectCountOfReadyMessagesMustBeReturned" +
                                       ".fast", new List<string>
                    {
                        "*.entity.create.booking",
                    }, maxretrycount, 5);

            ISubscriber slowSubscriber =
                await bookingCreatedSubscriberFactory
                    .CreateSubscriberAsync("QueueInfoProvider_GetQueueInfo_CorrectCountOfReadyMessagesMustBeReturned" +
                                           ".slow", new List<string>
                    {
                        "*.entity.create.booking",
                    }, maxretrycount, 1);


            //create Publisher

            var publisher = container.Resolve<IPublisher>();

            string bookingRoutingKey = "changetracker.entity.create.booking";


            //act
            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale", Guid.NewGuid().ToString()) };


            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };

            for (int i = 1; i <= 8; i++)
            {
                await publisher.PublishEventAsync(bookingCreatedIntegrationEvent, bookingRoutingKey);
            }


            //wait 1 second
            await Task.Delay(1000);

            //ckeck the GetQueueInfo method first call
            var queueInfoProvider = container.Resolve<IQueueInfoProvider>();
            var fastSubscriberQueueSnapshotInfo =  queueInfoProvider.GetQueueInfo(
                "QueueInfoProvider_GetQueueInfo_CorrectCountOfReadyMessagesMustBeReturned.fast");
            fastSubscriberQueueSnapshotInfo.CountOfMessages.Should().Be(8);

            var slowSubscriberQueueSnapshotInfo = queueInfoProvider.GetQueueInfo(
                "QueueInfoProvider_GetQueueInfo_CorrectCountOfReadyMessagesMustBeReturned.slow");
            slowSubscriberQueueSnapshotInfo.CountOfMessages.Should().Be(8);

            //Let's create Subscriber

            var fastBookingTypedSubscriberConsumer = new BookingTypedParallellismCounterConsumer(500);
            var slowBookingTypedSubscriberConsumer = new BookingTypedParallellismCounterConsumer(500);
            fastSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingcreated", () => fastBookingTypedSubscriberConsumer)
               .Build());

            slowSubscriber.Subscribe(SubscriptionBuilder.Create()
                .AddSubscription("bookingcreated", () => slowBookingTypedSubscriberConsumer)
                .Build());

            //wait 10 seconds
            await Task.Delay(10000);

            //check

            //ckeck the GetQueueInfo method second call
            fastSubscriberQueueSnapshotInfo = queueInfoProvider.GetQueueInfo(
                "QueueInfoProvider_GetQueueInfo_CorrectCountOfReadyMessagesMustBeReturned.fast");
            fastSubscriberQueueSnapshotInfo.CountOfMessages.Should().Be(0);

            slowSubscriberQueueSnapshotInfo = queueInfoProvider.GetQueueInfo(
                "QueueInfoProvider_GetQueueInfo_CorrectCountOfReadyMessagesMustBeReturned.slow");
            slowSubscriberQueueSnapshotInfo.CountOfMessages.Should().Be(0);

            container.Dispose();

        }

        [Fact]
        public void QueueInfoProvider_GetQueueInfo_UnknownQueueExceptionShouldBeThrown()
        {
            //prepare
            IConfiguration configuration = ConfigurationHelper.ProvideConfiguration();
            var exchangeName =
                "QueueInfoProvider_GetQueueInfo_UnknownQueueExceptionShouldBeThrown.exchangename";
            configuration["rabbitmq:exchangename"] = exchangeName;
            configuration["rabbitmq:waitexchangename"] = exchangeName.Replace("exchangename", "waitexchangename");

            IContainer container = ConfigurationHelper.ConfigureContainer(configuration);

           
            var queueInfoProvider = container.Resolve<IQueueInfoProvider>();
            //act
            Action f = () =>
            {
                queueInfoProvider.GetQueueInfo(
                    "uknownsubscriber"); 
            };

            //check
            f.ShouldThrow<Exception>();

        }

        #region helpers
        public class BookingCreated
        {
            public string BookingName { get; set; }
        }
        public class BookingTypedParallellismCounterConsumer : IConsumer<BookingCreated>
        {
            private readonly int _delayForConsuming;
            private object _guard = new object();

            public int MaxParrelelEvents = 0;
            public int CountOfProcessedEvents = 0;

            private int _currentParrelelEvents = 0;

            public BookingTypedParallellismCounterConsumer(int delayForConsuming)
            {
                _delayForConsuming = delayForConsuming;
            }
            public async Task ConsumeAsync(IntegrationEvent<BookingCreated> integrationEvent)
            {
                lock (_guard)
                {
                    _currentParrelelEvents = _currentParrelelEvents + 1;
                    if (_currentParrelelEvents > MaxParrelelEvents)
                    {
                        MaxParrelelEvents = _currentParrelelEvents;
                    }
                }

                await Task.Delay(_delayForConsuming);

                lock (_guard)
                {
                    _currentParrelelEvents = _currentParrelelEvents - 1;
                    CountOfProcessedEvents = CountOfProcessedEvents + 1;
                }
            }
        }
        #endregion
    }
}
