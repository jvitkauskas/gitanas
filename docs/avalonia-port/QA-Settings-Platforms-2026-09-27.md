# Settings platform review (2026-09-27)

Reviewed the built-in settings pages, their host adapters, related HOME dialog, and bundled plugin settings for Windows-specific controls and explanations. This is a source review plus Linux headless UI validation, not a native click-through on every OS.

## Changes

| Area | Finding and change |
| --- | --- |
| SSH | Hide the OpenSSH/PuTTY comparison wherever PuTTY is unavailable (the preceding fix). An imported `plink.exe` command now appears as an editable custom SSH command on Linux/macOS, instead of selecting a hidden PuTTY option. Merely opening/saving preserves that command. |
| Git paths | Replace Git for Windows/WSL instructions and `.cmd`/`.exe` wording on Linux/macOS. The executable picker offers `git` and all files. Download Git links to the [official installation page](https://git-scm.com/install/) there. |
| HOME | Use Unix help and `$VARIABLE` notation on Linux/macOS. Hide the unsupported USERPROFILE choice; ignore Windows-only candidate locations and saved USERPROFILE selection there. Keep custom HOME available, and start its folder picker in the default home directory. |
| Git checklist | Missing-Git repair guidance on Linux/macOS recommends installing Git through the package manager or setting its command, rather than installing Git for Windows. |
| Appearance | Hide the registry-backed Visual Studio branch-name setting on Linux/macOS. Remove the legacy auto-scale checkbox on every Avalonia backend: no runtime consumer uses it; Avalonia controls display scaling. |
| General | Hide the separate Git console-window option on Linux/macOS. Its consumer sets `ProcessStartInfo.CreateNoWindow`, which controls Windows console creation. |
| Advanced | Hide update preferences on Linux/macOS: the current feed supplies upstream Windows installers. Preserve their stored values when saving these platforms' settings. |
| Auto compile submodules | Use MSBuild/dotnet captions without `.exe`, while retaining existing settings keys. Both executable choices are already supported by the plugin. |

## Relevant or already handled

- Explorer shell-extension settings are already omitted outside Windows.
- Windows installation, shell registration, bundled Unix tools, PuTTY and obsolete Windows credential-helper checklist rows are already omitted there.
- The Git-for-Windows Unix-tools directory is already hidden outside Windows.
- Console emulators and shell choices are filtered by supported environment. The built-in terminal, console fonts/themes and command-output mode remain relevant on Unix.
- Line-ending choices remain valid Git configuration on every OS, including CRLF for repositories that require it.
- Editor, diff/merge-tool, credentials, Git workflow, appearance, font, history, scripts and build-server settings remain relevant; a Windows-era internal setting name alone is not a reason to hide them.

## Validation and limits

- Release solution build succeeded.
- Full `GitUI.Avalonia.Tests` suite: 1,249 passed, 1 skipped, no failures (final run).
- Updated HOME tests cover platform-dependent variables and ignoring Windows candidate directories. Added an imported-PuTTY-path regression covering visibility through the custom-client choice and preservation on save.
- Regenerated English translations, including the platform-independent plugin captions; translation checks run in the suite.
- Inspected Linux headless renders of Git Paths, HOME, Appearance and Advanced. Fixture paths may still look like Windows paths; they are synthetic test data.
- Windows and macOS were not run. Existing Windows controls and explanations remain except for the unused auto-scale checkbox and portable MSBuild captions.
- The manual Help → Check for updates flow still uses the upstream release feed. It needs a separate release/distribution policy for the experimental port; hiding its settings does not implement a Unix updater.
- Build logs, test results and screenshots are retained under `artifacts/local-launch/platform-settings*`. Tests use existing isolated fixtures; no real settings changes or application restart were performed.
