using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OcrSnippingApp.Services
{
    /// <summary>
    /// メッセージ専用ウィンドウでホットキーを受信
    /// フォームの表示状態に依存せず、常にハンドルを持つ
    /// </summary>
    public sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        const int WM_HOTKEY = 0x0312;
        const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_WIN = 0x0008, MOD_NOREPEAT = 0x4000;

        [DllImport("user32.dll", SetLastError = true)] 
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [DllImport("user32.dll", SetLastError = true)] 
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly int _id = 1;
        public event Action? HotkeyPressed;

        public void EnsureHandle()
        {
            if (Handle != IntPtr.Zero) return;
            CreateHandle(new CreateParams { Caption = "OcrSnippingAppHotkey" });
            Logger.LogInfo($"HotkeyWindow handle created: {Handle}");
        }

        public bool Register(uint mods, Keys key)
        {
            EnsureHandle();
            bool ok = RegisterHotKey(Handle, _id, mods | MOD_NOREPEAT, (uint)key);
            if (ok)
            {
                Logger.LogInfo($"Hotkey registered: mods=0x{mods:X} key={key}");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                var message = new Win32Exception(error).Message;
                Logger.LogError($"Hotkey register failed: Win32Error={error} (0x{error:X}), Message={message}");
            }
            return ok;
        }

        public void Unregister()
        {
            if (Handle == IntPtr.Zero) return;
            UnregisterHotKey(Handle, _id);
            Logger.LogInfo("Hotkey unregistered");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                Logger.LogDebug($"WM_HOTKEY received: WParam={m.WParam}, LParam={m.LParam}");
                HotkeyPressed?.Invoke();
                return;
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            try 
            { 
                Unregister(); 
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Hotkey unregister error: {ex.Message}");
            }
            finally 
            { 
                if (Handle != IntPtr.Zero) 
                {
                    DestroyHandle();
                    Logger.LogInfo("HotkeyWindow handle destroyed");
                }
            }
        }

        public static uint ParseModifiers(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            uint m = 0;
            foreach (var t in s.Split('+', StringSplitOptions.RemoveEmptyEntries))
            {
                switch (t.Trim().ToLowerInvariant())
                {
                    case "ctrl":
                    case "control":
                        m |= MOD_CONTROL; 
                        break;
                    case "alt":
                        m |= MOD_ALT; 
                        break;
                    case "shift":
                        m |= MOD_SHIFT; 
                        break;
                    case "win":
                    case "windows":
                        m |= MOD_WIN; 
                        break;
                }
            }
            return m;
        }

        public static Keys ParseKey(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Keys.None;
            
            // 1文字の場合は大文字に変換
            if (s.Length == 1)
            {
                s = s.ToUpperInvariant();
            }
            
            return Enum.TryParse<Keys>(s, true, out var k) ? k : Keys.None;
        }
    }
}
