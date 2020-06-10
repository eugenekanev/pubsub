using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace Uptick.Platform.PubSub.Sdk.TestConsumer
{
    public static class SerilogInitializer
    {
        public static void Initialize()
        {
            var outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [UserId:{UserId}] {Message}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.ColoredConsole(outputTemplate: outputTemplate)
                .WriteTo.Trace(outputTemplate: outputTemplate)
                .CreateLogger();
        }
    }
}
