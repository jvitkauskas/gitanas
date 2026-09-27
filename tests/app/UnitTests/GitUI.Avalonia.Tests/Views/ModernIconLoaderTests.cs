using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.Controls.FileStatusList;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The icons of the Modern theme (<see cref="ModernIconLoader"/>, eng/ModernIcons).</summary>
[TestFixture]
public sealed class ModernIconLoaderTests : HeadlessTest
{
    [Test]
    public Task The_Modern_theme_serves_its_icons_for_the_theme_variant_and_keeps_the_others() => OnUiThreadAsync(() =>
    {
        Uri push = new("avares://GitUI.Avalonia/Assets/Push.png");
        Uri logo = new("avares://GitUI.Avalonia/Assets/GitGui.png");
        ModernIconLoader.Install().Should().BeTrue("the asset loader of Avalonia is replaced");

        using Bitmap light = Load(push);
        light.PixelSize.Width.Should().Be(32, "the Modern icons are drawn at 2x");
        ImageLightness.IsModernIcon(light).Should().BeTrue();
        ImageLightness.ForTheme(light, ThemeVariant.Dark).Should().BeSameAs(light, "a Modern icon is drawn for its variant already");

        UseTheme(ThemeVariant.Dark);
        using Bitmap dark = Load(push);
        FirstOpaque(dark).Should().NotEqual(FirstOpaque(light), "the dark variant has its own color");

        using Bitmap original = Load(logo);
        ImageLightness.IsModernIcon(original).Should().BeFalse("an icon without a Modern one keeps its original");
    });

    [Test]
    public Task Every_Modern_icon_is_available_by_its_public_name_in_both_color_modes() => OnUiThreadAsync(() =>
    {
        const string root = "avares://GitUI.Avalonia/Assets/";
        Uri[] lightIcons = AssetLoader.GetAssets(new Uri(root + "Modern/Light/"), null).ToArray();
        lightIcons.Should().HaveCountGreaterThan(200);
        foreach (ThemeVariant variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            UseTheme(variant);
            foreach (Uri light in lightIcons)
            {
                string name = light.AbsolutePath.Split('/')[^1];
                Uri publicName = new(root + name);
                AssetLoader.Exists(publicName).Should().BeTrue(name);
                using Bitmap icon = Load(publicName);
                icon.PixelSize.Width.Should().Be(32, name);
                ImageLightness.IsModernIcon(icon).Should().BeTrue(name);
                using Stream expected = AssetLoader.Open(new Uri(root + $"Modern/{variant.Key}/{name}"));
                using Stream actual = AssetLoader.Open(publicName);
                using MemoryStream expectedBytes = new();
                using MemoryStream actualBytes = new();
                expected.CopyTo(expectedBytes);
                actual.CopyTo(actualBytes);
                actualBytes.ToArray().Should().Equal(expectedBytes.ToArray(), name);
            }
        }
    });

    [Test]
    public Task Icons_can_be_loaded_on_a_worker_thread_after_a_color_mode_change() => OnUiThreadAsync(async () =>
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            UseTheme(variant);
            byte[] expected = ReadIcon();
            byte[] actual = await Task.Run(ReadIcon);
            actual.Should().Equal(expected);
        }

        static byte[] ReadIcon()
        {
            using Stream icon = AssetLoader.Open(new Uri("avares://GitUI.Avalonia/Assets/Settings.png"));
            using MemoryStream data = new();
            icon.CopyTo(data);
            return data.ToArray();
        }
    });

    [Test]
    public Task Cached_file_and_settings_icons_follow_the_color_mode() => OnUiThreadAsync(() =>
    {
        UseTheme(ThemeVariant.Light);
        Bitmap lightFile = FileStatusIconConverter.GetIcon("File")!;
        Bitmap lightSettings = (Bitmap)SettingsIconConverter.Instance.Convert("Settings", typeof(Bitmap), null, System.Globalization.CultureInfo.InvariantCulture)!;
        UseTheme(ThemeVariant.Dark);
        Bitmap darkFile = FileStatusIconConverter.GetIcon("File")!;
        Bitmap darkSettings = (Bitmap)SettingsIconConverter.Instance.Convert("Settings", typeof(Bitmap), null, System.Globalization.CultureInfo.InvariantCulture)!;
        FirstOpaque(darkFile).Should().NotEqual(FirstOpaque(lightFile));
        FirstOpaque(darkSettings).Should().NotEqual(FirstOpaque(lightSettings));
        UseTheme(ThemeVariant.Light);
        FileStatusIconConverter.GetIcon("File").Should().BeSameAs(lightFile);
        SettingsIconConverter.Instance.Convert("Settings", typeof(Bitmap), null, System.Globalization.CultureInfo.InvariantCulture).Should().BeSameAs(lightSettings);
    });

    private static Bitmap Load(Uri uri) => new(AssetLoader.Open(uri));

    // The color of the first opaque pixel.
    private static byte[] FirstOpaque(Bitmap bitmap)
    {
        byte[] pixels = new byte[bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4];
        System.Runtime.InteropServices.GCHandle handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new global::Avalonia.PixelRect(bitmap.PixelSize), handle.AddrOfPinnedObject(), pixels.Length, bitmap.PixelSize.Width * 4);
        }
        finally
        {
            handle.Free();
        }

        int opaque = Enumerable.Range(0, pixels.Length / 4).First(i => pixels[(i * 4) + 3] == 255);
        return pixels[(opaque * 4)..((opaque * 4) + 3)];
    }
}
