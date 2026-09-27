# Selected improvements from the other Avalonia port

Reviewed Nikola (@begota98)'s [PR #13189](https://github.com/gitextensions/gitextensions/pull/13189)
at `a3133e50d080bc03654c2e17a367e06807126e45`, against this fork's
`258cc6df1ee570e12732cf742d535a5af2960edc`. The process-group idea, saved-image comparisons
and native-compositor test approach below are adapted from that work. We retain this fork's
presentation layer and current Avalonia/Wayland packages.

## Changes

- **Cancellation:** the plain-text console starts Linux commands through `setsid` when
  available. Cancellation traverses descendants, and signals a process group only when
  the canceled command is its leader. Commands sharing the application's group use
  descendant traversal only. The shared `Executable` cancellation/disposal path now
  kills descendants too. On macOS this pass uses descendant traversal; it does not add
  the other port's native process-group launcher. Windows console Ctrl+C behavior remains.
- **Accessibility:** named controls receive stable automation IDs unless explicitly set;
  string tooltips supply accessible help text. Icon-button accessible names follow tooltip
  updates instead of retaining the original text. Explicit names, help and IDs are preserved.
- **Graph regression tests:** 24 reviewed reference images cover linear, merge and crossing
  histories, light/dark backgrounds, and 100/125/150/200% rendering. Comparisons use actual
  pixels, fail for missing references, and attach actual/difference images. Generation is
  explicitly opt-in and reports inconclusive rather than passing. The fixtures are font-free
  and use fixed colors, so they check renderer behavior independently of control themes.
- **Wayland smoke harness:** `eng/WaylandSmoke` hosts the real rename dialog and production
  backend initialization in an isolated headless Sway session. Classic/Modern × light/dark
  runs move between two real Wayland outputs at 125% and 150%, check modal ownership/task
  completion, and verify stable logical size after a scale round trip. The app is not
  instrumented and the test is outside the main solution.

## Validation on Linux

Host: Omarchy/Arch, .NET SDK 10.0.111. Linux commands here use `-p:UseAppHost=false` because
this distribution's SDK identifies its default apphost RID as `arch-x64`, for which NuGet
has no Microsoft runtime pack. No production build configuration was changed for this.

- Full **Release Avalonia test project: 1,293 passed, 2 skipped, 0 failed**. Both skipped
  cases are Windows-specific. The console undercounts skips; numbers are from the TRX.
- Focused **Release process tests: 10 passed, 0 failed**, including six new cases for
  private/inherited groups, real child cancellation, raw/ArgumentList quoting with an
  executable path containing spaces, missing launcher lookup, already-exited handling,
  and shared Executable cancellation. The isolated-group case also checks `/proc` to
  verify that the wrapper retains the command PID as group leader.
- Focused Debug graph/accessibility run: **27 passed**.
- Full Debug Avalonia run: 1,289 passed, 2 skipped, 4 failed. All four failures hit the
  existing AppSettings debug assertion that testhost.dll must point to GitExtensions.exe,
  while settings-page tests resolve the dictionary/resource directory. They are outside
  the changed paths; Release passes all four. The failure remains documented rather than
  relaxing the assertion as part of this change.
- Native Wayland harness: **4/4 scenarios passed** with XWayland disabled, DISPLAY unset,
  actual RenderScaling transitions 1.25 → 1.5 → 1.25, an owned modal on the other output,
  completed modal task on close, and native `wl_surface` protocol evidence. Classic/light
  and Modern/dark compositor screenshots were visually inspected. All 24 graph reference
  images were also inspected in a contact sheet.
- The native harness builds with **0 warnings, 0 errors**. Test-project builds report
  existing Avalonia `Watermark` deprecation warnings. `git diff --check` passes.

The initial native harness failed because the installed Sway binary has `cap_sys_nice`
and this host's realtime CPU limit is zero. Tracing removed the capability and exposed the
cause. The final harness uses `setpriv --no-new-privs`, so its test compositor cannot enable
privileged realtime scheduling. The successful four-scenario run used no tracing.
Sway, swaybg and strace were installed while diagnosing this. Final harness requirements
are Sway/swaymsg, setpriv (util-linux), .NET, and optionally grim; swaybg/strace are not used.
No user display settings were changed and no test compositor remains running.

## Reproduction and limits

```sh
dotnet test tests/app/UnitTests/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  -c Release -p:UseAppHost=false

dotnet test tests/app/UnitTests/GitCommands.Tests/GitCommands.Tests.csproj \
  -c Release -p:UseAppHost=false \
  --filter 'FullyQualifiedName~ProcessExtensionsTests|FullyQualifiedName~ExecutableTests'

python3 eng/WaylandSmoke/run.py /tmp/gitextensions-wayland-results
```

Evidence from this run is under
`/home/julius/VirtualMachines/gitextensions-qa/pr13189-adoption/`, including `wayland/summary.json`,
stage JSON reports, compositor screenshots, and the graph contact sheet. TRX results are
in `artifacts/Release/TestsResults/adoption-avalonia-release.trx` and
`adoption-process-release.trx`.

These are Linux results, not Windows/macOS validation. The Wayland host tests a real dialog,
not a complete repository workflow, physical input blocking or screen-reader operation.
Graph references cover the renderer, not the full browser/commit-window appearance. Native
file-type icons, Flatpak packaging and a portal-only picker policy are not included in this
pass. Cancellation after a parent has already exited, or children that deliberately detach
into a different session, is not guaranteed by this change.
