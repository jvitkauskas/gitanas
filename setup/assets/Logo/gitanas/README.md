# Gitanas folded-ribbon icons

Icon-only draft: orange and charcoal folded ribbon, with no wordmark, on a
transparent background. These assets are not yet wired into the application.

- `ribbon-source.png`: original generated raster master (1254 × 1254).
- `gitanas-{size}.png`: square PNGs at 16, 20, 24, 32, 40, 48, 64, 96, 128,
  192, 256, 512 and 1024 pixels, with alpha transparency.
- `gitanas.ico`: Windows icon containing 16, 24, 32, 48, 64, 128 and 256-pixel images.
- `gitanas.icns`: macOS container with standard and Retina PNG representations
  through 1024 pixels. Container structure verified on Linux; not tested in macOS.
- `gitanas-icons.zip`: PNG, ICO and ICNS exports together with this README.

Created using the built-in image-generation tool, editing the selected ribbon
concept. Prompt: remove the Gitanas wordmark, preserve the orange and charcoal
interlocking folded ribbon, center it on a transparent square canvas, and keep
its proportions and negative-space opening without extra elements.

PNG sizes were exported with ImageMagick Lanczos resampling. The ICO was packaged
with ImageMagick; the ICNS packages those same PNGs without changing their pixels.
These are raster exports, not vector artwork. The dark ribbon may have low
contrast on very dark backgrounds.

To regenerate the PNG sizes and Windows icon from this directory:

```sh
for size in 16 20 24 32 40 48 64 96 128 192 256 512 1024; do
  magick ribbon-source.png -filter Lanczos -resize "${size}x${size}" "gitanas-${size}.png"
done
magick gitanas-1024.png -define icon:auto-resize=256,128,64,48,32,24,16 gitanas.ico
```
