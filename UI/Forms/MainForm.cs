using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoClickSelector.Core;
using AutoClickSelector.Models;
using AutoClickSelector.Services;
using AutoClickSelector.UI.Controls;
using AutoClickSelector.UI.Themes;
using static AutoClickSelector.Services.TextFormatter;

namespace AutoClickSelector.UI.Forms;

public sealed partial class MainForm : Form
{
    private const string AppTitle = "Auto Click Selector";
    private const string OpenBrowserIdleText = "Mở trình duyệt";
    private const string PickElementIdleText = "Chọn phần tử";
    private const int ThemeTransitionMilliseconds = 220;
    private const int DwmUseImmersiveDarkMode = 20;

    private readonly BrowserController browser = new();
    private readonly SemaphoreSlim clickGate = new(1, 1);
    private readonly Dictionary<string, string> savedSelectors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stopwatch runStopwatch = new();
    private readonly System.Windows.Forms.Timer statsTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer themeTimer = new() { Interval = 15 };
    private readonly AutoClickService autoClickService;
    private readonly KeywordWatcherService keywordWatcher;
    private readonly HashSet<Task> activeBrowserOperations = [];
    private readonly System.Windows.Forms.Timer settingsSaveTimer = new() { Interval = 700 };
    private readonly List<(Control Control, string Text)> localizedToolTips = [];
    private readonly List<(RoundedTextBox Box, string Text)> localizedPlaceholders = [];

    private NotifyIcon? trayIcon;
    private bool exitRequested;
    private bool hotkeysRegistered;
    private bool isLoadingSettings;
    private string lastTabUrl = string.Empty;
    private KeywordState keywordState = KeywordState.Off;

    private CancellationTokenSource? autoClickCancellation;
    private CancellationTokenSource? keywordCancellation;
    private TaskCompletionSource<bool>? startValidationCompletion;
    private Task? autoClickTask;
    private Task? keywordWatcherTask;
    private Task? manualClickTask;
    private CancellationTokenSource? manualClickCancellation;
    private bool shutdownInProgress;
    private bool shutdownComplete;
    private string keywordSelector = string.Empty;

    private Palette? themeFrom;
    private Palette? themeTo;
    private bool themeTargetDark;
    private long themeStartedAt;

    private bool isStarting;
    private bool isPickingElement;
    private bool isOpeningBrowser;
    private bool isScanningTabs;
    private bool isSelectingTab;
    private bool isRefreshingTabs;
    private bool isLoadingSelectors;
    private bool isSyncingKeywordToggle;
    private int totalClicks;
    private int successfulClicks;
    private int failedClicks;

    public MainForm()
    {
        AppSettings settings = SettingsStore.Current;
        UiTheme.SetDark(settings.DarkMode);
        Localization.SetLanguage(settings.Language);
        CreateInterface();
        ApplySettingsToControls(settings);
        lastTabUrl = settings.LastTabUrl;
        CreateTrayIcon();

        Shown += (_, _) => ActiveControl = null;

        settingsSaveTimer.Tick += SettingsSaveTimer_Tick;

        autoClickService = new AutoClickService(
            browser,
            clickGate,
            Log,
            RecordClick,
            TryRecoverBrowserAsync);
        keywordWatcher = new KeywordWatcherService(
            browser, clickGate, Log, RecordClick, SetKeywordState, TryRecoverBrowserAsync);

        statsTimer.Tick += (_, _) => UpdateStatistics();
        themeTimer.Tick += ThemeTimer_Tick;

        LoadSavedSelectors();
        SetKeywordState(KeywordState.Off);
        UpdateConnectionStatus();
        UpdateAutoState();
        UpdateStatistics();
        UpdateControlStates();

        Log(ActivityLevel.Info,
            Localization.T("Sẵn sàng. Bấm \"Mở trình duyệt\" để mở trình duyệt, hoặc \"Quét tab\" nếu trình duyệt đã mở sẵn với cổng kết nối."));
    }

    private bool IsAutoClickRunning => autoClickCancellation != null;

    private bool IsKeywordWatcherRunning => keywordCancellation != null;

    private bool CanClickOnceNow =>
        !IsAutoClickRunning &&
        manualClickTask == null &&
        !isStarting &&
        !isPickingElement &&
        !isOpeningBrowser &&
        !isScanningTabs &&
        !isSelectingTab;

    private bool IsBusy =>
        IsAutoClickRunning ||
        IsKeywordWatcherRunning ||
        activeBrowserOperations.Count > 0 ||
        manualClickTask != null ||
        isStarting ||
        isPickingElement ||
        isOpeningBrowser ||
        isScanningTabs ||
        isSelectingTab;

    private void UpdateControlStates()
    {
        bool idle = !IsBusy;
        bool hasSelector = !string.IsNullOrWhiteSpace(txtSelector.Text);
        bool canStartNew = !isStarting && !isPickingElement && !isOpeningBrowser && !isScanningTabs && !isSelectingTab;

        cmbTabs.Enabled = idle;
        btnRefreshTabs.Enabled = idle;
        btnOpenBrowser.Enabled = idle;

        cmbSavedSelectors.Enabled = idle;
        txtSelectorName.Enabled = idle;
        btnSaveSelector.Enabled = idle;
        btnDeleteSelector.Enabled = idle && cmbSavedSelectors.SelectedIndex >= 0;
        txtSelector.Enabled = idle;
        btnPickElement.Enabled = idle;
        btnValidateSelector.Enabled = idle;
        btnShowVisible.Enabled = idle;
        btnRefreshPage.Enabled = idle;
        btnClick.Enabled = hasSelector && CanClickOnceNow;

        numInterval.Enabled = idle && hasSelector;
        cmbIntervalUnit.Enabled = idle && hasSelector;
        numWaitTimeout.Enabled = idle && hasSelector;
        txtKeyword.Enabled = idle;

        btnStart.Enabled = !IsAutoClickRunning && canStartNew && hasSelector;
        btnStop.Enabled = IsAutoClickRunning;
        toggleKeyword.Enabled = IsKeywordWatcherRunning || canStartNew;

        cmbBrowser.Enabled = idle;
        btnBrowseBrowser.Enabled = idle;
    }

    private void UpdateAutoState()
    {
        (string text, Color color) = isStarting
            ? (Localization.T("Đang khởi động..."), UiTheme.Warning)
            : IsAutoClickRunning
                ? (Localization.T("Đang chạy"), UiTheme.Success)
                : (Localization.T("Đã dừng"), UiTheme.TextMuted);

        lblAutoState.Text = Localization.T("Chạy thao tác: ") + text;
        lblAutoState.ForeColor = color;
    }

