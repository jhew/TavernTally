using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        public OverlayWindow()
        {
            InitializeComponent();               // <-- requires XAML class match
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
            
            // Initialize the window but don't show it yet
            // The UpdateOverlayVisibility method will control actual visibility
            WindowState = WindowState.Normal;
            ShowInTaskbar = false;
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
                        Left   = r.X + _settings.OffsetX;
                        Top    = r.Y + _settings.OffsetY;
                        Width  = r.Width;
                        Height = r.Height;
                        RenderHud();
                    });
                };
                _tracker.Start();

                // Hotkeys
                _hotkeys = new HotkeyManager(this);
                _hotkeys.ToggleOverlay += () => { _settings.ShowOverlay = !_settings.ShowOverlay; _settings.Save(); };
                _hotkeys.IncreaseScale += () => { _settings.UiScale = Math.Min(2.0, _settings.UiScale + 0.05); _settings.Save(); };
                _hotkeys.DecreaseScale += () => { _settings.UiScale = Math.Max(0.5, _settings.UiScale - 0.05); _settings.Save(); };
                _hotkeys.Register();

                // Tray - with error handling
                try
                {
                    _tray = new TrayIcon(_settings.ShowOverlay);
                    _tray.OverlayToggleRequested += (_, enabled) => { 
                        _settings.ShowOverlay = enabled; 
                        _settings.Save(); 
                        UpdateOverlayVisibility();
                    };
                    _tray.SettingsRequested += (_, __) => { /* Show settings dialog */ };
                    _tray.OpenRequested += (_, __) =>
                    {
                        // Handle open request - maybe show main window or settings
                    };
                    _tray.ExitRequested += (_, __) => System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to create tray icon: {ex.Message}", "TavernTally Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }

                // Log tail + parser (safe)
                string powerLog = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Blizzard", "Hearthstone", "Logs", "Power.log");
                _tail.OnLine += (line) => LogParser.Apply(line, _state);
                _tail.Start(powerLog);

                // Foreground check
                _fg.OnChange += _ => Dispatcher.Invoke(RenderHud);
                _fg.Start();

                // Set initial overlay visibility
                UpdateOverlayVisibility();
                RenderHud();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Initialization error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "TavernTally Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void UpdateOverlayVisibility()
        {
            if (_settings.ShowOverlay)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        // ---------- Percent-based anchors ----------
        private System.Windows.Point Px(double xpct, double ypct) => new(Width * xpct, Height * ypct);

        private static readonly double[] ShopXPcts7 = { 0.32, 0.40, 0.48, 0.56, 0.64, 0.72, 0.80 };
        private static readonly double[] BoardXPcts = { 0.18, 0.28, 0.38, 0.48, 0.58, 0.68, 0.78 };

        private System.Windows.Point[] ShopAnchors(int n)
        {
            n = Math.Clamp(n, 0, 7);
            if (n <= 0) return Array.Empty<System.Windows.Point>();
            if (n == 1) return new[] { Px(0.56, _settings.ShopYPct) };

            return Enumerable.Range(0, n)
                .Select(i =>
                {
                    double t = i / (double)(n - 1);
                    int idx = (int)Math.Round(t * (ShopXPcts7.Length - 1));
                    return Px(ShopXPcts7[idx], _settings.ShopYPct);
                })
                .ToArray();
        }

        private System.Windows.Point[] BoardAnchors(int n)
        {
            n = Math.Clamp(n, 0, 7);
            return Enumerable.Range(0, n)
                .Select(i => Px(BoardXPcts[i], _settings.BoardYPct))
                .ToArray();
        }

        private System.Windows.Point[] HandAnchors(int n)
        {
            n = Math.Clamp(n, 0, 10);
            if (n <= 0) return Array.Empty<System.Windows.Point>();
            if (n == 1) return new[] { Px(0.50, _settings.HandYPct) };

            double start = 0.26, end = 0.74;
            return Enumerable.Range(0, n)
                .Select(i =>
                {
                    double t = i / (double)(n - 1);
                    return Px(start + (end - start) * t, _settings.HandYPct);
                })
                .ToArray();
        }

        // ---------- Render ----------
        private void RenderHud()
        {
            if (HudCanvas == null) return;
            HudCanvas.Children.Clear();

            if (!_settings.ShowOverlay || !_fg.HearthstoneIsForeground || !_state.InBattlegrounds)
                return;

            // Shop numbers
            var shop = ShopAnchors(_state.ShopCount);
            for (int i = 0; i < shop.Length; i++)
                AddLabel((i + 1).ToString(), shop[i].X, shop[i].Y);

            // Board letters
            var board = BoardAnchors(_state.BoardCount);
            for (int i = 0; i < board.Length; i++)
                AddLabel(((char)('A' + i)).ToString(), board[i].X, board[i].Y);

            // Hand numbers
            var hand = HandAnchors(_state.HandCount);
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
