#!/usr/bin/env python3
"""Extract a release archive and check native dependencies and GUI startup.

Run inside an X11 or Wayland session (CI uses xvfb-run). Requires Python 3.12+.
"""

import argparse
import hashlib
import os
import platform
import subprocess
import tarfile
import tempfile
import time
from pathlib import Path


def check(archive):
    checksum = archive.with_suffix('.gz.sha256').read_text().split()[0]
    if hashlib.sha256(archive.read_bytes()).hexdigest() != checksum:
        raise RuntimeError('Archive checksum mismatch')
    arch = {'x86_64': 'x64', 'aarch64': 'arm64'}.get(platform.machine())
    if arch is None or not archive.name.endswith(f'-linux-{arch}.tar.gz'):
        raise RuntimeError('Startup checks require a host matching the archive architecture')
    with tempfile.TemporaryDirectory(prefix='gitanas release check ') as temporary:
        root = Path(temporary)
        with tarfile.open(archive) as source:
            source.extractall(root, filter='data')
        app = root / 'Gitanas'
        for binary in [app / 'Gitanas', app / 'BugReporter', *sorted(app.rglob('*.so'))]:
            # LTTng's optional tracing provider is not needed to run the app and
            # may target a different LTTng ABI than the distribution provides.
            if binary.name == 'libcoreclrtraceptprovider.so':
                continue
            result = subprocess.run(['ldd', str(binary)], text=True, capture_output=True)
            if result.returncode or 'not found' in result.stdout:
                raise RuntimeError(f'Unresolved native dependencies: {binary}\n{result.stdout}{result.stderr}')
        env = os.environ.copy()
        for key, folder in (('XDG_CONFIG_HOME', 'config'), ('XDG_DATA_HOME', 'data'),
                            ('XDG_CACHE_HOME', 'cache'), ('DOTNET_ROOT', 'empty-dotnet'),
                            (f'DOTNET_ROOT_{arch.upper()}', 'empty-dotnet')):
            destination = root / folder
            destination.mkdir(exist_ok=True)
            env[key] = str(destination)
        env['GIT_CONFIG_GLOBAL'] = str(root / 'gitconfig')
        env['GIT_CONFIG_NOSYSTEM'] = '1'
        # Keep the caller's display/session environment; no changes to desktop or
        # real preferences. Launch from outside the app directory, also with spaces.
        for name, seconds in (('Gitanas', 15), ('BugReporter', 5)):
            with tempfile.TemporaryFile(mode='w+') as log:
                process = subprocess.Popen([str(app / name)], cwd=root, env=env,
                                           stdout=log, stderr=subprocess.STDOUT)
                try:
                    deadline = time.monotonic() + seconds
                    while time.monotonic() < deadline:
                        if process.poll() is not None:
                            log.seek(0)
                            raise RuntimeError(f'{name} exited ({process.returncode}):\n{log.read()}')
                        time.sleep(0.25)
                    maps = Path(f'/proc/{process.pid}/maps').read_text()
                    if str(app / 'libcoreclr.so') not in maps:
                        raise RuntimeError(f'{name} did not load the packaged .NET runtime')
                    print(f'{name}: startup passed using bundled .NET', flush=True)
                finally:
                    if process.poll() is None:
                        process.terminate()
                        try:
                            process.wait(timeout=5)
                        except subprocess.TimeoutExpired:
                            process.kill()
                            process.wait()
        print('Checksum, extraction, executable permissions, and native dependencies passed', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    check(parser.parse_args().archive.resolve())
