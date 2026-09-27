# Linux release archives

The **Release builds** workflow builds self-contained `linux-x64` and
`linux-arm64` archives on version tags, alongside the Windows and macOS packages.
All six build/startup jobs must pass before the release is published. Manual
workflow runs upload artifacts only.

## Run

Choose x64 for Intel/AMD, or ARM64 for a 64-bit ARM desktop. Download the archive
and its `.sha256` file from [Releases](https://github.com/jvitkauskas/gitanas/releases):

```sh
sha256sum -c Gitanas-<version>-linux-x64.tar.gz.sha256
tar -xzf Gitanas-<version>-linux-x64.tar.gz
./Gitanas/Gitanas
```

Substitute the actual version and architecture. Extract the whole archive;
`Gitanas` is the executable inside the enclosing `Gitanas` directory. No root
installation, .NET installation, or extra `chmod` is needed. The application can
also be launched from another working directory using its absolute path.

These archives include .NET 10, Avalonia (including the patched Wayland backend),
all bundled plugins, translations, dictionaries, the standalone crash reporter,
and native rendering, syntax-highlighting, and terminal libraries. License
notices are included. Git and external tools are not bundled.

Settings and user plugins remain in the user's configuration/data directories
(respecting `XDG_CONFIG_HOME` and `XDG_DATA_HOME`). To update, extract a new release
into a separate directory and use that copy. Do not overlay an old installation,
as obsolete files could remain. There is no built-in updater, desktop-menu
registration, or system package installation.

## System requirements

The initial CI baseline is **Ubuntu 24.04**, with native x64 and ARM64 runners.
These are **glibc** builds, not Alpine/musl builds. Compatibility with other
distributions depends on their native libraries; bundling .NET does not bundle
an entire Linux system. Older distributions are not guaranteed to work.

Install Git, fonts, and your distribution's usual desktop graphics libraries.
.NET also uses ICU, OpenSSL, zlib, and other system libraries. For Ubuntu 24.04:

```sh
sudo apt install git openssh-client ca-certificates libicu74 libssl3t64 \
  libgssapi-krb5-2 libc6 libgcc-s1 libstdc++6 zlib1g libfontconfig1 \
  libx11-6 libice6 libsm6 libxext6 libxrender1 libxrandr2 libxi6 libxcursor1 \
  libwayland-client0 libwayland-cursor0 libwayland-egl1 libxkbcommon0 \
  libegl1 libgl1 libdbus-1-3 fonts-dejavu-core
```

Package names differ on other distributions. Install SSH, GPG, credential helpers,
editors, and diff/merge tools as needed; Gitanas uses the normal host installations.
Linux credential storage additionally needs `secret-tool` (`libsecret-tools` on
Ubuntu) and a running Secret Service provider, such as GNOME Keyring or a
compatible KDE wallet service.

Native Wayland is experimental and selected automatically in Wayland sessions.
To use X11 (or XWayland in a Wayland session), run:

```sh
GITEXTENSIONS_USE_WAYLAND=0 ./Gitanas/Gitanas
```

## Build and validation

From a checkout with submodules initialized, using the .NET 10 SDK and Python
3.12 or later:

```sh
python3 eng/linux/build.py linux-x64 0.1.0
python3 eng/linux/build.py linux-arm64 0.1.0
```

Each command recreates its isolated `artifacts/linux/<rid>/` directory. Archives
and checksums are written to `dist/`. Cross-publishing is supported; GUI startup
checks require a host of the matching architecture. The version argument names
the archive; CI stamps assembly versions first using `eng/set_version_to.cs`.
Local releases should do the same in a disposable checkout.

Packaging verifies both self-contained runtime configs, the crash reporter's
dependencies, ELF architectures, required native/content files, and licenses.
It removes Windows/macOS content and preserves executable permissions in tar.

CI extracts the archive into a path containing spaces, checks its checksum and
native dependencies with `ldd`, and launches both apphosts under Xvfb. It verifies
that each process loads the packaged `libcoreclr.so`, with `DOTNET_ROOT` pointing
to an empty directory and isolated user configuration. The check can also run
inside an existing X11/Wayland session:

```sh
python3 eng/linux/smoke-test.py artifacts/linux/linux-x64/dist/*.tar.gz
```

This is a startup check, not a replacement for interactive Git/desktop QA.
The optional .NET LTTng tracing provider is excluded from the `ldd` requirement;
its tracing dependency is not required for normal application startup.

Initial local validation (2026-09-28): both architectures cross-built and passed
packaging checks. The x64 archive passed the extraction/startup test on Arch
under Xvfb and in the Ubuntu 24.04/GNOME and Fedora 44/KDE VMs under native
Wayland, including the standalone crash reporter. ARM64 execution awaits the
first native CI run.
