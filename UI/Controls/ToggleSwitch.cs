using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Controls;

/// <summary>Công tắc bật/tắt tự vẽ, nhãn nằm bên phải.</summary>
public sealed class ToggleSwitch : Control
{
    private const int TrackWidth = 42;
    private const int TrackHeight = 22;
    private const int Gap = 10;

    private bool isOn;

    public ToggleSwitch()
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
        BackColor = UiTheme.Surface;
        ForeColor = UiTheme.TextPrimary;
        TabStop = true;
        Size = new Size(170, 28);
        MinimumSize = new Size(0, 28);
    }

    public event EventHandler? CheckedChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => isOn;
        set
        {
            if (isOn == value)
                return;

            isOn = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text = TextRenderer.MeasureText(Text, Font);
        return new Size(TrackWidth + Gap + text.Width + 4, 28);
    }

    public void RefreshTheme() => Invalidate();

    protected override void OnPaint(PaintEventArgs pevent)
    {
        Graphics graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        float top = (Height - TrackHeight) / 2f;
        var track = new RectangleF(0.5f, top, TrackWidth, TrackHeight);

        Color trackColor = Checked && Enabled ? UiTheme.Accent : UiTheme.Border;
        if (!Enabled)
            trackColor = Palette.Mix(trackColor, BackColor, 0.5f);

        using (GraphicsPath path = UiDrawing.RoundedRectangle(track, TrackHeight / 2f))
        using (var brush = new SolidBrush(trackColor))
        {
            graphics.FillPath(brush, path);
        }

        const float knobInset = 3f;
        float knobSize = TrackHeight - knobInset * 2;
        float knobX = Checked ? track.Right - knobSize - knobInset : track.X + knobInset;
        Color knobColor = Checked ? UiTheme.OnAccent : UiTheme.Surface;

        using (var knob = new SolidBrush(knobColor))
        {
            graphics.FillEllipse(knob, knobX, top + knobInset, knobSize, knobSize);
        }

        var labelRect = new Rectangle(TrackWidth + Gap, 0, Math.Max(0, Width - TrackWidth - Gap), Height);
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            labelRect,
            Enabled ? UiTheme.TextPrimary : UiTheme.TextMuted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (Enabled)
            Checked = !Checked;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Enabled && e.KeyCode == Keys.Space)
            Checked = !Checked;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }
}
