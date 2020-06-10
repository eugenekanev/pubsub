using System;
using System.Collections.Generic;

namespace Uptick.Platform.PubSub.Sdk.Exceptions
{
    public class MalformedEventException : Exception
    {

        public IReadOnlyList<string> Errors { get; }

        public MalformedEventException()
        {
            Errors = new List<string>();
        }

        public MalformedEventException(IList<string> errors) : base(string.Join(" ; ", errors))
        {
            Errors = new List<string>(errors);
        }

        public MalformedEventException(IList<string> errors, Exception innerException) : 
            base(string.Join(" ; ", errors), innerException)
        {
            Errors = new List<string>(errors);
        }
    }
}
