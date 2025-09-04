using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace TavernTally
{
    internal static class SingleInstance
    {
        private static EventWaitHandle? _eventWaitHandle;
        private static FileStream? _lockFile;

        public static bool TryAcquire()
        {
            try
            {
                var callId = Guid.NewGuid().ToString("N")[0..8]; // Unique identifier for this call

                var eventName = $"TavernTally.Event.{Environment.UserName}";
                var lockFilePath = Path.Combine(Path.GetTempPath(), $"TavernTally.{Environment.UserName}.lock");

                // Try to acquire the file lock first (most reliable method)
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        // Try to create and lock a file - this is atomic
                        _lockFile = new FileStream(lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                        _lockFile.Write(System.Text.Encoding.UTF8.GetBytes($"{Environment.ProcessId}:{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}"));
                        _lockFile.Flush();

                        // If we got here, we successfully created the lock file
                        // Now try the named event as secondary protection
                        try
                        {
                            bool createdNew;
                            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName, out createdNew);
                            
                            // We are the first instance
                            return true;
                        }
                        catch
                        {
                            // Event creation failed, but we have the file lock, so we're still first
                            return true;
                        }
                    }
                    catch (IOException) when (attempt < 2)
                    {
                        // Lock file exists, check if the process that created it is still running
                        try
                        {
                            if (File.Exists(lockFilePath))
                            {
                                var lockContent = File.ReadAllText(lockFilePath);
                                var parts = lockContent.Split(':');
                                if (parts.Length >= 1 && int.TryParse(parts[0], out int lockPid))
                                {
                                    try
                                    {
                                        var lockProcess = Process.GetProcessById(lockPid);
                                        if (lockProcess.ProcessName != "TavernTally")
                                        {
                                            // PID exists but it's not TavernTally, lock file is stale
                                            File.Delete(lockFilePath);
                                            continue; // Retry the loop
                                        }
                                    }
                                    catch (ArgumentException)
                                    {
                                        // Process doesn't exist, lock file is stale
                                        File.Delete(lockFilePath);
                                        continue; // Retry the loop
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Lock file cleanup failed, continue with normal retry logic
                        }
                        // File already exists or is locked, wait a bit and retry
                        Thread.Sleep(100);
                        continue;
                    }
                    catch (IOException)
                    {
                        // File is locked by another instance
                        return false;
                    }
                    catch
                    {
                        // Any other error, cleanup and fail
                        Cleanup();
                        return false;
                    }
                }

                System.Windows.MessageBox.Show("All attempts failed", "Debug", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception in TryAcquire: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Critical Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        public static void Cleanup()
        {
            try
            {
                _lockFile?.Dispose();
                _lockFile = null;
            }
            catch { }

            try
            {
                _eventWaitHandle?.Dispose();
                _eventWaitHandle = null;
            }
            catch { }

            // More aggressive lock file cleanup
            try
            {
                var lockFilePath = Path.Combine(Path.GetTempPath(), $"TavernTally.{Environment.UserName}.lock");
                if (File.Exists(lockFilePath))
                {
                    // Force delete even if locked (this can happen if process crashed)
                    File.SetAttributes(lockFilePath, FileAttributes.Normal);
                    File.Delete(lockFilePath);
                }
            }
            catch { }
            
            // Also clean up any potential dev restart flags
            try
            {
                var flagPath = Path.Combine(Path.GetTempPath(), "TavernTally.DevRestart.flag");
                if (File.Exists(flagPath))
                {
                    File.Delete(flagPath);
                }
            }
            catch { }
        }

        public static void ShowAlreadyRunningNotice()
        {
            try
            {
                // Check if this is a development restart request
                var args = Environment.GetCommandLineArgs();
                bool isDevRestart = args.Contains("--dev-restart") || 
                                   File.Exists(Path.Combine(Path.GetTempPath(), "TavernTally.DevRestart.flag"));

                if (isDevRestart)
                {
                    // Development mode: Kill existing instance and allow this one to start
                    var result = MessageBox.Show(
                        "Development restart detected. Kill existing instance and start new one?",
                        "TavernTally Development", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        KillExistingInstances();
                        // Clean up the flag file
                        try
                        {
                            File.Delete(Path.Combine(Path.GetTempPath(), "TavernTally.DevRestart.flag"));
                        }
                        catch { }
                        return; // Allow this instance to continue
                    }
                }

                MessageBox.Show("TavernTally is already running in the system tray.",
                    "TavernTally", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { }
        }

        public static void KillExistingInstances()
        {
            try
            {
                var currentPid = Environment.ProcessId;
                var processes = Process.GetProcessesByName("TavernTally");
                
                foreach (var process in processes)
                {
                    if (process.Id != currentPid) // Don't kill ourselves
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(3000); // Wait up to 3 seconds
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public static void CreateDevRestartFlag()
        {
            try
            {
                var flagPath = Path.Combine(Path.GetTempPath(), "TavernTally.DevRestart.flag");
                File.WriteAllText(flagPath, DateTime.Now.ToString());
            }
            catch { }
        }
    }
}
