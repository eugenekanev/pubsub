using System;
using System.Collections.Generic;

namespace Uptick.Platform.PubSub.Sdk
{
    public class OnlyOnceRegistrationSubscriberController : ISubscriberController
    {
        private readonly object _guard;
        private readonly Dictionary<string, IDisposable> _nameHandles;

        private class NameHandle : IDisposable
        {
            private readonly OnlyOnceRegistrationSubscriberController _subscriberController;
            private readonly string _name;
            public NameHandle(OnlyOnceRegistrationSubscriberController subscriberController, string name)
            {
                _subscriberController = subscriberController;
                _name = name;
            }

            #region IDisposable Support
            private bool _disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _subscriberController.UnRegister(_name);
                    }

                    _disposedValue = true;
                }
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion
        }


        public OnlyOnceRegistrationSubscriberController()
        {
            _guard = new object();
            _nameHandles = new Dictionary<string, IDisposable>();
        }
        public bool RegisterSubscriberName(string name, out IDisposable nameHandle)
        {
            lock (_guard)
            {
                if (_nameHandles.ContainsKey(name))
                {
                    nameHandle = null;
                    return false;
                }
                else
                {
                    nameHandle = new NameHandle(this, name);
                    _nameHandles.Add(name, nameHandle);
                    return true;
                }
            }
        }

        private void UnRegister(string name)
        {
            lock (_guard)
            {
                if (_nameHandles.ContainsKey(name))
                {
                    _nameHandles.Remove(name);
                }
            }
        }
    }
}
