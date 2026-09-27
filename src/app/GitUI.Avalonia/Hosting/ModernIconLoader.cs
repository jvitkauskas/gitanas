using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  The icons of the Modern theme (docs/avalonia-port/MODERN.md): the asset loader of Avalonia, with the icons of
///  <c>avares://GitUI.Avalonia/Assets/&lt;name&gt;.png</c> replaced by the line icons of <c>Assets/Modern/{Light,Dark}</c>
///  (generated from Octicons by eng/ModernIcons) where there is one, for the theme variant of the application. The views
///  keep their icons: XAML and code load them through <see cref="AssetLoader"/>, which this loader serves.
/// </summary>
internal sealed class ModernIconLoader(IAssetLoader inner) : IAssetLoader
{
    private const string Prefix = "avares://GitUI.Avalonia/Assets/";

    /// <summary>
    ///  Serves the Modern icons from now on (once), in place of the asset loader of the platform. The locator of Avalonia is
    ///  internal since Avalonia 12: bound through reflection, and without the Modern icons if that fails.
    /// </summary>
    public static bool Install()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        object? locator = typeof(AvaloniaLocator).GetProperty("CurrentMutable", flags)?.GetValue(null);
        IAssetLoader? current = Current();
        if (current is null or ModernIconLoader)
        {
            return current is not null;
        }

        try
        {
            object? registration = typeof(AvaloniaLocator).GetMethod("Bind", flags)?.MakeGenericMethod(typeof(IAssetLoader)).Invoke(locator, null);
            registration?.GetType().GetMethod("ToConstant", flags)?.MakeGenericMethod(typeof(ModernIconLoader)).Invoke(registration, [new ModernIconLoader(current)]);
        }
        catch (Exception exception) when (exception is TargetInvocationException or InvalidOperationException or ArgumentException)
        {
            Trace.WriteLine($"The icons of the Modern theme are not available: {exception.Message}");
        }

        return Current() is ModernIconLoader;

        IAssetLoader? Current() => typeof(AvaloniaLocator).GetMethod("GetService", flags, [typeof(Type)])?.Invoke(locator, [typeof(IAssetLoader)]) as IAssetLoader;
    }

    /// <summary>The Modern icon of <paramref name="uri"/> for the theme variant, if it has one.</summary>
    internal Uri Resolve(Uri uri, Uri? baseUri = null)
    {
        Uri absolute = uri.IsAbsoluteUri || baseUri is null ? uri : new Uri(baseUri, uri);
        if (!GitExtensionsAvaloniaApp.IsModern || !absolute.IsAbsoluteUri)
        {
            return uri;
        }

        string text = absolute.OriginalString;
        if (!text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            || !text.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || text.IndexOf('/', Prefix.Length) >= 0)
        {
            return uri;
        }

        string variant = Application.Current?.ActualThemeVariant == ThemeVariant.Dark ? "Dark" : "Light";
        Uri modern = new($"{Prefix}Modern/{variant}/{text[Prefix.Length..]}");
        return inner.Exists(modern) ? modern : uri;
    }

    public bool Exists(Uri uri, Uri? baseUri = null) => inner.Exists(uri, baseUri);

    public Stream Open(Uri uri, Uri? baseUri = null) => inner.Open(Resolve(uri, baseUri), baseUri);

    public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
        => inner.OpenAndGetAssembly(Resolve(uri, baseUri), baseUri);

    public Assembly? GetAssembly(Uri uri, Uri? baseUri = null) => inner.GetAssembly(uri, baseUri);

    public IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri) => inner.GetAssets(uri, baseUri);

    public void SetDefaultAssembly(Assembly assembly) => inner.SetDefaultAssembly(assembly);

    public void InvalidateAssemblyCache(string name) => inner.InvalidateAssemblyCache(name);

    public void InvalidateAssemblyCache() => inner.InvalidateAssemblyCache();
}
