using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.LeftPanel;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the main window; ids match <c>FormBrowse</c>.</summary>
public sealed class BrowseStrings : ViewStrings
{
    public BrowseStrings()
        : base("FormBrowse")
    {
        Title = Add("$this", "Text", "Git Extensions");
        CommitTab = Add("CommitInfoTabPage", "Text", "Commit");
        DiffTab = Add("DiffTabPage", "Text", "Diff");
        TreeTab = Add("TreeTabPage", "Text", "File tree");
        ConsoleTab = Add("_consoleTabCaption", "Text", "Console");
        Refresh = Add("RefreshButton", "ToolTipText", "Refresh");
        CommitButton = Add("toolStripButtonCommit", "Text", "Commit");
        PullButton = Add("toolStripButtonPull", "Text", "Pull");
        PushButton = Add("toolStripButtonPush", "Text", "Push");
        ManageStashesToolTip = Add("toolStripSplitStash", "ToolTipText", "Manage stashes");
        SettingsToolTip = Add("EditSettings", "ToolTipText", "Settings");
        BranchToolTip = Add("branchSelect", "ToolTipText", "Change current branch");
        FileExplorerToolTip = Add("toolStripFileExplorer", "ToolTipText", "File Explorer");
        GitBashToolTip = Add("userShell", "ToolTipText", "Git bash");
        NoWorkingFolder = Add("_noWorkingFolderText", "Text", "No working directory");
        Loading = Add("_loading", "Text", "Loading...");

        // Start
        StartMenu = Add("fileToolStripMenuItem", "Text", "&Start");
        Open = Add("openToolStripMenuItem", "Text", "&Open...");
        Clone = Add("cloneToolStripMenuItem", "Text", "C&lone repository...");
        Init = Add("initNewRepositoryToolStripMenuItem", "Text", "&Create new repository...");
        FavouriteRepositories = Add("tsmiFavouriteRepositories", "Text", "&Favorite repositories");
        RecentRepositories = Add("tsmiRecentRepositories", "Text", "&Recent repositories");
        ClearRecentRepositories = Add("tsmiRecentRepositoriesClear", "Text", "Clear list");
        Exit = Add("exitToolStripMenuItem", "Text", "E&xit");

        // Dashboard
        DashboardMenu = Add("dashboardToolStripMenuItem", "Text", "&Dashboard");
        RefreshDashboard = Add("refreshDashboardToolStripMenuItem", "Text", "&Refresh");

        // Repository
        RepositoryMenu = Add("repositoryToolStripMenuItem", "Text", "&Repository");
        RefreshMenu = Add("refreshToolStripMenuItem", "Text", "&Refresh");
        FileExplorer = Add("fileExplorerToolStripMenuItem", "Text", "File E&xplorer");
        Remotes = Add("manageRemoteRepositoriesToolStripMenuItem1", "Text", "Remo&te repositories...");
        ManageSubmodules = Add("manageSubmodulesToolStripMenuItem", "Text", "Manage &submodules...");
        UpdateAllSubmodules = Add("updateAllSubmodulesToolStripMenuItem", "Text", "&Update all submodules");
        SynchronizeAllSubmodules = Add("synchronizeAllSubmodulesToolStripMenuItem", "Text", "Synchronize all su&bmodules");
        ManageWorktrees = Add("manageWorktreeToolStripMenuItem", "Text", "Manage &worktrees...");
        EditGitIgnore = Add("editgitignoreToolStripMenuItem1", "Text", "Edit .git&ignore");
        EditGitInfoExclude = Add("editgitinfoexcludeToolStripMenuItem", "Text", "Edit .git/info/&exclude");
        EditGitAttributes = Add("editGitAttributesToolStripMenuItem", "Text", "Edit .git&attributes");
        EditMailMap = Add("editmailmapToolStripMenuItem", "Text", "Edit .&mailmap");
        GitMaintenance = Add("gitMaintenanceToolStripMenuItem", "Text", "&Git maintenance");
        OptimizeHistory = Add("optimizeHistoryToolStripMenuItem", "Text", "&Optimize history queries");
        CompressGitDatabase = Add("compressGitDatabaseToolStripMenuItem", "Text", "&Compress git database");
        RecoverLostObjects = Add("recoverLostObjectsToolStripMenuItem", "Text", "&Recover lost objects...");
        DeleteIndexLock = Add("deleteIndexLockToolStripMenuItem", "Text", "&Delete index.lock");
        EditLocalGitConfig = Add("editLocalGitConfigToolStripMenuItem", "Text", "&Edit .git/config");
        RepoSettings = Add("repoSettingsToolStripMenuItem", "Text", "Rep&ository settings...");
        SparseWorkingCopy = Add("menuitemSparse", "Text", "Sparse Wor&king Copy");
        CloseRepository = Add("closeToolStripMenuItem", "Text", "&Close (go to Dashboard)");

        // Commands
        CommandsMenu = Add("commandsToolStripMenuItem", "Text", "&Commands");
        Commit = Add("commitToolStripMenuItem", "Text", "&Commit...");
        Pull = Add("pullToolStripMenuItem", "Text", "Pull&/Fetch...");
        Push = Add("pushToolStripMenuItem", "Text", "&Push...");
        ManageStashes = Add("stashToolStripMenuItem", "Text", "Ma&nage stashes...");
        ResetChanges = Add("resetToolStripMenuItem", "Text", "&Reset changes...");
        CleanWorkingDirectory = Add("cleanupToolStripMenuItem", "Text", "Clean &working directory...");
        CreateBranch = Add("branchToolStripMenuItem", "Text", "Create &branch...");
        DeleteBranch = Add("deleteBranchToolStripMenuItem", "Text", "De&lete branch...");
        CheckoutBranch = Add("checkoutBranchToolStripMenuItem", "Text", "Chec&kout branch...");
        MergeBranches = Add("mergeBranchToolStripMenuItem", "Text", "&Merge branches...");
        Rebase = Add("rebaseToolStripMenuItem", "Text", "R&ebase...");
        SolveMergeConflicts = Add("runMergetoolToolStripMenuItem", "Text", "&Solve merge conflicts...");
        CreateTag = Add("tagToolStripMenuItem", "Text", "Create &tag...");
        DeleteTag = Add("deleteTagToolStripMenuItem", "Text", "&Delete tag...");
        CherryPick = Add("cherryPickToolStripMenuItem", "Text", "Cherr&y pick...");
        Archive = Add("archiveToolStripMenuItem", "Text", "Archi&ve revision...");
        CheckoutRevision = Add("checkoutToolStripMenuItem", "Text", "Check&out revision...");
        Bisect = Add("bisectToolStripMenuItem", "Text", "B&isect...");
        FormatPatch = Add("formatPatchToolStripMenuItem", "Text", "&Format patch...");
        ApplyPatch = Add("applyPatchToolStripMenuItem", "Text", "&Apply patch...");
        ViewPatch = Add("patchToolStripMenuItem", "Text", "View patc&h file...");
        Reflog = Add("toolStripMenuItemReflog", "Text", "Show reflo&g...");

        // The pull and stash buttons
        PullMerge = Add("mergeToolStripMenuItem", "Text", "Pull - &merge");
        PullRebase = Add("rebaseToolStripMenuItem1", "Text", "Pull - &rebase");
        Fetch = Add("fetchToolStripMenuItem", "Text", "&Fetch");
        FetchAll = Add("fetchAllToolStripMenuItem", "Text", "Fetch &all");
        FetchPruneAll = Add("fetchPruneAllToolStripMenuItem", "Text", "F&etch and prune all");
        OpenPullDialog = Add("pullToolStripMenuItem1", "Text", "Open &pull dialog...");
        StashChanges = Add("stashChangesToolStripMenuItem", "Text", "&Stash");
        StashStaged = Add("stashStagedToolStripMenuItem", "Text", "S&tash staged");
        StashPop = Add("stashPopToolStripMenuItem", "Text", "Stash &pop");
        ManageStashesItem = Add("manageStashesToolStripMenuItem", "Text", "&Manage stashes...");
        CreateStash = Add("createAStashToolStripMenuItem", "Text", "&Create a stash...");

        // Tools
        ToolsMenu = Add("toolsToolStripMenuItem", "Text", "&Tools");
        GitBash = Add("gitBashToolStripMenuItem", "Text", "Git &bash");
        Terminal = Add("terminalToolStripMenuItem", "Text", "Open in &Terminal");
        GitGui = Add("gitGUIToolStripMenuItem", "Text", "Git &GUI");
        GitK = Add("kGitToolStripMenuItem", "Text", "Git&K");
        GitCommandLog = Add("gitcommandLogToolStripMenuItem", "Text", "Git &command log");
        Settings = Add("settingsToolStripMenuItem", "Text", "&Settings...");

        // Help
        HelpMenu = Add("helpToolStripMenuItem", "Text", "&Help");
        UserManual = Add("userManualToolStripMenuItem", "Text", "User &manual");
        Changelog = Add("changelogToolStripMenuItem", "Text", "&Changelog");
        Translate = Add("translateToolStripMenuItem", "Text", "&Translate");
        Donate = Add("donateToolStripMenuItem", "Text", "&Donate");
        ReportAnIssue = Add("reportAnIssueToolStripMenuItem", "Text", "&Report an issue");
        CheckForUpdates = Add("checkForUpdatesToolStripMenuItem", "Text", "Check for &updates");
        About = Add("aboutToolStripMenuItem", "Text", "&About");
    }

