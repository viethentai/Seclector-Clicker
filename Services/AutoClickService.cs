using AutoClickSelector.Core;
using AutoClickSelector.Models;
using static AutoClickSelector.Services.TextFormatter;

namespace AutoClickSelector.Services;

/// <summary>Bấm Selector theo chu kỳ cho tới khi bị hủy hoặc gặp lỗi không thể phục hồi.</summary>
public sealed class AutoClickService
{
    private const int MaxConsecutiveFailures = 3;

    private readonly BrowserController browser;
    private readonly SemaphoreSlim clickGate;
    private readonly Action<ActivityLevel, string> log;
    private readonly Action<bool> recordClick;

    /// <param name="clickGate">Khóa dùng chung để hai chức năng không bấm cùng lúc.</param>
    /// <param name="log">Ghi một dòng vào nhật ký.</param>
    /// <param name="recordClick">Ghi nhận một lần click, true nếu thành công.</param>
    public AutoClickService(
        BrowserController browser,
        SemaphoreSlim clickGate,
        Action<ActivityLevel, string> log,
        Action<bool> recordClick)
    {
        this.browser = browser;
        this.clickGate = clickGate;
        this.log = log;
        this.recordClick = recordClick;
    }

    public async Task RunAsync(
        string selector,
        int intervalMilliseconds,
        int waitTimeoutMilliseconds,
        CancellationToken token)
    {
        int consecutiveFailures = 0;

        while (!token.IsCancellationRequested)
        {
            if (!await clickGate.WaitAsync(0))
            {
                await Task.Delay(100, token);
                continue;
            }

            try
            {
                await browser.ClickAsync(selector, waitTimeoutMilliseconds, token);

                consecutiveFailures = 0;
                recordClick(true);
                log(ActivityLevel.Success, "Đã click Selector.");
            }
            catch (SelectorWaitTimeoutException)
            {
                recordClick(false);
                log(ActivityLevel.Warning,
                    $"Không thấy Selector hiển thị sau {Seconds(waitTimeoutMilliseconds)} giây chờ. " +
                    "Dừng chạy thao tác để tránh lặp lại.");
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Lỗi tạm thời (phần tử vừa render lại, click quá thời gian) không nên dừng vòng lặp.
                recordClick(false);
                consecutiveFailures++;
                log(ActivityLevel.Error,
                    $"Click lỗi ({consecutiveFailures}/{MaxConsecutiveFailures}): {DescribeError(exception)}");

                if (!browser.IsConnected || consecutiveFailures >= MaxConsecutiveFailures)
                    throw;
            }
            finally
            {
                clickGate.Release();
            }

            await Task.Delay(intervalMilliseconds, token);
        }
    }
}
