using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Controls;

public sealed class RoundedTextBox : Control
{
    private const int CornerRadius = 8;
    private const int TextInset = 10;

    private readonly TextBox input;

    public RoundedTextBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = UiTheme.SurfaceAlt;
        ForeColor = UiTheme.TextPrimary;
        Font = UiTheme.Body;

        input = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.Body
        };
        input.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
        input.GotFocus += (_, _) => Invalidate();
        input.LostFocus += (_, _) => Invalidate();
        Controls.Add(input);
    }

    public override string Text
    {
        get => input.Text;
        set => input.Text = value ?? string.Empty;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PlaceholderText
    {
        get => input.PlaceholderText;
        set => input.PlaceholderText = value;
    }

    public void Clear() => input.Clear();

    public override Size GetPreferredSize(Size proposedSize) => new(120, 32);

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutInput();
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        input.Enabled = Enabled;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        input.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(UiTheme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using GraphicsPath path = UiDrawing.RoundedRectangle(rect, CornerRadius);
        using (var fill = new SolidBrush(UiTheme.SurfaceAlt))
            g.FillPath(fill, path);
        using (var pen = new Pen(input.Focused ? UiTheme.Accent : UiTheme.Border))
            g.DrawPath(pen, path);
    }

    private void LayoutInput()
    {
        int height = input.PreferredHeight;
        input.SetBounds(
            TextInset,
            Math.Max(0, (Height - height) / 2),
            Math.Max(0, Width - TextInset * 2),
            height);
    }
}
