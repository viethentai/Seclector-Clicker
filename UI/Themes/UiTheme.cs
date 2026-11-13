using System.Drawing;
using System.Windows.Forms;
using AutoClickSelector.UI.Components;

namespace AutoClickSelector.UI.Themes;

public static class UiTheme
{
    public static Palette Current { get; private set; } = Palette.Light;

    public static bool IsDark { get; private set; }

    public static Color Background => Current.Background;
    public static Color Surface => Current.Surface;
    public static Color SurfaceAlt => Current.SurfaceAlt;
    public static Color SurfaceHover => Current.SurfaceHover;
    public static Color Border => Current.Border;
    public static Color TextPrimary => Current.TextPrimary;
    public static Color TextMuted => Current.TextMuted;
    public static Color Accent => Current.Accent;
    public static Color OnAccent => Current.OnAccent;
    public static Color Success => Current.Success;
    public static Color Warning => Current.Warning;
    public static Color Danger => Current.Danger;
    public static Color Process => Current.Process;

    public static readonly Font Title = new("Segoe UI Semibold", 16f);
    public static readonly Font Heading = new("Segoe UI Semibold", 10.5f);
    public static readonly Font Body = new("Segoe UI", 9.5f);
    public static readonly Font Small = new("Segoe UI", 8.5f);

    public static void SetDark(bool dark) => SetPalette(dark ? Palette.Dark : Palette.Light, dark);

    public static void SetPalette(Palette palette, bool dark)
    {
        Current = palette;
        IsDark = dark;
    }

    public static Color Remap(Color color, Palette from) => Palette.Translate(color, from, Current);

    public static Color BackdropOf(Control control)
    {
        Control? current = control.Parent;

        while (current != null)
        {
            if (current is SectionCard)
                return Surface;

            if (current.BackColor != Color.Transparent)
                return current.BackColor;

            current = current.Parent;
        }

        return Background;
    }

    public static void ClearBackdrop(Control control, Graphics graphics) =>
        graphics.Clear(BackdropOf(control));
}
