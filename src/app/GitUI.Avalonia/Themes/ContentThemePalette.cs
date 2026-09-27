using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia;

/// <summary>Readable links and input hints on the content surfaces supplied by the host theme.</summary>
internal static class ContentThemePalette
{
    internal static ResourceDictionary Create(bool dark, IReadOnlyDictionary<string, uint>? colors)
    {
        Color window = Get(ThemeColors.Window, dark ? 0xFF202020 : 0xFFFFFFFF);
        Color control = Get(ThemeColors.Control, dark ? 0xFF212121 : 0xFFF0F0F0);
        Color link = ModernThemePalette.ReadableText(Get(ThemeColors.HotTrack, dark ? 0xFF99CFFF : 0xFF0063B1), window, control);
        ResourceDictionary palette = ModernThemePalette.Create(dark, colors);
        Color hint = ModernThemePalette.ReadableText(((SolidColorBrush)palette["ThemeForegroundLowBrush"]!).Color, window);
        return new ResourceDictionary
        {
            ["SystemControlHyperlinkTextBrush"] = new SolidColorBrush(link),
            ["SystemControlHyperlinkBaseMediumBrush"] = new SolidColorBrush(link),
            ["TextControlPlaceholderForeground"] = new SolidColorBrush(hint),
            ["TextControlPlaceholderForegroundPointerOver"] = new SolidColorBrush(hint),
            ["TextControlPlaceholderForegroundFocused"] = new SolidColorBrush(hint),

            // Fluent otherwise halves the opacity of an already muted placeholder color.
            ["TextControlPlaceholderOpacity"] = 1.0,
        };

        Color Get(string key, uint fallback) => Color.FromUInt32(colors is not null && colors.TryGetValue(key, out uint value) ? value : fallback);
    }
}
