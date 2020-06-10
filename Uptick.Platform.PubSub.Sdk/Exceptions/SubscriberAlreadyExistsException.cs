using System;

namespace Uptick.Platform.PubSub.Sdk.Exceptions
{
    public class SubscriberAlreadyExistsException : Exception
    {
        public SubscriberAlreadyExistsException()
        {
            
        }
        public SubscriberAlreadyExistsException(string message) : base(message)
        {
        }
    }
}