    public TranslatedText Title { get; }

    public TranslatedText CommitTab { get; }

    public TranslatedText DiffTab { get; }

    public TranslatedText TreeTab { get; }

    public TranslatedText ConsoleTab { get; }

    public TranslatedText Refresh { get; }

    public TranslatedText CommitButton { get; }

    public TranslatedText PullButton { get; }

    public TranslatedText PushButton { get; }

    public TranslatedText ManageStashesToolTip { get; }

    public TranslatedText SettingsToolTip { get; }

    public TranslatedText BranchToolTip { get; }

    public TranslatedText FileExplorerToolTip { get; }

    public TranslatedText GitBashToolTip { get; }

    public TranslatedText NoWorkingFolder { get; }

    public TranslatedText Loading { get; }

    public TranslatedText StartMenu { get; }

    public TranslatedText Open { get; }

    public TranslatedText Clone { get; }

    public TranslatedText Init { get; }

    public TranslatedText FavouriteRepositories { get; }

    public TranslatedText RecentRepositories { get; }

    public TranslatedText ClearRecentRepositories { get; }

    public TranslatedText Exit { get; }

    public TranslatedText DashboardMenu { get; }

    public TranslatedText RefreshDashboard { get; }

    public TranslatedText RepositoryMenu { get; }

    public TranslatedText RefreshMenu { get; }

