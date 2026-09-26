# Large-repository performance — 2026-09-26

Investigated the real Avalonia application on the Linux host, starting from `3f324e640` on `avalonia`.
Three small changes remove unnecessary history-row work and a multi-second deep-history navigation freeze.

## Repositories and measurement

| Repository | Commit checked out | Reachable commits, all refs | Tracked files | Refs |
| --- | --- | ---: | ---: | ---: |
| Git, `https://github.com/git/git.git` | `0f8e75abebff0877cae681a3d5ff31ac47f54220` | 85,787 | 4,852 | 1,021 |
| Linux, `https://git.kernel.org/pub/scm/linux/kernel/git/torvalds/linux.git` | `fd179f8a05be3ccae366b9b96e176b51fbe54aab` | 1,484,045 | 96,045 | 950 |

Both are fresh, non-shallow clones. GitHub's Linux Git endpoint did not return even its reference advertisement;
the official kernel.org mirror worked. That server ignored `--filter=blob:none`, so the resulting clone includes
all historical blobs: 11,844,313 packed objects, a 3.43 GiB pack, and about 5.3 GiB including the checkout.
No repository maintenance or commit-graph generation was applied before these application measurements.

The host has a Ryzen 7 7700, 16 logical CPUs and 64 GB installed RAM. Runs used Git 2.55.0, Release builds,
SDK 10.0.201 and .NET runtime 10.0.5,
native Wayland under Hyprland, and the existing 3840×2160 display at 200% scaling. VMs, builds, profilers and test
suites were inactive during the comparison runs. These are warm-filesystem, fresh-process measurements, not
cold-disk benchmarks. The first exploratory kernel run overlapped a short unit test and is excluded from the
comparison table.

A temporary external .NET startup hook observed the actual window and view models. It recorded the history's
`Loaded` event, process RSS and a dispatcher heartbeat every 100 ms, and drove history refresh, selection,
scroll-into-view, tabs and file filters. It did not replace Git or render headlessly. Startup means process-hook
initialization to completed history loading, not necessarily the first painted frame. Refresh means the history
model's reload, not a complete repository rescan. A heartbeat delay measures UI responsiveness, not frame rate;
synchronous selection-handler durations were recorded separately. RSS includes native and managed allocations.

Both variants used the same portable scratch settings and redirected HOME, XDG and Git configuration. The normal
history cap is 100,000 commits; Linux therefore displays 100,002 rows including the working tree and index.
Git displays 85,789 rows. Each timed process performed five history refreshes and ten selections. Two interleaved
kernel comparisons were followed by a final-code run. EventPipe sampling and Git Trace2 identified hot paths;
the sampled trace runs are separate from the unprofiled timing comparisons.

## Changes and results

1. `RevisionRefLabels.Build` returns immediately for commits without visible references. Previously it sorted
   empty reference lists and rebuilt the repository's remote-tracking lookup for every history row. A sampled
   four-refresh Git run attributed about 1.32 seconds of UI-thread time to reference-label construction.
2. `RevisionGridRow` formats its date on first display and caches it. Previously loading history formatted every
   row's date, including tens of thousands of rows the user had not scrolled to. The same profile attributed
   about 0.62 seconds to date formatting. The selected timestamp and absolute/relative display option are preserved.
3. `UpdateGraphColumnWidth` stops scanning once the renderer's existing 40-lane width limit is reached.
   A jump to row 50,000 previously queried lane counts for the whole 100,002-row graph cache, creating unused
   per-row lane dictionaries on the UI thread. The focused trace attributed **8.20 seconds** to this method,
   almost entirely `BuildSegmentLanes`. Further rows cannot increase the rendered width. Reload still resets
   the width; drawing, the history limit and graph layout settings are unchanged.

| Operation | Before | After |
| --- | ---: | ---: |
| Linux, default-cap startup | 3.31–3.38 s | 2.76–2.87 s |
| Linux, median history refresh | 1.43 s | 1.29–1.35 s |
| Linux, median largest UI delay per refresh | 247 ms | 189–196 ms |
| Linux, largest startup UI delay | 2.05–2.14 s | 0.96–1.13 s |
| Git, startup in the matched final pair | 3.36 s | 2.27 s |
| Git, median history refresh in that pair | 991 ms | 898 ms |
| Linux, jump to row 50,000: subsequent UI delay | 7.64 s | 46 ms |
| Same jump: requested graph cache ready | 8.96 s | 1.27 s |
| Same jump workload: peak RSS | 2.50 GiB | 1.51 GiB |

