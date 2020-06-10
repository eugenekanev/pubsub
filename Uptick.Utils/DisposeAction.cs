using System;

namespace Uptick.Utils
{
    public class DisposeAction : IDisposable
    {
        private readonly Action _action;

        public DisposeAction(Action action)
        {
            _action = action;
        }

        void IDisposable.Dispose()
        {
            _action();
        }
    }
}