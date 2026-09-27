using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Avalonia;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

[TestFixture]
public sealed class ModernThemePaletteTests
{
    [TestCase(0xFF0063B1u, 0xFF000000u, 0xFFFFFFFFu)]
    [TestCase(0xFFFFD966u, 0xFFFFFFFFu, 0xFF000000u)]
    [TestCase(0xFF123456u, 0xFFF0F0F0u, 0xFFF0F0F0u)]
    public void Accent_text_is_readable_even_when_the_host_supplies_a_low_contrast_pair(uint background, uint foreground, uint expected)
    {
        ResourceDictionary palette = ModernThemePalette.Create(true, new Dictionary<string, uint>
        {
            [ThemeColors.Highlight] = background,
            [ThemeColors.HighlightText] = foreground,
        });

        ((SolidColorBrush)palette["HighlightBrush"]!).Color.Should().Be(Color.FromUInt32(background));
        ((SolidColorBrush)palette["HighlightForegroundBrush"]!).Color.Should().Be(Color.FromUInt32(expected));
    }
}