    public TranslatedText FileExplorer { get; }

    public TranslatedText Remotes { get; }

    public TranslatedText ManageSubmodules { get; }

    public TranslatedText UpdateAllSubmodules { get; }

    public TranslatedText SynchronizeAllSubmodules { get; }

    public TranslatedText ManageWorktrees { get; }

    public TranslatedText EditGitIgnore { get; }

    public TranslatedText EditGitInfoExclude { get; }

    public TranslatedText EditGitAttributes { get; }

    public TranslatedText EditMailMap { get; }

    public TranslatedText GitMaintenance { get; }

    public TranslatedText OptimizeHistory { get; }

    public TranslatedText CompressGitDatabase { get; }

    public TranslatedText RecoverLostObjects { get; }

    public TranslatedText DeleteIndexLock { get; }

    public TranslatedText EditLocalGitConfig { get; }

    public TranslatedText RepoSettings { get; }

    public TranslatedText SparseWorkingCopy { get; }

    public TranslatedText CloseRepository { get; }

    public TranslatedText CommandsMenu { get; }

    public TranslatedText Commit { get; }

    public TranslatedText Pull { get; }

    public TranslatedText Push { get; }

    public TranslatedText ManageStashes { get; }

    public TranslatedText ResetChanges { get; }

    public TranslatedText CleanWorkingDirectory { get; }

    public TranslatedText CreateBranch { get; }

    public TranslatedText DeleteBranch { get; }

    public TranslatedText CheckoutBranch { get; }

    public TranslatedText MergeBranches { get; }

    public TranslatedText Rebase { get; }

    public TranslatedText SolveMergeConflicts { get; }

    public TranslatedText CreateTag { get; }

    public TranslatedText DeleteTag { get; }

    public TranslatedText CherryPick { get; }

    public TranslatedText Archive { get; }

    public TranslatedText CheckoutRevision { get; }

    public TranslatedText Bisect { get; }

    public TranslatedText FormatPatch { get; }

    public TranslatedText ApplyPatch { get; }

    public TranslatedText ViewPatch { get; }

    public TranslatedText Reflog { get; }

    public TranslatedText PullMerge { get; }

    public TranslatedText PullRebase { get; }

    public TranslatedText Fetch { get; }

    public TranslatedText FetchAll { get; }

    public TranslatedText FetchPruneAll { get; }

    public TranslatedText OpenPullDialog { get; }

    public TranslatedText StashChanges { get; }

    public TranslatedText StashStaged { get; }

    public TranslatedText StashPop { get; }

    public TranslatedText ManageStashesItem { get; }

    public TranslatedText CreateStash { get; }

    public TranslatedText ToolsMenu { get; }

    public TranslatedText GitBash { get; }

    /// <summary>On macOS instead of <see cref="GitBash"/>: the repository in Terminal.</summary>
    public TranslatedText Terminal { get; }

    public TranslatedText GitGui { get; }

    public TranslatedText GitK { get; }

    public TranslatedText GitCommandLog { get; }

    public TranslatedText Settings { get; }

    public TranslatedText HelpMenu { get; }

    public TranslatedText UserManual { get; }

    public TranslatedText Changelog { get; }

    public TranslatedText Translate { get; }

    public TranslatedText Donate { get; }

    public TranslatedText ReportAnIssue { get; }

    public TranslatedText CheckForUpdates { get; }

    public TranslatedText About { get; }
}

/// <summary>The commands of the menus and the toolbar of the main window (the handlers of <c>FormBrowse</c>).</summary>
public enum BrowseCommand
{
    Refresh,
    Open,
    Clone,
    Init,
    Exit,

    /// <summary>"Clear list" of the recent repositories (<c>tsmiRecentRepositoriesClear_Click</c>).</summary>
    ClearRecentRepositories,

    /// <summary>"Close (go to Dashboard)": the dashboard, without a repository (<c>CloseToolStripMenuItemClick</c>).</summary>
    CloseRepository,

    /// <summary>"Refresh" of the Dashboard menu.</summary>
    RefreshDashboard,

    /// <summary>"Recent repositories settings" of the Dashboard menu.</summary>
    RecentRepositoriesSettings,
    FileExplorer,
    Remotes,
    ManageSubmodules,
    UpdateAllSubmodules,
    SynchronizeAllSubmodules,
    ManageWorktrees,
    EditGitIgnore,
    EditGitInfoExclude,
    EditGitAttributes,
    EditMailMap,
    CompressGitDatabase,
    RecoverLostObjects,
    DeleteIndexLock,
    EditLocalGitConfig,
    RepoSettings,
    SparseWorkingCopy,
    Commit,

