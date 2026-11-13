using AutoClickSelector.Models;
using Microsoft.Playwright;

namespace AutoClickSelector.Core;

public sealed class BrowserController
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IPage? page;
    private bool isPickingElement;
    private bool cancelPickRequested;

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
            throw new InvalidOperationException("Chưa kết nối trình duyệt.");

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
                }
            }
        }

        return result;
    }

    public bool IsCurrentPage(BrowserTab tab) => ReferenceEquals(tab.Page, page);

    public async Task SelectTabAsync(BrowserTab tab, bool bringToFront = true)
    {
        if (browser?.IsConnected != true)
            throw new InvalidOperationException("Chưa kết nối trình duyệt.");

        if (tab.Page.IsClosed)
            throw new InvalidOperationException("Tab đã đóng. Hãy quét lại danh sách tab.");

        page = tab.Page;
        await UseBrowserColorSchemeAsync(page);

        if (bringToFront)
            await page.BringToFrontAsync();
    }

    private static async Task UseBrowserColorSchemeAsync(IPage target)
    {
        try
        {
            ColorScheme scheme = WindowsTheme.AppsUseDarkTheme() ? ColorScheme.Dark : ColorScheme.Light;
            await target.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = scheme });
        }
        catch (PlaywrightException)
        {
        }
    }

    public async Task ClickAsync(
        string selector,
        int waitTimeoutMilliseconds = 3000,
        CancellationToken cancellationToken = default)
    {
        IPage currentPage = RequireSelectedPage();

        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("CSS Selector không được để trống.");

        ILocator element = currentPage.Locator(selector).First;

        int waitMilliseconds = Math.Max(1, waitTimeoutMilliseconds);
        await WaitForVisibleAsync(element, waitMilliseconds, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        await element.ClickAsync(new LocatorClickOptions
        {
            Timeout = Math.Clamp(waitMilliseconds, 1, 5000)
        });
    }

    public async Task FillAsync(
        string selector,
        string text,
        int waitTimeoutMilliseconds = 5000,
        CancellationToken cancellationToken = default)
    {
        IPage currentPage = RequireSelectedPage();

        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("CSS Selector không được để trống.");

        ILocator element = currentPage.Locator(selector).First;
        int waitMilliseconds = Math.Max(1, waitTimeoutMilliseconds);

        await WaitForVisibleAsync(element, waitMilliseconds, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        await element.FillAsync(text ?? string.Empty, new LocatorFillOptions
        {
            Timeout = Math.Clamp(waitMilliseconds, 1, 5000)
        });
    }

    public async Task PressKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        await RequireSelectedPage().Keyboard.PressAsync(key);
    }

    private static async Task WaitForVisibleAsync(
        ILocator element,
        int waitMilliseconds,
        CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(waitMilliseconds);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await element.IsVisibleAsync())
                return;

            if (DateTime.UtcNow >= deadline)
                throw new SelectorWaitTimeoutException(waitMilliseconds);

            await Task.Delay(100, cancellationToken);
        }
    }

    public async Task<(string Keyword, string Snippet)?> FindAdOnScreenAsync(IReadOnlyList<string> keywords)
    {
        string[] found = await EvaluateOnScreenTextAsync(keywords, list: false);
        return found.Length >= 2 ? (found[0], found[1]) : null;
    }

    public async Task<List<string>> GetVisibleTextAsync(IReadOnlyList<string> keywords)
    {
        string[] found = await EvaluateOnScreenTextAsync(keywords, list: true);
        return found.ToList();
    }

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
            }
        }

        await currentPage.Keyboard.PressAsync("ArrowDown");
        return "phím ↓ (không thấy nút Selector)";
    }

    public async Task<bool> BringBrowserToForegroundAsync()
    {
        IPage currentPage = RequireSelectedPage();

        await currentPage.BringToFrontAsync();

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
        }

        if (!string.IsNullOrWhiteSpace(currentPage.Url))
            candidates.Add(currentPage.Url);

        return WindowActivator.ActivateChromiumWindow(candidates);
    }

    public async Task<string?> GetElementSignatureAsync(string selector)
    {
        IPage currentPage = RequireSelectedPage();

        if (string.IsNullOrWhiteSpace(selector))
            return null;

        try
        {
            return await currentPage.EvaluateAsync<string?>(
                """
                (sel) => {
                    const el = document.querySelector(sel);
                    if (!el) return null;
                    const rect = el.getBoundingClientRect();
                    const text = String(el.innerText || el.value || "").trim().slice(0, 60);
                    const className = typeof el.className === "string" ? el.className : "";
                    const disabled = el.disabled === true || el.getAttribute("aria-disabled") === "true";
                    return [
                        el.tagName, el.id || "", className, disabled ? "1" : "0",
                        Math.round(rect.width) + "x" + Math.round(rect.height), text
                    ].join("|");
                }
                """,
                selector);
        }
        catch (PlaywrightException)
        {
            return null;
        }
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

    public async Task<string?> PickElementAsync()
    {
        IPage currentPage = RequireSelectedPage();
        cancelPickRequested = false;

        try
        {
            await currentPage.BringToFrontAsync();
            if (cancelPickRequested)
                return null;

            isPickingElement = true;
            return await currentPage.EvaluateAsync<string?>(PickElementScript);
        }
        finally
        {
            isPickingElement = false;
        }
    }

    public async Task CancelPickElementAsync()
    {
        cancelPickRequested = true;
        IPage? currentPage = page;
        if (!isPickingElement || currentPage == null || currentPage.IsClosed)
            return;

        try
        {
            await currentPage.EvaluateAsync("() => window.__autoClickSelectorCancelPick?.()");
        }
        catch (PlaywrightException)
        {
        }
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

    public async Task<bool> OpenTabAsync(string url, CancellationToken cancellationToken = default)
    {
        if (browser?.IsConnected != true)
            throw new InvalidOperationException("Chưa kết nối trình duyệt.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return false;

        cancellationToken.ThrowIfCancellationRequested();

        IBrowserContext context = browser.Contexts.FirstOrDefault()
            ?? throw new InvalidOperationException("Không tìm thấy Browser Context.");

        IPage newPage = await context.NewPageAsync();
        await newPage.GotoAsync(uri.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        page = newPage;
        return true;
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
                delete window.__autoClickSelectorCancelPick;
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

            window.__autoClickSelectorCancelPick = () => finish(null);
            document.addEventListener('click', onClick, true);
            document.addEventListener('keydown', onKey, true);
            document.addEventListener('mousemove', onMove, true);
            blockedEvents.forEach(name =>
                document.addEventListener(name, onBlocked, true));
        })
        """;
}
