using EasyNetQ;
using System;

namespace Uptick.Platform.PubSub.Sdk.RabbitMQ
{
    /// <summary>
    /// Connection bus
    /// </summary>
    public interface IBroker
    {
        /// <summary>Gets or sets connection bus</summary>
        IAdvancedBus Bus { get; set; }

        /// <summary>
        /// The register disconnected action.
        /// </summary>
        /// <param name="action">add action on disconnect</param>
        void RegisterDisconnectedAction(Action action);
    }
}
