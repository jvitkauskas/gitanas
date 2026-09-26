# QA click-through of the port

A manual check of Git Extensions (the Avalonia port, branch `avalonia`) on Windows, Linux (native or WSLg) and macOS, by clicking
through the application as a user would. Read [HANDOFF.md](HANDOFF.md) first (state of the port, rules), and the
"Platforms" section of [ledger.md](ledger.md) for what was already checked and what is known to be open.

The machine is someone's real machine: nothing may change outside the scratch folders. No writes to the real Git
Extensions settings, the Windows registry, the real `~/.gitconfig`, real repositories, or the real credential store
(except items the test creates and deletes again).

Latest additional compositor coverage: [Ubuntu GNOME and Fedora KDE VM QA](QA-VM-Linux-2026-09-26.md), including
full builds/tests, native Wayland workflows, live scaling and X11 fallback. See that report for explicit limits.
Additional Windows coverage: [Windows 10 LTSC VM QA](QA-VM-Windows-2026-09-26.md), including the full build,
all 18 test suites, five external merge tools, native dialogs and live 100–200% scaling.

## Setup

### Every system

- Build: `dotnet build GitExtensions.slnx -c Release` (after `git submodule update --init`). The application is
  `artifacts/Release/bin/GitExtensions/net10.0/GitExtensions` (`GitExtensions.exe` on Windows).
- Copy that folder to a scratch folder and run the copy as portable (`IsPortable` = `True` in its
  `GitExtensions.dll.config`), so its settings (`GitExtensions.settings`) and `WindowPositions.xml` stay in that folder.
  A `GitExtensions.settings` that sets `translation` (`English`), `CheckForUpdates` (`false`) and `gitcommand` skips the
  language dialog; leave them out to test the first run.
- Before refreshing a portable copy, quit it and wait for its processes to exit. Never overwrite loaded DLLs; use a
  fresh complete copy for a new build if needed. Inspect windows owned by `BugReporter` as well as `GitExtensions`:
  an error report runs in a separate process and can remain after the main application exits.
- Keep git's global config a scratch one: `HOME` set to a scratch folder, or `GIT_CONFIG_GLOBAL=<scratch>/.gitconfig`
  when the tool needs the real `HOME` (the Keychain, gh, VS Code). Start without `GIT_EDITOR` (agent shells may set it
  to `true`, which overrides `core.editor`). The Repair buttons of the checklist and the editor and diff tool settings
  write to that file.
- Work in scratch repositories only (make commits, branches, conflicts, stashes, remotes there). A local bare
  repository, or an `sshd` on 127.0.0.1, can stand in for a server.

### Windows

- Some integration settings live in the registry (`HKCU\Software\GitExtensions`) even in portable mode: the shell
  extension settings (`CascadeShellMenuItems`, `AlwaysShowAllCommands`) and `ShowCurrentBranchInVS`. Look at those
  pages, do not change them, and do not register the shell extension. `CheckSettings` is stored in the portable
  settings file; nonportable Windows installations still use the registry for it. A successful checklist scan may
  disable it automatically without changing the installed application's preference.
- Set `HOME` (Git for Windows honors it) or `GIT_CONFIG_GLOBAL` for the process you start.
- Driving the UI:
  - The desktop is shared with the user, and synthetic input moves their real mouse and keyboard. Say so before you
    start, and check the foreground window before typing.
  - Capture a window with `PrintWindow` (flag `PW_RENDERFULLCONTENT` = 2) into a bitmap of `GetWindowRect`. The
    capturing process must be DPI aware (`SetProcessDpiAwarenessContext(-4)` first), or at a scaling above 100% the
    image is only the top-left part of the window.
  - Avalonia exposes UI Automation on Windows (`System.Windows.Automation` in Windows PowerShell): find elements by
    name and use `InvokePattern`, `ValuePattern`, `ExpandCollapsePattern` and `SelectionItemPattern`. That is more
    reliable than coordinates. Otherwise `SetCursorPos` plus `mouse_event`/`SendInput`, in physical pixels.
  - `SetForegroundWindow` is refused unless the caller owns the foreground: bring the window up before clicking.
- Running the test suites shows real windows (`UI.IntegrationTests`): run them with `--blame-hang-timeout 3m`, one
  project after another, with a watchdog per project.

### Linux (WSLg)

- Build in the Linux file system, never from `/mnt/c`: that would overwrite the Windows `artifacts` and `obj` folders. A
  clone of the branch in `~` (for example `~/ge`), brought up to date with `git pull` or with an `rsync` of the Windows
  worktree that excludes `bin/`, `obj/`, `artifacts/` and `.git`. The .NET SDK is in `~/.dotnet`: `source ~/.profile`
  first, else the app says "You must install .NET".
- Tools (ask the user for the sudo password; do not store it):
  - For driving the UI: `xdotool`, `imagemagick`, `x11-utils`.
  - For the app to use: `meld`, `gitk`, `pcmanfm` (a file manager for Show in folder), `xdg-utils`, `gnome-keyring`
    and `libsecret-tools` (the Secret Service), `openssh-server` (stop `sshd` afterwards).
