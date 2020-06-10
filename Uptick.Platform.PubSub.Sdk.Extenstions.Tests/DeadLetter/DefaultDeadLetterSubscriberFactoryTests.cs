using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Uptick.Platform.PubSub.Sdk.Extenstions.DeadLetter;
using Uptick.Platform.PubSub.Sdk.Extenstions.Tracker;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.DeadLetter
{
    public class DefaultDeadLetterSubscriberFactoryTests
    {
        private readonly Mock<ISubscriberFactory> _subscriberFactory;
        private readonly Mock<IModelNamingConventionController> _modelNamingConventionController;
        private readonly Func<DefaultDeadLetterSubscriberFactory> _DefaultDeadLetterSubscriberFactoryFunc;
        private readonly Mock<IPublisher> _publisher;

        public DefaultDeadLetterSubscriberFactoryTests()
        {
            _subscriberFactory = new Mock<ISubscriberFactory>();
            _modelNamingConventionController = new Mock<IModelNamingConventionController>();
            _publisher = new Mock<IPublisher>();

            _DefaultDeadLetterSubscriberFactoryFunc = () => new DefaultDeadLetterSubscriberFactory(_subscriberFactory.Object,
                _modelNamingConventionController.Object, _publisher.Object);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void CreateSubscriberAsync_NameIsInvalid_Should_throw_exception(string subscriberName)
        {
            //act
            Func<Task> f = async () =>
            {
                await _DefaultDeadLetterSubscriberFactoryFunc().CreateSubscriberAsync(subscriberName, 10, false);
            };

            //check
            f.ShouldThrow<ArgumentException>("targetSubscriber argument can not be null or empty");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void CreateSubscriberAsync_CorrectParametersAreProvided_DefaultDeadLetterSubscriberMustBeCreated(bool temporary)
        {
            //prepare
      
            var subscriberName = "subscriber1";
            var prefetchcount = 5;

            _modelNamingConventionController.Setup(x=> x.GetDeadLetterQueueName(subscriberName))
                .Returns((string queueName) => string.Concat("deadletter.", queueName));


            var mockSubscriber = new Mock<ISubscriber>();

            var subscriberFactoryHasBeenCalledWithCorrectParameters = false;

            _subscriberFactory.Setup(x => x.CreateSubscriberAsync(string.Concat("deadletter.", subscriberName), temporary,
                    null, 0,prefetchcount, false, It.IsAny<DeadLetterEventsSubscriptionSelector>(),false))
                .Returns((string name, bool temp, IEnumerable<string> routings, int retry, int prefetch, bool explicitAcknowledgments,
                    ISubscriptionSelector subscriptionSelector, bool storedeadletter) =>
                {
                    subscriberFactoryHasBeenCalledWithCorrectParameters = true;
                    return Task.FromResult(mockSubscriber.Object);
                });

            //act

            ISubscriber deadletterSubscriber =
                await _DefaultDeadLetterSubscriberFactoryFunc()
                    .CreateSubscriberAsync(subscriberName, prefetchcount, temporary);

            //check
            subscriberFactoryHasBeenCalledWithCorrectParameters.Should().BeTrue();
            DefaultDeadLetterSubscriber actualDeadLetterSubscriber = (DefaultDeadLetterSubscriber)deadletterSubscriber;
            actualDeadLetterSubscriber.Should().NotBeNull();
        }

    }
}
