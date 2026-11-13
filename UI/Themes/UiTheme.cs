using System.Drawing;

namespace AutoClickSelector.UI.Themes;

/// <summary>Trạng thái giao diện hiện tại: bảng màu đang dùng và phông chữ.</summary>
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

    /// <summary>Đổi màu cũ trong "from" thành màu tương ứng của bảng hiện tại.</summary>
    public static Color Remap(Color color, Palette from) => Palette.Translate(color, from, Current);
}
