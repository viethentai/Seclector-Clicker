using System.Drawing;
using System.Windows.Forms;
using AutoClickSelector.Core;
using AutoClickSelector.Models;
using AutoClickSelector.Services;
using AutoClickSelector.UI.Components;
using AutoClickSelector.UI.Controls;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Forms;

public sealed partial class MainForm
{
    private const string UnitSecond = "giây";
    private const string UnitMinute = "phút";
    private const string IconResourceName = "AutoClickSelector.Resources.icon.ico";
    private const int WheelStepPixels = 40;

    private ToolTip toolTip = null!;

    private Label lblConnection = null!;
    private Label lblLanguage = null!;
    private ToggleSwitch toggleDarkMode = null!;
    private ThemedComboBox cmbLanguage = null!;

    private ThemedComboBox cmbBrowser = null!;
    private ThemedButton btnBrowseBrowser = null!;
    private ThemedComboBox cmbTabs = null!;
    private ThemedButton btnOpenBrowser = null!;
    private ThemedButton btnRefreshTabs = null!;
    private Label lblStatus = null!;

    private ThemedComboBox cmbSavedSelectors = null!;
    private RoundedTextBox txtSelectorName = null!;
    private ThemedButton btnSaveSelector = null!;
    private ThemedButton btnDeleteSelector = null!;
    private RoundedTextBox txtSelector = null!;
    private ThemedButton btnPickElement = null!;
    private ThemedButton btnValidateSelector = null!;
    private ThemedButton btnShowVisible = null!;
    private Label lblSelectorValidation = null!;

    private ThemedNumericBox numInterval = null!;
    private ThemedComboBox cmbIntervalUnit = null!;
    private ThemedNumericBox numWaitTimeout = null!;

    private ThemedButton btnStart = null!;
    private ThemedButton btnStop = null!;
    private ThemedButton btnClick = null!;
    private ThemedButton btnRefreshPage = null!;
    private Label lblAutoState = null!;
    private Label lblStats = null!;

    private RoundedTextBox txtKeyword = null!;
    private ToggleSwitch toggleKeyword = null!;
    private Label lblKeywordState = null!;

    private ToggleSwitch toggleMinimizeToTray = null!;
    private ToggleSwitch toggleCloseToTray = null!;
    private ToggleSwitch toggleNotifications = null!;
    private ToggleSwitch toggleGlobalHotkeys = null!;
    private ToggleSwitch toggleAvoidDuplicate = null!;

    private LogView logView = null!;

    private RoundedTextBox txtLogSearch = null!;
    private ThemedComboBox cmbLogFilter = null!;
    private ThemedButton btnLogExport = null!;
    private ThemedButton btnLogClear = null!;

    private Panel bodyHost = null!;
    private TableLayoutPanel bodyColumns = null!;
    private ThemedScrollBar bodyBar = null!;

    private void CreateInterface()
    {
        toolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 400, ReshowDelay = 200 };

        SuspendLayout();

        Text = "Auto Click Selector";
        StartPosition = FormStartPosition.CenterScreen;

        FormBorderStyle = FormBorderStyle.FixedSingle;
        MinimizeBox = true;
        MaximizeBox = false;
        ClientSize = new Size(1180, 800);
        Size lockedSize = Size;
        MinimumSize = lockedSize;
        MaximumSize = lockedSize;

        BackColor = UiTheme.Background;
        Font = UiTheme.Body;
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;

