#!/usr/bin/env python3
"""Build self-contained Linux x64/ARM64 release archives."""

import argparse
import hashlib
import json
import re
import shutil
import struct
import subprocess
import tarfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def run(*args):
    subprocess.run([str(arg) for arg in args], cwd=ROOT, check=True)


def package(work, rid, version):
    app = work / 'Gitanas'
    shutil.copytree(work / 'publish', app)
    build = work / 'build/bin/GitExtensions/net10.0'
    for name in ('BugReporter', 'BugReporter.dll', 'BugReporter.deps.json',
                 'BugReporter.runtimeconfig.json'):
        shutil.copy2(work / 'reporter-publish' / name, app / name)
    shutil.copytree(build / 'Translation', app / 'Translation', dirs_exist_ok=True)
    shutil.copytree(ROOT / 'setup/assets/Dictionaries', app / 'Dictionaries')
    for project in sorted((ROOT / 'src/plugins').rglob('GitExtensions.Plugins.*.csproj')):
        name = project.stem.removeprefix('GitExtensions.Plugins.')
        source = build / 'Plugins' / name
        if not (source / f'{project.stem}.dll').is_file():
            raise RuntimeError(f'Missing plugin: {project.stem}')
        destination = app / 'Plugins' / name
        destination.mkdir(parents=True, exist_ok=True)
        for library in source.glob('*.dll'):
            if not (app / library.name).exists():
                shutil.copy2(library, destination)

    # ConEmu/ConPTY content bypasses the SDK's RID filtering. Linux uses Porta.Pty.
    for name in ('ConEmu', 'x64', 'arm64'):
        shutil.rmtree(app / name, ignore_errors=True)
    for runtime in (app / 'runtimes').glob('*'):
        if runtime.is_dir() and runtime.name not in (rid, 'linux', 'unix'):
            shutil.rmtree(runtime)
    for path in app.rglob('*'):
        if path.is_file() and path.suffix in ('.exe', '.pdb', '.dylib'):
            path.unlink()
    for path in app.glob('*.xml'):
        path.unlink()

    shutil.copy2(ROOT / 'LICENSE.md', app / 'LICENSE.md')
    shutil.copy2(ROOT / 'setup/assets/Logo/gitanas/gitanas-256.png', app / 'gitanas.png')
    notices = app / 'licenses/Avalonia.Wayland'
    notices.mkdir(parents=True)
    for name in ('LICENSE.md', 'NOTICE.md'):
        shutil.copy2(ROOT / 'third_party/avalonia-wayland' / name, notices / name)

    configs = []
    for name in ('Gitanas', 'BugReporter'):
        config = json.loads((app / f'{name}.runtimeconfig.json').read_text())['runtimeOptions']
        if 'framework' in config or 'frameworks' in config:
            raise RuntimeError(f'{name} must not depend on a system .NET runtime')
        runtime = next((f for f in config.get('includedFrameworks', [])
                        if f['name'] == 'Microsoft.NETCore.App'), None)
        if runtime is None or not runtime['version'].startswith('10.0.'):
            raise RuntimeError(f'{name} must include the .NET 10 runtime')
        configs.append(runtime)
    if configs[0] != configs[1]:
        raise RuntimeError('Application and crash reporter must share the same runtime')
    assets = json.loads((work / 'build/obj/GitExtensions/project.assets.json').read_text())
    runtime_pack = next((Path(folder) / f'microsoft.netcore.app.runtime.{rid}' / configs[0]['version']
                         for folder in assets['packageFolders']
                         if (Path(folder) / f'microsoft.netcore.app.runtime.{rid}' /
                             configs[0]['version'] / 'LICENSE.TXT').is_file()), None)
    if runtime_pack is None:
        raise RuntimeError('Cannot find the bundled .NET runtime license')
    runtime_notices = app / 'licenses/dotnet'
    runtime_notices.mkdir()
    for name in ('LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT'):
        shutil.copy2(runtime_pack / name, runtime_notices / name)

    required = ('Gitanas', 'Gitanas.dll', 'BugReporter', 'libhostfxr.so', 'libhostpolicy.so',
                'libcoreclr.so', 'System.Private.CoreLib.dll', 'libSkiaSharp.so',
                'libHarfBuzzSharp.so', 'libonigwrap.so', 'libporta_pty.so',
                'Avalonia.Wayland.dll', 'OCTICONS-LICENSE.txt', 'Translation/English.xlf',
                'Dictionaries/en-US.dic', 'Gitanas.dll.config')
    for name in required:
        if not (app / name).is_file():
            raise RuntimeError(f'Missing archive content: {name}')
    for dependency in (work / 'reporter-publish').iterdir():
        if dependency.suffix in ('.dll', '.so') and not (app / dependency.name).is_file():
            raise RuntimeError(f'Missing crash reporter dependency: {dependency.name}')
    # Leave the configuration XML untouched. Settings belong in the user's XDG
    # directories, so replacing the application directory preserves preferences.
    config = ET.parse(app / 'Gitanas.dll.config')
    if config.find(".//setting[@name='IsPortable']/value").text != 'False':
        raise RuntimeError('Linux releases must use per-user settings')

    expected = 183 if rid == 'linux-arm64' else 62
    for path in app.rglob('*'):
        if not path.is_file():
            continue
        with path.open('rb') as stream:
            header = stream.read(20)
        native = header[:4] == b'\x7fELF'
        if native and (header[4:6] != b'\x02\x01' or struct.unpack('<H', header[18:20])[0] != expected):
            raise RuntimeError(f'Wrong native architecture: {path}')
        if (path.suffix == '.so' or path.name in ('Gitanas', 'BugReporter')) and not native:
            raise RuntimeError(f'Expected an ELF binary: {path}')
        path.chmod(0o755 if path.name in ('Gitanas', 'BugReporter', 'createdump') else 0o644)

    (app / 'README.txt').write_text(
        f'Gitanas {version} ({rid})\n\n'
        'Extract the complete archive, then run ./Gitanas from the extracted Gitanas folder.\n'
        '.NET 10 is included. Install Git separately using your distribution package manager.\n'
        'These builds target glibc-based desktop Linux, not Alpine/musl.\n'
        'Desktop graphics libraries, fonts, ICU and OpenSSL are still required.\n'
        'SSH, GPG, credential helpers, terminals and diff/edit tools use your system installations.\n'
        'Settings are stored in your user configuration/data directories, not alongside the app.\n'
        'Native Wayland is experimental; set GITEXTENSIONS_USE_WAYLAND=0 to use X11/XWayland.\n'
        'No system installation or automatic updates are performed.\n'
        'See https://github.com/jvitkauskas/gitanas/blob/main/eng/linux/README.md\n', encoding='utf-8')

    dist = work / 'dist'
    dist.mkdir()
    archive = dist / f'Gitanas-{version}-{rid}.tar.gz'

    def metadata(info):
        info.uid = info.gid = 0
        info.uname = info.gname = 'root'
        if info.isdir():
            info.mode = 0o755
        return info

    with tarfile.open(archive, 'w:gz', compresslevel=9) as output:
        output.add(app, arcname='Gitanas', filter=metadata)
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    archive.with_suffix('.gz.sha256').write_text(f'{digest}  {archive.name}\n')
    print(f'Created {archive} ({archive.stat().st_size / 1024**2:.1f} MiB)', flush=True)


