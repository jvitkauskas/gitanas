# Disk image artwork

`background.svg` is the editable 720 × 500 point source. The background contains
installation and first-launch instructions; Finder supplies the actual app and
Applications shortcut icons. Their centers are (184, 210) and (536, 210), matching
`dmg_package.py`. Keep their labels clear of the instruction panel.

Typography uses [Inter 4.1](https://github.com/rsms/inter/releases/tag/v4.1):
Inter Display SemiBold for the title, Inter Regular/SemiBold elsewhere. Inter is
licensed under the SIL Open Font License. Font files are not distributed in the
DMG; the text is rendered into the background images.

After installing Inter and librsvg, regenerate both checked-in images:

```sh
rsvg-convert -o eng/macos/dmg/background.png eng/macos/dmg/background.svg
rsvg-convert -z 2 -o eng/macos/dmg/background@2x.png eng/macos/dmg/background.svg
```

`dmgbuild` combines the 1× and 2× images into a multi-resolution TIFF for Finder.
The release runner needs no fonts or SVG renderer because it uses these PNGs.
