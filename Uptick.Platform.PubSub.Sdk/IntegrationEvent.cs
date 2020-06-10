using System;
using Uptick.Utils;

namespace Uptick.Platform.PubSub.Sdk
{
    public class IntegrationEvent
    {
        public Guid EventId { get; set; }
        public Guid? CorrelationId { get; set; }
        public DateTime EventCreationDate { get; set; }
        public string EventType { get; set; }
        public string Version { get; set; }
    }


    public class IntegrationEvent<T> : IntegrationEvent
    {
        public IntegrationEvent()
            :this(default(T), string.Empty)
        {   

        }

        public IntegrationEvent(T content, string eventType)
        {
            EventId = Guid.NewGuid();
            EventCreationDate = DateTime.UtcNow;
            Version = "0.1";
            CorrelationId = CorrelationHelper.GetCorrelationId();
            Content = content;
            EventType = eventType;
        }

        public T Content { get; set; }

        public override string ToString()
        {
            return $"The IntegrationEvent. EventType:{EventType} EventId:{EventId} " +
                   $"CorrelationId:{CorrelationId} EventCreationDate:{EventCreationDate} " +
                   $"Version:{Version} Content:{Content}";
        }
    }
}
