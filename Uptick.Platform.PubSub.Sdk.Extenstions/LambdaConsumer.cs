using System;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions
{
    public class LambdaConsumer<T> : IConsumer<T>
    {
        
        private readonly Func<IntegrationEvent<T>, Task> _consumefunc;

        public LambdaConsumer(Func<IntegrationEvent<T>, Task> consumefunc)
        {
            _consumefunc = consumefunc;
        }
        
        public async Task ConsumeAsync(IntegrationEvent<T> integrationEvent)
        {
            await _consumefunc(integrationEvent);
        }
    }
}