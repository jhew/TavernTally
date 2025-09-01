using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Serilog;

namespace TavernTally.App
{
    public partial class CalibrationWindow : Window
    {
        private readonly Settings _settings;
        private readonly OverlayWindow? _overlayWindow;
        private Settings _originalSettings;
        private bool _isTestingOverlay = false;

        public event EventHandler<Settings>? SettingsChanged;

        public CalibrationWindow(Settings settings, OverlayWindow? overlayWindow = null)
        {
            InitializeComponent();
            
            _settings = settings;
            _overlayWindow = overlayWindow;
            _originalSettings = CloneSettings(settings);
            
            // Set window icon programmatically to ensure it loads correctly
            SetWindowIcon();
            
            InitializeControls();
            WireUpEvents();
        }

        private void InitializeControls()
        {
            // Load current settings into controls
            ShopYSlider.Value = _settings.ShopYPct;
            BoardYSlider.Value = _settings.BoardYPct;
            HandYSlider.Value = _settings.HandYPct;
            ScaleSlider.Value = _settings.UiScale;
            OffsetXSlider.Value = _settings.OffsetX;
            OffsetYSlider.Value = _settings.OffsetY;
            
            UpdateValueLabels();
        }

        private void WireUpEvents()
        {
            // Wire up slider events
            ShopYSlider.ValueChanged += (s, e) => {
                _settings.ShopYPct = e.NewValue;
                UpdateValueLabels();
                NotifySettingsChanged();
            };
            
            BoardYSlider.ValueChanged += (s, e) => {
                _settings.BoardYPct = e.NewValue;
                UpdateValueLabels();
                NotifySettingsChanged();
            };
            
            HandYSlider.ValueChanged += (s, e) => {
                _settings.HandYPct = e.NewValue;
                UpdateValueLabels();
                NotifySettingsChanged();
            };
            
            ScaleSlider.ValueChanged += (s, e) => {
                _settings.UiScale = e.NewValue;
                UpdateValueLabels();
                NotifySettingsChanged();
            };
            
            OffsetXSlider.ValueChanged += (s, e) => {
                _settings.OffsetX = e.NewValue;
                UpdateValueLabels();
                NotifySettingsChanged();
            };
            
            OffsetYSlider.ValueChanged += (s, e) => {
                _settings.OffsetY = e.NewValue;
                UpdateValueLabels();
                NotifySettingsChanged();
            };
        }

        private void UpdateValueLabels()
        {
            ShopYValue.Text = $"{_settings.ShopYPct:P0}";
            BoardYValue.Text = $"{_settings.BoardYPct:P0}";
            HandYValue.Text = $"{_settings.HandYPct:P0}";
            ScaleValue.Text = $"{_settings.UiScale:P0}";
            OffsetXValue.Text = $"{_settings.OffsetX:F0}px";
            OffsetYValue.Text = $"{_settings.OffsetY:F0}px";
        }

        private void NotifySettingsChanged()
        {
            if (!_isTestingOverlay)
            {
                SettingsChanged?.Invoke(this, _settings);
                StatusText.Text = "Settings updated - click 'Test Overlay' to preview";
            }
        }

        private async void TestOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isTestingOverlay) return;
            
            try
            {
                _isTestingOverlay = true;
                TestOverlayButton.IsEnabled = false;
                StatusText.Text = "Testing overlay for 5 seconds...";
                
                // Get test configuration
                var shopCount = int.Parse(((ComboBoxItem)TestShopCombo.SelectedItem).Content.ToString()!);
                var boardCount = int.Parse(((ComboBoxItem)TestBoardCombo.SelectedItem).Content.ToString()!);
                var handCount = int.Parse(((ComboBoxItem)TestHandCombo.SelectedItem).Content.ToString()!);
                
                Log.Information("Starting overlay calibration test: Shop={ShopCount}, Board={BoardCount}, Hand={HandCount}", 
                    shopCount, boardCount, handCount);
                
                // Create a test overlay with current settings
                await ShowTestOverlay(shopCount, boardCount, handCount);
                
                StatusText.Text = "Test completed. Adjust settings and test again if needed.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during overlay test");
                StatusText.Text = "Test failed - check logs for details";
                System.Windows.MessageBox.Show($"Test overlay failed: {ex.Message}", "Calibration Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _isTestingOverlay = false;
                TestOverlayButton.IsEnabled = true;
            }
        }

        private async Task ShowTestOverlay(int shopCount, int boardCount, int handCount)
        {
            // Create a temporary test overlay window
            var testOverlay = new CalibrationTestOverlay(_settings, shopCount, boardCount, handCount);
            
            try
            {
                testOverlay.Show();
                
                // Show for 5 seconds
                await Task.Delay(5000);
            }
            finally
            {
                testOverlay.Close();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Reset all calibration settings to defaults?\n\nThis will restore:\n" +
                "• Shop Y: 15%\n• Board Y: 42%\n• Hand Y: 85%\n• UI Scale: 100%\n• Offsets: 0px",
                "Reset Calibration",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                // Reset to defaults (fine-tuned for 4K card alignment)
                _settings.ShopYPct = 0.15;
                _settings.BoardYPct = 0.42;
                _settings.HandYPct = 0.85;
                _settings.UiScale = 1.0;
                _settings.OffsetX = 0;
                _settings.OffsetY = 0;
                
                // Update UI
                InitializeControls();
                NotifySettingsChanged();
                
                StatusText.Text = "Settings reset to defaults";
                Log.Information("Overlay calibration settings reset to defaults");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save settings to file
                Settings.Save(_settings);
                
                StatusText.Text = "Settings saved successfully!";
                Log.Information("Overlay calibration settings saved");
                
                // Close the calibration window
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save calibration settings");
                StatusText.Text = "Failed to save settings";
                System.Windows.MessageBox.Show($"Failed to save settings: {ex.Message}", "Save Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Restore original settings
            _settings.ShopYPct = _originalSettings.ShopYPct;
            _settings.BoardYPct = _originalSettings.BoardYPct;
            _settings.HandYPct = _originalSettings.HandYPct;
            _settings.UiScale = _originalSettings.UiScale;
            _settings.OffsetX = _originalSettings.OffsetX;
            _settings.OffsetY = _originalSettings.OffsetY;
            
            NotifySettingsChanged();
            
            DialogResult = false;
            Close();
        }

        private static Settings CloneSettings(Settings original)
        {
            return new Settings
            {
                ShowOverlay = original.ShowOverlay,
                DebugAlwaysShowOverlay = original.DebugAlwaysShowOverlay,
                UiScale = original.UiScale,
                OffsetX = original.OffsetX,
                OffsetY = original.OffsetY,
                ShopYPct = original.ShopYPct,
                BoardYPct = original.BoardYPct,
                HandYPct = original.HandYPct,
                UpdateJsonUrl = original.UpdateJsonUrl
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            Log.Information("Calibration window closed");
            base.OnClosed(e);
        }

        private void SetWindowIcon()
        {
            try
            {
                // Try to load icon from the same location as the executable
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    // Use System.Drawing.Icon to load, then convert to WPF ImageSource
                    using (var icon = new System.Drawing.Icon(iconPath))
                    {
                        Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        Log.Debug($"Successfully loaded window icon using System.Drawing.Icon from: {iconPath}");
                        return;
                    }
                }

                Log.Warning($"Could not find app.ico file at: {iconPath}");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to set window icon");
            }
        }
    }
}
