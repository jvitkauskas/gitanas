using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.LeftPanel;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.Shells;
using GitUI.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the main window (docs/avalonia-port/PLAN.md, phase 7). The port is unfinished, so it is used only when named
///  in <c>GE_AVALONIA</c> (e.g. <c>GE_AVALONIA=all,FormBrowse</c>).
/// </summary>
internal static partial class AvaloniaDialogs
{
    private static readonly List<BrowseSession> _browseSessions = [];
    private static CancellationTokenSource? _mainLoop;

    /// <summary>
    ///  The Avalonia port of <see cref="GitUICommands.StartBrowseDialog"/>: the main window, shown modeless; without a message
    ///  loop yet (the start of the application), Avalonia runs the message loop until the last main window closes (as
    ///  <c>Application.Run</c> did): Avalonia owns the lifetime of the application.
    /// </summary>
    public static bool TryShowBrowse(IGitUICommands commands, BrowseArguments args)
    {
        AvaloniaUi.EnsureInitialized(GetOptions);
        ShowBrowseWindow(commands, args);
        if (!AvaloniaUi.IsMainLoopRunning)
        {
            using CancellationTokenSource mainLoop = new();
            _mainLoop = mainLoop;
            AvaloniaUi.RunMainLoop(mainLoop.Token);
            _mainLoop = null;
        }

        return true;
    }

    /// <summary>Closes the main windows (the application quits once the last one is closed).</summary>
    private static void CloseBrowseWindows()
    {
        foreach (BrowseSession session in _browseSessions.ToList())
        {
            session.Close();
        }
    }

    private static BrowseWindow ShowBrowseWindow(IGitUICommands commands, BrowseArguments args)
    {
        BrowseWindow window = new()
        {
            PositionName = "FormBrowse",
            PositionStore = WindowPositionStore.Instance,
            SelectedId = args.SelectedId.IsZero ? null : args.SelectedId,
            FirstId = args.FirstId.IsZero ? null : args.FirstId,
        };
        BrowseSession session = new(window, args);
        session.Load(commands);

        _browseSessions.Add(session);
        window.Closed += (_, _) =>
        {
            session.Dispose();
            _browseSessions.Remove(session);
            if (_browseSessions.Count == 0)
            {
                _mainLoop?.Cancel();
            }
        };
        AvaloniaDialogHost.Show(window, ownerHandle: 0);
        return window;
    }

