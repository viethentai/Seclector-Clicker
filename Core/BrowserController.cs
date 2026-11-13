using AutoClickSelector.Models;
using Microsoft.Playwright;

namespace AutoClickSelector.Core;

/// <summary>
/// Điều khiển CốC CốC qua Playwright (CDP): quét tab, chọn tab, bấm phần tử,
/// đọc chữ đang hiển thị và chuyển video.
/// </summary>
public sealed class BrowserController
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IPage? page;

    public bool IsBrowserConnected => browser?.IsConnected == true;

    public bool IsConnected => browser?.IsConnected == true && page?.IsClosed == false;

    public async Task ConnectAsync(int port = BrowserLauncher.DebugPort)
    {
        if (IsBrowserConnected)
            return;

        await StopAsync();

        IPlaywright? newPlaywright = null;
        IBrowser? newBrowser = null;

        try
        {
            newPlaywright = await Playwright.CreateAsync();
            newBrowser = await newPlaywright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{port}");

            if (newBrowser.Contexts.Count == 0)
                throw new InvalidOperationException("Không tìm thấy Browser Context.");

            playwright = newPlaywright;
            browser = newBrowser;
            page = null;
        }
        catch
        {
            if (newBrowser != null)
            {
                try
                {
                    await newBrowser.CloseAsync();
                }
                catch
                {
                    // Bỏ qua: đang trên đường báo lỗi kết nối gốc.
                }
            }

            newPlaywright?.Dispose();
            throw;
        }
    }

    public async Task<List<BrowserTab>> GetTabsAsync()
    {
        var result = new List<BrowserTab>();

        if (browser?.IsConnected != true)
            throw new InvalidOperationException("Chưa kết nối CốC CốC.");

        foreach (IBrowserContext context in browser.Contexts)
        {
            foreach (IPage tabPage in context.Pages)
            {
                if (tabPage.IsClosed)
                    continue;

                try
                {
                    string url = tabPage.Url;
                    string title;

                    try
                    {
                        title = await tabPage.TitleAsync();
                    }
                    catch
                    {
                        if (tabPage.IsClosed)
                            continue;

                        title = "(Không lấy được tiêu đề)";
                    }

                    if (string.IsNullOrWhiteSpace(title))
                        title = "(Không có tiêu đề)";

                    result.Add(new BrowserTab(tabPage, title, url));
                }
                catch when (tabPage.IsClosed)
                {
                    // Tab có thể đóng trong lúc đang quét danh sách.
                }
            }
        }

        return result;
    }

    public bool IsCurrentPage(BrowserTab tab) => ReferenceEquals(tab.Page, page);

    public async Task SelectTabAsync(BrowserTab tab, bool bringToFront = true)
    {
        if (browser?.IsConnected != true)
            throw new InvalidOperationException("Chưa kết nối CốC CốC.");

        if (tab.Page.IsClosed)
            throw new InvalidOperationException("Tab đã đóng. Hãy quét lại danh sách tab.");

        page = tab.Page;
        await UseBrowserColorSchemeAsync(page);

        if (bringToFront)
            await page.BringToFrontAsync();
    }

    /// <summary>
    /// Đặt prefers-color-scheme của trang theo chế độ ứng dụng mặc định của Windows, để các trang
    /// theo máy hiển thị đúng với hệ thống thay vì bị ép sáng. Trang tự chọn sáng/tối bằng nút trên web
    /// không bị ảnh hưởng. Nếu trang đã đóng thì bỏ qua.
    /// </summary>
    private static async Task UseBrowserColorSchemeAsync(IPage target)
    {
        try
        {
            ColorScheme scheme = WindowsTheme.AppsUseDarkTheme() ? ColorScheme.Dark : ColorScheme.Light;
            await target.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = scheme });
        }
        catch (PlaywrightException)
        {
            // Trang có thể vừa đóng; không cần báo lỗi.
        }
    }

    /// <summary>
    /// Chờ Selector hiển thị rồi bấm. Dùng vòng lặp kiểm tra thay vì chờ của Playwright
    /// để nút Dừng luôn phản hồi ngay.
    /// </summary>
    public async Task ClickAsync(
        string selector,
        int waitTimeoutMilliseconds = 3000,
        CancellationToken cancellationToken = default)
    {
        IPage currentPage = RequireSelectedPage();

        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("CSS Selector không được để trống.");

        // .First: nếu Selector khớp nhiều phần tử thì vẫn bấm được phần tử đầu tiên,
        // thay vì ném lỗi "strict mode violation" của Playwright.
        ILocator element = currentPage.Locator(selector).First;

        int waitMilliseconds = Math.Max(1, waitTimeoutMilliseconds);
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMilliseconds);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await element.IsVisibleAsync())
                break;

            if (DateTime.UtcNow >= deadline)
                throw new SelectorWaitTimeoutException(waitMilliseconds);

            await Task.Delay(100, cancellationToken);
        }

        // Playwright click có thể chờ actionability (scroll/overlay/animation).
        // Giới hạn riêng thao tác click để lệnh Dừng không bị treo hàng phút.
        cancellationToken.ThrowIfCancellationRequested();
        await element.ClickAsync(new LocatorClickOptions
        {
            Timeout = Math.Clamp(waitMilliseconds, 1, 5000)
        });
    }

    /// <summary>
    /// Tìm từ khóa trong chữ đang nằm trong khung nhìn. TikTok giữ sẵn các video kế bên
    /// ngoài màn hình, nên không được quét toàn trang: nếu không sẽ thấy chữ "Sponsored"
    /// của video tiếp theo quá sớm.
    /// </summary>
    public async Task<(string Keyword, string Snippet)?> FindAdOnScreenAsync(IReadOnlyList<string> keywords)
    {
        string[] found = await EvaluateOnScreenTextAsync(keywords, list: false);
        return found.Length >= 2 ? (found[0], found[1]) : null;
    }

    /// <summary>Chẩn đoán: chữ đang hiển thị trên màn hình. Dòng khớp từ khóa có dấu ★ phía trước.</summary>
    public async Task<List<string>> GetVisibleTextAsync(IReadOnlyList<string> keywords)
    {
        string[] found = await EvaluateOnScreenTextAsync(keywords, list: true);
        return found.ToList();
    }

    /// <summary>
    /// Chuyển sang video kế tiếp: bấm Selector nếu nút đang hiển thị,
    /// nếu không thì nhấn phím mũi tên xuống.
    /// </summary>
    public async Task<string> GoToNextVideoAsync(
        string selector,
        int waitTimeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        IPage currentPage = RequireSelectedPage();

        if (!string.IsNullOrWhiteSpace(selector))
        {
            try
            {
                await ClickAsync(selector, waitTimeoutMilliseconds, cancellationToken);
                return "nút Selector";
            }
            catch (SelectorWaitTimeoutException)
            {
                // Nút không hiển thị: dùng bàn phím ở bên dưới.
            }
        }

        await currentPage.Keyboard.PressAsync("ArrowDown");
        return "phím ↓ (không thấy nút Selector)";
    }

    /// <summary>
    /// Đưa tab đang chọn lên trước và nâng cửa sổ trình duyệt lên trên các cửa sổ khác.
    /// Trả về false nếu không tìm được cửa sổ.
    /// </summary>
    public async Task<bool> BringBrowserToForegroundAsync()
    {
        IPage currentPage = RequireSelectedPage();

        await currentPage.BringToFrontAsync();

        // Cho trình duyệt một chút thời gian để cập nhật tiêu đề cửa sổ.
        await Task.Delay(200);

        var candidates = new List<string>();

        try
        {
            string title = await currentPage.TitleAsync();
            if (!string.IsNullOrWhiteSpace(title))
                candidates.Add(title.Trim());
        }
        catch (PlaywrightException)
        {
            // Dùng URL bên dưới làm phương án thay thế.
        }

        if (!string.IsNullOrWhiteSpace(currentPage.Url))
            candidates.Add(currentPage.Url);

        return WindowActivator.ActivateChromiumWindow(candidates);
    }

    public async Task<SelectorValidationResult> ValidateSelectorAsync(string selector)
    {
        IPage currentPage = RequireSelectedPage();

        if (string.IsNullOrWhiteSpace(selector))
            return new SelectorValidationResult(false, 0, 0, "Selector đang trống.");

        try
        {
            ILocator locator = currentPage.Locator(selector);
            int count = await locator.CountAsync();
            int visibleCount = 0;

            for (int index = 0; index < count; index++)
            {
                if (await locator.Nth(index).IsVisibleAsync())
                    visibleCount++;
            }

            return new SelectorValidationResult(true, count, visibleCount, null);
        }
        catch (Exception exception)
        {
            return new SelectorValidationResult(false, 0, 0, exception.Message);
        }
    }

    /// <summary>
    /// Đưa người dùng sang trình duyệt để bấm chọn một phần tử; trả về CSS Selector
    /// được tạo, hoặc null nếu hủy bằng phím Esc (hoặc hết 2 phút).
    /// </summary>
    public async Task<string?> PickElementAsync()
    {
        IPage currentPage = RequireSelectedPage();

        await currentPage.BringToFrontAsync();
        return await currentPage.EvaluateAsync<string?>(PickElementScript);
    }

    public async Task RefreshPageAsync()
    {
        IPage currentPage = RequireSelectedPage();

        await currentPage.ReloadAsync(new PageReloadOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
    }

    public async Task StopAsync()
    {
        if (browser != null)
        {
            try
            {
                await browser.CloseAsync();
            }
            catch
            {
                // Trình duyệt có thể đã đóng sẵn.
            }

            browser = null;
        }

        playwright?.Dispose();
        playwright = null;
        page = null;
    }

    private IPage RequireSelectedPage()
    {
        IPage? currentPage = page;
        if (currentPage == null || currentPage.IsClosed)
            throw new InvalidOperationException("Chưa chọn tab trình duyệt.");

        return currentPage;
    }

    private async Task<string[]> EvaluateOnScreenTextAsync(IReadOnlyList<string> keywords, bool list)
    {
        IPage currentPage = RequireSelectedPage();

        string[]? result = await currentPage.MainFrame.EvaluateAsync<string[]>(
            OnScreenTextScript,
            new { keywords = keywords.ToArray(), list });

        return result ?? Array.Empty<string>();
    }

    // Trả về [từ khóa, đoạn chữ] cho lần khớp đầu tiên (chế độ tìm kiếm),
    // hoặc danh sách chữ hiển thị (chế độ chẩn đoán).
    private const string OnScreenTextScript = """
        (arg) => {
            if (!document.body) return [];

            const normalize = (value) => String(value || "")
                .toLowerCase()
                .normalize("NFD")
                .replace(/[\u0300-\u036f]/g, "")
                .replace(/đ/g, "d")
                .replace(/[\u200b-\u200d\ufeff]/g, "")
                .replace(/\s+/g, " ");

            const needles = arg.keywords
                .map(original => ({ original, key: normalize(original).trim() }))
                .filter(needle => needle.key);

            const isShown = (element) => {
                if (typeof element.checkVisibility === "function") {
                    return element.checkVisibility({ checkOpacity: true, checkVisibilityCSS: true });
                }
                const style = getComputedStyle(element);
                return style.display !== "none" && style.visibility !== "hidden" && Number(style.opacity) > 0;
            };

            const onScreen = (node) => {
                const element = node.parentElement;
                if (!element || !isShown(element)) return false;
                const range = document.createRange();
                range.selectNodeContents(node);
                const rect = range.getBoundingClientRect();
                if (rect.width < 1 || rect.height < 1) return false;
                const x = rect.left + rect.width / 2;
                const y = rect.top + rect.height / 2;
                return x >= 0 && x <= window.innerWidth && y >= 0 && y <= window.innerHeight;
            };

            const results = [];
            const seen = new Set();
            const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
            let node;
            while ((node = walker.nextNode())) {
                const raw = (node.nodeValue || "").trim();
                if (!raw) continue;

                const tag = node.parentElement ? node.parentElement.tagName : "";
                if (tag === "SCRIPT" || tag === "STYLE" || tag === "NOSCRIPT") continue;

                const text = normalize(raw);
                const hit = needles.find(needle => text.includes(needle.key));
                if (!arg.list && !hit) continue;
                if (!onScreen(node)) continue;

                if (!arg.list) return [hit.original, raw.slice(0, 80)];

                const snippet = raw.slice(0, 60);
                if (seen.has(snippet)) continue;
                seen.add(snippet);
                results.push((hit ? "★ " : "") + snippet);
                if (results.length >= 40) break;
            }

            return arg.list ? results : [];
        }
        """;

    // Phủ lên trang một lớp chọn phần tử. Chặn các sự kiện chuột để không kích hoạt
    // liên kết hay nút của trang trong lúc đang chọn.
    private const string PickElementScript = """
        () => new Promise((resolve) => {
            const root = document.documentElement;
            const oldCursor = root.style.cursor;
            const overlay = document.createElement('div');
            let finished = false;
            const timer = setTimeout(() => finish(null), 120000);

            overlay.textContent = 'Bấm vào phần tử cần chọn — Esc để hủy';
            Object.assign(overlay.style, {
                position: 'fixed', top: '12px', left: '50%',
                transform: 'translateX(-50%)', zIndex: '2147483647',
                padding: '10px 16px', borderRadius: '8px',
                background: '#202124', color: '#fff',
                font: '14px sans-serif', boxShadow: '0 2px 12px #0008',
                pointerEvents: 'none'
            });
            const highlight = document.createElement('div');
            Object.assign(highlight.style, {
                position: 'fixed', zIndex: '2147483646',
                pointerEvents: 'none', display: 'none',
                boxSizing: 'border-box', borderRadius: '2px',
                border: '2px solid #1a73e8',
                background: 'rgba(26, 115, 232, 0.15)'
            });
            const host = document.body || root;
            host.appendChild(highlight);
            host.appendChild(overlay);
            root.style.cursor = 'crosshair';

            const blockedEvents = [
                'pointerdown', 'pointerup', 'mousedown', 'mouseup',
                'auxclick', 'dblclick', 'contextmenu',
                'touchstart', 'touchend'
            ];

            function onMove(event) {
                const target = event.target instanceof Element ? event.target : null;
                if (!target || target === overlay || target === highlight) return;
                const rect = target.getBoundingClientRect();
                Object.assign(highlight.style, {
                    display: 'block',
                    left: rect.left + 'px', top: rect.top + 'px',
                    width: rect.width + 'px', height: rect.height + 'px'
                });
            }

            function onBlocked(event) {
                event.preventDefault();
                event.stopPropagation();
                event.stopImmediatePropagation();
            }

            function finish(value) {
                if (finished) return;
                finished = true;
                clearTimeout(timer);
                document.removeEventListener('click', onClick, true);
                document.removeEventListener('keydown', onKey, true);
                document.removeEventListener('mousemove', onMove, true);
                blockedEvents.forEach(name =>
                    document.removeEventListener(name, onBlocked, true));
                root.style.cursor = oldCursor;
                overlay.remove();
                highlight.remove();
                resolve(value);
            }

            function makeSelector(element) {
                if (element.id) {
                    const byId = '#' + CSS.escape(element.id);
                    if (document.querySelectorAll(byId).length === 1) return byId;
                }

                const tag = element.localName;
                for (const attr of ['data-testid', 'data-test', 'name', 'aria-label']) {
                    const value = element.getAttribute(attr);
                    if (!value) continue;
                    const escaped = value.replaceAll('\\', '\\\\').replaceAll('"', '\\"');
                    const candidate = `${tag}[${attr}="${escaped}"]`;
                    if (document.querySelectorAll(candidate).length === 1) return candidate;
                }

                const classes = [...element.classList]
                    .filter(name => !/^([a-f0-9]{8,}|css-|sc-)/i.test(name))
                    .map(name => '.' + CSS.escape(name));
                if (classes.length) {
                    const candidate = tag + classes.join('');
                    if (document.querySelectorAll(candidate).length === 1) return candidate;
                }

                const parts = [];
                let current = element;
                while (current && current.nodeType === Node.ELEMENT_NODE) {
                    let part = current.localName;
                    if (current.id) {
                        part = '#' + CSS.escape(current.id);
                        parts.unshift(part);
                        break;
                    }
                    const siblings = current.parentElement
                        ? [...current.parentElement.children].filter(item => item.localName === current.localName)
                        : [];
                    if (siblings.length > 1)
                        part += `:nth-of-type(${siblings.indexOf(current) + 1})`;
                    parts.unshift(part);
                    current = current.parentElement;
                }
                return parts.join(' > ');
            }

            function onClick(event) {
                const target = event.target instanceof Element ? event.target : null;
                if (!target || target === overlay || overlay.contains(target)) return;
                event.preventDefault();
                event.stopPropagation();
                event.stopImmediatePropagation();
                finish(makeSelector(target));
            }

            function onKey(event) {
                if (event.key !== 'Escape') return;
                event.preventDefault();
                event.stopImmediatePropagation();
                finish(null);
            }

            document.addEventListener('click', onClick, true);
            document.addEventListener('keydown', onKey, true);
            document.addEventListener('mousemove', onMove, true);
            blockedEvents.forEach(name =>
                document.addEventListener(name, onBlocked, true));
        })
        """;
}
