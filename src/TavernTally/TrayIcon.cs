using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace TavernTally
{
    /// <summary>
    /// UI-only wrapper around NotifyIcon. Raises events for the host app to handle.
    /// </summary>
    public sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon? _notifyIcon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _openItem;
        private readonly ToolStripMenuItem _toggleOverlayItem;
        private readonly ToolStripMenuItem _calibrateItem;
        private readonly ToolStripMenuItem _settingsItem;
        private readonly ToolStripMenuItem _aboutItem;
        private readonly ToolStripMenuItem _exitItem;

        private bool _overlayEnabled;

        public event EventHandler? OpenRequested;
        public event EventHandler<bool>? OverlayToggleRequested;
        public event EventHandler? CalibrateRequested;
        public event EventHandler? SettingsRequested;
        public event EventHandler? ExitRequested;

        public TrayIcon(bool overlayInitiallyEnabled = true)
        {
            _overlayEnabled = overlayInitiallyEnabled;

            _menu = new ContextMenuStrip();

            _openItem = new ToolStripMenuItem("Open", null, (_, __) => OpenRequested?.Invoke(this, EventArgs.Empty));
            _toggleOverlayItem = new ToolStripMenuItem("Enable Overlay", null, OnToggleOverlayClick) { CheckOnClick = false };
            _calibrateItem = new ToolStripMenuItem("🎯 Calibrate Overlay…", null, (_, __) => CalibrateRequested?.Invoke(this, EventArgs.Empty));
            _settingsItem = new ToolStripMenuItem("Settings…", null, (_, __) => SettingsRequested?.Invoke(this, EventArgs.Empty));
            _aboutItem = new ToolStripMenuItem("About TavernTally", null, OnAboutClick);
            _exitItem = new ToolStripMenuItem("Exit", null, (_, __) => ExitRequested?.Invoke(this, EventArgs.Empty));

            _menu.Items.AddRange(new ToolStripItem[]
            {
                _openItem,
                new ToolStripSeparator(),
                _toggleOverlayItem,
                _calibrateItem,
                _settingsItem,
                new ToolStripSeparator(),
                _aboutItem,
                _exitItem
            });

            try
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = LoadIcon(),
                    Text = $"TavernTally - Hearthstone Companion",
                    Visible = true,
                    ContextMenuStrip = _menu
                };

                _notifyIcon.DoubleClick += (_, __) => OpenRequested?.Invoke(this, EventArgs.Empty);

                UpdateOverlayMenuVisual();
                
                // Force the icon to appear
                _notifyIcon.Visible = false;
                _notifyIcon.Visible = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to create system tray icon: {ex.Message}", "TavernTally Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>Call this when overlay state changes elsewhere (keeps the menu in sync).</summary>
        public void SetOverlayEnabled(bool enabled)
        {
            _overlayEnabled = enabled;
            UpdateOverlayMenuVisual();
        }

        private void OnToggleOverlayClick(object? sender, EventArgs e)
        {
            _overlayEnabled = !_overlayEnabled;
            UpdateOverlayMenuVisual();
            OverlayToggleRequested?.Invoke(this, _overlayEnabled);
        }

        private void UpdateOverlayMenuVisual()
        {
            _toggleOverlayItem.Checked = _overlayEnabled;
            _toggleOverlayItem.Text = _overlayEnabled ? "Disable Overlay" : "Enable Overlay";
        }

        private void OnAboutClick(object? sender, EventArgs e)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            var aboutMessage = $"TavernTally\nHearthstone Companion Application\n\nVersion: {version}\n\nDeveloped for enhanced Hearthstone gameplay experience.";
            
            System.Windows.MessageBox.Show(aboutMessage, "About TavernTally", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private static Icon LoadIcon()
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
                        return new Icon(path);
                    }
                }
                catch
                {
                    // Continue to next path if this one fails
                }
            }

            // Fallback to system icon
            System.Windows.MessageBox.Show("Using system application icon as fallback", "Icon Debug", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return SystemIcons.Application;
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            _menu.Dispose();
        }
    }
}
