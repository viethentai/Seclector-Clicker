using System.Diagnostics;
using AutoClickSelector.Core;
using AutoClickSelector.Models;
using static AutoClickSelector.Services.TextFormatter;

namespace AutoClickSelector.Services;

public sealed class KeywordWatcherService
{
    private const int PollMilliseconds = 500;
    private const int AfterSkipMilliseconds = 1500;
    private const int NextButtonWaitMilliseconds = 1500;
    private const int MaxConsecutiveSkips = 5;
    private const int MaxConsecutiveFailures = 3;
    private const int HeartbeatSeconds = 30;

    private readonly BrowserController browser;
    private readonly SemaphoreSlim clickGate;
    private readonly Action<ActivityLevel, string> log;
    private readonly Action<bool> recordClick;
    private readonly Action<KeywordState> setState;
    private readonly Func<CancellationToken, Task<bool>>? recover;

    public KeywordWatcherService(
        BrowserController browser,
        SemaphoreSlim clickGate,
        Action<ActivityLevel, string> log,
        Action<bool> recordClick,
        Action<KeywordState> setState,
        Func<CancellationToken, Task<bool>>? recover = null)
    {
        this.browser = browser;
        this.clickGate = clickGate;
        this.log = log;
        this.recordClick = recordClick;
        this.setState = setState;
        this.recover = recover;
    }

    public async Task RunAsync(string selector, IReadOnlyList<string> keywords, CancellationToken token)
    {
        int consecutiveFailures = 0;
        int consecutiveSkips = 0;
        int totalSkipped = 0;
        var heartbeat = Stopwatch.StartNew();

        while (!token.IsCancellationRequested)
        {
            int delay = PollMilliseconds;
            bool connectionLost = false;

            try
            {
                var ad = await browser.FindAdOnScreenAsync(keywords);

                if (ad is null)
                {
                    consecutiveFailures = 0;
                    consecutiveSkips = 0;
                    setState(KeywordState.Waiting);

                    if (heartbeat.Elapsed.TotalSeconds >= HeartbeatSeconds)
                    {
                        heartbeat.Restart();
                        log(ActivityLevel.Process, Localization.F(
                            "Đang chờ từ khóa... (đã xử lý {0} lần)", totalSkipped));
                    }
                }
                else
                {
                    var (keyword, snippet) = ad.Value;

                    if (consecutiveSkips >= MaxConsecutiveSkips)
                    {
                        log(ActivityLevel.Warning, Localization.F(
                            "Đã chuyển video liên tiếp {0} lần nhưng vẫn thấy \"{1}\" ({2}). Có thể từ khóa trùng với chữ cố định trên trang, hoặc nút 'Video tiếp theo' không hoạt động. Đã dừng chức năng này.",
                            MaxConsecutiveSkips,
                            keyword,
                            snippet));
                        return;
                    }

                    setState(KeywordState.Processing);
                    log(ActivityLevel.Info, Localization.F(
                        "Phát hiện từ khóa \"{0}\" trong \"{1}\". Đang chuyển video...", keyword, snippet));

                    string how;
                    await clickGate.WaitAsync(token);
                    try
                    {
                        how = await browser.GoToNextVideoAsync(selector, NextButtonWaitMilliseconds, token);
                    }
                    finally
                    {
                        clickGate.Release();
                    }

                    consecutiveFailures = 0;
                    consecutiveSkips++;
                    totalSkipped++;
                    recordClick(true);
                    log(ActivityLevel.Success, Localization.F("Đã chuyển video bằng {0}.", how));

                    setState(KeywordState.Waiting);
                    delay = AfterSkipMilliseconds;
                    heartbeat.Restart();
                }
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
                        "Lỗi ({0}/{1}): {2}",
                        consecutiveFailures,
                        MaxConsecutiveFailures,
                        DescribeError(exception)));

                    if (consecutiveFailures >= MaxConsecutiveFailures)
                        throw;
                }
            }

            if (connectionLost)
            {
                setState(KeywordState.Processing);

                if (recover is null || !await recover(token))
                {
                    log(ActivityLevel.Error, Localization.T(
                        "Tab hoặc trình duyệt đã đóng và không thể kết nối lại. Hãy quét tab và chọn lại."));
                    throw new InvalidOperationException(Localization.T("Mất kết nối trình duyệt."));
                }

                consecutiveFailures = 0;
                log(ActivityLevel.Success, Localization.T("Đã kết nối lại. Tiếp tục theo dõi từ khóa."));
                setState(KeywordState.Waiting);
            }

            await Task.Delay(delay, token);
        }
    }
}