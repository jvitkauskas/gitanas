using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.Avalonia.Controls.FileStatusList;

/// <summary>The icon of an image key of the file status list (the images of the WinForms <c>FileStatusList</c>).</summary>
public sealed class FileStatusIconConverter : IValueConverter
{
    public static FileStatusIconConverter Instance { get; } = new();

    private static readonly Dictionary<(string Name, bool Dark), Bitmap?> _icons = [];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string key ? GetIcon(key) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static Bitmap? GetIcon(string key)
    {
        (string Name, bool Dark) cacheKey = (key, global::Avalonia.Application.Current?.ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark);
        if (!_icons.TryGetValue(cacheKey, out Bitmap? icon))
        {
            string file = key switch
            {
                FileStatusIcons.DefaultFileImage => "File",
                FileStatusIcons.GitGrepIconName => "ViewFile",
                _ => key,
            };
            Uri uri = new($"avares://GitUI.Avalonia/Assets/{file}.png");
            icon = AssetLoader.Exists(uri) ? new Bitmap(AssetLoader.Open(uri)) : null;
            _icons[cacheKey] = icon;
        }

        return icon;
    }
}
