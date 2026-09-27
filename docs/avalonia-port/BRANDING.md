# Gitanas branding

The application name, window titles, settings headings, translated product names,
About dialog, executable metadata and GitHub token description use **Gitanas**.
The folded-ribbon PNG/ICO assets in `setup/assets/Logo/gitanas/` supply the window,
dashboard, About, settings and submodule icons, and the Windows executable icon.
The About dialog retains upstream authorship and icon attribution; donation links
explicitly support the original Git Extensions project.

Build with `dotnet build GitExtensions.slnx -c Release -p:UseAppHost=false`.
Run `dotnet artifacts/Release/bin/GitExtensions/net10.0/Gitanas.dll`.
Apphost builds produce `Gitanas` / `Gitanas.exe`. The source solution, project
folders, output folder, namespaces, plugin assembly names, translation IDs and
legacy resource member names stay stable for compatibility. Historical QA reports,
upstream copyright notices and links explaining upstream behavior are retained.

## Settings and integration

- Settings are now named `Gitanas.settings`, under the Gitanas application data
  directory (on Linux, `$XDG_CONFIG_HOME/Gitanas/Gitanas/`, normally
  `~/.config/Gitanas/Gitanas/`). Portable builds keep them beside the executable.
- On the first Gitanas launch, existing `GitExtensions.settings` is copied from
  the corresponding old location if the new file is absent. Existing Gitanas
  settings are never overwritten; the old installation is left untouched.
- Stored credential identifiers remain compatible so saved credentials still work.
  Cache/user-plugin directories use the new product name; separately installed
  user plugins can be placed in the new `Gitanas/UserPlugins` directory.
- Script commands accept `gitanas` / `{gitanas}` as well as the old aliases.
- Homepage, contributions and bug reports link to `jvitkauskas/gitanas`. Update
  checks read that repository's published GitHub releases, honoring the prerelease
  preference and selecting a newer numeric version tag (`v1.2.3`, for example).
  Downloads open the fork's releases page on every OS, never the upstream MSI.
- Windows installer product/upgrade/component identities, shell-extension COM
  identities and registry paths are separate from the upstream application.

## Validation on Linux

- Release solution build: passed, no warnings or errors in the final build.
- Avalonia suite: 1,288 passed; two Windows-specific cases skipped.
- Debug settings suite: 1,136 cases passed, including the three migration cases
  (copy, existing destination, absent source).
- Executable-path detection: three cases passed with the new filename.
- Release feed and embedded assets: six cases passed; one Windows-only case skipped.
- Native Wayland light/dark scaling/modal smoke test: both cases passed.
- The real renamed application launched under an isolated native Wayland compositor
  with German settings imported from the old location; the original file remained
  unchanged. Dashboard and About were also rendered in light/dark headless checks.

Windows installer/native-shell and macOS execution were not tested in this pass.
Avalonia's current native Wayland backend ignores window icons and does not expose
an application ID; the in-window ribbon works, but compositor/dock icon association
still depends on backend support. X11 and Windows receive the new window icon.
