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

## Polish follow-up

The two appearance observations above were addressed after the initial QA commit (`4843a095c`):

- Light secondary text now uses 60% black instead of 54%, and unselected tabs no longer apply an extra 0.85 opacity
  on top of that brush. This also restores the intended icon opacity and improves dark unselected-tab contrast.
- Modern windows on Windows 10 20H1+ request dark/light native frames on opening and theme changes. The call is
  guarded by OS version and a real HWND; Windows 11 remains handled by Avalonia. A frame refresh is necessary on
  the tested Windows 10 VM: setting the DWM attribute alone returned success but left the old title-bar color.
  `SWP_FRAMECHANGED` also left active dialogs light; the final code repaints both caption states with
  [`WM_NCACTIVATE`](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-ncactivate), restoring the actual active
  state without changing focus. The opening refresh is queued after the host finishes opening/centering the window.
  This is the [Windows 10 workaround documented by Avalonia](https://docs.avaloniaui.net/troubleshooting/platform-specific-issues/windows#title-bar-stays-light-when-switching-to-dark-theme-on-windows-10),
  with a best-effort fallback if the DWM call fails.
- Modern on Wayland requests Avalonia-drawn decorations through the existing `ForceDrawnDecorations` backend
  option. This uses the same theme-aware frame on KDE and GNOME without changing desktop preferences. It replaces
  KDE's native button styling/order for Modern; other control themes keep the compositor's decorations. X11 and
  macOS decoration behavior are unchanged. The Avalonia option is experimental, like the Wayland backend itself.

The Wayland check confirmed light/dark main windows on GNOME and KDE, a dark Settings dialog on GNOME, and a dark
Commit dialog on KDE. KDE title-bar dragging, maximizing and bottom-right resizing worked; an
`xdg_toplevel.resize` request was observed during the resize check (the launch log was subsequently overwritten).
A smaller window wraps the toolbar without losing commands. Opening a repository with Ctrl+O and dismissing the modal with Escape returned to the main window without repeated reopening.

A separate Fluent launch on KDE retained the compositor frame, confirming the decoration choice is scoped to Modern.

Windows 10 final-build checks confirmed that active main windows and newly opened Settings dialogs immediately
show dark native title bars, without external DWM calls or switching focus away and back. Light main/Settings
windows retain light frames, and maximizing the dark main window works. A read-only DWM probe confirmed light
mode uses attribute value 0. Earlier diagnostic screenshots with white active captions are intermediate failures,
not final evidence. All three desktops were checked at 100%; the coverage limits of the initial pass still apply.

Evidence for this follow-up is under `/home/julius/VirtualMachines/gitextensions-qa/modern-polish-2026-09-27/`.
The 17 selected screenshots are in `/home/julius/Pictures/GitExtensions-Modern-Polish-2026-09-27/` and the adjacent
ZIP, with an index distinguishing Modern from the Fluent comparison. Settings checklist warnings in screenshots
reflect the deliberately isolated QA HOME, not theme regressions.

The final Linux Release build passed (5 obsolete-Watermark warnings, 0 errors), and all 18 suite invocations passed:
25,386 tests passed, 167 skipped, 0 failed. The final Windows Release build also passed (11 warnings, 0 errors;
7 existing warnings plus 4 line-ending warnings from the QA checkout).
The final Windows run passed all 18 suites: 25,766 passed, 43 skipped, 0 failed, including 191 native UI integration
tests. Counts use TRX `total - executed` for skipped. Final evidence is in `verified-linux/` and `windows-verified/`;
intermediate runs are retained separately.

The polish app instances and private Linux D-Bus/portal helpers were stopped. The Windows polish launch/build/test
tasks were removed after completion. Test VMs, fixtures and portable builds remain available; no display settings
or global desktop theme preferences were changed.
