using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uptick.Utils
{
    public class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public AsyncLock(int maxCount = 1)
        {
            _semaphore = new SemaphoreSlim(maxCount, maxCount);
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        public async Task<IDisposable> LockAsync()
        {
            await _semaphore.WaitAsync();
            return new DisposeAction(() => _semaphore.Release());
        }

        public async Task<IDisposable> LockAsync(CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            return new DisposeAction(() => _semaphore.Release());
        }
    }
}