# UI improvements after the WinForms comparison — 2026-09-26

Implemented the layout and usability findings from a comparison against the official Git Extensions
7.2.1.7 WinForms portable application in the Windows 10 VM. The comparison used the same generated
repository on Windows and Ubuntu; its report and screenshots remain in the local VM evidence directory.
This batch starts from `c9595567d` on `avalonia`.

## Changes

- The Commit dialog divides the file and message/diff panes proportionally instead of reserving 380 pixels
  for files at every width. The action column sizes between 170 and 200 pixels, with wrapping labels and
  vertical scrolling. Previous messages, templates, Create branch and Options wrap onto additional rows
  instead of disappearing when the message pane is narrow. Existing commands and menus are retained.
- File filters offer the two WinForms regex examples and up to ten recent expressions per list instance.
  Enter or leaving the input remembers a nonempty, valid expression that matches files. Choosing a history
  item applies it; duplicates move to the front. Invalid expressions preserve the existing error and
  selection behavior and are not remembered. History is session-local, not persisted or shared between lists.
- Input hints use a readable muted foreground without Fluent's additional 50% opacity. Hyperlinks bind
  dynamically to the theme, including text constructed before visual attachment. Readable custom link colors
  are retained; low-contrast colors fall back to the better contrasting black or white against the host
  content and control surfaces.
- Branch, file and settings navigation panes use the host theme's `WindowBrush`, separating content from
  surrounding controls in light mode while following dark/custom theme colors.

## Automated validation

Release builds and all 18 test-project invocations were run sequentially on Linux and Windows with SDK
10.0.201, `--no-build --no-restore --blame-hang-timeout 3m`. Windows native UI integration tests completed
before manual interaction with that desktop.

| System | Passed | Skipped | Failed |
|---|---:|---:|---:|
| Linux host | 25,349 | 167 | 0 |
| Windows 10 Enterprise LTSC 2021, 19044.7725 | 25,742 | 40 | 0 |

Windows includes all 191 native UI integration tests. Sixteen new cases cover filter-history bounds,
deduplication, invalid/unmatched expressions, list isolation, input acceptance, selection preservation,
late-bound link resources, readable custom colors, actual light/dark template hint contrast, and Commit
command bounds/non-overlap at 700, 950 and 1200 pixels with both normal and longer translated labels.
Existing obsolete `Watermark` warnings remain; Windows also reports existing ConEmu warnings and Git
line-ending warnings from the temporary source overlay. No build errors occurred.

## Manual checks

Both guests used 1920×1080 at 100% scaling, portable scratch applications and the same dirty repository:
HEAD `1cc58c11089813f0f194e9e95cd715f03f5ea7b2`, one staged file, two unstaged modifications and one untracked file.
The Ubuntu 24.04.5 GNOME guest ran the native Wayland backend, with protocol tracing enabled.

- Light/dark main and Commit panes on both guests, plus settings navigation in Ubuntu light/dark and Windows
  dark: readable hints and links, distinct content surfaces, visible selection and focus. On Windows, OS
  title-bar colors remain controlled by the OS.
- Commit commands remain visible at the 700-pixel minimum and the sampled intermediate/default sizes.
  The previously clipped Create branch command opens its dialog at minimum width on Wayland; cancellation
  returns to Commit without changing the repository.
- File-filter history opens by pointer and F4, shows examples and accepted filters, and applies a chosen
  expression. Clearing restores the file list. Invalid regex handling and staged/unstaged isolation were
  also exercised. F4 opens history while the filter has focus; Alt+Down retains the containing dialog's
  existing next-file/change navigation rather than competing for that shortcut.
- Five additional Commit/Settings cycles with menu/tooltip interaction initially completed without a
  protocol exception. The same popup-order failure recurred during a later dismissal sequence; see below.

This pass does not repeat every external-tool/Git workflow from the VM reports, test physical or mixed-DPI
displays, cover every translation, or add macOS verification. Layout tests use longer German labels as a
stress case; they do not constitute a complete German translation audit.

## Open Wayland issue

The preceding comparison observed one Mutter disconnection at approximately 17:29:55 UTC:

```text
xdg_wm_base@7: error 2: destroyed popup not top most popup
Avalonia.Wayland.AvaloniaWaylandProtocolErrorException: protocol error
```

The sequence involved Commit dismissal, viewer/menu/tooltip interaction and Settings. It recurred once in
this batch's `linux-final-dark.log`, after the initial five-cycle check, during a later dismissal sequence.
The new trace records `xdg_toplevel@90.close()`, then destruction of that toplevel and its `xdg_surface@83`,
followed by `xdg_popup@93.destroy()`; Mutter reports the popup-order error immediately afterwards. The trace
is stronger evidence of the teardown ordering, but the exact deterministic UI trigger remains unknown.

No change to the vendored Wayland backend or claim of a popup-lifetime fix is included here; the existing
key-repeat patch addresses a separate defect. This remains a native Wayland stability limitation. The existing
`GITEXTENSIONS_USE_WAYLAND=0` X11 override remains available. No upstream message was sent during this batch.

## Local evidence and reuse

Under `/home/julius/VirtualMachines/gitextensions-qa/`:

- `comparison/UI-comparison.md` and `.html`: original official WinForms comparison, screenshot pairs and
  the earlier crash evidence in `comparison/evidence/`.
- `ui-improvements/linux/`: build/test logs, TRX results, totals, `wayland-logs.tar.gz` and
  `popup-crash-context.log` with the newly captured failing teardown sequence.
- `ui-improvements/windows-evidence.zip` and `windows-totals.json`: Windows build and all test results.
- `ui-improvements/screenshots/`: 19 selected captures from the improved application. Original captures,
  including intermediate harness/setup states, remain in the guest-specific evidence directories.

Reusable applications are `/home/qa/qa/comparison/app` in Ubuntu and
`C:\QA\comparison\avalonia-improved` in Windows. Their launchers are the existing Linux `launch.py` and
`C:\QA\comparison\run-avalonia-improved.ps1`; both redirect writable preferences and Git configuration
to scratch homes. The original official WinForms reference and previous cold checkpoints are retained.

All tested Git Extensions/BugReporter processes were closed and both guests shut down after testing. Both
fixture HEADs and dirty-file states stayed unchanged. The normal Windows qa settings directory and
`HKCU\Software\GitExtensions` remain absent. The host Git config SHA-256 remains
`c13840001af4a4654f5684a0b06d4444e4e66ee98c62e3154eb9061e4280358f`; host display settings were not changed.
