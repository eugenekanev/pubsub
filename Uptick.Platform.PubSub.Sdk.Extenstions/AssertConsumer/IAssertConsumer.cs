using System;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.AssertConsumer
{
    public interface IAssertConsumer<T> : IDisposable
        where T : class
    {
        Task SubscribeAsync();
        void ClearEvents();
        void WaitAssert(Action<T[]> assertionFunc, int retryCount = 100, int retryTimeoutRatioMs = 100);
        Task WaitAssertAsync(Func<T[], Task> assertionFunc, int retryCount = 50, int retryTimeoutRatioMs = 100);
    }
}