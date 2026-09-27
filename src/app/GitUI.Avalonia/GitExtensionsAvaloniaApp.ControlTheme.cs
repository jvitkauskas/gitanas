using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Classic.Avalonia.Theme;
using GitCommands;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia;

/// <summary>
///  The control theme of the application: Fluent, or a community theme (<c>simple</c>, <c>classic</c>), chosen in the Colors settings (<c>AppSettings.AvaloniaControlTheme</c>) or with the environment variable
///  <c>GE_AVALONIA_THEME</c>. The views are written for Fluent:
///  another theme restyles the controls over Fluent, whose brushes the views still use (e.g. the dialog footers).
///  Simple and Classic also receive palettes derived from the Git Extensions colors.
/// </summary>
public partial class GitExtensionsAvaloniaApp
{
    internal const string ControlThemeVariable = "GE_AVALONIA_THEME";

    private bool _hasControlTheme;
    private bool _usesFluentControls;

    /// <summary>
    ///  Whether the control theme is Modern: the windows use its layout too (the main window's toolbar in the title bar, the
    ///  sidebar, the filters above the history), see <c>BrowseWindow.ApplyModernLayout</c>.
    /// </summary>
    public static bool IsModern { get; internal set; }

    /// <summary>
    ///  The control theme to use: <see cref="ControlThemeVariable"/> when set (to try one without changing the settings), else
    ///  <paramref name="setting"/>, else Fluent (also for an unknown name).
    /// </summary>
    internal static string ResolveControlTheme(string? setting, string? variable = null)
    {
        variable ??= Environment.GetEnvironmentVariable(ControlThemeVariable);
        string? name = (string.IsNullOrWhiteSpace(variable) ? setting : variable)?.Trim().ToLowerInvariant();
        return name is not null && AppSettings.AvaloniaControlThemes.Contains(name) ? name : AppSettings.AvaloniaControlThemes[0];
    }

    /// <summary>
    ///  Adds Fluent (compact) and its styles of the DataGrid, color picker and editor before the styles of the application,
    ///  then the control theme <paramref name="name"/> over them: the community themes restyle the controls, and the
    ///  resources of Fluent that the views use (brushes, the chevron of the trees) stay. In code: the XAML compiler inlines
    ///  the style includes, which could not be replaced.
    /// </summary>
    internal void AddControlTheme(string name)
    {
        _usesFluentControls = name is not ("simple" or "classic");
        IsModern = name == "modern";
        const string colorPickerSimple = "avares://Avalonia.Controls.ColorPicker/Themes/Simple/Simple.xaml";
        const string dataGridSimple = "avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml";
        IStyle[] community = name switch
        {
            "simple" => [new SimpleTheme(), Include(colorPickerSimple), Include(dataGridSimple), Include("avares://GitUI.Avalonia/Themes/Simple.axaml")],
            "classic" => [new ClassicTheme { FontAliasing = false }, Include("avares://Classic.Avalonia.Theme.ColorPicker/Classic.axaml"), Include("avares://Classic.Avalonia.Theme.DataGrid/Classic.axaml"), Include("avares://GitUI.Avalonia/Themes/Classic.axaml")],
            "modern" => [Include("avares://GitUI.Avalonia/Themes/Fluent.axaml")],
            _ => [Include("avares://GitUI.Avalonia/Themes/Fluent.axaml")],
        };

        // Compact density matches the information density of the WinForms UI.
        IStyle[] styles =
        [
            new FluentTheme { DensityStyle = DensityStyle.Compact },
            Include("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml"),
            Include("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"),
            Include("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml"),
            .. community,
        ];
        for (int i = 0; i < styles.Length; i++)
        {
            Styles.Insert(i, styles[i]);
        }

        // Modern restyles the styles of the application too (e.g. the tabs): after them.
        if (IsModern)
        {
            Styles.Add(Include("avares://GitUI.Avalonia/Themes/Modern.axaml"));
            ModernIconLoader.Install();
        }

        static StyleInclude Include(string source)
            => new((Uri?)null) { Source = new Uri(source) };
    }
}
