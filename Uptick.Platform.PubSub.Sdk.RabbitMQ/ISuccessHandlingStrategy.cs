using System;
using System.Threading.Tasks;
using EasyNetQ;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    public interface ISuccessHandlingStrategy
    {
        Task HandleSuccessAsync(IntegrationEvent integrationEvent);
    }
}