# Windows API reachability audit (2026-09-27)

Reviewed native imports and their callers in the application and bundled plugins, plus registry, COM, DPAPI,
System.Drawing and Windows executable launch paths. Follow-up to the Linux error when opening a recent repository.

## Findings and fixes

| Trigger | Windows-only path | Resolution |
| --- | --- | --- |
| Open a recent or favorite repository | `GetKeyState` in `RepositoryHistoryUIService` | The preceding fix polls modifier keys only on Windows. Other systems switch the current window's repository. |
| Rename/delete a reference when the selected commit has multiple eligible references | `GetCursorPos` in the revision-grid quick picker | Query pointer coordinates only on Windows. Otherwise leave the position unset so Avalonia places the picker over its owner. Mark the import Windows-only for platform analysis. |
| Switch worktrees through an owned dialog | `GetAncestor` / `GetWindow` in `TrySetBrowseWorkingDir` | Follow Avalonia window owners off Windows, including synthetic Wayland handles. Preserve native owner traversal on Windows and centralize the imports in the platform-annotated hosting class. |
| Import settings containing a `plink.exe` or `TortoisePlink.exe` SSH path | PuTTY controls and recovery handlers could be enabled on Unix, eventually calling `cmd.exe` or Pageant | Enable the PuTTY integration only on Windows. Preserve the custom SSH command. Independently guard host-key recovery, which can also be requested by clone error handling. |

## Other paths reviewed

- Clipboard, DPI, monitor telemetry, avatar connectivity detection, long/short file paths and error-dialog ownership
  already select a portable backend or guard their native calls.
- Taskbar overlays, jump lists, native WebView2 hosting, ConEmu and Mintty are gated by platform support.
- Visual Studio integration is offered and invoked only when Windows installation detection succeeds.
- Registry settings and shell-extension registration UI are platform-gated. Font, color, file and message dialogs use
  the registered Avalonia dialog host on Unix.
- Credential storage selects the OS implementation. Build-server DPAPI storage is Windows-only.
- File-manager, Git GUI and script shell launches have Unix alternatives. The updater guards `msiexec.exe`, but its
  manual check still uses the upstream Windows release feed: the experimental port's distribution policy remains a
  separate limitation, as recorded in the settings review.

## Validation

- Release solution build succeeded (final incremental build: no warnings or errors).
- Final full suites: `GitUI.Tests` 20,155 passed / 2 skipped; `GitCommands.Tests` 3,392 passed / 1 skipped;
  `GitUI.Avalonia.Tests` 1,253 passed / 1 skipped. Total: 24,800 passed, 4 skipped, no failures.
- Linux application smoke tests used a disposable portable copy, temporary repositories/settings, private D-Bus
  session and Xvfb. No user settings or repositories were modified.
- Before the fixes, both the revision-grid rename/delete command handlers and the worktree owner-routing entry point
  reproduced `DllNotFoundException: user32.dll`.
- After the fixes, both reference pickers opened with the browse window as owner and no forced screen position, then
  cancelled without an error dialog. The worktree route resolved two nested owners, returned success and switched the
  browse window from `repo-b` to `repo-a`.
- Added headless regressions for nested ownership without native handles (the Wayland case), unknown/closed window
  identities, and accepting a reference from an owner-centered picker. Added imported PuTTY and Unix host-key
  recovery regressions.
- Full test-suite results are recorded in `artifacts/local-launch/platform-api-*-tests.log`; before/after application
  results are in `platform-api-before.log` and `platform-api-after.log` in the same directory.
- Application smoke harness: `/tmp/ge-platform-api-qa-20260927`. The recent/favorite repository smoke from the preceding
  fix is under `/tmp/ge-recent-repositories-qa-20260927`.

This is a source audit plus Linux runtime/headless validation, not proof that every possible external tool or plugin
configuration is portable. Windows and macOS were not run for this change; native Wayland ownership was covered by
the headless backend's synthetic handles rather than another desktop click-through.
