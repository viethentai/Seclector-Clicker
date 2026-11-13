namespace AutoClickSelector.Models;

public sealed class AppSettings
{
    public int Version { get; set; } = 1;

    public bool DarkMode { get; set; }

    public string Language { get; set; } = "vi";

    public string Keyword { get; set; } = "Sponsored";

    public decimal IntervalValue { get; set; } = 1m;

    public string IntervalUnit { get; set; } = "second";

    public decimal WaitTimeoutSeconds { get; set; } = 10m;

    public string Selector { get; set; } = string.Empty;

    public string SelectorName { get; set; } = string.Empty;

    public string LastTabUrl { get; set; } = string.Empty;

    public bool MinimizeToTray { get; set; } = true;

    public bool CloseToTray { get; set; }

    public bool ShowNotifications { get; set; } = true;

    public bool PlaySound { get; set; } = true;

    public bool GlobalHotkeys { get; set; } = true;

    public bool AvoidDuplicateClicks { get; set; } = true;

    public string BrowserFamily { get; set; } = "coccoc";
}
