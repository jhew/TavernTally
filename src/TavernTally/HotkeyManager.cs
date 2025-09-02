using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TavernTally
{
    public class HotkeyManager : IDisposable
    {
        private readonly Window _window;
        private HwndSource? _source;
        private const int WM_HOTKEY = 0x0312;

        public event Action? ToggleOverlay;
        public event Action? IncreaseScale;
        public event Action? DecreaseScale;
        public event Action? ToggleManualBattlegrounds;
        public event Action? IncreaseShopCount;
        public event Action? DecreaseShopCount;
        public event Action? ResetBattlegrounds;

        public HotkeyManager(Window window) { _window = window; }

        public void Register()
        {
            _source = HwndSource.FromHwnd(new WindowInteropHelper(_window).EnsureHandle());
            _source.AddHook(WndProc);

            // F8
            RegisterHotKey(_source.Handle, 1, 0, 0x77);
            // Ctrl+=
            RegisterHotKey(_source.Handle, 2, MOD_CONTROL, 0xBB);
            // Ctrl+-
            RegisterHotKey(_source.Handle, 3, MOD_CONTROL, 0xBD);
            // Ctrl+F8 (Manual Battlegrounds toggle)
            RegisterHotKey(_source.Handle, 4, MOD_CONTROL, 0x77);
            // Ctrl+Shift+= (Increase shop count)
            RegisterHotKey(_source.Handle, 5, MOD_CONTROL | MOD_SHIFT, 0xBB);
            // Ctrl+Shift+- (Decrease shop count)  
            RegisterHotKey(_source.Handle, 6, MOD_CONTROL | MOD_SHIFT, 0xBD);
            // Ctrl+F9 (Reset Battlegrounds detection)
            RegisterHotKey(_source.Handle, 7, MOD_CONTROL, 0x78);
        }

        public void Unregister()
        {
            if (_source == null) return;
            UnregisterHotKey(_source.Handle, 1);
            UnregisterHotKey(_source.Handle, 2);
            UnregisterHotKey(_source.Handle, 3);
            UnregisterHotKey(_source.Handle, 4);
            UnregisterHotKey(_source.Handle, 5);
            UnregisterHotKey(_source.Handle, 6);
            UnregisterHotKey(_source.Handle, 7);
            _source.RemoveHook(WndProc);
            _source = null;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == 1) ToggleOverlay?.Invoke();
                else if (id == 2) IncreaseScale?.Invoke();
                else if (id == 3) DecreaseScale?.Invoke();
                else if (id == 4) ToggleManualBattlegrounds?.Invoke();
                else if (id == 5) IncreaseShopCount?.Invoke();
                else if (id == 6) DecreaseShopCount?.Invoke();
                else if (id == 7) ResetBattlegrounds?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose() => Unregister();

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
