using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using GitUI.Avalonia;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The Modern appearance is always installed over its Fluent control base.</summary>
[TestFixture]
public sealed class ControlThemeTests : HeadlessTest
{
    [Test]
    public Task Modern_is_always_loaded_and_legacy_theme_overrides_are_ignored() => OnUiThreadAsync(() =>
    {
        string? previous = Environment.GetEnvironmentVariable("GE_AVALONIA_THEME");
        try
        {
            Environment.SetEnvironmentVariable("GE_AVALONIA_THEME", "classic");
            GitExtensionsAvaloniaApp app = (GitExtensionsAvaloniaApp)global::Avalonia.Application.Current!;
            app.ApplyOptions(new AvaloniaUiOptions(false, "Segoe UI", 12));
            app.Styles[0].Should().BeOfType<FluentTheme>();
            app.Styles.OfType<StyleInclude>().Should().ContainSingle(style =>
                style.Source == new Uri("avares://GitUI.Avalonia/Themes/Modern.axaml"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GE_AVALONIA_THEME", previous);
        }
    });
}
