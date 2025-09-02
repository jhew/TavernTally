using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TavernTally
{
    public static class DisplayDebugger
    {
        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MonitorInfo
        {
            public uint Size;
            public Rect Monitor;
            public Rect WorkArea;
            public uint Flags;
        }

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

        public static void DebugDisplaySetup()
        {
            Console.WriteLine("=== DISPLAY DEBUG INFO ===");
            
            // Get primary screen info
            var primaryWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            var primaryHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            Console.WriteLine($"Primary Screen: {primaryWidth}x{primaryHeight}");
            
            // Get virtual screen info
            var virtualWidth = System.Windows.SystemParameters.VirtualScreenWidth;
            var virtualHeight = System.Windows.SystemParameters.VirtualScreenHeight;
            var virtualLeft = System.Windows.SystemParameters.VirtualScreenLeft;
            var virtualTop = System.Windows.SystemParameters.VirtualScreenTop;
            Console.WriteLine($"Virtual Screen: {virtualWidth}x{virtualHeight} at ({virtualLeft},{virtualTop})");
            
            // Get work area
            var workAreaWidth = System.Windows.SystemParameters.WorkArea.Width;
            var workAreaHeight = System.Windows.SystemParameters.WorkArea.Height;
            Console.WriteLine($"Work Area: {workAreaWidth}x{workAreaHeight}");
            
            Console.WriteLine("\nMonitor Details:");
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData) =>
            {
                var mi = new MonitorInfo();
                mi.Size = (uint)Marshal.SizeOf(mi);
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    Console.WriteLine($"Monitor: {mi.Monitor.Left},{mi.Monitor.Top} to {mi.Monitor.Right},{mi.Monitor.Bottom} " +
                                    $"(Size: {mi.Monitor.Right - mi.Monitor.Left}x{mi.Monitor.Bottom - mi.Monitor.Top})");
                    Console.WriteLine($"WorkArea: {mi.WorkArea.Left},{mi.WorkArea.Top} to {mi.WorkArea.Right},{mi.WorkArea.Bottom}");
                    Console.WriteLine($"Primary: {(mi.Flags & 1) == 1}");
                    Console.WriteLine();
                }
                return true;
            }, IntPtr.Zero);
            
            Console.WriteLine("=== END DISPLAY DEBUG ===");
        }
    }
}
