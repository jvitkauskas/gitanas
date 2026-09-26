# Handoff: working on the Avalonia port

Notes for whoever continues the port (a person or an agent session), on any machine. The plans are `PLAN.md` (the
port, done) and `CROSS-PLATFORM.md` (macOS and Linux, next); `ledger.md` records every ported form and what differs.

## State (2026-09-25)

- Branch `avalonia` of the fork `jvitkauskas/gitextensions`: the Avalonia port is complete on Windows, WinForms is
  removed, and the parity gaps of the ledger are closed or recorded as deliberate. (`avalonia-3rdparty` stayed at the
  commit before the cross-platform work.)
- Phase 0 of `CROSS-PLATFORM.md` (guard rails and CI) is done: the solution builds on Linux, and CI has a Linux job.
- Phase 1 (portable core libraries) is done: GitExtensions.Extensibility, GitExtUtils, GitUIPluginInterfaces,
  GitCommands, ResourceManager, GitUI.RevisionGraph and GitUI.Presentation target `net10.0`, and their tests run on Linux.
  The "Platforms" section of the ledger says what runs where.
- Phase 2 (the application starts on Linux and macOS) is under way, approved by the owner. The spike of the modal
  message boxes passed on X11 (WSLg) and Windows. Done: `Avalonia.Desktop`, owners, message boxes, task dialogs, common
  dialogs, clipboard, system theme; all application projects target `net10.0`. Under WSLg the application starts,
  browses, diffs and commits. Every project, plugins and tests included, targets `net10.0`, and the tests run on Linux
  (all but `UI.IntegrationTests`). macOS: see "macOS" below.
- Phase 3 (git, tools and processes on each system) is done for Linux and macOS: the file manager, git discovery,
  editors, diff and merge tools, shells, scripts and plugins.
- Phase 4 (credentials, SSH, the Windows capabilities) is done for Linux and macOS: the askpass prompt of Git
  Extensions, the Secret Service and the Keychain for credentials, the Windows features hidden.
- Phase 5 (macOS and Linux conventions) is under way on macOS: Cmd, the application menu, the dialog buttons, fonts.
- Work in batches: each ends with a build, the full test suites (on Windows, and the portable ones on Linux), a commit,
  a fast-forward of `avalonia` and a push of it.

## macOS (2026-09-25)

The macOS pass of phases 2-5 ran on a Mac (Apple silicon, macOS 26, .NET SDK 10.0.401, Xcode). Windows and Linux are
checked separately by the owner: keep their behavior unchanged, put macOS-only changes behind `OperatingSystem.IsMacOS()`
where shared code would otherwise change, and say in each commit what could not be verified.

