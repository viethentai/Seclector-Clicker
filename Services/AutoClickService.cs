using AutoClickSelector.Core;
using AutoClickSelector.Models;
using static AutoClickSelector.Services.TextFormatter;

namespace AutoClickSelector.Services;

public sealed class AutoClickService
{
    private const int MaxConsecutiveFailures = 3;

    private readonly BrowserController browser;
    private readonly SemaphoreSlim clickGate;
    private readonly Action<ActivityLevel, string> log;
    private readonly Action<bool> recordClick;
    private readonly Func<CancellationToken, Task<bool>>? recover;

    public AutoClickService(
        BrowserController browser,
        SemaphoreSlim clickGate,
        Action<ActivityLevel, string> log,
        Action<bool> recordClick,
        Func<CancellationToken, Task<bool>>? recover = null)
    {
        this.browser = browser;
        this.clickGate = clickGate;
        this.log = log;
        this.recordClick = recordClick;
        this.recover = recover;
    }

    public async Task RunAsync(
        string selector,
        int intervalMilliseconds,
        int waitTimeoutMilliseconds,
        CancellationToken token)
    {
        int consecutiveFailures = 0;
        string? lastSignature = null;
        DateTime lastClickAt = DateTime.MinValue;
        TimeSpan duplicateWindow = TimeSpan.FromMilliseconds(Math.Max(1500, intervalMilliseconds));

        while (!token.IsCancellationRequested)
        {
            bool connectionLost = false;
            bool skippedDuplicate = false;

            if (!await clickGate.WaitAsync(0))
            {
                await Task.Delay(100, token);
                continue;
            }

            try
            {
                string? signature = SettingsStore.Current.AvoidDuplicateClicks
                    ? await browser.GetElementSignatureAsync(selector)
                    : null;

                if (signature != null && signature == lastSignature &&
                    DateTime.UtcNow - lastClickAt < duplicateWindow)
                {
                    skippedDuplicate = true;
                }
                else
                {
                    await browser.ClickAsync(selector, waitTimeoutMilliseconds, token);

                    lastSignature = signature;
                    lastClickAt = DateTime.UtcNow;
                    consecutiveFailures = 0;
                    recordClick(true);
                    log(ActivityLevel.Success, Localization.T("Đã click Selector."));
                }
            }
            catch (SelectorWaitTimeoutException)
            {
                recordClick(false);
                log(ActivityLevel.Warning, Localization.F(
                    "Không thấy Selector hiển thị sau {0} giây chờ. Dừng chạy thao tác để tránh lặp lại.",
                    Seconds(waitTimeoutMilliseconds)));
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                recordClick(false);
                consecutiveFailures++;

                if (!browser.IsConnected)
                {
                    connectionLost = true;
                }
                else
                {
                    log(ActivityLevel.Error, Localization.F(
                        "Click lỗi ({0}/{1}): {2}",
                        consecutiveFailures,
                        MaxConsecutiveFailures,
                        DescribeError(exception)));

                    if (consecutiveFailures >= MaxConsecutiveFailures)
                        throw;
                }
            }
            finally
            {
                clickGate.Release();
            }

            if (skippedDuplicate)
                log(ActivityLevel.Info, Localization.T("Bỏ qua click trùng: phần tử chưa thay đổi trạng thái."));

            if (connectionLost)
            {
                if (recover is null || !await recover(token))
                {
                    log(ActivityLevel.Error, Localization.T("Mất kết nối trình duyệt và không thể kết nối lại."));
                    throw new InvalidOperationException(Localization.T("Mất kết nối trình duyệt."));
                }

                consecutiveFailures = 0;
                lastSignature = null;
                log(ActivityLevel.Success, Localization.T("Đã kết nối lại. Tiếp tục chạy thao tác."));
            }

            await Task.Delay(intervalMilliseconds, token);
        }
    }
}