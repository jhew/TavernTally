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
            Background = Brushes.Red;
            
            var textBlock = new TextBlock
            {
                Text = "TEST WINDOW VISIBLE!\nIf you can see this, WPF is working.",
                FontSize = 16,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            
            Content = textBlock;
        }
    }
    
    public static class TestProgram
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            var window = new TestWindow();
            window.Show();
            app.Run(window);
        }
    }
}