    /// <summary>The pull button: the default pull action, silently unless it is to open the dialog.</summary>
    PullDefault,
    Pull,
    PullMerge,
    PullRebase,
    Fetch,
    FetchAll,
    FetchPruneAll,
    OpenPullDialog,
    Push,

    /// <summary>The push dialog, pushing at once (the QuickPush hotkey).</summary>
    QuickPush,
    ManageStashes,
    StashChanges,
    StashStaged,
    StashPop,
    CreateStash,
    ResetChanges,
    CleanWorkingDirectory,
    CreateBranch,
    DeleteBranch,
    CheckoutBranch,
    MergeBranches,
    Rebase,
    SolveMergeConflicts,
    CreateTag,
    DeleteTag,
    CherryPick,
    Archive,
    CheckoutRevision,
    Bisect,
    FormatPatch,
    ApplyPatch,
    ViewPatch,
    Reflog,
    GitBash,
    GitGui,
    GitK,
    GitCommandLog,
    Settings,
    UserManual,
    Changelog,
    Translate,
    Donate,
    ReportAnIssue,
    CheckForUpdates,
    About,

    /// <summary>
    ///  As <c>RevisionGridControl.ViewSelectedRevisions</c> (a double click on a revision): the commit and its diff in a window,
    ///  else without selection the compare dialog.
    /// </summary>
    ViewSelectedRevisions,
    OptimizeHistory,
}

/// <summary>
///  The revisions selected in the grid: the latest selected first (as <c>GetSelectedRevisions()</c> of the WinForms grid), and
///  from the newest to the oldest.
/// </summary>
public sealed record BrowseSelection(IReadOnlyList<GitRevision> LatestSelectedFirst, IReadOnlyList<GitRevision> Descending);

/// <summary>The submenus whose items are read when the Start menu opens (as their <c>DropDownOpening</c>).</summary>
public enum BrowseSubmenu
{
    RecentRepositories,
    FavouriteRepositories,

    /// <summary>The Navigate menu, built by the application (<see cref="BrowseViewModel.GetModelSubmenuItems"/>).</summary>
    Navigate,

    /// <summary>The View menu, built by the application (<see cref="BrowseViewModel.GetModelSubmenuItems"/>).</summary>
    View,
}

/// <summary>An item of a menu of the main window, or a separator (no command, action, submenu or children).</summary>
public sealed record BrowseMenuItem(string Header, BrowseCommand? Command, object? Icon = null, IReadOnlyList<BrowseMenuItem>? Children = null)
{
    public static BrowseMenuItem Separator { get; } = new("-", null);

    /// <summary>The action of an item without a <see cref="Command"/> (e.g. a recent repository).</summary>
    public Action? Invoke { get; init; }

    /// <summary>The text shown on the right (the <c>ShortcutKeyDisplayString</c>, e.g. the branch of a recent repository).</summary>
    public string? Shortcut { get; init; }

    public string? ToolTip { get; init; }

    /// <summary>The submenu whose items are read with <see cref="BrowseViewModel.GetSubmenuItems"/>.</summary>
    public BrowseSubmenu? Submenu { get; init; }

    /// <summary>Whether the item can be clicked (e.g. not the "Loading..." of the plugins).</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Whether the item is checked (e.g. the current worktree); <see langword="null"/> if it is not a check item.</summary>
    public bool? IsChecked { get; init; }

    public bool IsSeparator => Header == "-" && Command is null && Children is null && Invoke is null && Submenu is null;
}

/// <summary>What the main window needs from the application.</summary>
public interface IBrowseHost
{
    /// <summary>As <c>FormBrowse.SetTitle</c> (<c>IAppTitleGenerator</c>): the repository and its branch.</summary>
    string GetTitle();

    /// <summary>The current branch, or the detached head (the branch button).</summary>
    string GetCurrentBranch();

    /// <summary>Runs the command of a menu item (the dialogs of <c>UICommands</c>) with the revisions selected in the grid.</summary>
    void Run(BrowseCommand command, BrowseSelection selection);

    /// <summary>The files of the diffs of the selected revisions (as <c>RevisionDiffControl</c>, <c>FileStatusDiffCalculator</c>).</summary>
    Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken);

    /// <summary>
    ///  The items of the recent (or favourite) repositories menu (<c>IRepositoryHistoryUIService</c>): each opens its
    ///  repository in this window (with Ctrl, in a new instance).
    /// </summary>
    IReadOnlyList<BrowseMenuItem> GetRepositoriesMenu(bool favourites);

    /// <summary>Raised when the repository changed (<c>RepoChangedNotifier</c>, <c>PostRepositoryChanged</c>), e.g. by a dialog.</summary>
    event EventHandler? RepositoryChanged;

    /// <summary>
    ///  The commit of a link of the commit info: a (partial) hash, or with <paramref name="isRef"/> a branch or a tag
    ///  (<c>TryResolvePartialCommitId</c>, <c>CommitDataManager.GetCommitData</c>).
    /// </summary>
    ObjectId? ResolveCommit(string commitOrRef, bool isRef) => null;

    /// <summary>As <c>MessageBoxes.RevisionFilteredInGrid</c>: the commit of a link is not shown by the grid (e.g. a filter).</summary>
    void ShowRevisionFiltered(ObjectId objectId)
    {
    }
}

