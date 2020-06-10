using System;

namespace Uptick.Utils
{
    public interface IDateTimeProvider
    {
        DateTime GetUtcNow();
    }
}