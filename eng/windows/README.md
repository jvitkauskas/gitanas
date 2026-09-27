# Windows release ZIPs

The **Release builds** workflow produces `windows-x64` and `windows-arm64`
framework-dependent ZIPs alongside the Linux archives and macOS bundles on version tags. The
release is published only after all six builds and startup checks succeed.
Manual workflow runs upload artifacts without creating a release.

## Installation

1. Choose `Gitanas-<version>-windows-x64-framework-dependent.zip` for Intel/AMD
   Windows, or `Gitanas-<version>-windows-arm64-framework-dependent.zip` for Windows
   on ARM.
2. Extract the entire ZIP into a writable folder.
3. Run `Gitanas/Gitanas.exe`. If the matching .NET runtime is missing, the native
   GUI launcher offers a download link. Install the runtime, then launch again.
4. Install Git separately if needed.

The requirement is **.NET 10 Runtime (`Microsoft.NETCore.App`)** for the ZIP's
architecture. Neither the SDK nor Windows Desktop Runtime is required.
Installing x64 .NET does not satisfy the ARM64 build. Runtime downloads are at
<https://dotnet.microsoft.com/download/dotnet/10.0>.

The application accepts .NET 10 servicing updates; it does not require exactly
10.0.0 or download/install .NET itself. Updates to the shared runtime are managed
separately from Gitanas. The ZIP includes Avalonia and its native dependencies,
plugins, translations, dictionaries, the crash reporter, SSH helper, optional
Explorer extension DLLs, and license notices. It omits the .NET runtime, the
upstream plugin manager, and installer machinery.

These are portable packages: settings are saved beside the application. Do not
extract them into Program Files. Extraction does not register shell extensions,
create shortcuts, or modify the registry. Windows binaries are unsigned.

## Build and validation

On Windows, install the .NET 10 SDK, Python 3.9+, Git, and Visual Studio C++ build
tools with Windows SDK and ATL support (including ARM64 components for ARM64
builds). Initialize submodules, then run:

```powershell
python eng/windows/build.py win-x64 0.1.0
python eng/windows/build.py win-arm64 0.1.0
```

As with macOS, release CI stamps assembly versions using `eng/set_version_to.cs`
before building. For a local release, run that script first in a disposable
checkout; the version argument above controls archive naming.

Each build recreates `artifacts/windows/<rid>/` and writes the ZIP and SHA-256
checksum to its `dist/` directory. The script reuses `src/native/build.proj` for
Windows helpers and bypasses the legacy MSI/plugin-manager publishing targets
with `ReleaseArchive=true`.

Both executable apphosts and runtimeconfig files are checked to ensure the
correct architecture and a shared .NET 10 dependency. Packaging fails if a
required plugin, native helper, or content file is absent, or if a .NET runtime
was accidentally bundled. CI extracts the ZIP and checks that the application
stays running for 15 seconds on a runner of the matching architecture. This
startup check uses an installed runtime; it does not test the missing-runtime
dialog or replace interactive QA.

For Linux cross-publish inspection, append `--managed-only`. This builds both
managed entry points but deliberately skips Windows native compilation and
produces no release ZIP.
