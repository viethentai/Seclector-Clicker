using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;

namespace AutoClickSelector.Core;

/// <summary>
/// Mở CốC CốC với cổng remote debugging (9222) để BrowserController có thể kết nối.
/// Không truyền URL nào và không thay đổi hồ sơ (profile) của người dùng.
/// </summary>
internal static class BrowserLauncher
{
    public const int DebugPort = 9222;

    private const string BrowserFileName = "browser.exe";

    /// <summary>Đường dẫn đã lưu trước đó nếu còn tồn tại; nếu không thì thử các vị trí mặc định.</summary>
    public static string? FindExecutable()
    {
        string? saved = LoadSavedExecutable();
        if (!string.IsNullOrWhiteSpace(saved) && File.Exists(saved))
            return saved;

        return GetDefaultCandidates().FirstOrDefault(File.Exists);
    }

    public static void SaveExecutable(string path)
    {
        string file = SettingsFile;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(new BrowserSettings(path)));
    }

    /// <summary>Kiểm tra cổng debug có đang nhận kết nối không (không gửi dữ liệu gì).</summary>
    public static async Task<bool> IsDebugPortOpenAsync(int port = DebugPort)
    {
        using var client = new TcpClient();
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
            await client.ConnectAsync("127.0.0.1", port, timeout.Token);
            return client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<bool> WaitForDebugPortAsync(int port, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsDebugPortOpenAsync(port))
                return true;

            await Task.Delay(300);
        }

        return false;
    }

    public static bool IsBrowserRunning(string exePath)
    {
        List<Process> instances = FindInstances(exePath);
        bool running = instances.Count > 0;
        DisposeAll(instances);
        return running;
    }

    /// <summary>
    /// Yêu cầu các cửa sổ CốC CốC đóng lại (giống bấm nút X, nên trình duyệt có thể lưu phiên).
    /// Trả về true khi không còn tiến trình nào trong thời gian chờ.
    /// </summary>
    public static async Task<bool> CloseBrowserAsync(string exePath, TimeSpan timeout)
    {
        List<Process> instances = FindInstances(exePath);
        foreach (Process process in instances)
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                    process.CloseMainWindow();
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
            {
                // Tiến trình có thể đã thoát; bỏ qua.
            }
        }

        DisposeAll(instances);

        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (!IsBrowserRunning(exePath))
                return true;

            await Task.Delay(300);
        }

        return !IsBrowserRunning(exePath);
    }

    public static void Start(string exePath, int port = DebugPort)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"--remote-debugging-port={port}",
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty
        };

        using Process? process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Hệ điều hành không khởi chạy được tiến trình trình duyệt.");
    }

    private static List<Process> FindInstances(string exePath)
    {
        string fullPath = Path.GetFullPath(exePath);
        string processName = Path.GetFileNameWithoutExtension(fullPath);
        var matches = new List<Process>();

        foreach (Process process in Process.GetProcessesByName(processName))
        {
            bool isMatch = false;
            try
            {
                isMatch = string.Equals(
                    process.MainModule?.FileName,
                    fullPath,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
            {
                // Không đọc được đường dẫn (tiến trình của người dùng khác hoặc đã thoát).
            }

            if (isMatch)
                matches.Add(process);
            else
                process.Dispose();
        }

        return matches;
    }

    private static void DisposeAll(List<Process> processes)
    {
        foreach (Process process in processes)
            process.Dispose();
    }

    private static IEnumerable<string> GetDefaultCandidates()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        yield return Path.Combine(local, "CocCoc", "Browser", "Application", BrowserFileName);
        yield return Path.Combine(programFiles, "CocCoc", "Browser", "Application", BrowserFileName);
        yield return Path.Combine(programFilesX86, "CocCoc", "Browser", "Application", BrowserFileName);
    }

    private static string SettingsFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoClickSelector",
        "browser-settings.json");

    private static string? LoadSavedExecutable()
    {
        try
        {
            if (!File.Exists(SettingsFile))
                return null;

            return JsonSerializer.Deserialize<BrowserSettings>(File.ReadAllText(SettingsFile))?.BrowserPath;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed record BrowserSettings(string? BrowserPath);
}
