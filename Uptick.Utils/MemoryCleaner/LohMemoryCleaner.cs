using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;

namespace Uptick.Utils.MemoryCleaner
{
    public class LohMemoryCleaner : IDisposable
    {
        private readonly Stopwatch _watch;
        private readonly Timer _timer;

        private readonly IMemoryCleanerStrategy _strategy;

        public LohMemoryCleaner(IMemoryCleanerStrategy strategy, TimeSpan interval, Action<string> getMessage)
        {
            _strategy = strategy;
            _strategy.CurrentSize = GC.GetTotalMemory(false);

            _watch = new Stopwatch();

            _timer = new Timer(state =>
            {
                var message = Execute();
                if (!string.IsNullOrEmpty(message))
                {
                    getMessage(message);
                }
            }, null, 0, (int) interval.TotalMilliseconds);
        }

        private string Execute()
        {
            var beforeClean = _strategy.CurrentSize = GC.GetTotalMemory(false);
            if (!_strategy.IsReadyToCleanup)
            {
                return string.Empty;
            }


            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            _watch.Restart();
            _strategy.CurrentSize = GC.GetTotalMemory(true);
            _watch.Stop();
           
            return $"Memory cleanup in {(int) _watch.Elapsed.TotalMilliseconds} ms. " +
                   $"Current: {PrettifyBytes(beforeClean)}, " +
                   $"Deleted: {PrettifyBytes(beforeClean - _strategy.CurrentSize)}, " +
                   $"Max: {PrettifyBytes(_strategy.Max)}, " +
                   $"Avg: {PrettifyBytes(_strategy.Avg)}.";
        }

        private static string PrettifyBytes(double value)
        {
            string[] suffixes = {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= Math.Pow(1024, i + 1))
                {
                    return ThreeNonZeroDigits(value / Math.Pow(1024, i)) + " " + suffixes[i];
                }
            }

            return ThreeNonZeroDigits(value / Math.Pow(1024, suffixes.Length - 1)) + " " + suffixes[suffixes.Length - 1];
        }

        private static string ThreeNonZeroDigits(double value)
        {
            return value >= 100
                ? value.ToString("0,0")
                : value.ToString(value >= 10 ? "0.0" : "0.00");
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}