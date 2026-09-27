# The icons of the Modern theme

`icons.txt` maps the icons of Git Extensions (`src/app/GitUI.Avalonia/Assets/<name>.png`) to
[Octicons](https://github.com/primer/octicons) (v19.38.0, MIT, see `OCTICONS-LICENSE`), with a color and an optional badge.
`octicons-16.json` holds the 16 px path data of the Octicons it uses; `custom-16.json` the icons that Octicons has not, drawn
on the same grid (the Git mark, after the Git logo by Jason Long, CC BY 3.0), with the shapes cut out of them. After a change:

```sh
dotnet run eng/ModernIcons/Generate.cs
```

It writes `src/app/GitUI.Avalonia/Assets/Modern/{Light,Dark}/<name>.png`, 32 px (16 at 2x). A new Octicon needs its path
data in `octicons-16.json` (from `lib/build/data.json` of the octicons package, `heights.16.path`). See
docs/avalonia-port/MODERN.md.

Bundled plugin icons use the `Plugin<AssemblySuffix>` names in `icons.txt`.
`plugins.json` maps those names to PNG resources relative to `src/plugins`.
Run `dotnet run eng/ModernIcons/Generate.cs -- --plugins-only` to regenerate just
these icons. The generator writes light/dark assets for the application and a
neutral gray PNG for each plugin's embedded `PluginIcon.png` fallback. This
fallback remains readable in hosts that only support the plugin API's image bytes.
The host selects the named assets for bundled plugins in menus and settings;
other plugins continue supplying their own images. The two chart glyphs in
`custom-16.json` are original Gitanas artwork.

`GitUI.Avalonia.csproj` copies `OCTICONS-LICENSE` to `OCTICONS-LICENSE.txt`
in build and publish output, including consuming applications. The notice stays
alongside single-file builds and is included by the Windows installer.
