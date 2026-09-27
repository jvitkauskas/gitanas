using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Images drawn for light backgrounds, adapted to a dark theme (<c>ColorHelper.AdaptLightness</c>, <c>LightnessCorrection</c>):
///  the lightness of each pixel is mapped from black..white onto text..background, keeping its hue.
/// </summary>
public static class ImageLightness
{
    // The text and background of the dark theme (the WindowText and Window colors that LightnessCorrection maps onto).
    private static readonly Color DarkText = Color.FromRgb(0xE6, 0xE6, 0xE6);
    private static readonly Color DarkBackground = Color.FromRgb(0x20, 0x20, 0x20);

    private static readonly ConditionalWeakTable<IImage, Bitmap> _adapted = [];

    // The original of each adapted image.
    private static readonly ConditionalWeakTable<IImage, IImage> _originals = [];

    /// <summary>The image for the theme: adapted (once) on a dark theme, else itself.</summary>
    public static IImage? ForTheme(IImage? image, ThemeVariant? theme)
    {
        if (image is not Bitmap bitmap || theme != ThemeVariant.Dark || IsModernIcon(bitmap))
        {
            return image;
        }

        return _adapted.GetValue(bitmap, source =>
        {
            Bitmap adapted = Adapt((Bitmap)source, DarkText, DarkBackground);
            _originals.AddOrUpdate(adapted, source);
            return adapted;
        });
    }

    /// <summary>
    ///  Whether <paramref name="bitmap"/> is an icon of the Modern theme (<see cref="ModernIconLoader"/>), drawn for its theme
    ///  variant already: eng/ModernIcons marks them with the alphas 1, 2 and 3 in the top left corner.
    /// </summary>
    internal static bool IsModernIcon(Bitmap bitmap)
    {
        if (bitmap.PixelSize.Width < 2 || bitmap.PixelSize.Height < 2)
        {
            return false;
        }

        byte[] pixels = new byte[2 * 2 * 4];
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(0, 0, 2, 2), handle.AddrOfPinnedObject(), pixels.Length, 2 * 4);
        }
        finally
        {
            handle.Free();
        }

        return pixels[3] == 1 && pixels[7] == 2 && pixels[11] == 3;
    }

    /// <summary>As <c>LightnessCorrection</c>: the lightness mapped onto <paramref name="text"/>..<paramref name="background"/>.</summary>
    public static Bitmap Adapt(Bitmap source, Color text, Color background)
    {
        PixelSize size = source.PixelSize;
        WriteableBitmap target = new(size, source.Dpi, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using ILockedFramebuffer buffer = target.Lock();
        source.CopyPixels(buffer);

        double textL = text.ToHsl().L;
        double backgroundL = background.ToHsl().L;
        byte[] row = new byte[size.Width * 4];
        for (int y = 0; y < size.Height; y++)
        {
            nint address = buffer.Address + (y * buffer.RowBytes);
            Marshal.Copy(address, row, 0, row.Length);
            for (int x = 0; x < row.Length; x += 4)
            {
                if (row[x + 3] == 0)
                {
                    continue;
                }

                HslColor hsl = Color.FromRgb(row[x + 2], row[x + 1], row[x]).ToHsl();

                // Near black, the hue is not distinguishable: the saturation is not kept.
                double saturation = hsl.L > 0.1 ? hsl.S : hsl.S * hsl.L / 0.1;
                Color adapted = HslColor.ToRgb(hsl.H, saturation, textL + (hsl.L * (backgroundL - textL)), 1);
                row[x] = adapted.B;
                row[x + 1] = adapted.G;
                row[x + 2] = adapted.R;
            }

            Marshal.Copy(row, 0, address, row.Length);
        }

        return target;
    }

    /// <summary>
    ///  The image of an <see cref="Image"/> adapted to its theme (<c>AdaptLightness</c>), set in XAML instead of its source:
    ///  <c>h:ImageLightness.Source="avares://..."</c>.
    /// </summary>
    public static readonly AttachedProperty<IImage?> SourceProperty =
        AvaloniaProperty.RegisterAttached<Image, IImage?>("Source", typeof(ImageLightness));

    public static IImage? GetSource(Image image) => image.GetValue(SourceProperty);

    public static void SetSource(Image image, IImage? value) => image.SetValue(SourceProperty, value);

    /// <summary>
    ///  Whether the source of an <see cref="Image"/> (e.g. bound) is adapted to its theme, as the icons that WinForms adapted
    ///  (<c>h:ImageLightness.Adapt="True"</c>).
    /// </summary>
    public static readonly AttachedProperty<bool> AdaptProperty =
        AvaloniaProperty.RegisterAttached<Image, bool>("Adapt", typeof(ImageLightness));

    public static bool GetAdapt(Image image) => image.GetValue(AdaptProperty);

    public static void SetAdapt(Image image, bool value) => image.SetValue(AdaptProperty, value);

    // Whether the theme of the image is watched.
    private static readonly AttachedProperty<bool> IsWatchedProperty =
        AvaloniaProperty.RegisterAttached<Image, bool>("IsWatched", typeof(ImageLightness));

    static ImageLightness()
    {
        SourceProperty.Changed.AddClassHandler<Image>((image, _) =>
        {
            Watch(image);
            Apply(image);
        });
        AdaptProperty.Changed.AddClassHandler<Image>((image, _) =>
        {
            Watch(image);
            UpdateAdapted(image);
        });
        Image.SourceProperty.Changed.AddClassHandler<Image>((image, _) =>
        {
            if (GetAdapt(image))
            {
                UpdateAdapted(image);
            }
        });
    }

    private static void Watch(Image image)
    {
        if (!image.GetValue(IsWatchedProperty))
        {
            image.SetValue(IsWatchedProperty, true);
            image.ActualThemeVariantChanged += (_, _) =>
            {
                if (image.IsSet(SourceProperty))
                {
                    Apply(image);
                }
                else if (GetAdapt(image))
                {
                    UpdateAdapted(image);
                }
            };
        }
    }

    private static void Apply(Image image) => image.Source = ForTheme(GetSource(image), image.ActualThemeVariant);

    // The source as set (e.g. by a binding, which SetCurrentValue keeps), or its adapted image.
    private static void UpdateAdapted(Image image)
    {
        IImage? current = image.Source;
        IImage? original = current is not null && _originals.TryGetValue(current, out IImage? source) ? source : current;
        IImage? wanted = GetAdapt(image) ? ForTheme(original, image.ActualThemeVariant) : original;
        if (!ReferenceEquals(wanted, current))
        {
            image.SetCurrentValue(Image.SourceProperty, wanted);
        }
    }
}
