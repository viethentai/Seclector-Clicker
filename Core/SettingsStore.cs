using System.Text.Json;
using AutoClickSelector.Models;

namespace AutoClickSelector.Core;

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private static AppSettings? current;

    public static AppSettings Current => current ??= Load();

    public static bool Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);

            string temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(Current, Options));
            File.Move(temporary, FilePath, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static bool Update(Action<AppSettings> change)
    {
        change(Current);
        return Save();
    }

    private static AppSettings Load()
    {
        AppSettings? settings = TryRead(FilePath) ?? TryRead(LegacyPath);
        return settings ?? new AppSettings();
    }

    private static AppSettings? TryRead(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoClickSelector");

    private static string FilePath => Path.Combine(Folder, "app-settings.json");

    private static string LegacyPath => Path.Combine(Folder, "ui-settings.json");
}
