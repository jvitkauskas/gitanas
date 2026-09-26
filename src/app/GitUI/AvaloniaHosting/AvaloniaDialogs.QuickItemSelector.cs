using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the quick picker of the revision grid (docs/avalonia-port/PLAN.md).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  Shows the Avalonia port of <c>FormQuickGitRefSelector</c> at <paramref name="location"/> (in screen pixels, as
    ///  <c>GetQuickItemSelectorLocation</c>), or centered over its owner when no location is supplied.
    /// </summary>
    /// <param name="selected">The chosen ref, or <see langword="null"/> if cancelled (or there are no refs).</param>
    public static bool TryShowQuickRefSelector(IWin32Window? owner, QuickRefAction action, IReadOnlyList<IGitRef> refs, Point? location, out IGitRef? selected)
    {
        selected = null;
        QuickItemSelectorViewModel viewModel = QuickItemSelectorViewModel.ForRefs(ViewStrings.Load<QuickItemSelectorStrings>(), action, refs);
        selected = ShowQuickItemSelector(owner, viewModel, location) as IGitRef;
        return true;
    }

    /// <summary>Shows the Avalonia port of <c>FormQuickStringSelector</c> at <paramref name="location"/> (in screen pixels).</summary>
    /// <param name="selected">The chosen string, or <see langword="null"/> if cancelled (or there are no strings).</param>
    public static bool TryShowQuickStringSelector(IWin32Window? owner, IReadOnlyList<string> values, Point location, out string? selected)
    {
        selected = null;
        selected = ShowQuickItemSelector(owner, QuickItemSelectorViewModel.ForStrings(ViewStrings.Load<QuickItemSelectorStrings>(), values), location) as string;
        return true;
    }

    private static object? ShowQuickItemSelector(IWin32Window? owner, QuickItemSelectorViewModel viewModel, Point? location)
    {
        // As FormQuickItemSelector.Init: without items, the picker closes at once as cancelled.
        if (viewModel.Items.Count == 0)
        {
            return null;
        }

        AvaloniaUi.EnsureInitialized(GetOptions);
        QuickItemSelectorWindow window = new()
        {
            DataContext = viewModel,
            StartupScreenPosition = location is { } point ? new global::Avalonia.PixelPoint(point.X, point.Y) : null,
        };
        return AvaloniaDialogHost.ShowDialog(window, owner?.Handle ?? 0) ? viewModel.SelectedValue : null;
    }
}
