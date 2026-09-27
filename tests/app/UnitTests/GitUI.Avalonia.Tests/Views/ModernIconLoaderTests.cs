using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using GitUI.Avalonia;
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

        GitExtensionsAvaloniaApp.IsModern = true;
        try
        {
            using Bitmap light = Load(push);
            light.PixelSize.Width.Should().Be(32, "the Modern icons are drawn at 2x");
            ImageLightness.IsModernIcon(light).Should().BeTrue();
            ImageLightness.ForTheme(light, ThemeVariant.Dark).Should().BeSameAs(light, "a Modern icon is drawn for its variant already");

            UseTheme(ThemeVariant.Dark);
            using Bitmap dark = Load(push);
            FirstOpaque(dark).Should().NotEqual(FirstOpaque(light), "the dark variant has its own color");

            using Bitmap original = Load(logo);
            ImageLightness.IsModernIcon(original).Should().BeFalse("an icon without a Modern one keeps its original");
        }
        finally
        {
            GitExtensionsAvaloniaApp.IsModern = false;
        }

        // Other control themes: the original icons.
        using Bitmap fluent = Load(push);
        fluent.PixelSize.Width.Should().Be(16);
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
