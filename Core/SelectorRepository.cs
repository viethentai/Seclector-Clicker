using System.Text.Json;

namespace AutoClickSelector.Core;

/// <summary>Đọc và ghi danh sách CSS Selector đã lưu. Không phụ thuộc giao diện.</summary>
internal static class SelectorRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static Dictionary<string, string> Load()
    {
        string path = StorePath;
        if (!File.Exists(path))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static void Save(IReadOnlyDictionary<string, string> selectors)
    {
        string path = StorePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(selectors, JsonOptions));
    }

    private static string StorePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoClickSelector",
        "selectors.json");
}
