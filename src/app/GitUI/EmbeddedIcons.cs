using System.Collections.Concurrent;
using System.Collections.Frozen;
using Avalonia.Platform;

namespace GitUI;

/// <summary>
///  The icons of the resources (<c>Resources\Icons</c>, the images of <c>Properties.Images</c> of that folder) as PNG data,
///  on every system: the <c>Images</c> of the resources are GDI+ bitmaps, which only Windows reads
///  (docs/avalonia-port/CROSS-PLATFORM.md, phase 2). Each is an <c>EmbeddedResource</c> of GitUI.csproj named
///  <c>GitUI.Icons.{file name}</c>; an icon is found by the name of its image in <c>Images</c>, ignoring case.
/// </summary>
internal static class EmbeddedIcons
{
    private const string Prefix = "GitUI.Icons.";

    /// <summary>The images of <c>Images</c> whose file has another name (not only another case).</summary>
    private static readonly FrozenDictionary<string, string> _fileByImage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["BranchLocalRoot"] = "LocalBranchRoot",
        ["BranchRemoteRoot"] = "RemoteBranchRoot",
        ["DonateBadge"] = "Donate",
        ["UiScrollBar"] = "ui-scroll-bar",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>The manifest names of the icons by the name of their file without its extension.</summary>
    private static readonly Lazy<FrozenDictionary<string, string>> _resourceByFile = new(() =>
        typeof(EmbeddedIcons).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .ToFrozenDictionary(name => Path.GetFileNameWithoutExtension(name[Prefix.Length..]), StringComparer.OrdinalIgnoreCase));

    private static readonly ConcurrentDictionary<string, byte[]?> _icons = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The names of the icons: the names of their images in <c>Images</c> (else of their files, without the extension).</summary>
    public static IEnumerable<string> Names
        => _resourceByFile.Value.Keys
            .Select(file => _fileByImage.FirstOrDefault(pair => string.Equals(pair.Value, file, StringComparison.OrdinalIgnoreCase)).Key ?? file)
            .Order(StringComparer.OrdinalIgnoreCase);

    /// <summary>The icon of the image <paramref name="name"/> of <c>Images</c> (or of the file <c>Resources\Icons\{name}.png</c>).</summary>
    /// <exception cref="ArgumentException">GitUI.csproj does not embed that icon.</exception>
    public static byte[] Get(string name) => TryGet(name) ?? throw new ArgumentException($"The icon {name} is not embedded.", nameof(name));

    /// <summary>The icon of the image <paramref name="name"/> of <c>Images</c>, or <see langword="null"/> if there is none.</summary>
    public static byte[]? TryGet(string name) => _icons.GetOrAdd(name, Read);

    /// <summary>
    ///  The icon <paramref name="name"/> for the Avalonia views: the name of their asset when GitUI.Avalonia has one (with a Modern light/dark
    ///  variant where available), else its PNG data.
    /// </summary>
    public static object? ForAvalonia(string name)
        => AssetLoader.Exists(new Uri($"avares://GitUI.Avalonia/Assets/{name}.png")) ? name : TryGet(name);

    /// <summary>As <see cref="ForAvalonia(string)"/>, for an icon of <see cref="Get"/>.</summary>
    public static object ForAvalonia(byte[] icon)
        => _icons.FirstOrDefault(pair => ReferenceEquals(pair.Value, icon)).Key is { } name ? ForAvalonia(name) ?? icon : icon;

    private static byte[]? Read(string name)
    {
        string file = _fileByImage.GetValueOrDefault(name, name);
        if (!_resourceByFile.Value.TryGetValue(file, out string? resourceName))
        {
            return null;
        }

        using Stream stream = typeof(EmbeddedIcons).Assembly.GetManifestResourceStream(resourceName)!;
        using MemoryStream data = new();
        stream.CopyTo(data);

        // As PNG (an .ico is converted).
        return resourceName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? data.ToArray() : PngImages.ToPng(data.ToArray());
    }
}
