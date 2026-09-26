using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Classic.Avalonia.Theme;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia;

/// <summary>
///  The Avalonia application object. While WinForms owns the process (docs/avalonia-port/PLAN.md, phases 1-6)
///  it is set up without starting a lifetime, see <see cref="AvaloniaUi"/>.
/// </summary>
public partial class GitExtensionsAvaloniaApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        if (OperatingSystem.IsMacOS())
        {
            MacOSApplicationMenu.Install(this);
        }
    }

    /// <summary>
    ///  Applies the Git Extensions appearance settings (theme, colors and font) to all Avalonia windows.
    /// </summary>
    internal void ApplyOptions(AvaloniaUiOptions options)
    {
        // The control theme is set once, before the first window: another one needs a restart.
        if (!_hasControlTheme)
        {
            _hasControlTheme = true;
            AddControlTheme(ResolveControlTheme(options.ControlTheme));
        }

        ThemeVariant variant = options.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        RequestedThemeVariant = variant;

        if (_usesFluentControls && Styles.OfType<FluentTheme>().FirstOrDefault() is { } fluentControls)
        {
            foreach ((object key, object? value) in FluentThemePalette.Create(options.IsDarkTheme, options.Colors))
            {
                fluentControls.Resources[key] = value;
            }
        }

        if (Styles.OfType<SimpleTheme>().FirstOrDefault() is { } simpleTheme)
        {
            // Simple also has non-variant accent resources: replace those at the same level as its palette.
            foreach ((object key, object? value) in SimpleThemePalette.Create(options.IsDarkTheme, options.Colors))
            {
                simpleTheme.Resources[key] = value;
            }
        }

        if (Styles.OfType<ClassicTheme>().FirstOrDefault() is { } classicTheme)
        {
            foreach ((object key, object? value) in ClassicThemePalette.Create(options.IsDarkTheme, options.Colors))
            {
                classicTheme.Resources[key] = value;
            }

            if (options.FontSize > 0)
            {
                classicTheme.Resources["FontSizeNormal"] = options.FontSize;
            }
        }

        if (!string.IsNullOrWhiteSpace(options.FontFamily))
        {
            Resources["ContentControlThemeFontFamily"] = new FontFamily(options.FontFamily);
        }

        if (!string.IsNullOrWhiteSpace(options.MonospaceFontFamily))
        {
            Resources["MonospaceFontFamily"] = new FontFamily(options.MonospaceFontFamily);
        }

        if (!string.IsNullOrWhiteSpace(options.EditorFontFamily))
        {
            Resources["EditorFontFamily"] = new FontFamily(options.EditorFontFamily);
        }

        if (options.EditorFontSize > 0)
        {
            Resources["EditorFontSize"] = options.EditorFontSize;
        }

        if (!string.IsNullOrWhiteSpace(options.CommitFontFamily))
        {
            Resources["CommitFontFamily"] = new FontFamily(options.CommitFontFamily);
        }

        if (options.CommitFontSize > 0)
        {
            Resources["CommitFontSize"] = options.CommitFontSize;
        }

        if (options.FontSize > 0)
        {
            // Fluent sizes most controls from this resource.
            Resources["ControlContentThemeFontSize"] = options.FontSize;

            // As RevisionDataGridView.UpdateRowHeight: the height of a line of the font and 9 pixels.
            Resources["RevisionGridRowHeight"] = GetRevisionGridRowHeight(options.FontSize);
        }

        foreach ((object key, object? value) in ContentThemePalette.Create(options.IsDarkTheme, options.Colors))
        {
            Resources[key] = value;
        }

        if (options.Colors is { } colors)
        {
            ApplyColors(variant, colors);
        }
    }

    /// <summary>The height of the rows of the revision grid for the font size (a line of about 1.25 times it, and 9 pixels).</summary>
    internal static double GetRevisionGridRowHeight(double fontSize) => Math.Ceiling(fontSize * 1.25) + 9;

    private void ApplyColors(ThemeVariant variant, IReadOnlyDictionary<string, uint> colors)
    {
        // The Git Extensions theme drives Fluent's palette, so that Avalonia dialogs match the WinForms ones
        // (e.g. the gray dialog background of the default theme, or a custom theme's colors).
        ColorPaletteResources palette = new();
        if (TryGetColor(ThemeColors.Control, out Color region))
        {
            palette.RegionColor = region;
        }

        if (TryGetColor(ThemeColors.Highlight, out Color accent))
        {
            palette.Accent = accent;
        }

        if (Styles.OfType<FluentTheme>().FirstOrDefault() is { } fluentTheme)
        {
            fluentTheme.Palettes[variant] = palette;
        }

        // SystemColors.Window and Control of the theme, for the views that use them as WinForms did (e.g. the commit info).
        if (TryGetColor(ThemeColors.Window, out Color window))
        {
            Resources["WindowBrush"] = new SolidColorBrush(window);
        }

        if (TryGetColor(ThemeColors.Control, out Color control))
        {
            Resources["ControlBrush"] = new SolidColorBrush(control);
        }

        // AppColor values (diff colors, graph lanes, ...) for views to use as {DynamicResource AppColor.<name>}.
        foreach ((string key, uint argb) in colors)
        {
            if (key.StartsWith(ThemeColors.AppColorPrefix, StringComparison.Ordinal))
            {
                Resources[key] = new SolidColorBrush(Color.FromUInt32(argb));
            }
        }

        bool TryGetColor(string key, out Color color)
        {
            bool found = colors.TryGetValue(key, out uint argb);
            color = Color.FromUInt32(argb);
            return found;
        }
    }
}
