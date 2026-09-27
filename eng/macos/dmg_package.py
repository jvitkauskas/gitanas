"""Create and verify a styled, Retina-ready disk image from a signed app bundle."""

import hashlib
import os
import plistlib
import subprocess
from pathlib import Path

import dmgbuild
from ds_store import DSStore


def run(*args):
    subprocess.run([str(arg) for arg in args], check=True)


def build_disk_image(app, archive, preview):
    artwork = Path(__file__).resolve().parent / 'dmg'
    volume = 'Gitanas Installer'
    locations = {'Gitanas.app': (184, 210), 'Applications': (536, 210)}
    dmgbuild.build_dmg(str(archive), volume, settings={
        'format': 'UDZO',
        'filesystem': 'HFS+',
        'files': [str(app)],
        'symlinks': {'Applications': '/Applications'},
        'icon': str(app / 'Contents/Resources/gitanas.icns'),
        'background': str(artwork / 'background.png'),
        # Hiding the extension with SetFile adds com.apple.FinderInfo to the
        # signed bundle, which codesign --strict rejects. Leave it untouched.
        'icon_locations': locations,
        'window_rect': ((160, 160), (720, 500)),
        'default_view': 'icon-view',
        'icon_size': 96,
        'text_size': 14,
        'grid_spacing': 80,
        'show_icon_preview': False,
        'show_status_bar': False,
        'show_toolbar': False,
        'show_pathbar': False,
        'show_sidebar': False,
    })
    run('hdiutil', 'verify', archive)
    mounted = plistlib.loads(subprocess.check_output([
        'hdiutil', 'attach', '-readonly', '-nobrowse', '-plist', str(archive)]))
    mount = Path(next(e['mount-point'] for e in mounted['system-entities'] if 'mount-point' in e))
    try:
        if os.readlink(mount / 'Applications') != '/Applications':
            raise RuntimeError('Missing Applications drop target')
        if not (mount / '.background.tiff').is_file():
            raise RuntimeError('Missing Retina background')
        with DSStore.open(str(mount / '.DS_Store'), 'r') as store:
            for name, position in locations.items():
                if tuple(store[name]['Iloc'][:2]) != position:
                    raise RuntimeError(f'Wrong icon position: {name}')
            if store['.']['icvp']['backgroundType'] != 2:
                raise RuntimeError('Finder background is not configured')
        run('codesign', '--verify', '--deep', '--strict', '--verbose=2', mount / 'Gitanas.app')
        # Test the same copy-out operation as dragging into Applications, without
        # installing into the runner's actual /Applications directory.
        installed = app.parent / 'dmg-installed/Gitanas.app'
        run('ditto', mount / 'Gitanas.app', installed)
        run('codesign', '--verify', '--deep', '--strict', '--verbose=2', installed)
        # A preview is useful for visual QA, but screen-capture permission is not
        # required to produce an otherwise verified disk image.
        preview.mkdir(parents=True, exist_ok=True)
        script = '''on run argv
            tell application "Finder"
                open (POSIX file (item 1 of argv))
                activate
            end tell
            delay 3
        end run'''
        try:
            opened = subprocess.run(['osascript', '-e', script, str(mount)], timeout=30)
            if opened.returncode == 0:
                captured = subprocess.run(['screencapture', '-x', str(preview / 'finder.png')], timeout=15)
                if captured.returncode != 0:
                    print('Finder screenshot unavailable on this runner', flush=True)
        except subprocess.TimeoutExpired:
            print('Finder preview timed out; disk image verification succeeded', flush=True)
    finally:
        run('hdiutil', 'detach', mount)
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    archive.with_suffix('.dmg.sha256').write_text(f'{digest}  {archive.name}\n')
    print(f'Created {archive} ({archive.stat().st_size / 1024**2:.1f} MiB)', flush=True)
