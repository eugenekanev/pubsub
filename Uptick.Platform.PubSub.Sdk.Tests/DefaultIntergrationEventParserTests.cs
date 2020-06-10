using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Tests
{
    public class DefaultIntergrationEventParserTests1
    {
        [Fact]
        public void Parse_AllTheFieldsCorrect_SuccessShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntegrationEvent<BookingCreated>
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                EventCreationDate = DateTime.UtcNow,
                EventType = "bookingCreated",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeTrue();
            result.IntegrationEvent.ShouldBeEquivalentTo(bookingCreatedEvent);
            result.Errors.Count.Should().Be(0);
        }


        [Fact]
        public void Parse_EventIdFieldIsMissed_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntergrationEventWithoutEventId<BookingCreated>()
            {
                CorrelationId = Guid.NewGuid(),
                EventCreationDate = DateTime.UtcNow,
                EventType = "bookingCreated",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("EventId token is missed");
        }

        [Fact]
        public void Parse_EventIdFieldIsNotGuid_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntergrationEventWithNonGuidEventId<BookingCreated>()
            {
                EventId = 123,
                CorrelationId = Guid.NewGuid(),
                EventCreationDate = DateTime.UtcNow,
                EventType = "bookingCreated",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("EventId is not GUID");
        }

        [Fact]
        public void Parse_CorrelationIdFieldIsMissed_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntergrationEventWithoutCorrelationId<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                EventCreationDate = DateTime.UtcNow,
                EventType = "bookingCreated",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("CorrelationId token is missed");
        }

        [Fact]
        public void Parse_CorrelationIdFieldIsNotGuid_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntergrationEventWithNonGuidCorrelationId<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                CorrelationId = 123,
                EventCreationDate = DateTime.UtcNow,
                EventType = "bookingCreated",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("CorrelationId is not GUID");
        }

        [Fact]
        public void Parse_CorrelationIdFieldIsEmpty_SuccessShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntegrationEvent<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                CorrelationId = null,
                EventCreationDate = DateTime.UtcNow,
                EventType = "bookingCreated",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeTrue();
            result.IntegrationEvent.ShouldBeEquivalentTo(bookingCreatedEvent);
            result.Errors.Count.Should().Be(0);
        }

        [Fact]
        public void Parse_EventCreationDateFieldIsMissed_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntergrationEventWithoutEventCreationDate<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                EventType = "bookingCreated",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("EventCreationDate token is missed");
        }

        [Fact]
        public void Parse_EventCreationDateFieldIsNotDateTime_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntergrationEventWithNonDateTimeEventCreationDate<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                EventCreationDate = 123,
                EventType = "bookingCreated",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("EventCreationDate is not DateTime");
        }

        [Fact]
        public void Parse_EventTypeFieldIsMissed_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntergrationEventWithoutEventType<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                EventCreationDate = DateTime.UtcNow,
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("EventType token is missed");
        }

        [Fact]
        public void Parse_EventTypeFieldIsEmpty_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntegrationEvent<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                EventCreationDate = DateTime.UtcNow,
                EventType = "",
                Version = "0.1",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("EventType can not be empty");
        }

        [Fact]
        public void Parse_VersionFieldIsMissed_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntergrationEventWithoutVersion<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                EventCreationDate = DateTime.UtcNow,
                EventType = "bookingcreated",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("Version token is missed");
        }

        [Fact]
        public void Parse_VersionFieldIsEmpty_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent = new IntegrationEvent<BookingCreated>()
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                EventCreationDate = DateTime.UtcNow,
                EventType = "bookingcreated",
                Version = "",
                Content = new BookingCreated() { BookingName = "bookingName" }
            };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.First().Should().Be("Version can not be empty");
        }

        [Fact]
        public void Parse_CorrelationIdFieldIsMissedEventCreationDateIsNotDateTime_ErrorShouldBeReturned()
        {
            //prepare
            var bookingCreatedEvent =
                new IntergrationEventWithoutCorrelationIdAndWrongEventCreationDate<BookingCreated>()
                {
                    EventId = Guid.NewGuid(),
                    EventCreationDate = 13,
                    EventType = "bookingcreated",
                    Version = "0.1",
                    Content = new BookingCreated() { BookingName = "bookingName" }
                };

            //act 
            IntergrationEventParsingResult result =
                DefaultIntergrationEventParser.Parse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bookingCreatedEvent)));

            //check
            result.Success.Should().BeFalse();
            result.IntegrationEvent.Should().BeNull();
            result.Errors.Count.Should().Be(2);
            result.Errors[0].Should().Be("CorrelationId token is missed");
            result.Errors[1].Should().Be("EventCreationDate is not DateTime");
        }

        #region helpers

        public class IntergrationEventWithoutEventId<T>
        {

            public Guid? CorrelationId { get; set; }
            public DateTime EventCreationDate { get; set; }

            public string EventType { get; set; }

            public string Version { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithNonGuidEventId<T>
        {
            public int EventId { get; set; }
            public Guid? CorrelationId { get; set; }
            public DateTime EventCreationDate { get; set; }

            public string EventType { get; set; }

            public string Version { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithoutCorrelationId<T>
        {
            public Guid EventId { get; set; }
            public DateTime EventCreationDate { get; set; }

            public string EventType { get; set; }

            public string Version { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithNonGuidCorrelationId<T>
        {
            public Guid EventId { get; set; }
            public int CorrelationId { get; set; }
            public DateTime EventCreationDate { get; set; }

            public string EventType { get; set; }

            public string Version { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithoutEventCreationDate<T>
        {
            public Guid EventId { get; set; }
            public Guid? CorrelationId { get; set; }

            public string EventType { get; set; }

            public string Version { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithNonDateTimeEventCreationDate<T>
        {
            public Guid EventId { get; set; }
            public Guid? CorrelationId { get; set; }
            public int EventCreationDate { get; set; }

            public string EventType { get; set; }

            public string Version { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithoutEventType<T>
        {
            public Guid EventId { get; set; }
            public Guid? CorrelationId { get; set; }

            public DateTime EventCreationDate { get; set; }


            public string Version { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithoutVersion<T>
        {
            public Guid EventId { get; set; }
            public Guid? CorrelationId { get; set; }

            public DateTime EventCreationDate { get; set; }

            public string EventType { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithoutCorrelationIdAndWrongEventCreationDate<T>
        {
            public Guid EventId { get; set; }
            public int EventCreationDate { get; set; }

            public string EventType { get; set; }

            public string Version { get; set; }

            public T Content { get; set; }
        }

        public class IntergrationEventWithoutContent
        {
            public Guid EventId { get; set; }
            public Guid? CorrelationId { get; set; }

            public DateTime EventCreationDate { get; set; }

            public string EventType { get; set; }

            public string Version { get; set; }
        }

        public class BookingCreated
        {
            [JsonRequired]
            public string BookingName { get; set; }
        }

        public class WrongBookingCreated
        {
            public string Name { get; set; }
        }


        #endregion
    }
}
