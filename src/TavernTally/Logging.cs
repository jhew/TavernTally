using System;
using System.IO;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Async;

namespace TavernTally
{
    public static class Logging
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;

            try
            {
                var logDir = Path.Combine(Settings.Dir, "logs");
                Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(logDir, "taverntally.log");

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()  // Changed from Debug to Information for better performance
                    .WriteTo.Async(a => a.File(
                        logFile,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}", // Removed milliseconds for better performance
                        buffered: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(5)  // Reduced from 10s to 5s for more responsive logging
                    ))
                    .CreateLogger();

                _initialized = true;
                Log.Information("=== TavernTally started at {Time} ===", DateTime.Now);
                Log.Information("Log file location: {LogFile}", logFile);
            }
            catch (Exception ex)
            {
                _initialized = false;
                throw new InvalidOperationException($"Failed to initialize logging: {ex.Message}", ex);
            }
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            
            try 
            { 
                Log.Information("=== TavernTally logging shutdown at {Time} ===", DateTime.Now);
                Log.CloseAndFlush(); 
            } 
            catch (Exception ex)
            {
                // Last resort error handling
                System.Diagnostics.Debug.WriteLine($"Error during log shutdown: {ex.Message}");
            }
            finally
            {
                _initialized = false;
            }
        }
    }
}
