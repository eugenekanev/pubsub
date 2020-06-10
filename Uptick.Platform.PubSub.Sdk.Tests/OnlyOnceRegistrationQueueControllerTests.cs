using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Uptick.Platform.PubSub.Sdk.Tests
{
    public class OnlyOnceRegistrationQueueControllerTests
    {
        [Fact]
        public void RegisterQueueName_Two_Different_Names_BothNamesShouldBeRegistered()
        {
            //prepare
            OnlyOnceRegistrationSubscriberController onlyOnceRegistrationSubscriberController = 
                new OnlyOnceRegistrationSubscriberController();

            //act
            bool sharingruleshandlerNameRegistration =onlyOnceRegistrationSubscriberController
                .RegisterSubscriberName("identity.sharingruleshandler", out IDisposable sharingruleshandlerNameHandle);
            bool assoicationlogicNameRegistration = onlyOnceRegistrationSubscriberController
                .RegisterSubscriberName("crm.assoicationlogic", out IDisposable assoicationlogicNameHandle);

            //check
            sharingruleshandlerNameRegistration.Should().BeTrue();
            assoicationlogicNameRegistration.Should().BeTrue();
            sharingruleshandlerNameHandle.Should().NotBeNull();
            assoicationlogicNameHandle.Should().NotBeNull();
            sharingruleshandlerNameHandle.Should().NotBe(assoicationlogicNameHandle);
        }

        [Fact]
        public void RegisterQueueName_Two_Equal_Names_SecondCallMustReturnFalse()
        {
            //prepare
            OnlyOnceRegistrationSubscriberController onlyOnceRegistrationSubscriberController =
                new OnlyOnceRegistrationSubscriberController();

            //act
            bool sharingruleshandlerNameRegistration = onlyOnceRegistrationSubscriberController
                .RegisterSubscriberName("identity.sharingruleshandler", out IDisposable firstSharingruleshandlerNameHandle);
            bool duplicateNameRegistration = onlyOnceRegistrationSubscriberController
                .RegisterSubscriberName("identity.sharingruleshandler", out IDisposable secondSharingruleshandlerNameHandle);

            //check
            sharingruleshandlerNameRegistration.Should().BeTrue();
            duplicateNameRegistration.Should().BeFalse();
            firstSharingruleshandlerNameHandle.Should().NotBeNull();
            secondSharingruleshandlerNameHandle.Should().BeNull();
        }

        [Fact]
        public void RegisterQueueName_Two_Equal_Names_with_Dispose_In_the_Middle_ReRegistrationMustBeSucccesful()
        {
            //prepare
            OnlyOnceRegistrationSubscriberController onlyOnceRegistrationSubscriberController =
                new OnlyOnceRegistrationSubscriberController();

            //act
            bool sharingruleshandlerNameRegistration = onlyOnceRegistrationSubscriberController
                .RegisterSubscriberName("identity.sharingruleshandler", out IDisposable firstSharingruleshandlerNameHandle);
            firstSharingruleshandlerNameHandle.Dispose();

            bool secondNameRegistration = onlyOnceRegistrationSubscriberController
                .RegisterSubscriberName("identity.sharingruleshandler", out IDisposable secondSharingruleshandlerNameHandle);

            //check
            sharingruleshandlerNameRegistration.Should().BeTrue();
            secondNameRegistration.Should().BeTrue();
            firstSharingruleshandlerNameHandle.Should().NotBeNull();
            secondSharingruleshandlerNameHandle.Should().NotBeNull();
        }
    }
}
