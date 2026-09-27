#!/usr/bin/env python3
"""Build framework-dependent Windows release ZIPs, including the native helpers."""

import argparse
import hashlib
import json
import re
import shutil
import struct
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path
from xml.dom import minidom

ROOT = Path(__file__).resolve().parents[2]


def enable_portable_mode(config_file):
    # ElementTree moves assemblyBinding's namespace declaration to the root,
    # where System.Configuration rejects it as an unrecognized attribute.
    config = minidom.parse(str(config_file))
    settings = [node for node in config.getElementsByTagName('setting')
                if node.getAttribute('name') == 'IsPortable']
    if len(settings) != 1:
        raise RuntimeError('Missing or ambiguous portable-settings switch')
    value = settings[0].getElementsByTagName('value')[0]
    for child in list(value.childNodes):
        value.removeChild(child)
    value.appendChild(config.createTextNode('True'))
    config_file.write_bytes(config.toxml(encoding='utf-8'))


def run(*args):
    subprocess.run([str(arg) for arg in args], cwd=ROOT, check=True)


def pe_machine(path, gui=False):
    with path.open('rb') as stream:
        if stream.read(2) != b'MZ':
            raise RuntimeError(f'Not a Windows executable: {path}')
        stream.seek(0x3c)
        offset = struct.unpack('<I', stream.read(4))[0]
        stream.seek(offset)
        if stream.read(4) != b'PE\0\0':
            raise RuntimeError(f'Invalid PE signature: {path}')
        machine = struct.unpack('<H', stream.read(2))[0]
        if gui:
            stream.seek(offset + 24 + 68)
            if struct.unpack('<H', stream.read(2))[0] != 2:
                raise RuntimeError(f'Expected a GUI apphost with the runtime-install dialog: {path}')
        return machine


