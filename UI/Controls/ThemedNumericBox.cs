using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Controls;

/// <summary>Ô số tự vẽ: nhập trực tiếp, hai nút tăng/giảm (giữ để lặp), mũi tên bàn phím.</summary>
public sealed class ThemedNumericBox : Control
{
    private const int CornerRadius = 8;
    private const int ButtonAreaWidth = 24;
    private const int TextInset = 10;
    private const int InitialRepeatMilliseconds = 400;
    private const int RepeatMilliseconds = 60;

    private readonly TextBox input;
    private readonly System.Windows.Forms.Timer repeatTimer = new() { Interval = InitialRepeatMilliseconds };
    private decimal minimum;
    private decimal maximum = 100m;
    private decimal value;
    private decimal increment = 1m;
    private int decimalPlaces;
    private int repeatDirection;

    public ThemedNumericBox()
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
            Font = UiTheme.Body,
            TextAlign = HorizontalAlignment.Left
        };
        input.Leave += (_, _) => CommitText();
        input.KeyDown += Input_KeyDown;
        Controls.Add(input);

        repeatTimer.Tick += (_, _) =>
        {
            Step(repeatDirection);
            repeatTimer.Interval = RepeatMilliseconds;
        };

        input.Text = FormatValue(value);
        LayoutInput();
    }

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Minimum
    {
        get => minimum;
        set
        {
            minimum = value;
            if (maximum < minimum)
                maximum = minimum;
            SetValueInternal(this.value, raise: false);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Maximum
    {
        get => maximum;
        set
        {
            maximum = value;
            if (minimum > maximum)
                minimum = maximum;
            SetValueInternal(this.value, raise: false);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Increment
    {
        get => increment;
        set => increment = Math.Max(0m, value);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DecimalPlaces
    {
        get => decimalPlaces;
        set
        {
            decimalPlaces = Math.Clamp(value, 0, 28);
            SetValueInternal(this.value, raise: false);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Value
    {
        get => value;
        set => SetValueInternal(value, raise: true);
    }

    public override Size GetPreferredSize(Size proposedSize) => new(120, 32);

    public void RefreshTheme() => Invalidate();

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

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(UiTheme.Surface);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (GraphicsPath path = UiDrawing.RoundedRectangle(rect, CornerRadius))
        {
            using (var fill = new SolidBrush(UiTheme.SurfaceAlt))
                g.FillPath(fill, path);
            using (var pen = new Pen(input.Focused ? UiTheme.Accent : UiTheme.Border))
                g.DrawPath(pen, path);
        }

        int divider = Width - ButtonAreaWidth;
        using (var divPen = new Pen(UiTheme.Border))
            g.DrawLine(divPen, divider, 8, divider, Height - 8);

        Color arrowColor = Enabled ? UiTheme.TextMuted : Palette.Mix(UiTheme.TextMuted, UiTheme.SurfaceAlt, 0.5f);
        float cx = divider + ButtonAreaWidth / 2f;
        float upCy = Height * 0.32f;
        float downCy = Height * 0.68f;
        using var arrowBrush = new SolidBrush(arrowColor);
        g.FillPolygon(arrowBrush, new[]
        {
            new PointF(cx - 4, upCy + 2),
            new PointF(cx + 4, upCy + 2),
            new PointF(cx, upCy - 2)
        });
        g.FillPolygon(arrowBrush, new[]
        {
            new PointF(cx - 4, downCy - 2),
            new PointF(cx + 4, downCy - 2),
            new PointF(cx, downCy + 2)
        });
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        input.Focus();

        if (e.Button != MouseButtons.Left || !Enabled || e.X < Width - ButtonAreaWidth)
            return;

        int direction = e.Y < Height / 2 ? 1 : -1;
        CommitText();
        Step(direction);

        repeatDirection = direction;
        repeatTimer.Interval = InitialRepeatMilliseconds;
        repeatTimer.Start();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        StopRepeat();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        StopRepeat();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            repeatTimer.Dispose();
        base.Dispose(disposing);
    }

    private void LayoutInput()
    {
        int height = input.PreferredHeight;
        int width = Math.Max(0, Width - TextInset - ButtonAreaWidth - 4);
        input.SetBounds(TextInset, Math.Max(0, (Height - height) / 2), width, height);
    }

    private void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
                CommitText();
                Step(1);
                e.SuppressKeyPress = true;
                break;
            case Keys.Down:
                CommitText();
                Step(-1);
                e.SuppressKeyPress = true;
                break;
            case Keys.Enter:
                CommitText();
                e.SuppressKeyPress = true;
                break;
        }
    }

    private void Step(int direction)
    {
        if (direction == 0)
            return;

        SetValueInternal(value + direction * increment, raise: true);
    }

    private void StopRepeat()
    {
        repeatDirection = 0;
        repeatTimer.Stop();
    }

    /// <summary>Đọc lại chữ người dùng vừa gõ; nếu không hợp lệ thì khôi phục giá trị cũ.</summary>
    private void CommitText()
    {
        if (decimal.TryParse(input.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal parsed))
            SetValueInternal(parsed, raise: true);
        else
            input.Text = FormatValue(value);
    }

    private void SetValueInternal(decimal candidate, bool raise)
    {
        decimal rounded = Math.Round(candidate, decimalPlaces);
        decimal clamped = Math.Clamp(rounded, minimum, maximum);
        bool changed = clamped != value;

        value = clamped;
        input.Text = FormatValue(value);

        if (changed && raise)
            ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private string FormatValue(decimal number) =>
        number.ToString("F" + decimalPlaces, CultureInfo.CurrentCulture);
}
