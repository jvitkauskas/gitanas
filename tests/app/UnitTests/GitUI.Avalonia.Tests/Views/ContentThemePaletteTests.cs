using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

[TestFixture]
public sealed class ContentThemePaletteTests : HeadlessTest
{
    [TestCase(false)]
    [TestCase(true)]
    public Task Input_hints_remain_readable_in_the_control_template(bool dark) => OnUiThreadAsync(() =>
    {
        GitExtensionsAvaloniaApp app = (GitExtensionsAvaloniaApp)Application.Current!;
        TextBox input = new() { PlaceholderText = "Filter files" };
        Window window = new() { Content = input, Width = 300, Height = 100 };
        try
        {
            app.ApplyOptions(new AvaloniaUiOptions(dark, "Segoe UI", 12));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            TextBlock placeholder = input.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Placeholder");
            placeholder.Opacity.Should().Be(1.0, "the theme already mutes the hint color");
            Color foreground = ((SolidColorBrush)placeholder.Foreground!).Color;
            Color background = ((SolidColorBrush)input.Background!).Color;
            double light = Luminance(foreground);
            double surface = Luminance(background);
            ((Math.Max(light, surface) + 0.05) / (Math.Min(light, surface) + 0.05)).Should().BeGreaterThanOrEqualTo(4.5);
        }
        finally
        {
            window.Close();
            app.ApplyOptions(new AvaloniaUiOptions(false, "Segoe UI", 12));
        }

        static double Luminance(Color color)
        {
            return (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

            static double Linear(byte value)
            {
                double channel = value / 255.0;
                return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
            }
        }
    });

    [TestCase(true, 0xFF0000FFu, 0xFF202020u, 0xFFFFFFFFu)]
    [TestCase(false, 0xFF0000FFu, 0xFFFFFFFFu, 0xFF0000FFu)]
    [TestCase(true, 0xFF99CFFFu, 0xFF202020u, 0xFF99CFFFu)]
    [TestCase(false, 0xFFDDDDDDu, 0xFFFFFFFFu, 0xFF000000u)]
    public void Links_preserve_readable_custom_colors_and_replace_low_contrast_ones(bool dark, uint link, uint background, uint expected)
    {
        ResourceDictionary palette = ContentThemePalette.Create(dark, new Dictionary<string, uint>
        {
            [ThemeColors.HotTrack] = link,
            [ThemeColors.Window] = background,
            [ThemeColors.Control] = background,
        });

        ((SolidColorBrush)palette["SystemControlHyperlinkTextBrush"]!).Color.Should().Be(Color.FromUInt32(expected));
        ((SolidColorBrush)palette["SystemControlHyperlinkBaseMediumBrush"]!).Color.Should().Be(Color.FromUInt32(expected));
    }
}
