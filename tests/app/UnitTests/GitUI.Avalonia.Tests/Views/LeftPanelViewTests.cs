using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Controls.LeftPanel;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.LeftPanel;
using GitUIPluginInterfaces;
using static GitUI.AvaloniaTests.ViewModels.LeftPanelViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the left panel (the port of <c>RepoObjectsTree</c>).</summary>
[TestFixture]
public sealed class LeftPanelViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (Window window, LeftPanelView view, LeftPanelViewModel panel, FakeLeftPanelHost _) = Show();

        // Expanded folders and remotes, an underlined (multi-selected) node, a search result and a merged branch.
        panel.BranchesTree.Children[2].IsExpanded = true;
        panel.RemotesTree.Children[0].IsExpanded = true;
        panel.RemotesTree.Children[2].IsExpanded = true;
        panel.TagsTree.IsExpanded = true;
        panel.StashesTree.IsExpanded = true;
        ((LeftPanelBranchNode)panel.BranchesTree.Children[1]).IsMerged = true;
        panel.ClickNode(panel.TagsTree.Children[0], multiple: false, includingDescendants: false);
        panel.SearchText = "login";
        panel.Search();
        Dispatcher.UIThread.RunJobs();

        view.Tree.GetVisualDescendants().OfType<ListBoxItem>().Should().HaveCountGreaterThan(15);
        TextBlock current = TextOf(view, "main (1↑ 2↓)");
        current.FontWeight.Should().Be(global::Avalonia.Media.FontWeight.Bold);
        TextOf(view, "v1.0").TextDecorations.Should().NotBeNull("the multi-selected node is underlined");
        TextOf(view, "login").Classes.Should().Contain("grayed", "its revision is not in the grid");
        SaveScreenshot(window.CaptureRenderedFrame(), $"left-panel-{theme}");
        window.Close();
    });

    [Test]
    public Task The_context_menu_is_filled_for_the_selected_node() => OnUiThreadAsync(() =>
    {
        (Window window, LeftPanelView view, LeftPanelViewModel panel, FakeLeftPanelHost host) = Show();
        panel.ClickNode(panel.TagsTree.Children[0], multiple: false, includingDescendants: false);
        panel.SelectedNode = panel.TagsTree.Children[0];

        IReadOnlyList<LeftPanelMenuItem> items = view.FillContextMenu();

        items.Should().NotBeEmpty();
        List<MenuItem> menuItems = [.. view.Menu.Items.OfType<MenuItem>()];
        menuItems.Select(m => m.Header).Should().Contain(["_Filter for selected", "Chec_kout tag revision...", "_Delete tag...", "_Sort by"]);
        menuItems.Single(m => (string?)m.Header == "_Sort by").Items.OfType<MenuItem>().Should().Contain(m => m.IsChecked);
        view.Menu.Items.OfType<Separator>().Should().NotBeEmpty();
        menuItems.Single(m => (string?)m.Header == "_Delete tag...").Icon.Should().BeOfType<Image>();

        menuItems.Single(m => (string?)m.Header == "_Delete tag...").RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.Runs.Should().ContainSingle().Which.Action.Should().Be(LeftPanelAction.DeleteTag);

        panel.ClickNode(panel.TagsTree.Children[0], multiple: true, includingDescendants: false);
        panel.SelectedNode = null;
        view.FillContextMenu().Should().BeEmpty("the menu does not open without items");
        window.Close();
    });

    [Test]
    public Task A_click_selects_the_node_and_its_revision_and_ctrl_click_adds_it_to_the_multi_selection() => OnUiThreadAsync(() =>
    {
        (Window window, LeftPanelView view, LeftPanelViewModel panel, FakeLeftPanelHost _) = Show();
        LeftPanelNode experiment = panel.BranchesTree.Children[1];
        LeftPanelNode tag = panel.TagsTree.Children[0];
        panel.TagsTree.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();

        Click(window, TextOf(view, "experiment"), RawInputModifiers.None);
        panel.SelectedNode.Should().BeSameAs(experiment);
        panel.Grid.SelectedRow!.Subject.Should().Be("Experiment");
        panel.GetMultiSelectedNodes().Should().Equal(experiment);

        Click(window, TextOf(view, "v1.0"), TestKeys.Command);
        panel.SelectedNode.Should().BeSameAs(tag);
        panel.GetMultiSelectedNodes().Should().Equal(experiment, tag);
        view.Tree.SelectedItem.Should().BeSameAs(view.FlatTree!.RowOf(tag));

        // A click on the selected node selects its revision again (e.g. after selecting another one in the grid).
        panel.Grid.SelectedRow = panel.Grid.Rows[0];
        Click(window, TextOf(view, "v1.0"), RawInputModifiers.None, x: 20);
        panel.Grid.SelectedRow!.Subject.Should().Be("Release");
        panel.GetMultiSelectedNodes().Should().Equal(tag);
        window.Close();
    });

    [Test]
    public Task The_hotkeys_apply_to_the_selected_node_but_not_to_the_text_of_the_search_box() => OnUiThreadAsync(() =>
    {
        (Window window, LeftPanelView view, LeftPanelViewModel panel, FakeLeftPanelHost host) = Show();
        panel.SelectedNode = panel.BranchesTree.Children[1];
        view.Tree.Focus();

        view.ProcessHotkey(0x71).Should().BeTrue("F2");
        view.ProcessHotkey(0x2E).Should().BeTrue("Delete");
        view.ProcessHotkey(0x41).Should().BeFalse("A is no hotkey");
        host.Runs.Select(r => r.Action).Should().Equal(LeftPanelAction.RenameBranch, LeftPanelAction.DeleteBranch);

        view.SearchBox.Focus();
        Dispatcher.UIThread.RunJobs();
        view.ProcessHotkey(0x2E).Should().BeFalse("Delete edits the text of the search box");
        view.ProcessHotkey(0x72).Should().BeTrue("F3 searches");
        window.Close();
    });

    [Test]
    public Task The_toolbar_hides_the_trees_and_enter_in_the_search_box_searches() => OnUiThreadAsync(() =>
    {
        (Window window, LeftPanelView view, LeftPanelViewModel panel, FakeLeftPanelHost _) = Show();
        ToggleButton showTags = view.GetLogicalDescendants().OfType<ToggleButton>().Single(b => b.Name == "showTagsButton");
        showTags.IsChecked.Should().BeTrue();

        showTags.IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        panel.ShowTags.Should().BeFalse();
        panel.Trees.Should().NotContain(panel.TagsTree);

        view.SearchBox.Text = "experiment";
        view.SearchBox.Focus();
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        panel.SelectedNode!.Text.Should().Be("experiment");
        window.Close();
    });

    [Test]
    public Task The_main_window_shows_the_left_panel_beside_the_grid_and_its_toggle_hides_it() => OnUiThreadAsync(() =>
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost _, FakeLeftPanelSettings _) = Create();
        BrowseViewModel viewModel = new(
            new BrowseStrings(),
            new NullBrowseHost(),
            panel.Grid,
            new CommitInfoViewTests.FakeHost(),
            new DiffViewModelTests.FakeViewerHost(),
            new FileStatusListStrings(),
            new FileStatusTreeOptions())
        {
            LeftPanel = panel,
        };
        BrowseWindow window = new() { Width = 1100, Height = 700, DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.LeftPanel.IsVisible.Should().BeTrue();
        window.LeftPanel.Bounds.Width.Should().BeGreaterThan(200);
        window.ToggleLeftPanelButton.IsChecked.Should().BeTrue();
        ToolTip.GetTip(window.ToggleLeftPanelButton).Should().Be("Toggle left panel");
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-left-panel");

        window.ToggleLeftPanelButton.IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        panel.IsVisible.Should().BeFalse();
        window.LeftPanel.IsVisible.Should().BeFalse();
        window.RevisionGrid.Bounds.Width.Should().BeGreaterThan(1000, "the grid takes the width of the panel");

        window.ToggleLeftPanelButton.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        window.LeftPanel.Bounds.Width.Should().BeGreaterThan(200);

        // Without a left panel (e.g. in the dashboard), neither the panel nor its toggle is shown.
        viewModel.LeftPanel = null;
        Dispatcher.UIThread.RunJobs();
        window.LeftPanel.IsVisible.Should().BeFalse();
        window.ToggleLeftPanelButton.IsVisible.Should().BeFalse();
        viewModel.LeftPanel = panel;
        Dispatcher.UIThread.RunJobs();
        window.LeftPanel.IsVisible.Should().BeTrue();
        window.Close();
    });

    private static (Window Window, LeftPanelView View, LeftPanelViewModel Panel, FakeLeftPanelHost Host) Show()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        LeftPanelView view = new() { DataContext = panel };
        Window window = new() { Width = 320, Height = 720, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, view, panel, host);
    }

    private static TextBlock TextOf(LeftPanelView view, string text)
        => view.Tree.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == text);

    // Not a double click: another point than the last click, as the time between the clicks does not count.
    private static void Click(Window window, Visual target, RawInputModifiers modifiers, double x = 5)
    {
        Point point = target.TranslatePoint(new Point(x, target.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class NullBrowseHost : IBrowseHost
    {
        public event EventHandler? RepositoryChanged
        {
            add { }
            remove { }
        }

        public string GetTitle() => "repo (main) - Gitanas";

        public string GetCurrentBranch() => "main";

        public void Run(BrowseCommand command, BrowseSelection selection)
        {
        }

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FileStatusGroup>>([]);

        public IReadOnlyList<BrowseMenuItem> GetRepositoriesMenu(bool favourites) => [];
    }
}
