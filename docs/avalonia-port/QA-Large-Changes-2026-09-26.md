# Large change sets and file-tree performance — 2026-09-26

Follow-up to [the history performance investigation](QA-Performance-2026-09-26.md), starting from `fb09960a4`.
The main finding is a severe selection-restoration bottleneck when staging thousands of selected files.
Git itself completed quickly; updating the selected rows made the application unresponsive.

## Workloads and method

Used the actual Release application on native Linux/Wayland, with the same Ryzen 7 7700, 64 GB RAM,
3840×2160 display at 200%, Git 2.55.0 and .NET 10.0.5 as the earlier investigation. Portable settings,
HOME, XDG directories and Git configuration were isolated. No display settings were changed.

- A disposable repository contains two commits affecting **10,000 text files** in 100 directories. The first
  adds the files; the second changes one line in each. Each file has 30 short lines. Another line in every file
  is modified in the working directory for staging tests.
- The existing kernel clone supplies **96,045 tracked files** for the file-tree test and 100,000 history rows.
- A separate correctness workload has 500 modified files, 250 deletions and 250 new files with spaces and Unicode
  in their paths. These 1,000 changes were staged and unstaged successfully.

A temporary external startup hook observes the real windows, dispatcher heartbeat, file-list completion events,
patch loading and process memory. It invokes the actual ListBox selection operation and application commands.
These are application measurements, not headless model-only timings or simulated Git results. The hook is a local
diagnostic artifact and is not shipped with the application.

Two interleaved comparisons used fresh processes and a warm filesystem/object cache. Builds, test suites, VMs and
profilers were inactive during the timing comparisons. Final-code confirmation runs also cover selection,
filtered staging and the kernel tree. The mixed-file workload is a correctness check, excluded from timing comparisons.
Separate EventPipe traces locate the slow paths; profiled runs are excluded from the timing table.

Command durations measure synchronous UI-handler work. The 100 ms dispatcher heartbeat measures subsequent stalls;
it does not sample a command while that same hook callback is executing it. File-list completion is an event,
whereas first-patch readiness is sampled every 100 ms. Neither is an exact first-painted-frame measurement.

## Fixes

1. **Batch file selections.** The file-list selection collection publishes one completed update instead of every
   intermediate selection. This applies both to selecting rows in the UI and to restoring selection in the model.
   Bulk UI selection also uses a membership set instead of searching an ever-growing selection repeatedly.
2. **Restore row ranges in one pass.** Selection synchronization walks visible rows once and selects contiguous
   ranges. Previously, each appended selected node cleared and reconstructed the selection, searching the entire
   row collection for every selected item.
3. **Scroll after layout.** A restored selection scrolls into view after the virtualizing panel has laid out the
   replacement rows. Immediate scrolling to the bottom of a freshly replaced 10,000-file list forced expensive
   layout. The pending scroll checks that its row still exists and is still selected. Hidden selected nodes stay
   selected, and the last visible selected row remains the scroll target.
4. **Reuse directory comparisons.** Each file-tree sort caches comparisons between directory paths and skips
   comparing identical paths. The existing culture-sensitive ordering, folder precedence and full-name comparison
   are preserved. The cache lives only for that sort.

## Measurements

| Operation | Before | After |
| --- | ---: | ---: |
| Select all 10,000 files | 1,682 ms | 18 ms |
| Stage those 10,000 selected files, including selection restoration | Still blocked after more than 176 s; stopped | 237–245 ms |
| Stage all 10,000 files without a bulk selection | 210–212 ms | 208–210 ms |
| List the 10,000 files of a selected commit | 42–113 ms | 37–100 ms |
| Same commit: file list and first patch ready, sampled | 99–199 ms | 98–200 ms |
| Load the kernel file tree, including Git and list construction | 855–866 ms | 622–788 ms |
| Largest dispatcher delay during that tree load | 467–478 ms | 334–400 ms |
| Clear the kernel file-tree filter | 376–378 ms | 277–325 ms |

