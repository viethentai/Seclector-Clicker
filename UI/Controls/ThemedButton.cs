using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Controls;

/// <summary>Kiểu nút: Primary (nhấn mạnh), Secondary (mặc định), Danger (nguy hiểm), Ghost (chỉ chữ).</summary>
public enum ButtonVariant
{
    Secondary,
    Primary,
    Danger,
    Ghost
}

/// <summary>Nút bo góc tự vẽ, đổi màu theo giao diện sáng/tối.</summary>
public sealed class ThemedButton : Button
{
    private const int CornerRadius = 8;
    private const int ButtonHeight = 36;

    private bool hovered;

    public ThemedButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = UiTheme.Body;
        BackColor = UiTheme.Surface;
        ForeColor = UiTheme.TextPrimary;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ButtonVariant Variant { get; set; } = ButtonVariant.Secondary;

    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text = TextRenderer.MeasureText(Text, Font);
        return new Size(Math.Max(96, text.Width + 32), ButtonHeight);
    }

    public void RefreshTheme() => Invalidate();

    protected override void OnPaint(PaintEventArgs pevent)
    {
        Graphics graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        (Color fill, Color border, Color text) = ResolveColors();

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using GraphicsPath path = UiDrawing.RoundedRectangle(rect, CornerRadius);

        if (fill.A > 0)
        {
            using var brush = new SolidBrush(fill);
            graphics.FillPath(brush, path);
        }

        if (border.A > 0)
        {
            using var pen = new Pen(border);
            graphics.DrawPath(pen, path);
        }

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            ClientRectangle,
            text,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hovered = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    private (Color Fill, Color Border, Color Text) ResolveColors()
    {
        (Color fill, Color border, Color text) = Variant switch
        {
            ButtonVariant.Primary => (UiTheme.Accent, Color.Transparent, UiTheme.OnAccent),
            ButtonVariant.Danger => (UiTheme.Danger, Color.Transparent, UiTheme.OnAccent),
            ButtonVariant.Ghost => (hovered ? UiTheme.SurfaceHover : Color.Transparent, Color.Transparent, UiTheme.Accent),
            _ => (hovered ? UiTheme.SurfaceHover : UiTheme.SurfaceAlt, UiTheme.Border, UiTheme.TextPrimary),
        };

        if (!Enabled)
        {
            const float fade = 0.55f;
            if (fill.A > 0) fill = Palette.Mix(fill, BackColor, fade);
            if (border.A > 0) border = Palette.Mix(border, BackColor, fade);
            text = UiTheme.TextMuted;
        }

        return (fill, border, text);
    }
}