    private void SetKeywordState(KeywordState state)
    {
        keywordState = state;

        (string text, Color color) = state switch
        {
            KeywordState.Waiting => (Localization.T("Đang chờ từ khóa"), UiTheme.Accent),
            KeywordState.Processing => (Localization.T("Đang xử lý"), UiTheme.Process),
            KeywordState.Stopping => (Localization.T("Đang tắt..."), UiTheme.Warning),
            _ => (Localization.T("Đã tắt"), UiTheme.TextMuted)
        };

        lblKeywordState.Text = text;
        lblKeywordState.ForeColor = color;
    }

    private void SetKeywordToggle(bool value)
    {
        isSyncingKeywordToggle = true;
        try
        {
            toggleKeyword.Checked = value;
        }
        finally
        {
            isSyncingKeywordToggle = false;
        }
    }

    private void UpdateConnectionStatus()
    {
        if (browser.IsConnected && cmbTabs.SelectedItem is BrowserTab tab)
            SetConnection(Localization.T("● Sẵn sàng"), UiTheme.Success, Localization.F("Đã chọn: {0} | {1}", tab.Title, ShortUrl(tab.Url)));
        else if (browser.IsBrowserConnected)
            SetConnection(Localization.T("● Đã kết nối"), UiTheme.Warning, Localization.T("Hãy quét tab và chọn một tab."));
        else
            SetConnection(Localization.T("● Chưa kết nối"), UiTheme.Danger, Localization.T("Mở trình duyệt hoặc quét tab để kết nối."));
    }

    private void SetConnection(string pillText, Color color, string detail)
    {
        lblConnection.Text = pillText;
        lblConnection.ForeColor = color;
        lblStatus.Text = detail;
        lblStatus.ForeColor = color;
    }

    private void UpdateStatistics()
    {
        string elapsed = runStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
        lblStats.Text = string.Format(
            Localization.T("Đã click: {0}  |  Thành công: {1}  |  Lỗi: {2}  |  Thời gian: {3}"),
            totalClicks,
            successfulClicks,
            failedClicks,
            elapsed);
    }

    private void ResetStatistics()
    {
        totalClicks = 0;
        successfulClicks = 0;
        failedClicks = 0;
        runStopwatch.Restart();
    }

    private void RecordClick(bool succeeded)
    {
        totalClicks++;
        if (succeeded)
            successfulClicks++;
        else
            failedClicks++;

        UpdateStatistics();
    }

    private void Log(ActivityLevel level, string message) => logView.AddEntry(level, message);

    private AppSettings Settings => SettingsStore.Current;

    private void ApplySettingsToControls(AppSettings settings)
    {
        isLoadingSettings = true;
        try
        {
            txtKeyword.Text = settings.Keyword;
            txtSelector.Text = settings.Selector;
            txtSelectorName.Text = settings.SelectorName;

            numInterval.Value = Math.Clamp(settings.IntervalValue, numInterval.Minimum, numInterval.Maximum);
            cmbIntervalUnit.SelectedIndex = IsMinuteUnit(settings.IntervalUnit) ? 1 : 0;
            numWaitTimeout.Value = Math.Clamp(settings.WaitTimeoutSeconds, numWaitTimeout.Minimum, numWaitTimeout.Maximum);

            cmbLanguage.SelectedIndex = Localization.IsEnglish ? 1 : 0;

            toggleMinimizeToTray.Checked = settings.MinimizeToTray;
            toggleCloseToTray.Checked = settings.CloseToTray;
            toggleNotifications.Checked = settings.ShowNotifications;
            toggleGlobalHotkeys.Checked = settings.GlobalHotkeys;
            toggleAvoidDuplicate.Checked = settings.AvoidDuplicateClicks;
            cmbBrowser.SelectedIndex = BrowserFamilyIndex(settings.BrowserFamily);
        }
        finally
        {
            isLoadingSettings = false;
        }
    }

    private static bool IsMinuteUnit(string unit) =>
        string.Equals(unit, "minute", StringComparison.OrdinalIgnoreCase);

    private void SaveSettings(Action<AppSettings> change)
    {
        if (isLoadingSettings)
            return;

        SettingsStore.Update(change);
    }

    private void QueueSettingsSave(Action<AppSettings> change)
    {
        if (isLoadingSettings)
            return;

        change(SettingsStore.Current);
        settingsSaveTimer.Stop();
        settingsSaveTimer.Start();
    }

    private void SettingsSaveTimer_Tick(object? sender, EventArgs e)
    {
        settingsSaveTimer.Stop();
        SettingsStore.Save();
    }

    private void CmbLanguage_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (isLoadingSettings)
            return;

        string language = cmbLanguage.SelectedIndex == 1 ? Localization.English : Localization.Vietnamese;
        if (language == Localization.Language)
            return;

        Localization.SetLanguage(language);
        SettingsStore.Current.Language = language;
        settingsSaveTimer.Stop();
        SettingsStore.Save();
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Localization.Apply(this);

        foreach ((Control control, string text) in localizedToolTips)
            toolTip.SetToolTip(control, Localization.T(text));

        foreach ((RoundedTextBox box, string text) in localizedPlaceholders)
            box.PlaceholderText = Localization.T(text);

