using Microsoft.Playwright;

namespace AutoClickSelector.Models;

/// <summary>Một tab đang mở trong trình duyệt được điều khiển.</summary>
public sealed class BrowserTab
{
    internal BrowserTab(IPage page, string title, string url)
    {
        Page = page;
        Title = title;
        Url = url;
    }

    internal IPage Page { get; }

    public string Title { get; }

    public string Url { get; }

    public override string ToString() => $"{Title} | {Url}";
}
