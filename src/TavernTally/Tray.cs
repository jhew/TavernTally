using System;
using System.Threading;
using System.Windows.Forms;
using Serilog;

namespace TavernTally
{
    // Note: This file contains legacy WinForms-based tray implementation
    // Current application uses WPF with TrayIcon.cs instead
    
    /// <summary>
    /// Legacy tray application context - not currently used
    /// Keeping for reference/backup purposes
    /// Current implementation is in TrayIcon.cs (WPF-based)
    /// </summary>
    [Obsolete("This class is not used in current implementation. Use TrayIcon.cs instead.")]
    public class Tray
    {
        // Main method removed - using WPF App.xaml as entry point instead
    }

    /// <summary>
    /// Owns the lifetime of the tray icon and optional windows (e.g., settings).
    /// </summary>
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly IOverlayController _overlay;
        private readonly TrayIcon _trayIcon;
        private SettingsForm? _settings;

        public TrayApplicationContext()
        {
            _overlay = new OverlayController();
            _trayIcon = new TrayIcon(overlayInitiallyEnabled: _overlay.IsEnabled);

            // Wire tray events
            _trayIcon.SettingsRequested += (_, __) => ShowSettings();

            _trayIcon.OverlayToggleRequested += (_, enabled) =>
            {
                if (enabled) _overlay.Enable(); else _overlay.Disable();
                _trayIcon.SetOverlayEnabled(_overlay.IsEnabled);
            };

            _trayIcon.ExitRequested += (_, __) => ExitThread();
        }

        private void ShowSettings()
        {
            if (_settings is { IsDisposed: false })
            {
                // Bring existing window to front
                if (_settings.WindowState == FormWindowState.Minimized) _settings.WindowState = FormWindowState.Normal;
                _settings.Activate();
                _settings.BringToFront();
                return;
            }

            _settings = new SettingsForm(_overlay, new Settings());
            _settings.FormClosed += (_, __) => _settings = null;
            _settings.Show();
            _settings.Activate();
        }

        protected override void ExitThreadCore()
        {
            // Clean shutdown
            _settings?.Close();
            _settings?.Dispose();
            _trayIcon.Dispose();
            base.ExitThreadCore();
        }
    }

    /// <summary>Stub implementation — replace with your real overlay logic.</summary>
    internal sealed class OverlayController : IOverlayController
    {
        public bool IsEnabled { get; private set; } = true;

        public void Enable()
        {
            IsEnabled = true;
            // Note: Overlay enabling is handled by OverlayWindow based on settings
            Log.Debug("Overlay controller enabled");
        }

        public void Disable()
        {
            IsEnabled = false;
            // Note: Overlay disabling is handled by OverlayWindow based on settings
            Log.Debug("Overlay controller disabled");
        }
    }
}
