using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace TavernTally.App
{
    public class ForegroundWatcher : IDisposable
    {
        private readonly System.Timers.Timer _timer = new(250);
        public bool HearthstoneIsForeground { get; private set; }
        public event Action<bool>? OnChange;

        public ForegroundWatcher()
        {
            _timer.Elapsed += (_, __) => Tick();
        }

        public void Start() { _timer.Start(); Tick(); }
        public void Stop() { _timer.Stop(); }

        private void Tick()
        {
            var h = GetForegroundWindow();
            GetWindowThreadProcessId(h, out uint pid);
            string procName = "";
            try { procName = Process.GetProcessById((int)pid).ProcessName; } catch { }
            bool isHs = !string.IsNullOrEmpty(procName) && procName.Equals("Hearthstone", StringComparison.OrdinalIgnoreCase);

            if (isHs != HearthstoneIsForeground)
            {
                HearthstoneIsForeground = isHs;
                OnChange?.Invoke(isHs);
            }
        }

        public void Dispose() => _timer.Dispose();

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
