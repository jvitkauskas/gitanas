# Commit graph reference images

`RevisionGraphGoldenTests` draws this port's real graph renderer for linear, merge and
crossing histories at 100%, 125%, 150% and 200%, on fixed light/dark backgrounds. The
fixtures have no fonts or control-theme dependencies. Their colors are fixed to isolate
geometry, clipping, non-relative coloring and the current-commit outline.

Normal test runs compare pixels and fail on missing references or changes beyond the
small antialiasing tolerance. Actual images are attached to the test result; failures also
attach a magenta difference map. These tests run with the normal Avalonia test project.

For an intentional rendering change, regenerate explicitly:

```sh
GE_UPDATE_GOLDENS=1 dotnet test tests/app/UnitTests/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  -c Release -p:UseAppHost=false --filter FullyQualifiedName~RevisionGraphGoldenTests
```

Generation marks cases inconclusive, not passed. Review all changed PNGs, then rerun
without the variable before committing them. Never enable regeneration in CI.

The saved-image testing approach is inspired by Nikola (@begota98)'s Avalonia port,
[PR #13189](https://github.com/gitextensions/gitextensions/pull/13189). Fixtures and
rendering are adapted to our own graph and test-host architecture.
