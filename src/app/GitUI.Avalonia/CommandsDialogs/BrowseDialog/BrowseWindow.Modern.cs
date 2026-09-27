using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>
///  The layout of the Modern control theme (docs/avalonia-port/MODERN.md): the same controls, bindings and commands as the
///  default layout, moved. The toolbar goes into the title bar above the history (on macOS the window extends under it), the
///  left panel becomes a full-height sidebar on the translucent material of the window, and the filters get a bar of their
///  own above the history.
/// </summary>
public partial class BrowseWindow
{
    /// <summary>The height of the title bar with the toolbar in it (the unified toolbar of macOS).</summary>
    private const double ModernTitleBarHeight = 52;

    /// <summary>The room of the window buttons of macOS (close, minimize, zoom) at the left of the title bar.</summary>
    private const double TrafficLightsWidth = 78;

    private void ApplyModernLayout()
    {
        if (!GitExtensionsAvaloniaApp.IsModern || Content is not DockPanel root)
        {
            return;
        }

        Classes.Add("modern");
        Styles.Add(new StyleInclude((Uri?)null) { Source = new Uri("avares://GitUI.Avalonia/Themes/ModernBrowse.axaml") });

        // The toolbar and the filters, out of the top of the window: above the history.
        root.Children.Remove(toolbar);
        toolbar.Children.Remove(filterToolBarHost);
        Border toolbarBar = new() { Classes = { "modernToolbar" }, Child = toolbar };
        toolbarBar.Bind(IsVisibleProperty, toolbar.GetObservable(IsVisibleProperty));
        Border filtersBar = new() { Classes = { "modernFilters" }, Child = filterToolBarHost };
        filtersBar.Bind(IsVisibleProperty, filterToolBarHost.GetObservable(IsVisibleProperty));

        // The content column: the toolbar, the filters, then the grid and the tabs (contentGrid keeps its rows).
        mainSplit.Children.Remove(contentGrid);
        Grid content = new() { Classes = { "modernContent" }, RowDefinitions = new RowDefinitions("Auto,Auto,*") };
        Grid.SetColumn(content, 2);
        Grid.SetRow(filtersBar, 1);
        Grid.SetRow(contentGrid, 2);
        content.Children.Add(toolbarBar);
        content.Children.Add(filtersBar);
        content.Children.Add(contentGrid);
        mainSplit.Children.Add(content);
        mainSplit.Margin = default;
        contentGrid.Margin = new Thickness(0, 0, 0, 0);

        // The sidebar: the left panel on the material of the sidebar, below the title bar.
        mainSplit.Children.Remove(leftColumn);
        Border sidebar = new() { Classes = { "modernSidebar" }, Child = leftColumn };
        mainSplit.Children.Insert(0, sidebar);
        leftPanel.FindControl<ListBox>("tree")?.SetValue(ListBox.BackgroundProperty, Brushes.Transparent);

        if (OperatingSystem.IsMacOS())
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = ModernTitleBarHeight;
            TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.None];
            Background = Brushes.Transparent;
            leftColumn.Margin = new Thickness(0, ModernTitleBarHeight, 0, 0);

            // Without the sidebar, the toolbar leaves the room of the window buttons.
            sidebar.GetObservable(BoundsProperty).Subscribe(bounds =>
                toolbarBar.Padding = new Thickness(bounds.Width < 10 ? TrafficLightsWidth : 8, 2, 8, 2));
        }
    }
}
