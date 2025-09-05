using System;
using System.Threading;
using System.Windows;
using Serilog;

namespace TavernTally
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Logging.Init();
                Log.Information("TavernTally application starting up");
            }
            catch (Exception ex)
            {
                var message = $"Failed to initialize logging: {ex.Message}";
                System.Windows.MessageBox.Show(message, "TavernTally Startup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                // Continue anyway - logging is important but not critical for basic function
            }

            try
            {
                var main = new OverlayWindow();
                // Set this as the main window for proper application lifecycle management
                System.Windows.Application.Current.MainWindow = main;
                // Show the window to trigger Loaded event, then let it control its own visibility
                main.Show();
            }
            catch (Exception ex)
            {
                var message = $"Failed to create main window: {ex.Message}\n\nStack trace:\n{ex.StackTrace}";
                System.Windows.MessageBox.Show(message, "TavernTally Critical Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Fatal(ex, "Failed to create main window");
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("App.OnExit called - application shutting down");
            try
            {
                Log.Information("TavernTally application shutting down");
                Logging.Shutdown();
            }
            catch (Exception ex)
            {
                // Can't do much here, but try to log if possible
                try { Log.Error(ex, "Error during logging shutdown"); } catch { }
            }
            
            try
            {
                // Release the single instance resources
                SingleInstance.Cleanup();
                Log.Information("SingleInstance cleanup completed");
            }
            catch (Exception ex)
            {
                try { Log.Error(ex, "Error during single instance cleanup"); } catch { }
            }
            
            base.OnExit(e);
        }
    }
}
