using System.Collections.Generic;

namespace Uptick.Platform.PubSub.Sdk
{
    public class IntergrationEventParsingResult
    {
        public IntegrationEvent IntegrationEvent { get; }

        public IList<string> Errors { get; }

        public bool Success { get; }

        public IntergrationEventParsingResult(IntegrationEvent integrationEvent,
            IList<string> errors, bool success)
        {
            IntegrationEvent = integrationEvent;
            Errors = errors;
            Success = success;
        }
    }
}
