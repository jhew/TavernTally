using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace TavernTally.App
{
    /// <summary>
    /// Discovers Hearthstone log files across multiple potential installation locations
    /// </summary>
    public static class HearthstoneLogFinder
    {
        /// <summary>
        /// Attempts to find the Hearthstone Power.log file in various potential locations
        /// </summary>
        /// <returns>The path to Power.log if found, null otherwise</returns>
        public static string? FindPowerLog()
        {
            var potentialPaths = GetPotentialLogPaths();
            
            Log.Information("Searching for Hearthstone Power.log in {Count} potential locations", potentialPaths.Count);
            
            foreach (var path in potentialPaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var fileInfo = new FileInfo(path);
                        Log.Information("Found Power.log at: {Path} (Size: {Size} bytes, Modified: {Modified})", 
                            path, fileInfo.Length, fileInfo.LastWriteTime);
                        return path;
                    }
                    else
                    {
                        Log.Debug("Power.log not found at: {Path}", path);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error checking path: {Path}", path);
                }
            }
            
            Log.Warning("Power.log not found in any of the {Count} checked locations", potentialPaths.Count);
            return null;
        }

        /// <summary>
        /// Gets all potential paths where Hearthstone Power.log might be located
        /// </summary>
        private static List<string> GetPotentialLogPaths()
        {
            var paths = new List<string>();
            
            // Standard Blizzard App installation (most common)
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            paths.Add(Path.Combine(localAppData, "Blizzard", "Hearthstone", "Logs", "Power.log"));
            
            // Alternative Blizzard folder structures
            paths.Add(Path.Combine(localAppData, "Blizzard App", "Hearthstone", "Logs", "Power.log"));
            paths.Add(Path.Combine(localAppData, "Battle.net", "Hearthstone", "Logs", "Power.log"));
            
            // Program Files installations (less common but possible)
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            paths.Add(Path.Combine(programFiles, "Hearthstone", "Logs", "Power.log"));
            paths.Add(Path.Combine(programFilesX86, "Hearthstone", "Logs", "Power.log"));
            paths.Add(Path.Combine(programFiles, "Blizzard", "Hearthstone", "Logs", "Power.log"));
            paths.Add(Path.Combine(programFilesX86, "Blizzard", "Hearthstone", "Logs", "Power.log"));
            
            // User profile based installations
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(userProfile, "AppData", "Local", "Blizzard", "Hearthstone", "Logs", "Power.log"));
            paths.Add(Path.Combine(userProfile, "Documents", "Hearthstone", "Logs", "Power.log"));
            
            // Steam installation paths
            var steamPaths = GetSteamInstallPaths();
            foreach (var steamPath in steamPaths)
            {
                paths.Add(Path.Combine(steamPath, "steamapps", "common", "Hearthstone", "Logs", "Power.log"));
            }
            
            // Epic Games Store paths
            paths.Add(Path.Combine(programFiles, "Epic Games", "Hearthstone", "Logs", "Power.log"));
            paths.Add(Path.Combine(programFilesX86, "Epic Games", "Hearthstone", "Logs", "Power.log"));
            
            // Custom installation detection - check running Hearthstone process
            var hsProcessPaths = GetHearthstoneProcessPaths();
            foreach (var processPath in hsProcessPaths)
            {
                var logsPath = Path.Combine(Path.GetDirectoryName(processPath) ?? "", "Logs", "Power.log");
                paths.Add(logsPath);
            }
            
            // Remove duplicates and invalid paths
            return paths.Distinct().Where(p => !string.IsNullOrEmpty(p)).ToList();
        }

        /// <summary>
        /// Gets potential Steam installation paths
        /// </summary>
        private static List<string> GetSteamInstallPaths()
        {
            var steamPaths = new List<string>();
            
            try
            {
                // Default Steam path
                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                steamPaths.Add(Path.Combine(programFilesX86, "Steam"));
                
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                steamPaths.Add(Path.Combine(programFiles, "Steam"));
                
                // Check Steam registry for custom install paths (Windows only)
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                        if (key?.GetValue("InstallPath") is string steamPath)
                        {
                            steamPaths.Add(steamPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Could not read Steam registry path");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error detecting Steam paths");
            }
            
            return steamPaths.Where(Directory.Exists).ToList();
        }

        /// <summary>
        /// Gets installation paths based on currently running Hearthstone processes
        /// </summary>
        private static List<string> GetHearthstoneProcessPaths()
        {
            var paths = new List<string>();
            
            try
            {
                var hsProcesses = System.Diagnostics.Process.GetProcessesByName("Hearthstone");
                foreach (var process in hsProcesses)
                {
                    try
                    {
                        var processPath = process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(processPath))
                        {
                            paths.Add(processPath);
                            Log.Debug("Found Hearthstone process at: {Path}", processPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Could not get path for Hearthstone process {ProcessId}", process.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error detecting Hearthstone process paths");
            }
            
            return paths;
        }

        /// <summary>
        /// Checks if Hearthstone logging is likely enabled based on log file characteristics
        /// </summary>
        /// <param name="logPath">Path to the Power.log file</param>
        /// <returns>True if logging appears to be active</returns>
        public static bool IsLoggingActive(string logPath)
        {
            try
            {
                if (!File.Exists(logPath))
                    return false;
                
                var fileInfo = new FileInfo(logPath);
                
                // Check if file has been modified recently (within last hour)
                var isRecent = fileInfo.LastWriteTime > DateTime.Now.AddHours(-1);
                
                // Check if file has reasonable size (not empty, but not too large either)
                var hasReasonableSize = fileInfo.Length > 0 && fileInfo.Length < 100 * 1024 * 1024; // 100MB max
                
                Log.Debug("Log file analysis - Recent: {Recent}, Size: {Size} bytes, ReasonableSize: {ReasonableSize}", 
                    isRecent, fileInfo.Length, hasReasonableSize);
                
                return isRecent && hasReasonableSize;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error checking if logging is active for: {LogPath}", logPath);
                return false;
            }
        }

        /// <summary>
        /// Provides suggestions for enabling Hearthstone logging
        /// </summary>
        /// <returns>User-friendly instructions for enabling logging</returns>
        public static string GetLoggingInstructions()
        {
            return @"To enable Hearthstone logging:

1. Close Hearthstone completely
2. Navigate to your Hearthstone installation folder
3. Create a file called 'log.config' with the following content:

[Power]
LogLevel=1
FilePrinting=true
ConsolePrinting=false
ScreenPrinting=false

4. Restart Hearthstone
5. Restart TavernTally

The log file will be created at: %LOCALAPPDATA%\Blizzard\Hearthstone\Logs\Power.log";
        }

        /// <summary>
        /// Attempts to automatically create the log.config file in the Hearthstone installation directory
        /// </summary>
        /// <returns>True if log.config was created successfully, false otherwise</returns>
        public static bool TryCreateLogConfig()
        {
            var installPaths = GetHearthstoneInstallationPaths();
            
            Log.Information("Attempting to create log.config in {Count} potential Hearthstone installation directories", installPaths.Count);
            
            foreach (var installPath in installPaths)
            {
                try
                {
                    if (Directory.Exists(installPath))
                    {
                        var configPath = Path.Combine(installPath, "log.config");
                        
                        // Check if log.config already exists
                        if (File.Exists(configPath))
                        {
                            // Verify it has the correct content
                            var existingContent = File.ReadAllText(configPath);
                            if (existingContent.Contains("[Power]") && existingContent.Contains("LogLevel=1"))
                            {
                                Log.Information("log.config already exists with correct configuration at: {Path}", configPath);
                                return true;
                            }
                            else
                            {
                                Log.Information("log.config exists but has incorrect configuration, updating: {Path}", configPath);
                            }
                        }
                        
                        // Create or update the log.config file
                        var logConfig = @"[Power]
LogLevel=1
FilePrinting=true
ConsolePrinting=false
ScreenPrinting=false
";
                        
                        File.WriteAllText(configPath, logConfig);
                        Log.Information("Successfully created log.config at: {Path}", configPath);
                        return true;
                    }
                    else
                    {
                        Log.Debug("Hearthstone installation directory not found: {Path}", installPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to create log.config at: {Path}", installPath);
                }
            }
            
            Log.Warning("Could not create log.config in any Hearthstone installation directory");
            return false;
        }

        /// <summary>
        /// Gets potential Hearthstone installation directory paths
        /// </summary>
        private static List<string> GetHearthstoneInstallationPaths()
        {
            var paths = new List<string>();
            
            // Standard Blizzard App installations
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            // Most common Blizzard installation paths
            paths.Add(Path.Combine(programFilesX86, "Hearthstone"));
            paths.Add(Path.Combine(programFiles, "Hearthstone"));
            paths.Add(Path.Combine(programFilesX86, "Blizzard App", "Hearthstone"));
            paths.Add(Path.Combine(programFiles, "Blizzard App", "Hearthstone"));
            paths.Add(Path.Combine(programFilesX86, "Battle.net", "Hearthstone"));
            paths.Add(Path.Combine(programFiles, "Battle.net", "Hearthstone"));
            
            // Steam installation paths
            var steamPaths = GetSteamInstallPaths();
            foreach (var steamPath in steamPaths)
            {
                paths.Add(Path.Combine(steamPath, "steamapps", "common", "Hearthstone"));
            }
            
            // Epic Games Store paths
            paths.Add(Path.Combine(programFiles, "Epic Games", "Hearthstone"));
            paths.Add(Path.Combine(programFilesX86, "Epic Games", "Hearthstone"));
            
            // Custom installation detection - check running Hearthstone process
            var hsProcessPaths = GetHearthstoneProcessPaths();
            foreach (var processPath in hsProcessPaths)
            {
                var installDir = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrEmpty(installDir))
                {
                    paths.Add(installDir);
                }
            }
            
            // User profile based installations (less common)
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(userProfile, "AppData", "Local", "Blizzard", "Hearthstone"));
            paths.Add(Path.Combine(userProfile, "Documents", "Hearthstone"));
            
            // Remove duplicates and invalid paths
            return paths.Distinct().Where(p => !string.IsNullOrEmpty(p)).ToList();
        }
    }
}
