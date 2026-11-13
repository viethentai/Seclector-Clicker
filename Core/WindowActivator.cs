using System.Runtime.InteropServices;
using System.Text;

namespace AutoClickSelector.Core;

/// <summary>Hỗ trợ Win32 để chuyển qua lại giữa ứng dụng này và cửa sổ trình duyệt.</summary>
internal static class WindowActivator
{
    private const int SwRestore = 9;
    private const byte VkMenu = 0x12;
    private const uint KeyEventKeyUp = 0x0002;
    private const string ChromiumWindowClass = "Chrome_WidgetWin_1";

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    /// <summary>Khôi phục (nếu đang thu nhỏ) và đưa cửa sổ lên trước.</summary>
    public static bool Activate(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return false;

        if (IsIconic(handle))
            ShowWindow(handle, SwRestore);

        // Nhấn và thả Alt cho phép tiến trình này lấy quyền foreground từ ứng dụng khác.
        keybd_event(VkMenu, 0, 0, UIntPtr.Zero);
        keybd_event(VkMenu, 0, KeyEventKeyUp, UIntPtr.Zero);

        return SetForegroundWindow(handle);
    }

    /// <summary>
    /// Tìm cửa sổ Chromium có tiêu đề bắt đầu bằng một trong các giá trị gợi ý
    /// (tiêu đề hoặc URL của tab đang chọn) rồi đưa cửa sổ đó lên trước.
    /// </summary>
    public static bool ActivateChromiumWindow(IEnumerable<string> titleCandidates)
    {
        var candidates = titleCandidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .ToList();

        if (candidates.Count == 0)
            return false;

        IntPtr found = IntPtr.Zero;
        var className = new StringBuilder(256);

        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle))
                return true;

            className.Clear();
            GetClassName(handle, className, className.Capacity);
            if (className.ToString() != ChromiumWindowClass)
                return true;

            int length = GetWindowTextLength(handle);
            if (length == 0)
                return true;

            var title = new StringBuilder(length + 1);
            GetWindowText(handle, title, title.Capacity);
            string windowTitle = title.ToString();

            foreach (string candidate in candidates)
            {
                if (windowTitle.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    found = handle;
                    return false;
                }
            }

            return true;
        }, IntPtr.Zero);

        return found != IntPtr.Zero && Activate(found);
    }
}
