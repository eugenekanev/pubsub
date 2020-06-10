using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk
{
    public class DeadLetterEventDescriptor<T>
    {
        public IntegrationEvent<T> Original { get; set; }

        public int CountOfAttempts { get; set; }

        public string LastAttemptError { get; set; }
    }
}