- The portable copy and its scratch repositories are in the Linux file system. `CheckSettings` is in the settings file
  there. Start the app with `DISPLAY=:0`.
- Run the app in the foreground of a `wsl.exe` call that runs in the background, with a timeout (`timeout -s KILL`).
  Processes detached with `setsid` or `nohup` die when the WSL session ends.
- Driving the UI:
  - Capture a window with `import -window <id>` (ImageMagick), with the ids of `xwininfo -root -tree` (class
    `GitExtensions`). Captures of the root window, or from Windows with `PrintWindow`, do not work.
  - Click with `xdotool mousemove <x> <y> click 1` (screen coordinates; `--window` does not reach the windows). The first
    click on a new window may only activate it. Type with `xdotool type` and `xdotool key`.
  - Menus and popups are nameless override-redirect windows.
  - GTK apps such as meld and the keyring prompter use Wayland, so X11 tools do not see them: capture the Windows screen
    to see them.

### Native Linux

- The same portable-copy and scratch-repository rules apply. Install tools through the distribution's package manager
  when the owner authorizes installation; record which packages remain installed. Vendor archives can instead live
  below the scratch folder, with launchers on a scratch `PATH`.
- Set **all** writable XDG roots before starting the application, external tools, or a private D-Bus session:
  `XDG_CONFIG_HOME`, `XDG_DATA_HOME`, `XDG_CACHE_HOME` and `XDG_STATE_HOME`, as well as `HOME` and `GIT_CONFIG_GLOBAL`.
  A private bus started with the real environment can autoactivate dconf, portals and file-manager helpers with the
  real paths even when the app itself uses scratch HOME. Start the bus after exporting the scratch environment;
  setting the app's environment later is insufficient. Do not reuse a real user's Secret Service for fixture tests.
- On a shared Wayland desktop, a separate Xvfb display with a small window manager (for example Openbox) allows X11
  click-through without moving the user's pointer. Set `DISPLAY` for every input/capture command, unset
  `WAYLAND_DISPLAY`, and use `GDK_BACKEND=x11` / `QT_QPA_PLATFORM=xcb` for external tools. Electron may need explicit
  X11 flags in its scratch configuration. Report Xvfb coverage separately from native Wayland coverage.
- On Linux, a nonempty `WAYLAND_DISPLAY` selects native Wayland by default. The backend is still experimental;
  set `GITEXTENSIONS_USE_WAYLAND=0` to force X11 (XWayland on a Wayland desktop). `1` remains supported, but is no
  longer necessary. Without a Wayland display, or if Wayland initialization fails, X11 is used. Confirm the actual
  backend with compositor client information. Test default native startup with `DISPLAY` unset, explicit opt-out,
  an invalid Wayland socket with X11 available, and an X11-only session. Do not infer the backend merely from the
  session type or environment variables.
- Official Avalonia 12.1.3 has a modal-dialog key-repeat defect: Ctrl+O can keep reopening the repository dialog
  after release/cancel. This checkout uses a vendored source patch for that defect; see
  [package provenance and rebuild/removal instructions](../../third_party/avalonia-wayland/README.md).
  Verify the vendored artifact with `python3 third_party/avalonia-wayland/rebuild.py --verify-only`.
  Future package updates must pass keyboard-opened modal dialogs, held/released shortcuts and nested dialogs,
  as well as a restore with fresh NuGet caches to verify package source mapping.
- Check native scaling without `AVALONIA_GLOBAL_SCALE_FACTOR` or `AVALONIA_SCREEN_SCALE_FACTORS`, including a live
  fractional-scale change and nested dialogs. Change physical display settings only with the owner's authorization;
  back up and restore them afterwards. For XWayland with compositor scaling disabled, the existing process-local
  workaround is `AVALONIA_GLOBAL_SCALE_FACTOR=2` on a 200% monitor. Do not force that value on other configurations
  or mix it into the native scaling test; it can double-scale a compositor-scaled XWayland window.
- Arch's distribution .NET SDK may select an unavailable `arch-x64` apphost package. The tested override is
  `dotnet build GitExtensions.slnx -c Release -p:NETCoreSdkRuntimeIdentifier=linux-x64`. Install DejaVu fonts for the
  headless Avalonia suite too. Run projects sequentially with the same hang timeout/watchdog as Windows.
- Close external tools normally when checking wait/exit behavior. Record and stop private bus helpers, keyring,
  SSH agent, window manager and Xvfb processes during cleanup, without killing the user's desktop services.
- The [native Linux report of 2026-09-26](QA-Linux-2026-09-26.md) records seven diff/merge tools and seven editor
  commands exercised, prerequisites, fixes, remaining coverage, and the isolation mistakes that motivated these
  environment instructions.

### macOS

