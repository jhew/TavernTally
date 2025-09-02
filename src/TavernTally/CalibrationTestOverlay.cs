using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Runtime.InteropServices;

namespace TavernTally
{
    /// <summary>
    /// A test overlay window used during calibration to show how the overlay will appear
    /// </summary>
    public class CalibrationTestOverlay : Window
    {
        private readonly Settings _settings;
        private readonly int _shopCount;
        private readonly int _boardCount;
        private readonly int _handCount;
        private Canvas? _hudCanvas;

        public CalibrationTestOverlay(Settings settings, int shopCount, int boardCount, int handCount)
        {
            _settings = settings;
            _shopCount = shopCount;
            _boardCount = boardCount;
            _handCount = handCount;

            InitializeWindow();
            CreateContent();
            UpdateOverlay();
        }

        private void InitializeWindow()
        {
            // Position over Hearthstone window
            var hsRect = GetHearthstoneWindowRect();
            if (hsRect.HasValue)
            {
                Left = hsRect.Value.Left;
                Top = hsRect.Value.Top;
                Width = hsRect.Value.Width;
                Height = hsRect.Value.Height;
            }
            else
            {
                // Fallback to center of screen
                Width = 800;
                Height = 600;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            Background = System.Windows.Media.Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
        }

        private void CreateContent()
        {
            _hudCanvas = new Canvas
            {
                Width = Width,
                Height = Height
            };
            Content = _hudCanvas;
        }

        public void UpdateOverlay()
        {
            if (_hudCanvas == null) return;
            
            _hudCanvas.Children.Clear();

            // Add semi-transparent background for better visibility during testing
            var background = new System.Windows.Shapes.Rectangle
            {
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 0, 0)),
                Width = Width,
                Height = Height
            };
            _hudCanvas.Children.Add(background);

            // Add calibration info overlay
            AddCalibrationInfo();

            // Shop numbers
            AddShopNumbers();

            // Board numbers
            AddBoardNumbers();

            // Hand numbers  
            AddHandNumbers();
        }

        private void AddCalibrationInfo()
        {
            var infoPanel = new StackPanel
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 44, 62, 80)),
                Margin = new Thickness(10)
            };

            var titleText = new TextBlock
            {
                Text = "🎯 CALIBRATION TEST",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(10, 10, 10, 5),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            var infoText = new TextBlock
            {
                Text = $"Shop: {_shopCount} | Board: {_boardCount} | Hand: {_handCount}",
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            var instructionText = new TextBlock
            {
                Text = "Adjust sliders in calibration window to align numbers with card positions",
                FontSize = 10,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 180)),
                Margin = new Thickness(10, 5, 10, 10),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            infoPanel.Children.Add(titleText);
            infoPanel.Children.Add(infoText);
            infoPanel.Children.Add(instructionText);

            Canvas.SetLeft(infoPanel, 10);
            Canvas.SetTop(infoPanel, 10);
            _hudCanvas!.Children.Add(infoPanel);
        }

        private void AddShopNumbers()
        {
            var anchors = ShopAnchors(_shopCount);
            for (int i = 0; i < anchors.Length; i++)
            {
                var label = CreateNumberLabel($"S{i + 1}", GetColorForIndex(i));
                Canvas.SetLeft(label, anchors[i].X - 15);
                Canvas.SetTop(label, anchors[i].Y - 15);
                _hudCanvas!.Children.Add(label);
            }
        }

        private void AddBoardNumbers()
        {
            var anchors = BoardAnchors(_boardCount);
            for (int i = 0; i < anchors.Length; i++)
            {
                var label = CreateNumberLabel($"B{i + 1}", GetColorForIndex(i));
                Canvas.SetLeft(label, anchors[i].X - 15);
                Canvas.SetTop(label, anchors[i].Y - 15);
                _hudCanvas!.Children.Add(label);
            }
        }

        private void AddHandNumbers()
        {
            var anchors = HandAnchors(_handCount);
            for (int i = 0; i < anchors.Length; i++)
            {
                var label = CreateNumberLabel($"H{i + 1}", GetColorForIndex(i));
                Canvas.SetLeft(label, anchors[i].X - 15);
                Canvas.SetTop(label, anchors[i].Y - 15);
                _hudCanvas!.Children.Add(label);
            }
        }

        private TextBlock CreateNumberLabel(string text, System.Windows.Media.Color color)
        {
            var label = new TextBlock
            {
                Text = text,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 0, 0, 0)),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            // Add glow effect for better visibility
            label.Effect = new DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                BlurRadius = 3,
                ShadowDepth = 1,
                Opacity = 0.8
            };

            return label;
        }

        private System.Windows.Media.Color GetColorForIndex(int index)
        {
            var colors = new[]
            {
                "#FF4444", "#44FF44", "#4444FF", "#FFFF44", "#FF44FF", "#44FFFF", "#FFA500"
            };
            
            var colorString = colors[index % colors.Length];
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorString);
        }

        // ========== POSITIONING LOGIC (copied from OverlayWindow) ==========

        private System.Windows.Point Px(double xpct, double ypct) => new System.Windows.Point(Width * xpct, Height * ypct);

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

        // ========== HEARTHSTONE WINDOW DETECTION ==========

        private Rect? GetHearthstoneWindowRect()
        {
            var hwnd = FindWindow("UnityWndClass", "Hearthstone");
            if (hwnd == IntPtr.Zero)
                return null;

            GetWindowRect(hwnd, out var rect);
            return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
