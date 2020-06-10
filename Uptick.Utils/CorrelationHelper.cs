using System;
using System.Threading;

namespace Uptick.Utils
{
    public static class CorrelationHelper
    {
        private static readonly AsyncLocal<Guid?> GeneratedCorrelationId = new AsyncLocal<Guid?>();
        private static readonly AsyncLocal<Guid?> CorrelationId = new AsyncLocal<Guid?>();

        public static Guid GetCorrelationId()
        {
            GeneratedCorrelationId.Value = GeneratedCorrelationId.Value ?? Guid.NewGuid();
            return CorrelationId.Value ?? GeneratedCorrelationId.Value.Value;
        }

        public static void SetCorrelationId(Guid correlationId)
        {
            CorrelationId.Value = correlationId;
        }
    }
}