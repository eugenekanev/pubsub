using System;

namespace Uptick.Platform.PubSub.Sdk.Exceptions
{
    public class DiscardEventException : Exception
    {
        public DiscardEventException()
        {
            
        }
        public DiscardEventException(string message) : base(message)
        {
        }

        public DiscardEventException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
