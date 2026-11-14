using System.Drawing;

namespace AutoClickSelector.UI.Themes;

public sealed record Palette(
    Color Background,
    Color Surface,
    Color SurfaceAlt,
    Color SurfaceHover,
    Color Border,
    Color TextPrimary,
    Color TextMuted,
    Color Accent,
    Color OnAccent,
    Color Success,
    Color Warning,
    Color Danger,
    Color Process)
{
    public static readonly Palette Light = new(
        Background: Color.FromArgb(243, 245, 249),
        Surface: Color.FromArgb(255, 255, 255),
        SurfaceAlt: Color.FromArgb(246, 248, 252),
        SurfaceHover: Color.FromArgb(232, 238, 248),
        Border: Color.FromArgb(217, 222, 232),
        TextPrimary: Color.FromArgb(31, 41, 55),
        TextMuted: Color.FromArgb(107, 114, 128),
        Accent: Color.FromArgb(37, 99, 235),
        OnAccent: Color.FromArgb(255, 255, 255),
        Success: Color.FromArgb(22, 163, 74),
        Warning: Color.FromArgb(202, 111, 4),
        Danger: Color.FromArgb(220, 38, 38),
        Process: Color.FromArgb(124, 58, 237));

    public static readonly Palette Dark = new(
        Background: Color.FromArgb(17, 19, 24),
        Surface: Color.FromArgb(30, 33, 40),
        SurfaceAlt: Color.FromArgb(38, 42, 51),
        SurfaceHover: Color.FromArgb(48, 53, 64),
        Border: Color.FromArgb(58, 64, 76),
        TextPrimary: Color.FromArgb(229, 231, 235),
        TextMuted: Color.FromArgb(156, 163, 175),
        Accent: Color.FromArgb(96, 165, 250),
        OnAccent: Color.FromArgb(11, 18, 32),
        Success: Color.FromArgb(74, 222, 128),
        Warning: Color.FromArgb(251, 191, 36),
        Danger: Color.FromArgb(248, 113, 113),
        Process: Color.FromArgb(167, 139, 250));

    private Color[] Slots => new[]
    {
        Background, Surface, SurfaceAlt, SurfaceHover, Border,
        TextPrimary, TextMuted, Accent, OnAccent, Success, Warning, Danger, Process
    };

    public static Color Translate(Color color, Palette from, Palette to)
    {
        Color[] source = from.Slots;
        Color[] target = to.Slots;

        for (int index = 0; index < source.Length; index++)
        {
            if (source[index] == color)
                return target[index];
        }

        return color;
    }

    public static Palette Lerp(Palette a, Palette b, float t) => new(
        Mix(a.Background, b.Background, t),
        Mix(a.Surface, b.Surface, t),
        Mix(a.SurfaceAlt, b.SurfaceAlt, t),
        Mix(a.SurfaceHover, b.SurfaceHover, t),
        Mix(a.Border, b.Border, t),
        Mix(a.TextPrimary, b.TextPrimary, t),
        Mix(a.TextMuted, b.TextMuted, t),
        Mix(a.Accent, b.Accent, t),
        Mix(a.OnAccent, b.OnAccent, t),
        Mix(a.Success, b.Success, t),
        Mix(a.Warning, b.Warning, t),
        Mix(a.Danger, b.Danger, t),
        Mix(a.Process, b.Process, t));

    public static Color Mix(Color a, Color b, float t) => Color.FromArgb(
        (int)Math.Round(a.A + (b.A - a.A) * t),
        (int)Math.Round(a.R + (b.R - a.R) * t),
        (int)Math.Round(a.G + (b.G - a.G) * t),
        (int)Math.Round(a.B + (b.B - a.B) * t));
}
