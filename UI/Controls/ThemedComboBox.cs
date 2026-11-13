using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutoClickSelector.UI.Components;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Controls;

public sealed class ThemedComboBox : Control
{
    internal const int PopupInset = 4;

    private const int CornerRadius = 8;
    private const int ArrowAreaWidth = 28;
    private const int ItemHeight = 26;
    private const int MaxVisibleItems = 5;
    private const int ReopenGuardMilliseconds = 200;

    private readonly List<object> items = new();
    private int selectedIndex = -1;
    private ComboPopup? popup;
    private long lastPopupClosedAt;

    public ThemedComboBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);

        Cursor = Cursors.Hand;
        Font = UiTheme.Body;
        BackColor = UiTheme.SurfaceAlt;
        ForeColor = UiTheme.TextPrimary;
        TabStop = true;
        Height = 32;
    }

    public event EventHandler? SelectedIndexChanged;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public List<object> Items => items;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            int next = value < 0 || value >= items.Count ? -1 : value;
            if (next == selectedIndex)
                return;

            selectedIndex = next;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public object? SelectedItem
    {
        get => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;
        set => SelectedIndex = value == null ? -1 : items.IndexOf(value);
    }

    public override Size GetPreferredSize(Size proposedSize) => new(120, 32);

    public void RefreshTheme() => Invalidate();

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnPaintBackground(PaintEventArgs e) =>
        UiTheme.ClearBackdrop(this, e.Graphics);

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (GraphicsPath path = UiDrawing.RoundedRectangle(rect, CornerRadius))
        {
            Color fill = Enabled ? UiTheme.SurfaceAlt : Palette.Mix(UiTheme.SurfaceAlt, UiTheme.Surface, 0.5f);
            Color border = Focused || popup != null ? UiTheme.Accent : UiTheme.Border;
            using (var brush = new SolidBrush(fill))
                g.FillPath(brush, path);
            using (var pen = new Pen(border))
                g.DrawPath(pen, path);
        }

        Color textColor = Enabled ? UiTheme.TextPrimary : UiTheme.TextMuted;
        var textRect = new Rectangle(10, 0, Width - ArrowAreaWidth - 14, Height);
        TextRenderer.DrawText(
            g,
            GetItemText(SelectedItem),
            Font,
            textRect,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

        float cx = Width - ArrowAreaWidth / 2f - 2;
        float cy = Height / 2f;
        PointF[] arrow =
        {
            new(cx - 5, cy - 2.5f),
            new(cx + 5, cy - 2.5f),
            new(cx, cy + 2.5f)
        };
        using var arrowBrush = new SolidBrush(textColor);
        g.FillPolygon(arrowBrush, arrow);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button != MouseButtons.Left || !Enabled)
            return;

        if (popup != null)
        {
            popup.Close();
            return;
        }

        if (Environment.TickCount64 - lastPopupClosedAt < ReopenGuardMilliseconds)
            return;

        OpenPopup();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Up:
                if (items.Count > 0)
                    SelectedIndex = Math.Max(0, selectedIndex - 1);
                e.Handled = true;
                break;
            case Keys.Down:
                if (items.Count > 0)
                    SelectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
                e.Handled = true;
                break;
            case Keys.Enter:
            case Keys.Space:
            case Keys.F4:
                if (popup != null) popup.Close();
                else OpenPopup();
                e.Handled = true;
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            popup?.Dispose();
        base.Dispose(disposing);
    }

    private static string GetItemText(object? item) => item?.ToString() ?? string.Empty;

    private void OpenPopup()
    {
        if (items.Count == 0 || !Enabled)
            return;

        int visible = Math.Min(items.Count, MaxVisibleItems);
        var newPopup = new ComboPopup(
            items,
            GetItemText,
            selectedIndex,
            ItemHeight,
            index => SelectedIndex = index)
        {
            Size = new Size(Width, visible * ItemHeight + PopupInset * 2),
            Location = PointToScreen(new Point(0, Height + 2))
        };

        newPopup.FormClosed += (_, _) =>
        {
            WheelRouter.ClearModal(newPopup);
            popup = null;
            lastPopupClosedAt = Environment.TickCount64;
            Invalidate();
        };

        popup = newPopup;

        WheelRouter.SetModal(newPopup, newPopup.HandleWheel);
        Invalidate();
        newPopup.Show(FindForm());
    }
}