def assemble(work, rid, version):
    package = work / 'Gitanas'
    shutil.copytree(work / 'publish', package)
    build = work / 'build/bin/GitExtensions/net10.0'
    for name in ('BugReporter.exe', 'BugReporter.dll', 'BugReporter.deps.json',
                 'BugReporter.runtimeconfig.json'):
        shutil.copy2(work / 'reporter-publish' / name, package / name)
    shutil.copytree(build / 'Translation', package / 'Translation', dirs_exist_ok=True)
    shutil.copytree(ROOT / 'setup/assets/Dictionaries', package / 'Dictionaries')
    for project in sorted((ROOT / 'src/plugins').rglob('GitExtensions.Plugins.*.csproj')):
        name = project.stem.removeprefix('GitExtensions.Plugins.')
        source = build / 'Plugins' / name
        if not (source / f'{project.stem}.dll').is_file():
            raise RuntimeError(f'Missing plugin: {project.stem}')
        destination = package / 'Plugins' / name
        destination.mkdir(parents=True, exist_ok=True)
        for library in source.glob('*.dll'):
            if not (package / library.name).exists():
                shutil.copy2(library, destination)

    for folder, names in (
        ('GitExtSshAskPass', ('GitExtSshAskPass.exe',)),
        ('GitExtensionsShellEx', ('GitExtensionsShellEx32.dll', 'GitExtensionsShellEx64.dll')),
    ):
        for name in names:
            shutil.copy2(work / 'native/bin' / folder / name, package / name)

    # Keep the architecture-specific native payload. Some NuGet packages copy
    # content directly and bypass the SDK's normal runtime-identifier filtering.
    for runtime in (package / 'runtimes').glob('*'):
        if runtime.is_dir() and runtime.name not in (rid, 'win'):
            shutil.rmtree(runtime)
    other_arch = 'x64' if rid == 'win-arm64' else 'arm64'
    shutil.rmtree(package / other_arch, ignore_errors=True)
    for path in package.rglob('*.pdb'):
        path.unlink()
    for path in package.glob('*.xml'):
        path.unlink()

    # ZIPs are portable: settings travel with the extracted directory. Unzip to
    # a writable directory, not Program Files. No registry/shell registration is run.
    enable_portable_mode(package / 'Gitanas.dll.config')

    shutil.copy2(ROOT / 'LICENSE.md', package / 'LICENSE.md')
    notices = package / 'licenses/Avalonia.Wayland'
    notices.mkdir(parents=True)
    for name in ('LICENSE.md', 'NOTICE.md'):
        shutil.copy2(ROOT / 'third_party/avalonia-wayland' / name, notices / name)
    (package / 'README.txt').write_text(
        f'Gitanas {version} ({rid})\n\n'
        'Extract the entire ZIP into a writable folder, then run Gitanas.exe.\n'
        f'Requires the {rid.removeprefix("win-")} .NET 10 Runtime (Microsoft.NETCore.App).\n'
        'If it is missing, the launcher offers a link to download it; install it and launch again.\n'
        'The SDK and Windows Desktop Runtime are not required.\n'
        'Runtime downloads: https://dotnet.microsoft.com/download/dotnet/10.0\n'
        'Install Git separately: https://git-scm.com/downloads/win\n'
        'Settings are stored alongside this portable application.\n'
        'These binaries are not code-signed.\n', encoding='utf-8')

    expected = 0xaa64 if rid == 'win-arm64' else 0x8664
    for name in ('Gitanas', 'BugReporter'):
        if pe_machine(package / f'{name}.exe', gui=True) != expected:
            raise RuntimeError(f'Wrong architecture: {name}')
        runtime = json.loads((package / f'{name}.runtimeconfig.json').read_text())['runtimeOptions']
        framework = runtime.get('framework', {})
        if ('includedFrameworks' in runtime or framework.get('name') != 'Microsoft.NETCore.App'
                or framework.get('version') != '10.0.0'):
            raise RuntimeError(f'{name} must depend on the shared .NET 10 runtime')
    for name in ('coreclr.dll', 'hostfxr.dll', 'System.Private.CoreLib.dll'):
        if (package / name).exists():
            raise RuntimeError(f'The ZIP must not bundle the .NET runtime: {name}')
    for name in ('libSkiaSharp.dll', 'libHarfBuzzSharp.dll', 'libonigwrap.dll', f'{rid.removeprefix("win-")}/OpenConsole.exe',
                 'OCTICONS-LICENSE.txt', 'Translation/English.xlf', 'Dictionaries/en-US.dic'):
        if not (package / name).is_file():
            raise RuntimeError(f'Missing package content: {name}')
    for name in ('libSkiaSharp.dll', 'libHarfBuzzSharp.dll', 'libonigwrap.dll',
                 'conpty.dll', f'{rid.removeprefix("win-")}/OpenConsole.exe'):
        if pe_machine(package / name) != expected:
            raise RuntimeError(f'Wrong native dependency architecture: {name}')
    # The existing x64 native build intentionally uses a separate 32-bit askpass
    # process; ARM64 builds use a native ARM64 helper.
    askpass_arch = 0xaa64 if rid == 'win-arm64' else 0x14c
    if pe_machine(package / 'GitExtSshAskPass.exe') != askpass_arch:
        raise RuntimeError('Wrong SSH helper architecture')
    if pe_machine(package / 'GitExtensionsShellEx64.dll') != expected:
        raise RuntimeError('Wrong Explorer extension architecture')
    for dependency in (work / 'reporter-publish').glob('*.dll'):
        if not (package / dependency.name).is_file():
            raise RuntimeError(f'Missing crash reporter dependency: {dependency.name}')

    dist = work / 'dist'
    dist.mkdir()
    archive = dist / f'Gitanas-{version}-windows-{rid.removeprefix("win-")}-framework-dependent.zip'
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as output:
        for path in sorted(package.rglob('*')):
            if path.is_file():
                output.write(path, Path('Gitanas') / path.relative_to(package))
    checksum = hashlib.sha256(archive.read_bytes()).hexdigest()
    archive.with_suffix('.zip.sha256').write_text(f'{checksum}  {archive.name}\n')
    print(f'Created {archive} ({archive.stat().st_size // 1024 // 1024} MiB)', flush=True)


def build(args):
    if not re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?', args.version):
        raise ValueError('Expected a version such as 0.1.0 or 0.1.0-preview.1')
    if sys.platform != 'win32' and not args.managed_only:
        raise RuntimeError('Native helper compilation requires Windows and Visual Studio C++ tools')
    work = ROOT / 'artifacts/windows' / args.rid
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
    common = ('-c', 'Release', '-r', args.rid, '--self-contained', 'false',
              '-p:RuntimeFrameworkVersion=10.0.0', '-p:TargetLatestRuntimePatch=false',
              '-p:PublishSingleFile=false', '-p:PublishTrimmed=false')
    run('dotnet', 'publish', 'src/app/GitExtensions/GitExtensions.csproj', *common,
        '-p:ReleaseArchive=true', f'-p:ArtifactsDir={work}/build/', '-o', work / 'publish')
    run('dotnet', 'publish', 'src/app/BugReporter/BugReporter.csproj', *common,
        '-p:IsPublishable=true', '-p:UseAppHost=true',
        f'-p:ArtifactsDir={work}/reporter-build/', '-o', work / 'reporter-publish')
    if args.managed_only:
        print(f'Managed cross-publish complete: {work}. No release ZIP was created.')
        return
    run('dotnet', 'build', 'src/native/build.proj', '-c', 'Release',
        f'-p:TargetPlatform={args.rid.removeprefix("win-")}', f'-p:ArtifactsDir={work}/native/')
    assemble(work, args.rid, args.version)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('rid', choices=('win-x64', 'win-arm64'))
    parser.add_argument('version')
    parser.add_argument('--managed-only', action='store_true',
                        help='Cross-publish for inspection only; do not build native helpers or create a ZIP')
    build(parser.parse_args())
