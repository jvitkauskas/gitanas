# Gitanas

Gitanas is an experimental unofficial cross-platform port of
[Git Extensions](https://github.com/gitextensions/gitextensions), with a desktop
interface built using Avalonia for Linux, Windows and macOS.

<img width="1512" height="982" alt="Screenshot 2026-09-27 at 22 54 37" src="https://github.com/user-attachments/assets/d3727568-e96a-4b2c-967a-04564f9f7fd1" />

## Status

The port includes repository browsing, commit history, diffs, staging and commits,
branch management, and integration with external editors and diff/merge tools.
Linux supports X11 and experimental native Wayland. The Modern control theme
offers light and dark appearances.

This is a work in progress. Platform support and feature parity are still being
tested; see the [QA checklist and reports](docs/avalonia-port/QA.md) for coverage
and known limitations. The application is branded as Gitanas; internal project
and plugin names retain their existing identifiers for compatibility.

## Build and run

Install Git and the .NET 10 SDK, then:

```sh
git clone --recurse-submodules https://github.com/jvitkauskas/gitanas.git
cd gitanas
dotnet build GitExtensions.slnx -c Release -p:UseAppHost=false
dotnet artifacts/Release/bin/GitExtensions/net10.0/Gitanas.dll
```

For an existing checkout, run `git submodule update --init --recursive` before
building. The only remaining submodule is `Git.hub`, used by the GitHub
integration; see [external dependencies](externals/README.md).

`UseAppHost=false` produces a framework-dependent application launched with
`dotnet`, avoiding distribution-specific apphost packages. Linux also needs the
usual desktop graphics libraries and fonts; platform setup and testing details
are in the [QA guide](docs/avalonia-port/QA.md).

## Feedback

Please report problems with this fork in the
[Gitanas issue tracker](https://github.com/jvitkauskas/gitanas/issues).

## Credits and license

- [Git Extensions and its contributors](https://github.com/gitextensions/gitextensions)
  created the original application on which Gitanas is based.
- [Avalonia](https://avaloniaui.net/) provides the cross-platform UI framework.
- Modern icons
  include [GitHub Octicons](eng/ModernIcons/OCTICONS-LICENSE).

Gitanas retains the [GNU GPL v3 license](LICENSE.md). Third-party components retain
their own licenses and attribution notices, including the
[patched Avalonia Wayland package](third_party/avalonia-wayland/README.md).
