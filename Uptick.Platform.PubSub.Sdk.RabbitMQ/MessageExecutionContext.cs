using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ;
using Newtonsoft.Json.Linq;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public class MessageExecutionContext
    {
        public byte[] BodyBytes { get; }
        public MessageProperties MessageProperties { get; }
        public MessageReceivedInfo MessageReceivedInfo { get; }

        public IntergrationEventParsingResult IntergrationEventParsingResult { get; set; }

        public ISubscription Subscription { get; set; }
        public Exception Exception { get; set; }

        public string SerializedMessage { get; set; }

        public IntegrationEvent<DeadLetterEventDescriptor<JObject>> DeadLetterIntegrationEvent { get; set; }


        public MessageExecutionContext(byte[] bodyBytes, MessageProperties messageProperties,
            MessageReceivedInfo messageReceivedInfo)
        {
            BodyBytes = bodyBytes;
            MessageProperties = messageProperties;
            MessageReceivedInfo = messageReceivedInfo;
        }
    }
}
