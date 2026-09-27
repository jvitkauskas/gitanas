# Gitanas

Gitanas is an experimental cross-platform port of
[Git Extensions](https://github.com/gitextensions/gitextensions), with a desktop
interface built using Avalonia for Linux, Windows and macOS.

It builds on years of work by the Git Extensions maintainers and contributors.
They deserve credit for the original application, its Git functionality and much
of the interface that this port adapts. Gitanas is an independent fork, not an
official Git Extensions release.

## Status

The port includes repository browsing, commit history, diffs, staging and commits,
branch management, and integration with external editors and diff/merge tools.
Linux supports X11 and experimental native Wayland. Both Classic and Modern
control themes offer light and dark appearances.

This is a work in progress. Platform support and feature parity are still being
tested; see the [QA checklist and reports](docs/avalonia-port/QA.md) for coverage
and known limitations. Application names, executable names and some in-app links
still use Git Extensions while the fork is being established.

## Build and run

Install Git and the .NET 10 SDK, then:

```sh
git clone --recurse-submodules https://github.com/jvitkauskas/gitanas.git
cd gitanas
dotnet build GitExtensions.slnx -c Release -p:UseAppHost=false
dotnet artifacts/Release/bin/GitExtensions/net10.0/GitExtensions.dll
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
- Original icons include work by [Yusuke Kamiyamane](https://p.yusukekamiyamane.com/)
  under [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/). Modern icons
  include [GitHub Octicons](eng/ModernIcons/OCTICONS-LICENSE).

Gitanas retains the [GNU GPL v3 license](LICENSE.md). Third-party components retain
their own licenses and attribution notices, including the
[patched Avalonia Wayland package](third_party/avalonia-wayland/README.md).
