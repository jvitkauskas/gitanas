using System.Collections;
using System.Globalization;
using System.Resources;
using GitUI;
using GitUI.Properties;
using SkiaSharp;

namespace GitUITests;

/// <summary>The icons of the resources as PNG data, without GDI+ (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).</summary>
public sealed class EmbeddedIconsTests
{
    // The images of Images outside Resources\Icons: no icons.
    private static readonly string[] _notIcons =
    [
        "DashboardBackgroundBlue", "DashboardBackgroundGrey", "GitExtensionsLogo16", "GitExtensionsLogo256", "GitExtensionsLogoWide",
        "HelpCommandMerge", "HelpCommandMergeFastForward", "HelpCommandRebase", "HelpPullFetch", "HelpPullMerge", "HelpPullMergeFastForward",
        "HelpPullRebase",
    ];

    [Test]
    [Platform(Include = "Win")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows6.1")]
    public void Every_icon_of_the_resources_is_embedded_as_the_same_image()
    {
        ResourceSet resources = Images.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        List<string> names = [];
        foreach (DictionaryEntry entry in resources)
        {
            string name = (string)entry.Key;
            if (entry.Value is not Bitmap bitmap || _notIcons.Contains(name))
            {
                continue;
            }

            names.Add(name);
            byte[]? icon = EmbeddedIcons.TryGet(name);
            icon.Should().NotBeNull(name);
            using SKBitmap decoded = SKBitmap.Decode(icon);
            decoded.Width.Should().Be(bitmap.Width, name);
            decoded.Height.Should().Be(bitmap.Height, name);
        }

        names.Should().HaveCountGreaterThan(200);
        EmbeddedIcons.Names.Select(name => name.ToLowerInvariant()).Should().Contain(names.Select(name => name.ToLowerInvariant()),
            "the names of the icons are the names of the images (ignoring case), for the icons of the scripts");
    }

    [Test]
    public void Every_icon_property_has_decodable_PNG_data_without_GDI()
    {
        string[] names = typeof(Images).GetProperties()
            .Where(property => property.PropertyType == typeof(Bitmap) && !_notIcons.Contains(property.Name))
            .Select(property => property.Name).ToArray();
        names.Should().HaveCountGreaterThan(200);
        foreach (string name in names)
        {
            using SKBitmap? bitmap = SKBitmap.Decode(EmbeddedIcons.Get(name));
            bitmap.Should().NotBeNull(name);
            bitmap!.Width.Should().BeGreaterThan(0, name);
        }

        using SKBitmap modernPush = SKBitmap.Decode(EmbeddedIcons.Get("Push"));
        modernPush.Width.Should().Be(32, "shared icon resources now use the Modern artwork");
    }

    [Test]
    public void Names_are_found_ignoring_case_and_unknown_ones_are_not()
    {
        EmbeddedIcons.TryGet("puttygen").Should().BeSameAs(EmbeddedIcons.TryGet("PuttyGen"));
        EmbeddedIcons.Get("BranchLocalRoot").Should().NotBeEmpty();
        EmbeddedIcons.TryGet("NoSuchIcon").Should().BeNull();
        ((Action)(() => EmbeddedIcons.Get("NoSuchIcon"))).Should().Throw<ArgumentException>();
    }
}
