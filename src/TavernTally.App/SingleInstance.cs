using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace TavernTally.App
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

                var eventName = $"TavernTally.App.Event.{Environment.UserName}";
                var lockFilePath = Path.Combine(Path.GetTempPath(), $"TavernTally.App.{Environment.UserName}.lock");

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
                                        if (lockProcess.ProcessName != "TavernTally.App")
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

            // Also try to delete the lock file
            try
            {
                var lockFilePath = Path.Combine(Path.GetTempPath(), $"TavernTally.App.{Environment.UserName}.lock");
                if (File.Exists(lockFilePath))
                {
                    File.Delete(lockFilePath);
                }
            }
            catch { }
        }

        public static void ShowAlreadyRunningNotice()
        {
            try
            {
                MessageBox.Show("TavernTally is already running in the system tray.",
                    "TavernTally", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { }
        }
    }
}
