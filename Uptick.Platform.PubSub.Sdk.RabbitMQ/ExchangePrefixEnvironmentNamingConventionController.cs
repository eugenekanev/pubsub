namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public class ExchangePrefixEnvironmentNamingConventionController : IEnvironmentNamingConventionController
    {
        private readonly string _exchangeName;

        public ExchangePrefixEnvironmentNamingConventionController(string exchangeName)
        {
            _exchangeName = exchangeName;
        }

        #region IModelNamingConventionController and inherited methods implementation
        string IModelNamingConventionController.GetQueueName(string subscriberName)
        {
            return subscriberName;
        }

        string IModelNamingConventionController.GetDeadLetterQueueName(string subscriberName)
        {
            return string.Concat("deadletter.", subscriberName);
        }

        string IModelNamingConventionController.GetRetryQueueName(string subscriberName, int retryIndex)
        {
            return string.Concat("retry.", retryIndex, ".", subscriberName);
        }

        public string GetDeadLetterQueueMessageEventType(string originalEventType)
        {
            return string.Concat("deadletter.", originalEventType);
        }

        public string GetDeadLetterQueueRoutingKey(string subscriberName)
        {
            return string.Concat("deadletter.", subscriberName);
        }

        public string GetRetryRoutingKey(string subscriberName, int retryIndex)
        {
            return string.Concat("retry.", retryIndex, ".", subscriberName);
        }

        public string GetTrackingRoutingKey(string subscriberName)
        {
            return string.Concat("ok.", subscriberName);
        }

        public string GetTrackingMessageEventType(string originalEventType)
        {
            return string.Concat("ok.", originalEventType);
        }

        public bool IsTrackingMessageEventType(string eventType)
        {
            return eventType.StartsWith("ok.");
        }

        public bool IsDeadLetterQueueMessageEventType(string eventType)
        {
            return eventType.StartsWith("deadletter.");
        }

        public string GetDirectRoutingKey(string subscriberName)
        {
            return string.Concat("direct.", subscriberName);
        }
        #endregion


        #region IEnvironmentNamingConventionController and overriden methods implementation
        public string GetQueueName(string subscriberName)
        {
            return string.Concat(_exchangeName, ".", (this as IModelNamingConventionController).GetQueueName(subscriberName));
        }


        public string GetDeadLetterQueueName(string subscriberName)
        {
            return string.Concat(_exchangeName, ".", (this as IModelNamingConventionController).GetDeadLetterQueueName(subscriberName));
        }


        public string GetRetryQueueName(string subscriberName, int retryIndex)
        {
            return string.Concat(_exchangeName, ".", (this as IModelNamingConventionController).GetRetryQueueName(subscriberName, retryIndex));
        }
        #endregion

    }
}
