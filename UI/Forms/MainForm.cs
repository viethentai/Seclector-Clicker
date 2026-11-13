using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoClickSelector.Core;
using AutoClickSelector.Models;
using AutoClickSelector.Services;
using AutoClickSelector.UI.Controls;
using AutoClickSelector.UI.Themes;
using static AutoClickSelector.Services.TextFormatter;

namespace AutoClickSelector.UI.Forms;

/// <summary>Cửa sổ chính: nối giao diện với các dịch vụ thao tác và lưu trạng thái.</summary>
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
        UiTheme.SetDark(AppPreferences.LoadDarkMode());
        CreateInterface();

        autoClickService = new AutoClickService(browser, clickGate, Log, RecordClick);
        keywordWatcher = new KeywordWatcherService(browser, clickGate, Log, RecordClick, SetKeywordState);

        statsTimer.Tick += (_, _) => UpdateStatistics();
        themeTimer.Tick += ThemeTimer_Tick;

        LoadSavedSelectors();
        SetKeywordState(KeywordState.Off);
        UpdateConnectionStatus();
        UpdateAutoState();
        UpdateStatistics();
        UpdateControlStates();

        Log(ActivityLevel.Info,
            "Sẵn sàng. Bấm \"Mở trình duyệt\" để mở CốC CốC, hoặc \"Quét tab\" nếu CốC CốC đã mở sẵn với cổng kết nối.");
    }

    private bool IsAutoClickRunning => autoClickCancellation != null;

    private bool IsKeywordWatcherRunning => keywordCancellation != null;

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

    // ------------------------------------------------------------------
    // Trạng thái giao diện
    // ------------------------------------------------------------------

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
        btnClick.Enabled = idle && hasSelector;

        numInterval.Enabled = idle && hasSelector;
        cmbIntervalUnit.Enabled = idle && hasSelector;
        numWaitTimeout.Enabled = idle && hasSelector;
        txtKeyword.Enabled = idle;

        btnStart.Enabled = !IsAutoClickRunning && canStartNew && hasSelector;
        btnStop.Enabled = IsAutoClickRunning;
        toggleKeyword.Enabled = IsKeywordWatcherRunning || canStartNew;
    }

    private void UpdateAutoState()
    {
        (string text, Color color) = isStarting
            ? ("Đang khởi động...", UiTheme.Warning)
            : IsAutoClickRunning
                ? ("Đang chạy", UiTheme.Success)
                : ("Đã dừng", UiTheme.TextMuted);

        lblAutoState.Text = "Chạy thao tác: " + text;
        lblAutoState.ForeColor = color;
    }

    private void SetKeywordState(KeywordState state)
    {
        (string text, Color color) = state switch
        {
            KeywordState.Waiting => ("Đang chờ từ khóa", UiTheme.Accent),
            KeywordState.Processing => ("Đang xử lý", UiTheme.Process),
            KeywordState.Stopping => ("Đang tắt...", UiTheme.Warning),
            _ => ("Đã tắt", UiTheme.TextMuted)
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
            SetConnection("● Sẵn sàng", UiTheme.Success, $"Đã chọn: {tab.Title} | {ShortUrl(tab.Url)}");
        else if (browser.IsBrowserConnected)
            SetConnection("● Đã kết nối", UiTheme.Warning, "Hãy quét tab và chọn một tab.");
        else
            SetConnection("● Chưa kết nối", UiTheme.Danger, "Mở trình duyệt hoặc quét tab để kết nối.");
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
        lblStats.Text = $"Đã click: {totalClicks}  |  Thành công: {successfulClicks}  |  Lỗi: {failedClicks}  |  Thời gian: {elapsed}";
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

    private void StartTrackedBrowserOperation(Func<Task> operation)
    {
        if (shutdownInProgress)
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
                Log(ActivityLevel.Error, "Thao tác trình duyệt gặp lỗi: " + DescribeError(exception));
        }
        finally
        {
            activeBrowserOperations.Remove(task);
            if (!shutdownInProgress)
                UpdateControlStates();
        }
    }

    // ------------------------------------------------------------------
    // Trình duyệt: mở, quét tab, chọn tab
    // ------------------------------------------------------------------

    private void BtnRefreshTabs_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(BtnRefreshTabsAsync);

    private async Task BtnRefreshTabsAsync()
    {
        isScanningTabs = true;
        UpdateControlStates();
        try
        {
            Log(ActivityLevel.Process, "Đang kết nối CốC CốC để quét tab...");
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
        btnOpenBrowser.Text = "Đang mở...";
        UpdateControlStates();
        try
        {
            await OpenBrowserAsync();
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, "Không mở được trình duyệt: " + DescribeError(exception));
        }
        finally
        {
            isOpeningBrowser = false;
            btnOpenBrowser.Text = OpenBrowserIdleText;
            UpdateControlStates();
            UpdateConnectionStatus();
        }
    }

    private async Task OpenBrowserAsync()
    {
        if (await BrowserLauncher.IsDebugPortOpenAsync())
        {
            Log(ActivityLevel.Success, "CốC CốC đã mở sẵn với cổng kết nối. Hãy bấm 'Quét tab' để chọn tab.");
            return;
        }

        string? executable = BrowserLauncher.FindExecutable();
        if (executable == null)
        {
            Log(ActivityLevel.Warning, "Không tìm thấy CốC CốC ở vị trí mặc định. Hãy chọn file browser.exe của CốC CốC.");
            executable = PromptForBrowserExecutable();
            if (executable == null)
            {
                Log(ActivityLevel.Warning, "Chưa chọn trình duyệt nên đã hủy mở trình duyệt.");
                return;
            }

            try
            {
                BrowserLauncher.SaveExecutable(executable);
                Log(ActivityLevel.Info, "Đã lưu đường dẫn trình duyệt để lần sau dùng lại.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Log(ActivityLevel.Warning, "Không lưu được đường dẫn trình duyệt: " + DescribeError(exception));
            }
        }

        if (BrowserLauncher.IsBrowserRunning(executable))
        {
            Log(ActivityLevel.Warning, "CốC CốC đang chạy nhưng chưa mở cổng kết nối nên không thể điều khiển.");
            DialogResult answer = MessageBox.Show(
                this,
                "CốC CốC đang mở mà không có cổng kết nối.\n\n" +
                "Đóng CốC CốC để ứng dụng mở lại với cấu hình kết nối? Các tab chưa lưu có thể bị mất.",
                AppTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (answer != DialogResult.Yes)
            {
                Log(ActivityLevel.Info, "Đã hủy. Hãy tự đóng CốC CốC rồi bấm 'Mở trình duyệt' lại.");
                return;
            }

            Log(ActivityLevel.Process, "Đang đóng CốC CốC...");
            bool closed = await BrowserLauncher.CloseBrowserAsync(executable, TimeSpan.FromSeconds(15));
            if (!closed)
            {
                Log(ActivityLevel.Error,
                    "CốC CốC vẫn đang chạy (có thể đang chạy nền). Hãy tắt chế độ chạy nền của CốC CốC " +
                    "hoặc thoát từ khay hệ thống, rồi bấm 'Mở trình duyệt' lại.");
                return;
            }

            Log(ActivityLevel.Info, "Đã đóng CốC CốC.");
        }

        Log(ActivityLevel.Process, "Đang khởi chạy trình duyệt...");
        try
        {
            BrowserLauncher.Start(executable, BrowserLauncher.DebugPort);
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, "Không khởi chạy được trình duyệt. Hãy kiểm tra đường dẫn: " + DescribeError(exception));
            return;
        }

        bool ready = await BrowserLauncher.WaitForDebugPortAsync(BrowserLauncher.DebugPort, TimeSpan.FromSeconds(15));
        if (ready)
        {
            Log(ActivityLevel.Success, "Trình duyệt đã được mở thành công. Hãy bấm 'Quét tab' để chọn tab.");
        }
        else
        {
            Log(ActivityLevel.Error,
                "Đã gọi lệnh mở CốC CốC nhưng cổng kết nối chưa sẵn sàng. Có thể CốC CốC còn chạy nền từ phiên trước. " +
                "Hãy đóng hẳn CốC CốC rồi bấm 'Mở trình duyệt' lại.");
        }
    }

    private string? PromptForBrowserExecutable()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn file browser.exe của CốC CốC",
            Filter = "Chương trình (browser.exe)|browser.exe|Chương trình (*.exe)|*.exe",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    private async Task RefreshTabsAsync()
    {
        List<BrowserTab> tabs = await browser.GetTabsAsync();

        // Ưu tiên tab đang gắn kết nối; nếu trang đã chuyển URL thì tìm theo đối tượng trang.
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
            // Không đưa tab ra trước mỗi lần quét, tránh làm phiền người dùng.
            await browser.SelectTabAsync(tabs[selectedIndex], bringToFront: false);
            Log(ActivityLevel.Success, $"Tìm thấy {tabs.Count} tab; đang chọn: {ShortUrl(tabs[selectedIndex].Url)}");
        }
        else if (tabs.Count > 0)
        {
            Log(ActivityLevel.Info, $"Tìm thấy {tabs.Count} tab. Hãy chọn tab cần dùng trong danh sách.");
        }
        else
        {
            Log(ActivityLevel.Warning, "Không tìm thấy tab đang mở.");
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
            UpdateConnectionStatus();
            Log(ActivityLevel.Success, $"Đã chọn tab: {selectedTab.Title} | {ShortUrl(selectedTab.Url)}");
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, "Không thể chọn tab: " + DescribeError(exception));
            UpdateConnectionStatus();
        }
        finally
        {
            isSelectingTab = false;
            UpdateControlStates();
        }
    }

    // ------------------------------------------------------------------
    // Selector: kiểm tra, chọn, lưu
    // ------------------------------------------------------------------

    private void BtnValidateSelector_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(ValidateSelectorAsync);

    private async Task ValidateSelectorAsync()
    {
        if (string.IsNullOrWhiteSpace(txtSelector.Text))
        {
            ShowSelectorValidation(false, "Vui lòng nhập Selector trước.");
            return;
        }

        if (isPickingElement)
        {
            Log(ActivityLevel.Warning, "Hãy hoàn tất hoặc hủy thao tác chọn phần tử trước.");
            return;
        }

        if (!browser.IsConnected)
        {
            ShowSelectorValidation(false, "Hãy kết nối và chọn tab trước.");
            return;
        }

        btnValidateSelector.Enabled = false;
        try
        {
            SelectorValidationResult result = await browser.ValidateSelectorAsync(txtSelector.Text);
            if (!result.IsValid)
            {
                ShowSelectorValidation(false, "✗ Selector không hợp lệ: " + result.Error);
            }
            else if (result.MatchCount == 0)
            {
                ShowSelectorValidation(true, "Selector hợp lệ nhưng chưa tìm thấy phần tử nào.");
            }
            else
            {
                ShowSelectorValidation(true,
                    $"✓ Selector hợp lệ  |  Tìm thấy: {result.MatchCount}  |  Hiển thị: {result.VisibleCount}");
            }
        }
        catch (Exception exception)
        {
            ShowSelectorValidation(false, "Không thể kiểm tra Selector: " + DescribeError(exception));
            UpdateConnectionStatus();
        }
        finally
        {
            UpdateControlStates();
        }
    }

    /// <summary>Liệt kê chữ đang hiển thị để người dùng tìm từ khóa quảng cáo.</summary>
    private void BtnShowVisible_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(ShowVisibleTextAsync);

    private async Task ShowVisibleTextAsync()
    {
        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, "Hãy quét tab và chọn một tab trước.");
            return;
        }

        btnShowVisible.Enabled = false;
        try
        {
            List<string> lines = await browser.GetVisibleTextAsync(ParseKeywords(txtKeyword.Text));

            if (lines.Count == 0)
            {
                Log(ActivityLevel.Info, "Không thấy văn bản nào đang hiển thị trên màn hình.");
            }
            else
            {
                Log(ActivityLevel.Info, $"Văn bản đang hiển thị trên màn hình ({lines.Count}; ★ = khớp từ khóa):");
                foreach (string line in lines)
                    Log(ActivityLevel.Info, "    " + line);
            }
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, "Không thể đọc văn bản trên trang: " + DescribeError(exception));
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
            Log(ActivityLevel.Warning, "Hãy kết nối và chọn tab trước khi chọn phần tử.");
            return;
        }

        if (IsBusy)
        {
            Log(ActivityLevel.Warning, "Đang có thao tác khác. Hãy dừng các chức năng đang chạy trước khi chọn phần tử.");
            return;
        }

        if (!await clickGate.WaitAsync(0))
        {
            Log(ActivityLevel.Warning, "Đang có thao tác khác. Vui lòng thử lại.");
            return;
        }

        isPickingElement = true;
        btnPickElement.Text = "Đang chọn...";
        lblSelectorValidation.Text = "Đang chờ bạn chọn phần tử trên trình duyệt (Esc để hủy).";
        lblSelectorValidation.Visible = true;
        lblSelectorValidation.ForeColor = UiTheme.Warning;
        UpdateControlStates();
        Log(ActivityLevel.Process,
            "Đang chuyển sang trình duyệt. Hãy bấm vào phần tử cần chọn (Esc để hủy); " +
            "ứng dụng sẽ tự quay lại sau khi bạn chọn.");

        try
        {
            if (!await browser.BringBrowserToForegroundAsync())
                Log(ActivityLevel.Warning, "Không tự chuyển được sang trình duyệt. Hãy tự mở tab đó rồi bấm vào phần tử.");

            string? selector = await browser.PickElementAsync();
            if (string.IsNullOrWhiteSpace(selector))
            {
                Log(ActivityLevel.Info, "Đã hủy chọn phần tử.");
                return;
            }

            txtSelector.Text = selector;
            lblSelectorValidation.Text = "Đã tạo Selector. Hãy bấm Kiểm tra trước khi lưu hoặc chạy.";
            lblSelectorValidation.Visible = true;
            lblSelectorValidation.ForeColor = UiTheme.Warning;
            Log(ActivityLevel.Success, "Đã chọn phần tử và tạo Selector: " + selector);
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, "Không thể chọn phần tử: " + DescribeError(exception));
            UpdateConnectionStatus();
        }
        finally
        {
            isPickingElement = false;
            btnPickElement.Text = PickElementIdleText;
            clickGate.Release();
            if (!shutdownInProgress)
            {
                UpdateControlStates();
                UpdateConnectionStatus();

                // Chọn xong (hoặc hủy): đưa cửa sổ ứng dụng trở lại.
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
            Log(ActivityLevel.Warning, "Vui lòng nhập tên cho Selector.");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtSelector.Text))
        {
            Log(ActivityLevel.Warning, "Vui lòng nhập CSS Selector.");
            return;
        }

        string name = txtSelectorName.Text.Trim();
        savedSelectors[name] = txtSelector.Text.Trim();
        SaveSelectors();
        RefreshSavedSelectorList(name);
        Log(ActivityLevel.Success, $"Đã lưu Selector \"{name}\".");
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
        Log(ActivityLevel.Info, $"Đã xóa Selector \"{name}\".");
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
            Log(ActivityLevel.Error, "Không thể tải danh sách Selector đã lưu: " + DescribeError(exception));
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
            Log(ActivityLevel.Error, "Không thể lưu Selector: " + DescribeError(exception));
        }
    }

    private void BtnRefreshPage_Click(object? sender, EventArgs e) =>
        StartTrackedBrowserOperation(RefreshPageAsync);

    private async Task RefreshPageAsync()
    {
        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, "Vui lòng kết nối và chọn tab trước.");
            return;
        }

        try
        {
            await browser.RefreshPageAsync();
            Log(ActivityLevel.Success, "Đã làm mới trang web.");
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, "Không thể làm mới trang web: " + DescribeError(exception));
        }
    }

    // ------------------------------------------------------------------
    // Chạy thao tác: bấm Selector theo chu kỳ
    // ------------------------------------------------------------------

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (IsAutoClickRunning || isStarting)
        {
            Log(ActivityLevel.Info, "Chạy thao tác đang chạy.");
            return;
        }

        if (isPickingElement)
        {
            Log(ActivityLevel.Warning, "Hãy hoàn tất hoặc hủy thao tác chọn phần tử trước.");
            return;
        }

        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, "Hãy quét tab và chọn một tab trước.");
            return;
        }

        string selector = txtSelector.Text.Trim();
        if (selector.Length == 0)
        {
            Log(ActivityLevel.Warning, "Vui lòng nhập CSS Selector trước khi bắt đầu.");
            return;
        }

        if (shutdownInProgress)
            return;

        // Đặt cờ trước khi await để không mở thêm vòng lặp khi người dùng bấm dồn.
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
            Log(ActivityLevel.Error, "Không thể bắt đầu: " + DescribeError(exception));
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
            ShowSelectorValidation(false, "Selector không hợp lệ. Vui lòng kiểm tra lại.");
            return;
        }

        if (validation.MatchCount == 0)
            Log(ActivityLevel.Warning, $"Chưa thấy phần tử. Sẽ chờ tối đa {numWaitTimeout.Value} giây.");

        if (IsKeywordWatcherRunning && string.Equals(selector, keywordSelector, StringComparison.Ordinal))
        {
            Log(ActivityLevel.Warning,
                "Lưu ý: 'Click khi có từ khóa' đang dùng cùng Selector này. Hai chức năng sẽ cùng bấm vào một nút.");
        }

        var cancellation = new CancellationTokenSource();
        autoClickCancellation = cancellation;
        ResetStatistics();
        statsTimer.Start();
        UpdateStatistics();
        UpdateControlStates();
        UpdateAutoState();

        Log(ActivityLevel.Process, "Chạy thao tác: bắt đầu bấm Selector theo chu kỳ. Bấm Dừng để kết thúc.");

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
            // Người dùng bấm Dừng.
        }
        catch (SelectorWaitTimeoutException)
        {
            // Đã ghi nhật ký trong dịch vụ.
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, "Chạy thao tác đã dừng do lỗi: " + DescribeError(exception));
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

            Log(ActivityLevel.Info, "Chạy thao tác đã dừng.");
        }
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        if (!IsAutoClickRunning)
            return;

        Log(ActivityLevel.Info, "Đang dừng chạy thao tác...");
        autoClickCancellation?.Cancel();
    }

    private async void BtnClick_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSelector.Text))
        {
            Log(ActivityLevel.Warning, "Vui lòng nhập Selector trước khi Click.");
            return;
        }

        if (!browser.IsConnected)
        {
            Log(ActivityLevel.Warning, "Hãy quét tab và chọn một tab trước.");
            return;
        }

        if (!await clickGate.WaitAsync(0))
        {
            Log(ActivityLevel.Warning, "Đang có click khác.");
            return;
        }

        btnClick.Enabled = false;
        var cancellation = new CancellationTokenSource();
        manualClickCancellation = cancellation;
        try
        {
            Log(ActivityLevel.Process, $"Đang chờ Selector tối đa {numWaitTimeout.Value} giây...");
            Task clickTask = browser.ClickAsync(
                txtSelector.Text,
                GetWaitTimeoutMilliseconds(),
                cancellation.Token);
            manualClickTask = clickTask;
            await clickTask;

            RecordClick(true);
            Log(ActivityLevel.Success, "Click thành công.");
        }
        catch (OperationCanceledException)
        {
            if (!shutdownInProgress)
                Log(ActivityLevel.Info, "Đã hủy thao tác Click.");
        }
        catch (SelectorWaitTimeoutException)
        {
            RecordClick(false);
            Log(ActivityLevel.Warning, "Không tìm thấy phần tử trong thời gian chờ. Hãy kiểm tra Selector và thử lại.");
        }
        catch (Exception exception)
        {
            RecordClick(false);
            Log(ActivityLevel.Error, "Click lỗi: " + DescribeError(exception));
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

    // ------------------------------------------------------------------
    // Click khi có từ khóa: theo dõi màn hình (độc lập với chạy thao tác)
    // ------------------------------------------------------------------

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
            // Đang tắt từ lần trước; không mở thêm vòng lặp song song.
            SetKeywordToggle(false);
            Log(ActivityLevel.Warning, "Click khi có từ khóa đang tắt. Hãy thử bật lại sau giây lát.");
            return;
        }

        string selector = txtSelector.Text.Trim();
        List<string> keywords = ParseKeywords(txtKeyword.Text);

        string? problem = null;
        if (isStarting || isPickingElement || isOpeningBrowser || isScanningTabs)
            problem = "Đang có thao tác khác. Hãy thử lại sau.";
        else if (!browser.IsConnected)
            problem = "Hãy mở trình duyệt, quét tab và chọn một tab trước.";
        else if (selector.Length == 0)
            problem = "Vui lòng nhập Selector (nút 'Video tiếp theo') trước khi bật chức năng này.";
        else if (keywords.Count == 0)
            problem = "Vui lòng nhập ít nhất một từ khóa (cách nhau bằng dấu phẩy).";

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
        Log(ActivityLevel.Process, $"Click khi có từ khóa: đã bật. Từ khóa: {string.Join(", ", keywords)}.");

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
            // Người dùng đã tắt.
        }
        catch (Exception exception)
        {
            Log(ActivityLevel.Error, "Click khi có từ khóa đã dừng do lỗi: " + DescribeError(exception));
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
            Log(ActivityLevel.Info, "Click khi có từ khóa: đã tắt.");
        }
    }

    // ------------------------------------------------------------------
    // Phím tắt, tham số, giao diện sáng/tối
    // ------------------------------------------------------------------

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
        decimal multiplier = cmbIntervalUnit.SelectedItem?.ToString() == UnitMinute ? 60_000m : 1_000m;
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

        try
        {
            AppPreferences.SaveDarkMode(themeTargetDark);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log(ActivityLevel.Warning, "Không thể lưu tùy chọn giao diện: " + DescribeError(exception));
        }

        Log(ActivityLevel.Info, themeTargetDark ? "Đã chuyển sang giao diện tối." : "Đã chuyển sang giao diện sáng.");
    }

    /// <summary>Chuyển màu dần dần trong khoảng 220 ms cho dễ nhìn.</summary>
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

    /// <summary>
    /// Đổi màu từng control từ bảng cũ sang bảng mới, rồi đệ quy xuống các control con.
    /// Các control tự vẽ (nút, công tắc, nhật ký) được vẽ lại theo bảng màu hiện tại.
    /// </summary>
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

    /// <summary>Tô thanh tiêu đề Windows theo chế độ sáng/tối (Windows 10 1809 trở lên).</summary>
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
            // Hệ điều hành cũ không hỗ trợ; giữ thanh tiêu đề mặc định.
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTitleBarTheme();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel || shutdownComplete)
            return;

        // Giữ message loop hoạt động trong lúc các tác vụ nền kết thúc.
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
            // Chờ thao tác kiểm tra Selector đang dở; nó có thể khởi chạy Auto Click
            // ngay sau khi hoàn tất, vì vậy hủy token lần nữa bên dưới.
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
            // Tiếp tục giải phóng kết nối ngay cả khi một tác vụ nền kết thúc do lỗi.
            Log(ActivityLevel.Warning, "Một tác vụ nền kết thúc khi đóng ứng dụng: " + DescribeError(exception));
        }
        finally
        {
            try
            {
                await browser.StopAsync();
            }
            catch (Exception exception)
            {
                Log(ActivityLevel.Warning, "Không thể đóng hoàn toàn kết nối trình duyệt: " + DescribeError(exception));
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
