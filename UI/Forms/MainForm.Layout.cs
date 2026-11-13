using System.Drawing;
using System.Windows.Forms;
using AutoClickSelector.Models;
using AutoClickSelector.UI.Components;
using AutoClickSelector.UI.Controls;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Forms;

/// <summary>Phần dựng giao diện của cửa sổ chính (bố cục, control, nhãn).</summary>
public sealed partial class MainForm
{
    private const string UnitSecond = "giây";
    private const string UnitMinute = "phút";
    private const string IconResourceName = "AutoClickSelector.Resources.icon.ico";
    private const int WheelStepPixels = 40;

    private ToolTip toolTip = null!;

    private Label lblConnection = null!;
    private ToggleSwitch toggleDarkMode = null!;

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

    private LogView logView = null!;

    private Panel bodyHost = null!;
    private TableLayoutPanel bodyColumns = null!;
    private ThemedScrollBar bodyBar = null!;

    private void CreateInterface()
    {
        toolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 400, ReshowDelay = 200 };

        SuspendLayout();

        Text = "Auto Click Selector";
        StartPosition = FormStartPosition.CenterScreen;

        // Kích thước cố định: không kéo giãn, không phóng to; vẫn có nút thu nhỏ.
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

        // Bố cục ba hàng: tiêu đề, phần thao tác (có cuộn tự vẽ) và nhật ký (chiều cao cố định).
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

        // Đăng ký sau khi mọi control đã tồn tại để tránh tham chiếu null.
        txtSelector.TextChanged += (_, _) => UpdateControlStates();
        txtKeyword.TextChanged += (_, _) => AppPreferences.SaveKeyword(txtKeyword.Text);

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
        var lblSubtitle = CreateLabel("Tự động thao tác trên trình duyệt CốC CốC", UiTheme.Small, UiTheme.TextMuted, 0);
        titles.Controls.Add(lblTitle);
        titles.Controls.Add(lblSubtitle);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = UiTheme.Background,
            Margin = new Padding(0)
        };
        lblConnection = CreateLabel("Chưa kết nối", UiTheme.Heading, UiTheme.Danger, 8);
        toggleDarkMode = new ToggleSwitch
        {
            Text = "Chế độ tối",
            Checked = UiTheme.IsDark,
            BackColor = UiTheme.Background
        };
        toggleDarkMode.CheckedChanged += ToggleDarkMode_CheckedChanged;
        actions.Controls.Add(lblConnection);
        actions.Controls.Add(toggleDarkMode);

        header.Controls.Add(titles, 0, 0);
        header.Controls.Add(actions, 1, 0);
        return header;
    }

    /// <summary>
    /// Khung thao tác cuộn bằng thanh tự vẽ: nội dung được đặt lệch lên trên theo vị trí cuộn.
    /// </summary>
    private Control BuildBody()
    {
        var left = CreateStack();
        var right = CreateStack();

        AddToStack(left, BuildConnectionCard(), new Padding(0, 0, 10, 16));
        AddToStack(left, BuildSelectorCard(), new Padding(0, 0, 10, 16));
        AddToStack(left, BuildIntervalCard(), new Padding(0, 0, 10, 0));

        AddToStack(right, BuildRunCard(), new Padding(10, 0, 0, 16));
        AddToStack(right, BuildKeywordCard(), new Padding(10, 0, 0, 0));

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

    /// <summary>Đo lại nội dung theo chiều rộng hiện tại rồi cập nhật phạm vi cuộn.</summary>
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

        btnOpenBrowser = CreateButton("Mở trình duyệt", ButtonVariant.Primary, BtnOpenBrowser_Click);
        btnRefreshTabs = CreateButton("Quét tab", ButtonVariant.Secondary, BtnRefreshTabs_Click);
        card.AddInline(btnOpenBrowser, btnRefreshTabs);
        toolTip.SetToolTip(btnOpenBrowser, "Mở CốC CốC với cổng điều khiển. Nếu đang mở sẵn với cổng đó thì chỉ kết nối lại.");
        toolTip.SetToolTip(btnRefreshTabs, "Tìm các tab đang mở để chọn tab cần điều khiển.");

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
        toolTip.SetToolTip(btnPickElement, "Chuyển sang trình duyệt, bấm vào phần tử cần chọn. Nhấn Esc để hủy.");
        toolTip.SetToolTip(btnShowVisible, "Liệt kê chữ đang hiển thị trong khung nhìn, giúp tìm từ khóa quảng cáo.");

        lblSelectorValidation = CreateLabel(string.Empty, UiTheme.Body, UiTheme.TextMuted, 0);
        lblSelectorValidation.Visible = false;
        card.AddRow(lblSelectorValidation, 0);
        return card;
    }

    private SectionCard BuildIntervalCard()
    {
        var card = new SectionCard("3. Chu kỳ click");

        card.AddRow(CreateHint("Khoảng nghỉ giữa các lần click"));
        numInterval = CreateNumeric(minimum: 0.2m, maximum: 100000m, value: 1m, increment: 0.5m, decimalPlaces: 1);
        cmbIntervalUnit = CreateCombo();
        cmbIntervalUnit.Items.AddRange(new object[] { UnitSecond, UnitMinute });
        cmbIntervalUnit.SelectedIndex = 0;
        card.AddInline(numInterval, cmbIntervalUnit);

        card.AddRow(CreateHint("Thời gian chờ tối đa để Selector hiển thị (giây)"));
        numWaitTimeout = CreateNumeric(minimum: 1m, maximum: 600m, value: 10m, increment: 1m, decimalPlaces: 1);
        card.AddRow(numWaitTimeout);
        return card;
    }

    private SectionCard BuildRunCard()
    {
        var card = new SectionCard("4. Chạy thao tác");

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
        var card = new SectionCard("5. Click khi có từ khóa");

        card.AddRow(CreateHint("Từ khóa, cách nhau bằng dấu phẩy"));
        txtKeyword = CreateTextBox("Ví dụ: Sponsored, Quảng cáo");
        txtKeyword.Text = AppPreferences.LoadKeyword();
        card.AddRow(txtKeyword);

        toggleKeyword = new ToggleSwitch { Text = "Bật theo dõi từ khóa", BackColor = UiTheme.Surface };
        toggleKeyword.CheckedChanged += ToggleKeyword_CheckedChanged;
        card.AddRow(toggleKeyword);

        lblKeywordState = CreateLabel(string.Empty, UiTheme.Body, UiTheme.TextMuted, 6);
        card.AddRow(lblKeywordState);

        card.AddRow(CreateHint(
            "Khi thấy từ khóa trên màn hình, ứng dụng bấm Selector ở mục 2 để chuyển sang video tiếp theo. " +
            "Chỉ tính chữ đang hiển thị trong khung nhìn."), 0);
        return card;
    }

    private SectionCard BuildLogCard()
    {
        var card = new SectionCard("Nhật ký hoạt động") { AutoSize = false, Dock = DockStyle.Fill };
        logView = new LogView();
        card.AddFill(logView);
        return card;
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
        Text = text,
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
        var button = new ThemedButton { Text = text, Variant = variant };
        button.Click += onClick;
        return button;
    }

    private static ThemedComboBox CreateCombo() => new();

    private static RoundedTextBox CreateTextBox(string placeholder) => new() { PlaceholderText = placeholder };

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
