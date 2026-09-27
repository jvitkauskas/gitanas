# Upstream master review — 27 September 2026

Reviewed `gitextensions/gitextensions` master at `e265607b631a7fd32b60c317ba096209fabf0135`
against the Avalonia fork point `66050831e32ddc9ea2e610896f4cf96f7655805e`.
There are two upstream commits since that point. Neither requires merging the removed WinForms implementation.

| Upstream change | Disposition in the Avalonia port |
| --- | --- |
| [`a4d51838f`](https://github.com/gitextensions/gitextensions/commit/a4d51838f57b568fc6756417ae8b3995db4f109a), always name the file in conflict-resolution dialogs | Adapted to `ResolveConflictsViewModel`, `SolveConflictQuestion` and the Avalonia dialog host. |
| [`e265607b6`](https://github.com/gitextensions/gitextensions/commit/e265607b631a7fd32b60c317ba096209fabf0135), batch expansion when selecting all files | Already addressed by the port's `af72747eb`: batched selection notifications, selection ranges and deferred scrolling. The upstream `FileStatusList`/WinForms tree no longer exists here. Existing bulk-selection tests pass. |

## Adapted conflict fix

- All four resolution prompt paths (binary, missing base, missing local and missing remote) pass the file's name as
  the task-dialog heading, including when the detail text is empty.
- A file deleted on both sides gets its own description with the correct merge/rebase side labels.
- Unhandled combinations clear the description instead of displaying text from the previously selected file.
- The English translation catalog uses the same new string ID and source text as upstream.
- Resolution choices, apply-to-all behavior and rebase semantics are unchanged, matching the scope of upstream's fix.

Six new regression cases cover both merge/rebase descriptions, unhandled combinations, and deleted-on-both-sides
prompts alone or after another conflict. Existing prompt assertions now verify the headings for the other paths.

## Validation

- Linux Release solution build: passed (seven existing warnings).
- English catalog regenerated with `_UpdateEnglishTranslations`; its only change is the new upstream string.
- Focused conflict, file-list and translation tests: 198 passed, one Windows-only test skipped.
- All 18 Linux test-project invocations: 25,384 passed, 167 skipped, zero failures. Native Windows UI tests do not
  run on Linux.
- Real Linux application smoke test on an isolated Xvfb/Openbox desktop: constructed a base-only unmerged index
  entry, checked the deleted-on-both-sides description, opened the resolution prompt and verified its filename
  heading. Cancel left the unmerged index unchanged. Application, window manager and X server closed afterwards.
- Windows 10 VM: Release solution build passed; all 18 test projects passed (25,764 passed, 43 skipped),
  including all 191 native UI integration tests. The six new regression cases pass on both systems.
- macOS was not rerun for this change.

Local evidence is retained under `/home/julius/VirtualMachines/gitextensions-qa/upstream-sync-2026-09-27`.
The Windows build/test checkout is isolated at `C:\QA\upstream-source`; Linux smoke fixtures and settings are also
isolated from the user's repositories and settings.
