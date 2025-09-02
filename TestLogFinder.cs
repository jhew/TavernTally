using System;
using System.IO;
using TavernTally;
using Serilog;

class Program
{
    static void Main()
    {
        // Setup logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
            
        Console.WriteLine("Testing HearthstoneLogFinder...");
        
        var logPath = HearthstoneLogFinder.FindPowerLog();
        Console.WriteLine($"Found log: {logPath ?? "NULL"}");
        
        if (logPath != null)
        {
            var isActive = HearthstoneLogFinder.IsLoggingActive(logPath);
            Console.WriteLine($"Is active: {isActive}");
            
            if (File.Exists(logPath))
            {
                var fileInfo = new FileInfo(logPath);
                Console.WriteLine($"File size: {fileInfo.Length} bytes");
                Console.WriteLine($"Last modified: {fileInfo.LastWriteTime}");
                
                var lines = File.ReadAllLines(logPath);
                Console.WriteLine($"Total lines: {lines.Length}");
                if (lines.Length > 0)
                {
                    Console.WriteLine($"Last line: {lines[lines.Length - 1]}");
                }
            }
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
