using System.Diagnostics;
using AutoClickSelector.Core;
using AutoClickSelector.Models;
using static AutoClickSelector.Services.TextFormatter;

namespace AutoClickSelector.Services;

/// <summary>
/// Theo dõi video đang hiển thị. Khi thấy từ khóa (ví dụ quảng cáo) thì bấm Selector
/// hoặc phím mũi tên xuống để chuyển sang video kế tiếp.
/// </summary>
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

    public KeywordWatcherService(
        BrowserController browser,
        SemaphoreSlim clickGate,
        Action<ActivityLevel, string> log,
        Action<bool> recordClick,
        Action<KeywordState> setState)
    {
        this.browser = browser;
        this.clickGate = clickGate;
        this.log = log;
        this.recordClick = recordClick;
        this.setState = setState;
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
                        log(ActivityLevel.Process, $"Đang chờ từ khóa... (đã xử lý {totalSkipped} lần)");
                    }
                }
                else
                {
                    var (keyword, snippet) = ad.Value;

                    // Nếu chuyển video liên tiếp mà từ khóa vẫn còn, khả năng cao là từ khóa
                    // trùng với chữ cố định trên trang hoặc nút chuyển không hoạt động.
                    if (consecutiveSkips >= MaxConsecutiveSkips)
                    {
                        log(ActivityLevel.Warning,
                            $"Đã chuyển video liên tiếp {MaxConsecutiveSkips} lần nhưng vẫn thấy \"{keyword}\" " +
                            $"({snippet}). Có thể từ khóa trùng với chữ cố định trên trang, hoặc nút " +
                            "'Video tiếp theo' không hoạt động. Đã dừng chức năng này.");
                        return;
                    }

                    setState(KeywordState.Processing);
                    log(ActivityLevel.Info, $"Phát hiện từ khóa \"{keyword}\" trong \"{snippet}\". Đang chuyển video...");

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
                    log(ActivityLevel.Success, $"Đã chuyển video bằng {how}.");

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
                    log(ActivityLevel.Error,
                        "Tab hoặc trình duyệt đã đóng hoặc không còn hoạt động. Hãy quét tab và chọn lại.");
                    throw;
                }

                log(ActivityLevel.Error,
                    $"Lỗi ({consecutiveFailures}/{MaxConsecutiveFailures}): {DescribeError(exception)}");

                if (consecutiveFailures >= MaxConsecutiveFailures)
                    throw;
            }

            await Task.Delay(delay, token);
        }
    }
}