/// <summary>The tabs below the grid (<c>CommitInfoTabControl</c>).</summary>
public enum BrowseTab
{
    Commit,
    Diff,
    FileTree,
    Gpg,
    Console,
    OutputHistory,
    BuildReport,
}

/// <summary>
///  Port of <c>FormBrowse</c> (the main window), first part: the revision grid with the commit and diff tabs of the selected
///  revisions, the toolbar and the menus of its commands; without a valid repository, the dashboard instead (as
///  <c>ShowDashboard</c>). Another repository is shown with a new view model for its module (as <c>SetGitModule</c>).
/// </summary>
public sealed partial class BrowseViewModel : DialogViewModel
{
    private readonly IBrowseHost _host;
    private readonly IReadOnlyList<BrowseMenuItem> _baseMenus;
    private CancellationTokenSource? _loadingDiffs;

    public BrowseViewModel(
        BrowseStrings strings,
        IBrowseHost host,
        RevisionGridViewModel grid,
        ICommitInfoHost commitInfoHost,
        IFileViewerHost fileViewerHost,
        FileStatusListStrings fileStatusListStrings,
        FileStatusTreeOptions fileStatusTreeOptions,
        DashboardViewModel? dashboard = null)
    {
        Strings = strings;
        _host = host;
        Dashboard = dashboard;
        Grid = grid;

        // As RevisionInfo.ShowBranchesAsLinks = true: the containing branches and tags go to their commit.
        CommitInfo = new CommitInfoViewModel(commitInfoHost) { ShowBranchesAsLinks = true };

        // As RevisionInfo_CommandClicked: the hashes of the parents and children, the branches and tags are links.
        CommitInfo.CommandClicked += (_, e) => OnCommitInfoCommand(e.Command, e.Data);
        Files = new FileStatusListViewModel(fileStatusListStrings, fileStatusTreeOptions) { HasFileSettings = true, HasShowDiffForAllParents = true, HasRefreshButton = true, CanAutoRefresh = true };
        Viewer = new FileViewerViewModel(fileViewerHost);

        // As FileViewer_TopScrollReached and FileViewer_BottomScrollReached of RevisionDiffControl, without the menu item.
        Viewer.ScrollOnThrough(() => Files);
        Viewer.EnableAutomaticContinuousScroll = false;
        Viewer.SearchOnThrough(() => Files);

        // As the additionalCommandInfo of ShowSelectedFileDiffAsync: a range diff of the paths the grid is filtered by.
        Viewer.RangeDiffPathFilter = () => Filters?.State.PathFilter ?? "";
        Files.SelectionChanged += (_, _) => ShowDiffFile();
        Grid.SelectionChanged += (_, _) => ShowSelectedRevisions();
        InitializeFileTree(fileViewerHost, fileStatusListStrings, fileStatusTreeOptions);
        InitializeBlame();
        InitializeFileMenus();
        InitializeGpg();
        InitializeConsole();
        InitializeWorkingDirectoryStatus();
        InitializeToolbar();
        InitializeOutputHistory();

        // Before the layout, which may select another tab (UpdateBuildReport).
        InitializeBuildReport();
        InitializeLayout();
        InitializeToolbarItems();

        // As the WinForms grid without a revision to select: the current checkout (else the first revision) is selected.
        Grid.Loaded += (_, _) =>
        {
            if (Grid.SelectedRow is null && Grid.Rows.FirstOrDefault(row => !row.Revision.IsArtificial) is { } first)
            {
                Grid.SelectedRow = first;
            }
        };
        host.RepositoryChanged += (_, _) => RefreshRevisions();

        InitializePlugins();
        _baseMenus = CreateMenus(strings, dashboard);
        Menus = AddDynamicMenus(_baseMenus);
        Title = host.GetTitle();
        CurrentBranch = host.GetCurrentBranch();
    }

    public BrowseStrings Strings { get; }

    public RevisionGridViewModel Grid { get; }

    public CommitInfoViewModel CommitInfo { get; }

    public FileStatusListViewModel Files { get; }

    public FileViewerViewModel Viewer { get; }

    /// <summary>The dashboard, shown instead of the grid and the tabs when there is no valid repository.</summary>
    public DashboardViewModel? Dashboard { get; }

    public bool IsDashboard => Dashboard is not null;

    /// <summary>The left panel (<c>RepoObjectsTree</c>) beside the grid, with its toggle (<c>toggleLeftPanel</c>); none in the dashboard.</summary>
    [ObservableProperty]
    public partial LeftPanelViewModel? LeftPanel { get; set; }

    /// <summary>The main menu (<c>mainMenuStrip</c>).</summary>
    public IReadOnlyList<BrowseMenuItem> Menus { get; private set; }

