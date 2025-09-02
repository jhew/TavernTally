using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;

namespace TavernTally
{
    /// <summary>
    /// Tracks Hearthstone's client rectangle in screen coordinates
    /// and notifies listeners when it changes.
    /// </summary>
    public sealed class WindowTracker : IDisposable
    {
        private readonly System.Timers.Timer _timer = new(100); // 10 Hz
        private IntPtr _targetHwnd = IntPtr.Zero;
        private Rect _lastRect;

        public string ProcessName { get; set; } = "Hearthstone";
        public event Action<Rect>? OnClientRectChanged;

        public WindowTracker()
        {
            _timer.Elapsed += (_, __) => Tick();
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
        public void Dispose() => _timer.Dispose();

        private void Tick()
        {
            try
            {
                if (_targetHwnd == IntPtr.Zero || !IsWindow(_targetHwnd))
                {
                    _targetHwnd = FindHearthstoneHwnd();
                    if (_targetHwnd == IntPtr.Zero) return;
                }

                if (!IsWindowVisible(_targetHwnd)) return;

                if (!GetClientRect(_targetHwnd, out RECT rc)) return;

                // Top-left client → screen
                POINT tl = new() { x = rc.left, y = rc.top };
                if (!ClientToScreen(_targetHwnd, ref tl)) return;
                // Bottom-right client → screen
                POINT br = new() { x = rc.right, y = rc.bottom };
                if (!ClientToScreen(_targetHwnd, ref br)) return;

                var rect = new Rect(tl.x, tl.y, br.x - tl.x, br.y - tl.y);
                if (!rect.Equals(_lastRect))
                {
                    _lastRect = rect;
                    OnClientRectChanged?.Invoke(rect);
                }
            }
            catch { /* ignore noisy failures */ }
        }

        private IntPtr FindHearthstoneHwnd()
        {
            foreach (var p in Process.GetProcessesByName(ProcessName))
            {
                try
                {
                    if (p.MainWindowHandle != IntPtr.Zero && IsWindow(p.MainWindowHandle))
                        return p.MainWindowHandle;
                }
                catch { }
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int left, top, right, bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x, y; }
    }
}
