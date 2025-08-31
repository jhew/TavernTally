using System;
using System.Threading;
using System.Windows;
using Serilog;

namespace TavernTally.App
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Logging.Init();
            }
            catch (Exception ex)
            {
               System.Windows.MessageBox.Show("Logging init failed: " + ex.Message);
            }

            var main = new OverlayWindow();
            // Show the window to trigger Loaded event, then let it control its own visibility
            main.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logging.Shutdown();
            
            // Release the single instance resources
            SingleInstance.Cleanup();
            
            base.OnExit(e);
        }
    }
}