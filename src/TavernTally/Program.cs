using System;
using System.Linq;
using System.Windows;

namespace TavernTally
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // Check for development restart flag
                bool isDevRestart = args.Contains("--dev-restart") || 
                                   System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TavernTally.DevRestart.flag"));

                // Check for single instance BEFORE creating the Application
                if (!SingleInstance.TryAcquire())
                {
                    if (isDevRestart)
                    {
                        // Development mode: Try to kill existing and retry
                        SingleInstance.ShowAlreadyRunningNotice(); // This now handles dev restart
                        
                        // Try to acquire again after killing existing instances
                        System.Threading.Thread.Sleep(1000); // Give time for cleanup
                        if (!SingleInstance.TryAcquire())
                        {
                            System.Windows.MessageBox.Show("Failed to restart - existing instance could not be terminated.",
                                "TavernTally Development", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        SingleInstance.ShowAlreadyRunningNotice();
                        return; // Exit immediately, don't even create the Application
                    }
                }

                // Only create and run the application if we're the first instance
                var app = new App();
                app.Run();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception in Program.Main: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Critical Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
