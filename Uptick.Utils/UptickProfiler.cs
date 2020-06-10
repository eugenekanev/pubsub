using System;
using System.Diagnostics;

namespace Uptick.Utils
{
    public class UptickProfiler: IDisposable
    {
        private readonly Stopwatch _watch;
        private readonly string _message;
        private static Action<string, TimeSpan> _logger;

        public UptickProfiler(string message = null)
        {
            _watch= new Stopwatch();
            _watch.Start();
            _message = message;
        }

        public static Action<string, TimeSpan> Logger
        {
            set => _logger = value;
        }

        public static UptickProfiler Create(string message)
        {
            return new UptickProfiler(message);
        }

        public void Dispose()
        {
            _logger?.Invoke(_message, _watch.Elapsed);
        }
    }
}