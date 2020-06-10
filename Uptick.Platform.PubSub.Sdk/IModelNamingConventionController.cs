using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk
{
    public interface IModelNamingConventionController
    {
        string GetQueueName(string subscriberName);
        string GetRetryQueueName(string subscriberName, int retryIndex);
        string GetDeadLetterQueueName(string subscriberName);

        string GetDirectRoutingKey(string subscriberName);

        string GetRetryRoutingKey(string subscriberName, int retryIndex);
        string GetDeadLetterQueueRoutingKey(string subscriberName);
        string GetDeadLetterQueueMessageEventType(string eventType);

        string GetTrackingRoutingKey(string subscriberName);

        string GetTrackingMessageEventType(string originalEventType);

        bool IsTrackingMessageEventType(string eventType);
        bool IsDeadLetterQueueMessageEventType(string eventType);
    }
}
