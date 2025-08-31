using System;
using System.Windows;

namespace TavernTally.App
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // Check for single instance BEFORE creating the Application
                if (!SingleInstance.TryAcquire())
                {
                    SingleInstance.ShowAlreadyRunningNotice();
                    return; // Exit immediately, don't even create the Application
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
