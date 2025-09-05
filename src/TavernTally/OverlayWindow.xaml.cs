using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;

namespace TavernTally
{
    /// <summary>Minimal interface for overlay control.</summary>
    internal interface IOverlayController
    {
        bool IsEnabled { get; }
        void Enable();
        void Disable();
    }

    public partial class OverlayWindow : Window
    {
        private readonly Settings _settings = Settings.Load();
        private readonly BgState _state = new();
        private readonly LogTail _tail = new();
        private readonly ForegroundWatcher _fg = new();
        private HotkeyManager? _hotkeys;
        private TrayIcon? _tray;
        private readonly WindowTracker _tracker = new();
        private bool _isInitialized = false;
        private double _lastDetectedWidth = 0;
        private double _lastDetectedHeight = 0;
        private int _logLinesProcessed = 0;

        public OverlayWindow()
        {
            InitializeComponent();
            
            this.Visibility = Visibility.Hidden;
            
            SourceInitialized += (_, __) => MakeClickThrough();
            Loaded += OnLoaded;
            Closed += (_, __) =>
            {
                Log.Information("OverlayWindow.Closed event triggered - cleaning up resources");
                _hotkeys?.Dispose();
                _fg.Dispose();
                _tail.Dispose();
                _tray?.Dispose();
                _tracker.Dispose();
                Log.Information("OverlayWindow cleanup completed");
            };
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;

            try
            {
                _tracker.OnClientRectChanged += r =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _lastDetectedWidth = r.Width;
                        _lastDetectedHeight = r.Height;
                        
                        Log.Information($"[WINDOW DETECTION] Hearthstone window: X={r.X}, Y={r.Y}, Width={r.Width}, Height={r.Height}");
                        
                        // Skip positioning in debug mode to prevent override
                        if (_settings.DebugAlwaysShowOverlay)
                        {
                            Log.Information("DEBUG: Skipping window tracker positioning due to debug mode");
                            RenderHud();
                            return;
                        }
                        
                        if (r.Width <= 0 || r.Height <= 0 || r.X < -10000 || r.Y < -10000)
                        {
                            Left = 100;
                            Top = 100; 
                            Width = 800;
                            Height = 600;
                            Log.Warning("Hearthstone window not detected properly, forcing overlay to visible position");
                        }
                        else
                        {
                            Left = r.X + _settings.OffsetX;
                            Top = r.Y + _settings.OffsetY;
                            Width = r.Width;
                            Height = r.Height;
                            
                            Log.Information($"[OVERLAY POSITIONED] Using full detected dimensions: Left={Left}, Top={Top}, Width={Width}, Height={Height}");
                        }
                        
                        RenderHud();
                    });
                };
                _tracker.Start();

                _hotkeys = new HotkeyManager(this);
                _hotkeys.ToggleOverlay += () => { 
                    _settings.ShowOverlay = !_settings.ShowOverlay; 
                    _settings.Save(); 
                    Log.Information($"Overlay toggled: {_settings.ShowOverlay}");
                    UpdateOverlayVisibility();
                };
                _hotkeys.IncreaseScale += () => { _settings.UiScale = Math.Min(2.0, _settings.UiScale + 0.05); _settings.Save(); };
                _hotkeys.DecreaseScale += () => { _settings.UiScale = Math.Max(0.5, _settings.UiScale - 0.05); _settings.Save(); };
                _hotkeys.ResetBattlegrounds += () => {
                    _state.Reset();
                    Log.Information("Battlegrounds state reset");
                    UpdateOverlayVisibility();
                };
                
                // Register the hotkeys
                _hotkeys.Register();
                Log.Information("Hotkeys registered: F8=Toggle, Ctrl+F9=Reset");

                var parser = LogParser.Apply;

