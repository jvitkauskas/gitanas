using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia;

/// <summary>Simple's brush names differ from Fluent's; keep its surfaces and states in the host theme's palette.</summary>
internal static class SimpleThemePalette
{
    internal static ResourceDictionary Create(bool dark, IReadOnlyDictionary<string, uint>? colors)
    {
        ResourceDictionary resources = new();
        Color surface = Get(ThemeColors.Control, dark ? 0xFF212121 : 0xFFF3F4F6);
        Color foreground = Get(ThemeColors.ControlText, dark ? 0xFFF1F3F5 : 0xFF20242B);
        Color window = Get(ThemeColors.Window, dark ? 0xFF18191B : 0xFFFFFFFF);
        Color accent = Get(ThemeColors.Highlight, dark ? 0xFF2463A6 : 0xFF2165AE);
        Color accentText = Get(ThemeColors.HighlightText, 0xFFFFFFFF);

        // The host can supply black highlight text with a dark blue accent.
        if (Contrast(accent, accentText) < 4.5)
        {
            accentText = Contrast(accent, Colors.White) >= Contrast(accent, Colors.Black) ? Colors.White : Colors.Black;
        }

        Color accentShade = Luminance(accentText) > Luminance(accent) ? Colors.Black : Colors.White;

        Set("ThemeBackground", surface);
        Set("ThemeForeground", foreground);
        Set("ThemeForegroundLow", Mix(surface, foreground, dark ? 0.66 : 0.72));
        Set("ThemeBorderLow", Mix(surface, foreground, 0.16));
        Set("ThemeBorderMid", Mix(surface, foreground, 0.30));
        Set("ThemeBorderHigh", Mix(surface, foreground, 0.48));
        Set("ThemeControlLow", Mix(surface, foreground, 0.04));
        Set("ThemeControlMid", dark ? Mix(surface, foreground, 0.07) : window);
        Set("ThemeControlMidHigh", Mix(surface, foreground, 0.16));
        Set("ThemeControlHigh", Mix(surface, foreground, 0.20));
        Set("ThemeControlVeryHigh", Mix(surface, foreground, 0.48));
        Set("ThemeControlHighlightLow", Mix(surface, foreground, 0.05));
        Set("ThemeControlHighlightMid", Mix(surface, foreground, 0.10));
        Set("ThemeControlHighlightHigh", Mix(surface, foreground, 0.16));
        Set("Highlight", accent);
        Set("HighlightForeground", accentText);

        // Use opaque tints so a hover/selection does not depend on the row's graph or diff background.
        Set("ThemeAccent", Mix(surface, accent, dark ? 0.65 : 0.35));
        Set("ThemeAccent", Mix(surface, accent, dark ? 0.55 : 0.28), "2");
        Set("ThemeAccent", Mix(surface, accent, dark ? 0.42 : 0.20), "3");
        Set("ThemeAccent", Mix(surface, accent, dark ? 0.32 : 0.13), "4");
        Set("Highlight", Mix(accent, accentShade, 0.12), "2");
        Set("SimpleInputBackground", dark ? Mix(window, surface, 0.65) : window);
        Set("SimpleAccentHover", Mix(accent, accentShade, 0.06));
        Set("SimpleFocus", dark ? Mix(accent, foreground, 0.45) : accent);
        return resources;

        Color Get(string key, uint fallback) => Color.FromUInt32(colors is not null && colors.TryGetValue(key, out uint value) ? value : fallback);

        // Some templates request a Color, others a Brush. Override both: Simple's stock brushes use StaticResource.
        void Set(string name, Color color, string suffix = "")
        {
            resources[name + "Color" + suffix] = color;
            resources[name + "Brush" + suffix] = new SolidColorBrush(color);
        }
    }

    private static Color Mix(Color background, Color foreground, double amount)
    {
        return Color.FromRgb(Channel(background.R, foreground.R), Channel(background.G, foreground.G), Channel(background.B, foreground.B));

        byte Channel(byte from, byte to) => (byte)Math.Round(from + ((to - from) * amount));
    }

    /// <summary>Preserves a theme color when readable; otherwise chooses the best contrasting neutral.</summary>
    internal static Color ReadableText(Color preferred, params Color[] backgrounds)
    {
        if (backgrounds.All(background => Contrast(preferred, background) >= 4.5))
        {
            return preferred;
        }

        return backgrounds.Min(background => Contrast(Colors.White, background)) >= backgrounds.Min(background => Contrast(Colors.Black, background))
            ? Colors.White : Colors.Black;
    }

    private static double Contrast(Color first, Color second)
    {
        double a = Luminance(first);
        double b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(Color color)
    {
        return (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

        static double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}