internal sealed class ComboPopup : Form
{
    private const int ScrollBarWidth = 10;

    private readonly IReadOnlyList<object> items;
    private readonly Func<object?, string> textOf;
    private readonly int selectedIndex;
    private readonly int rowHeight;
    private readonly Action<int> onPick;
    private readonly ThemedScrollBar scrollBar;
    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 15 };
    private int hoverIndex = -1;
    private int targetHeight;
    private long animationStartedAt;

    private const int AnimationMilliseconds = 150;

    public ComboPopup(
        IReadOnlyList<object> items,
        Func<object?, string> textOf,
        int selectedIndex,
        int rowHeight,
        Action<int> onPick)
    {
        this.items = items;
        this.textOf = textOf;
        this.selectedIndex = selectedIndex;
        this.rowHeight = rowHeight;
        this.onPick = onPick;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        Font = UiTheme.Body;
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        };

        scrollBar = new ThemedScrollBar { Dock = DockStyle.Right, Width = ScrollBarWidth };
        scrollBar.ValueChanged += (_, _) => ClearHoverAndRedraw();
        scrollBar.MouseEnter += (_, _) => ClearHoverAndRedraw();
        Controls.Add(scrollBar);
    }

    private int ContentHeight => items.Count * rowHeight + ThemedComboBox.PopupInset * 2;

    public void HandleWheel(int delta) => scrollBar.ScrollBy(-delta / 120 * rowHeight);

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        scrollBar.SetRange(ContentHeight, ClientSize.Height);
        Invalidate();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (selectedIndex >= 0)
        {
            scrollBar.SetValueSilently(selectedIndex * rowHeight);
            Invalidate();
        }

        animationStartedAt = Environment.TickCount64;
        animationTimer.Tick += (_, _) => StepAnimation();
        animationTimer.Start();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        targetHeight = Height;
        Height = 1;
        Opacity = 0.15;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            animationTimer.Dispose();

        base.Dispose(disposing);
    }

    private void StepAnimation()
    {
        float progress = Math.Clamp(
            (Environment.TickCount64 - animationStartedAt) / (float)AnimationMilliseconds,
            0f,
            1f);

        float eased = 1f - MathF.Pow(1f - progress, 3f);

        Height = Math.Max(1, (int)Math.Round(targetHeight * eased));
        Opacity = 0.15 + 0.85 * eased;

        if (progress < 1f)
            return;

        animationTimer.Stop();
        Height = targetHeight;
        Opacity = 1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(UiTheme.Surface);

        int offset = scrollBar.Value;
        int rowWidth = ClientSize.Width - scrollBar.Width - ThemedComboBox.PopupInset * 2;

        for (int index = 0; index < items.Count; index++)
        {
            int y = ThemedComboBox.PopupInset + index * rowHeight - offset;
            if (y + rowHeight < 0 || y > ClientSize.Height)
                continue;

            var row = new Rectangle(ThemedComboBox.PopupInset, y, rowWidth, rowHeight);
            if (index == hoverIndex)
            {
                using GraphicsPath path = UiDrawing.RoundedRectangle(row, 6);
                using var hover = new SolidBrush(UiTheme.SurfaceHover);
                g.FillPath(hover, path);
            }

            Color color = index == selectedIndex ? UiTheme.Accent : UiTheme.TextPrimary;
            var textRect = new Rectangle(row.X + 10, row.Y, row.Width - 14, row.Height);
            TextRenderer.DrawText(
                g,
                textOf(items[index]),
                Font,
                textRect,
                color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        using var border = new Pen(UiTheme.Border);
        g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int index = IndexAt(e.Location);
        if (index != hoverIndex)
        {
            hoverIndex = index;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        ClearHoverAndRedraw();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || hoverIndex < 0)
            return;

        int picked = hoverIndex;
        Close();
        onPick(picked);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Close();
    }

    private int IndexAt(Point point)
    {
        if (point.X < 0 || point.X >= ClientSize.Width - scrollBar.Width)
            return -1;
        if (point.Y < ThemedComboBox.PopupInset)
            return -1;

        int y = point.Y - ThemedComboBox.PopupInset + scrollBar.Value;
        int index = y / rowHeight;
        return index < items.Count ? index : -1;
    }

    private void ClearHoverAndRedraw()
    {
        hoverIndex = -1;
        Invalidate();
    }
}