- Drive the UI with `screencapture -x -o -l <window id>` (window ids from `CGWindowListCopyWindowInfo`, e.g. through
  `osascript -l JavaScript`) and `cliclick`. Pitfalls:
  - Escape must be sent with `osascript -e 'tell application "System Events" to key code 53'`.
  - Do not send Cmd shortcuts with System Events (`keystroke "c" using command down`): its Cmd modifier event has key
    code 0, which the application receives as an extra Cmd+A (e.g. the revision grid selects all). Post the keys with
    CoreGraphics (`CGEvent` for key code 55, then the key, with `.maskCommand`), as a physical keyboard sends them.
  - A lost click is retried with `dd:x,y w:100 du:x,y`.
  - Check `lsappinfo front` before typing, since someone may be using the Mac.
  - A macOS crash report dialog swallows input; close it with `killall UserNotificationCenter`.
  - A locked screen stops Avalonia from starting.

## What to test

Go through each area and watch for errors, crashes, hangs, and clipped or unreadable text. Also check the conventions
of the system:

| | Windows | Linux | macOS |
|---|---|---|---|
| Shortcut modifier | Ctrl | Ctrl | Cmd |
| Dialog button order | OK, Cancel | OK, Cancel | Cancel, OK |
| Menu bar | in the window | in the window | the macOS menu bar |

1. **Startup**: the first-run dialogs (language, the settings checklist), the dashboard, open and clone a repository,
   the recent repositories.
2. **Main window**:
   - The revision grid (graph, selection, Ctrl/Cmd+C copies hashes, the context menus) and the left panel (branches,
     remotes, tags, stashes, submodules).
   - The commit info, diff, file tree, blame and console tabs.
   - The menus and their shortcuts. On macOS: the application menu (About, Settings Cmd+, and Quit Cmd+Q).
   - Light, dark and system themes. Check scaling above 100% where the display allows it.
3. **Everyday git**:
   - Commit (stage, unstage, amend, Ctrl/Cmd+Return), and push, pull and fetch (to a local remote, over SSH with the
     host key and passphrase prompts, over HTTPS).
   - Create, check out, rename, delete and merge branches; rebase, cherry-pick, revert, reset.
   - Stash, tags, remotes (with color and prefix), submodules, worktrees.
4. **Conflicts**: the conflicts dialog and "Open in <tool>" for each installed merge tool, and the diff tools from the
   diff tab. The tools offered depend on the system (`DiffMergeTool.IsAvailable`); test the ones that are installed:
   - Windows: all of them, including WinMerge, TortoiseGitMerge, the Visual Studio diff and Araxis.
   - Linux: Meld (installed in WSL), KDiff3, P4Merge, Beyond Compare 4, Sublime Merge, VS Code, DiffMerge.
   - macOS: FileMerge, KDiff3, Meld, P4Merge, Sublime Merge, Beyond Compare, VS Code, Araxis.
5. **Tools and dialogs**:
   - The settings pages (fonts, hotkeys, git config, editors, diff/merge tools, SSH), the file history, the search and
     filter boxes.
   - The file, folder, font and color pickers; scripts; the terminal tab (the default shell of the system).
   - Plugins (e.g. Find large files, Delete unused branches, Statistics, gitk and Gource where installed); build server
     integration (a fake server is fine).
   - Opening files and folders: "Show in folder" and "Open with" (Explorer; `xdg-open` and the file manager over D-Bus
     on Linux; `open`/`open -R` on macOS), and the editor for commit messages and files.
6. **Credentials**: the askpass prompts (on Windows: GitExtSshAskPass and PuTTY where installed), and saving the
   credentials of a plugin. Delete every item the test created:
   - Windows: the Credential Manager, `cmdkey /list` and `cmdkey /delete:GitExtensions_<name>`.
   - Linux: the Secret Service, `secret-tool search --all target <name>` and `secret-tool clear target <name>`. An
     unlocked keyring may ask for its password in a prompter window.
   - macOS: the Keychain, `security delete-generic-password -s <name>`.
7. **Only on Windows**: the Git Bash and ConEmu terminals, the taskbar progress and jump list, and the WebView2 build
   report. These are hidden on Linux and macOS: check that nothing points to them there.

## When something is wrong

Try to fix it, in the style of the surrounding code:

- Find the cause first: reproduce it, and read the log of the app and the console.
- Fix it for every system when the bug is general. When a change would alter the behavior on another system, put it
  behind `OperatingSystem.IsWindows()`, `IsLinux()` or `IsMacOS()`.
- Add or extend a headless test in `tests/app/UnitTests/GitUI.Avalonia.Tests` (or the matching project). The test
  fails without the fix and passes with it.
- Build, run every test suite (on each system you can reach: Windows and WSL from the same machine), and check the fix
  in the app again. Update the ledger (and HANDOFF.md if the state changed).
- Commit per the rules of HANDOFF.md: the co-author line, no `prototypes/`, and say what could not be checked on other
  systems. Other agents may push to `avalonia` meanwhile, so pull before you commit, and rebase if the push is
  rejected.
- When a fix is too large or unclear, write the issue down (steps, screenshot, what was expected) instead of guessing.

Finish with a short report per system: what was tested, what was fixed (commits), and what is still open. Then clean up:

- Close the application and every helper process: the app, `sshd`, meld, and the file managers the test opened.
- Confirm that `~/.gitconfig`, the credential stores and (on Windows) `HKCU\Software\GitExtensions` are as they were.
