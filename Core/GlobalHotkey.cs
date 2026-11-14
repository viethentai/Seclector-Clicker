using System.Runtime.InteropServices;

namespace AutoClickSelector.Core;

internal static class GlobalHotkey
{
    public const int WmHotkey = 0x0312;

    public const int StartId = 0x4101;
    public const int StopId = 0x4102;
    public const int ClickId = 0x4103;

    private const uint NoModifiers = 0;
    private const uint VkF6 = 0x75;
    private const uint VkF7 = 0x76;
    private const uint VkF8 = 0x77;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public static bool RegisterAll(IntPtr handle)
    {
        if (!RegisterHotKey(handle, StartId, NoModifiers, VkF6))
            return false;

        if (!RegisterHotKey(handle, StopId, NoModifiers, VkF7))
        {
            UnregisterHotKey(handle, StartId);
            return false;
        }

        if (!RegisterHotKey(handle, ClickId, NoModifiers, VkF8))
        {
            UnregisterHotKey(handle, StopId);
            UnregisterHotKey(handle, StartId);
            return false;
        }

        return true;
    }

    public static void UnregisterAll(IntPtr handle)
    {
        UnregisterHotKey(handle, StartId);
        UnregisterHotKey(handle, StopId);
        UnregisterHotKey(handle, ClickId);
    }
}
