using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.Tracker
{
    public class DefaultTrackerFactoryTests
    {
        private readonly Mock<ISubscriberFactory> _subscriberFactory;
        private readonly Mock<ITrackerNamingConventionController> _trackerNamingConvensionController;
        private readonly Mock<IModelNamingConventionController> _modelNamingConventionController;
        private readonly Func<DefaultTrackerFactory> _createDefaultTrackerFactoryFunc;

        public DefaultTrackerFactoryTests()
        {
            _subscriberFactory = new Mock<ISubscriberFactory>();
            _trackerNamingConvensionController = new Mock<ITrackerNamingConventionController>();
            _modelNamingConventionController = new Mock<IModelNamingConventionController>();

            _createDefaultTrackerFactoryFunc = () => new DefaultTrackerFactory(_subscriberFactory.Object, _trackerNamingConvensionController.Object,
                _modelNamingConventionController.Object);
        }

        [Fact]
        public void CreateTrackerAsync_NameIsInvalid_Should_throw_exception()
        {
            //act
            Func<Task> f = async () =>
            {
                await _createDefaultTrackerFactoryFunc().CreateTrackerAsync(null, false, new List<string> { "targetSubscriberName" }, 0, 10);
            };

            //check
            f.ShouldThrow<ArgumentException>("name argument can not be null or empty");
        }

        [Fact]
        public void CreateTrackerAsync_TargetSubscribersListIsEmpty_Should_throw_exception()
        {
            //act
            Func<Task> f = async () =>
            {
                await _createDefaultTrackerFactoryFunc().CreateTrackerAsync("servicecontroller", false, new List<string>(), 0, 10);
            };

            //check
            f.ShouldThrow<ArgumentException>("targetSubscriberNames argument can not be null or empty");
        }

        [Fact]
        public void CreateTrackerAsync_TargetSubscribersListIsNull_Should_throw_exception()
        {
            //act
            Func<Task> f = async () =>
            {
                await _createDefaultTrackerFactoryFunc().CreateTrackerAsync("servicecontroller", false, null, 0, 10);
            };

            //check
            f.ShouldThrow<ArgumentException>("targetSubscriberNames argument can not be null or empty");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void CreateTrackerAsync_CorrectParametersAreProvided_TrackerMustBeCreated(bool temporary)
        {
            //prepare
            var trackername = "trackername";
            var subscriber1 = "subscriber1";
            var subscriber2 = "subscriber2";
            var retrycount = 10;
            var prefetchcount = 5;

            var targetSubscriberNames = new List<string>() {subscriber1, subscriber2};

            var trackingRoutingKeys = new List<string>
            {
                string.Concat("ok.", subscriber1),
                string.Concat("ok.", subscriber2),
                string.Concat("deadletter.", subscriber1),
                string.Concat("deadletter.", subscriber2)
            };

            _trackerNamingConvensionController.Setup(x => x.GetTrackerQueueName(trackername))
                .Returns((string queueName) => string.Concat("tracking.", trackername));

            _trackerNamingConvensionController
                .Setup(x => x.GetTrackerQueueRoutingKeys(new List<string> {subscriber1, subscriber2}))
                .Returns((IEnumerable<string> subscribersNames) => new List<string>(trackingRoutingKeys));

            var mockSubscriber = new Mock<ISubscriber>();



            var subscriberFactoryHasBeenCalledWithCorrectParameters = false;

            _subscriberFactory.Setup(x => x.CreateSubscriberAsync(string.Concat("tracking.", trackername), temporary,
                    trackingRoutingKeys, retrycount,
                    prefetchcount, It.IsAny<TrackingEventsSubscriptionSelector>()))
                .Returns((string name, bool temp, IEnumerable<string> routings, int retry, int prefetch,
                    ISubscriptionSelector subscriptionSelector) =>
                {
                    subscriberFactoryHasBeenCalledWithCorrectParameters = true;
                    return Task.FromResult(mockSubscriber.Object);
                });

            //act

            ITracker tracker =
                await _createDefaultTrackerFactoryFunc()
                    .CreateTrackerAsync(trackername, temporary, targetSubscriberNames, retrycount, prefetchcount);

            //check
            subscriberFactoryHasBeenCalledWithCorrectParameters.Should().BeTrue();
            DefaultTracker actualTracker = (DefaultTracker)tracker;
            actualTracker.Should().NotBeNull();
        }

    }
}
