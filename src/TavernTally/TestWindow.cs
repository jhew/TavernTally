using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TavernTally
{
    public class TestWindow : Window
    {
        public TestWindow()
        {
            Title = "TavernTally Test Window";
            Width = 400;
            Height = 300;
            Left = 200;
            Top = 200;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Topmost = true;
            Background = System.Windows.Media.Brushes.Red;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            ShowInTaskbar = true;
            
            var textBlock = new TextBlock
            {
                Text = "TEST WINDOW VISIBLE!\nIf you can see this, WPF is working.\nThis should be a bright red window.",
                FontSize = 16,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                TextAlignment = System.Windows.TextAlignment.Center
            };
            
            Content = textBlock;
        }
    }
}