        if (LoadIcon() is { } icon)
            Icon = icon;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Background,
            Padding = new Padding(22, 18, 22, 18)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildBody(), 0, 1);
        root.Controls.Add(BuildLogCard(), 0, 2);
        Controls.Add(root);

        txtSelector.TextChanged += (_, _) =>
        {
            UpdateControlStates();
            QueueSettingsSave(settings => settings.Selector = txtSelector.Text.Trim());
        };
        txtSelectorName.TextChanged += (_, _) =>
            QueueSettingsSave(settings => settings.SelectorName = txtSelectorName.Text.Trim());
        txtKeyword.TextChanged += (_, _) =>
            QueueSettingsSave(settings => settings.Keyword = txtKeyword.Text);
        numInterval.ValueChanged += (_, _) =>
            SaveSettings(settings => settings.IntervalValue = numInterval.Value);
        cmbIntervalUnit.SelectedIndexChanged += (_, _) =>
            SaveSettings(settings => settings.IntervalUnit = cmbIntervalUnit.SelectedIndex == 1 ? "minute" : "second");
        numWaitTimeout.ValueChanged += (_, _) =>
            SaveSettings(settings => settings.WaitTimeoutSeconds = numWaitTimeout.Value);

        Shown += (_, _) => LayoutBody();

        ResumeLayout(true);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background,
            Margin = new Padding(0, 0, 0, 14)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titles = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = UiTheme.Background,
            Margin = new Padding(0)
        };

        var lblTitle = CreateLabel("Auto Click Selector", UiTheme.Title, UiTheme.TextPrimary, 0);
        var lblSubtitle = CreateLabel(
            "Tự động thao tác trên trình duyệt", UiTheme.Small, UiTheme.TextMuted, 0);

        titles.Controls.Add(lblTitle);
        titles.Controls.Add(lblSubtitle);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.Background,
            Margin = new Padding(0)
        };

        lblConnection = CreateLabel("Chưa kết nối", UiTheme.Heading, UiTheme.Danger, 0);
        lblConnection.Margin = new Padding(0, 10, 14, 0);

        lblLanguage = CreateLabel("Ngôn ngữ", UiTheme.Small, UiTheme.TextMuted, 0);
        lblLanguage.Margin = new Padding(0, 14, 6, 0);

        cmbLanguage = new ThemedComboBox { Width = 132, Margin = new Padding(0, 6, 12, 0) };
        cmbLanguage.Items.AddRange(new object[] { "Tiếng Việt", "English" });
        cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;

        toggleDarkMode = new ToggleSwitch
        {
            Text = Localization.T("Chế độ tối"),
            Tag = "Chế độ tối",
            Checked = UiTheme.IsDark,
            BackColor = UiTheme.Background,
            Margin = new Padding(0, 8, 0, 0)
        };
        toggleDarkMode.CheckedChanged += ToggleDarkMode_CheckedChanged;

        actions.Controls.Add(lblConnection);
        actions.Controls.Add(lblLanguage);
        actions.Controls.Add(cmbLanguage);
        actions.Controls.Add(toggleDarkMode);

        header.Controls.Add(titles, 0, 0);
        header.Controls.Add(actions, 1, 0);
        return header;
    }

    private Control BuildBody()
    {
        var left = CreateStack();
        var right = CreateStack();

        AddToStack(left, BuildConnectionCard(), new Padding(0, 0, 10, 16));
        AddToStack(left, BuildSelectorCard(), new Padding(0, 0, 10, 0));

        AddToStack(right, BuildRunCard(), new Padding(10, 0, 0, 16));
        AddToStack(right, BuildKeywordCard(), new Padding(10, 0, 0, 16));
        AddToStack(right, BuildSettingsCard(), new Padding(10, 0, 0, 0));

        bodyColumns = new TableLayoutPanel
        {
            AutoSize = false,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background,
            Margin = new Padding(0)
        };
        bodyColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bodyColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bodyColumns.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bodyColumns.Controls.Add(left, 0, 0);
        bodyColumns.Controls.Add(right, 1, 0);

        bodyBar = new ThemedScrollBar { Dock = DockStyle.Right, Width = 10 };
        bodyBar.ValueChanged += (_, _) => bodyColumns.Top = -bodyBar.Value;

        bodyHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        bodyHost.Controls.Add(bodyColumns);
        bodyHost.Controls.Add(bodyBar);
        bodyHost.Resize += (_, _) => LayoutBody();

        WheelRouter.Register(bodyHost, delta => bodyBar.ScrollBy(-delta / 120 * WheelStepPixels));
        return bodyHost;
    }

    private void LayoutBody()
    {
        if (bodyHost == null || bodyColumns == null || bodyBar == null)
            return;

        int width = Math.Max(0, bodyHost.ClientSize.Width - bodyBar.Width);
        bodyColumns.Width = width;
        bodyColumns.Height = bodyColumns.GetPreferredSize(new Size(width, 0)).Height;
        bodyBar.SetRange(bodyColumns.Height, bodyHost.ClientSize.Height);
        bodyColumns.Top = -bodyBar.Value;
    }

    private SectionCard BuildConnectionCard()
    {
        var card = new SectionCard("1. Trình duyệt");

        card.AddRow(CreateHint("Chọn trình duyệt cần mở"));

        cmbBrowser = CreateCombo();
        cmbBrowser.SelectedIndexChanged += CmbBrowser_SelectedIndexChanged;
        RefreshBrowserItems();

        btnBrowseBrowser = CreateButton("...", ButtonVariant.Secondary, BtnBrowseBrowser_Click);
        card.AddRow(CreateSplitRow(cmbBrowser, btnBrowseBrowser));

        btnOpenBrowser = CreateButton("Mở trình duyệt", ButtonVariant.Primary, BtnOpenBrowser_Click);
        btnRefreshTabs = CreateButton("Quét tab", ButtonVariant.Secondary, BtnRefreshTabs_Click);
        card.AddInline(btnOpenBrowser, btnRefreshTabs);

        SetToolTip(btnOpenBrowser,
            "Mở trình duyệt đã chọn với cổng điều khiển. Nếu đang mở sẵn với cổng đó thì chỉ kết nối lại.");
        SetToolTip(btnRefreshTabs, "Tìm các tab đang mở để chọn tab cần điều khiển.");
        SetToolTip(btnBrowseBrowser, "Chọn đường dẫn tệp .exe của trình duyệt không có trong danh sách.");

        card.AddRow(CreateHint("Tab đang điều khiển"));

        cmbTabs = CreateCombo();
        cmbTabs.SelectedIndexChanged += CmbTabs_SelectedIndexChanged;
        card.AddRow(cmbTabs);

        lblStatus = CreateLabel(string.Empty, UiTheme.Body, UiTheme.TextMuted, 0);
        card.AddRow(lblStatus, 0);
        return card;
    }

    private SectionCard BuildSelectorCard()
    {
        var card = new SectionCard("2. Selector");

        card.AddRow(CreateHint("Selector đã lưu"));

        cmbSavedSelectors = CreateCombo();
        cmbSavedSelectors.SelectedIndexChanged += CmbSavedSelectors_SelectedIndexChanged;
        card.AddRow(cmbSavedSelectors);

        txtSelectorName = CreateTextBox("Tên gợi nhớ, ví dụ: Nút video tiếp theo");
        card.AddRow(txtSelectorName);

        btnSaveSelector = CreateButton("Lưu", ButtonVariant.Primary, BtnSaveSelector_Click);
        btnDeleteSelector = CreateButton("Xóa", ButtonVariant.Danger, BtnDeleteSelector_Click);
        card.AddInline(btnSaveSelector, btnDeleteSelector);

        card.AddRow(CreateHint("CSS Selector"));

        txtSelector = CreateTextBox("Ví dụ: button[data-e2e='arrow-right']");
        card.AddRow(txtSelector);

        btnPickElement = CreateButton("Chọn phần tử", ButtonVariant.Secondary, BtnPickElement_Click);
        btnValidateSelector = CreateButton("Kiểm tra", ButtonVariant.Secondary, BtnValidateSelector_Click);
        btnShowVisible = CreateButton("Xem chữ hiển thị", ButtonVariant.Secondary, BtnShowVisible_Click);
        card.AddInline(btnPickElement, btnValidateSelector, btnShowVisible);

        SetToolTip(btnPickElement,
            "Chuyển sang trình duyệt, bấm vào phần tử cần chọn. Nhấn Esc để hủy.");
        SetToolTip(btnShowVisible,
            "Liệt kê chữ đang hiển thị trong khung nhìn, giúp tìm từ khóa quảng cáo.");

        lblSelectorValidation = CreateLabel(string.Empty, UiTheme.Body, UiTheme.TextMuted, 0);
        lblSelectorValidation.Visible = false;
        card.AddRow(lblSelectorValidation, 0);
        return card;
    }

    private SectionCard BuildRunCard()
    {
        var card = new SectionCard("3. Chạy thao tác");

        card.AddRow(CreateHint("Khoảng nghỉ giữa các lần click"));

        numInterval = CreateNumeric(minimum: 0.2m, maximum: 100000m, value: 1m, increment: 0.5m, decimalPlaces: 1);
        cmbIntervalUnit = CreateCombo();
        cmbIntervalUnit.Items.AddRange(new object[] { Localization.T(UnitSecond), Localization.T(UnitMinute) });
        cmbIntervalUnit.SelectedIndex = 0;
        card.AddInline(numInterval, cmbIntervalUnit);

        card.AddRow(CreateHint("Thời gian chờ tối đa để Selector hiển thị (giây)"));

        numWaitTimeout = CreateNumeric(minimum: 1m, maximum: 600m, value: 10m, increment: 1m, decimalPlaces: 1);
        card.AddRow(numWaitTimeout);

        btnStart = CreateButton("Bắt đầu (F6)", ButtonVariant.Primary, BtnStart_Click);
        btnStop = CreateButton("Dừng (F7)", ButtonVariant.Danger, BtnStop_Click);
        card.AddInline(btnStart, btnStop);

        btnClick = CreateButton("Click 1 lần (F8)", ButtonVariant.Secondary, BtnClick_Click);
        btnRefreshPage = CreateButton("Làm mới trang", ButtonVariant.Secondary, BtnRefreshPage_Click);
        card.AddInline(btnClick, btnRefreshPage);

        lblAutoState = CreateLabel(string.Empty, UiTheme.Heading, UiTheme.TextMuted, 4);
        card.AddRow(lblAutoState, 4);

        lblStats = CreateLabel(string.Empty, UiTheme.Small, UiTheme.TextMuted, 0);
        card.AddRow(lblStats, 0);
        return card;
    }

    private SectionCard BuildKeywordCard()
    {
        var card = new SectionCard("4. Click khi có từ khóa");

        card.AddRow(CreateHint("Từ khóa, cách nhau bằng dấu phẩy"));

        txtKeyword = CreateTextBox("Ví dụ: Sponsored, Quảng cáo");
        txtKeyword.Text = SettingsStore.Current.Keyword;
        card.AddRow(txtKeyword);

        toggleKeyword = new ToggleSwitch
        {
            Text = Localization.T("Bật theo dõi từ khóa"),
            Tag = "Bật theo dõi từ khóa",
            BackColor = UiTheme.Surface
        };
        toggleKeyword.CheckedChanged += ToggleKeyword_CheckedChanged;
        card.AddRow(toggleKeyword);

        lblKeywordState = CreateLabel(string.Empty, UiTheme.Body, UiTheme.TextMuted, 6);
        card.AddRow(lblKeywordState);

        card.AddRow(CreateHint(
            "Khi thấy từ khóa trên màn hình, ứng dụng bấm Selector ở mục 2 để chuyển sang video tiếp theo. " +
            "Chỉ tính chữ đang hiển thị trong khung nhìn."), 0);
        return card;
    }

    private SectionCard BuildSettingsCard()
    {
        var card = new SectionCard("5. Cài đặt");

        toggleMinimizeToTray = CreateSettingToggle(
            "Thu nhỏ xuống khay khi chạy", ToggleMinimizeToTray_CheckedChanged);
        card.AddRow(toggleMinimizeToTray);

        toggleCloseToTray = CreateSettingToggle(
            "Thoát xuống khay khi bấm X", ToggleCloseToTray_CheckedChanged);
        card.AddRow(toggleCloseToTray);

        toggleNotifications = CreateSettingToggle(
            "Thông báo khi dừng hoặc lỗi", ToggleNotifications_CheckedChanged);
        card.AddRow(toggleNotifications);

        toggleGlobalHotkeys = CreateSettingToggle(
            "Phím tắt toàn cục (F6/F7/F8)", ToggleGlobalHotkeys_CheckedChanged);
        card.AddRow(toggleGlobalHotkeys);

        toggleAvoidDuplicate = CreateSettingToggle(
            "Bỏ qua click trùng khi phần tử không đổi", ToggleAvoidDuplicate_CheckedChanged);
        card.AddRow(toggleAvoidDuplicate, 0);
        return card;
    }

    private SectionCard BuildLogCard()
    {
        var card = new SectionCard("Nhật ký hoạt động") { AutoSize = false, Dock = DockStyle.Fill };

        logView = new LogView();

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 32,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 0, 0, 8)
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        txtLogSearch = CreateTextBox("Tìm trong nhật ký...");
        txtLogSearch.Dock = DockStyle.Fill;
        txtLogSearch.Margin = new Padding(0, 0, 8, 0);
        txtLogSearch.TextChanged += (_, _) => logView.SetSearch(txtLogSearch.Text);

        cmbLogFilter = CreateCombo();
        cmbLogFilter.Dock = DockStyle.Fill;
        cmbLogFilter.Margin = new Padding(0, 0, 8, 0);
        cmbLogFilter.SelectedIndexChanged += CmbLogFilter_SelectedIndexChanged;

        btnLogExport = CreateButton("Xuất", ButtonVariant.Secondary, BtnLogExport_Click);
        btnLogExport.Dock = DockStyle.Fill;
        btnLogExport.Margin = new Padding(0, 0, 8, 0);

        btnLogClear = CreateButton("Xoá nhật ký", ButtonVariant.Ghost, BtnLogClear_Click);
        btnLogClear.Dock = DockStyle.Fill;
        btnLogClear.Margin = new Padding(0);

        toolbar.Controls.Add(txtLogSearch, 0, 0);
        toolbar.Controls.Add(cmbLogFilter, 1, 0);
        toolbar.Controls.Add(btnLogExport, 2, 0);
        toolbar.Controls.Add(btnLogClear, 3, 0);

        card.AddRow(toolbar, 8);

        RefreshLogFilterItems();
        cmbLogFilter.SelectedIndex = 0;

        card.AddFill(logView);
        return card;
    }

    private void RefreshLogFilterItems()
    {
        if (cmbLogFilter == null)
            return;

        int selected = cmbLogFilter.SelectedIndex;
        cmbLogFilter.Items.Clear();
        cmbLogFilter.Items.AddRange(new object[]
        {
            Localization.T("Tất cả"),
            Localization.T("Lỗi"),
            Localization.T("Cảnh báo"),
            Localization.T("Thành công"),
            Localization.T("Thông tin"),
            Localization.T("Đang xử lý")
        });
        cmbLogFilter.SelectedIndex = selected < 0 || selected >= cmbLogFilter.Items.Count ? 0 : selected;
    }

    private void RefreshBrowserItems()
    {
        if (cmbBrowser == null)
            return;

        int selected = cmbBrowser.SelectedIndex;
        cmbBrowser.Items.Clear();
        cmbBrowser.Items.AddRange(new object[]
        {
            "Cốc Cốc",
            "Microsoft Edge",
            "Google Chrome",
            Localization.T("Trình duyệt khác")
        });
        cmbBrowser.SelectedIndex = selected < 0 || selected >= cmbBrowser.Items.Count ? 0 : selected;
    }

    private void RefreshIntervalUnitItems()
    {
        if (cmbIntervalUnit == null)
            return;

        int selected = cmbIntervalUnit.SelectedIndex;
        cmbIntervalUnit.Items.Clear();
        cmbIntervalUnit.Items.AddRange(new object[] { Localization.T(UnitSecond), Localization.T(UnitMinute) });
        cmbIntervalUnit.SelectedIndex = selected < 0 ? 0 : selected;
    }

    private static TableLayoutPanel CreateSplitRow(Control main, Control side)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        row.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        main.Dock = DockStyle.Fill;
        main.Margin = new Padding(0, 0, 8, 0);
        side.Dock = DockStyle.Fill;
        side.Margin = new Padding(0);

        row.Controls.Add(main, 0, 0);
        row.Controls.Add(side, 1, 0);
        return row;
    }

    private static TableLayoutPanel CreateStack()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            BackColor = UiTheme.Background,
            Margin = new Padding(0)
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return stack;
    }

    private static void AddToStack(TableLayoutPanel stack, Control card, Padding margin)
    {
        int row = stack.RowCount;
        stack.RowCount = row + 1;
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.Dock = DockStyle.Fill;
        card.Margin = margin;
        stack.Controls.Add(card, 0, row);
    }

    private static Label CreateLabel(string text, Font font, Color color, int bottomMargin) => new()
    {
        Text = Localization.T(text),
        Tag = text,
        Font = font,
        ForeColor = color,
        BackColor = Color.Transparent,
        AutoSize = true,
        MaximumSize = new Size(420, 0),
        Margin = new Padding(0, 0, 0, bottomMargin)
    };

    private static Label CreateHint(string text) => CreateLabel(text, UiTheme.Small, UiTheme.TextMuted, 4);

    private static ThemedButton CreateButton(string text, ButtonVariant variant, EventHandler onClick)
    {
        var button = new ThemedButton { Text = Localization.T(text), Tag = text, Variant = variant };
        button.Click += onClick;
        return button;
    }

    private static ThemedComboBox CreateCombo() => new();

    private RoundedTextBox CreateTextBox(string placeholder)
    {
        var box = new RoundedTextBox { PlaceholderText = Localization.T(placeholder) };
        localizedPlaceholders.Add((box, placeholder));
        return box;
    }

    private void SetToolTip(Control control, string text)
    {
        toolTip.SetToolTip(control, Localization.T(text));
        localizedToolTips.Add((control, text));
    }

    private ToggleSwitch CreateSettingToggle(string text, EventHandler changed)
    {
        var toggle = new ToggleSwitch
        {
            Text = Localization.T(text),
            Tag = text,
            BackColor = UiTheme.Surface
        };
        toggle.CheckedChanged += changed;
        return toggle;
    }

    private static ThemedNumericBox CreateNumeric(
        decimal minimum,
        decimal maximum,
        decimal value,
        decimal increment,
        int decimalPlaces) => new()
    {
        DecimalPlaces = decimalPlaces,
        Increment = increment,
        Minimum = minimum,
        Maximum = maximum,
        Value = value
    };

    private static Icon? LoadIcon()
    {
        try
        {
            using Stream? stream = typeof(MainForm).Assembly.GetManifestResourceStream(IconResourceName);
            return stream is null ? null : new Icon(stream);
        }
        catch (Exception exception) when (exception is IOException or ArgumentException)
        {
            return null;
        }
    }
}