The original bulk-selection run was stopped 183 seconds after process startup, more than 176 seconds after its
Stage command began. It is a censored measurement, not a completed staging time. Its Git Trace2 log shows
`git update-index --add --stdin` succeeding in **95 ms**; an eight-second UI-thread profile sampled entirely inside
selection restoration. A final patched run spent **94 ms** in the same Git operation and **245 ms** in the whole
Stage handler. Changing the Git backend would not address this bottleneck.

The first selection fix exposed a subsequent **5.7-second** layout pause after unstaging and restoring the large
selection. Deferring scrolling removed that pause. In the repeated final workload, unstaging used a roughly
296–300 ms handler followed by a 68–73 ms maximum sampled dispatcher delay during its reload. The layout-pause
comparison is against an intermediate build, not a separately completed original-build bulk-selection run.

There is no demonstrated substantial improvement in ordinary Stage all or first-patch readiness. The first-ever
exploratory staging run, which wrote new blobs, took about 650 ms; subsequent comparisons reuse existing objects.
The file-tree change is a modest improvement, while the selection fix removes the major usability failure.

## Correctness and regression validation

Git independently verified 10,000 staged files and no remaining worktree changes after staging, then an empty
index and all 10,000 changes after unstaging. Filtering to five directories staged exactly **500 files**, leaving
**9,500 unstaged**; unstaging restored all 10,000. The mixed workload likewise returned all 1,000 changes, including
its untracked files. The application logs contain no hook-reported application exception in the completed runs.

Four new regression cases cover:

- A bulk UI selection publishes once; model-driven full and discontiguous selections survive reloading and clear correctly.
- Nested collection batches publish only their completed state and do not notify for an empty batch.
- Disposing a batch scope twice does not break later notifications.
- Sorting and filtering retain nested-directory, root-file and culturally equivalent path ordering.

Release builds and all 18 test-project invocations passed on Linux and Windows. Projects ran sequentially on
each system with `--no-build --no-restore --blame-hang-timeout 3m` and isolated settings/Git homes.

| System | Passed | Skipped | Failed |
| --- | ---: | ---: | ---: |
| Native Linux | 25,360 | 167 | 0 |
| Windows 10 LTSC VM | 25,753 | 40 | 0 |

Windows includes all 191 native UI integration tests. Existing obsolete Avalonia `Watermark` warnings appeared
in the builds; there were no build errors. No translations changed.

## Limits and remaining work

Staging still performs Git work synchronously; slow storage or substantially larger file contents can cause a pause.
The kernel tree still takes several hundred milliseconds of UI work. Moving construction off the UI thread would
need cancellation and careful selection handling and is not part of this patch. Unrestricted history loading,
containment queries, giant individual diffs, binary-heavy repositories and network filesystems are unchanged.
The first patch of a selected commit is loaded on demand; this test does not render all 10,000 patches simultaneously.
Windows regression coverage does not establish equivalent Windows performance gains; macOS was not available.

## Local evidence

`/home/julius/VirtualMachines/gitextensions-qa/performance-large/` retains the disposable repositories, portable
baseline/improved applications, external startup hook, measurement scripts, Git Trace2 logs and EventPipe traces.
The reported comparisons use `*-verified-*` and final `*-complete*` records; other records are exploratory.
`before-selection-1-aborted.json` records the bounded original-build run, and `improved-mixed-checked` records the
mixed-file correctness check. These artifacts and repository changes are not application dependencies or committed fixtures.

`linux/totals.json`, `windows-totals.json` and `windows-evidence.zip` retain suite results. All QA application and
reporter processes were closed; the Windows VM was shut down. Its normal qa settings folder and Git Extensions
registry key remain absent. The host Git config hash is unchanged
(`c13840001af4a4654f5684a0b06d4444e4e66ee98c62e3154eb9061e4280358f`). The Git and kernel clones remain clean;
both synthetic repositories retain their intended unstaged changes and have empty indexes for repeat testing.
