using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AutoClickSelector.UI.Themes;

namespace AutoClickSelector.UI.Components;

public sealed class SectionCard : TableLayoutPanel
{
    private const int CornerRadius = 12;
    private const int DefaultGap = 8;

    public SectionCard(string title)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        ColumnCount = 1;
        RowCount = 0;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(18);
        Margin = new Padding(0);
        BackColor = UiTheme.Background;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Text = title,
            Tag = title,
            Font = UiTheme.Heading,
            ForeColor = UiTheme.TextPrimary,
            BackColor = Color.Transparent,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        AddRowInternal(titleLabel, SizeType.AutoSize, 0);
    }

    public void AddRow(Control control, int bottomGap = DefaultGap)
    {
        control.Margin = new Padding(0, 0, 0, bottomGap);

        if (control is Label)
        {
            AddRowInternal(control, SizeType.AutoSize, 0);
            return;
        }

        control.Dock = DockStyle.Fill;
        int height = control.GetPreferredSize(Size.Empty).Height;
        int row = RowCount;
        RowCount = row + 1;
        RowStyles.Add(height > 0
            ? new RowStyle(SizeType.Absolute, height + bottomGap)
            : new RowStyle(SizeType.AutoSize));
        Controls.Add(control, 0, row);
    }

    public void AddInline(params Control[] controls)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = controls.Length,
            RowCount = 1,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 0, 0, DefaultGap)
        };

        float weight = 100f / controls.Length;
        for (int index = 0; index < controls.Length; index++)
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, weight));
        row.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        for (int index = 0; index < controls.Length; index++)
        {
            Control control = controls[index];
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            control.Margin = new Padding(0, 0, index < controls.Length - 1 ? DefaultGap : 0, 0);
            row.Controls.Add(control, index, 0);
        }

        AddRowInternal(row, SizeType.AutoSize, 0);
    }

    public void AddFill(Control control)
    {
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0);
        AddRowInternal(control, SizeType.Percent, 100);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1f, ClientSize.Height - 1f);
        using GraphicsPath path = UiDrawing.RoundedRectangle(rect, CornerRadius);
        using var fill = new SolidBrush(UiTheme.Surface);
        using var border = new Pen(UiTheme.Border);

        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    private void AddRowInternal(Control control, SizeType sizeType, float percent)
    {
        int row = RowCount;
        RowCount = row + 1;
        RowStyles.Add(new RowStyle(sizeType, percent));
        Controls.Add(control, 0, row);
    }
}
