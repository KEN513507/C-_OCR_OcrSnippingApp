param(
    [Parameter(Mandatory = $true)]
    [string]$Modifiers,

    [Parameter(Mandatory = $true)]
    [string]$Key
)

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class HotkeyProbe
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

        private static uint ParseModifiers(string modifiers)
        {
            uint result = 0;
            var parts = (modifiers ?? string.Empty).Split(new[] {'+'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in parts)
            {
                var part = raw.Trim().ToLowerInvariant();
                switch (part)
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

    public static string TryRegister(string modifiers, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "REGISTER_FAIL:InvalidKey";
        }

        uint virtualKey = (uint)char.ToUpperInvariant(key[0]);
        uint modifierFlags = ParseModifiers(modifiers ?? string.Empty);

        bool registered = RegisterHotKey(IntPtr.Zero, 99, modifierFlags, virtualKey);
        int error = Marshal.GetLastWin32Error();

        if (registered)
        {
            UnregisterHotKey(IntPtr.Zero, 99);
            return "REGISTER_OK";
        }

        return "REGISTER_FAIL:" + error;
    }
}
"@

$result = [HotkeyProbe]::TryRegister($Modifiers, $Key)
Write-Output $result
