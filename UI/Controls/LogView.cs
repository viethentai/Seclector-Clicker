using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutoClickSelector.Models;
using AutoClickSelector.UI.Components;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Controls;

/// <summary>
/// Khung nhật ký bo góc với thanh cuộn tự vẽ. Nội dung là một RichTextBox không có thanh cuộn gốc;
/// chiều cao của nó bằng đúng nội dung và được đẩy lên/xuống trong một khung cắt (clip) như thanh cuộn của khung chính.
/// </summary>
public sealed class LogView : Control
{
    private const int MaxEntries = 500;
    private const int MaxRichHeight = 30000;
    private const int CornerRadius = 10;
    private const int Inset = 8;
    private const int BarWidth = 10;
    private const int WheelStepPixels = 40;

    private readonly List<LogEntry> entries = new();
    private readonly Panel viewport;
    private readonly RichTextBox output;
    private readonly ThemedScrollBar scrollBar;
    private Palette? renderedPalette;

    public LogView()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = UiTheme.SurfaceAlt;

        viewport = new Panel { BackColor = UiTheme.SurfaceAlt };

        output = new RichTextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            DetectUrls = false,
            WordWrap = true,
            HideSelection = false,
            ScrollBars = RichTextBoxScrollBars.None,
            Font = UiTheme.Body,
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.TextPrimary,
            Location = Point.Empty
        };

        scrollBar = new ThemedScrollBar { Width = BarWidth };
        scrollBar.ValueChanged += (_, _) => output.Top = -scrollBar.Value;

        viewport.Controls.Add(output);
        Controls.Add(viewport);
        Controls.Add(scrollBar);

        WheelRouter.Register(viewport, delta => scrollBar.ScrollBy(-delta / 120 * WheelStepPixels));
    }

    public void AddEntry(ActivityLevel level, string message)
    {
        // Đang ở cuối thì tự cuộn theo dòng mới; nếu người dùng đã cuộn lên thì giữ nguyên vị trí.
        bool follow = scrollBar.Value >= scrollBar.MaxValue;

        entries.Add(new LogEntry(DateTime.Now, level, message));
        if (entries.Count > MaxEntries)
        {
            entries.RemoveRange(0, entries.Count - MaxEntries);
            Render(follow);
            return;
        }

        output.Height = MaxRichHeight;
        AppendEntry(entries[^1]);
        Relayout(follow);
    }

    public void ClearEntries()
    {
        entries.Clear();
        Render(followBottom: true);
    }

    public void RefreshTheme()
    {
        if (!ReferenceEquals(renderedPalette, UiTheme.Current))
            Render(followBottom: false);
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Relayout(followBottom: false);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(UiTheme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using GraphicsPath path = UiDrawing.RoundedRectangle(rect, CornerRadius);
        using var fill = new SolidBrush(UiTheme.SurfaceAlt);
        g.FillPath(fill, path);
    }

    /// <summary>Đặt lại vùng hiển thị, đo chiều cao nội dung và cập nhật thanh cuộn.</summary>
    private void Relayout(bool followBottom)
    {
        int viewHeight = Math.Max(0, Height - Inset * 2);
        int viewWidth = Math.Max(0, Width - Inset * 2 - BarWidth - 4);

        viewport.SetBounds(Inset, Inset, viewWidth, viewHeight);
        scrollBar.SetBounds(Width - Inset - BarWidth, Inset, BarWidth, viewHeight);

        // Chiều rộng quyết định cách xuống dòng; đặt chiều cao lớn trước để đo chính xác.
        output.Width = viewWidth;
        output.Height = MaxRichHeight;
        int content = Math.Min(MeasureContentHeight(), MaxRichHeight);
        output.Height = Math.Max(viewHeight, content);

        scrollBar.SetRange(output.Height, Math.Max(1, viewHeight));
        if (followBottom)
            scrollBar.ScrollToEnd();

        output.Top = -scrollBar.Value;
    }

    private int MeasureContentHeight()
    {
        if (output.TextLength == 0)
            return 0;

        Point last = output.GetPositionFromCharIndex(output.TextLength - 1);
        return last.Y + output.Font.Height + 8;
    }

    private void Render(bool followBottom)
    {
        output.Clear();
        renderedPalette = UiTheme.Current;
        output.Height = MaxRichHeight;

        foreach (LogEntry entry in entries)
            AppendEntry(entry);

        Relayout(followBottom);
    }

    private void AppendEntry(LogEntry entry)
    {
        AppendColored($"[{entry.Time:HH:mm:ss}] ", UiTheme.TextMuted);
        AppendColored($"{LevelLabel(entry.Level)}  ", LevelColor(entry.Level));
        AppendColored(entry.Message + Environment.NewLine, UiTheme.TextPrimary);
    }

    private void AppendColored(string text, Color color)
    {
        output.SelectionStart = output.TextLength;
        output.SelectionLength = 0;
        output.SelectionColor = color;
        output.SelectedText = text;
    }

    private static string LevelLabel(ActivityLevel level) => level switch
    {
        ActivityLevel.Success => "THÀNH CÔNG",
        ActivityLevel.Warning => "CẢNH BÁO",
        ActivityLevel.Error => "LỖI",
        ActivityLevel.Process => "ĐANG XỬ LÝ",
        _ => "THÔNG TIN"
    };

    private static Color LevelColor(ActivityLevel level) => level switch
    {
        ActivityLevel.Success => UiTheme.Success,
        ActivityLevel.Warning => UiTheme.Warning,
        ActivityLevel.Error => UiTheme.Danger,
        ActivityLevel.Process => UiTheme.Process,
        _ => UiTheme.Accent
    };

    private sealed record LogEntry(DateTime Time, ActivityLevel Level, string Message);
}
