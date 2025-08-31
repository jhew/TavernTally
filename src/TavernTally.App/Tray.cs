using System;
using System.Threading;
using System.Windows.Forms;

namespace TavernTally.App
{
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
            _trayIcon.OpenRequested += (_, __) => ShowSettings(); // Reuse "Open" to show settings or main window
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

            _settings = new SettingsForm(_overlay);
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

    /// <summary>Minimal interface so TrayIcon stays UI-only.</summary>
    internal interface IOverlayController
    {
        bool IsEnabled { get; }
        void Enable();
        void Disable();
    }

    /// <summary>Stub implementation — replace with your real overlay logic.</summary>
    internal sealed class OverlayController : IOverlayController
    {
        public bool IsEnabled { get; private set; } = true;

        public void Enable()
        {
            // TODO: attach overlay / start numbering
            IsEnabled = true;
            // e.g., OverlayService.Instance.Show();
        }

        public void Disable()
        {
            // TODO: detach overlay / stop numbering
            IsEnabled = false;
            // e.g., OverlayService.Instance.Hide();
        }
    }
}
