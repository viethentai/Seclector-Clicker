using System.Globalization;
using AutoClickSelector.Core;

namespace AutoClickSelector.Services;

/// <summary>Các hàm định dạng văn bản dùng chung cho giao diện và dịch vụ.</summary>
public static class TextFormatter
{
    /// <summary>Bỏ query string và fragment để nhật ký không ghi token hay tham số phiên.</summary>
    public static string ShortUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            ? uri.GetLeftPart(UriPartial.Path)
            : url;

    /// <summary>"Sponsored, Learn More" thành ["Sponsored", "Learn More"], bỏ trùng lặp.</summary>
    public static List<string> ParseKeywords(string text) =>
        text.Split(
                new[] { ',', ';', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Lấy dòng đầu của thông báo lỗi gốc, đủ ngắn để hiển thị trong nhật ký.</summary>
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

    /// <summary>Thông báo lỗi thân thiện khi không kết nối được với CốC CốC.</summary>
    public static string DescribeBrowserError(Exception exception)
    {
        string detail = exception.GetBaseException().Message.Trim();
        if (string.IsNullOrWhiteSpace(detail))
            detail = "Không có thông tin chi tiết.";

        if (detail.Contains(BrowserLauncher.DebugPort.ToString(), StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("ECONNREFUSED", StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("connectovercdp", StringComparison.OrdinalIgnoreCase))
        {
            return "Không kết nối được với CốC CốC. Hãy bấm 'Mở trình duyệt' " +
                   $"(hoặc mở CốC CốC với cổng {BrowserLauncher.DebugPort}) rồi quét lại. Chi tiết: " + detail;
        }

        return "Lỗi trình duyệt: " + detail;
    }

    /// <summary>Đổi mili giây thành chuỗi số giây, ví dụ 10000 thành "10", 1500 thành "1,5".</summary>
    public static string Seconds(int milliseconds) =>
        (milliseconds / 1000d).ToString("0.#", CultureInfo.CurrentCulture);
}
