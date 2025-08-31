using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Serilog;

namespace TavernTally.App
{
    public partial class LoggingSetupWindow : Window
    {
        private readonly bool _logFileExists;
        private readonly bool _loggingActive;

        public LoggingSetupWindow(bool logFileExists, bool loggingActive)
        {
            InitializeComponent();
            
            _logFileExists = logFileExists;
            _loggingActive = loggingActive;
            
            SetWindowIcon();
            InitializeContent();
        }

        private void InitializeContent()
        {
            if (_logFileExists && !_loggingActive)
            {
                StatusText.Text = "Hearthstone log file found but appears inactive.";
                StatusDetails.Text = "The log file exists but hasn't been updated recently. Hearthstone logging may not be properly configured.";
            }
            else if (!_logFileExists)
            {
                StatusText.Text = "Hearthstone log file not found.";
                StatusDetails.Text = "TavernTally needs access to Hearthstone's game logs to track your Battlegrounds matches.";
            }
        }

        private async void AutoConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AutoConfigureButton.IsEnabled = false;
                AutoConfigureButton.Content = "Configuring...";
                AutoConfigStatus.Visibility = Visibility.Visible;
                AutoConfigStatus.Text = "Searching for Hearthstone installation...";

                // Attempt to create the log.config file
                bool success = HearthstoneLogFinder.TryCreateLogConfig();

                if (success)
                {
                    AutoConfigStatus.Text = "✅ Successfully configured Hearthstone logging!\n\nNext steps:\n1. Restart Hearthstone (if it's running)\n2. Restart TavernTally\n\nLogging will be active after restart.";
                    AutoConfigStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
                    AutoConfigureButton.Content = "Configuration Complete";
                    AutoConfigureButton.Background = System.Windows.Media.Brushes.DarkGreen;

                    // Update OK button text
                    OkButton.Content = "Restart TavernTally";
                }
                else
                {
                    AutoConfigStatus.Text = "❌ Could not automatically configure logging.\n\nThis might happen if:\n• Hearthstone is installed in a custom location\n• You don't have write permissions\n• Hearthstone is currently running\n\nPlease try the manual setup instructions below.";
                    AutoConfigStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
                    AutoConfigureButton.Content = "Auto-Configure Failed";
                    AutoConfigureButton.Background = System.Windows.Media.Brushes.DarkRed;
                    AutoConfigureButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during auto-configuration");
                AutoConfigStatus.Text = $"❌ Error during configuration: {ex.Message}\n\nPlease try the manual setup instructions below.";
                AutoConfigStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
                AutoConfigureButton.Content = "Configuration Failed";
                AutoConfigureButton.Background = System.Windows.Media.Brushes.DarkRed;
                AutoConfigureButton.IsEnabled = false;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SetWindowIcon()
        {
            try
            {
                // Try multiple locations for the icon file
                var iconPaths = new[]
                {
                    "app.ico",  // Current directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico"),  // App directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "assets", "app.ico"),  // Assets folder
                    "..\\..\\assets\\app.ico"  // Relative to project
                };

                foreach (var path in iconPaths)
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            Icon = BitmapFrame.Create(new Uri(Path.GetFullPath(path), UriKind.Absolute));
                            Log.Debug($"Successfully loaded window icon from: {path}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"Failed to load icon from {path}: {ex.Message}");
                    }
                }
                
                Log.Warning("Could not find app.ico file for logging setup window icon");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to set logging setup window icon");
            }
        }
    }
}
