using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TavernTally.App
{
    internal sealed class SettingsForm : Form
    {
        private readonly IOverlayController _overlay;
        private readonly CheckBox _chkOverlay;
        private readonly CheckBox _chkStartup;
        private readonly Button _btnClose;

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRunValueName = "TavernTally";

        public SettingsForm(IOverlayController overlay)
        {
            _overlay = overlay;
            Text = "TavernTally — Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(14);

            _chkOverlay = new CheckBox { Text = "Enable Overlay (show card numbering)", AutoSize = true, Checked = _overlay.IsEnabled };
            _chkOverlay.CheckedChanged += (_, __) =>
            {
                if (_chkOverlay.Checked) _overlay.Enable();
                else _overlay.Disable();
            };

            _chkStartup = new CheckBox { Text = "Start TavernTally with Windows", AutoSize = true, Checked = IsStartupEnabled() };
            _chkStartup.CheckedChanged += (_, __) =>
            {
                try
                {
                    SetStartupEnabled(_chkStartup.Checked);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not update startup setting:\n" + ex.Message, "TavernTally",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _chkStartup.Checked = IsStartupEnabled(); // revert
                }
            };

            _btnClose = new Button { Text = "Close", AutoSize = true };
            _btnClose.Click += (_, __) => Close();

            var layout = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false
            };
            layout.Controls.Add(_chkOverlay);
            layout.Controls.Add(_chkStartup);
            layout.Controls.Add(new Label { AutoSize = true, Text = "Changes take effect immediately.", Padding = new Padding(0, 6, 0, 8) });
            layout.Controls.Add(_btnClose);

            Controls.Add(layout);
        }

        private static string GetExePath()
        {
            // Prefer actual process MainModule path; fallback to assembly location.
            try
            {
                using var p = Process.GetCurrentProcess();
                return p.MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location;
            }
            catch
            {
                return Assembly.GetExecutingAssembly().Location;
            }
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                var v = key?.GetValue(AppRunValueName) as string;
                var exe = GetExePath();
                return !string.IsNullOrWhiteSpace(v) && v.Trim('"').Equals(exe, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void SetStartupEnabled(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                           ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (key == null) throw new InvalidOperationException("Unable to open HKCU Run key.");

            var exe = GetExePath();

            if (enable)
            {
                // Quote the path to handle spaces.
                key.SetValue(AppRunValueName, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(AppRunValueName, throwOnMissingValue: false);
            }
        }
    }
}
