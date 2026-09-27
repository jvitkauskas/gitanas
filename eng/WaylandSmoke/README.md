# Native Wayland smoke test

This opt-in tool tests the port's actual Avalonia backend initialization and rename dialog,
using a separate Sway compositor with two virtual outputs. It does not load a repository,
change the user's monitor configuration, or save application settings. It is intentionally
outside the main solution and normal headless test run.

Required: Linux, .NET 10 SDK, Sway, swaymsg, setpriv (util-linux); grim optionally captures screenshots.

```sh
python3 eng/WaylandSmoke/run.py /tmp/gitextensions-wayland-results
```

The runner prevents Sway from acquiring elevated capabilities (including realtime scheduling)
with `setpriv --no-new-privs`. This also supports hosts with a zero realtime CPU limit.
It builds its host, starts Sway with XWayland disabled and no DISPLAY, then tests
Classic and Modern in light and dark colors. For each combination it:

- moves a real dialog from 125% to 150% and back, asserting actual RenderScaling;
- opens an owned modal, checks its ownership and pending completion task, and moves it
  to the differently scaled output;
- closes the modal and checks task completion;
- checks that the round trip preserves the owner's logical client size within one DIP;
- verifies native Wayland protocol traffic and saves stage reports, logs and optional PNGs.

The harness checks ownership and modal completion, not physical input blocking or
screen-reader behavior. Headless compositor tests complement the manual GNOME/KDE tests;
they do not reproduce every desktop's decorations, GPU or input stack.

The test host exits after 90 seconds even if the runner fails. The runner terminates the
probe/compositor and removes its temporary runtime/config/data directories on exit. Output
artifacts stay in the explicitly chosen directory. The source also serves as a minimal
reproducer for fractional scaling regressions without embedding diagnostic code in the app.

Inspired by Nikola (@begota98)'s conformance work in
[gitextensions/gitextensions#13189](https://github.com/gitextensions/gitextensions/pull/13189),
reviewed at a3133e50d080bc03654c2e17a367e06807126e45; adapted to this port's own hosting layer.
