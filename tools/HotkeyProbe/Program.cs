using System;
using System.Runtime.InteropServices;

namespace HotkeyProbe
{
    internal static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        private static int Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                Console.WriteLine("Usage: HotkeyProbe <Modifiers> <Key>\nExample: HotkeyProbe \"Ctrl+Shift\" W");
                return 1;
            }

            var modifiers = args[0];
            var key = args.Length > 1 ? args[1] : string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                Console.WriteLine("Key must be provided.");
                return 1;
            }

            uint virtualKey = (uint)char.ToUpperInvariant(key[0]);
            uint modifierFlags = ParseModifiers(modifiers);

            bool registered = RegisterHotKey(IntPtr.Zero, 99, modifierFlags, virtualKey);
            int error = Marshal.GetLastWin32Error();

            if (registered)
            {
                Console.WriteLine("REGISTER_OK");
                UnregisterHotKey(IntPtr.Zero, 99);
                return 0;
            }
            else
            {
                Console.WriteLine($"REGISTER_FAIL:{error}");
                return error == 0 ? 1 : error;
            }
        }

        private static uint ParseModifiers(string modifiers)
        {
            uint result = 0;
            foreach (var part in modifiers.Split('+', StringSplitOptions.RemoveEmptyEntries))
            {
                switch (part.Trim().ToLowerInvariant())
                {
                    case "ctrl":
                    case "control":
                        result |= MOD_CONTROL;
                        break;
                    case "alt":
                        result |= MOD_ALT;
                        break;
                    case "shift":
                        result |= MOD_SHIFT;
                        break;
                    case "win":
                    case "windows":
                        result |= MOD_WIN;
                        break;
                }
            }

            return result | MOD_NOREPEAT;
        }
    }
}
