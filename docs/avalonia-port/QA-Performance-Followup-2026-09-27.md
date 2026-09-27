# Measured performance follow-up — 2026-09-27

This pass evaluates the recommendations in the independent performance review against the actual application.
The baseline is `4844b6132`; the retained changes prioritize UI responsiveness and inexpensive repository maintenance.
The original review was left unchanged.

## Retained changes and live UI measurements

The full Linux kernel checkout has 96,045 tracked files and HEAD
`fd179f8a05be3ccae366b9b96e176b51fbe54aab`. The application loads its normal capped 100,000 history rows. The host has 16 logical CPUs, .NET 10.0.11
(SDK 10.0.111) and Git 2.55.0.
Baseline and modified portable builds ran on the same Linux desktop, with isolated HOME/XDG directories and private
D-Bus sessions. A diagnostic startup hook invokes the real view-model commands and samples the UI dispatcher every
16 ms. No benchmark code or startup hook is included in the application.

There were two application launches per build, with five repetitions of each operation per launch. The first
repetition is treated as warm-up, leaving eight samples per operation. The table reports medians. These are warm
cache Linux measurements; no corresponding speedup is claimed for Windows, macOS, cold disks or network shares.
The final UI timing runs were sequential and did not overlap builds or test suites.

| Operation | Baseline | Retained change | Meaning |
|---|---:|---:|---|
| Clear filter, restoring 96,045-file tree: synchronous command duration | 243.5 ms | 0.39 ms | The command returns while the tree is built in the background |
| Same operation: longest dispatcher tick gap | 258.2 ms | 42.1 ms | Includes applying the completed tree, not just dispatching work |
| Same operation: completed tree | 242.9 ms | 243.1 ms | Total latency is unchanged; clearing skips the typing delay |
| Selective `^README$` filter: completed tree | 6.5 ms | 106.5 ms | Intentional 100 ms debounce for nonempty filters on large trees |
| 60,000-line diff: synchronous load duration | 142.0 ms | 9.6 ms | Diff text and line colors are available before word highlighting |
| Same diff: longest dispatcher tick gap | 144.4 ms | 20.7 ms | The later highlighting update is included |
| Same diff: highlighting complete | 142.0 ms | about 153.2 ms | The asynchronous completion timestamp is sampled, with roughly 16 ms precision |

The first opening of the full file tree was measured once per launch: its longest tick gap fell from
421.7–480.5 ms to 32.1–32.7 ms. Completion took 692–900 ms before and 761–789 ms after; there is no established
improvement in total loading time. The diff fixture is synthetic, with 30,000 removed/added line pairs and about
3.5 million characters, matching the expensive analyzer path identified in the review. It is not a measured kernel
commit diff, nor a claim that ordinary small diffs are 15 times faster.

The scoped implementation:

- File Tree mode uses background construction at 5,000 files or more, snapshots filter/grouping options, and
  cancels obsolete builds. Nonempty filter changes debounce for 100 ms; clearing the filter and loading a revision
  start immediately. Applying nodes and restoring selection still happen on the UI thread. Small trees and the
  commit/staging lists retain their synchronous behavior.
- Diffs of at least 128K characters defer inline word analysis when called with a synchronization context. The plain diff
  is displayed first. A later notification updates the highlighting without reloading the document or resetting
  the selection. Replacing the document cancels/discards obsolete markers. Small diffs remain immediate.
- These are responsiveness changes, not reductions in the algorithms' allocation or CPU requirements. Observed peak
  RSS across the whole UI sequence was 922–942 MiB before and 968–969 MiB after. Two runs are insufficient to establish
  retained-memory behavior, but there is no memory-saving claim.

## Commit-graph maintenance

The new **Repository → Git maintenance → Optimize history queries** command runs
`git maintenance run --task=commit-graph` through the existing cancellable process dialog. It does not register a
scheduler, change global Git settings, fetch, or run a full garbage collection. Automatic maintenance on repository
open is deliberately not included.

