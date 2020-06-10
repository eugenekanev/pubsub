using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Aggregator
{
    public class Aggregator<T, TM> : IConsumer<T>
    {
        private readonly Func<IntegrationEvent<T>, Task<TM>> _mapFunc;
        private readonly Func<IEnumerable<TM>, Task> _reduceFunc;
        private readonly int _bucketsize;

        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private int _currentBucketSize;
        private Bucket<TM> _currentBucket;
        private readonly Timer _timer;
        private DateTime _oldestCapturedEvent;
        private readonly TimeSpan _maxCapturedEventWaitingTime;
        private readonly TimeSpan _noExecuteInPeriod;

        public Aggregator(Func<IntegrationEvent<T>, Task<TM>> mapFunc, int bucketsize, Func<IEnumerable<TM>, Task> reduceFunc, TimeSpan maxWaitingTime)
        {
            _mapFunc = mapFunc;
            _reduceFunc = reduceFunc;
            _bucketsize = bucketsize;

            //magic const for Timer class
            _noExecuteInPeriod = TimeSpan.FromMilliseconds(-1);
            //magic const for Timer class

            _maxCapturedEventWaitingTime = maxWaitingTime;

            StartNewBucket();


            if (maxWaitingTime != TimeSpan.Zero)
            {
                _timer = new Timer((e) =>
                {
                    FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                }, null, maxWaitingTime, _noExecuteInPeriod);
            }
           
        }

        public async Task ConsumeAsync(IntegrationEvent<T> integrationEvent)
        {
            TM mapResult = await _mapFunc(integrationEvent);
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            Bucket<TM> closedBucket = null;

            await _mutex.WaitAsync();
            
            if (_currentBucketSize == 0)
                {
                    _oldestCapturedEvent = DateTime.UtcNow;
                }

            _currentBucketSize = _currentBucketSize + 1;

            _currentBucket.Add(taskCompletionSource, mapResult);

            if (_currentBucketSize < _bucketsize)
                {
                }
            else
                {
                    closedBucket = _currentBucket;

                    StartNewBucket();

                }
            
            _mutex.Release();
            
            if (closedBucket != null)
            {
                await ProcessClosedBucket(closedBucket);
            }

            await taskCompletionSource.Task;
        }

        private async Task FlushAsync()
        {
            await _mutex.WaitAsync();
            Bucket<TM> closedBucket =null;
           
            if (_oldestCapturedEvent == DateTime.MinValue)
                {
                    _timer.Change(_maxCapturedEventWaitingTime, _noExecuteInPeriod);
                }
            else
                {
                   TimeSpan maxTimeLeftToWaitForCurrentBucket = _maxCapturedEventWaitingTime.Subtract(DateTime.UtcNow.Subtract(_oldestCapturedEvent));

                    if (maxTimeLeftToWaitForCurrentBucket > TimeSpan.Zero)
                    {
                        _timer.Change(maxTimeLeftToWaitForCurrentBucket, _noExecuteInPeriod);
                    }
                    else
                    {
                        closedBucket = _currentBucket;

                        StartNewBucket();
                        _timer.Change(_maxCapturedEventWaitingTime, _noExecuteInPeriod);
                    }
                    
                }

            _mutex.Release();

            if (closedBucket != null)
            {
                await ProcessClosedBucket(closedBucket);
            }
        }

        private void StartNewBucket()
        {
            _oldestCapturedEvent = DateTime.MinValue;
            _currentBucketSize = 0;
            _currentBucket = new Bucket<TM>();
        }

        private async Task ProcessClosedBucket(Bucket<TM> bucket)
        {
            try
            {
                await _reduceFunc(bucket.Values);
                bucket.Succed();
            }
            catch (Exception e)
            {
                bucket.Fail(e);
            }
        }
    }
}
