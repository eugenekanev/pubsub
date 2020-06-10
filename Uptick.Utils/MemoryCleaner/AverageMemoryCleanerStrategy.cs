using System.Collections.Generic;
using System.Linq;

namespace Uptick.Utils.MemoryCleaner
{
    public class AverageMemoryCleanerStrategy : IMemoryCleanerStrategy
    {
        private readonly Queue<long> _queue;
        private readonly int _queueSize;
        private readonly double _overSize;

        // lock to prevent _queue.ToArray() exception Destination array was not long enough (usually when debug)
        private readonly object _lockObject = new object();

        public long Max { get; private set; }
        public long Avg { get; private set; }
        public bool IsReadyToCleanup { get; private set; }

        public long CurrentSize
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue.Last();
                }
            }
            set
            {
                lock (_lockObject)
                {
                    if (_queue.Count >= _queueSize)
                    {
                        _queue.Dequeue();
                    }

                    if (value > Max)
                    {
                        Max = value;
                    }

                    _queue.Enqueue(value);

                    Avg = _queue.ToArray().Sum() / _queue.Count;
                    IsReadyToCleanup = (double) CurrentSize / Avg <= _overSize;
                }
            }
        }

        public AverageMemoryCleanerStrategy(double overSize, int queueSize)
        {
            lock (_lockObject)
            {
                _queueSize = queueSize;
                _overSize = overSize;
                _queue = new Queue<long>(_queueSize);
            }
        }
    }
}