    /// <summary>The items of the drop down of the pull button (<c>toolStripButtonPull</c>).</summary>
    public IReadOnlyList<BrowseMenuItem> PullItems =>
    [
        new(Strings.PullMerge.AccessKeyText, BrowseCommand.PullMerge, "PullMerge"),
        new(Strings.PullRebase.AccessKeyText, BrowseCommand.PullRebase, "PullRebase"),
        new(Strings.Fetch.AccessKeyText, BrowseCommand.Fetch, "PullFetch"),
        new(Strings.FetchAll.AccessKeyText, BrowseCommand.FetchAll, "PullFetchAll"),
        new(Strings.FetchPruneAll.AccessKeyText, BrowseCommand.FetchPruneAll, "PullFetchPruneAll"),
        BrowseMenuItem.Separator,
        new(Strings.OpenPullDialog.AccessKeyText, BrowseCommand.OpenPullDialog, "Pull"),
        BrowseMenuItem.Separator,
        CreateDefaultPullActionMenu(),
    ];

    /// <summary>The items of the drop down of the stash button (<c>toolStripSplitStash</c>).</summary>
    public IReadOnlyList<BrowseMenuItem> StashItems =>
    [
        new(Strings.StashChanges.AccessKeyText, BrowseCommand.StashChanges),
        new(Strings.StashStaged.AccessKeyText, BrowseCommand.StashStaged, "Stash"),
        new(Strings.StashPop.AccessKeyText, BrowseCommand.StashPop),
        BrowseMenuItem.Separator,
        new(Strings.ManageStashesItem.AccessKeyText, BrowseCommand.ManageStashes),
        new(Strings.CreateStash.AccessKeyText, BrowseCommand.CreateStash),
    ];

    [ObservableProperty]
    public partial string Title { get; private set; }

    /// <summary>The current branch (<c>branchSelect</c>).</summary>
    [ObservableProperty]
    public partial string CurrentBranch { get; private set; }

    [ObservableProperty]
    public partial BrowseTab SelectedTab { get; set; }

    /// <summary>Loads the revisions, or the repositories of the dashboard (when the window is shown).</summary>
    /// <param name="firstId">With <paramref name="selectedId"/>, the revision selected first (<c>FirstId</c>).</param>
    public void Initialize(ObjectId? selectedId, ObjectId? firstId = null)
    {
        if (Dashboard is not null)
        {
            Dashboard.Refresh();
            return;
        }

        Grid.Load(selectedId, firstId);
    }

    /// <summary>As <c>RefreshRevisions</c>: the revisions are loaded again, keeping the selection (the dashboard is refreshed).</summary>
    [RelayCommand]
    public void RefreshRevisions()
    {
        Title = _host.GetTitle();
        CurrentBranch = _host.GetCurrentBranch();
        if (Dashboard is not null)
        {
            Dashboard.Refresh();
            return;
        }

        Grid.Load(Grid.SelectedRow?.ObjectId);
    }

    /// <summary>The items of a submenu, read when the Start menu opens (as the <c>DropDownOpening</c> of <c>StartToolStripMenuItem</c>).</summary>
    public IReadOnlyList<BrowseMenuItem> GetSubmenuItems(BrowseSubmenu submenu)
    {
        if (submenu == BrowseSubmenu.FavouriteRepositories)
        {
            return _host.GetRepositoriesMenu(favourites: true);
        }

        IReadOnlyList<BrowseMenuItem> recent = _host.GetRepositoriesMenu(favourites: false);
        return recent.Count == 0
            ? recent
            : [.. recent, BrowseMenuItem.Separator, new(Strings.ClearRecentRepositories.AccessKeyText, BrowseCommand.ClearRecentRepositories)];
    }

    /// <summary>A command of the menus or the toolbar.</summary>
    [RelayCommand]
    private void Run(BrowseCommand command)
    {
        if (command == BrowseCommand.Refresh)
        {
            RefreshRevisions();
            return;
        }

        if (command == BrowseCommand.Exit)
        {
            Close(accepted: true);
            return;
        }

        if (command == BrowseCommand.RefreshDashboard)
        {
            Dashboard?.Refresh();
            return;
        }

        if (command == BrowseCommand.RecentRepositoriesSettings)
        {
            Dashboard?.ConfigureRecentRepositories();
            return;
        }

        // On macOS the command opens Terminal (the console tab has its own button).
        if (command == BrowseCommand.GitBash && !OperatingSystem.IsMacOS() && ShowShellInConsole())
        {
            return;
        }

        _host.Run(command, new BrowseSelection(Grid.GetSelectedRevisionsLatestSelectedFirst(), Grid.GetSelectedRevisions(descending: true)));
    }

    /// <summary>As <c>FormBrowse.RevisionInfo_CommandClicked</c>: a link of the commit info selects its commit in the grid.</summary>
    private void OnCommitInfoCommand(string command, string? data)
    {
        switch (command)
        {
            case "navigatebackward":
                Grid.NavigateBackward();
                return;
            case "navigateforward":
                Grid.NavigateForward();
                return;
            case "gotocommit" or "gotobranch" or "gototag" when data is not null:
                if (_host.ResolveCommit(data, isRef: command != "gotocommit") is { IsZero: false } objectId && !Grid.SelectRevision(objectId))
                {
                    _host.ShowRevisionFiltered(objectId);
                }

                return;
        }
    }

