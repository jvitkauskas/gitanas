# Windows 10 VM QA — 2026-09-26

This pass uses a fresh Windows VM installed from the owner's USB media and follows [QA.md](QA.md).
It complements the [earlier Windows pass](QA-Windows-2026-09-25.md) and the
[Ubuntu/Fedora VM pass](QA-VM-Linux-2026-09-26.md). Results below apply to the specific configuration tested.

## System and isolation

- Windows 10 Enterprise LTSC 2021 x64, version 21H2, updated to **19044.7725**.
- USB image `en-us_windows_10_iot_enterprise_ltsc_2021_x64_dvd_257ad90f.iso`; image index 1 is standard
  Enterprise LTSC. A local copy was used, leaving the USB unchanged.
- QEMU/KVM, user-session libvirt, UEFI/q35, **8 vCPUs, 16 GiB RAM, 100 GiB expanding disk**, QXL graphics,
  e1000e networking through passt, USB tablet. No host Windows partition, shared host repository or inbound
  network forwarding was attached. Guest-agent commands use the local virtio-serial channel.
- Full source clone and submodules at **`4764ea0fbfa44ff0e2da57ad2805fc3157553fe9`**, .NET SDK **10.0.201**,
  Avalonia **12.1.3** and the committed **12.1.3-gitextensions-wayland.1** package. Its SHA256 matched provenance.
- Builds/tests run under the interactive `qa` account with scratch Git configuration and HOME. Manual checks
  use a complete portable copy and scratch repositories under `C:\QA`, with isolated HOME, Git global config,
  APPDATA and LOCALAPPDATA. The qa user's normal Git config, GitExtensions settings and
  `HKCU\Software\GitExtensions` were absent before testing.

Guest tools installed: Git 2.55.0.windows.5, WinMerge 2.16.58.2, KDiff3 1.12.4, Meld 3.22.2,
TortoiseGit 2.19.1.0, Notepad++ 8.9.8.1 and VS Code 1.139.1. The VC++ x64 runtime, QXL and virtio-serial
drivers and QEMU guest agent were installed too. Exact inventory, download URLs and checksums are retained locally.

## Original-image prerequisite

The unupdated ISO installed Windows build **19044.1288**. The first Release build failed in .NET's compiler
process with `Your Windows doesn't fully support CET. Please install all available Windows updates.`
Installing the offered Windows and .NET Framework cumulative updates and rebooting resolved the failure.
No CPU feature or process exploit mitigation was disabled. This was an OS/runtime prerequisite, not an application
regression. The original failure and Windows Update results are retained alongside the successful build log.

## Automated build and tests

The full `dotnet build GitExtensions.slnx -c Release` passed with **7 warnings and no errors**.
All **18 test projects** (excluding the helper `CommonTestUtils`) ran sequentially in Release with
`--no-build --no-restore --blame-hang-timeout 3m` and a 12-minute per-project watchdog.
Every invocation exited 0: **25,726 passed, 40 skipped, 0 failed**. These totals were independently counted
from individual test outcomes in all 18 retained TRX files. `UI.IntegrationTests` passed all **191** tests
on the real interactive Windows desktop; `BugReporter.IntegrationTests` passed its test too. Manual input
was kept separate from the native integration suite.

## Manual click-through

QMP mouse/keyboard input drove the guest desktop; Windows UI Automation supplied window/control information.
Screenshots captured the VM display. This did not move the host pointer or change the host display settings.

| Area | Result |
| --- | --- |
| First run | English selection, settings checklist, Git discovery and diff-tool configuration worked. |
| Commit | Stage one file, unstage, stage all, paste a Unicode/multiline message, then commit with Ctrl+Enter. Git verified the result and clean working tree. |
| Push/pull | Push to the local bare remote, create/push a peer commit, then fast-forward through the application's pull action. Both operations completed successfully. |
| External merge tools | WinMerge, TortoiseGitMerge, KDiff3, Meld and VS Code each opened a real three-way conflict from **Resolve conflicts → Open in tool**. Save, close, rescan and commit completed; Git verified the resolution and two-parent merge commit. The filename contained a space. |
| External diff | **Open with difftool → First → Second → winmerge** showed the expected `base`/`remote vscode` two-way comparison. Normal close returned to the application. |
| Editor/terminal | The embedded Git Bash session ran `git commit --allow-empty`; Notepad++ opened the message, Git waited for save/close, and the expected Unicode commit appeared. This exercised Git's configured editor through the terminal. |
| Browse | Revision graph, internal diff, file-history window and blame rendered the fixture's expected revisions/content. |
| Clipboard | Unicode and multiline text round-tripped through the commit editor without losing characters or line breaks. |
| Dialogs | Native font picker and native folder picker opened; selecting the scratch repository returned successfully. Six held Ctrl+O / Escape cycles each produced exactly one open-repository dialog and returned to the main window. |
| Scaling | Live 100% → 150% → 200% → 100% on one QXL display, using 2560×1600 for the high-scale checks. Settings and the native font dialog stayed open through 150% → 200%; controls remained accessible. Folder picking and repeated modal cycles passed at 200%. Restored 1920×1080 at 100%. |
| Relaunch/isolation | The persistent **Git Extensions QA** desktop shortcut reopened the portable app and fixture. After closing it, no GitExtensions/BugReporter process remained, and the normal qa Git config, GitExtensions settings directory and registry key were still absent. The host Git config checksum was unchanged. |

Merge evidence in the disposable repository:

| Tool | Verified merge commit |
| --- | --- |
| WinMerge | `691ff816` |
| TortoiseGitMerge | `b91cc087` |
| KDiff3 | `7d52ae87` |
| Meld | `d6254305` |
| VS Code | `154e7511` |

The initial TortoiseGitMerge attempt chose unchanged local content and left an empty-commit confirmation open;
the test driver advanced too early. The fixture driver was strengthened to reject an unfinished merge, and
TortoiseGitMerge and KDiff3 were repeated successfully on fresh branches. This was a testing sequence error,
not an established application defect. Merge-tool backup files were removed from the scratch fixture afterwards.

## Findings and limits

No new application blocker was confirmed and no application code change was needed for this pass.
The old OS image's CET prerequisite was resolved by Windows Update as described above. The vendored Wayland
package was verified as part of the build inputs; **Windows uses Avalonia's Windows backend**, so these results
do not constitute another Wayland test.

This is substantial additional coverage, not completion of every row in `QA.md`. Not manually checked here:
SSH/HTTPS authentication and credential storage, Windows shell-extension registration, Visual Studio integration,
WebView2-specific flows, every plugin, separate ConEmu/Mintty configuration, IME/drag-and-drop, dark theme,
physical GPU rendering, multiple monitors or mixed-DPI movement. Other commercial diff/merge tools and editors
were not installed in this VM. Windows 11 was not tested in this pass. Earlier reports' unrelated open findings
are not closed by these results.

## Retained environment

The VM is `ge-qa-windows10` in `qemu:///session`. Machine files, installer provenance, scripts, screenshots and
logs are under `/home/julius/VirtualMachines/gitextensions-qa/windows10`; its `README.md` documents reuse.
`tests-and-logs.tar.gz` contains the build, all test logs/TRX files and guest inventory; `test-totals.json` records
the independently parsed outcomes. Screenshots and the six-cycle window inventory are retained beside them.
The VM is powered off, does not autostart, and has a cold post-QA checkpoint under
`/home/julius/VirtualMachines/gitextensions-qa/checkpoints/post-qa/windows10`.
The temporary host ISO loop mount was removed; the original USB was unchanged.
These large and machine-specific artifacts do not belong in the source repository. No host sudo password is saved.
