using System;
using System.Collections.Generic;
using System.Text;

namespace Uptick.Platform.PubSub.Sdk.Management
{
    public interface IQueueInfoProvider
    {
        QueueInfoSnapshot GetQueueInfo(string subscriberName, bool temporary = false);
    }
}
