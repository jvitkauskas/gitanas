#!/usr/bin/env python3
"""Assemble a published Gitanas app; sign and archive it on macOS."""

import argparse
import json
import plistlib
import shutil
import subprocess
import sys
from pathlib import Path


def run(*args):
    subprocess.run(args, check=True)


def package(args):
    root = Path(__file__).resolve().parents[2]
    work = args.work_dir.resolve()
    app = work / 'Gitanas.app'
    contents = app / 'Contents'
    binaries = contents / 'MacOS'
    resources = contents / 'Resources'
    build = work / 'build/bin/GitExtensions/net10.0'
    if app.exists():
        shutil.rmtree(app)
    shutil.copytree(work / 'publish', binaries)
    # These content files bypass RID filtering in their upstream NuGet packages.
    # The macOS terminal uses Porta.Pty, not the Windows-only ConEmu executables.
    shutil.rmtree(binaries / 'ConEmu', ignore_errors=True)
    for platform in (binaries / 'runtimes').glob('*'):
        if platform.is_dir() and platform.name.startswith(('win', 'linux')):
            shutil.rmtree(platform)
    for name in ('BugReporter', 'BugReporter.dll', 'BugReporter.deps.json',
                 'BugReporter.runtimeconfig.json'):
        shutil.copy2(work / 'reporter-publish' / name, binaries / name)
    resources.mkdir()
    shutil.copy2(root / 'setup/assets/Logo/gitanas/gitanas.icns', resources)
    shutil.copytree(build / 'Translation', binaries / 'Translation', dirs_exist_ok=True)
    shutil.copytree(root / 'setup/assets/Dictionaries', binaries / 'Dictionaries')

    # Plugins share the application's dependencies. Keep plugin-specific managed
    # dependencies beside their plugin, where ManagedExtensibility resolves them.
    plugin_projects = sorted((root / 'src/plugins').rglob('GitExtensions.Plugins.*.csproj'))
    for project in plugin_projects:
        name = project.stem.removeprefix('GitExtensions.Plugins.')
        source = build / 'Plugins' / name
        assembly = source / f'{project.stem}.dll'
        if not assembly.is_file():
            raise RuntimeError(f'Missing bundled plugin: {assembly}')
        destination = binaries / 'Plugins' / name
        destination.mkdir(parents=True, exist_ok=True)
        for library in source.glob('*.dll'):
            if not (binaries / library.name).exists():
                shutil.copy2(library, destination)

    shutil.copy2(root / 'LICENSE.md', resources / 'LICENSE.md')
    notices = resources / 'Avalonia.Wayland'
    notices.mkdir()
    for name in ('LICENSE.md', 'NOTICE.md'):
        shutil.copy2(root / 'third_party/avalonia-wayland' / name, notices / name)

    numeric_version = args.version.split('-')[0]
    with (contents / 'Info.plist').open('wb') as stream:
        plistlib.dump({
            'CFBundleIdentifier': 'io.github.jvitkauskas.gitanas',
            'CFBundleName': 'Gitanas',
            'CFBundleDisplayName': 'Gitanas',
            'CFBundleExecutable': 'Gitanas',
            'CFBundleIconFile': 'gitanas.icns',
            'CFBundlePackageType': 'APPL',
            'CFBundleInfoDictionaryVersion': '6.0',
            'CFBundleShortVersionString': numeric_version,
            'CFBundleVersion': numeric_version,
            'LSMinimumSystemVersion': '14.0',
            'LSApplicationCategoryType': 'public.app-category.developer-tools',
            'NSHighResolutionCapable': True,
        }, stream)

    # Missing runtime/native/content files must fail packaging, not the user's launch.
    required = ('Gitanas', 'Gitanas.dll', 'Gitanas.runtimeconfig.json',
                'libhostfxr.dylib', 'libcoreclr.dylib', 'libAvaloniaNative.dylib',
                'libSkiaSharp.dylib', 'libHarfBuzzSharp.dylib',
                'libonigwrap.dylib', 'libporta_pty.dylib',
                'OCTICONS-LICENSE.txt', 'Translation/English.xlf',
                'Dictionaries/en-US.dic', 'Gitanas.dll.config')
    for name in required:
        if not (binaries / name).is_file():
            raise RuntimeError(f'Missing bundle content: {name}')
    config = json.loads((binaries / 'Gitanas.runtimeconfig.json').read_text())
    if 'includedFrameworks' not in config['runtimeOptions']:
        raise RuntimeError('The bundle must include the .NET runtime')
    for dependency in (work / 'reporter-publish').iterdir():
        if dependency.suffix in ('.dll', '.dylib') and not (binaries / dependency.name).is_file():
            raise RuntimeError(f'Missing crash reporter dependency: {dependency.name}')

    # Runtime packs are download dependencies, not ordinary entries in the assets
    # file's libraries map. Resolve their license files from the NuGet package roots.
    runtime_version = next(f['version'] for f in config['runtimeOptions']['includedFrameworks']
                           if f['name'] == 'Microsoft.NETCore.App')
    assets = json.loads((work / 'build/obj/GitExtensions/project.assets.json').read_text())
    runtime_pack = next((Path(folder) / f'microsoft.netcore.app.runtime.{args.rid}' / runtime_version
                         for folder in assets['packageFolders']
                         if (Path(folder) / f'microsoft.netcore.app.runtime.{args.rid}' /
                             runtime_version / 'LICENSE.TXT').is_file()), None)
    if runtime_pack is None:
        raise RuntimeError('Cannot find the bundled .NET runtime license')
    runtime_notices = resources / 'dotnet'
    runtime_notices.mkdir()
    for name in ('LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT'):
        shutil.copy2(runtime_pack / name, runtime_notices / name)
    (binaries / 'Gitanas').chmod(0o755)
    (binaries / 'BugReporter').chmod(0o755)

    if args.bundle_only:
        print(f'Unsigned bundle for inspection only: {app}')
        return
    if sys.platform != 'darwin':
        raise RuntimeError('Signing and archiving require macOS')

    # Sign every file under MacOS, including managed DLLs: codesign treats this
    # directory as nested code. Non-Mach-O signatures use extended attributes,
    # which ditto preserves in the ZIP. Sign the main executable via the bundle
    # LAST, because codesign recognizes it as the bundle and validates siblings.
    # Ad-hoc signing needs no Apple credentials or hardened-runtime entitlements.
    macho_headers = {bytes.fromhex(h) for h in (
        'feedface', 'cefaedfe', 'feedfacf', 'cffaedfe', 'cafebabe', 'bebafeca',
        'cafebabf', 'bfbafeca')}
    expected_arch = 'arm64' if args.rid == 'osx-arm64' else 'x86_64'
    for path in sorted(binaries.rglob('*')):
        if not path.is_file() or path == binaries / 'Gitanas':
            continue
        with path.open('rb') as stream:
            native = stream.read(4) in macho_headers
        if native:
            run('lipo', str(path), '-verify_arch', expected_arch)
        run('codesign', '--force', '--sign', '-', str(path))
    run('lipo', str(binaries / 'Gitanas'), '-verify_arch', expected_arch)
    run('plutil', '-lint', str(contents / 'Info.plist'))
    run('codesign', '--force', '--sign', '-', str(app))
    run('codesign', '--verify', '--deep', '--strict', '--verbose=2', str(app))

    dist = work / 'dist'
    dist.mkdir(exist_ok=True)
    from dmg_package import build_disk_image

    archive = dist / f'Gitanas-{args.version}-macos-{args.rid.removeprefix("osx-")}.dmg'
    build_disk_image(app, archive, work / 'preview')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--rid', choices=('osx-arm64', 'osx-x64'), required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--work-dir', type=Path, required=True)
    parser.add_argument('--bundle-only', action='store_true')
    package(parser.parse_args())
