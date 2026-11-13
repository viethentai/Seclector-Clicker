using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace AutoClickSelector.Core;

internal static class BrowserLauncher
{
    public const int DebugPort = 9222;

    public static string? FindExecutable(string family)
    {
        if (IsFamily(family, "custom"))
        {
            string? saved = LoadSavedExecutable();
            return saved != null && File.Exists(saved) ? saved : null;
        }

        foreach (string candidate in GetCandidates(family))
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public static void SaveExecutable(string path)
    {
        string file = SettingsFile;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(new BrowserSettings(path)));
    }

    public static async Task<bool> IsDebugPortOpenAsync(int port = DebugPort)
    {
        if (port is < 1 or > 65535)
            return false;

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            string json = await client.GetStringAsync($"http://127.0.0.1:{port}/json/version", timeout.Token);
            using JsonDocument document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("Browser", out JsonElement browser)
                || browser.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(browser.GetString()))
                return false;

            if (!document.RootElement.TryGetProperty("webSocketDebuggerUrl", out JsonElement socket)
                || socket.ValueKind != JsonValueKind.String
                || !Uri.TryCreate(socket.GetString(), UriKind.Absolute, out Uri? socketUri))
                return false;

            return (socketUri.Scheme == "ws" || socketUri.Scheme == "wss")
                && socketUri.IsLoopback;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException or UriFormatException)
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
            Arguments = $"--remote-debugging-address=127.0.0.1 --remote-debugging-port={port}",
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty
        };

        using Process? process = Process.Start(startInfo);

        if (process == null)
            throw new InvalidOperationException("Hệ điều hành không khởi chạy được tiến trình trình duyệt.");
    }

    private static IEnumerable<string> GetCandidates(string family)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        if (IsFamily(family, "edge"))
        {
            yield return Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe");
            yield return Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe");
            yield break;
        }

        if (IsFamily(family, "chrome"))
        {
            yield return Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe");
            yield return Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe");
            yield return Path.Combine(local, "Google", "Chrome", "Application", "chrome.exe");
            yield break;
        }

        yield return Path.Combine(local, "CocCoc", "Browser", "Application", "browser.exe");
        yield return Path.Combine(programFiles, "CocCoc", "Browser", "Application", "browser.exe");
        yield return Path.Combine(programFilesX86, "CocCoc", "Browser", "Application", "browser.exe");
    }

    private static bool IsFamily(string family, string expected) =>
        string.Equals(family, expected, StringComparison.OrdinalIgnoreCase);

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
