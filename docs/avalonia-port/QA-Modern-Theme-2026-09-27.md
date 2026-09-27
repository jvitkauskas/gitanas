# Modern control theme: Linux and Windows visual QA (2026-09-27)

## Build and environments

Pulled `avalonia` from `3e338856b` to `0c266d37f`, including the five Modern theme commits. No application code was
changed during this pass. Modern was loaded from the saved `AvaloniaControlTheme=modern` setting, without
`GE_AVALONIA_THEME`. The built-in `light` and `dark` color themes were tested separately after restarting.

| System | Execution | Visual result at 1920 × 1080, 100% |
|---|---|---|
| Ubuntu 24.04.5 LTS, GNOME | Native Wayland VM | Light and dark main windows, diff, settings and menus render correctly; dark dashboard and narrow main window checked |
| Fedora 44, KDE Plasma | Native Wayland VM | Light and dark main windows and Commit dialogs render correctly; dark diff and Push dialog also inspected |
| Windows 10 Enterprise LTSC 2021, build 19044.7725 | Native Windows VM | Light and dark main windows, diffs, Commit dialogs and dashboards render correctly; dark settings, menus and narrow Commit dialog checked |

Linux applications were built on the Arch host and copied into both VMs. `DISPLAY` was removed before launch;
Wayland protocol logs show the actual `wl_display`/`xdg_wm_base` connection. Windows was built independently in
`C:\QA\modern-source` at the same commit. All runs used portable copies, isolated settings/HOME and Git configuration,
and scratch repositories. The older commit shown in history screenshots belongs to the fixture, not the application build.

## Visual checks and observations

- The full-height sidebar, separate filter row and relocated toolbar retain their layout in both color themes.
  The monochrome action icons are present, with colored file-state and repository-state indicators. Logos and other
  assets with no Modern replacement intentionally retain their original artwork.
- Selected tabs have a visible filled segment. History selection, staged/unstaged selections, added/deleted diff
  lines, menus, checkboxes, text fields, links and commit-message completion remain distinguishable.
- Switching Commit/Diff tabs, dragging the main history/details splitter, opening Settings and Commit from the
  toolbar, selecting staged files, entering a draft message and navigating to the dashboard worked.
  No commit or remote operation was performed for this visual pass.
- The GNOME main window remains usable at approximately 930 pixels wide: the toolbar wraps, and hiding the sidebar
  restores history space. The Windows Commit dialog at roughly 900 × 700 keeps the commit buttons visible and wraps
  the message toolbar. Long diff lines use horizontal scrolling. The KDE Commit dialog was inspected at its default
  size; attempted synthetic resizing did not establish a smaller-window check there.
- **Minor contrast polish:** enabled unselected tabs and the small toolbar captions look faint in light mode and
  can resemble disabled controls. `ModernSecondaryTextBrush` uses `#8A000000`, with an additional `0.85` opacity on
  unselected tab content. Darkening the light secondary text would improve discoverability. No theme styles were
  changed as part of this test.
- **Platform appearance:** Windows and KDE retain light system decorations even with dark application colors in
  these desktop configurations. The toolbar stays below the system title bar on Linux/Windows, and the sidebar is
  opaque. The integrated title bar and vibrancy are macOS-only by design, rather than missing Linux/Windows features.
- Saving a light-to-dark change in GNOME and manually relaunching the unchanged saved settings loaded the dark
  theme correctly. The restart confirmation appeared, but the automated Yes/Enter attempts were inconclusive;
  choosing No and restarting manually was used. Automatic restart is not claimed as verified.

No Modern-specific crash, missing-icon defect, overlapping-control blocker or unusable color combination was
established in these views. This is visual smoke coverage, not a complete workflow or accessibility certification.
HiDPI/fractional scaling, multiple monitors, X11, Windows 11, localized layouts and macOS were not rerun in this pass.

## Automated validation

Both Release solution builds succeeded with **7 warnings and 0 errors** (existing ConEmuInside and obsolete
Watermark warnings). All 18 test-project invocations completed on each build:

| Build | Passed | Skipped | Failed |
|---|---:|---:|---:|
| Linux | 25,386 | 167 | 0 |
| Windows | 25,766 | 43 | 0 |

Windows includes **191 UI integration tests**; that project discovers no tests on Linux. Counts above are summed
from TRX counters; skipped is `total - executed`. The new Modern layout/command and icon-loader tests pass.
Windows suites ran sequentially with hang detection and a per-project watchdog, before the manual Windows check.
The automated suites are broader regression checks; they do not substitute for the manual Modern screenshots.

## Evidence and cleanup

Local evidence is retained outside Git:

- `/home/julius/VirtualMachines/gitextensions-qa/modern-theme-2026-09-27/`: launch/build/test scripts, Linux logs/TRX,
  Windows test evidence ZIP and normalized logs/TRX, Wayland logs, environment and cleanup records.
- `/home/julius/Pictures/GitExtensions-Modern-2026-09-27/`: 23 selected, unmodified desktop screenshots and an index.
- `/home/julius/Pictures/GitExtensions-Modern-2026-09-27.zip`: the same shareable screenshot set.

The test application instances and private D-Bus/portal helpers were stopped, and the Modern Windows launch/test
scheduled tasks were removed. VM disks, portable copies, test fixtures and evidence remain for reproduction. No
new software was installed and no display scaling or desktop theme preferences were changed during this pass.
