using System;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ.Tests
{
    public class EasyNetQPublisherTests
    {

        [Theory]
        [InlineData(true, 2)]
        [InlineData(false, 1)]
        public async void PublishAsync_MessageMustBeSerializedAndCorrectlyRouted(bool persistant, byte deliveryMode)
        {
            //prepare

            var mockBus = new Mock<IAdvancedBus>();

            var mockExchange = new Mock<IExchange>();

            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();

            var routingKey = "routingKey";

            IExchange actualExchange = null;
            string actualRouting = string.Empty;
            bool actualMandatory = true;
            MessageProperties actualProperties = null;
            byte[] actualMsg = null;

            mockBus.Setup(x => x.PublishAsync(mockExchange.Object, routingKey, false, 
                It.IsAny<MessageProperties>(), It.IsAny<byte[]>()))
                .Returns((IExchange exchange, string routing, bool mandatory,
                    MessageProperties properties, byte[] msg) =>
                { return Task.FromResult(true); })
                .Callback<IExchange, string, bool, MessageProperties, byte[]>(
                (exchange, routing, mandatory, properties, msg) =>
                {
                    actualExchange = exchange;
                    actualRouting = routing;
                    actualMandatory = mandatory;
                    actualProperties = properties;
                    actualMsg = msg;
                });

            mockBus.Setup(x => x.ExchangeDeclareAsync("exchangename", ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                        bool passive, bool durable, bool autoDelete, bool internalflag,
                        string alternateExchange, bool delayed) =>
                    { return Task.FromResult(mockExchange.Object); });

            //act
            EasyNetQPublisher easyNetQPublisher =
                new EasyNetQPublisher(mockBus.Object, "exchangename", mockModelNamingConventionController.Object);

            BookingCreated bookingCreated =
                new BookingCreated() { BookingName = string.Concat("Microsoft Sale",
                Guid.NewGuid().ToString()) };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };
            await easyNetQPublisher.PublishEventAsync(bookingCreatedIntegrationEvent, 
                routingKey, persistant);



            //check
            actualExchange.Should().Be(mockExchange.Object);
            actualRouting.Should().Be(routingKey);
            actualMandatory.Should().Be(false);
            actualProperties.ShouldBeEquivalentTo(new MessageProperties() { DeliveryMode = deliveryMode });
            var messagejson = JsonConvert.SerializeObject(bookingCreatedIntegrationEvent);
            var messagebytes = Encoding.UTF8.GetBytes(messagejson);
            actualMsg.ShouldBeEquivalentTo(messagebytes);
        }

        [Theory]
        [InlineData(true, 2)]
        [InlineData(false, 1)]
        public async void PublishDirectEventAsync_MessageMustBeSerializedAndCorrectlyRouted(bool persistant, byte deliveryMode)
        {
            //prepare

            var mockBus = new Mock<IAdvancedBus>();

            var mockExchange = new Mock<IExchange>();

            var mockModelNamingConventionController = new Mock<IModelNamingConventionController>();

            var subscriberName = "subscriber";
            var routingKey = string.Concat("direct.", subscriberName); ;

            IExchange actualExchange = null;
            string actualRouting = string.Empty;
            bool actualMandatory = true;
            MessageProperties actualProperties = null;
            byte[] actualMsg = null;

            mockModelNamingConventionController.Setup(x => x.GetDirectRoutingKey(subscriberName)).Returns(routingKey);

            mockBus.Setup(x => x.PublishAsync(mockExchange.Object, routingKey, false,
                It.IsAny<MessageProperties>(), It.IsAny<byte[]>()))
                .Returns((IExchange exchange, string routing, bool mandatory,
                    MessageProperties properties, byte[] msg) =>
                { return Task.FromResult(true); })
                .Callback<IExchange, string, bool, MessageProperties, byte[]>(
                (exchange, routing, mandatory, properties, msg) =>
                {
                    actualExchange = exchange;
                    actualRouting = routing;
                    actualMandatory = mandatory;
                    actualProperties = properties;
                    actualMsg = msg;
                });

            mockBus.Setup(x => x.ExchangeDeclareAsync("exchangename", ExchangeType.Topic,
                    false, true, false, false, null, false))
                .Returns((string exchangename, string exchanegType,
                        bool passive, bool durable, bool autoDelete, bool internalflag,
                        string alternateExchange, bool delayed) =>
                { return Task.FromResult(mockExchange.Object); });

            //act
            EasyNetQPublisher easyNetQPublisher =
                new EasyNetQPublisher(mockBus.Object, "exchangename", mockModelNamingConventionController.Object);

            BookingCreated bookingCreated =
                new BookingCreated()
                {
                    BookingName = string.Concat("Microsoft Sale",
                Guid.NewGuid().ToString())
                };

            var bookingCreatedIntegrationEvent = new IntegrationEvent<BookingCreated>()
            {
                Content = bookingCreated,
                EventType = "bookingcreated"
            };
            await easyNetQPublisher.PublishDirectEventAsync(bookingCreatedIntegrationEvent,
                subscriberName, persistant);



            //check
            actualExchange.Should().Be(mockExchange.Object);
            actualRouting.Should().Be(routingKey);
            actualMandatory.Should().Be(false);
            actualProperties.ShouldBeEquivalentTo(new MessageProperties() { DeliveryMode = deliveryMode });
            var messagejson = JsonConvert.SerializeObject(bookingCreatedIntegrationEvent);
            var messagebytes = Encoding.UTF8.GetBytes(messagejson);
            actualMsg.ShouldBeEquivalentTo(messagebytes);
        }

        #region Helpers
        public class BookingCreated
        {
            public string BookingName { get; set; }
        }
        #endregion
    }
}
