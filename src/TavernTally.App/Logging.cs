using System;
using System.IO;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Async;

namespace TavernTally.App
{
    public static class Logging
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            var logDir = Path.Combine(Settings.Dir, "logs");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, "taverntally.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Async(a => a.File(
                    logFile,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                ))
                .CreateLogger();

            Log.Information("=== TavernTally started at {Time} ===", DateTime.Now);
        }

        public static void Shutdown()
        {
            try {  Log.CloseAndFlush(); } catch { }
        }
    }
}