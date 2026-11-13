using Microsoft.Win32;

namespace AutoClickSelector.Core;

/// <summary>
/// Đọc chế độ màu mặc định cho ứng dụng của Windows
/// (Cài đặt > Cá nhân hóa > Màu sắc > Chọn chế độ ứng dụng mặc định).
/// </summary>
internal static class WindowsTheme
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool AppsUseDarkTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            object? value = key?.GetValue("AppsUseLightTheme");
            return value is int lightFlag && lightFlag == 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
