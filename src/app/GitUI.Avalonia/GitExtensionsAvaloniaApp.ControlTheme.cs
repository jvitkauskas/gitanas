using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia;

/// <summary>The Modern appearance, built on Fluent controls with compact density.</summary>
public partial class GitExtensionsAvaloniaApp
{
    private bool _hasControlTheme;

    /// <summary>Adds the base controls before application styles, then the Modern overrides.</summary>
    private void AddControlTheme()
    {
        IStyle[] styles =
        [
            new FluentTheme { DensityStyle = DensityStyle.Compact },
            Include("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml"),
            Include("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"),
            Include("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml"),
            Include("avares://GitUI.Avalonia/Themes/Fluent.axaml"),
        ];
        for (int i = 0; i < styles.Length; i++)
        {
            Styles.Insert(i, styles[i]);
        }

        Styles.Add(Include("avares://GitUI.Avalonia/Themes/Modern.axaml"));
        ModernIconLoader.Install();

        static StyleInclude Include(string source)
            => new((Uri?)null) { Source = new Uri(source) };
    }
}
