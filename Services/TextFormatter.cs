using System.Globalization;
using AutoClickSelector.Core;

namespace AutoClickSelector.Services;

public static class TextFormatter
{
    public static string ShortUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            ? uri.GetLeftPart(UriPartial.Path)
            : url;

    public static List<string> ParseKeywords(string text) =>
        text.Split(
                new[] { ',', ';', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string DescribeError(Exception exception)
    {
        string message = exception.GetBaseException().Message;
        string? firstLine = message
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrEmpty(firstLine)
            ? exception.GetType().Name
            : firstLine;
    }

    public static string DescribeBrowserError(Exception exception)
    {
        string detail = exception.GetBaseException().Message.Trim();
        if (string.IsNullOrWhiteSpace(detail))
            detail = "Không có thông tin chi tiết.";

        if (detail.Contains(BrowserLauncher.DebugPort.ToString(), StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("ECONNREFUSED", StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("connectovercdp", StringComparison.OrdinalIgnoreCase))
        {
            return "Không kết nối được với trình duyệt. Hãy bấm 'Mở trình duyệt' " +
                   $"(hoặc mở trình duyệt với cổng {BrowserLauncher.DebugPort}) rồi quét lại. Chi tiết: " + detail;
        }

        return "Lỗi trình duyệt: " + detail;
    }

    public static string Seconds(int milliseconds) =>
        (milliseconds / 1000d).ToString("0.#", CultureInfo.CurrentCulture);
}