    /// <summary>
    ///  As <c>FormBrowse.SetWorkingDir</c> for <see cref="GitUICommands.WorktreeSwitch"/>: the Avalonia main window owning
    ///  <paramref name="owner"/> (e.g. through the worktrees dialog) shows the directory.
    /// </summary>
    public static bool TrySetBrowseWorkingDir(IWin32Window? owner, string path)
    {
        foreach (nint handle in AvaloniaDialogHost.GetWindowAndOwnerHandles(owner?.Handle ?? 0))
        {
            if (_browseSessions.FirstOrDefault(s => s.Handle == handle) is { } session)
            {
                session.SetWorkingDir(path);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  The main window and the view model of its current repository: as <c>FormBrowse.SetGitModule</c>, another repository
    ///  gets new commands, and a new view model (the grid, the menus and the title; the dashboard without a valid repository).
    /// </summary>
    private sealed partial class BrowseSession(BrowseWindow window, BrowseArguments args) : IDisposable
    {
        private BrowseHost? _host;
        private RevisionGridViewModel? _grid;
        private BrowseViewModel? _viewModel;
        private GridBuildServerWatcher? _buildServerWatcher;

        // The filters of the command line apply to the first repository only.
        private BrowseArguments? _arguments = args;

        public nint Handle => window.OwnerHandle;

        public void Close() => window.Close();

        public IGitUICommands? Commands => _host?.Commands;

        /// <param name="selectedId">The revision to select once loaded (<c>RevisionGrid.SelectedId</c>), else the current one.</param>
        /// <param name="firstId">With <paramref name="selectedId"/>, the revision selected first (<c>RevisionGrid.FirstId</c>).</param>
        public void Load(IGitUICommands commands, ObjectId? selectedId = null, ObjectId? firstId = null)
        {
            Dispose();

            BrowseHost host = new(commands, window, this);
            BrowseGridFilter gridFilter = new(commands, () => new NativeWindowOwner(window), _arguments is { } arguments ? CreateBrowseFilter(arguments) : new FilterInfo());
            _arguments = null;
            RevisionGridHost gridHost = new(
                commands,
                currentCheckout => gridFilter.Filter.GetRevisionFilter(new Lazy<ObjectId>(() => currentCheckout)),
                showArtificial: true,
                getPathFilter: _ => gridFilter.GetPathFilter())
            {
                AheadBehindDataProvider = new GitCommands.Git.AheadBehindDataProvider(() => commands.Module.GitExecutable),
            };
            RevisionGridViewModel grid = new(gridHost, GetDisplayOptions())
            {
                MultiSelect = true,
                ShowArtificialCommitChanges = AppSettings.ShowGitStatusForArtificialCommits && AppSettings.RevisionGraphShowArtificialCommits,
            };
            gridFilter.Grid = grid;
            ApplyColumns(grid);
            BrowseViewModel? browseViewModel = null;
            RevisionGridMenuBuilder gridMenu = new((GitUICommands)commands, () => new NativeWindowOwner(window), grid, () => browseViewModel?.RefreshRevisions())
            {
                Filter = gridFilter,
            };
            grid.ContextMenuProvider = gridMenu.Build;

            // As RevisionGridControl: the "RevisionGrid" hotkeys, and the build statuses of the build server (ShowBuildServerInfo).
            grid.Hotkeys = LoadHotkeys(commands, HotkeyCommands.RevisionGridSettingsName);
            grid.CommandHandler = gridMenu.ExecuteCommand;
            GridBuildServerWatcher buildServerWatcher = new(commands, grid, () => new NativeWindowOwner(window));

            // As ShowDashboard: the dashboard without a valid repository.
            bool isValid = commands.Module.IsValidGitWorkingDir();
            DashboardViewModel? dashboard = isValid
                ? null
                : new DashboardViewModel(ViewStrings.Load<DashboardStrings>(), ViewStrings.Load<UserRepositoriesListStrings>(), new DashboardHost(commands, window, this));
            BrowseViewModel viewModel = new(
                ViewStrings.Load<BrowseStrings>(),
                host,
                grid,
                new CommitInfoHost(commands),
                new FileViewerHost(commands),
                ViewStrings.Load<FileStatusListStrings>(),
                GetFileStatusTreeOptions(),
                dashboard)
            {
                Filters = new FilterToolBarViewModel(ViewStrings.Load<FilterToolBarStrings>(), gridFilter),
                NavigateMenuProvider = gridMenu.CreateNavigateItems,
                ViewMenuProvider = gridMenu.CreateViewItems,
                RevisionDiffHotkeys = LoadHotkeys(commands, HotkeyCommands.RevisionDiffSettingsName),
            };
            browseViewModel = viewModel;
            UseFileStatusListMenu(viewModel.Files, commands, window);
            host.DiffFiles = viewModel.Files;

            // As RevisionDiffControl: git grep in the files of the diff and of the file tree (CanUseFindInCommitFilesGitGrep),
            // the prompt starting with the text selected in the viewer.
            viewModel.Files.GitGrep = CreateGitGrep(commands);
            viewModel.Files.GetSelectedText = () => viewModel.Viewer.Editor.SelectedText;
            UseFindInCommitFilesGitGrep(viewModel.Files, window);
            if (viewModel.FileTree is { } fileTree)
            {
                // Not UseFileStatusListMenu: the sorting of the file tree is not the one of the diff lists.
                fileTree.MenuHost = new FileStatusListMenuHost(commands, window);
                fileTree.GitGrep = CreateGitGrep(commands);
                fileTree.GetSelectedText = () => viewModel.TreeViewer?.Editor.SelectedText;
                UseFindInCommitFilesGitGrep(fileTree, window);
            }

            // The left panel, whose "filter for selected" sets the branch filter of the toolbar; none on the dashboard.
            if (isValid)
            {
                FilterToolBarViewModel filters = viewModel.Filters!;
                LeftPanelViewModel leftPanel = AttachLeftPanel(
                    commands,
                    window,
                    viewModel,
                    grid,
                    setBranchFilter: refs => filters.SetBranchFilter(refs ?? ""),
                    isBranchFilterActive: () => gridFilter.Filter.IsShowFilteredBranchesChecked && !string.IsNullOrWhiteSpace(gridFilter.Filter.BranchFilter));
                gridMenu.SelectInLeftPanel = leftPanel.SelectInLeftPanel;
            }

            // As FormBrowse (IBrowseRepo): the scripts and the plugins see the selection of the grid.
            commands.BrowseRepo = new BrowseRepoAdapter(commands, grid, window, this);
            _host = host;
            _grid = grid;
            _viewModel = viewModel;
            _buildServerWatcher = buildServerWatcher;

            if (isValid)
            {
                // As WorkingDirectoryToolStripSplitButton.RefreshContent: the repository is the most recent one.
                ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistory.AddAsMostRecentAsync(commands.Module.WorkingDir));
            }

            // As LoadHotkeys(HotkeySettingsName): the "Browse" hotkeys, with the ones of the scripts.
            window.Hotkeys = LoadHotkeys(commands, HotkeyCommands.BrowseSettingsName);
            window.ShowViewModel(viewModel, selectedId, firstId);
            if (TaskbarProgress.IsPlatformSupported)
            {
                AttachTaskbar(commands, isValid);
            }
        }

        /// <summary>As <c>FormBrowse.SetGitModule</c>, once the current event is handled (e.g. the click of a menu item).</summary>
        public void SetGitModule(IGitModule module, ObjectId? selectedId = null, ObjectId? firstId = null)
            => global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_host is not { } host)
                {
                    return;
                }

                // Reset branch colors whenever we open a new repository; as revisionDiff.RepositoryChanged, forget the remembered file.
                module.ResetRemoteColors();
                RememberFileContextMenuController.Default.RememberedDiffFileItem = null;
                if (module.IsValidGitWorkingDir())
                {
                    AppSettings.RecentWorkingDir = module.WorkingDir;
                }

                Load(host.Commands.WithGitModule(module), selectedId, firstId);
            });

