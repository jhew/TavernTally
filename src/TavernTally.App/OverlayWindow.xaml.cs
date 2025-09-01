using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;

namespace TavernTally.App
{
    public partial class OverlayWindow : Window
    {
        private readonly Settings _settings = Settings.Load();

        private readonly BgState _state = new();        // ensure BgState.cs exists (see below)
        private readonly LogTail _tail = new();
        private readonly ForegroundWatcher _fg = new();
        private HotkeyManager? _hotkeys;
        private TrayIcon? _tray;
        private readonly WindowTracker _tracker = new();
        private bool _isInitialized = false;  // Prevent double initialization
        private double _lastDetectedWidth = 0;
        private double _lastDetectedHeight = 0;

        public OverlayWindow()
        {
            InitializeComponent();               // <-- requires XAML class match
            
            // Start hidden by default - only show when appropriate conditions are met
            this.Visibility = Visibility.Hidden;
            
            SourceInitialized += (_, __) => MakeClickThrough();
            Loaded += OnLoaded;
            Closed += (_, __) =>
            {
                _hotkeys?.Dispose();
                _fg.Dispose();
                _tail.Dispose();
                _tray?.Dispose();
                _tracker.Dispose();
            };
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Prevent double initialization
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;

            try
            {
                // Track Hearthstone client rect and pin overlay to it
                _tracker.OnClientRectChanged += r =>
            {
                Dispatcher.Invoke(() =>
                {
                    _lastDetectedWidth = r.Width;
                    _lastDetectedHeight = r.Height;
                    
                    Log.Information($"[WINDOW DETECTION] Hearthstone window: X={r.X}, Y={r.Y}, Width={r.Width}, Height={r.Height}");
                    
                    // DEBUGGING: Force overlay to be visible on primary screen if Hearthstone detection fails
                    if (r.Width <= 0 || r.Height <= 0 || r.X < -10000 || r.Y < -10000)
                    {
                        // Hearthstone not detected properly - position overlay on main screen center
                        Left = 100;
                        Top = 100; 
                        Width = 800;
                        Height = 600;
                        Log.Warning("Hearthstone window not detected properly, forcing overlay to visible position");
                    }
                    else
                    {
                        // Position overlay to match Hearthstone's position and use full detected size
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

            // Hotkeys
            _hotkeys = new HotkeyManager(this);
            _hotkeys.ToggleOverlay += () => { _settings.ShowOverlay = !_settings.ShowOverlay; _settings.Save(); };
            _hotkeys.IncreaseScale += () => { _settings.UiScale = Math.Min(2.0, _settings.UiScale + 0.05); _settings.Save(); };
            _hotkeys.DecreaseScale += () => { _settings.UiScale = Math.Max(0.5, _settings.UiScale - 0.05); _settings.Save(); };
            _hotkeys.ToggleManualBattlegrounds += () => { 
                _settings.ManualBattlegroundsMode = !_settings.ManualBattlegroundsMode; 
                _settings.Save(); 
                Log.Information("Manual Battlegrounds mode: {Mode}", _settings.ManualBattlegroundsMode ? "ENABLED" : "DISABLED");
                
                // When enabling manual mode, also enable manual counts and set reasonable defaults
                if (_settings.ManualBattlegroundsMode)
                {
                    _state.UseManualCounts = true;
                    _state.SetMode(true); // Force Battlegrounds mode
                    if (_state.ManualShopCount == 0)
                    {
                        _state.ManualShopCount = 3; // Default to tier 1 (3 shop slots)
                    }
                }
                
                RenderHud(); // Update the overlay to show the changes
            };
            _hotkeys.IncreaseShopCount += () => {
                _state.UseManualCounts = true;
                _state.ManualShopCount = Math.Min(7, _state.ManualShopCount + 1);
                Log.Information("Manual shop count: {Count}", _state.ManualShopCount);
                RenderHud();
            };
            _hotkeys.DecreaseShopCount += () => {
                _state.UseManualCounts = true;
                _state.ManualShopCount = Math.Max(0, _state.ManualShopCount - 1);
                Log.Information("Manual shop count: {Count}", _state.ManualShopCount);
                RenderHud();
            };
            _hotkeys.Register();

            // Tray
            _tray = new TrayIcon(_settings.ShowOverlay);
            _tray.OverlayToggleRequested += (_, enabled) => { _settings.ShowOverlay = enabled; _settings.Save(); };
            _tray.CalibrateRequested += (_, __) => ShowCalibrationWindow();
            _tray.SettingsRequested += (_, __) => { /* Show settings dialog */ };
            _tray.OpenRequested += (_, __) =>
            {
                // Handle open request - maybe show main window or settings
            };
            _tray.ExitRequested += (_, __) => System.Windows.Application.Current.Shutdown();

            // Log tail + parser with robust log file discovery
            var powerLogPath = HearthstoneLogFinder.FindPowerLog();
            if (powerLogPath != null)
            {
                if (HearthstoneLogFinder.IsLoggingActive(powerLogPath))
                {
                    _tail.OnLine += (line) => 
                    {
                        var previousShop = _state.ShopCount;
                        var previousHand = _state.HandCount;
                        var previousBoard = _state.BoardCount;
                        
                        LogParser.Apply(line, _state);
                        
                        // Trigger overlay update if any counts changed
                        if (_state.ShopCount != previousShop || 
                            _state.HandCount != previousHand || 
                            _state.BoardCount != previousBoard)
                        {
                            Dispatcher.Invoke(() => {
                                Log.Information("State changed - updating overlay: Shop={Shop}, Hand={Hand}, Board={Board}", 
                                    _state.ShopCount, _state.HandCount, _state.BoardCount);
                                RenderHud();
                            });
                        }
                    };
                    _tail.Start(powerLogPath);
                    Log.Information("Hearthstone logging is active and working");
                }
                else
                {
                    // Log file exists but appears inactive - show restart message
                    Log.Warning("Hearthstone log file found but appears inactive. Hearthstone may need to be restarted.");
                    ShowLoggingRestartWarning();
                }
            }
            else
            {
                // No log file found - check if logging is configured at all
                CheckAndConfigureLogging();
            }

            // Foreground check
            _fg.OnChange += _ => Dispatcher.Invoke(RenderHud);
            _fg.Start();

            RenderHud();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Initialization error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "TavernTally Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ShowCalibrationWindow()
        {
            try
            {
                Log.Information("Opening overlay calibration window");
                
                var calibrationWindow = new CalibrationWindow(_settings, this);
                calibrationWindow.SettingsChanged += (_, newSettings) =>
                {
                    // Update overlay in real-time as settings change
                    Dispatcher.Invoke(RenderHud);
                };
                
                // Show as modal dialog
                var result = calibrationWindow.ShowDialog();
                
                if (result == true)
                {
                    // Settings were saved, update overlay
                    RenderHud();
                    Log.Information("Overlay calibration completed and saved");
                }
                else
                {
                    // Settings were cancelled, make sure overlay reflects current saved settings
                    RenderHud();
                    Log.Information("Overlay calibration cancelled");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error showing calibration window");
                System.Windows.MessageBox.Show($"Failed to open calibration window: {ex.Message}", 
                    "Calibration Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // ---------- Responsive positioning system ----------
        // Universal positioning system that adapts to different screen sizes, resolutions, and aspect ratios
        // Supports: 4K, 1080p, 1440p, ultrawide (21:9), standard wide (16:9, 16:10), and legacy (4:3, 5:4)
        // Uses user settings as base values and applies intelligent adjustments based on detected screen characteristics
        private System.Windows.Point Px(double xpct, double ypct) => new(Width * xpct, Height * ypct);

        /// <summary>
        /// Gets responsive positioning values based on detected screen resolution and aspect ratio
        /// Uses settings values as base and applies responsive adjustments
        /// </summary>
        private (double shopY, double boardY, double handY) GetResponsiveYPositions()
        {
            double aspectRatio = Width / Height;
            double baseWidth = _lastDetectedWidth > 0 ? _lastDetectedWidth : Width;
            double baseHeight = _lastDetectedHeight > 0 ? _lastDetectedHeight : Height;
            
            // Start with user's configured base values
            double shopY = _settings.ShopYPct;
            double boardY = _settings.BoardYPct; 
            double handY = _settings.HandYPct;
            
            // Detect common resolutions and adjust accordingly
            bool is4K = baseWidth >= 3840 || baseHeight >= 2160;
            bool isUltrawide = aspectRatio > 1.9; // 21:9 or wider
            bool isStandardWide = aspectRatio >= 1.6 && aspectRatio <= 1.9; // 16:9, 16:10
            bool isOldRatio = aspectRatio < 1.6; // 4:3, 5:4
            
            Log.Debug($"[RESPONSIVE] Resolution: {baseWidth}x{baseHeight}, Aspect: {aspectRatio:F2}, 4K: {is4K}, Ultrawide: {isUltrawide}");
            
            // Apply responsive adjustments to base values
            if (is4K)
            {
                // 4K adjustments - UI elements are relatively smaller, slight shifts needed
                shopY += 0.00; // Keep user setting for 4K
                boardY -= 0.02; // Slight adjustment for 4K
                handY -= 0.02; // Slight adjustment for 4K
            }
            else if (isUltrawide)
            {
                // Ultrawide monitors - cards may be positioned differently due to UI scaling
                shopY += 0.03;
                boardY += 0.02;
                handY += 0.02;
            }
            else if (isOldRatio)
            {
                // 4:3 or 5:4 monitors - older aspect ratios have different UI proportions
                shopY += 0.06;
                boardY += 0.05;
                handY += 0.04;
            }
            
            // Fine-tune based on actual height
            if (baseHeight < 720)
            {
                // Very low resolution adjustments
                shopY += 0.02;
                boardY += 0.02;
                handY -= 0.02;
            }
            else if (baseHeight > 1440 && !is4K)
            {
                // High resolution (but not 4K) adjustments
                shopY -= 0.01;
                boardY -= 0.01;
                handY -= 0.01;
            }
            
            // Ensure values stay within reasonable bounds
            shopY = Math.Clamp(shopY, 0.05, 0.40);
            boardY = Math.Clamp(boardY, 0.30, 0.70);
            handY = Math.Clamp(handY, 0.75, 0.95);
            
            return (shopY, boardY, handY);
        }

        /// <summary>
        /// Gets responsive X positions for shop cards based on screen width and aspect ratio
        /// </summary>
        private double[] GetResponsiveShopXPositions(int cardCount)
        {
            if (cardCount <= 0) return Array.Empty<double>();
            
            double aspectRatio = Width / Height;
            double baseWidth = _lastDetectedWidth > 0 ? _lastDetectedWidth : Width;
            
            // Base positions for standard layouts
            double[] basePositions = { 0.17, 0.27, 0.37, 0.47, 0.57, 0.67, 0.77 };
            
            // Adjust for ultrawide monitors
            if (aspectRatio > 1.9)
            {
                // Ultrawide - cards are more centered horizontally
                double centerOffset = (aspectRatio - 1.78) * 0.05; // Adjust center based on how wide
                basePositions = basePositions.Select(x => x + centerOffset * (x - 0.5)).ToArray();
            }
            
            // Adjust for very wide or narrow screens
            if (baseWidth > 3440) // Very wide screens
            {
                // Spread cards slightly more
                basePositions = basePositions.Select(x => 0.5 + (x - 0.5) * 1.02).ToArray();
            }
            else if (baseWidth < 1366) // Smaller screens
            {
                // Compress cards slightly
                basePositions = basePositions.Select(x => 0.5 + (x - 0.5) * 0.98).ToArray();
            }
            
            return basePositions.Take(cardCount).ToArray();
        }

        /// <summary>
        /// Gets responsive X positions for board cards
        /// </summary>
        private double[] GetResponsiveBoardXPositions(int cardCount)
        {
            if (cardCount <= 0) return Array.Empty<double>();
            
            // Board positions are similar to shop but may have slight differences
            var shopPositions = GetResponsiveShopXPositions(cardCount);
            
            // Board cards are often slightly more spread out
            return shopPositions.Select(x => 0.5 + (x - 0.5) * 1.01).ToArray();
        }

        private System.Windows.Point[] ShopAnchors(int n)
        {
            n = Math.Clamp(n, 0, 7);
            if (n <= 0) return Array.Empty<System.Windows.Point>();
            
            var (shopY, _, _) = GetResponsiveYPositions();
            var xPositions = GetResponsiveShopXPositions(n);
            
            if (n == 1) return new[] { Px(0.50, shopY) };
            
            return xPositions.Select(x => Px(x, shopY)).ToArray();
        }

        private System.Windows.Point[] BoardAnchors(int n)
        {
            n = Math.Clamp(n, 0, 7);
            if (n <= 0) return Array.Empty<System.Windows.Point>();
            
            var (_, boardY, _) = GetResponsiveYPositions();
            var xPositions = GetResponsiveBoardXPositions(n);
            
            return xPositions.Select(x => Px(x, boardY)).ToArray();
        }

        private System.Windows.Point[] HandAnchors(int n)
        {
            n = Math.Clamp(n, 0, 10);
            if (n <= 0) return Array.Empty<System.Windows.Point>();
            
            var (_, _, handY) = GetResponsiveYPositions();
            
            if (n == 1) return new[] { Px(0.50, handY) };

            // Responsive hand positioning - adapts to screen width
            double aspectRatio = Width / Height;
            double baseWidth = _lastDetectedWidth > 0 ? _lastDetectedWidth : Width;
            
            // Adjust hand spread based on screen characteristics
            double start = 0.32, end = 0.68;
            
            if (aspectRatio > 1.9) // Ultrawide
            {
                start = 0.38;
                end = 0.62;
            }
            else if (baseWidth < 1366) // Smaller screens
            {
                start = 0.28;
                end = 0.72;
            }
            
            return Enumerable.Range(0, n)
                .Select(i =>
                {
                    double t = i / (double)(n - 1);
                    return Px(start + (end - start) * t, handY);
                })
                .ToArray();
        }

        private void ShowLoggingRestartWarning()
        {
            Log.Warning("Hearthstone logging appears inactive. Please restart Hearthstone for TavernTally to work properly.");
            // Could show a user notification here in the future
        }

        private void CheckAndConfigureLogging()
        {
            Log.Information("No Hearthstone log file found. Checking if logging configuration is needed...");
            
            // Try to create/verify log.config file exists with correct settings
            bool success = HearthstoneLogFinder.TryCreateLogConfig();
            if (success)
            {
                Log.Information("Hearthstone logging configuration completed successfully. Please restart Hearthstone for changes to take effect.");
            }
            else
            {
                Log.Warning("Failed to configure Hearthstone logging automatically. Manual configuration may be required.");
                Log.Information("Manual setup instructions: {Instructions}", HearthstoneLogFinder.GetLoggingInstructions());
            }
        }

        // ---------- Render ----------
        private void RenderHud()
        {
            if (HudCanvas == null) return;
            HudCanvas.Children.Clear();

            // Debug logging to help troubleshoot overlay issues
            Log.Information("RenderHud: ShowOverlay={ShowOverlay}, HearthstoneIsForeground={Foreground}, InBattlegrounds={InBG}, InRecruitPhase={InRecruit}, ManualMode={ManualMode}", 
                _settings.ShowOverlay, _fg.HearthstoneIsForeground, _state.InBattlegrounds, _state.InRecruitPhase, _settings.ManualBattlegroundsMode);

            // RELAXED CONDITIONS FOR DEBUGGING: Show status information when Hearthstone is foreground
            bool inBattlegroundsMode = _state.InBattlegrounds || _settings.ManualBattlegroundsMode;
            bool shouldShow = _settings.ShowOverlay && 
                              _fg.HearthstoneIsForeground && 
                              inBattlegroundsMode && 
                              _state.InRecruitPhase;
            
            // Show debug info when Hearthstone is foreground (even if not in BG mode)
            bool showDebugInfo = _settings.ShowOverlay && _fg.HearthstoneIsForeground;
            
            // Hide the entire window if no reason to show anything
            if (!shouldShow && !showDebugInfo)
            {
                this.Visibility = Visibility.Hidden;
                return;
            }

            // Show the window when we have something to display
            this.Visibility = Visibility.Visible;

            // If not in proper game mode, show debug status
            if (!shouldShow && showDebugInfo)
            {
                AddLabel($"TavernTally Status:", 0.02 * Width, 0.05 * Height);
                AddLabel($"Hearthstone Foreground: {_fg.HearthstoneIsForeground}", 0.02 * Width, 0.10 * Height);
                AddLabel($"In Battlegrounds: {_state.InBattlegrounds}", 0.02 * Width, 0.15 * Height);
                AddLabel($"In Recruit Phase: {_state.InRecruitPhase}", 0.02 * Width, 0.20 * Height);
                AddLabel($"Manual Mode: {_settings.ManualBattlegroundsMode}", 0.02 * Width, 0.25 * Height);
                AddLabel($"Press Ctrl+F8 for manual mode", 0.02 * Width, 0.30 * Height);
                return;
            }

            Log.Information("Rendering overlay - Hand: {Hand}, Board: {Board}, Shop: {Shop} (Manual: {UseManual}, ManualShop: {ManualShop})", 
                _state.HandCount, _state.BoardCount, _state.ShopCount, _state.UseManualCounts, _state.ManualShopCount);

            // Use effective counts (manual overrides when available)
            int effectiveShop = _state.EffectiveShopCount;
            int effectiveHand = _state.EffectiveHandCount; 
            int effectiveBoard = _state.EffectiveBoardCount;

            Log.Information("Effective counts - Shop: {Shop}, Hand: {Hand}, Board: {Board}", effectiveShop, effectiveHand, effectiveBoard);

            // If debug mode is enabled and no cards detected, show test overlays for positioning
            if (_settings.DebugAlwaysShowOverlay && effectiveShop == 0 && effectiveHand == 0 && effectiveBoard == 0)
            {
                Log.Information("Debug mode: Showing test overlays for positioning");
                
                // Show detected window dimensions for debugging
                AddLabel($"Detected: {_lastDetectedWidth}x{_lastDetectedHeight}", 0.02 * Width, 0.15 * Height);
                AddLabel($"Overlay: {Width}x{Height}", 0.02 * Width, 0.20 * Height);
                
                // Add corner markers to see overlay window bounds
                // Position right-side text well within window bounds to account for text width
                AddLabel("TOP-LEFT", 0.02 * Width, 0.02 * Height);
                AddLabel("TOP-RIGHT", 0.65 * Width, 0.02 * Height);  // Moved significantly left to stay within bounds
                AddLabel("BOT-LEFT", 0.02 * Width, 0.90 * Height);  
                AddLabel("BOT-RIGHT", 0.65 * Width, 0.90 * Height); // Moved significantly left to stay within bounds
                
                // Use proper positioning system for test overlays
                var testShop = ShopAnchors(3); // Show 3 test shop cards
                for (int i = 0; i < testShop.Length; i++)
                    AddLabel($"SHOP {i + 1}", testShop[i].X, testShop[i].Y);
                
                var testBoard = BoardAnchors(4); // Show 4 test board cards
                for (int i = 0; i < testBoard.Length; i++)
                    AddLabel($"{(char)('A' + i)}", testBoard[i].X, testBoard[i].Y);
                
                var testHand = HandAnchors(2); // Show 2 test hand cards  
                for (int i = 0; i < testHand.Length; i++)
                    AddLabel($"HAND {i + 1}", testHand[i].X, testHand[i].Y);
                return;
            }

            // Shop numbers
            var shop = ShopAnchors(effectiveShop);
            for (int i = 0; i < shop.Length; i++)
                AddLabel((i + 1).ToString(), shop[i].X, shop[i].Y);

            // Board letters
            var board = BoardAnchors(effectiveBoard);
            for (int i = 0; i < board.Length; i++)
                AddLabel(((char)('A' + i)).ToString(), board[i].X, board[i].Y);

            // Hand numbers
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
                    Color = Colors.Black, Opacity = 0.85, BlurRadius = 6, ShadowDepth = 0
                },
                IsHitTestVisible = false
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb,  y);
            HudCanvas.Children.Add(tb);
        }

        // ---------- Click-through ----------
        private void MakeClickThrough()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