def build(args):
    if not re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?', args.version):
        raise ValueError('Expected a version such as 0.1.0 or 0.1.0-preview.1')
    work = ROOT / 'artifacts/linux' / args.rid
    if work.exists():
        shutil.rmtree(work)
    work.mkdir(parents=True)
    solution = ROOT / 'GitExtensions.slnx'
    projects = [p.attrib['Path'] for p in ET.parse(solution).iter('Project')
                if p.attrib['Path'].endswith('.csproj') and not p.attrib['Path'].startswith('tests/')]
    filtered = work / 'Gitanas.slnf'
    filtered.write_text(json.dumps({'solution': {'path': str(solution), 'projects': projects}}))
    run('dotnet', 'build', filtered, '-c', 'Release', '-p:UseAppHost=false',
        f'-p:ArtifactsDir={work}/build/')
    common = ('-c', 'Release', '-r', args.rid, '--self-contained', 'true',
              '-p:RuntimeFrameworkVersion=', '-p:TargetLatestRuntimePatch=true',
              '-p:PublishSingleFile=false', '-p:PublishTrimmed=false')
    run('dotnet', 'publish', 'src/app/GitExtensions/GitExtensions.csproj', *common,
        '-p:ReleaseArchive=true', f'-p:ArtifactsDir={work}/build/', '-o', work / 'publish')
    run('dotnet', 'publish', 'src/app/BugReporter/BugReporter.csproj', *common,
        '-p:IsPublishable=true', '-p:UseAppHost=true',
        f'-p:ArtifactsDir={work}/reporter-build/', '-o', work / 'reporter-publish')
    package(work, args.rid, args.version)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('rid', choices=('linux-x64', 'linux-arm64'))
    parser.add_argument('version')
    build(parser.parse_args())
