using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Controls;

/// <summary>
/// Thanh cuộn dọc tự vẽ theo bảng màu. Không gắn trực tiếp vào control nào:
/// chủ sở hữu gọi SetRange khi nội dung đổi và đọc Value để cuộn.
/// </summary>
public sealed class ThemedScrollBar : Control
{
    private const int MinThumbHeight = 28;

    private int total;
    private int viewport = 1;
    private int value;
    private bool dragging;
    private bool hoverThumb;
    private int dragOffset;

    public ThemedScrollBar()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        Width = 10;
        BackColor = UiTheme.Surface;
    }

    public event EventHandler? ValueChanged;

    public bool IsDragging => dragging;

    public int Value => value;

    public int MaxValue => Math.Max(0, total - viewport);

    private bool Scrollable => MaxValue > 0;

    /// <summary>Đặt tổng chiều dài nội dung và chiều dài vùng nhìn thấy (cùng đơn vị).</summary>
    public void SetRange(int totalSize, int viewportSize)
    {
        total = Math.Max(0, totalSize);
        viewport = Math.Max(1, viewportSize);
        value = Math.Clamp(value, 0, MaxValue);
        Invalidate();
    }

    /// <summary>Cập nhật vị trí mà không phát sự kiện (dùng khi đồng bộ với control khác).</summary>
    public void SetValueSilently(int newValue)
    {
        value = Math.Clamp(newValue, 0, MaxValue);
        Invalidate();
    }

    public void ScrollBy(int delta) => SetValue(value + delta);

    public void ScrollToEnd() => SetValue(MaxValue);

    private void SetValue(int newValue)
    {
        int clamped = Math.Clamp(newValue, 0, MaxValue);
        if (clamped == value)
            return;

        value = clamped;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private Rectangle ThumbRect()
    {
        if (!Scrollable)
            return Rectangle.Empty;

        int track = Height;
        int length = Math.Clamp((int)((long)track * viewport / total), MinThumbHeight, track);
        int top = (int)((track - length) * (value / (double)MaxValue));
        return new Rectangle(1, top, Math.Max(1, Width - 2), length);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(UiTheme.Surface);

        if (!Scrollable)
            return;

        var rail = new RectangleF(1, 0, Width - 2, Height);
        using (GraphicsPath railPath = UiDrawing.RoundedRectangle(rail, (Width - 2) / 2f))
        using (var railBrush = new SolidBrush(UiTheme.SurfaceAlt))
        {
            g.FillPath(railBrush, railPath);
        }

        Rectangle thumb = ThumbRect();
        Color color = dragging ? UiTheme.Accent : hoverThumb ? UiTheme.TextMuted : UiTheme.Border;
        using GraphicsPath thumbPath = UiDrawing.RoundedRectangle(thumb, thumb.Width / 2f);
        using var thumbBrush = new SolidBrush(color);
        g.FillPath(thumbBrush, thumbPath);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Scrollable || e.Button != MouseButtons.Left)
            return;

        Rectangle thumb = ThumbRect();
        if (thumb.Contains(e.Location))
        {
            dragging = true;
            dragOffset = e.Y - thumb.Top;
            Capture = true;
        }
        else
        {
            // Bấm vào rãnh: nhảy một trang về phía đã bấm.
            SetValue(value + (e.Y < thumb.Top ? -viewport : viewport));
        }

        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hover = ThumbRect().Contains(e.Location);
        if (hover != hoverThumb)
        {
            hoverThumb = hover;
            Invalidate();
        }

        if (!dragging)
            return;

        int range = Height - ThumbRect().Height;
        if (range <= 0)
            return;

        int top = Math.Clamp(e.Y - dragOffset, 0, range);
        SetValue((int)Math.Round(top / (double)range * MaxValue));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        dragging = false;
        Capture = false;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hoverThumb = false;
        Invalidate();
    }
}
