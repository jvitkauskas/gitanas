# macOS releases

The `Release builds` workflow runs when a version tag is pushed, for example
`v0.1.0` or `v0.1.0-preview.1`. It builds natively on Apple Silicon and Intel
GitHub-hosted runners, creates `Gitanas.app` for each architecture, applies an
ad-hoc signature, checks its signature and native architectures, and packages a
styled DMG. The app is copied back out of the mounted DMG and checked for signature integrity
and successful startup.

When **all six Linux/Windows/macOS** builds succeed, the workflow creates a GitHub release and attaches:

- `Gitanas-<version>-macos-arm64.dmg` for Apple Silicon.
- `Gitanas-<version>-macos-x64.dmg` for Intel.
- A SHA-256 checksum file for each DMG.

Tags with a suffix such as `-preview.1` produce prereleases. The workflow first
creates a draft, uploads all builds, then publishes it. A failed build creates
no release; a failed upload can leave a draft that a rerun completes. Existing
release notes are preserved on reruns. No Apple account, signing certificate,
or custom GitHub secret is needed; the release job uses `GITHUB_TOKEN` with
`contents: write` permission.

To release a reviewed commit on `main`, after committing and pushing the workflow:

```sh
git switch main
git pull --ff-only
git tag -a v0.1.0 -m 'Gitanas 0.1.0'
git push origin v0.1.0
```

Choose the desired version instead of reusing an existing tag. Builds on every
merge are intentionally omitted: version tags identify the downloadable releases.
Use **Actions → Release builds → Run workflow** to test packaging first. Manual
runs accept a version and upload workflow artifacts only, even when run on a tag.

## Installation

Requires macOS 14 or later and Git (for example, installed through Xcode Command
Line Tools or Homebrew). The .NET runtime is included. Open the DMG and drag `Gitanas` onto the
Applications shortcut. Open Gitanas from Applications. The background includes
the installation and first-launch approval instructions, rendered at 1× and 2×
resolution for Retina displays.

These builds are **ad-hoc signed, not notarized**. After the first blocked launch,
users who trust the download can choose **System Settings → Privacy & Security
→ Open Anyway**. Ad-hoc signing does not establish a verified developer identity.
See [Apple's instructions](https://support.apple.com/102445).

Settings are stored in the user's application data directory, outside the signed
bundle. The bundle includes plugins, translations, dictionaries, and license
notices. It does not download the old Git Extensions plugin manager.

## Local packaging

With Git submodules initialized, .NET 10 SDK, Python 3.9 or later, and Xcode Command
Line Tools on a Mac, install the pinned build-only DMG tools into a virtual
environment:

```sh
python3 -m venv .tools/dmg-venv
source .tools/dmg-venv/bin/activate
python -m pip install --require-hashes -r eng/macos/requirements-dmg.txt
# Stamp the version first in a disposable checkout; this changes source files.
(cd eng && dotnet run set_version_to.cs -- -v 0.1.0 -t 0.1.0)
bash eng/macos/build.sh osx-arm64 0.1.0
# Or, on an Intel Mac:
bash eng/macos/build.sh osx-x64 0.1.0
```

Outputs are in `artifacts/macos/<rid>/dist/`. Each build recreates its isolated
`artifacts/macos/<rid>/` directory. The build script uses `MacOSBundle=true` to
bypass the legacy Windows publishing/cleanup targets, and selects the current
.NET runtime patch supplied by the SDK rather than the repository's original
10.0.0 runtime pin. Trimming and single-file publishing are disabled to preserve
plugin discovery and the existing file layout.

On Linux, append `--bundle-only` to cross-publish and inspect the unsigned `.app`.
This deliberately produces no release DMG: signing, signature verification, and
startup checks need macOS. A startup check is not a replacement for interactive
QA, particularly Git/SSH, plugins, file dialogs, and Retina rendering.

The background source and font/rendering instructions are in [dmg/README.md](dmg/README.md).
`dmgbuild` writes the Finder layout directly, without Finder scripting during
image creation. Verification mounts the read-only image, checks its Applications
link and background metadata, verifies code signatures before and after copying
the app out, and retains an optional Finder screenshot as a separate CI artifact.
