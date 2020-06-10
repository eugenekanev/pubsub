using System;

namespace Uptick.Utils
{
    public class SystemDateTimeProvider: IDateTimeProvider
    {
        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}