The jump comparison isolates the width fix: both applications already contain the first two optimizations and
perform the same file-tree/diff actions beforehand. Selecting and requesting the jump itself took about 78 ms
in both runs; the table's UI delay is the subsequent freeze. The background graph calculation still takes time.
The final code's ordinary kernel selection handlers ranged from 27 to 44 ms, with no demonstrated improvement;
the changes target loading and graph-width work. Process memory across repeated refreshes varied with garbage
collection, so the deep-jump result is not a claim of a universal memory reduction.

## Other exercised workloads and remaining costs

- The kernel HEAD merge lists 2,223 changed files. The Diff tab loaded its file list and first patch. The File tree
  tab loaded all 96,045 files; filtering to `^README$`, displaying README, clearing the filter, jumping to rows
  50,000 and 99,999, and returning to HEAD completed without an application exception.
- Building the full file tree produced a roughly 415 ms dispatcher delay. Clearing its filter rebuilt the tree
  synchronously in about 372 ms. Moving tree construction/filtering off the UI thread, with cancellation and
  preserved selection, is a useful follow-up; it is not changed in this batch.
- Git Trace2 recorded some `git tag --contains` calls around 6.3–6.5 seconds and a deep-history
  `git branch --merged` call at 5.85 seconds. These run in the background but delay commit metadata. They are
  distinct from the graph-width freeze. Repository commit-graph maintenance and containment-query caching are
  candidates for a separate comparison, not measured improvements here.
- Raising the scratch history cap to 2,000,000 loaded **all 1,484,047 rows**. One run before/after the first two
  changes took 29.81/24.70 seconds, with largest UI delays of 12.20/8.11 seconds. Peak RSS was 5.15/5.68 GiB;
  memory did not improve in this extreme run. These single runs are exploratory, not statistical claims. The
  existing 100,000-commit default remains appropriate. Incremental/lazy row models and bounded UI batches would
  be needed to make unbounded history pleasant; this patch does not claim to solve that workload.

## Regression validation

Release builds and all 18 test projects passed on both systems. Projects ran sequentially per system with
`--no-build --no-restore --blame-hang-timeout 3m` and isolated Git/settings homes.

| System | Passed | Skipped | Failed |
| --- | ---: | ---: | ---: |
| Native Linux host | 25,356 | 167 | 0 |
| Windows 10 Enterprise LTSC 2021 VM | 25,749 | 40 | 0 |

Windows includes all 191 native UI integration tests. Existing obsolete `Watermark` and ConEmu warnings, plus
Windows temporary-overlay line-ending warnings, appeared in build logs; no build errors remain.

Seven new test cases cover absent/hidden reference labels without tracking-data reads, both timestamp choices
with absolute/relative date formatting, and graph width at the render limit followed by a narrow-history reload.
Existing graph ordering, selection, labels, rendering, dates and UI tests are also run. No translation changed.

This is a native Linux performance investigation, with Windows regression validation. It does not establish
equivalent speedups on Windows/macOS, disk-cold or network repositories, blame or pathological giant text files.
The previously documented intermittent Wayland popup-order defect is outside these workloads and is not fixed.

## Local evidence and reproduction

Evidence and scratch applications remain under `/home/julius/VirtualMachines/gitextensions-qa/performance/`:

- `repos/git`, `repos/linux`: the clones above; application tests only read them.
- `baseline`, `improved`: portable application copies, with the normal 100,000-commit limit restored.
- `hook/`, `launch.py`, `benchmark.py`, `exercise.py`, `full-history.py`, `summarize.py`: the local measurement harness.
- `evidence/repositories.json`, `measurements.json`, per-run JSONL/summary files and Git Trace2 logs.
- `evidence/git-refresh.nettrace`, `linux-jump-profile.nettrace`, their Speedscope exports and analysis text:
  the sampled hot paths before their respective fixes. The installed `tools/dotnet-trace` is version 10.0.745401.
- `linux/`: full Linux suite logs, TRX files and totals. `windows-evidence.zip`, `windows-totals.json` and
  `windows-cleanup.json`: the corresponding Windows results and cleanup checks.

For another local comparison, use a fresh output name, for example:

```sh
python3 benchmark.py improved linux-repeat /home/julius/VirtualMachines/gitextensions-qa/performance/repos/linux
```

Run it from that evidence root while other QA/build workloads are stopped. The hook adds measurement overhead to
both variants. The harness and captured binaries are local diagnostic artifacts, not application dependencies.

All tested application/reporter processes were closed and the Windows VM shut down. Both repository checkouts
remain clean. The ordinary Windows qa settings folder and `HKCU\Software\GitExtensions` remain absent.
The host's Git config hash is unchanged (`c13840001af4a4654f5684a0b06d4444e4e66ee98c62e3154eb9061e4280358f`),
and no display settings were changed.
