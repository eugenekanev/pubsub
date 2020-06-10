using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public interface IEnvironmentNamingConventionController : IModelNamingConventionController
    {
        new string GetQueueName(string subscriberName);

        new string GetRetryQueueName(string subscriberName, int retryIndex);
        new string GetDeadLetterQueueName(string subscriberName);
    }
}
