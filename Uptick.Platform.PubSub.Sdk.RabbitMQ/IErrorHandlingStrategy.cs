using System;
using System.Threading.Tasks;
using EasyNetQ;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public interface IErrorHandlingStrategy
    {
        Task HandleErrorAsync(MessageExecutionContext messageExecutionContext);
    }
}