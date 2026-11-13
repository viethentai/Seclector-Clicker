using System.Text.Json;

namespace AutoClickSelector.Models;

/// <summary>Lưu tùy chọn giữa các lần mở ứng dụng: chế độ sáng/tối và từ khóa đã nhập.</summary>
internal static class AppPreferences
{
    private const string DefaultKeyword = "Sponsored";

    public static bool LoadDarkMode() => Load().DarkMode;

    public static string LoadKeyword() => Load().Keyword ?? DefaultKeyword;

    public static void SaveDarkMode(bool darkMode) =>
        Update(settings => settings with { DarkMode = darkMode });

    public static void SaveKeyword(string keyword) =>
        Update(settings => settings with { Keyword = keyword });

    private static UiSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile))
                return new UiSettings(false, null);

            return JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(SettingsFile))
                ?? new UiSettings(false, null);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return new UiSettings(false, null);
        }
    }

    private static void Update(Func<UiSettings, UiSettings> change)
    {
        try
        {
            UiSettings next = change(Load());
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(next));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Không lưu được tùy chọn thì bỏ qua; ứng dụng vẫn chạy bình thường.
        }
    }

    private static string SettingsFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoClickSelector",
        "ui-settings.json");

    private sealed record UiSettings(bool DarkMode, string? Keyword);
}