                _fg.OnChange += visible =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Log.Information($"Foreground changed: {visible}");
                        UpdateOverlayVisibility();
                    });
                };

                _tail.OnLine += line =>
                {
                    _logLinesProcessed++;
                    LogParser.Apply(line, _state);
                    
                    Dispatcher.Invoke(() =>
                    {
                        UpdateOverlayVisibility();
                    });
                };

                _tail.OnInitialDetectionComplete += (recentLines) =>
                {
                    LogParser.CompleteInitialDetection(_state);
                    LogParser.ScanForShopContents(recentLines, _state);
                    
                    Dispatcher.Invoke(() =>
                    {
                        UpdateOverlayVisibility();
                    });
                };

                var logPath = HearthstoneLogFinder.FindPowerLog();
                if (!string.IsNullOrEmpty(logPath))
                {
                    Log.Information($"Starting log tail on: {logPath}");
                    _tail.Start(logPath);
                }
                else
                {
                    Log.Error("Could not find Hearthstone log file");
                }

                _tray = new TrayIcon(_settings.ShowOverlay);
                
                // Wire up tray exit event to properly shut down the application
                _tray.ExitRequested += (_, __) => 
                {
                    Log.Information("Exit requested from system tray");
                    // Clean up single instance resources first
                    SingleInstance.Cleanup();
                    
                    // Close the window first to ensure proper cleanup
                    this.Close();
                    
                    // Give WPF a moment to process the window close
                    System.Threading.Thread.Sleep(100);
                    
                    // Then shut down the application
                    System.Windows.Application.Current.Shutdown();
                    
                    // As a fallback, force exit if WPF shutdown doesn't work
                    System.Threading.Tasks.Task.Delay(500).ContinueWith(_ => 
                    {
                        Log.Warning("WPF shutdown didn't complete cleanly, forcing exit");
                        Environment.Exit(0);
                    });
                };

                // Handle tray overlay toggle requests
                _tray.OverlayToggleRequested += (_, enabled) =>
                {
                    _settings.ShowOverlay = enabled;
                    _settings.Save();
                    _tray.SetOverlayEnabled(enabled);
                    Log.Information($"Overlay toggled from tray: {_settings.ShowOverlay}");
                    UpdateOverlayVisibility();
                };

                // Handle tray settings requests
                _tray.SettingsRequested += (_, __) =>
                {
                    Log.Information("Settings requested from system tray");
                    ShowSettingsForm();
                };
                
                // Initial visibility update
                UpdateOverlayVisibility();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during overlay initialization");
            }
        }

        private void UpdateOverlayVisibility()
        {
            bool showOverlay = _settings.ShowOverlay;
            bool hearthstoneForeground = _fg.HearthstoneIsForeground || _settings.BypassForegroundCheck;
            bool inBattlegrounds = _state.InBattlegrounds;
            bool inRecruitPhase = _state.InRecruitPhase;
            
            // For coaching/streaming: Show overlay when Hearthstone is detected (not just foreground)
            // This allows overlay to display even when using Discord, VS Code, browser, etc.
            bool condition1 = showOverlay;
            bool condition2 = true; // Always true for coaching/streaming use case
            bool condition3 = inBattlegrounds;
            bool condition4 = inRecruitPhase;
            
            bool shouldShow = condition1 && condition2 && condition3 && condition4;

            if (shouldShow)
            {
                Visibility = Visibility.Visible;
                RenderHud();
                Log.Information("✅ Overlay made visible - All conditions met");
            }
            else
            {
                Visibility = Visibility.Hidden;
                Log.Information($"❌ Overlay hidden - Failed conditions: ShowOverlay:{condition1}, Foreground:{condition2}, Battlegrounds:{condition3}, RecruitPhase:{condition4}");
            }
        }

        private void ShowSettingsForm()
        {
            try
            {
                // Create a simple overlay controller for the settings form
                var overlayController = new SimpleOverlayController(_settings, UpdateOverlayVisibility);
                var settingsForm = new SettingsForm(overlayController, _settings);
                settingsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error showing settings form");
                System.Windows.MessageBox.Show($"Error opening settings: {ex.Message}", "TavernTally Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Simple overlay controller that works with the current Settings system
        /// </summary>
        private class SimpleOverlayController : IOverlayController
        {
            private readonly Settings _settings;
            private readonly Action _updateOverlayVisibility;

            public SimpleOverlayController(Settings settings, Action updateOverlayVisibility)
            {
                _settings = settings;
                _updateOverlayVisibility = updateOverlayVisibility;
            }

            public bool IsEnabled => _settings.ShowOverlay;

            public void Enable()
            {
                _settings.ShowOverlay = true;
                _settings.Save();
                _updateOverlayVisibility();
            }

            public void Disable()
            {
                _settings.ShowOverlay = false;
                _settings.Save();
                _updateOverlayVisibility();
            }
        }

        private void RenderHud()
        {
            HudCanvas.Children.Clear();
            
            if (!_settings.ShowOverlay)
                return;

            // Show minimal debug info if debug mode is enabled (but not the big debug block)
            if (_settings.DebugAlwaysShowOverlay)
            {
                var debugText = new TextBlock
                {
                    Text = $"TT Debug | BG: {_state.InBattlegrounds} | Recruit: {_state.InRecruitPhase} | Shop: {_state.ShopCount} Board: {_state.BoardCount} Hand: {_state.HandCount}",
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.Yellow,
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 0, 0)),
                    Padding = new Thickness(3),
                    IsHitTestVisible = false
                };
                
                Canvas.SetLeft(debugText, 10);
                Canvas.SetTop(debugText, 10);
                HudCanvas.Children.Add(debugText);
            }

            // Only show labels when actually in Battlegrounds
            if (!_state.InBattlegrounds)
                return;

            if (_state.ShouldAutoReset())
            {
                _state.Reset();
                return;
            }

            // Show minion count labels (overlay only appears during recruit phase)
            // Shop labels (1, 2, 3, ...)
            var effectiveShop = Math.Max(1, _state.ShopCount);
            var shop = ShopAnchors(effectiveShop);
            for (int i = 0; i < shop.Length; i++)
                AddLabel((i + 1).ToString(), shop[i].X, shop[i].Y);

            // Board labels (A, B, C, ... or 1, 2, 3, ... - let's use numbers for now)
            var effectiveBoard = Math.Max(1, _state.BoardCount);
            var board = BoardAnchors(effectiveBoard);
            for (int i = 0; i < board.Length; i++)
                AddLabel((i + 1).ToString(), board[i].X, board[i].Y);

            // Hand labels (1, 2, 3, ...)
            var effectiveHand = Math.Max(1, _state.HandCount);
            var hand = HandAnchors(effectiveHand);
            for (int i = 0; i < hand.Length; i++)
                AddLabel((i + 1).ToString(), hand[i].X, hand[i].Y);
        }

        private void AddLabel(string text, double x, double y)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 28 * _settings.UiScale,
                Foreground = System.Windows.Media.Brushes.White,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black, 
                    Opacity = 0.85, 
                    BlurRadius = 6, 
                    ShadowDepth = 0
                },
                IsHitTestVisible = false
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            HudCanvas.Children.Add(tb);
        }

        private void MakeClickThrough()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private void MakeClickable()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // Anchor positions for shop cards
        private (double X, double Y)[] ShopAnchors(int count)
        {
            var positions = new (double X, double Y)[count];
            var baseY = Height * 0.66;
            var startX = Width * 0.36;
            var spacing = Width * 0.048;
            
            for (int i = 0; i < count; i++)
            {
                positions[i] = (startX + i * spacing, baseY);
            }
            return positions;
        }

        // Anchor positions for board cards
        private (double X, double Y)[] BoardAnchors(int count)
        {
            var positions = new (double X, double Y)[count];
            var baseY = Height * 0.40;
            var startX = Width * 0.36;
            var spacing = Width * 0.055;
            
            for (int i = 0; i < count; i++)
            {
                positions[i] = (startX + i * spacing, baseY);
            }
            return positions;
        }

        // Anchor positions for hand cards
        private (double X, double Y)[] HandAnchors(int count)
        {
            var positions = new (double X, double Y)[count];
            var baseY = Height * 0.90;
            var centerX = Width * 0.5;
            var totalWidth = (count - 1) * Width * 0.06;
            var startX = centerX - totalWidth / 2;
            var spacing = Width * 0.06;
            
            for (int i = 0; i < count; i++)
            {
                positions[i] = (startX + i * spacing, baseY);
            }
            return positions;
        }
    }
}
