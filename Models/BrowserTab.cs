using Microsoft.Playwright;

namespace AutoClickSelector.Models;

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
