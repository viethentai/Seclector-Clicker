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

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            // Giữ nguyên file lỗi để người dùng có thể khôi phục thủ công.
            string backupPath = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            try
            {
                File.Copy(path, backupPath, overwrite: false);
            }
            catch (IOException)
            {
                // Không làm ứng dụng dừng chỉ vì không thể tạo bản sao lưu.
            }
            catch (UnauthorizedAccessException)
            {
                // Tiếp tục với danh sách rỗng; file gốc không bị xóa.
            }

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static void Save(IReadOnlyDictionary<string, string> selectors)
    {
        string path = StorePath;
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        string temporaryPath = Path.Combine(directory, $".selectors-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(selectors, JsonOptions));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static string StorePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoClickSelector",
        "selectors.json");
}