        UpdateConnectionStatus();
        UpdateAutoState();
        UpdateStatistics();
        SetKeywordState(keywordState);
        RefreshLogFilterItems();
        RefreshBrowserItems();
        RefreshIntervalUnitItems();
        UpdateDarkModeLabel(UiTheme.IsDark);
        UpdateControlStates();
    }

    private async Task<bool> TryRecoverBrowserAsync(CancellationToken token)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            if (token.IsCancellationRequested)
                return false;

            try
            {
                Log(ActivityLevel.Process, Localization.F("Mất kết nối trình duyệt. Đang thử kết nối lại ({0}/3)...", attempt));
                await Task.Delay(TimeSpan.FromSeconds(attempt), token);

                if (attempt == 2)
                    await RelaunchBrowserIfClosedAsync();

                await browser.ConnectAsync();

                if (!await SelectRecoveredTabAsync(token))
                {
                    Log(ActivityLevel.Warning, Localization.T("Chưa chọn được tab sau khi kết nối lại."));
                    continue;
                }

                await RefreshTabsAsync();
                UpdateConnectionStatus();
                SaveSettings(settings => settings.LastTabUrl = lastTabUrl);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Log(ActivityLevel.Warning, Localization.F("Kết nối lại thất bại: {0}", DescribeError(exception)));
            }
        }

        return false;
    }

    private async Task RelaunchBrowserIfClosedAsync()
    {
        string? executable = BrowserLauncher.FindExecutable(Settings.BrowserFamily);

        if (executable is null || BrowserLauncher.IsBrowserRunning(executable))
            return;

        try
        {
            Log(ActivityLevel.Process, Localization.T("Trình duyệt đã đóng. Đang mở lại..."));
            BrowserLauncher.Start(executable);

            bool ready = await BrowserLauncher.WaitForDebugPortAsync(
                BrowserLauncher.DebugPort, TimeSpan.FromSeconds(15));

            if (!ready)
                Log(ActivityLevel.Warning, Localization.T("Cổng kết nối chưa sẵn sàng sau 15 giây."));
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không mở lại được trình duyệt: {0}", DescribeError(exception)));
        }
    }

    private async Task<bool> SelectRecoveredTabAsync(CancellationToken token)
    {
        List<BrowserTab> tabs = await browser.GetTabsAsync();
        BrowserTab? target = FindLastTab(tabs);

        if (target is null && lastTabUrl.Length > 0)
        {
            Log(ActivityLevel.Process, Localization.F("Không thấy tab cũ. Đang mở lại: {0}", ShortUrl(lastTabUrl)));

            if (await browser.OpenTabAsync(lastTabUrl, token))
            {
                tabs = await browser.GetTabsAsync();
                target = FindLastTab(tabs);
            }
        }

        if (target is null)
            return false;

        await browser.SelectTabAsync(target, bringToFront: false);
        lastTabUrl = target.Url;
        Log(ActivityLevel.Success, Localization.F("Đã kết nối lại và chọn tab: {0}", ShortUrl(target.Url)));
        return true;
    }

    private BrowserTab? FindLastTab(List<BrowserTab> tabs)
    {
        if (lastTabUrl.Length == 0)
            return tabs.FirstOrDefault();

        return tabs.FirstOrDefault(tab =>
                   string.Equals(tab.Url, lastTabUrl, StringComparison.OrdinalIgnoreCase))
               ?? tabs.FirstOrDefault(tab =>
                   tab.Url.Length > 0 &&
                   tab.Url.StartsWith(lastTabUrl, StringComparison.OrdinalIgnoreCase));
    }

    private void CreateTrayIcon()
    {
        var menu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem(Localization.T("Mở cửa sổ"));
        openItem.Click += (_, _) => RestoreFromTray();

        var startItem = new ToolStripMenuItem(Localization.T("Bắt đầu (F6)"));
        startItem.Click += (_, _) =>
        {
            if (btnStart.Enabled)
                btnStart.PerformClick();
        };

        var stopItem = new ToolStripMenuItem(Localization.T("Dừng (F7)"));
        stopItem.Click += (_, _) =>
        {
            if (btnStop.Enabled)
                btnStop.PerformClick();
        };

        var exitItem = new ToolStripMenuItem(Localization.T("Thoát"));
        exitItem.Click += (_, _) =>
        {
            exitRequested = true;
            Close();
        };

        menu.Items.Add(openItem);
        menu.Items.Add(startItem);
        menu.Items.Add(stopItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        trayIcon = new NotifyIcon
        {
            Icon = Icon ?? SystemIcons.Application,
            Text = "Auto Click Selector",
            Visible = true,
            ContextMenuStrip = menu
        };
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        NotificationService.Attach(trayIcon);
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void RegisterHotkeys()
    {
        if (hotkeysRegistered || !Settings.GlobalHotkeys || !IsHandleCreated)
            return;

        hotkeysRegistered = GlobalHotkey.RegisterAll(Handle);
        if (!hotkeysRegistered)
            Log(ActivityLevel.Warning, Localization.T(
                "Không đăng ký được phím tắt toàn cục (có thể ứng dụng khác đang dùng F6/F7/F8)."));
    }

    private void ApplyHotkeySetting(bool enabled)
    {
        if (enabled)
        {
            RegisterHotkeys();
            return;
        }

        if (!hotkeysRegistered)
            return;

        GlobalHotkey.UnregisterAll(Handle);
        hotkeysRegistered = false;
    }

    private void ToggleMinimizeToTray_CheckedChanged(object? sender, EventArgs e) =>
        SaveSettings(settings => settings.MinimizeToTray = toggleMinimizeToTray.Checked);

    private void ToggleCloseToTray_CheckedChanged(object? sender, EventArgs e) =>
        SaveSettings(settings => settings.CloseToTray = toggleCloseToTray.Checked);

    private void ToggleNotifications_CheckedChanged(object? sender, EventArgs e) =>
        SaveSettings(settings => settings.ShowNotifications = toggleNotifications.Checked);

    private void ToggleGlobalHotkeys_CheckedChanged(object? sender, EventArgs e)
    {
        SaveSettings(settings => settings.GlobalHotkeys = toggleGlobalHotkeys.Checked);
        ApplyHotkeySetting(toggleGlobalHotkeys.Checked);
    }

    private void ToggleAvoidDuplicate_CheckedChanged(object? sender, EventArgs e) =>
        SaveSettings(settings => settings.AvoidDuplicateClicks = toggleAvoidDuplicate.Checked);

    private static string BrowserFamilyCode(int index) => index switch
    {
        1 => "edge",
        2 => "chrome",
        3 => "custom",
        _ => "coccoc"
    };

    private static int BrowserFamilyIndex(string family) => family switch
    {
        "edge" => 1,
        "chrome" => 2,
        "custom" => 3,
        _ => 0
    };

    private void CmbBrowser_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (isLoadingSettings || logView is null)
            return;

        if (cmbBrowser.SelectedIndex == 3)
        {
            PickCustomBrowser();
            return;
        }

        SaveSettings(settings => settings.BrowserFamily = BrowserFamilyCode(cmbBrowser.SelectedIndex));
        Log(ActivityLevel.Info, Localization.F("Trình duyệt sẽ mở: {0}", cmbBrowser.SelectedItem?.ToString() ?? string.Empty));
    }

    private void BtnBrowseBrowser_Click(object? sender, EventArgs e) => PickCustomBrowser();

    private void PickCustomBrowser()
    {
        using var dialog = new OpenFileDialog
        {
            Title = Localization.T("Chọn trình duyệt"),
            Filter = Localization.T("Chương trình (*.exe)|*.exe"),
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            isLoadingSettings = true;
            try
            {
                cmbBrowser.SelectedIndex = BrowserFamilyIndex(Settings.BrowserFamily);
            }
            finally
            {
                isLoadingSettings = false;
            }

            return;
        }

        try
        {
            BrowserLauncher.SaveExecutable(dialog.FileName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log(ActivityLevel.Warning, Localization.F("Không lưu được đường dẫn trình duyệt: {0}", DescribeError(exception)));
        }

        SettingsStore.Current.BrowserFamily = "custom";
        SettingsStore.Save();

        isLoadingSettings = true;
        try
        {
            cmbBrowser.SelectedIndex = 3;
        }
        finally
        {
            isLoadingSettings = false;
        }

        Log(ActivityLevel.Info, Localization.F("Đã chọn trình duyệt: {0}", dialog.FileName));
    }

    private void CmbLogFilter_SelectedIndexChanged(object? sender, EventArgs e)
    {
        logView?.SetLevelFilter(LevelForFilterIndex(cmbLogFilter.SelectedIndex));
    }

    private static ActivityLevel? LevelForFilterIndex(int index) => index switch
    {
        1 => ActivityLevel.Error,
        2 => ActivityLevel.Warning,
        3 => ActivityLevel.Success,
        4 => ActivityLevel.Info,
        5 => ActivityLevel.Process,
        _ => null
    };

    private void BtnLogExport_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = Localization.T("Xuất nhật ký"),
            Filter = Localization.T("Văn bản") + " (*.txt)|*.txt",
            FileName = $"nhat-ky-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            logView.ExportTo(dialog.FileName);
            Log(ActivityLevel.Success, Localization.F("Đã xuất nhật ký: {0}", dialog.FileName));
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không xuất được nhật ký: {0}", DescribeError(exception)));
        }
    }

    private void BtnLogClear_Click(object? sender, EventArgs e) => logView.ClearEntries();

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == GlobalHotkey.WmHotkey)
        {
            switch (message.WParam.ToInt32())
            {
                case GlobalHotkey.StartId when btnStart.Enabled:
                    btnStart.PerformClick();
                    return;
                case GlobalHotkey.StopId when btnStop.Enabled:
                    btnStop.PerformClick();
                    return;
                case GlobalHotkey.ClickId when btnClick.Enabled:
                    btnClick.PerformClick();
                    return;
            }
        }

        base.WndProc(ref message);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState != FormWindowState.Minimized || trayIcon == null || !Settings.MinimizeToTray)
            return;

        Hide();
        NotificationService.Notify(Localization.T("Ứng dụng vẫn chạy dưới khay hệ thống."));
    }

    private void StartTrackedBrowserOperation(Func<Task> operation)
    {
        if (shutdownInProgress || IsBusy)
            return;

        Task task = operation();
        activeBrowserOperations.Add(task);
        UpdateControlStates();
        _ = ObserveBrowserOperationAsync(task);
    }

    private async Task ObserveBrowserOperationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception exception)
        {
            if (!shutdownInProgress)
                Log(ActivityLevel.Error, Localization.F("Thao tác trình duyệt gặp lỗi: {0}", DescribeError(exception)));
        }
        finally
        {
            activeBrowserOperations.Remove(task);
            if (!shutdownInProgress)
                UpdateControlStates();
        }
    }

    private void BtnRefreshTabs_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(BtnRefreshTabsAsync);

    private async Task BtnRefreshTabsAsync()
    {
        isScanningTabs = true;
        UpdateControlStates();
        try
        {
            Log(ActivityLevel.Process, Localization.T("Đang kết nối trình duyệt để quét tab..."));
            await browser.ConnectAsync();
            await RefreshTabsAsync();
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, DescribeBrowserError(exception));
        }
        finally
        {
            isScanningTabs = false;
            UpdateControlStates();
            UpdateConnectionStatus();
        }
    }

    private void BtnOpenBrowser_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(BtnOpenBrowserAsync);

    private async Task BtnOpenBrowserAsync()
    {
        if (isOpeningBrowser)
            return;

        isOpeningBrowser = true;
        btnOpenBrowser.Text = Localization.T("Đang mở...");
        UpdateControlStates();
        try
        {
            await OpenBrowserAsync();
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không mở được trình duyệt: {0}", DescribeError(exception)));
        }
        finally
        {
            isOpeningBrowser = false;
            btnOpenBrowser.Text = Localization.T(OpenBrowserIdleText);
            UpdateControlStates();
            UpdateConnectionStatus();
        }
    }

    private async Task OpenBrowserAsync()
    {
        string family = Settings.BrowserFamily;

        if (family.Length == 0)
        {
            Log(ActivityLevel.Warning, Localization.T("Hãy chọn trình duyệt cần mở trong ô 'Chọn trình duyệt'."));
            return;
        }

        if (await BrowserLauncher.IsDebugPortOpenAsync())
        {
            Log(ActivityLevel.Success, Localization.T("Trình duyệt đã mở sẵn với cổng kết nối. Hãy bấm 'Quét tab' để chọn tab."));
            return;
        }

        string? executable = BrowserLauncher.FindExecutable(family);

        if (executable == null && string.Equals(family, "custom", StringComparison.OrdinalIgnoreCase))
        {
            executable = PromptForBrowserExecutable();

            if (executable == null)
            {
                Log(ActivityLevel.Warning, Localization.T("Chưa chọn đường dẫn trình duyệt nên đã hủy."));
                return;
            }

            BrowserLauncher.SaveExecutable(executable);
        }

        if (executable == null)
        {
            Log(ActivityLevel.Warning,
                Localization.T("Không tìm thấy trình duyệt đã chọn. Hãy bấm nút '...' để chọn đường dẫn."));
            return;
        }

        if (BrowserLauncher.IsBrowserRunning(executable))
        {
            Log(ActivityLevel.Warning, Localization.T("Trình duyệt đang chạy nhưng chưa mở cổng kết nối nên không thể điều khiển."));
            DialogResult answer = MessageBox.Show(
                Localization.T("Trình duyệt đang mở mà không có cổng kết nối.\n\nĐóng trình duyệt để ứng dụng mở lại với cấu hình kết nối? Các tab chưa lưu có thể bị mất."),
                AppTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (answer != DialogResult.Yes)
            {
                Log(ActivityLevel.Info, Localization.T("Đã hủy. Hãy tự đóng trình duyệt rồi bấm 'Mở trình duyệt' lại."));
                return;
            }

            Log(ActivityLevel.Process, Localization.T("Đang đóng trình duyệt..."));

            if (!await BrowserLauncher.CloseBrowserAsync(executable, TimeSpan.FromSeconds(15)))
            {
                Log(ActivityLevel.Error,
                    Localization.T("Trình duyệt vẫn đang chạy (có thể chạy nền). Hãy thoát hẳn rồi bấm 'Mở trình duyệt' lại."));
                return;
            }

            Log(ActivityLevel.Info, Localization.T("Đã đóng trình duyệt."));
        }

        Log(ActivityLevel.Process, Localization.T("Đang khởi chạy trình duyệt..."));

        try
        {
            BrowserLauncher.Start(executable, BrowserLauncher.DebugPort);
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error,
                Localization.F("Không khởi chạy được trình duyệt. Hãy kiểm tra đường dẫn: {0}", DescribeError(exception)));
            return;
        }

        bool ready = await BrowserLauncher.WaitForDebugPortAsync(
            BrowserLauncher.DebugPort, TimeSpan.FromSeconds(15));

        if (ready)
        {
            Log(ActivityLevel.Success, Localization.T("Trình duyệt đã mở thành công. Hãy bấm 'Quét tab' để chọn tab."));
        }
        else
        {
            Log(ActivityLevel.Error, Localization.T(
                "Đã gọi lệnh mở nhưng cổng kết nối chưa sẵn sàng. Có thể trình duyệt còn chạy nền từ phiên trước, hoặc phiên bản trình duyệt không cho phép cổng gỡ lỗi với hồ sơ mặc định."));
        }
    }

    private string? PromptForBrowserExecutable()
    {
        using var dialog = new OpenFileDialog
        {
            Title = Localization.T("Chọn trình duyệt"),
            Filter = Localization.T("Chương trình (*.exe)|*.exe"),
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    private async Task RefreshTabsAsync()
    {
        List<BrowserTab> tabs = await browser.GetTabsAsync();

        int selectedIndex = tabs.FindIndex(browser.IsCurrentPage);
        if (selectedIndex < 0 && cmbTabs.SelectedItem is BrowserTab previousTab)
            selectedIndex = tabs.FindIndex(tab => ReferenceEquals(tab.Page, previousTab.Page));

        isRefreshingTabs = true;
        try
        {
            cmbTabs.Items.Clear();
            cmbTabs.Items.AddRange(tabs.Cast<object>().ToArray());
            cmbTabs.SelectedIndex = selectedIndex;
        }
        finally
        {
            isRefreshingTabs = false;
        }

        if (selectedIndex >= 0)
        {
            await browser.SelectTabAsync(tabs[selectedIndex], bringToFront: false);
            lastTabUrl = tabs[selectedIndex].Url;
            SaveSettings(settings => settings.LastTabUrl = lastTabUrl);
            Log(ActivityLevel.Success, Localization.F("Tìm thấy {0} tab; đang chọn: {1}", tabs.Count, ShortUrl(tabs[selectedIndex].Url)));
        }
        else if (tabs.Count > 0)
        {
            Log(ActivityLevel.Info, Localization.F("Tìm thấy {0} tab. Hãy chọn tab cần dùng trong danh sách.", tabs.Count));
        }
        else
        {
            Log(ActivityLevel.Warning, Localization.T("Không tìm thấy tab đang mở."));
        }

        UpdateConnectionStatus();
    }

    private void CmbTabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (isRefreshingTabs ||
            IsBusy ||
            cmbTabs.SelectedItem is not BrowserTab selectedTab ||
            !browser.IsBrowserConnected)
        {
            return;
        }

        StartTrackedBrowserOperation(() => SelectTabAsync(selectedTab));
    }

    private async Task SelectTabAsync(BrowserTab selectedTab)
    {
        isSelectingTab = true;
        UpdateControlStates();
        try
        {
            await browser.SelectTabAsync(selectedTab);
            lastTabUrl = selectedTab.Url;
            SaveSettings(settings => settings.LastTabUrl = lastTabUrl);
            UpdateConnectionStatus();
            Log(ActivityLevel.Success, Localization.F("Đã chọn tab: {0} | {1}", selectedTab.Title, ShortUrl(selectedTab.Url)));
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không thể chọn tab: {0}", DescribeError(exception)));
            UpdateConnectionStatus();
        }
        finally
        {
            isSelectingTab = false;
            UpdateControlStates();
        }
    }

    private void BtnValidateSelector_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(ValidateSelectorAsync);

    private async Task ValidateSelectorAsync()
    {
        if (string.IsNullOrWhiteSpace(txtSelector.Text))
        {
            ShowSelectorValidation(false, Localization.T("Vui lòng nhập Selector trước."));
            return;
        }

        if (isPickingElement)
        {
            Log(ActivityLevel.Warning, Localization.T("Hãy hoàn tất hoặc hủy thao tác chọn phần tử trước."));
            return;
        }

        if (!browser.IsConnected)
        {
            ShowSelectorValidation(false, Localization.T("Hãy kết nối và chọn tab trước."));
            return;
        }

        btnValidateSelector.Enabled = false;
        try
        {
            SelectorValidationResult result = await browser.ValidateSelectorAsync(txtSelector.Text);
            if (!result.IsValid)
            {
                ShowSelectorValidation(false, Localization.F("✗ Selector không hợp lệ: {0}", result.Error ?? string.Empty));
            }
            else if (result.MatchCount == 0)
            {
                ShowSelectorValidation(true, Localization.T("Selector hợp lệ nhưng chưa tìm thấy phần tử nào."));
            }
            else
            {
                ShowSelectorValidation(true,
                    Localization.F("✓ Selector hợp lệ  |  Tìm thấy: {0}  |  Hiển thị: {1}", result.MatchCount, result.VisibleCount));
            }
        }
        catch (Exception exception)
        {
            ShowSelectorValidation(false, Localization.F("Không thể kiểm tra Selector: {0}", DescribeError(exception)));
            UpdateConnectionStatus();
        }
        finally
        {
            UpdateControlStates();
        }
    }

    private void BtnShowVisible_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(ShowVisibleTextAsync);

    private async Task ShowVisibleTextAsync()
    {
        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, Localization.T("Hãy quét tab và chọn một tab trước."));
            return;
        }

        btnShowVisible.Enabled = false;
        try
        {
            List<string> lines = await browser.GetVisibleTextAsync(ParseKeywords(txtKeyword.Text));

            if (lines.Count == 0)
            {
                Log(ActivityLevel.Info, Localization.T("Không thấy văn bản nào đang hiển thị trên màn hình."));
            }
            else
            {
                Log(ActivityLevel.Info, Localization.F("Văn bản đang hiển thị trên màn hình ({0}; ★ = khớp từ khóa):", lines.Count));
                foreach (string line in lines)
                    Log(ActivityLevel.Info, "    " + line);
            }
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không thể đọc văn bản trên trang: {0}", DescribeError(exception)));
            UpdateConnectionStatus();
        }
        finally
        {
            UpdateControlStates();
        }
    }

    private void ShowSelectorValidation(bool valid, string message)
    {
        string firstLine = message
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? message;

        lblSelectorValidation.Text = firstLine;

        lblSelectorValidation.Visible = true;
        lblSelectorValidation.ForeColor = valid ? UiTheme.Success : UiTheme.Danger;
        lblSelectorValidation.Visible = true;
        Log(valid ? ActivityLevel.Success : ActivityLevel.Error, message);
    }

    private void BtnPickElement_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(PickElementAsync);

    private async Task PickElementAsync()
    {
        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, Localization.T("Hãy kết nối và chọn tab trước khi chọn phần tử."));
            return;
        }

        if (IsBusy)
        {
            Log(ActivityLevel.Warning, Localization.T("Đang có thao tác khác. Hãy dừng các chức năng đang chạy trước khi chọn phần tử."));
            return;
        }

        if (!await clickGate.WaitAsync(0))
        {
            Log(ActivityLevel.Warning, Localization.T("Đang có thao tác khác. Vui lòng thử lại."));
            return;
        }

        isPickingElement = true;
        btnPickElement.Text = Localization.T("Đang chọn...");
        lblSelectorValidation.Text = Localization.T("Đang chờ bạn chọn phần tử trên trình duyệt (Esc để hủy).");
        lblSelectorValidation.Visible = true;
        lblSelectorValidation.ForeColor = UiTheme.Warning;
        UpdateControlStates();
        Log(ActivityLevel.Process, Localization.T(
            "Đang chuyển sang trình duyệt. Hãy bấm vào phần tử cần chọn (Esc để hủy); ứng dụng sẽ tự quay lại sau khi bạn chọn."));

        try
        {
            if (!await browser.BringBrowserToForegroundAsync())
                Log(ActivityLevel.Warning, Localization.T("Không tự chuyển được sang trình duyệt. Hãy tự mở tab đó rồi bấm vào phần tử."));

            if (shutdownInProgress)
                return;

            string? selector = await browser.PickElementAsync();
            if (string.IsNullOrWhiteSpace(selector))
            {
                if (!shutdownInProgress)
                    Log(ActivityLevel.Info, Localization.T("Đã hủy chọn phần tử."));
                return;
            }

            txtSelector.Text = selector;
            lblSelectorValidation.Text = Localization.T("Đã tạo Selector. Hãy bấm Kiểm tra trước khi lưu hoặc chạy.");
            lblSelectorValidation.Visible = true;
            lblSelectorValidation.ForeColor = UiTheme.Warning;
            Log(ActivityLevel.Success, Localization.F("Đã chọn phần tử và tạo Selector: {0}", selector));
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không thể chọn phần tử: {0}", DescribeError(exception)));
            UpdateConnectionStatus();
        }
        finally
        {
            isPickingElement = false;
            btnPickElement.Text = Localization.T(PickElementIdleText);
            clickGate.Release();
            if (!shutdownInProgress)
            {
                UpdateControlStates();
                UpdateConnectionStatus();

                if (!WindowActivator.Activate(Handle))
                    Activate();
            }
        }
    }

    private void CmbSavedSelectors_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (isLoadingSelectors || cmbSavedSelectors.SelectedItem is not string name)
            return;

        txtSelectorName.Text = name;
        txtSelector.Text = savedSelectors[name];
        UpdateControlStates();
    }

    private void BtnSaveSelector_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSelectorName.Text))
        {
            Log(ActivityLevel.Warning, Localization.T("Vui lòng nhập tên cho Selector."));
            return;
        }

        if (string.IsNullOrWhiteSpace(txtSelector.Text))
        {
            Log(ActivityLevel.Warning, Localization.T("Vui lòng nhập CSS Selector."));
            return;
        }

        string name = txtSelectorName.Text.Trim();
        savedSelectors[name] = txtSelector.Text.Trim();
        SaveSelectors();
        RefreshSavedSelectorList(name);
        Log(ActivityLevel.Success, Localization.F("Đã lưu Selector \"{0}\".", name));
    }

    private void BtnDeleteSelector_Click(object? sender, EventArgs e)
    {
        if (cmbSavedSelectors.SelectedItem is not string name)
            return;

        savedSelectors.Remove(name);
        SaveSelectors();
        RefreshSavedSelectorList();
        txtSelectorName.Clear();
        txtSelector.Clear();
        Log(ActivityLevel.Info, Localization.F("Đã xóa Selector \"{0}\".", name));
    }

    private void RefreshSavedSelectorList(string? selectName = null)
    {
        isLoadingSelectors = true;
        try
        {
            cmbSavedSelectors.Items.Clear();
            cmbSavedSelectors.Items.AddRange(savedSelectors.Keys
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Cast<object>()
                .ToArray());
            cmbSavedSelectors.SelectedItem = selectName;
        }
        finally
        {
            isLoadingSelectors = false;
            UpdateControlStates();
        }
    }

    private void LoadSavedSelectors()
    {
        try
        {
            foreach (KeyValuePair<string, string> item in SelectorRepository.Load())
                savedSelectors[item.Key] = item.Value;

            RefreshSavedSelectorList();
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không thể tải danh sách Selector đã lưu: {0}", DescribeError(exception)));
        }
    }

    private void SaveSelectors()
    {
        try
        {
            SelectorRepository.Save(savedSelectors);
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không thể lưu Selector: {0}", DescribeError(exception)));
        }
    }

    private void BtnRefreshPage_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(RefreshPageAsync);

    private async Task RefreshPageAsync()
    {
        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, Localization.T("Vui lòng kết nối và chọn tab trước."));
            return;
        }

        try
        {
            await browser.RefreshPageAsync();
            Log(ActivityLevel.Success, Localization.T("Đã làm mới trang web."));
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không thể làm mới trang web: {0}", DescribeError(exception)));
        }
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (IsAutoClickRunning || isStarting)
        {
            Log(ActivityLevel.Info, Localization.T("Chạy thao tác đang chạy."));
            return;
        }

        if (isPickingElement)
        {
            Log(ActivityLevel.Warning, Localization.T("Hãy hoàn tất hoặc hủy thao tác chọn phần tử trước."));
            return;
        }

        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, Localization.T("Hãy quét tab và chọn một tab trước."));
            return;
        }

        string selector = txtSelector.Text.Trim();
        if (selector.Length == 0)
        {
            Log(ActivityLevel.Warning, Localization.T("Vui lòng nhập CSS Selector trước khi bắt đầu."));
            return;
        }

        if (shutdownInProgress)
            return;

        isStarting = true;
        var validationCompletion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        startValidationCompletion = validationCompletion;
        UpdateControlStates();
        UpdateAutoState();

        SelectorValidationResult validation;
        try
        {
            validation = await browser.ValidateSelectorAsync(selector);
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Không thể bắt đầu: {0}", DescribeError(exception)));
            UpdateConnectionStatus();
            return;
        }
        finally
        {
            isStarting = false;
            if (ReferenceEquals(startValidationCompletion, validationCompletion))
                startValidationCompletion = null;
            validationCompletion.TrySetResult(true);
            UpdateControlStates();
            UpdateAutoState();
        }

        if (shutdownInProgress)
            return;

        if (!validation.IsValid)
        {
            ShowSelectorValidation(false, Localization.T("Selector không hợp lệ. Vui lòng kiểm tra lại."));
            return;
        }

        if (validation.MatchCount == 0)
            Log(ActivityLevel.Warning, Localization.F("Chưa thấy phần tử. Sẽ chờ tối đa {0} giây.", numWaitTimeout.Value));

        if (IsKeywordWatcherRunning && string.Equals(selector, keywordSelector, StringComparison.Ordinal))
        {
            Log(ActivityLevel.Warning,
                Localization.T("Lưu ý: 'Click khi có từ khóa' đang dùng cùng Selector này. Hai chức năng sẽ cùng bấm vào một nút."));
        }

        var cancellation = new CancellationTokenSource();
        autoClickCancellation = cancellation;
        ResetStatistics();
        statsTimer.Start();
        UpdateStatistics();
        UpdateControlStates();
        UpdateAutoState();

        Log(ActivityLevel.Process, Localization.T("Chạy thao tác: bắt đầu bấm Selector theo chu kỳ. Bấm Dừng để kết thúc."));

        Task runTask = autoClickService.RunAsync(
            selector,
            GetIntervalMilliseconds(),
            GetWaitTimeoutMilliseconds(),
            cancellation.Token);
        autoClickTask = runTask;

        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (SelectorWaitTimeoutException)
        {
            NotificationService.Warning(
                Localization.T("Chạy thao tác đã dừng"),
                Localization.T("Không thấy Selector hiển thị trong thời gian chờ."));
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Chạy thao tác đã dừng do lỗi: {0}", DescribeError(exception)));
            NotificationService.Error(
                Localization.T("Đã dừng do lỗi"),
                DescribeError(exception));
        }
        finally
        {
            if (ReferenceEquals(autoClickTask, runTask))
                autoClickTask = null;
            if (ReferenceEquals(autoClickCancellation, cancellation))
                autoClickCancellation = null;
            cancellation.Dispose();

            statsTimer.Stop();
            runStopwatch.Stop();
            UpdateConnectionStatus();
            UpdateStatistics();
            UpdateControlStates();
            UpdateAutoState();

            Log(ActivityLevel.Info, Localization.T("Chạy thao tác đã dừng."));
        }
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        if (!IsAutoClickRunning)
            return;

        Log(ActivityLevel.Info, Localization.T("Đang dừng chạy thao tác..."));
        autoClickCancellation?.Cancel();
    }

    private async void BtnClick_Click(object? sender, EventArgs e)
    {
        if (!CanClickOnceNow)
        {
            Log(ActivityLevel.Warning, Localization.T("Đang có thao tác khác. Vui lòng chờ thao tác hiện tại hoàn tất."));
            return;
        }

        if (string.IsNullOrWhiteSpace(txtSelector.Text))
        {
            Log(ActivityLevel.Warning, Localization.T("Vui lòng nhập Selector trước khi Click."));
            return;
        }

        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, Localization.T("Hãy quét tab và chọn một tab trước."));
            return;
        }

        if (!await clickGate.WaitAsync(0))
        {
            Log(ActivityLevel.Warning, Localization.T("Đang có click khác."));
            return;
        }

        btnClick.Enabled = false;
        var cancellation = new CancellationTokenSource();
        manualClickCancellation = cancellation;
        try
        {
            Log(ActivityLevel.Process, Localization.F("Đang chờ Selector tối đa {0} giây...", numWaitTimeout.Value));
            Task clickTask = browser.ClickAsync(
                txtSelector.Text,
                GetWaitTimeoutMilliseconds(),
                cancellation.Token);
            manualClickTask = clickTask;
            UpdateControlStates();
            await clickTask;

            RecordClick(true);
            Log(ActivityLevel.Success, Localization.T("Click thành công."));
        }
        catch (OperationCanceledException)
        {
            if (!shutdownInProgress)
                Log(ActivityLevel.Info, Localization.T("Đã hủy thao tác Click."));
        }
        catch (SelectorWaitTimeoutException)
        {
            RecordClick(false);
            Log(ActivityLevel.Warning, Localization.T("Không tìm thấy phần tử trong thời gian chờ. Hãy kiểm tra Selector và thử lại."));
        }
        catch (Exception exception)
        {
            RecordClick(false);
            Log(ActivityLevel.Error, Localization.F("Click lỗi: {0}", DescribeError(exception)));
        }
        finally
        {
            manualClickTask = null;
            if (ReferenceEquals(manualClickCancellation, cancellation))
                manualClickCancellation = null;
            cancellation.Dispose();
            clickGate.Release();
            UpdateControlStates();
        }
    }

    private void ToggleKeyword_CheckedChanged(object? sender, EventArgs e)
    {
        if (isSyncingKeywordToggle)
            return;

        if (toggleKeyword.Checked)
            StartKeywordWatcher();
        else
            StopKeywordWatcher();
    }

    private void StartKeywordWatcher()
    {
        if (shutdownInProgress)
        {
            SetKeywordToggle(false);
            return;
        }

        if (IsKeywordWatcherRunning)
        {
            SetKeywordToggle(false);
            Log(ActivityLevel.Warning, Localization.T("Click khi có từ khóa đang tắt. Hãy thử bật lại sau giây lát."));
            return;
        }

        string selector = txtSelector.Text.Trim();
        List<string> keywords = ParseKeywords(txtKeyword.Text);

        string? problem = null;
        if (isStarting || isPickingElement || isOpeningBrowser || isScanningTabs)
            problem = Localization.T("Đang có thao tác khác. Hãy thử lại sau.");
        else if (!browser.IsConnected)
            problem = Localization.T("Hãy mở trình duyệt, quét tab và chọn một tab trước.");
        else if (selector.Length == 0)
            problem = Localization.T("Vui lòng nhập Selector (nút 'Video tiếp theo') trước khi bật chức năng này.");
        else if (keywords.Count == 0)
            problem = Localization.T("Vui lòng nhập ít nhất một từ khóa (cách nhau bằng dấu phẩy).");

        if (problem != null)
        {
            SetKeywordToggle(false);
            Log(ActivityLevel.Warning, problem);
            return;
        }

        var cancellation = new CancellationTokenSource();
        keywordCancellation = cancellation;
        keywordSelector = selector;

        SetKeywordState(KeywordState.Waiting);
        UpdateControlStates();
        Log(ActivityLevel.Process, Localization.F("Click khi có từ khóa: đã bật. Từ khóa: {0}.", string.Join(", ", keywords)));

        keywordWatcherTask = RunKeywordWatcherSafeAsync(selector, keywords, cancellation);
    }

    private void StopKeywordWatcher()
    {
        if (keywordCancellation == null)
        {
            SetKeywordState(KeywordState.Off);
            UpdateControlStates();
            return;
        }

        SetKeywordState(KeywordState.Stopping);
        keywordCancellation.Cancel();
        UpdateControlStates();
    }

    private async Task RunKeywordWatcherSafeAsync(
        string selector,
        IReadOnlyList<string> keywords,
        CancellationTokenSource cancellation)
    {
        try
        {
            await keywordWatcher.RunAsync(selector, keywords, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, Localization.F("Click khi có từ khóa đã dừng do lỗi: {0}", DescribeError(exception)));
            NotificationService.Error(
                Localization.T("Click khi có từ khóa đã dừng"),
                DescribeError(exception));
        }
        finally
        {
            if (ReferenceEquals(keywordCancellation, cancellation))
                keywordCancellation = null;
            cancellation.Dispose();

            SetKeywordState(KeywordState.Off);
            SetKeywordToggle(false);
            UpdateControlStates();
            UpdateConnectionStatus();
            Log(ActivityLevel.Info, Localization.T("Click khi có từ khóa: đã tắt."));
        }
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F6 when btnStart.Enabled:
                btnStart.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.F7 when btnStop.Enabled:
                btnStop.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.F8 when btnClick.Enabled:
                btnClick.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
        }
    }

    private int GetIntervalMilliseconds()
    {
        decimal multiplier = cmbIntervalUnit.SelectedIndex == 1 ? 60_000m : 1_000m;
        return (int)Math.Min(numInterval.Value * multiplier, int.MaxValue);
    }

    private int GetWaitTimeoutMilliseconds() =>
        decimal.ToInt32(numWaitTimeout.Value * 1000m);

    private void ToggleDarkMode_CheckedChanged(object? sender, EventArgs e)
    {
        themeFrom = UiTheme.Current;
        themeTargetDark = toggleDarkMode.Checked;
        themeTo = themeTargetDark ? Palette.Dark : Palette.Light;
        themeStartedAt = Environment.TickCount64;
        themeTimer.Start();

        SaveSettings(settings => settings.DarkMode = themeTargetDark);
        UpdateDarkModeLabel(themeTargetDark);

        Log(ActivityLevel.Info, Localization.T(themeTargetDark
            ? "Đã chuyển sang giao diện tối."
            : "Đã chuyển sang giao diện sáng."));
    }

    private void UpdateDarkModeLabel(bool dark)
    {
        string label = dark ? "Chế độ tối" : "Chế độ sáng";
        toggleDarkMode.Tag = label;
        toggleDarkMode.Text = Localization.T(label);
    }

    private void ThemeTimer_Tick(object? sender, EventArgs e)
    {
        if (themeFrom is null || themeTo is null)
        {
            themeTimer.Stop();
            return;
        }

        float progress = Math.Clamp(
            (Environment.TickCount64 - themeStartedAt) / (float)ThemeTransitionMilliseconds,
            0f,
            1f);
        float eased = 1f - MathF.Pow(1f - progress, 3f);
        bool finished = progress >= 1f;

        Palette previous = UiTheme.Current;
        Palette next = finished ? themeTo : Palette.Lerp(themeFrom, themeTo, eased);
        UiTheme.SetPalette(next, themeTargetDark);
        ApplyThemeTo(this, previous);

        if (finished)
        {
            themeFrom = null;
            themeTo = null;
            themeTimer.Stop();
            ApplyTitleBarTheme();
        }
    }

    private static void ApplyThemeTo(Control control, Palette previous)
    {
        control.BackColor = UiTheme.Remap(control.BackColor, previous);
        control.ForeColor = UiTheme.Remap(control.ForeColor, previous);

        switch (control)
        {
            case ThemedButton button:
                button.RefreshTheme();
                break;
            case ToggleSwitch toggle:
                toggle.RefreshTheme();
                break;
            case LogView view:
                view.RefreshTheme();
                break;
        }

        foreach (Control child in control.Controls)
            ApplyThemeTo(child, previous);

        control.Invalidate(true);
    }

    private void ApplyTitleBarTheme()
    {
        if (!IsHandleCreated)
            return;

        int useDark = UiTheme.IsDark ? 1 : 0;
        try
        {
            DwmSetWindowAttribute(Handle, DwmUseImmersiveDarkMode, ref useDark, sizeof(int));
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTitleBarTheme();
        RegisterHotkeys();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!exitRequested && !shutdownInProgress && Settings.CloseToTray &&
            e.CloseReason == CloseReason.UserClosing && trayIcon != null)
        {
            e.Cancel = true;
            Hide();
            NotificationService.Notify(Localization.T("Ứng dụng vẫn chạy dưới khay hệ thống."));
            return;
        }

        base.OnFormClosing(e);
        if (e.Cancel || shutdownComplete)
            return;

        e.Cancel = true;
        if (shutdownInProgress)
            return;

        shutdownInProgress = true;
        Enabled = false;
        autoClickCancellation?.Cancel();
        keywordCancellation?.Cancel();
        manualClickCancellation?.Cancel();
        statsTimer.Stop();
        themeTimer.Stop();

        _ = CompleteShutdownAsync();
    }

    private async Task CompleteShutdownAsync()
    {
        try
        {
            Task? validationTask = startValidationCompletion?.Task;
            if (validationTask != null)
                await validationTask;

            autoClickCancellation?.Cancel();
            keywordCancellation?.Cancel();
            manualClickCancellation?.Cancel();
            await browser.CancelPickElementAsync();

            Task[] activeTasks = activeBrowserOperations
                .Concat(new[] { autoClickTask, keywordWatcherTask, manualClickTask })
                .Where(task => task != null)
                .Cast<Task>()
                .Distinct()
                .ToArray();

            if (activeTasks.Length > 0)
                await Task.WhenAll(activeTasks);
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Warning, Localization.F("Một tác vụ nền kết thúc khi đóng ứng dụng: {0}", DescribeError(exception)));
        }
        finally
        {
            try
            {
                await browser.StopAsync();
            }
            catch (Exception exception)
            {
                Log(ActivityLevel.Warning, Localization.F("Không thể đóng hoàn toàn kết nối trình duyệt: {0}", DescribeError(exception)));
            }

            if (hotkeysRegistered)
            {
                GlobalHotkey.UnregisterAll(Handle);
                hotkeysRegistered = false;
            }

            settingsSaveTimer.Stop();
            settingsSaveTimer.Dispose();
            SettingsStore.Save();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            themeTimer.Dispose();
            shutdownComplete = true;

            if (!IsDisposed && IsHandleCreated)
                BeginInvoke((Action)Close);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
