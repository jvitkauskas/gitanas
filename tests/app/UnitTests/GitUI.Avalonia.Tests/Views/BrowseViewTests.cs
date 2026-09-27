using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitCommands.Git.Gpg;
using GitExtensions.Extensibility.BuildServerIntegration;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.Blame;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.LeftPanel;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless tests of the main window (the port of <c>FormBrowse</c>, first part).</summary>
[TestFixture]
public sealed class BrowseViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();

        window.Title.Should().Be("repo (main) - Gitanas");
        viewModel.CurrentBranch.Should().Be("main");
        viewModel.Grid.SelectedRow.Should().BeSameAs(viewModel.Grid.Rows[0], "the first revision is selected once loaded");
        viewModel.CommitInfo.RevisionInfo.Should().NotBeEmpty("the commit info of the selected revision is shown");
        host.DiffsRequested.Should().ContainSingle().Which.Should().Equal(viewModel.Grid.Rows[0].Subject);
        viewModel.Files.AllEntries.Select(e => e.Item.Name).Should().Equal("src/file.cs");
        window.MainMenu.Items.Cast<MenuItem>().Where(m => m.IsVisible).Select(m => m.Header).Should().Equal("_Start", "_Repository", "_View", "_Commands", "_Plugins", "_Tools", "_Help");
        SaveScreenshot(window.CaptureRenderedFrame(), $"browse-commit-{theme}");

        window.Tabs.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.Diff);
        SaveScreenshot(window.CaptureRenderedFrame(), $"browse-diff-{theme}");
        window.Close();
    });

    [Test]
    public Task The_commit_hashes_of_the_commit_info_are_links_that_select_their_commit() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        RevisionGridRow target = viewModel.Grid.Rows[3];
        host.ResolvedCommits["a1a1a1a1"] = target.ObjectId;
        viewModel.Grid.SetSelectedRows([viewModel.Grid.Rows[2]]);
        Dispatcher.UIThread.RunJobs();

        viewModel.CommitInfo.Header.Single(line => line.Label == "Parent:").ValueXhtml.Should().Contain("gitext://gotocommit/", "the hashes are links in the main window");
        viewModel.CommitInfo.OnLinkClicked("gitext://gotocommit/a1a1a1a1");
        Dispatcher.UIThread.RunJobs();

        viewModel.Grid.SelectedRow!.ObjectId.Should().Be(target.ObjectId);

        // The containing branches and tags are links too.
        RevisionGridRow branchTarget = viewModel.Grid.Rows[1];
        host.ResolvedCommits["master"] = branchTarget.ObjectId;
        viewModel.CommitInfo.ShowBranchesAsLinks.Should().BeTrue();
        viewModel.CommitInfo.OnLinkClicked("gitext://gotobranch/master");
        Dispatcher.UIThread.RunJobs();
        viewModel.Grid.SelectedRow!.ObjectId.Should().Be(branchTarget.ObjectId);
        window.Close();
    });

    [Test]
    public Task A_double_click_on_a_revision_views_it_as_the_WinForms_grid() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        viewModel.Grid.SetSelectedRows([viewModel.Grid.Rows[2]]);
        Dispatcher.UIThread.RunJobs();

        // Enter in the grid activates the revision as a double click does.
        window.GetVisualDescendants().OfType<RevisionGridView>().Single().GetVisualDescendants().OfType<DataGrid>().Single().Focus();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        host.Runs.Should().ContainSingle();
        host.Runs[0].Command.Should().Be(BrowseCommand.ViewSelectedRevisions);
        host.Runs[0].Selection.LatestSelectedFirst.Select(r => r.ObjectId).Should().Equal(viewModel.Grid.Rows[2].ObjectId);
        window.Close();
    });

    [Test]
    public Task The_Modern_theme_moves_the_toolbar_and_the_filters_above_the_history() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        WrapPanel toolbar = window.FindControl<WrapPanel>("toolbar")!;
        Panel filters = window.FindControl<Panel>("filterToolBarHost")!;
        Grid contentGrid = window.FindControl<Grid>("contentGrid")!;

        window.Classes.Should().Contain("modern");
        (toolbar.Parent as Border)!.Classes.Should().Contain("modernToolbar");
        (filters.Parent as Border)!.Classes.Should().Contain("modernFilters");
        Grid content = (Grid)contentGrid.Parent!;
        Grid.GetRow((Control)toolbar.Parent!).Should().Be(0);
        Grid.GetRow((Control)filters.Parent!).Should().Be(1);
        Grid.GetRow(contentGrid).Should().Be(2);
        Grid.GetColumn(content).Should().Be(2, "the content keeps its column beside the sidebar");
        contentGrid.RowDefinitions.Should().HaveCount(3, "the rows of the grid and the tabs are unchanged");

        // The same controls, still bound: a command of the moved toolbar runs.
        window.FindControl<Button>("pushButton")!.Command!.Execute(BrowseCommand.Push);
        host.Runs[^1].Command.Should().Be(BrowseCommand.Push);
        window.Close();
    });

    [Test]
    public Task The_menus_and_the_toolbar_run_their_commands_on_the_selection() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        List<GitRevision> history = [.. viewModel.Grid.Rows.Select(r => r.Revision)];
        viewModel.Grid.SetSelectedRows([viewModel.Grid.Rows[3], viewModel.Grid.Rows[1]]);
        Dispatcher.UIThread.RunJobs();

        MenuItem commands = window.MainMenu.Items.Cast<MenuItem>().Single(m => (string?)m.Header == "_Commands");
        MenuItem cherryPick = commands.Items.OfType<MenuItem>().Single(m => (string?)m.Header == "Cherr_y pick...");
        cherryPick.Command!.Execute(cherryPick.CommandParameter);
        host.Runs.Should().ContainSingle();
        (BrowseCommand command, BrowseSelection selection) = host.Runs[0];
        command.Should().Be(BrowseCommand.CherryPick);
        selection.LatestSelectedFirst.Select(r => r.Subject).Should().Equal(history[1].Subject, history[3].Subject);
        selection.Descending.Select(r => r.Subject).Should().Equal(history[3].Subject, history[1].Subject);

        viewModel.RunCommand.Execute(BrowseCommand.PullDefault);
        host.Runs[^1].Command.Should().Be(BrowseCommand.PullDefault);
        viewModel.Menus[0].Children!.Should().Contain(m => m.IsSeparator);
        viewModel.PullItems.Where(i => i.Command is not null).Select(i => i.Command).Should().Equal(
            BrowseCommand.PullMerge, BrowseCommand.PullRebase, BrowseCommand.Fetch, BrowseCommand.FetchAll, BrowseCommand.FetchPruneAll, BrowseCommand.OpenPullDialog);

        // A dialog changed the repository: the revisions are loaded again, keeping the selection.
        host.Branch = "feature";
        host.RaiseRepositoryChanged();
        Dispatcher.UIThread.RunJobs();
        viewModel.CurrentBranch.Should().Be("feature");
        viewModel.Grid.SelectedRow.Should().NotBeNull();

        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.RunCommand.Execute(BrowseCommand.Exit);
        closed.Should().BeTrue();
        window.Close();
    });

    [Test]
    public Task The_file_tree_tab_shows_all_the_files_of_the_selected_revision() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        DiffViewModelTests.FakeViewerHost viewerHost = host.ViewerHost;
        GitRevision first = viewModel.Grid.SelectedRow!.Revision;
        host.TreesRequested.Should().BeEmpty("the tree is loaded when its tab is shown (FillFileTree)");

        window.Tabs.SelectedIndex = 2;
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.FileTree);
        host.TreesRequested.Should().Equal(first.Subject);
        FileStatusListViewModel tree = viewModel.FileTree!;
        tree.AllEntries.Select(e => e.Item.Name).Should().BeEquivalentTo("README.md", "src/a.cs", "src/b.cs");

        // As the WinForms file tree: the files and folders of the root, the folders collapsed, and no file selected.
        tree.Nodes.Should().HaveCount(2);
        tree.Nodes.Single(n => n.Entry is null).IsExpanded.Should().BeFalse();
        tree.SelectedEntries.Should().BeEmpty();
        tree.ShowNoFiles.Should().BeFalse();

        tree.Select(e => e.Item.Name == "src/b.cs");
        Dispatcher.UIThread.RunJobs();
        viewerHost.Requested[^1].Should().Be($"src/b.cs@{first.ObjectId.ToShortString()}", "the file is shown as it is in the revision");
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-file-tree");

        // Another revision: the same file stays selected, shown in that revision.
        GitRevision other = viewModel.Grid.Rows.Select(r => r.Revision).First(r => !r.IsArtificial && r.ObjectId != first.ObjectId);
        viewModel.Grid.SelectRevision(other.ObjectId);
        Dispatcher.UIThread.RunJobs();
        host.TreesRequested.Should().Equal(first.Subject, other.Subject);
        tree.SelectedEntry!.Item.Name.Should().Be("src/b.cs");
        viewerHost.Requested[^1].Should().Be($"src/b.cs@{other.ObjectId.ToShortString()}");

        // On another tab, selecting a revision does not load the tree.
        window.Tabs.SelectedIndex = 1;
        viewModel.Grid.SelectRevision(first.ObjectId);
        Dispatcher.UIThread.RunJobs();
        host.TreesRequested.Should().HaveCount(2);
        window.Close();
    });

    [Test]
    public Task The_gpg_tab_verifies_the_signatures_of_the_selected_commit() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        host.GpgRequested.Should().BeEmpty("the signatures are verified when the tab is shown");
        TaskCompletionSource<GpgInfo?> verification = new();
        host.GpgResult = verification.Task;

        window.Tabs.SelectedIndex = 3;
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.Gpg);
        host.GpgRequested.Should().Equal(viewModel.Grid.SelectedRow!.Subject);
        viewModel.CommitGpgText.Should().Be("Loading data...", "not the result of another revision while verifying");

        verification.SetResult(new GpgInfo(CommitStatus.GoodSignature, "gpg: Good signature from \"Alice\"", TagStatus.OneGood, "gpg: Good signature (tag)"));
        Dispatcher.UIThread.RunJobs();
        viewModel.CommitGpgIcon.Should().Be("CommitSignatureOk");
        viewModel.CommitGpgText.Should().Contain("Good signature from");
        viewModel.TagGpgIcon.Should().Be("TagOk");
        viewModel.TagGpgText.Should().Be("gpg: Good signature (tag)");
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-gpg");

        // Neither signed: "not signed" and no tag row.
        host.GpgResult = Task.FromResult<GpgInfo?>(null);
        viewModel.Grid.SelectedRow = viewModel.Grid.Rows.First(r => r != viewModel.Grid.SelectedRow && !r.Revision.IsArtificial);
        Dispatcher.UIThread.RunJobs();
        host.GpgRequested.Should().HaveCount(2);
        viewModel.CommitGpgText.Should().Be("Commit is not signed");
        viewModel.CommitGpgIcon.Should().BeNull();
        viewModel.TagGpgText.Should().BeNull("without a tag the tag row is hidden");

        // A tag that is not signed.
        host.GpgResult = Task.FromResult<GpgInfo?>(new GpgInfo(CommitStatus.NoSignature, "", TagStatus.TagNotSigned, null));
        viewModel.Grid.SelectedRow = viewModel.Grid.Rows.First(r => r != viewModel.Grid.SelectedRow && !r.Revision.IsArtificial);
        Dispatcher.UIThread.RunJobs();
        viewModel.TagGpgText.Should().Be("Tag is not signed");
        viewModel.TagGpgIcon.Should().BeNull();
        window.Close();
    });

    [Test]
    public Task The_console_tab_starts_the_shell_when_it_is_first_shown() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        viewModel.HasConsole.Should().BeTrue();
        host.Terminals.Should().BeEmpty("the terminal is created when its tab is first selected");

        viewModel.SelectedTab = BrowseTab.Console;
        FakeTerminal terminal = host.Terminals.Single();
        viewModel.ConsoleView.Should().BeSameAs(terminal);
        terminal.Calls.Should().Equal("start");

        // Back to the tab: the running shell is focused; an exited one is started again.
        viewModel.SelectedTab = BrowseTab.Diff;
        viewModel.SelectedTab = BrowseTab.Console;
        terminal.Calls.Should().Equal("start", "focus");
        terminal.IsShellRunning = false;
        viewModel.SelectedTab = BrowseTab.Diff;
        viewModel.SelectedTab = BrowseTab.Console;
        terminal.Calls.Should().Equal("start", "focus", "start");
        host.Terminals.Should().HaveCount(1);

        viewModel.Dispose();
        terminal.Calls[^1].Should().Be("dispose");
        window.Close();
    });

    [Test]
    public Task The_build_report_tab_shows_the_report_of_the_selected_revision() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();

        // The tabs are in the order of BrowseTab (the view model maps the selected index).
        window.Tabs.Items.Cast<TabItem>().Select(t => t.Name).Should().Equal("commitTab", "diffTab", "treeTab", "gpgTab", "consoleTab", "outputHistoryTab", "buildReportTab");
        Enum.GetNames<BrowseTab>().Should().Equal("Commit", "Diff", "FileTree", "Gpg", "Console", "OutputHistory", "BuildReport");
        viewModel.HasBuildReport.Should().BeFalse("the selected revision has no build status");
        window.BuildReportTab.IsVisible.Should().BeFalse();

        // The build server reports the build of the selected revision: the tab is shown, the report loaded once it is selected.
        GitRevision selected = viewModel.Grid.SelectedRow!.Revision;
        selected.BuildStatus = new BuildInfo { Status = BuildStatus.Success, Url = "https://ci.example.com/1" };
        Dispatcher.UIThread.RunJobs();
        viewModel.HasBuildReport.Should().BeTrue();
        viewModel.IsBuildReportInTab.Should().BeTrue();
        window.BuildReportTab.IsVisible.Should().BeTrue();
        FakeWebView webView = host.WebViews.Single();
        viewModel.BuildReportView.Should().BeSameAs(webView);
        webView.Calls.Should().BeEmpty("the report is loaded when its tab is shown");

        window.Tabs.SelectedIndex = (int)BrowseTab.BuildReport;
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.BuildReport);
        webView.Calls.Should().Equal("navigate https://ci.example.com/1");

        // As BuildReportWebBrowserOnNavigated: the favicon of the page in the header of the tab, kept for a page without one.
        using (Stream asset = global::Avalonia.Platform.AssetLoader.Open(new Uri("avares://GitUI.Avalonia/Assets/Settings.png")))
        using (MemoryStream png = new())
        {
            asset.CopyTo(png);
            webView.RaiseIconChanged(png.ToArray());
        }

        Dispatcher.UIThread.RunJobs();
        viewModel.BuildReportIcon.Should().NotBeNull();
        window.BuildReportTab.GetVisualDescendants().OfType<Image>().Single(i => i.Name == "buildReportIcon").IsVisible.Should().BeTrue();
        webView.RaiseIconChanged(null);
        viewModel.BuildReportIcon.Should().NotBeNull();

        // A build server that does not show its report in the tab: the link opens it in the browser.
        selected.BuildStatus = new BuildInfo { Status = BuildStatus.Failure, Url = "https://ci.example.com/2", ShowInBuildReportTab = false };
        Dispatcher.UIThread.RunJobs();
        viewModel.IsBuildReportInTab.Should().BeFalse();
        viewModel.BuildReportUrl.Should().Be("https://ci.example.com/2");
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-build-report-link");
        viewModel.OpenBuildReportCommand.Execute(null);
        host.OpenedUrls.Should().Equal("https://ci.example.com/2");

        // A revision without a build status: the tab is hidden, the first tab shown instead.
        viewModel.Grid.SelectedRow = viewModel.Grid.Rows.First(r => r.Revision != selected && !r.Revision.IsArtificial);
        Dispatcher.UIThread.RunJobs();
        viewModel.HasBuildReport.Should().BeFalse();
        window.Tabs.SelectedIndex.Should().Be(0);
        webView.Calls[^1].Should().Be("clear");

        // Disabled (ShowBuildResultPage): no tab even with a report.
        host.IsBuildReportEnabled = false;
        viewModel.Grid.SelectedRow = viewModel.Grid.Rows.First(r => r.Revision == selected);
        Dispatcher.UIThread.RunJobs();
        viewModel.HasBuildReport.Should().BeFalse();

        viewModel.Dispose();
        webView.Calls[^1].Should().Be("dispose");
        window.Close();
    });

    [Test]
    public Task Another_repository_keeps_the_selected_tab() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel _, FakeBrowseHost _) = Show();
        window.Tabs.SelectedIndex = 2;
        Dispatcher.UIThread.RunJobs();

        // As SetGitModule: a new view model for the other repository; its file tree is loaded, as its tab is shown.
        (BrowseWindow other, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        other.Close();
        window.ShowViewModel(viewModel);
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.FileTree);
        window.Tabs.SelectedIndex.Should().Be(2);
        host.TreesRequested.Should().NotBeEmpty();
        window.Close();
    });

    [Test]
    public Task The_plugins_menu_lists_the_plugins_once_they_are_loaded() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        List<string> Headers() => [.. window.MainMenu.Items.Cast<MenuItem>().Where(m => m.IsVisible).Select(m => (string)m.Header!)];
        MenuItem Menu(string header) => window.MainMenu.Items.Cast<MenuItem>().Single(m => (string?)m.Header == header);

        // As pluginsLoadingToolStripMenuItem while the plugins load.
        MenuItem loading = Menu("_Plugins").Items.OfType<MenuItem>().First();
        loading.Header.Should().Be("Loading...");
        loading.IsEnabled.Should().BeFalse();

        host.LoadPlugins([new("Statistics", null, true, "statistics"), new("Plugin Manager", null, false, "manager"), new("Delete obsolete branches", null, true, "delete")], "GitHub");
        Dispatcher.UIThread.RunJobs();

        Headers().Should().Equal("_Start", "_Repository", "_View", "_Commands", "GitHub", "_Plugins", "_Tools", "_Help");
        List<object?> plugins = [.. Menu("_Plugins").Items.Select(i => i is MenuItem m ? m.Header : "-")];
        plugins.Should().Equal("Delete obsolete branches", "Statistics", "-", "Plugin Manager", "Plugins _settings...");
        MenuItem statistics = Menu("_Plugins").Items.OfType<MenuItem>().Single(m => (string?)m.Header == "Statistics");
        statistics.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.PluginRuns.Should().Equal("statistics");

        MenuItem viewPullRequests = Menu("GitHub").Items.OfType<MenuItem>().Single(m => (string?)m.Header == "View _pull requests...");
        viewPullRequests.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.HostCommands.Should().Equal(RepositoryHostCommand.ViewPullRequests);
        viewModel.Menus.Should().Contain(m => m.Header == "_Plugins");
        window.Close();
    });

    [Test]
    public Task The_navigate_and_view_menus_are_the_ones_of_the_grid() => OnUiThreadAsync(() =>
    {
        int built = 0;
        (BrowseWindow window, BrowseViewModel _, FakeBrowseHost _) = Show(navigate: () =>
        {
            built++;
            return [new MenuModelItem("Go to _parent commit", () => { })];
        });

        List<MenuItem> menus = [.. window.MainMenu.Items.Cast<MenuItem>()];
        menus.Where(m => m.IsVisible).Select(m => m.Header).Should().StartWith(["_Start", "_Repository", "_Navigate", "_View", "_Commands"]);
        menus.Single(m => (string?)m.Header == "_View").Items.OfType<MenuItem>().Select(m => m.Header).Should().Equal(["Toolbars"], "only the Toolbars menu without the items of the grid");
        MenuItem navigate = menus.Single(m => (string?)m.Header == "_Navigate");
        navigate.Items.OfType<MenuItem>().Select(m => m.Header).Should().Equal("Go to _parent commit");
        built.Should().Be(1);

        // Built again when the main menu closes, as the grid's settings may have changed.
        window.MainMenu.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuBase.ClosedEvent));
        Dispatcher.UIThread.RunJobs();
        built.Should().Be(2);
        window.Close();
    });

    [Test]
    public Task The_toolbar_shows_the_status_the_working_directory_the_worktrees_and_the_shells() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();

        // As UpdateCommitButtonAndGetBrush: the number of changes with the image of the state.
        TextBlock commitText = window.FindControl<TextBlock>("commitButtonText")!;
        commitText.Text.Should().Be("Commit");
        host.RaiseStatus(new BrowseWorkingDirectoryStatus(3, "RepoStateDirty"));
        Dispatcher.UIThread.RunJobs();
        commitText.Text.Should().Be("Commit (3)");
        viewModel.CommitButtonIcon.Should().Be("RepoStateDirty");
        host.RaiseStatus(new BrowseWorkingDirectoryStatus(null, null));
        viewModel.CommitButtonText.Should().Be("Commit");

        // As WorkingDirectoryToolStripSplitButton.
        host.RecentRepositoriesMenu = [new("~/other", null) { Invoke = () => { } }];
        viewModel.WorkingDirectoryText.Should().Be("~/repo");
        viewModel.GetWorkingDirectoryItems().Select(i => i.Header).Should().Equal(
            "_Favorite repositories", "-", "~/other", "-", "_Open...", "_Close (go to Dashboard)", "-", "Co_nfigure this menu...");

        // As UpdateWorktreeToolStipVisibility: shown with more than one worktree.
        window.FindControl<SplitButton>("worktreesButton")!.IsVisible.Should().BeTrue();
        viewModel.WorktreeItems.Select(i => (i.Header, i.IsChecked)).Should().Equal(("repo", true), ("repo-feature", false));

        // As userShell_Click: the button runs the first shell.
        viewModel.Shells.Select(s => s.Name).Should().Equal("bash", "pwsh");
        window.FindControl<SplitButton>("userShellButton")!.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(SplitButton.ClickEvent));
        viewModel.ShellItems[1].Invoke!();
        host.ShellRuns.Should().Equal("bash", "pwsh");

        // As LoadUserMenu: a button for each script of the user menu bar.
        Button script = window.FindControl<StackPanel>("scriptsToolBar")!.Children.OfType<Button>().Single();
        script.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        host.ShellRuns[^1].Should().Be("script Deploy");
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-toolbar");
        window.Close();
    });

    [Test]
    public Task The_shortcut_shown_beside_a_menu_command_is_the_hotkey_that_runs_it() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();

        // The Git bash hotkey runs the default shell when there is one, the menu item the Git bash command.
        foreach (BrowseCommand command in Enum.GetValues<BrowseCommand>().Where(c => c != BrowseCommand.GitBash))
        {
            if (BrowseViewModel.GetHotkeyCommand(command) is { } hotkey)
            {
                viewModel.ExecuteHotkeyCommand((int)hotkey).Should().BeTrue(command.ToString());
                host.Runs[^1].Command.Should().Be(command);
            }
        }

        BrowseViewModel.GetHotkeyCommand(BrowseCommand.Commit).Should().Be(BrowseHotkeyCommand.Commit);
        BrowseViewModel.GetHotkeyCommand(BrowseCommand.About).Should().BeNull();
        window.Close();
    });

    [Test]
    public Task The_hotkeys_run_the_commands_show_the_tabs_and_run_the_scripts() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();

        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.Commit).Should().BeTrue();
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.QuickPush).Should().BeTrue();
        host.Runs.Select(r => r.Command).Should().Equal(BrowseCommand.Commit, BrowseCommand.QuickPush);

        // As FocusDiff and FocusNextTab / FocusPrevTab (around the shown tabs).
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusDiff);
        Dispatcher.UIThread.RunJobs();
        window.Tabs.SelectedIndex.Should().Be((int)BrowseTab.Diff);
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusPrevTab);
        viewModel.SelectedTab.Should().Be(BrowseTab.Commit);
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusPrevTab);
        viewModel.SelectedTab.Should().Be(BrowseTab.OutputHistory, "the last shown tab");

        BrowseFocusTarget? focused = null;
        viewModel.FocusRequested += (_, target) => focused = target;
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusFilter);
        focused.Should().Be(BrowseFocusTarget.Filter);

        // The hotkeys of the scripts.
        viewModel.ExecuteHotkeyCommand(9001).Should().BeTrue();
        viewModel.ExecuteHotkeyCommand(9002).Should().BeFalse();
        host.ShellRuns.Should().Equal("script 9001");

        // As the ScriptOptionsProvider of RevisionDiffControl: the selected files and the line of the file at the caret.
        viewModel.SelectedTab = BrowseTab.Diff;
        viewModel.Files.SetDiff(null, new GitRevision(ObjectId.Parse("b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2")), [new GitItemStatus("f") { IsChanged = true, IsTracked = true }]);
        viewModel.Files.Select(_ => true);
        viewModel.Viewer.Show(new FileViewContent(FileViewKind.Diff, FileViewerContextMenuTests.Patch));
        viewModel.Viewer.Editor.CaretOffset = FileViewerContextMenuTests.Patch.IndexOf("+c", StringComparison.Ordinal) + 1;
        viewModel.ExecuteHotkeyCommand(9001);
        host.ShellRuns[^1].Should().Be("script 9001 f at 2:1");
        window.Close();
    });

    [Test]
    public Task The_output_tab_shows_the_history_and_copies_or_clears_it() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        viewModel.HasOutputHistory.Should().BeTrue();
        viewModel.OutputHistoryText.Should().Be("git fetch");

        host.AddOutput("git pull");
        viewModel.OutputHistoryText.Should().Be("git fetch\ngit pull");
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusOutputHistory).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        window.Tabs.SelectedIndex.Should().Be((int)BrowseTab.OutputHistory);
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-output-history");

        viewModel.CopyOutputHistory(selectedText: "");
        viewModel.CopyOutputHistory(selectedText: "pull");
        host.CopiedOutput.Should().Equal("git fetch\ngit pull", "pull");
        viewModel.ClearOutputHistoryCommand.Execute(null);
        viewModel.OutputHistoryText.Should().BeEmpty();
        window.Close();
    });

    [Test]
    public Task The_split_view_layout_and_the_commit_info_position_are_applied_and_saved() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        Border commitInfo = window.FindControl<Border>("commitInfoBorder")!;
        TabItem commitTab = window.FindControl<TabItem>("commitTab")!;
        commitInfo.Parent.Should().BeSameAs(commitTab);
        viewModel.ShowTabs.Should().BeTrue();
        viewModel.CommitInfoPositionIcon.Should().Be("LayoutFooterTab");

        // As toggleSplitViewLayout: the tabs are hidden, and the setting is saved.
        viewModel.ToggleSplitViewLayoutCommand.Execute(null);
        viewModel.ShowTabs.Should().BeFalse();
        host.ShowSplitViewLayout.Should().BeFalse();
        window.Tabs.IsVisible.Should().BeFalse();
        viewModel.ToggleSplitViewLayoutCommand.Execute(null);
        window.Tabs.IsVisible.Should().BeTrue();

        // As SetCommitInfoPosition: beside the grid, the commit tab is hidden and another tab is selected.
        viewModel.SelectedTab = BrowseTab.Commit;
        viewModel.CommitInfoPositionItems[2].Invoke!();
        host.CommitInfoPosition.Should().Be(GitCommands.CommitInfoPosition.RightwardFromList);
        viewModel.CommitInfoPositionIcon.Should().Be("LayoutSidebarTopRight");
        viewModel.SelectedTab.Should().Be(BrowseTab.Diff);
        commitInfo.Parent.Should().BeSameAs(window.FindControl<ContentControl>("rightCommitInfoHost"));
        commitTab.IsVisible.Should().BeFalse();
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusCommitInfo).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-commit-info-right");

        viewModel.CommitInfoPositionItems[1].Invoke!();
        commitInfo.Parent.Should().BeSameAs(window.FindControl<ContentControl>("leftCommitInfoHost"));
        viewModel.CommitInfoPositionItems[0].Invoke!();
        commitInfo.Parent.Should().BeSameAs(commitTab);
        commitTab.IsVisible.Should().BeTrue();
        window.Close();
    });

    [Test]
    public Task The_pull_button_runs_the_default_action_chosen_in_its_menu() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        viewModel.PullButtonIcon.Should().Be("Pull");
        viewModel.PullButtonToolTip.Should().Be("Open pull dialog");

        // As FillNextPullActionAsDefaultToolStripMenuItems: the actions, the default one checked; saved when chosen.
        BrowseMenuItem setDefault = viewModel.PullItems[^1];
        setDefault.Header.Should().Be("Set _default Pull button action");
        setDefault.Children!.Select(i => (i.Header, i.IsChecked)).Should().Equal(
            ("Pull - _merge", false), ("Pull - _rebase", false), ("_Fetch", false), ("Fetch _all", false), ("F_etch and prune all", false), ("Open _pull dialog...", true));
        setDefault.Children![3].Invoke!();
        host.DefaultPullAction.Should().Be(GitPullAction.FetchAll);
        viewModel.PullButtonIcon.Should().Be("PullFetchAll");
        viewModel.PullButtonToolTip.Should().Be("Fetch all");
        viewModel.PullItems[^1].Children!.Single(i => i.IsChecked == true).Header.Should().Be("Fetch _all");
        ToolTip.GetTip(window.FindControl<SplitButton>("pullButton")!).Should().Be("Fetch all");
        window.Close();
    });

    [Test]
    public Task The_toolbars_menu_shows_or_hides_the_toolbars_and_their_items() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show(withFilters: true);
        StackPanel shortcuts = window.FindControl<StackPanel>("fetchPullShortcuts")!;
        shortcuts.Children.Select(c => c.Name).Should().Equal(
            "pull_shortcut_fetchToolStripMenuItem", "pull_shortcut_fetchAllToolStripMenuItem", "pull_shortcut_fetchPruneAllToolStripMenuItem",
            "pull_shortcut_mergeToolStripMenuItem", "pull_shortcut_rebaseToolStripMenuItem1", "pull_shortcut_pullToolStripMenuItem1");
        shortcuts.Children.Should().AllSatisfy(c => c.IsVisible.Should().BeFalse("the shortcuts are hidden by default"));

        IReadOnlyList<BrowseMenuItem> toolbars = viewModel.GetToolbarsMenuItems();
        toolbars.Select(t => t.Header).Should().Equal("Standard", "Filters", "Scripts");
        IReadOnlyList<BrowseMenuItem> standard = toolbars[0].Children!;
        standard[0].Should().Match<BrowseMenuItem>(i => i.Header == "Standard" && i.IsChecked == true);
        standard.Skip(2).Select(i => i.Header).Should().StartWith(["Refresh", "Toggle split view layout", "Commit info position"], "without the left panel");
        standard.Should().OnlyContain(i => i.IsSeparator || i.IsChecked != null);

        // An item of the menu shows the shortcut, saved as not the default.
        standard.Single(i => i.Header == "Fetch").Invoke!();
        Dispatcher.UIThread.RunJobs();
        shortcuts.Children[0].IsVisible.Should().BeTrue();
        host.ToolbarVisibility.Should().ContainKey("pull_shortcut_fetchToolStripMenuItem").WhoseValue.Should().Be((true, false));
        ((Button)shortcuts.Children[0]).Command.Should().BeSameAs(viewModel.RunCommand);
        ((Button)shortcuts.Children[0]).CommandParameter.Should().Be(BrowseCommand.Fetch);

        // Another hides a button; the separator of a group without shown items is hidden (AdaptSeparatorsVisibility).
        Button refresh = window.FindControl<Button>("refreshButton")!;
        Separator afterRefresh = window.FindControl<WrapPanel>("toolbar")!.Children.OfType<Separator>().First();
        afterRefresh.IsVisible.Should().BeTrue();
        viewModel.SetToolbarItemShown("RefreshButton", false);
        Dispatcher.UIThread.RunJobs();
        refresh.IsVisible.Should().BeFalse();
        afterRefresh.IsVisible.Should().BeFalse("no item is shown before it");
        host.ToolbarVisibility["RefreshButton"].Should().Be((false, true));
        viewModel.GetToolbarsMenuItems()[0].Children!.Single(i => i.Header == "Refresh").IsChecked.Should().BeFalse();

        // The whole toolbar, and the Filters and Scripts toolbars (not saved).
        standard[0].Invoke!();
        Dispatcher.UIThread.RunJobs();
        window.FindControl<Button>("pushButton")!.IsVisible.Should().BeFalse();
        viewModel.GetToolbarsMenuItems()[0].Children![0].IsChecked.Should().BeFalse();
        toolbars[1].Children![0].Invoke!();
        toolbars[2].Invoke!();
        window.FindControl<Panel>("filterToolBarHost")!.IsVisible.Should().BeFalse();
        window.FindControl<StackPanel>("scriptsToolBar")!.IsVisible.Should().BeFalse();
        viewModel.GetToolbarsMenuItems()[1].Children![0].IsChecked.Should().BeFalse();
        viewModel.GetToolbarsMenuItems()[2].IsChecked.Should().BeFalse();
        toolbars[1].Children![0].Invoke!();
        Dispatcher.UIThread.RunJobs();

        // The items of the Filters toolbar; a group is saved by its group name (the Tag of its WinForms items).
        IReadOnlyList<BrowseMenuItem> filterItems = viewModel.GetToolbarsMenuItems()[1].Children!;
        filterItems.Skip(2).Select(i => (i.Header, i.IsChecked)).Should().Equal(
            ("Advanced filter", true), ("Show all reflog references", true), ("Show all branches", true), ("Branch filter", true), ("Text filter", true), ("Show only first parent", true));
        ComboBox branchFilterBox = window.FindControl<Control>("filterToolBar")!.GetVisualDescendants().OfType<ComboBox>().First(c => c.Name == "branchFilterBox");
        branchFilterBox.IsVisible.Should().BeTrue();
        filterItems.Single(i => i.Header == "Branch filter").Invoke!();
        Dispatcher.UIThread.RunJobs();
        branchFilterBox.IsVisible.Should().BeFalse();
        host.ToolbarVisibility["ToolBar_group:Branch filter"].Should().Be((false, true));
        viewModel.Filters!.ShowSeparator.Should().BeTrue("the advanced filter, reflog and branches buttons are before it");
        foreach (string key in (string[])["tsbtnAdvancedFilter", "tsbShowReflog", "tssbtnShowBranches"])
        {
            viewModel.SetFilterToolbarItemShown(key, false);
        }

        viewModel.Filters.ShowSeparator.Should().BeFalse("no item is shown before it");
        window.Close();

        // Loaded from the settings.
        (BrowseWindow window2, BrowseViewModel viewModel2, FakeBrowseHost _) = Show(
            configure: h => (h.ToolbarVisibility["toolStripButtonPush"], h.ToolbarVisibility["ToolBar_group:Text filter"]) = ((false, true), (false, true)),
            withFilters: true);
        viewModel2.ToolbarItems["toolStripButtonPush"].Should().BeFalse();
        window2.FindControl<Button>("pushButton")!.IsVisible.Should().BeFalse();
        viewModel2.Filters!.ItemVisibility["TextFilter"].Should().BeFalse();
        window2.Close();
    });

    [Test]
    public Task The_working_directory_menu_searches_the_recent_repositories() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel _, FakeBrowseHost _) = Show(configure: h => h.RecentRepositoriesMenu =
        [
            new("~/other", null) { Invoke = () => { } },
            new("C:/work/my__lib", null) { Invoke = () => { } },
        ]);
        MenuFlyout flyout = (MenuFlyout)window.FindControl<DropDownButton>("workingDirButton")!.Flyout!;
        TextBox search = (TextBox)((MenuItem)flyout.Items[0]!).Header!;
        search.PlaceholderText.Should().Be("Search repositories...");
        flyout.Items[1].Should().BeOfType<Separator>();

        List<MenuItem> items = [.. flyout.Items.OfType<MenuItem>().Skip(1)];
        items.Select(i => i.Header).Should().Equal("_Favorite repositories", "~/other", "C:/work/my__lib", "_Open...", "_Close (go to Dashboard)", "Co_nfigure this menu...");

        // As the TextChanged of _txtFilter: only the recent repositories are filtered, ignoring the case.
        search.Text = "MY_L";
        items.Where(i => i.IsVisible).Select(i => i.Header).Should().Equal("_Favorite repositories", "C:/work/my__lib", "_Open...", "_Close (go to Dashboard)", "Co_nfigure this menu...");
        search.Text = " ";
        items.Should().OnlyContain(i => i.IsVisible);
        window.Close();
    });

    [Test]
    public Task The_diff_and_file_tree_hotkeys_run_the_commands_of_the_selected_files() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show(
            configure: h => h.TreeFiles = ["README.md", "src/file.cs"],
            revisionDiffHotkeys:
            [
                new HotkeyBinding((int)RevisionDiffHotkeyCommand.OpenWithDifftool, 0x44 /* D */ | HotkeyBinding.Control),
                new HotkeyBinding((int)RevisionDiffHotkeyCommand.Blame, 0x42 /* B */ | HotkeyBinding.Control),
            ]);
        FileStatusListMenuTests.FakeMenuHost menu = new() { State = new() { CanOpenWithDifftool = true, CanDiffFirstToSelected = true, CanShowFileHistory = true, ShowShowInFileTree = true } };
        viewModel.Files.MenuHost = menu;
        window.Tabs.SelectedIndex = (int)BrowseTab.Diff;
        Dispatcher.UIThread.RunJobs();
        viewModel.Files.SelectedEntry!.Item.Name.Should().Be("src/file.cs");

        // As RevisionDiffControl.ProcessCmdKey: the keys in the diff tab run the item of the context menu.
        window.FindControl<Control>("diffFiles")!.GetVisualDescendants().OfType<ListBoxItem>().Last().Focus().Should().BeTrue();
        window.KeyPressQwerty(PhysicalKey.D, TestKeys.Command);
        menu.Log.Should().Contain("difftool FirstToSelected: src/file.cs");

        // Not for a disabled item (blame), nor outside the tab.
        window.KeyPressQwerty(PhysicalKey.B, TestKeys.Command);
        menu.Log.Should().NotContain(l => l.StartsWith("history"));
        int logged = menu.Log.Count;
        window.FindControl<Button>("refreshButton")!.Focus().Should().BeTrue();
        window.KeyPressQwerty(PhysicalKey.D, TestKeys.Command);
        menu.Log.Should().HaveCount(logged);

        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.ShowHistory, fileTree: false).Should().BeTrue();
        menu.Log.Should().Contain("history: src/file.cs");
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.OpenAsTempFile, fileTree: false).Should().BeFalse("the item is hidden");

        // As tsmiShowInFileTree: the file tree tab, with the file selected once loaded.
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.ShowFileTree, fileTree: false).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.FileTree);
        viewModel.FileTree!.SelectedEntry!.Item.Name.Should().Be("src/file.cs");

        // As GoToFirstParent of the grid.
        GitRevision selected = viewModel.Grid.SelectedRow!.Revision;
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.GoToFirstParent, fileTree: true).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        viewModel.Grid.SelectedRow!.Revision.ObjectId.Should().Be(selected.FirstParentId);
        window.Close();
    });

    [Test]
    public Task Blame_is_shown_instead_of_the_viewer_in_the_file_tree_and_the_diff_tab() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show(configure: h =>
        {
            h.TreeFiles = ["README.md", "src/file.cs"];
            h.DiffFiles = ["a.txt", "src/file.cs"];
        });
        FileStatusListMenuTests.FakeMenuHost menu = new() { State = new() { CanBlame = true, CanShowFileHistory = true, ShowShowInFileTree = true } };
        viewModel.Files.MenuHost = menu;
        viewModel.FileTree!.MenuHost = menu;
        window.Tabs.SelectedIndex = (int)BrowseTab.Diff;
        Dispatcher.UIThread.RunJobs();
        viewModel.Files.Select(entry => entry.Item.Name == "src/file.cs");
        Dispatcher.UIThread.RunJobs();

        // As BlameFile without UseDiffViewerForBlame: the blame in the file tree tab, not the dialog.
        viewModel.Files.ShowFileHistoryCommand.Execute(true);
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.FileTree);
        viewModel.FileTree.SelectedEntry!.Item.Name.Should().Be("src/file.cs");
        viewModel.FileTree.IsBlameShown.Should().BeTrue();
        viewModel.Files.IsBlameShown.Should().BeFalse();
        viewModel.IsTreeBlameVisible.Should().BeTrue();
        window.FindControl<Control>("treeBlame")!.IsEffectivelyVisible.Should().BeTrue();
        window.FindControl<Control>("treeViewer")!.IsEffectivelyVisible.Should().BeFalse();
        host.Blame.Blamed.Select(b => b.FileName).Should().Equal("src/file.cs");
        menu.Log.Should().NotContain(l => l.StartsWith("history"));

        // Kept for the next files of the tree, toggled back by the item.
        viewModel.FileTree.Select(entry => entry.Item.Name == "README.md");
        Dispatcher.UIThread.RunJobs();
        host.Blame.Blamed.Select(b => b.FileName).Should().Equal("src/file.cs", "README.md");
        viewModel.FileTree.ShowFileHistoryCommand.Execute(true);
        viewModel.IsTreeBlameVisible.Should().BeFalse();
        viewModel.FileTree.IsBlameShown.Should().BeFalse();

        // With UseDiffViewerForBlame: in the diff tab, until another file is selected.
        host.UseDiffViewerForBlame = true;
        window.Tabs.SelectedIndex = (int)BrowseTab.Diff;
        Dispatcher.UIThread.RunJobs();
        viewModel.Files.ShowFileHistoryCommand.Execute(true);
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.Diff);
        viewModel.IsDiffBlameVisible.Should().BeTrue();
        window.FindControl<Control>("diffBlame")!.IsEffectivelyVisible.Should().BeTrue();
        viewModel.Files.Select(entry => entry.Item.Name == "a.txt");
        Dispatcher.UIThread.RunJobs();
        viewModel.IsDiffBlameVisible.Should().BeFalse();
        viewModel.Files.IsBlameShown.Should().BeFalse();
        window.Close();
    });

    [Test]
    public Task The_diff_tab_menu_changes_the_files_of_the_working_directory_and_refreshes_its_diff() => OnUiThreadAsync(() =>
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        GitRevision index = new(ObjectId.IndexId) { Subject = "Commit index", ParentIds = [history[0].ObjectId] };
        GitRevision workTree = new(ObjectId.WorkTreeId) { Subject = "Working directory", ParentIds = [ObjectId.IndexId] };
        FilterToolBarViewTests.FakeFilterHost filterHost = new();
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show(
            configure: h => h.DiffFiles = ["a.txt", "b.txt", "c.txt", "src/d.cs"],
            revisionDiffHotkeys: [new HotkeyBinding((int)RevisionDiffHotkeyCommand.StageSelectedFile, 0x53 /* S */ | HotkeyBinding.Control)],
            history: [workTree, index, .. history],
            filterHost: filterHost);
        FileStatusListMenuTests.FakeMenuHost menu = new()
        {
            State = new()
            {
                ShowStage = true,
                CanFilterFileInGrid = true,
                DeleteFileText = "Delete file",
                ShowMove = true,
                ShowFindFile = true,
                ShowOpenInVisualStudio = true,
                ShowIgnore = true,
                ShowShowInFileTree = true,
            },
        };
        viewModel.Files.MenuHost = menu;
        viewModel.Grid.SelectRevision(ObjectId.WorkTreeId).Should().BeTrue();
        window.Tabs.SelectedIndex = (int)BrowseTab.Diff;
        Dispatcher.UIThread.RunJobs();
        viewModel.Files.Select(e => e.Item.Name == "b.txt");
        Dispatcher.UIThread.RunJobs();
        int requests = host.DiffsRequested.Count;

        // As StageFile_Click and RequestRefresh: the status and the working directory are refreshed, the next file selected.
        host.DiffFiles = ["a.txt", "c.txt", "src/d.cs"];
        window.FindControl<Control>("diffFiles")!.GetVisualDescendants().OfType<ListBoxItem>().Last().Focus().Should().BeTrue();
        window.KeyPressQwerty(PhysicalKey.S, TestKeys.Command);
        Dispatcher.UIThread.RunJobs();
        menu.Log.Should().Contain("stage: b.txt");
        host.StatusRefreshes.Should().Be(1);
        host.DiffsRequested.Should().HaveCount(requests + 1);
        host.DiffsRequested[^1].Should().Equal("Working directory");
        viewModel.Files.SelectedEntry!.Item.Name.Should().Be("c.txt");

        // The hotkeys of the other items added to the menu.
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.DeleteSelectedFiles, fileTree: false).Should().BeTrue();
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.RenameMove, fileTree: false).Should().BeTrue();
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.FindFile, fileTree: false).Should().BeTrue();
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.OpenInVisualStudio, fileTree: false).Should().BeTrue();
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.AddFileToGitIgnore, fileTree: false).Should().BeTrue();
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.FilterFileInGrid, fileTree: false).Should().BeTrue();
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.UnStageSelectedFile, fileTree: false).Should().BeFalse("the item is hidden");
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.FindInCommitFilesUsingGitGrep_DiffTab, fileTree: false).Should().BeFalse("git grep is not ported");
        Dispatcher.UIThread.RunJobs();
        menu.Log.Where(l => !l.StartsWith("state")).Should().Equal(
            "stage: b.txt",
            "delete: c.txt",
            "move: c.txt folder: ",
            "find: 3",
            "visual studio: c.txt",
            "ignore: c.txt folder: ");
        filterHost.Calls.Should().Contain("path \"c.txt\"");

        // As SelectFirstGroupChanges: all the files without diff groups.
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.SelectFirstGroupChanges, fileTree: false).Should().BeTrue();
        viewModel.Files.SelectedEntries.Should().HaveCount(3);

        // As OpenInFileTreeTab: a folder is selected in the file tree.
        host.TreeFiles = ["src/d.cs", "src/e.cs"];
        viewModel.Files.SelectedNodes.Clear();
        viewModel.Files.SelectedNodes.Add(viewModel.Files.Nodes.First(n => n.Entry is null));
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.ShowFileTree, fileTree: false).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.FileTree);
        viewModel.FileTree!.SelectedFolder!.Value.Should().Be("src");

        // In a commit, the diff is not loaded again.
        viewModel.Grid.SelectRevision(history[0].ObjectId).Should().BeTrue();
        window.Tabs.SelectedIndex = (int)BrowseTab.Diff;
        Dispatcher.UIThread.RunJobs();
        requests = host.DiffsRequested.Count;
        int statusRefreshes = host.StatusRefreshes;
        viewModel.Files.StageFilesCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        host.StatusRefreshes.Should().Be(statusRefreshes + 1);
        host.DiffsRequested.Should().HaveCount(requests);

        // Without the items (hidden for the selection), the hotkeys do nothing.
        menu.State = new();
        int logged = menu.Log.Count(l => !l.StartsWith("state"));
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.DeleteSelectedFiles, fileTree: false).Should().BeFalse();
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.StageSelectedFile, fileTree: false).Should().BeFalse();
        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.FilterFileInGrid, fileTree: false).Should().BeFalse();
        menu.Log.Count(l => !l.StartsWith("state")).Should().Be(logged);
        window.Close();
    });

    [Test]
    public Task Escape_clears_an_applied_filter_and_does_not_close_the_window() => OnUiThreadAsync(() =>
    {
        FilterToolBarViewTests.FakeFilterHost filterHost = new();
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost _) = Show(filterHost: filterHost);
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        window.IsVisible.Should().BeTrue("FormBrowse.CancelButtonClick does not close the window");
        filterHost.Calls.Should().BeEmpty("no filter to clear");

        // As CancelButtonClick: with a filter, the text filter is cleared.
        filterHost.Raise(new RevisionGridFilterState(AuthorFilter: "bob", HasFilter: true, FilterSummary: "Author: bob"));
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        window.IsVisible.Should().BeTrue();
        filterHost.Calls.Should().ContainSingle().Which.Should().StartWith("text ").And.NotContain("bob");
        viewModel.Filters!.RevisionFilter.Should().BeEmpty();
        window.Close();
    });

    [Test]
    public Task The_splitters_are_restored_when_the_window_opens_and_saved_when_it_closes() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show(configure: h =>
        {
            // As SplitterManager: in pixels at the DPI they were saved at.
            h.Splitters["MainSplitContainer"] = new(300, 2000, 192, Panel1Collapsed: true);
            h.Splitters["RightSplitContainer"] = new(500, 700, 96);
            h.Splitters["revisionDiff.DiffSplitContainer"] = new(250, 800, 96);
        });
        LeftPanelViewModel leftPanel = LeftPanelViewModelTests.Create().Panel;
        viewModel.LeftPanel = leftPanel;
        Dispatcher.UIThread.RunJobs();
        ColumnDefinition leftColumn = window.FindControl<Grid>("mainSplit")!.ColumnDefinitions[0];

        leftPanel.IsVisible.Should().BeFalse("hidden when the window was closed");
        leftColumn.Width.Value.Should().Be(0);
        window.FindControl<Grid>("contentGrid")!.RowDefinitions[2].Height.Value.Should().Be(200, "the tabs keep their height");
        window.FindControl<Grid>("diffPanel")!.ColumnDefinitions[0].Width.Value.Should().Be(250);
        leftPanel.IsVisible = true;
        Dispatcher.UIThread.RunJobs();
        leftColumn.Width.Value.Should().Be(150);

        window.Close();
        host.Splitters["MainSplitContainer"].Should().Match<SplitterPosition>(p => p.Distance == 150 && p.Dpi == 96 && !p.Panel1Collapsed);
        SplitterPosition tabs = host.Splitters["RightSplitContainer"];
        (tabs.Size - tabs.Distance).Should().Be(200);
        host.Splitters["fileTree.DiffSplitContainer"].Distance.Should().Be(300);
    });

    [Test]
    public Task The_output_history_panel_is_toggled_by_its_hotkey_and_saved() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show(configure: h => h.ShowOutputHistoryAsTab = false);
        viewModel.IsOutputHistoryTab.Should().BeFalse();
        window.FindControl<TabItem>("outputHistoryTab")!.IsVisible.Should().BeFalse("the history is a panel (ShowOutputHistoryAsTab off)");
        TextBox panel = window.FindControl<TextBox>("outputHistoryPanel")!;
        ColumnDefinition leftColumn = window.FindControl<Grid>("mainSplit")!.ColumnDefinitions[0];
        panel.IsVisible.Should().BeFalse();
        leftColumn.Width.Value.Should().Be(0, "without the left panel nor the output panel");

        // As FocusAndToggleIfPanel: shown and focused, alone in the left column without the left panel.
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusOutputHistory).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        panel.IsVisible.Should().BeTrue();
        panel.Text.Should().Be("git fetch");
        panel.IsFocused.Should().BeTrue();
        leftColumn.Width.Value.Should().BeGreaterThan(0);
        host.IsOutputHistoryPanelVisible.Should().BeTrue();
        viewModel.SelectedTab.Should().NotBe(BrowseTab.OutputHistory);
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-output-history-panel");

        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusOutputHistory).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        panel.IsVisible.Should().BeFalse();
        host.IsOutputHistoryPanelVisible.Should().BeFalse();
        leftColumn.Width.Value.Should().Be(0);
        window.Close();

        // Shown at once when it was left shown.
        (BrowseWindow window2, BrowseViewModel _, FakeBrowseHost _) = Show(configure: h => (h.ShowOutputHistoryAsTab, h.IsOutputHistoryPanelVisible) = (false, true));
        window2.FindControl<TextBox>("outputHistoryPanel")!.IsVisible.Should().BeTrue();
        window2.Close();
    });

    [Test]
    public Task The_submodules_button_lists_the_submodules_or_goes_to_the_superproject() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        viewModel.CanShowSubmodules.Should().BeTrue();
        viewModel.SubmoduleItems.Select(i => (i.Header, i.IsEnabled)).Should().Equal(("Loading...", false));

        // As PopulateToolbar, when the provider reported the structure.
        host.RaiseSubmodules([new("lib", null, "SubmoduleRevisionUp") { Invoke = () => host.ShellRuns.Add("open lib") }]);
        viewModel.SubmoduleItems.Single().Header.Should().Be("lib");

        // Without a superproject the click opens the drop down; with one it goes up.
        int opened = 0;
        viewModel.SubmodulesMenuRequested += (_, _) => opened++;
        viewModel.GoUpOrShowSubmodules();
        opened.Should().Be(1);
        viewModel.SubmodulesIcon.Should().Be("SubmodulesManage");
        host.HasSuperproject = true;
        viewModel.GoUpOrShowSubmodules();
        host.ShellRuns.Should().Equal("superproject");
        viewModel.SubmodulesIcon.Should().Be("NavigateUp");
        window.Close();
    });

    [Test]
    public Task The_window_opens_with_the_commit_info_beside_the_grid() => OnUiThreadAsync(() =>
    {
        // The layout selects another tab than the commit info in the constructor, before which the build report is ready.
        (BrowseWindow window, BrowseViewModel viewModel, _) = Show(configure: host => host.CommitInfoPosition = GitCommands.CommitInfoPosition.RightwardFromList);

        viewModel.SelectedTab.Should().NotBe(BrowseTab.Commit);
        viewModel.HasBuildReport.Should().BeFalse();
        window.Close();
    });

    private static (BrowseWindow Window, BrowseViewModel ViewModel, FakeBrowseHost Host) Show(Func<IReadOnlyList<MenuModelItem>>? navigate = null, Action<FakeBrowseHost>? configure = null, IReadOnlyList<HotkeyBinding>? revisionDiffHotkeys = null, bool withFilters = false, List<GitRevision>? history = null, FilterToolBarViewTests.FakeFilterHost? filterHost = null)
    {
        FakeBrowseHost host = new();
        configure?.Invoke(host);
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost(history ?? RevisionGridViewTests.CreateHistory()), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false))
        {
            MultiSelect = true,
        };
        BrowseViewModel viewModel = new(
            new BrowseStrings(),
            host,
            grid,
            new CommitInfoViewTests.FakeHost(),
            host.ViewerHost,
            new FileStatusListStrings(),
            new FileStatusTreeOptions())
        {
            NavigateMenuProvider = navigate,
            RevisionDiffHotkeys = revisionDiffHotkeys ?? [],
            Filters = withFilters || filterHost is not null ? new FilterToolBarViewModel(new FilterToolBarStrings(), filterHost ?? new FilterToolBarViewTests.FakeFilterHost()) : null,
        };
        BrowseWindow window = new() { Width = 1100, Height = 760, DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, viewModel, host);
    }

    internal sealed class FakeTerminal : IBrowseTerminal, IEmbeddedNativeView
    {
        public List<string> Calls { get; } = [];

        public IEmbeddedView View => this;

        public bool IsShellRunning { get; set; }

        public void StartShell()
        {
            Calls.Add("start");
            IsShellRunning = true;
        }

        public void Focus() => Calls.Add("focus");

        public void Dispose() => Calls.Add("dispose");

        public nint Attach(nint parentWindow) => 0;

        public void Detach()
        {
        }
    }

    internal sealed class FakeWebView : IBrowseWebView, IEmbeddedNativeView
    {
        public List<string> Calls { get; } = [];

        public IEmbeddedNativeView View => this;

        public void Navigate(string url) => Calls.Add($"navigate {url}");

        public void Clear() => Calls.Add("clear");

        public event EventHandler<byte[]?>? IconChanged;

        public void RaiseIconChanged(byte[]? icon) => IconChanged?.Invoke(this, icon);

        public void Dispose() => Calls.Add("dispose");

        public nint Attach(nint parentWindow) => 0;

        public void Detach()
        {
        }
    }

    internal sealed class FakeBrowseHost : IBrowseHost, IBrowseFileTreeHost, IBrowseGpgHost, IBrowseConsoleHost, IBrowsePluginsHost, IBrowseToolbarHost, IBrowseStatusHost, IBrowseScriptsHost, IBrowseOutputHistoryHost, IBrowseBuildReportHost, IBrowseLayoutHost, IBrowseToolbarItemsHost, IBrowseBlameHost
    {
        /// <summary>The commits of the links of the commit info (<see cref="IBrowseHost.ResolveCommit"/>).</summary>
        public Dictionary<string, ObjectId> ResolvedCommits { get; } = [];

        public ObjectId? ResolveCommit(string commitOrRef, bool isRef) => ResolvedCommits.TryGetValue(commitOrRef, out ObjectId objectId) ? objectId : null;

        public GitPullAction DefaultPullAction { get; set; }

        /// <summary>The host of the blames of the diff and file tree tabs.</summary>
        public BlameViewModelTests.FakeHost Blame { get; } = new();

        public bool UseDiffViewerForBlame { get; set; }

        public BlameViewModel CreateBlame() => BlameViewModelTests.Create(Blame);

        public GitRevision GetActualRevision(GitRevision revision) => revision;

        public ObjectId? GetCurrentCheckout() => null;

        GitRevision? IBrowseBlameHost.GetRevision(ObjectId objectId) => null;

        /// <summary>The saved visibility of the toolbar items, with their default.</summary>
        public Dictionary<string, (bool Visible, bool Default)> ToolbarVisibility { get; } = [];

        public bool GetToolbarItemVisibility(string key, bool defaultValue)
            => ToolbarVisibility.TryGetValue(key, out (bool Visible, bool Default) saved) ? saved.Visible : defaultValue;

        public void SetToolbarItemVisibility(string key, bool visible, bool defaultValue) => ToolbarVisibility[key] = (visible, defaultValue);

        public bool ShowSplitViewLayout { get; set; } = true;

        public GitCommands.CommitInfoPosition CommitInfoPosition { get; set; }

        /// <summary>The saved splitters, by name.</summary>
        public Dictionary<string, SplitterPosition> Splitters { get; } = [];

        public SplitterPosition? GetSplitter(string name) => Splitters.GetValueOrDefault(name);

        public void SaveSplitter(string name, SplitterPosition position) => Splitters[name] = position;

        public event EventHandler? OutputHistoryChanged;

        public bool IsOutputHistoryEnabled => true;

        public bool ShowOutputHistoryAsTab { get; set; } = true;

        public bool IsOutputHistoryPanelVisible { get; set; }

        public string OutputHistory { get; private set; } = "git fetch";

        public List<string> CopiedOutput { get; } = [];

        public void AddOutput(string line)
        {
            OutputHistory += "\n" + line;
            OutputHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearOutputHistory()
        {
            OutputHistory = "";
            OutputHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        public void CopyOutputHistory(string text) => CopiedOutput.Add(text);

        public bool RunScriptOfHotkey(int commandCode, ScriptSelection selection)
        {
            if (commandCode != 9001)
            {
                return false;
            }

            ShellRuns.Add($"script {commandCode}{(selection.SelectedFiles.Count > 0 ? $" {string.Join(",", selection.SelectedFiles)} at {selection.LineNumber}:{selection.ColumnNumber}" : "")}");
            return true;
        }

        public event EventHandler<BrowseWorkingDirectoryStatus>? WorkingDirectoryStatusChanged;

        public int StatusRefreshes { get; private set; }

        public void RequestStatusRefresh() => StatusRefreshes++;

        public List<string> ShellRuns { get; } = [];

        public string WorkingDirectoryCaption => "~/repo";

        public void RaiseStatus(BrowseWorkingDirectoryStatus status) => WorkingDirectoryStatusChanged?.Invoke(this, status);

        public Task<BrowseWorktreeMenu> GetWorktreeMenuAsync()
            => Task.FromResult(new BrowseWorktreeMenu(2, [new("repo", null) { IsChecked = true, Invoke = () => { } }, new("repo-feature", null) { IsChecked = false, Invoke = () => { } }]));

        public IReadOnlyList<BrowseShell> GetShells() => [new("bash", null, "bash"), new("pwsh", null, "pwsh")];

        public void RunShell(BrowseShell shell) => ShellRuns.Add(shell.Name);

        public event EventHandler<IReadOnlyList<BrowseMenuItem>?>? SubmoduleMenuChanged;

        public bool HasSuperproject { get; set; }

        public bool CanShowSubmodules => true;

        public void GoToSuperproject() => ShellRuns.Add("superproject");

        public void RaiseSubmodules(IReadOnlyList<BrowseMenuItem>? items) => SubmoduleMenuChanged?.Invoke(this, items);

        public IReadOnlyList<BrowseMenuItem> GetToolbarScripts() => [new("Deploy", null) { Invoke = () => ShellRuns.Add("script Deploy") }];

        public bool IsBuildReportEnabled { get; set; } = true;

        public List<FakeWebView> WebViews { get; } = [];

        public List<string> OpenedUrls { get; } = [];

        public IBrowseWebView? CreateWebView()
        {
            FakeWebView webView = new();
            WebViews.Add(webView);
            return webView;
        }

        public void OpenUrl(string url) => OpenedUrls.Add(url);

        public event EventHandler? PluginsChanged;

        public IReadOnlyList<BrowsePlugin>? Plugins { get; private set; }

        public string? RepositoryHostName { get; private set; }

        public List<object> PluginRuns { get; } = [];

        public List<RepositoryHostCommand> HostCommands { get; } = [];

        public void LoadPlugins(IReadOnlyList<BrowsePlugin> plugins, string? hostName)
        {
            Plugins = plugins;
            RepositoryHostName = hostName;
            PluginsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RunPlugin(BrowsePlugin plugin) => PluginRuns.Add(plugin.Plugin);

        public void OpenPluginSettings() => PluginRuns.Add("settings");

        public void RunRepositoryHostCommand(RepositoryHostCommand command) => HostCommands.Add(command);

        public List<string> GpgRequested { get; } = [];

        public Task<GpgInfo?> GpgResult { get; set; } = Task.FromResult<GpgInfo?>(null);

        public bool ShowGpgInformation => true;

        public Task<GpgInfo?> LoadGpgInfoAsync(GitRevision revision)
        {
            GpgRequested.Add(revision.Subject);
#pragma warning disable VSTHRD003 // The test completes the task.
            return GpgResult;
#pragma warning restore VSTHRD003
        }

        public List<FakeTerminal> Terminals { get; } = [];

        public bool IsConsoleAvailable => true;

        public IBrowseTerminal? CreateTerminal()
        {
            FakeTerminal terminal = new();
            Terminals.Add(terminal);
            return terminal;
        }

        public DiffViewModelTests.FakeViewerHost ViewerHost { get; } = new();

        public List<string> TreesRequested { get; } = [];

        public event EventHandler? RepositoryChanged;

        public string Branch { get; set; } = "main";

        public List<(BrowseCommand Command, BrowseSelection Selection)> Runs { get; } = [];

        public List<IReadOnlyList<string>> DiffsRequested { get; } = [];

        public IReadOnlyList<BrowseMenuItem> RecentRepositoriesMenu { get; set; } = [];

        public IReadOnlyList<BrowseMenuItem> FavouriteRepositoriesMenu { get; set; } = [];

        public string GetTitle() => $"repo ({Branch}) - Gitanas";

        public string GetCurrentBranch() => Branch;

        public void Run(BrowseCommand command, BrowseSelection selection) => Runs.Add((command, selection));

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
        {
            DiffsRequested.Add([.. revisions.Select(r => r.Subject)]);
            GitRevision second = revisions[0];
            GitRevision first = new(second.FirstParentId);
            StagedStatus staged = second.ObjectId == ObjectId.WorkTreeId ? StagedStatus.WorkTree : second.ObjectId == ObjectId.IndexId ? StagedStatus.Index : StagedStatus.None;
            GitItemStatus[] files = [.. DiffFiles.Select(name => new GitItemStatus(name) { IsTracked = true, IsChanged = true, Staged = staged })];
            return Task.FromResult<IReadOnlyList<FileStatusGroup>>([new FileStatusGroup(first, second, "Parent", files)]);
        }

        public IReadOnlyList<string> TreeFiles { get; set; } = ["README.md", "src/a.cs", "src/b.cs"];

        /// <summary>The files of the diffs of the selected revisions.</summary>
        public IReadOnlyList<string> DiffFiles { get; set; } = ["src/file.cs"];

        public Task<FileStatusGroup> GetTreeFilesAsync(GitRevision revision, CancellationToken cancellationToken)
        {
            TreesRequested.Add(revision.Subject);
            GitItemStatus[] files = [.. TreeFiles.Select(name => new GitItemStatus(name) { IsTracked = true })];
            return Task.FromResult(new FileStatusGroup(null, revision, $"grep:  {revision.ObjectId.ToShortString()}", files, IconName: FileStatusIcons.GitGrepIconName));
        }

        public IReadOnlyList<BrowseMenuItem> GetRepositoriesMenu(bool favourites) => favourites ? FavouriteRepositoriesMenu : RecentRepositoriesMenu;

        public void RaiseRepositoryChanged() => RepositoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
