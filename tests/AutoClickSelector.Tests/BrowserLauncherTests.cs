using AutoClickSelector.Core;
using Xunit;

namespace AutoClickSelector.Tests;

public sealed class BrowserLauncherTests
{
    [Theory]
    [InlineData("Chrome/132.0.0.0", "Mozilla/5.0 Chrome/132.0.0.0 Safari/537.36", "chrome", true)]
    [InlineData("Chrome/132.0.0.0", "Mozilla/5.0 Chrome/132.0.0.0 Safari/537.36 Edg/132.0.0.0", "edge", true)]
    [InlineData("CocCoc/132.0.0.0", "Mozilla/5.0 Chrome/132.0.0.0 CocCoc/132.0.0.0", "coccoc", true)]
    [InlineData("Chrome/132.0.0.0", "Mozilla/5.0 Chrome/132.0.0.0 Safari/537.36 Edg/132.0.0.0", "chrome", false)]
    [InlineData("CocCoc/132.0.0.0", "Mozilla/5.0 Chrome/132.0.0.0 CocCoc/132.0.0.0", "chrome", false)]
    [InlineData("Chrome/132.0.0.0", "Mozilla/5.0 Chrome/132.0.0.0 Safari/537.36", "edge", false)]
    [InlineData("Chrome/132.0.0.0", "Mozilla/5.0 Chrome/132.0.0.0 Safari/537.36", "unknown", false)]
    [InlineData("AnyBrowser/1.0", "Any user agent", "custom", true)]
    public void MatchesBrowserFamily_UsesBrowserAndUserAgentIdentity(
        string browser,
        string userAgent,
        string family,
        bool expected)
    {
        var endpoint = new BrowserLauncher.DebugEndpointInfo(browser, userAgent);

        Assert.Equal(expected, BrowserLauncher.MatchesBrowserFamily(endpoint, family));
    }
}