The underlying maintenance command was exercised on a separate bare mirror with a shared, read-only object source. A fresh maintenance run
completed in 11.73 seconds, wrote about 85 MiB of graph data and brought `tag --contains HEAD` to about 19 ms.
The original kernel checkout was not modified and still has no commit-graph. The benchmark mirrors remain available
for reproduction.

After all builds and tests stopped, an alternating A/B run compared the same mirror with
`-c core.commitGraph=false` and `true`, one warm-up pair followed by three measured pairs per query. Outputs
were checked for equality. Medians:

| Query | Without graph | With graph |
|---|---:|---:|
| Tags containing HEAD | 8,054.7 ms | 19.0 ms |
| Branches containing v2.6.12 | 8,673.0 ms | 41.9 ms |
| Tags containing v2.6.12 | 549.2 ms | 54.2 ms |
| 100,000-commit log with full metadata/body | 818.8 ms | 807.4 ms |

This supports keeping commit-graph maintenance for containment queries. The roughly 1% log difference is too small
to claim a history-load or startup improvement. The log command uses the same full metadata/body format in both
conditions; it is a raw Git control, not a measurement of application startup.

Simply setting `core.commitGraph=true` would not generate this data: the setting already defaults to true.
[Git configuration documentation](https://git-scm.com/docs/git-config#Documentation/git-config.txt-corecommitGraph)

## Rejected or deferred recommendations

- **Triple status refresh: rejected as described.** The review's harness explicitly calls three APIs. In each of
  the four final live application runs, a requested status refresh emitted exactly one `git status` process in the
  Git Trace2 log. The status monitor already shares the parsed result. This does not prove there can never be
  overlapping requests elsewhere, but it removes the basis for the proposed 182 ms saving per poll.
- **libgit2/object cache: deferred.** No measured benefit justifies a replacement of Git access. An in-memory cache
  also cannot remove first-launch object reads. No implementation was retained.
- **Revision memory/lazy rows: deferred to separate work.** This pass does not improve unbounded history or reduce
  per-row retention; the history limit remains important.
- **Graph layout and generic lock/cache rewrites: deferred.** No additional measured UI stall justified these
  changes. Current-directory and cache-caller correctness audits can be worthwhile independently of performance.

## Validation and reproduction

Five new headless regressions cover background marker parity and UI-thread publication, replacement by plain text,
editor selection retention, rapidly superseded tree filters with preserved selection, and clearing/changing
revisions while a build is pending. Existing synchronous behavior remains covered by the previous tests.

Evidence is retained outside Git under `/home/julius/VirtualMachines/gitextensions-qa/performance-review/`:
`baseline/`, `improved/`, the diagnostic `hook/`, launch/matrix scripts, `evidence/*-final-*.jsonl`, Git Trace2 logs,
`summary-final.json`, graph experiments, build logs and test results. Early prototypes and the variant that delayed
clearing a filter are retained separately; neither is used in the final table.

Release solution builds pass on Linux and Windows. All 18 test-project invocations pass on both platforms:

| Platform | Passed | Skipped | Failed |
|---|---:|---:|---:|
| Linux | 25,391 | 167 | 0 |
| Windows 10 | 25,771 | 43 | 0 |

Windows includes 191 native UI integration tests. The five new regressions pass on both platforms. Skipped counts
are calculated as TRX `total - executed`. Windows was validated for regressions, not benchmarked for speed; macOS
was not rerun. Linux's final build has 5 existing obsolete-Watermark warnings; Windows has 29 warnings including
existing warnings and line-ending warnings from the scratch source copy, with no build errors.

The timed application instances exited, no scratch-HOME processes remained, and the Windows QA task was removed.
The source kernel working tree is clean and its original absence of commit-graph files is preserved. Scratch
mirrors, portable builds and evidence remain available. No desktop settings or normal application settings were
changed.
