using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TavernTally
{
    internal sealed class SettingsForm : Form
    {
        private readonly IOverlayController _overlay;
        private readonly Settings _settings;
        private readonly CheckBox _chkOverlay;
        private readonly CheckBox _chkStartup;
        private readonly CheckBox _chkBypassForeground;
        private readonly Label _lblUiScale;
        private readonly TrackBar _trkUiScale;
        private readonly Label _lblOffsetX;
        private readonly NumericUpDown _numOffsetX;
        private readonly Label _lblOffsetY;
        private readonly NumericUpDown _numOffsetY;
        private readonly Button _btnClose;
        private readonly Button _btnReset;

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRunValueName = "TavernTally";

        public SettingsForm(IOverlayController overlay, Settings settings)
        {
            _overlay = overlay;
            _settings = settings;
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

            _chkBypassForeground = new CheckBox { Text = "Bypass foreground check (show overlay even when Hearthstone isn't active)", AutoSize = true, Checked = _settings.BypassForegroundCheck };
            _chkBypassForeground.CheckedChanged += (_, __) =>
            {
                _settings.BypassForegroundCheck = _chkBypassForeground.Checked;
                _settings.Save();
            };

            _lblUiScale = new Label { Text = "UI Scale:", AutoSize = true };
            _trkUiScale = new TrackBar { Minimum = 10, Maximum = 200, Value = (int)(_settings.UiScale * 100), TickFrequency = 10, Width = 200 };
            _trkUiScale.ValueChanged += (_, __) =>
            {
                _settings.UiScale = _trkUiScale.Value / 100.0;
                _settings.Save();
            };

            _lblOffsetX = new Label { Text = "Horizontal Offset (pixels):", AutoSize = true };
            _numOffsetX = new NumericUpDown { Minimum = -1000, Maximum = 1000, Value = (decimal)_settings.OffsetX, Width = 80 };
            _numOffsetX.ValueChanged += (_, __) =>
            {
                _settings.OffsetX = (double)_numOffsetX.Value;
                _settings.Save();
            };

            _lblOffsetY = new Label { Text = "Vertical Offset (pixels):", AutoSize = true };
            _numOffsetY = new NumericUpDown { Minimum = -1000, Maximum = 1000, Value = (decimal)_settings.OffsetY, Width = 80 };
            _numOffsetY.ValueChanged += (_, __) =>
            {
                _settings.OffsetY = (double)_numOffsetY.Value;
                _settings.Save();
            };

            _btnReset = new Button { Text = "Reset to Defaults", AutoSize = true };
            _btnReset.Click += (_, __) =>
            {
                if (MessageBox.Show("Reset all settings to defaults?", "TavernTally", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _settings.UiScale = 1.0;
                    _settings.OffsetX = 0;
                    _settings.OffsetY = 0;
                    _settings.BypassForegroundCheck = false;
                    _settings.Save();

                    // Update controls
                    _trkUiScale.Value = 100;
                    _numOffsetX.Value = 0;
                    _numOffsetY.Value = 0;
                    _chkBypassForeground.Checked = false;
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
            layout.Controls.Add(_chkBypassForeground);
            layout.Controls.Add(new Label { AutoSize = true, Text = "", Padding = new Padding(0, 8, 0, 0) }); // Spacer
            layout.Controls.Add(_lblUiScale);
            layout.Controls.Add(_trkUiScale);
            layout.Controls.Add(new Label { AutoSize = true, Text = "", Padding = new Padding(0, 8, 0, 0) }); // Spacer
            layout.Controls.Add(_lblOffsetX);
            layout.Controls.Add(_numOffsetX);
            layout.Controls.Add(_lblOffsetY);
            layout.Controls.Add(_numOffsetY);
            layout.Controls.Add(new Label { AutoSize = true, Text = "", Padding = new Padding(0, 8, 0, 0) }); // Spacer
            layout.Controls.Add(_btnReset);
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
