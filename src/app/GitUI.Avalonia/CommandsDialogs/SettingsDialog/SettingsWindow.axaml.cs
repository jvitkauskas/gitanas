using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog;

/// <summary>Avalonia port of <c>FormSettings</c>.</summary>
public partial class SettingsWindow : DialogWindow
{
    public SettingsWindow()
    {
        InitializeComponent();

#if DEBUG
        // As buttonDiscard, shown in debug builds.
        discardButton.IsVisible = true;
#endif

        // As textBoxFind_KeyUp: Enter selects the next page found.
        filter.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && DataContext is SettingsDialogViewModel viewModel)
            {
                viewModel.SelectNextFoundCommand.Execute(null);
                e.Handled = true;
            }
        };
    }

    /// <summary>The tree of the pages, e.g. for tests.</summary>
    public TreeView PagesTree => pagesTree;

    /// <summary>The shown page, e.g. for tests.</summary>
    public ContentControl PageContent => pageContent;
}

/// <summary>
///  The view of a settings page: the page view model <c>XxxViewModel</c> of GitUI.Presentation is shown by the control
///  <c>XxxView</c> of the <c>Pages</c> namespace.
/// </summary>
public sealed class SettingsPageLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Type?> _viewTypes = [];

    public bool Match(object? data) => data is SettingsPageViewModel && GetViewType(data.GetType()) is not null;

    public Control? Build(object? param)
        => param is not null && GetViewType(param.GetType()) is { } viewType ? (Control?)Activator.CreateInstance(viewType) : null;

    private static Type? GetViewType(Type viewModelType)
    {
        if (!_viewTypes.TryGetValue(viewModelType, out Type? viewType))
        {
            string name = viewModelType.Name.EndsWith("ViewModel", StringComparison.Ordinal) ? viewModelType.Name[..^"ViewModel".Length] + "View" : viewModelType.Name + "View";
            viewType = typeof(SettingsPageLocator).Assembly.GetType($"{typeof(SettingsPageLocator).Namespace}.Pages.{name}");
            _viewTypes[viewModelType] = viewType;
        }

        return viewType;
    }
}

/// <summary>Converters of the settings dialog: the icon of a page (an asset name), a bold highlighted page, a visible arrow.</summary>
public sealed class SettingsIconConverter : IValueConverter
{
    private static readonly Dictionary<(string Name, bool Dark), Bitmap?> _icons = [];

    public static SettingsIconConverter Instance { get; } = new();

    /// <summary>Whether the icon of a page is lightened on a dark theme (the Font and Link icons of <c>FormSettings</c>).</summary>
    public static IValueConverter AdaptsLightness { get; } = new FuncValueConverter<object?, bool>(icon => icon is "Font" or "Link");

    public static IValueConverter BoldWhenTrue { get; } = new FuncValueConverter<bool, FontWeight>(value => value ? FontWeight.Bold : FontWeight.Normal);

    public static IValueConverter VisibleWhenTrue { get; } = new FuncValueConverter<bool, double>(value => value ? 1 : 0);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] png)
        {
            // The image of a plugin or of a script, not shown if invalid.
            try
            {
                using MemoryStream stream = new(png);
                return new Bitmap(stream);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException or IOException)
            {
                return null;
            }
        }

        if (value is not string name)
        {
            return null;
        }

        (string Name, bool Dark) cacheKey = (name, global::Avalonia.Application.Current?.ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark);
        if (!_icons.TryGetValue(cacheKey, out Bitmap? icon))
        {
            Uri uri = new($"avares://GitUI.Avalonia/Assets/{name}.png");
            icon = AssetLoader.Exists(uri) ? new Bitmap(AssetLoader.Open(uri)) : null;
            _icons[cacheKey] = icon;
        }

        return icon;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
