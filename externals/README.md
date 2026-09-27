# External source dependencies

Gitanas keeps one Git submodule. Initialize it with:

```sh
git submodule update --init --recursive
```

| Component | How it is included | Why it is kept |
| --- | --- | --- |
| `Git.hub` | Submodule, pinned to the upstream Git Extensions repository | Referenced by `GitUI` and the `GitHub3` plugin for GitHub API access, avatar lookup and release queries. The existing target-framework override lets it build for `net10.0`. |
| `ConEmuInside` | Vendored sources in this directory | The WinForms-free wrapper used for optional ConEmu integration on Windows. Its sources and BSD license are self-contained; the original `conemu-inside` submodule is not needed. |
| `NetSpell.SpellChecker` | Vendored sources in this directory | Used by the commit-message spell checker. |

The old `ICSharpCode.TextEditor` submodule was a WinForms editor and is no longer
referenced by the build. The Avalonia interface uses AvaloniaEdit. The small
encoding-detection helper already copied into `GitUI/Editor/FileReader.cs` retains
its attribution; it does not require the editor submodule.

The original `conemu-inside` submodule is also removed: `ConEmuInside` above contains
the adapted sources actually compiled by this port. Its [README and license](ConEmuInside/README.md)
record their upstream provenance.

The Visual Studio VSIX extension is maintained in a separate upstream repository;
this checkout contains no VSIX project or extension sources. The application's
Windows-only “Open in Visual Studio” command is an editor integration, not a VSIX.

Other vendored dependencies live in `third_party`, including the
[patched Avalonia Wayland package](../third_party/avalonia-wayland/README.md), and
remain necessary for the current build.
