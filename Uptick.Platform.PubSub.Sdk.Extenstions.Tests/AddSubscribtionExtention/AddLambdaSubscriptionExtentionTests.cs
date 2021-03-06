using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Tests.AddSubscribtionExtention
{
    public class AddLambdaSubscriptionExtentionTests
    {
        [Fact]
        public async Task ISubscriberAddSubscribtion_SubscriptionAdded()
        {
            //arrange
            var mockSubscriber = new Mock<ISubscriber>();
            IDictionary<string, ISubscription> actualSubscriptions = null;
            mockSubscriber.Setup(x => x.Subscribe(It.IsAny<IDictionary<string, ISubscription>>()))
                .Callback<IDictionary<string, ISubscription>> (
                    (subscriptions) => { actualSubscriptions = subscriptions; });
            bool deadNotify = false;
            var deadletterNotifier = new Func<IntegrationEvent<Object>,Exception,Task>((x,ex) =>
            {
                deadNotify = true;
                return Task.CompletedTask;
            });
            var consumerNotify = false;
            var consumer = new Func<Object,Task>(x =>
            {
                consumerNotify = true;
                return Task.CompletedTask;
            });

            //act
            mockSubscriber.Object
                .AddSubscription("evtName", consumer, deadletterNotifier)
                .Build();
            
            //assert
            var subcr = Assert.Single(actualSubscriptions, x => x.Key =="evtName");
            await subcr.Value.NotifyAboutDeadLetterAsync("",new Exception());
            await subcr.Value.InvokeAsync("");
            Assert.True(deadNotify);
            Assert.True(consumerNotify);
        }
        
        [Fact]
        public async Task TaskISubscriberAddSubscribtion_SubscriptionAdded()
        {
            //arrange
            var mockSubscriber = new Mock<ISubscriber>();
            IDictionary<string, ISubscription> actualSubscriptions = null;
            mockSubscriber.Setup(x => x.Subscribe(It.IsAny<IDictionary<string, ISubscription>>()))
                .Callback<IDictionary<string, ISubscription>> (
                    (subscriptions) => { actualSubscriptions = subscriptions; });
            bool deadNotify = false;
            var deadletterNotifier = new Func<IntegrationEvent<Object>,Exception,Task>((x,ex) =>
            {
                deadNotify = true;
                return Task.CompletedTask;
            });
            var consumerNotify = false;
            var consumer = new Func<Object,Task>(x =>
            {
                consumerNotify = true;
                return Task.CompletedTask;
            });

            //act
            await Task.FromResult(mockSubscriber.Object)
                .AddSubscription("evtName", consumer, deadletterNotifier)
                .Build();
            
            //assert
            var subcr = Assert.Single(actualSubscriptions, x => x.Key =="evtName");
            await subcr.Value.NotifyAboutDeadLetterAsync("",new Exception());
            await subcr.Value.InvokeAsync("");
            Assert.True(deadNotify);
            Assert.True(consumerNotify);
        }
        
        [Fact]
        public async Task ChainedISubscriberAddSubscribtion_SubscriptionAdded()
        {
            //arrange
            var mockSubscriber = new Mock<ISubscriber>();
            IDictionary<string, ISubscription> actualSubscriptions = null;
            mockSubscriber.Setup(x => x.Subscribe(It.IsAny<IDictionary<string, ISubscription>>()))
                .Callback<IDictionary<string, ISubscription>> (
                    (subscriptions) => { actualSubscriptions = subscriptions; });
            var deadNotify =0;
            var deadletterNotifier = new Func<IntegrationEvent<Object>,Exception,Task>((x,ex) =>
            {
                deadNotify++;
                return Task.CompletedTask;
            });

            var consumerNotify = 0;
            var consumer = new Func<Object,Task>(x =>
            {
                consumerNotify++;
                return Task.CompletedTask;
            });
            
            //act
            mockSubscriber.Object
                .AddSubscription("evtName1",consumer ,deadletterNotifier)
                .AddSubscription("evtName2", consumer,deadletterNotifier).Build();
            
            //assert
            Assert.Equal(2,actualSubscriptions.Count);
            foreach (var subscription in actualSubscriptions)
            {
                await subscription.Value.NotifyAboutDeadLetterAsync("",new Exception());
                await subscription.Value.InvokeAsync("");
            }
            Assert.Equal(2,deadNotify);
            Assert.Equal(2,consumerNotify);
            Assert.Single(actualSubscriptions, x => x.Key == "evtName1");
            Assert.Single(actualSubscriptions, x => x.Key == "evtName2");
        }
        
        
        [Fact]
        public async Task ChainedTaskISubscriberAddSubscribtion_SubscriptionAdded()
        {
            //arrange
            var mockSubscriber = new Mock<ISubscriber>();
            IDictionary<string, ISubscription> actualSubscriptions = null;
            mockSubscriber.Setup(x => x.Subscribe(It.IsAny<IDictionary<string, ISubscription>>()))
                .Callback<IDictionary<string, ISubscription>> (
                    (subscriptions) => { actualSubscriptions = subscriptions; });
            var deadNotify = 0;
            var deadletterNotifier = new Func<IntegrationEvent<Object>,Exception,Task>((x,ex) =>
            {
                deadNotify++;
                return Task.CompletedTask;
            });
            var consumerNotify = 0;
            var consumer = new Func<Object,Task>(x =>
            {
                consumerNotify++;
                return Task.CompletedTask;
            });
            
            //act
            await Task.FromResult(mockSubscriber.Object)
                .AddSubscription("evtName1", consumer,deadletterNotifier)
                .AddSubscription("evtName2", consumer,deadletterNotifier)
                .Build();
            
            //assert
            Assert.Equal(2,actualSubscriptions.Count);
            foreach (var subscription in actualSubscriptions)
            {
                await subscription.Value.NotifyAboutDeadLetterAsync("",new Exception());
                await subscription.Value.InvokeAsync("");
            }
            Assert.Equal(2,deadNotify);
            Assert.Equal(2,consumerNotify);
            Assert.Single(actualSubscriptions, x => x.Key == "evtName1");
            Assert.Single(actualSubscriptions, x => x.Key == "evtName2");
        }
    }
}