Done and checked there (the ledger's "Platforms" section has the details): the build and every test suite (the submodules
must be checked out); the native backend; browse, diff, blame, commit, push, pull, stash; message boxes, task dialogs, the
file and font pickers (native open panel as a sheet), the clipboard; the dark theme (it was unreadable off Windows); the
terminal tab (zsh); git from Homebrew; editors (TextEdit with `open -W -n -e` waits); diff and merge tools (bundles of
`/Applications`, FileMerge / `opendiff`, Beyond Compare's `bcomp`); `open` and `open -R`; the askpass prompt with
macOS's OpenSSH (`ssh-add` of a key with a passphrase); the Keychain (opt-in test); Cmd for the shortcuts; the
application menu (About, Settings with Cmd+,, Quit that closes the windows) and the main menu in the menu bar, with the
shortcuts, also over the other windows; the order of the dialog buttons; the
default UI font (13 pixels, it was 17); the scaling of Retina displays (`DpiUtil`); a merge in each installed merge tool
(FileMerge, KDiff3, Meld, P4Merge, Sublime Merge, Beyond Compare, VS Code); a push over SSH to an OpenSSH server (host
key and passphrase asked) and over HTTPS to GitHub; the credentials of a build server in the Keychain (a fake Jenkins);
the color picker of the remotes.

Left for macOS, in batches:

1. **Not checked**: Araxis (not installed), DiffMerge (its Homebrew cask is disabled), the credentials page of a plugin
   (no plugin of the repository has a `CredentialsSetting`; the build server credentials were checked).
2. **Phase 6**: the `.app` bundle, signing and notarization. The application cannot start while the display sleeps
   or the screen is locked (Avalonia.Native: "not able to start the RenderTimer", -6661).

Done in the last batch of phase 5: the order of the buttons of the settings (`StackPanel.dialogButtons`) and of the
askpass prompt (a dialog footer), the theme of the askpass prompt (`ThemeModule.Load` in the askpass mode), the sizes
with decimals of the font picker and the Fonts page (9.75), the defaults of the hotkeys on keys that macOS takes
(Commit Cmd+Return, the tabs Cmd+Shift+] / [, the multi-selection of the left panel Cmd+Shift+Space; saved hotkeys on
such keys are moved at load), the menu of the main window in the menu bar of the other windows (without shortcuts,
disabled while modal), the default extension of the save dialogs (`*..txt` in the filter of the file history). Checked
with the macOS fonts: the stash, commit, push, create branch and settings dialogs fit.

Seen on macOS and fixed on every system: Cmd/Ctrl+C in the revision grid copies the hashes of the selection again (the
DataGrid copied its empty template cells), the commit dialog gives the focus to the unstaged files, the message or Amend
once its files are loaded (as `LoadUnstagedOutput`), and `dotnet GitExtensions.dll` takes the folder of the application
rather than the one of `dotnet` (`ApplicationInfo.GetExecutablePath`), and the translation target runs off Windows.
Also: Sublime Merge's output option (`-o "$MERGED"`; it asked where to save), the icons of the tabs of the color picker
(Avalonia 12.1 draws no stretched `PathGeometry`, as Fluent's `ColorView` icons are: they are drawn inside a
`GeometryGroup`), and the buttons of the conflicts dialog grow with their text ("Rescan merge conflicts" was cut).
The look was compared with the WinForms release on Windows 11 and brought closer to it (the ledger, "Layout").
The Tools menu: gitk and Git GUI are found where Homebrew and the distributions install them and say how to install
them when missing (`GitGuiTools`); on macOS "Git bash" is "Open in Terminal". The commit dialog had a second Rescan button above the unstaged files
(FormCommit has only the Refresh of the toolbar of that list): removed.

The subsequent QA click-through (2026-09-25) fixed first-run stack overflow in the plugin assembly resolver and
checkout error dialogs while typing partial branch names. Both have regression tests and were rechecked in the
macOS app; the Release build and all 18 suite invocations pass. Culture-dependent decimal expectations in two
headless tests were corrected too. Windows and WSL were rerun afterwards (builds, suites, a portable smoke test).
The [macOS QA report](QA-2026-09-25.md) records coverage and remaining checks. Its two open findings are closed: Cmd+C
selecting all revisions came from the test input (System Events sends its Cmd modifier with key code 0, which
Avalonia.Native reports as Cmd+A; with the key code of a real Cmd key only the selection is copied), and the Hotkeys
settings show the shortcuts of macOS (`⇧⌘X`, `KeysExtensions.ToMacOSText`).
P4Merge passed a saved resolution after the user approved Gatekeeper; no integration defect was established.
Test processes and credential entries were cleaned up; see the report for the whole-Keychain metadata comparison
limitation. `QA.md` now also covers the click-through on Windows and under WSLg.

## Windows QA (2026-09-25, in progress)

See [the Windows report](QA-Windows-2026-09-25.md). The first batch fixes portable registration and registry reads,
unchanged shell-settings writes, the portable checklist startup preference, and discovery of current Windows tool
installations. The report distinguishes actual UI checks from discovery-only checks. Linux manual QA is deferred by
the owner; shared changes are built and tested on Windows and WSL.

## Native Linux QA (2026-09-26)

See [the Linux report](QA-Linux-2026-09-26.md). Seven installed diff/merge tools
passed actual diffs and saved conflict resolutions; seven editor commands completed Git commit-message edits. The
Release build and all 18 suite invocations pass (25,316 passed, 167 skipped). This batch fixes Arch's `zeditor`
discovery and a fractional-line-height boundary in the diff viewer, with regressions. It also records unresolved
first-run language persistence, menu dismissal and dark-link contrast findings, the custom difftool list, remaining
manual coverage, and desktop-preference isolation limitations. Windows/macOS were not rerun. `QA.md` now includes
native-Linux setup and all writable XDG roots/private D-Bus activation isolation.

The subsequent actual Hyprland desktop check confirms that this build is an XWayland client; without `DISPLAY`,
the startup path fails with `XOpenDisplay failed` even with Wayland available. Browsing, diffs, settings, terminal
paste and launch of native Wayland Meld were checked. Default 200% monitor scaling is too small (an explicit
Avalonia scale override works), and Wayland clipboard `text/plain` returns empty while UTF-8 succeeds. The Linux
report records both findings and distinguishes this focused check from the earlier seven saved Xvfb merges.

The next fix batch closes first-run language loss by saving startup choices before the checklist loads independent
settings, verified through save/restart on Hyprland. Linux copy commands now supply the X11 `TEXT` alias required by
Hyprland's Wayland `text/plain` requests; hashes and Unicode/multiline messages pass actual desktop reads. Built-in
Avalonia text-control copying and native Wayland support are outside that workaround. The asynchronous headless
test helper now awaits the test body rather than hiding failures in a nested task, with a regression. Release build
and all 18 suite invocations pass: 25,320 passed, 167 skipped. Default HiDPI scaling and the other open findings remain.

The native follow-up adds **experimental opt-in** Wayland through `GITEXTENSIONS_USE_WAYLAND=1` (with X11 fallback),
fixes owner lookup on backends without native handles, and updates `DpiUtil` when a window's render scale changes.
Actual native startup without `DISPLAY`, browsing/diff, nested settings/font ownership, native TextBox Unicode
clipboard and terminal paste pass. Live 200% → 150% → 100% → 200% scaling works without overrides; the owner's
original display configuration was restored. Backend probes also pass for default XWayland and failed-Wayland/X11
fallback. Final Release build and all 18 suites pass: 25,330 passed, 167 skipped.

**Original opt-in blocker (now fixed):** official Avalonia 12.1.3 drops key-up/leave handling while a modal owner is
disabled and keeps dispatching its repeat timer. The vendored source patch described below fixes that defect;
the Linux report preserves its protocol trace and reproduction. Native cross-backend clipboard export, mixed-DPI
monitors, drag/drop and full workflow/tool parity remain unverified. Default XWayland scaling with Hyprland's
zero-scaling configuration still needs the existing `AVALONIA_GLOBAL_SCALE_FACTOR=2` workaround at 200%.

A later local upstream-source experiment fixes and verifies that repeat blocker: all 49 Wayland tests pass, with
four new cases proven failing on stock source, and the real patched app passes six modal cycles including held
shortcuts and nested Escape dismissal. All 18 Git Extensions suites also pass (25,330 passed, 167 skipped) with the
locally restored package. Source, patch, package and a runnable isolated demo are retained under
`/home/julius/avalonia-wayland-fix`; see the Linux report's "Local Avalonia source patch" section. The upstream issue
is [#22317](https://github.com/AvaloniaUI/Avalonia/issues/22317), and the fix is submitted as
[PR #22318](https://github.com/AvaloniaUI/Avalonia/pull/22318).

The subsequent vendoring batch adopts that fix as `Avalonia.Wayland 12.1.3-gitextensions-wayland.1`: root
`NuGet.Config` maps only this package ID to the checked-in feed in `third_party/nuget`, and the central version is
pinned exactly. Other Avalonia dependencies remain official 12.1.3. The package is rebuilt from committed source,
with the patch, licenses, SHA-256 provenance and an exercised rebuild helper in
[third_party/avalonia-wayland](../../third_party/avalonia-wayland/README.md). Follow its removal instructions once
an official release includes the fix. Native Wayland was kept opt-in during that batch.

The broader native Hyprland click-through now enables Wayland by default on Linux when `WAYLAND_DISPLAY` is
nonempty. `GITEXTENSIONS_USE_WAYLAND=0` forces X11; failed Wayland initialization also falls back to X11. Commit,
push/pull/clone, branches, stash, conflict resolution through native Meld, history/blame, clipboard exchange with
X11, terminal paste, file pickers, dark theme, plugins and live scaling passed. Five actual backend probes and six
held/released modal cycles pass on the final build. Release build and all 18 suites pass: 25,333 passed, 167 skipped.
See the Linux report for exact coverage and the non-blocking diff-menu finding. Other compositors, mixed-DPI
monitors, drag/drop and IME input remain unverified; Windows/macOS were not rerun.

## Ubuntu GNOME / Fedora KDE VMs (2026-09-26)

The [VM QA report](QA-VM-Linux-2026-09-26.md) extends native Wayland coverage to Mutter and KWin. Both guests
built `495ede8b5` and passed all 18 test-project invocations (25,333 passed, 167 skipped each). Actual UI checks
cover commit, push/pull, Meld conflict resolution, held modal shortcuts, clipboard between Wayland and X11,
terminal/editor integration, GTK folder picking and live 100%/150%/200% scaling. All five startup/fallback modes
pass. No new application blocker was confirmed. The report records VM graphics limitations and untested areas.
Reusable VMs and evidence are retained under `/home/julius/VirtualMachines/gitextensions-qa`, with local launch
instructions in its `README.md`; no VM disks or credentials belong in this repository.

## Windows 10 LTSC VM (2026-09-26)

The [Windows VM report](QA-VM-Windows-2026-09-26.md) records a fresh USB-media install, updated to 19044.7725,
at source `4764ea0fb`: Release build and all 18 suites passed (25,726 passed, 40 skipped), including all 191
native UI integration tests. Manual Git workflows, five external merge tools, WinMerge diff, Notepad++ through
Git Bash, history/blame, native dialogs, modal repetition and live 100–200% scaling passed. The original ISO
needed Windows updates to satisfy .NET's CET prerequisite; no application fix was necessary. The portable app
left the normal qa settings/registry absent. The reusable `ge-qa-windows10` VM, evidence and cold checkpoint
are retained locally; see its report for launch instructions and explicit coverage limits.

## Rules

- Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` when an agent makes them. Do not commit the
  untracked `prototypes/` folder.
- Tests must not change the machine: no writes to the real Git Extensions settings, the registry
  (`HKCU\Software\GitExtensions`, e.g. `gitcommand`), `WindowPositions.xml` or real repositories. A test that changes an
  `AppSettings` value saves and restores it, or uses a fake.
- Run tests with `--blame-hang-timeout 3m` (as CI): a test that shows a modal dialog otherwise hangs forever, and the
  slowest healthy test takes about 25 s. The driven dialogs of `UI.IntegrationTests` close themselves after 2 minutes
  or when a step of their driver fails, so such a test fails instead of hanging. The test assemblies run one at a time:
  each holds an exclusive lock on `%TEMP%/GitExtensionsTestAssemblySerializer.lock` (`TestAppSettingsAttribute`), which
  the system releases when a test host is killed.
- Smoke tests of the real app run a copy with portable settings (below), on scratch repositories, never on the
  user's own; close every instance afterwards (`Stop-Process -Id <pid> -Force` on Windows) and check it is gone.
  Starting the app on a folder that is not a repository may open the last used repository instead.

## Build and test

- Build: `dotnet build GitExtensions.slnx -c Release` (outputs under `artifacts/Release/bin`). `TreatWarningsAsErrors` is
  on, with StyleCop (e.g. SA1515: a comment needs a blank line before it; SA1119: no needless parentheses).
- Tests: every `tests/**/*.csproj` except `CommonTestUtils`, with `--no-build --blame-hang-timeout 3m`; 18 suites.
- Linux: the solution builds with the .NET 10 SDK (e.g. in WSL: `dotnet-install.sh --channel 10.0`, plus `libicu`
  and `fontconfig`). Build in a copy on the Linux file system rather than under `/mnt/c` (much faster, and file name
  case is checked as on a real Linux machine). NUnit skips the Windows-only tests there (`UI.IntegrationTests`, and the
  tests marked `[Platform(Include = "Win")]`). Test data written as Windows paths goes through `TestPaths.Native`.
- macOS: `brew install --cask dotnet-sdk` (Microsoft's; the `dotnet` formula that `powershell` pulls in must not shadow
  it), Xcode for FileMerge, `brew install git-gui` for gitk and Git GUI, `git submodule update --init` before the first build. The opt-in Keychain test:
  `GE_TEST_KEYCHAIN=1`. Smoke tests: the portable copy started with `HOME` set to a scratch folder (git's global config
  then is a scratch `.gitconfig`: the Repair buttons and the diff tool settings write to it) and without `GIT_EDITOR`
  (agent shells set it to `true`, which overrides `core.editor`). Windows are captured with `screencapture -l <id>`
  (the ids from `CGWindowListCopyWindowInfo`, e.g. through `osascript -l JavaScript`) and driven with `cliclick`
  (Accessibility and Screen Recording for the terminal app). `cliclick kp:esc` does not reach Avalonia windows: send
  Escape with `osascript -e 'tell application "System Events" to key code 53'`. Check the front app (`lsappinfo front`)
  before typing, as someone else may be using the Mac. A crash report dialog of macOS ("quit unexpectedly") takes the
  keyboard and ignores synthetic input: `killall UserNotificationCenter` closes it.
- Translations: a new or changed string of a strings class (`GitUI.Presentation/**/*Strings.cs`) needs English.xlf
  regenerated: `cd src/app/GitExtensions && dotnet msbuild -p:Configuration=Release -t:_UpdateEnglishTranslations
  -p:RunTranslationApp=true`. `ViewStringsTests` fails until then; new strings classes are added to its list. The target
  also runs off Windows (`dotnet TranslationApp.dll`, `grep`); it wrote an incomplete English.xlf there before, since the
  plugins were looked for next to `dotnet` (`ManagedExtensibility` now uses `AppContext.BaseDirectory`).
- CI has not run on this branch: `app-build.yml` builds `master`, `release*` and `experimental/**` pushes and pull
  requests only (add the branch, or use `workflow_dispatch`, to get the Linux job and the Windows publish with the MSI).
- A portable copy of the app for smoke tests: copy `artifacts/Release/bin/GitExtensions/net10.0` to another folder,
  keeping that folder's own `GitExtensions.settings` (portable mode) between copies. Portable mode is `IsPortable` =
  `True` in its `GitExtensions.dll.config`.
- Linux smoke tests (WSLg): the same portable copy of the Linux build, started with `DISPLAY=:0` on a scratch repository
  in the Linux file system, with a `GitExtensions.settings` that sets `translation`, `CheckForUpdates`=false and
  `CheckSettings`=false (else the language dialog and the checklist come first) and `gitcommand` (`/usr/bin/git`).
  Its windows cannot be captured from Windows (they are remote windows): `import -window <id>` (ImageMagick) with the
  ids of `xwininfo -root -tree` (class `GitExtensions`); `xdotool mousemove <x> <y> click 1` (screen coordinates,
  `--window` does not reach them) and `xdotool type` drive them. Kill the process afterwards.
- Publish needs an existing `artifacts/Release/publish/GitExtensions.PluginManager` folder to skip the download. Its
  PowerShell steps run Windows PowerShell with `-ExecutionPolicy Bypass` on Windows, `pwsh` elsewhere.

## Avalonia and editor quirks found during the port

- `TextBox.TextChanged` is raised after the change is applied, even for programmatic changes: code that reacts to its own
  edits must recognise them (see `ComboBoxSuggestAppend`).
- Avalonia 12 has no `ActualThemeVariantProperty` for class handlers: subscribe to `ActualThemeVariantChanged`.
- A `MenuFlyout` keeps the items it was first shown with; menus built when they open use `FreshMenuFlyout`.
- A `MenuItem` with `ToggleType` toggles itself when clicked; with a one-way `IsChecked` binding, raise the property
  changed notification after the command so the check mark follows the model.
- The `DataGrid` lists its selected items in row order; the revision grid tracks the selection order itself.
- AvaloniaEdit: `ScrollToVerticalOffset` does not scroll (set the offset of its `ScrollViewer`); the selection is dropped
  when the caret leaves it; there is no IME pre-edit.
- Windows wheel events lack the Alt modifier; track Alt from the key events of the top level.
- In headless tests, `await` continuations may leave the UI thread; poll with `Dispatcher.UIThread.RunJobs()` instead.
- Test fixtures of `GitRef` need a module (`TestGitModule.Instance`), since `IsTrackingRemote` reads `MergeWith` from it.
- Shell heredocs mangle `\\`, `\n` and similar escapes in code being edited; write edit scripts to a file instead.
- Linux GUI in WSL: WSLg shows X11 windows on the Windows desktop (`DISPLAY=:0`); Avalonia's X11 backend needs
  `libice6`, `libsm6` and the X11 libraries (`apt-get install libice6 libsm6 libx11-xcb1 libxrandr2 libxi6 libxcursor1`).
- `TranslationApp.exe` is started by name from its folder by `_UpdateEnglishTranslations`; a shell with
  `NoDefaultCurrentDirectoryInExePath` set (e.g. an agent's) cannot find it: unset it for that command.
- From Git Bash, `wsl.exe` arguments are mangled twice: MSYS converts paths (set `MSYS_NO_PATHCONV=1`) and `wsl.exe`
  expands `$` through the default shell of the distribution. Put Linux commands in a script file and run it with
  `wsl.exe -e bash <script>`.