    // As FormBrowse.RevisionGrid_SelectionChanged and FillCommitInfo / RevisionDiffControl.DisplayDiffTab.
    private void ShowSelectedRevisions()
    {
        IReadOnlyList<GitRevision> selected = Grid.GetSelectedRevisionsLatestSelectedFirst();

        // As SetRevisionWithChildren: the children of the grid are shown after the dates.
        CommitInfo.SetRevision(selected.Count == 0 ? null : selected[0], selected.Count == 0 ? null : Grid.GetChildren(selected[0].ObjectId));
        _ = ShowDiffsAsync(selected);
        UpdateFileTree(revisionChanged: true);
        UpdateGpgInfo(revisionChanged: true);
        UpdateBuildReport(revisionChanged: true);
    }

    /// <param name="refresh">Whether the files are shown again, keeping the selection (<c>RefreshArtificial</c>).</param>
    private async Task ShowDiffsAsync(IReadOnlyList<GitRevision> revisions, bool refresh = false)
    {
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loadingDiffs?.Cancel();
#pragma warning restore VSTHRD103
        _loadingDiffs = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadingDiffs.Token;
        if (revisions.Count == 0)
        {
            Files.SetGroups([]);
            return;
        }

        if (!refresh)
        {
            Files.SetLoading();
        }

        IReadOnlyList<FileStatusGroup> groups;
        try
        {
            groups = await _host.GetDiffsAsync(revisions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (refresh)
        {
            Files.RefreshGroups(groups);
        }
        else
        {
            Files.SetGroups(groups);
        }
    }

    // As FormBrowse.InternalInitialize: the Dashboard menu without a repository, else the Repository and Commands menus.
    private static IReadOnlyList<BrowseMenuItem> CreateMenus(BrowseStrings s, DashboardViewModel? dashboard)
    {
        IReadOnlyList<BrowseMenuItem> menus = CreateMenus(s);
        if (dashboard is null)
        {
            return menus;
        }

        BrowseMenuItem dashboardMenu = new(s.DashboardMenu.AccessKeyText, null, Children:
        [
            new(s.RefreshDashboard.AccessKeyText, BrowseCommand.RefreshDashboard, "ReloadRevisions"),
            BrowseMenuItem.Separator,
            new(dashboard.ListStrings.Configure.AccessKeyText, BrowseCommand.RecentRepositoriesSettings, "Settings"),
        ]);
        return [menus[0], dashboardMenu, .. menus.Skip(3)];
    }

    private static IReadOnlyList<BrowseMenuItem> CreateMenus(BrowseStrings s) =>
    [
        new(s.StartMenu.AccessKeyText, null, Children:
        [
            new(s.Init.AccessKeyText, BrowseCommand.Init, "RepoCreate"),
            new(s.Open.AccessKeyText, BrowseCommand.Open, "RepoOpen"),
            new(s.FavouriteRepositories.AccessKeyText, null, "Star") { Submenu = BrowseSubmenu.FavouriteRepositories },
            new(s.RecentRepositories.AccessKeyText, null, "RecentRepositories") { Submenu = BrowseSubmenu.RecentRepositories },
            BrowseMenuItem.Separator,
            new(s.Clone.AccessKeyText, BrowseCommand.Clone, "CloneRepoGit"),
            BrowseMenuItem.Separator,
            new(s.Exit.AccessKeyText, BrowseCommand.Exit),
        ]),
        new(s.RepositoryMenu.AccessKeyText, null, Children:
        [
            new(s.RefreshMenu.AccessKeyText, BrowseCommand.Refresh, "ReloadRevisions"),
            new(s.FileExplorer.AccessKeyText, BrowseCommand.FileExplorer, "BrowseFileExplorer"),
            BrowseMenuItem.Separator,
            new(s.Remotes.AccessKeyText, BrowseCommand.Remotes, "Remotes"),
            BrowseMenuItem.Separator,
            new(s.ManageSubmodules.AccessKeyText, BrowseCommand.ManageSubmodules, "SubmodulesManage"),
            new(s.UpdateAllSubmodules.AccessKeyText, BrowseCommand.UpdateAllSubmodules, "SubmodulesUpdate"),
            new(s.SynchronizeAllSubmodules.AccessKeyText, BrowseCommand.SynchronizeAllSubmodules, "SubmodulesSync"),
            BrowseMenuItem.Separator,
            new(s.ManageWorktrees.AccessKeyText, BrowseCommand.ManageWorktrees, "WorkTree"),
            BrowseMenuItem.Separator,
            new(s.EditGitIgnore.AccessKeyText, BrowseCommand.EditGitIgnore, "EditGitIgnore"),
            new(s.EditGitInfoExclude.AccessKeyText, BrowseCommand.EditGitInfoExclude, "EditGitIgnore"),
            new(s.EditGitAttributes.AccessKeyText, BrowseCommand.EditGitAttributes),
            new(s.EditMailMap.AccessKeyText, BrowseCommand.EditMailMap, "EditMailMap"),
            new(s.GitMaintenance.AccessKeyText, null, "Maintenance", Children:
            [
                new(s.OptimizeHistory.AccessKeyText, BrowseCommand.OptimizeHistory, "Maintenance"),
                new(s.CompressGitDatabase.AccessKeyText, BrowseCommand.CompressGitDatabase, "CompressGitDatabase"),
                new(s.RecoverLostObjects.AccessKeyText, BrowseCommand.RecoverLostObjects, "RecoverLostObjects"),
                new(s.DeleteIndexLock.AccessKeyText, BrowseCommand.DeleteIndexLock, "DeleteIndexLock"),
                new(s.EditLocalGitConfig.AccessKeyText, BrowseCommand.EditLocalGitConfig, "EditGitConfig"),
            ]),
            new(s.SparseWorkingCopy.AccessKeyText, BrowseCommand.SparseWorkingCopy),
            BrowseMenuItem.Separator,
            new(s.RepoSettings.AccessKeyText, BrowseCommand.RepoSettings, "Settings"),
            BrowseMenuItem.Separator,
            new(s.CloseRepository.AccessKeyText, BrowseCommand.CloseRepository, "DashboardFolderGit"),
        ]),
        new(s.CommandsMenu.AccessKeyText, null, Children:
        [
            new(s.Commit.AccessKeyText, BrowseCommand.Commit, "RepoStateClean"),
            new(s.Pull.AccessKeyText, BrowseCommand.Pull, "Pull"),
            new(s.Push.AccessKeyText, BrowseCommand.Push, "Push"),
            BrowseMenuItem.Separator,
            new(s.ManageStashes.AccessKeyText, BrowseCommand.ManageStashes, "Stash"),
            new(s.ResetChanges.AccessKeyText, BrowseCommand.ResetChanges, "ResetWorkingDirChanges"),
            new(s.CleanWorkingDirectory.AccessKeyText, BrowseCommand.CleanWorkingDirectory, "CleanupRepo"),
            BrowseMenuItem.Separator,
            new(s.CreateBranch.AccessKeyText, BrowseCommand.CreateBranch, "BranchCreate"),
            new(s.DeleteBranch.AccessKeyText, BrowseCommand.DeleteBranch, "BranchDelete"),
            new(s.CheckoutBranch.AccessKeyText, BrowseCommand.CheckoutBranch, "BranchCheckout"),
            new(s.MergeBranches.AccessKeyText, BrowseCommand.MergeBranches, "Merge"),
            new(s.Rebase.AccessKeyText, BrowseCommand.Rebase, "Rebase"),
            new(s.SolveMergeConflicts.AccessKeyText, BrowseCommand.SolveMergeConflicts, "SolveMerge"),
            BrowseMenuItem.Separator,
            new(s.CreateTag.AccessKeyText, BrowseCommand.CreateTag, "TagCreate"),
            new(s.DeleteTag.AccessKeyText, BrowseCommand.DeleteTag, "TagDelete"),
            BrowseMenuItem.Separator,
            new(s.CherryPick.AccessKeyText, BrowseCommand.CherryPick, "CherryPick"),
            new(s.Archive.AccessKeyText, BrowseCommand.Archive, "ArchiveRevision"),
            new(s.CheckoutRevision.AccessKeyText, BrowseCommand.CheckoutRevision, "Checkout"),
            new(s.Bisect.AccessKeyText, BrowseCommand.Bisect, "Bisect"),
            BrowseMenuItem.Separator,
            new(s.FormatPatch.AccessKeyText, BrowseCommand.FormatPatch, "PatchFormat"),
            new(s.ApplyPatch.AccessKeyText, BrowseCommand.ApplyPatch, "PatchApply"),
            new(s.ViewPatch.AccessKeyText, BrowseCommand.ViewPatch, "PatchView"),
            BrowseMenuItem.Separator,
            new(s.Reflog.AccessKeyText, BrowseCommand.Reflog, "Book"),
        ]),
        new(s.ToolsMenu.AccessKeyText, null, Children:
        [
            OperatingSystem.IsMacOS()
                ? new(s.Terminal.AccessKeyText, BrowseCommand.GitBash, "Console")
                : new(s.GitBash.AccessKeyText, BrowseCommand.GitBash, "GitForWindows"),
            new(s.GitGui.AccessKeyText, BrowseCommand.GitGui, "GitGui"),
            new(s.GitK.AccessKeyText, BrowseCommand.GitK, "Gitk"),
            BrowseMenuItem.Separator,
            new(s.GitCommandLog.AccessKeyText, BrowseCommand.GitCommandLog, "GitCommandLog"),
            BrowseMenuItem.Separator,
            new(s.Settings.AccessKeyText, BrowseCommand.Settings, "Settings"),
        ]),
        new(s.HelpMenu.AccessKeyText, null, Children:
        [
            new(s.UserManual.AccessKeyText, BrowseCommand.UserManual, "GotoManual"),
            new(s.Changelog.AccessKeyText, BrowseCommand.Changelog, "Changelog"),
            BrowseMenuItem.Separator,
            new(s.Translate.AccessKeyText, BrowseCommand.Translate, "Translate"),
            new(s.Donate.AccessKeyText, BrowseCommand.Donate, "DollarSign"),
            new(s.ReportAnIssue.AccessKeyText, BrowseCommand.ReportAnIssue, "BugReport"),
            new(s.CheckForUpdates.AccessKeyText, BrowseCommand.CheckForUpdates, "CheckForUpdates"),
            BrowseMenuItem.Separator,
            new(s.About.AccessKeyText, BrowseCommand.About, "Information"),
        ]),
    ];
}