        /// <summary>As <c>FormBrowse.SetWorkingDir</c>: an empty path shows the dashboard; the revisions to select, if any.</summary>
        public void SetWorkingDir(string path, ObjectId selectedId = default, ObjectId firstId = default)
        {
            if (_host is { } host)
            {
                SetGitModule(new GitModule(host.Commands.GetRequiredService<IGitExecutorProvider>(), path), selectedId.IsZero ? null : selectedId, firstId.IsZero ? null : firstId);
            }
        }

        public void Dispose()
        {
            if (_host?.Commands is { BrowseRepo: BrowseRepoAdapter adapter } commands && adapter.Window == window)
            {
                commands.BrowseRepo = null;
            }

            _buildServerWatcher?.Dispose();
            _viewModel?.Dispose();
            _host?.Dispose();
            _grid?.Dispose();
            _buildServerWatcher = null;
            _viewModel = null;
            _host = null;
            _grid = null;
        }
    }

    /// <summary>The dialogs and the git operations of the menus of <c>FormBrowse</c>, for the Avalonia main window.</summary>
    private sealed partial class BrowseHost : IBrowseHost, IDisposable
    {
        private readonly IGitUICommands _commands;
        private readonly BrowseWindow _window;
        private readonly BrowseSession _session;

        public BrowseHost(IGitUICommands commands, BrowseWindow window, BrowseSession session)
        {
            _commands = commands;
            _window = window;
            _session = session;
            commands.PostRepositoryChanged += OnPostRepositoryChanged;
        }

        public event EventHandler? RepositoryChanged;

        public IGitUICommands Commands => _commands;

        private IGitModule Module => _commands.Module;

        private NativeWindowOwner Owner => new(_window);

        public void Dispose()
        {
            _commands.PostRepositoryChanged -= OnPostRepositoryChanged;
            UnregisterPlugins();
            StopGitStatusMonitor();
            UnsubscribeOutputHistory();
            StopSubmoduleMenu();
            RepositoryChanged = null;
        }

        // As the DropDownOpening of StartToolStripMenuItem: the items of IRepositoryHistoryUIService, whose click opens the
        // repository (GitModuleChanged, handled here in this window) or, with Ctrl, a new instance.
        public IReadOnlyList<BrowseMenuItem> GetRepositoriesMenu(bool favourites)
        {
            IRepositoryHistoryUIService service = _commands.GetRequiredService<IRepositoryHistoryUIService>();
            return Convert(favourites ? service.GetFavouriteRepositoriesMenu() : service.GetRecentRepositoriesMenu());

            IReadOnlyList<BrowseMenuItem> Convert(IReadOnlyList<RepositoryMenuItem> items)
                => [.. items.Select(item => item switch
                {
                    { IsSeparator: true } => BrowseMenuItem.Separator,
                    { Children.Count: > 0 } category => new BrowseMenuItem(TranslatedText.ToAccessKeyText(category.Text), null, Children: Convert(category.Children)),
                    _ => new BrowseMenuItem(TranslatedText.ToAccessKeyText(item.Text), null, item.IsPinned ? "Pin" : null)
                    {
                        Invoke = () => Open(item),
                        Shortcut = item.BranchName,
                        ToolTip = item.ToolTip,
                    },
                })];

            void Open(RepositoryMenuItem repository) => AvaloniaUi.RunInHostContext(() =>
            {
                EventHandler<GitModuleEventArgs> handler = (_, e) => _session.SetGitModule(e.GitModule);
                service.GitModuleChanged += handler;
                try
                {
                    repository.Open?.Invoke();
                }
                finally
                {
                    service.GitModuleChanged -= handler;
                }
            });
        }

        // As FormBrowse.SetTitle.
        public string GetTitle()
        {
            IAppTitleGenerator titleGenerator = _commands.GetRequiredService<IAppTitleGenerator>();
            bool isValid = Module.IsValidGitWorkingDir();
            return titleGenerator.Generate(Module.WorkingDir, isValid, isValid ? Module.GetSelectedBranch() : null, TranslatedStrings.NoBranch);
        }

        public string GetCurrentBranch() => Module.IsValidGitWorkingDir() ? Module.GetSelectedBranch() : TranslatedStrings.NoBranch;

        /// <summary>The files of the diff tab, whose settings choose the files shown (<c>tsmiShowUntrackedFiles</c>, ...).</summary>
        public FileStatusListViewModel? DiffFiles { get; set; }

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
            => CalculateDiffsAsync(
                _commands,
                revisions,
                Module.GetCurrentCheckout(),
                allowMultiDiff: true,
                cancellationToken,
                showSkipWorktreeFiles: DiffFiles?.ShowSkipWorktreeFiles ?? false,
                showUntrackedFiles: DiffFiles?.ShowUntrackedFiles ?? true);

        public void Run(BrowseCommand command, BrowseSelection selection) => AvaloniaUi.RunInHostContext(() =>
        {
            GitRevision? latest = selection.LatestSelectedFirst.Count > 0 ? selection.LatestSelectedFirst[0] : null;
            NativeWindowOwner owner = Owner;
            switch (command)
            {
                // Start (StartToolStripMenuItem): another repository is shown in this window (SetGitModule).
                case BrowseCommand.Open:
                    if (TryShowOpenDirectory(owner, _commands.GetRequiredService<IGitExecutorProvider>(), Module, out IGitModule? module) && module is not null)
                    {
                        OpenModule(module);
                    }

                    break;
                case BrowseCommand.Clone:
                    _commands.StartCloneDialog(owner, string.Empty, false, (_, e) => OpenModule(e.GitModule));
                    break;
                case BrowseCommand.Init:
                    _commands.StartInitializeDialog(owner, gitModuleChanged: (_, e) => OpenModule(e.GitModule));
                    break;
                case BrowseCommand.ClearRecentRepositories:
                    // As tsmiRecentRepositoriesClear_Click.
                    ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistory.SaveRecentHistoryAsync([]));
                    break;
                case BrowseCommand.CloseRepository:
                    // As CloseToolStripMenuItemClick; the view model is replaced, so it is not refreshed.
                    _session.SetWorkingDir("");
                    return;

                // Repository
                case BrowseCommand.FileExplorer:
                    OsShellUtil.OpenWithFileExplorer(Module.WorkingDir);
                    break;
                case BrowseCommand.Remotes:
                    _commands.StartRemotesDialog(owner);
                    break;
                case BrowseCommand.ManageSubmodules:
                    _commands.StartSubmodulesDialog(owner);
                    break;
                case BrowseCommand.UpdateAllSubmodules:
                    _commands.StartUpdateSubmodulesDialog(owner);
                    break;
                case BrowseCommand.SynchronizeAllSubmodules:
                    _commands.StartSyncSubmodulesDialog(owner);
                    break;
                case BrowseCommand.ManageWorktrees:
                    TryShowManageWorktree(owner, _commands, out bool refresh);

                    if (refresh)
                    {
                        RepositoryChanged?.Invoke(this, EventArgs.Empty);
                    }

                    break;
                case BrowseCommand.EditGitIgnore:
                    _commands.StartEditGitIgnoreDialog(owner, false);
                    break;
                case BrowseCommand.EditGitInfoExclude:
                    _commands.StartEditGitIgnoreDialog(owner, true);
                    break;
                case BrowseCommand.EditGitAttributes:
                    _commands.StartEditGitAttributesDialog(owner);
                    break;
                case BrowseCommand.EditMailMap:
                    _commands.StartMailMapDialog(owner);
                    break;
                case BrowseCommand.OptimizeHistory:
                    ProcessDialogs.ReadProcess(owner, _commands, arguments: "maintenance run --task=commit-graph", Module.WorkingDir, input: null, useDialogSettings: true);
                    break;
                case BrowseCommand.CompressGitDatabase:
                    ProcessDialogs.ReadProcess(owner, _commands, arguments: "gc", Module.WorkingDir, input: null, useDialogSettings: true);
                    break;
                case BrowseCommand.RecoverLostObjects:
                    _commands.StartVerifyDatabaseDialog(owner);
                    break;
                case BrowseCommand.DeleteIndexLock:
                    try
                    {
                        Module.UnlockIndex(true);
                    }
                    catch (FileDeleteException ex)
                    {
                        MessageBoxes.ShowError(owner, ex.Message, ViewStrings.Load<BrowseStrings>().DeleteIndexLock.PlainText);
                    }

                    break;
                case BrowseCommand.EditLocalGitConfig:
                    _commands.StartFileEditorDialog(Module.ResolveGitInternalPath("config"), true);
                    break;
                case BrowseCommand.RepoSettings:
                    _commands.StartRepoSettingsDialog(owner);
                    break;
                case BrowseCommand.SparseWorkingCopy:
                    _commands.StartSparseWorkingCopyDialog(owner);
                    break;

                // Commands
                case BrowseCommand.Commit:
                    _commands.StartCommitDialog(owner);
                    break;
                case BrowseCommand.PullDefault:
                    // As ToolStripButtonPullClick: the default action silently, unless it is to open the dialog.
                    if (AppSettings.DefaultPullAction != GitPullAction.None)
                    {
                        _commands.StartPullDialogAndPullImmediately(owner, pullAction: AppSettings.DefaultPullAction);
                    }
                    else
                    {
                        _commands.StartPullDialog(owner, pullAction: AppSettings.FormPullAction);
                    }

                    break;
                case BrowseCommand.Pull:
                case BrowseCommand.OpenPullDialog:
                    _commands.StartPullDialog(owner, pullAction: AppSettings.FormPullAction);
                    break;
                case BrowseCommand.PullMerge:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.Merge);
                    break;
                case BrowseCommand.PullRebase:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.Rebase);
                    break;
                case BrowseCommand.Fetch:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.Fetch);
                    break;
                case BrowseCommand.FetchAll:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.FetchAll);
                    break;
                case BrowseCommand.FetchPruneAll:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.FetchPruneAll);
                    break;
                case BrowseCommand.Push:
                    _commands.StartPushDialog(owner, pushOnShow: false);
                    break;
                case BrowseCommand.QuickPush:
                    _commands.StartPushDialog(owner, pushOnShow: true);
                    break;
                case BrowseCommand.ManageStashes:
                    _commands.StartStashDialog(owner);
                    break;
                case BrowseCommand.StashChanges:
                    _commands.StashSave(owner, AppSettings.IncludeUntrackedFilesInManualStash);
                    break;
                case BrowseCommand.StashStaged:
                    _commands.StashStaged(owner);
                    break;
                case BrowseCommand.StashPop:
                    _commands.StashPop(owner);
                    break;
                case BrowseCommand.CreateStash:
                    _commands.StartStashDialog(owner, false);
                    break;
                case BrowseCommand.ResetChanges:
                    _commands.StartResetChangesDialog(owner, Module.GetWorkTreeFiles(), onlyWorkTree: false);
                    break;
                case BrowseCommand.CleanWorkingDirectory:
                    _commands.StartCleanupRepositoryDialog(owner);
                    break;
                case BrowseCommand.CreateBranch:
                    _commands.StartCreateBranchDialog(owner, latest?.ObjectId ?? default);
                    break;
                case BrowseCommand.DeleteBranch:
                    _commands.StartDeleteBranchDialog(owner, string.Empty);
                    break;
                case BrowseCommand.CheckoutBranch:
                    _commands.StartCheckoutBranch(owner);
                    break;
                case BrowseCommand.MergeBranches:
                    _commands.StartMergeBranchDialog(owner, null);
                    break;
                case BrowseCommand.Rebase:
                    Rebase(owner, selection.LatestSelectedFirst);
                    break;
                case BrowseCommand.SolveMergeConflicts:
                    _commands.StartResolveConflictsDialog(owner);
                    break;
                case BrowseCommand.CreateTag:
                    _commands.StartCreateTagDialog(owner, latest);
                    break;
                case BrowseCommand.DeleteTag:
                    _commands.StartDeleteTagDialog(owner, null);
                    break;
                case BrowseCommand.CherryPick:
                    _commands.StartCherryPickDialog(owner, selection.Descending);
                    break;
                case BrowseCommand.Archive:
                    // As ArchiveToolStripMenuItemClick.
                    if (selection.LatestSelectedFirst.Count is < 1 or > 2)
                    {
                        MessageBoxes.SelectOnlyOneOrTwoRevisions(owner);
                        break;
                    }

                    _commands.StartArchiveDialog(owner, selection.LatestSelectedFirst[0], selection.LatestSelectedFirst.Count == 2 ? selection.LatestSelectedFirst[1] : null);
                    break;
                case BrowseCommand.CheckoutRevision:
                    _commands.StartCheckoutRevisionDialog(owner);
                    break;
                case BrowseCommand.Bisect:
                    TryShowBisect(owner, _commands, new SelectionGridInfo(Module, selection));

                    break;
                case BrowseCommand.FormatPatch:
                    _commands.StartFormatPatchDialog(owner);
                    break;
                case BrowseCommand.ApplyPatch:
                    _commands.StartApplyPatchDialog(owner);
                    break;
                case BrowseCommand.ViewPatch:
                    _commands.StartViewPatchDialog(owner);
                    break;
                case BrowseCommand.Reflog:
                    TryShowReflog(owner, _commands);

                    break;

                // Tools (ToolsToolStripMenuItem)
                case BrowseCommand.GitBash:
                    StartGitBash(owner);
                    break;
                case BrowseCommand.GitGui:
                    RunGitTool(owner, GitGuiTools.GitGui, Module.RunGui);
                    break;
                case BrowseCommand.GitK:
                    RunGitTool(owner, GitGuiTools.GitK, Module.RunGitK);
                    break;
                case BrowseCommand.GitCommandLog:
                    TryShowGitCommandLog(owner);
                    break;
                case BrowseCommand.Settings:
                    _commands.StartSettingsDialog(owner);
                    break;

                // Help (HelpToolStripMenuItem)
                case BrowseCommand.UserManual:
                    OsShellUtil.OpenUrlInDefaultBrowser(AppSettings.DocumentationBaseUrl);
                    break;
                case BrowseCommand.Changelog:
                    TryShowChangeLog(owner);

                    break;
                case BrowseCommand.Translate:
                    OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/gitextensions/gitextensions/wiki/Translations");
                    break;
                case BrowseCommand.Donate:
                    TryShowDonate(owner);

                    break;
                case BrowseCommand.ReportAnIssue:
                    OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/gitextensions/gitextensions/issues");
                    break;
                case BrowseCommand.CheckForUpdates:
                    TrySearchForUpdatesAndShow(owner, alwaysShow: true);

                    break;
                case BrowseCommand.About:
                    TryShowAbout(owner);

                    break;
                case BrowseCommand.ViewSelectedRevisions:
                    if (selection.LatestSelectedFirst is [{ IsArtificial: false } revision, ..])
                    {
                        TryShowCommitDiff(owner, _commands, revision.ObjectId, modeless: true);
                    }
                    else if (selection.LatestSelectedFirst.Count == 0)
                    {
                        _commands.StartCompareRevisionsDialog(owner);
                    }

                    // A window of its own: nothing changed in the repository.
                    return;
            }

            // As the refresh after the dialogs of FormBrowse (e.g. RefreshRevisions, UpdateStashCount).
            RepositoryChanged?.Invoke(this, EventArgs.Empty);
        });

        public ObjectId? ResolveCommit(string commitOrRef, bool isRef)
            => !isRef
                ? Module.TryResolvePartialCommitId(commitOrRef, out ObjectId commitId) ? commitId : null
                : new CommitDataManager(() => Module).GetCommitData(commitOrRef)?.ObjectId;

        public void ShowRevisionFiltered(ObjectId objectId)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(Owner, objectId));

        // As RebaseToolStripMenuItemClick.
        private void Rebase(IWin32Window owner, IReadOnlyList<GitRevision> revisions)
        {
            if (revisions.Count == 0 || revisions[0].IsArtificial)
            {
                return;
            }

            string onto = revisions[0].ObjectId.ToString();
            if (revisions.Count == 2)
            {
                // The commits from the first selected commit (excluded) to HEAD, onto the second one.
                _commands.StartRebaseDialog(owner, revisions[1].ObjectId.ToShortString(), Module.GetSelectedBranch(), onto, interactive: false, startRebaseImmediately: false);
            }
            else
            {
                _commands.StartRebaseDialog(owner, onto);
            }
        }

        // As gitBashToolStripMenuItem_Click.
        // Off Windows gitk and git gui are not always installed with git: say how to install them rather than fail.
        private void RunGitTool(IWin32Window owner, string program, Action run)
        {
            if (!OperatingSystem.IsWindows() && GitGuiTools.Find(program, Module.GitExecutable) is null)
            {
                MessageBoxes.GitToolNotFound(owner, program);
                return;
            }

            try
            {
                run();
            }
            catch (Exception exception)
            {
                MessageBoxes.FailedToRunShell(owner, program, exception);
            }
        }

        private void StartGitBash(IWin32Window owner)
        {
            if (OperatingSystem.IsMacOS())
            {
                // "Open in Terminal": the repository in the Terminal application.
                try
                {
                    new Executable("/usr/bin/open", Module.WorkingDir).Start(new ArgumentBuilder { "-a", "Terminal", Module.WorkingDir.Quote() }, throwOnErrorExit: false);
                }
                catch (Exception exception)
                {
                    MessageBoxes.FailedToRunShell(owner, "Terminal", exception);
                }

                return;
            }

            IShellDescriptor? shell = _commands.GetRequiredService<IShellProvider>().GetShell(BashShell.ShellName);
            if (shell?.ExecutablePath is null)
            {
                return;
            }

            try
            {
                new Executable(shell.ExecutablePath, Module.WorkingDir).Start(createWindow: true, throwOnErrorExit: false);
            }
            catch (Exception exception)
            {
                MessageBoxes.FailedToRunShell(owner, shell.Name, exception);
            }
        }

        // As GitModuleChanged of the start menu: another repository is shown in this window.
        private void OpenModule(IGitModule module) => _session.SetGitModule(module);

        private void OnPostRepositoryChanged(object? sender, GitUIEventArgs e) => RepositoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The revision grid for the dialogs needing the WinForms one (<c>IRevisionGridInfo</c>), over the selection.</summary>
    private sealed class SelectionGridInfo(IGitModule module, BrowseSelection selection) : IRevisionGridInfo
    {
        public ObjectId CurrentCheckout => module.GetCurrentCheckout();

        public GitRevision GetRevision(ObjectId objectId)
            => selection.LatestSelectedFirst.FirstOrDefault(r => r.ObjectId == objectId) ?? module.GetRevision(objectId) ?? new GitRevision(objectId);

        public GitRevision? GetActualRevision(ObjectId objectId) => GetRevision(objectId);

        public GitRevision GetActualRevision(GitRevision revision) => revision;

        public IReadOnlyList<GitRevision> GetSelectedRevisions() => selection.LatestSelectedFirst;

        public string DescribeRevision(GitRevision revision, int maxLength = 0)
        {
            string description = $"{revision.ObjectId.ToShortString()}: {revision.Subject}";
            return maxLength > 0 && description.Length > maxLength ? description[..maxLength] : description;
        }

        public string GetCurrentBranch() => module.GetSelectedBranch();
    }

    /// <summary>The main window for the scripts and the plugins (<c>IBrowseRepo</c> of <c>FormBrowse</c>).</summary>
    private sealed class BrowseRepoAdapter(IGitUICommands commands, RevisionGridViewModel grid, BrowseWindow window, BrowseSession session) : IBrowseRepo
    {
        public BrowseWindow Window => window;

        public GitRevision? GetLatestSelectedRevision() => grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault();

        public IReadOnlyList<GitRevision> GetSelectedRevisions() => grid.GetSelectedRevisionsLatestSelectedFirst();

        public System.Drawing.Point GetQuickItemSelectorLocation()
        {
            global::Avalonia.PixelPoint position = window.Position;
            return new System.Drawing.Point(position.X + 100, position.Y + 100);
        }

        // As FormBrowse.GoToRef: the revision of the reference is selected.
        public void GoToRef(string refName, bool showNoRevisionMsg, bool toggleSelection = false)
        {
            ObjectId objectId = commands.Module.RevParse(refName);
            if (!objectId.IsZero && !grid.SelectRevision(objectId) && showNoRevisionMsg)
            {
                AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(new NativeWindowOwner(window), objectId));
            }
        }

        // As FormBrowse.SetWorkingDir: another repository is shown in the main window (the revisions to select are not passed yet).
        public void SetWorkingDir(string? path, ObjectId selectedId = default, ObjectId firstId = default) => session.SetWorkingDir(path ?? "", selectedId, firstId);
    }
}
