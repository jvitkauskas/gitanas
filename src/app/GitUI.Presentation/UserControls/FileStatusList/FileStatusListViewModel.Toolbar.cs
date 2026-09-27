using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.FileStatusList;

/// <summary>Strings of the toolbar of the file status list, its git grep box and its tree items; ids match <c>FileStatusList</c>.</summary>
public sealed class FileStatusListToolbarStrings : ViewStrings
{
    public FileStatusListToolbarStrings()
        : base("FileStatusList")
    {
        CollapseGroups = Add("btnCollapseGroups", "ToolTipText", "Collapse all groups, otherwise expand the selected group");
        Refresh = Add("btnRefresh", "ToolTipText", "Refresh artificial commit");
        UnequalChange = Add("btnUnequalChange", "ToolTipText", "Show files with different changes");
        OnlyB = Add("btnOnlyB", "ToolTipText", "Show files changed in B only");
        OnlyA = Add("btnOnlyA", "ToolTipText", "Show files changed in A only");
        SameChange = Add("btnSameChange", "ToolTipText", "Show files changed equally");
        FindInFilesGitGrep = Add("btnFindInFilesGitGrep", "ToolTipText", "Toggle 'Find in commit files using git-grep'");
        FindUsingMatchCase = Add("tsmiFindUsingMatchCase", "Text", "Match &case");
        FindUsingWholeWord = Add("tsmiFindUsingWholeWord", "Text", "Match &whole word");
        FindUsingOptions = Add("tsmiFindUsingOptions", "Text", "&Options");
        FindUsingDialog = Add("tsmiFindUsingDialog", "Text", "Using &dialog");
        FindUsingInputBox = Add("tsmiFindUsingInputBox", "Text", "Using &input box");
        FindUsingBoth = Add("tsmiFindUsingBoth", "Text", "Using &both");
        GitGrepWatermark = Add("cboFindInCommitFilesGitGrep", "Watermark", "Find in commit files using git-grep regular expression...");
        Settings = Add("btnSettings", "Text", "Settings");
        ShowIgnoredFiles = Add("tsmiShowIgnoredFiles", "Text", "Show &ignored files");
        ShowSkipWorktreeFiles = Add("tsmiShowSkipWorktreeFiles", "Text", "Show s&kip-worktree files");
        ShowAssumeUnchangedFiles = Add("tsmiShowAssumeUnchangedFiles", "Text", "Show &assumed-unchanged files");
        ShowUntrackedFiles = Add("tsmiShowUntrackedFiles", "Text", "Show &untracked files");
        EditGitIgnore = Add("tsmiEditGitIgnore", "Text", "&Edit ignored files");
        EditLocallyIgnoredFiles = Add("tmsiEditLocallyIgnoredFiles", "Text", "Edit &locally ignored files");
        RefreshOnFormFocus = Add("tsmiRefreshOnFormFocus", "Text", "&Refresh artificial commits on form focus");
        ShowDiffForAllParents = Add("tsmiShowDiffForAllParents", "Text", "&Show file differences for all parents");
        Toolbar = Add("tsmiToolbar", "Text", "&Toolbar");
        DenseTree = Add("tsmiDenseTree", "Text", "&Dense tree (merge single item with its folder node)");
        ShowGroupNodesInFlatList = Add("tsmiShowGroupNodesInFlatList", "Text", "Show &group nodes in flat list (if multiple)");
        OpenGitGrepDialog = Add("tsmiOpenFindInCommitFilesGitGrepDialog", "Text", "Find in &commit files using git-grep...");
        ShowGitGrepBox = Add("tsmiShowFindInCommitFilesGitGrep", "Text", "Show 'Find in commit fi&les using git-grep'");
        SelectAll = Add("_selectAll", "Text", "S&elect all");
        DisableCustomDiffTools = Add("_disableMenuItem", "Text", "Disable this dropdown", category: "TranslatedStrings");
        CollapseRootFolders = Add("_collapseRootFolders", "Text", "Collap&se root folders");
    }

    public TranslatedText CollapseGroups { get; }

    public TranslatedText Refresh { get; }

    public TranslatedText UnequalChange { get; }

    public TranslatedText OnlyB { get; }

    public TranslatedText OnlyA { get; }

    public TranslatedText SameChange { get; }

    public TranslatedText FindInFilesGitGrep { get; }

    public TranslatedText FindUsingMatchCase { get; }

    public TranslatedText FindUsingWholeWord { get; }

    public TranslatedText FindUsingOptions { get; }

    public TranslatedText FindUsingDialog { get; }

    public TranslatedText FindUsingInputBox { get; }

    public TranslatedText FindUsingBoth { get; }

    public TranslatedText GitGrepWatermark { get; }

    public TranslatedText Settings { get; }

    public TranslatedText ShowIgnoredFiles { get; }

    public TranslatedText ShowSkipWorktreeFiles { get; }

    public TranslatedText ShowAssumeUnchangedFiles { get; }

    public TranslatedText ShowUntrackedFiles { get; }

    public TranslatedText EditGitIgnore { get; }

    public TranslatedText EditLocallyIgnoredFiles { get; }

    public TranslatedText RefreshOnFormFocus { get; }

    public TranslatedText ShowDiffForAllParents { get; }

    public TranslatedText Toolbar { get; }

    public TranslatedText DenseTree { get; }

    public TranslatedText ShowGroupNodesInFlatList { get; }

    public TranslatedText OpenGitGrepDialog { get; }

    public TranslatedText ShowGitGrepBox { get; }

    public TranslatedText SelectAll { get; }

    public TranslatedText DisableCustomDiffTools { get; }

    public TranslatedText CollapseRootFolders { get; }
}

/// <summary>The files a list shows as its settings choose (<c>tsmiShowIgnoredFiles</c>, ...).</summary>
public sealed record FileStatusFileOptions(bool ShowIgnoredFiles = false, bool ShowAssumeUnchangedFiles = false, bool ShowSkipWorktreeFiles = false, bool ShowUntrackedFiles = true);

/// <summary>A difftool item and the custom difftool chosen in its submenu.</summary>
public sealed record DifftoolChoice(DifftoolKind Kind, string Tool);

/// <summary>How the file status list searches with git grep (<c>AppSettings.FileStatusFindInFilesGitGrepTypeIndex</c>).</summary>
public enum GitGrepUsing
{
    /// <summary>The prompt (<c>FormFindInCommitFilesGitGrep</c>).</summary>
    Dialog = 0,

    /// <summary>The search box of the list.</summary>
    InputBox = 1,

    /// <summary>Both.</summary>
    Both = 2,
}

/// <summary>
///  The git grep of the files of a list, for a revision (as <c>FileStatusDiffCalculator.SetGrep</c> and <c>Calculate</c> with
///  <c>refreshGrep</c>): the group of the matching files, or of all the files for an empty expression in the file tree.
/// </summary>
public delegate Task<FileStatusGroup?> FileStatusGitGrep(GitRevision revision, string grepArguments, bool fileTreeMode, CancellationToken cancellationToken);

/// <summary>
///  The toolbar of the file status list (port of <c>FileStatusList.Toolbar.cs</c>): collapsing the groups, the refresh of the
///  artificial commits, the A/B filter buttons, the git grep box with its options (<c>cboFindInCommitFilesGitGrep</c>,
///  <c>btnFindInFilesGitGrep</c>), the settings (the files shown, .gitignore, the diff for all parents) and the tree items of
///  the context menu (<c>FileStatusList.TreeContextMenu.cs</c>).
/// </summary>
public sealed partial class FileStatusListViewModel
{
    // As GrepStringRegex: an expression with its own -e is passed as is.
    [GeneratedRegex(@"(^|\s)-e(\s|\s+['""])", RegexOptions.ExplicitCapture)]
    private static partial Regex GrepStringRegex { get; }

    private CancellationTokenSource? _gitGrepLoading;

    /// <summary>The delay of git grep after a key is typed in the box (none in tests).</summary>
    internal int GitGrepDelayMilliseconds { get; set; } = 200;
    private FileStatusGroup? _gitGrepGroup;
    private bool _searchingWithoutTyping;

    public FileStatusListToolbarStrings ToolbarStrings { get; } = ViewStrings.Load<FileStatusListToolbarStrings>();

    /// <summary>
    ///  The git grep of the files of the list (<c>CanUseFindInCommitFilesGitGrep</c>), if the list has one: the diff and file
    ///  tree tabs of the main window.
    /// </summary>
    public FileStatusGitGrep? GitGrep
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(CanUseGitGrep));
            IsGitGrepBoxVisible = value is not null && AppSettings.ShowFindInCommitFilesGitGrep.Value;
        }
    }

    public bool CanUseGitGrep => GitGrep is not null;

    /// <summary>Whether the git grep box is shown (<c>AppSettings.ShowFindInCommitFilesGitGrep</c>).</summary>
    [ObservableProperty]
    public partial bool IsGitGrepBoxVisible { get; set; }

    /// <summary>The git grep expression of the box (<c>cboFindInCommitFilesGitGrep.Text</c>); the files are searched as it is typed.</summary>
    [ObservableProperty]
    public partial string GitGrepText { get; set; } = "";

    /// <summary>The expressions searched before, the last first (the items of <c>cboFindInCommitFilesGitGrep</c>).</summary>
    public ObservableCollection<string> GitGrepHistory { get; } = [];

    public bool HasGitGrepHistory => GitGrepHistory.Count > 0;

    /// <summary>Whether a git grep expression is searched (<c>FindInCommitFilesGitGrepActive</c>).</summary>
    public bool IsGitGrepActive => !string.IsNullOrEmpty(GitGrepText);

    /// <summary>The selected text of the viewer, which the git grep prompt starts with (<c>_getSelectedText</c>).</summary>
    public Func<string?>? GetSelectedText { get; set; }

    /// <summary>How git grep is searched: the prompt, the box or both (<c>AppSettings.FileStatusFindInFilesGitGrepTypeIndex</c>).</summary>
    public GitGrepUsing GitGrepUsing
    {
        get => (GitGrepUsing)Math.Clamp(AppSettings.FileStatusFindInFilesGitGrepTypeIndex.Value, 0, 2);
        set
        {
            AppSettings.FileStatusFindInFilesGitGrepTypeIndex.Value = (int)value;
            OnPropertyChanged();
        }
    }

    /// <summary>Match case in git grep (<c>AppSettings.GitGrepIgnoreCase</c>, reversed).</summary>
    public bool GitGrepMatchCase
    {
        get => !AppSettings.GitGrepIgnoreCase.Value;
        set
        {
            AppSettings.GitGrepIgnoreCase.Value = !value;
            OnPropertyChanged();
            SearchGitGrep(GitGrepText);
        }
    }

    /// <summary>Match whole words in git grep (<c>AppSettings.GitGrepMatchWholeWord</c>).</summary>
    public bool GitGrepWholeWord
    {
        get => AppSettings.GitGrepMatchWholeWord.Value;
        set
        {
            AppSettings.GitGrepMatchWholeWord.Value = value;
            OnPropertyChanged();
            SearchGitGrep(GitGrepText);
        }
    }

    /// <summary>The regular expression option of git grep (<c>AppSettings.GitGrepUserArguments</c>, e.g. <c>--perl-regexp</c>).</summary>
    public string GitGrepUserArguments
    {
        get => AppSettings.GitGrepUserArguments.Value;
        set
        {
            AppSettings.GitGrepUserArguments.Value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GitGrepOptionsText));
            SearchGitGrep(GitGrepText);
        }
    }

    /// <summary>The text of the Options item, with the option chosen (as <c>FindInFilesGitGrep_DropDownOpening</c>).</summary>
    public string GitGrepOptionsText => $"{ToolbarStrings.FindUsingOptions.AccessKeyText}: {GitGrepUserArguments}";

    /// <summary>Raised to show the git grep prompt with a text (as <c>ShowFindInCommitFileGitGrepDialog</c>), which the host shows.</summary>
    public event EventHandler<string>? GitGrepDialogRequested;

    /// <summary>Raised to close the git grep prompt (<c>_formFindInCommitFilesGitGrep.Close</c>).</summary>
    public event EventHandler? GitGrepDialogCloseRequested;

    /// <summary>Whether the git grep prompt is open (the host sets it).</summary>
    public bool IsGitGrepDialogOpen { get; set; }

    /// <summary>Show files with different changes in A and B (<c>btnUnequalChange</c>).</summary>
    [ObservableProperty]
    public partial bool ShowUnequalChange { get; set; } = true;

    /// <summary>Show files changed in B only (<c>btnOnlyB</c>).</summary>
    [ObservableProperty]
    public partial bool ShowOnlyB { get; set; } = true;

    /// <summary>Show files changed in A only (<c>btnOnlyA</c>).</summary>
    [ObservableProperty]
    public partial bool ShowOnlyA { get; set; } = true;

    /// <summary>Show files changed equally in A and B (<c>btnSameChange</c>).</summary>
    [ObservableProperty]
    public partial bool ShowSameChange { get; set; } = true;

    /// <summary>Whether the diffs have the A and B groups of a diff with a common BASE, which the A/B buttons filter (<c>HasDiffABGroups</c>).</summary>
    public bool HasDiffABGroups => _groups.Any(group => group.IconName is FileStatusIcons.DiffA or FileStatusIcons.DiffB);

    /// <summary>Whether the list has groups to collapse (<c>btnCollapseGroups.Visible</c>).</summary>
    public bool HasGroups => CanUseGitGrep || (Nodes.Count > 0 && Nodes[0].Group is not null);

    /// <summary>Whether artificial commits are shown, which the refresh button refreshes (<c>btnRefresh.Enabled</c>).</summary>
    public bool HasArtificialCommits => _groups.Any(group => group.SecondRevision.IsArtificial || group.FirstRevision?.IsArtificial == true);

    /// <summary>Whether the working directory is shown, whose files the settings choose (<c>tsmiShowUntrackedFiles.Enabled</c>).</summary>
    public bool HasWorkTree => _groups.Any(group => group.SecondRevision.ObjectId == ObjectId.WorkTreeId || group.FirstRevision?.ObjectId == ObjectId.WorkTreeId);

    /// <summary>
    ///  Whether the owner of the list reads the files to show from the settings (<see cref="ShowUntrackedFiles"/>, ...), which the
    ///  settings button then shows (the diff tab of the main window and the lists of the commit dialog).
    /// </summary>
    public bool HasFileSettings { get; init; }

    /// <summary>Whether the ignored and assumed-unchanged files can be shown (the unstaged files of the commit dialog).</summary>
    public bool HasIgnoredFileSettings { get; init; }

    /// <summary>Whether the refresh button is shown (<c>btnRefresh.Visible</c>, set by <c>Bind</c> and <c>BindContextMenu</c>).</summary>
    public bool HasRefreshButton { get; init; }

    /// <summary>Whether the owner refreshes the artificial commits on activation, which the settings choose (<c>canAutoRefresh</c>).</summary>
    public bool CanAutoRefresh { get; init; }

    /// <summary>The files to show that the settings choose, which the owner reads the files with.</summary>
    public FileStatusFileOptions FileOptions => new(ShowIgnoredFiles, ShowAssumeUnchangedFiles, ShowSkipWorktreeFiles, ShowUntrackedFiles);

    /// <summary>Whether "Show file differences for all parents" is in the settings (<c>_enableDisablingShowDiffForAllParents</c>).</summary>
    public bool HasShowDiffForAllParents { get; init; }

    /// <summary>Show the ignored files (<c>tsmiShowIgnoredFiles</c>).</summary>
    [ObservableProperty]
    public partial bool ShowIgnoredFiles { get; set; }

    /// <summary>Show the skip-worktree files (<c>tsmiShowSkipWorktreeFiles</c>).</summary>
    [ObservableProperty]
    public partial bool ShowSkipWorktreeFiles { get; set; }

    /// <summary>Show the assumed-unchanged files (<c>tsmiShowAssumeUnchangedFiles</c>).</summary>
    [ObservableProperty]
    public partial bool ShowAssumeUnchangedFiles { get; set; }

    /// <summary>Show the untracked files (<c>tsmiShowUntrackedFiles</c>).</summary>
    [ObservableProperty]
    public partial bool ShowUntrackedFiles { get; set; } = true;

    /// <summary><c>AppSettings.RefreshArtificialCommitOnApplicationActivated</c>.</summary>
    public bool RefreshOnFormFocus
    {
        get => AppSettings.RefreshArtificialCommitOnApplicationActivated;
        set
        {
            AppSettings.RefreshArtificialCommitOnApplicationActivated = value;
            OnPropertyChanged();
        }
    }

    /// <summary><c>AppSettings.ShowDiffForAllParents</c>; changing it shows the diffs again.</summary>
    public bool ShowDiffForAllParents
    {
        get => AppSettings.ShowDiffForAllParents;
        set
        {
            AppSettings.ShowDiffForAllParents = value;
            OnPropertyChanged();
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>The tooltip of "Show file differences for all parents" (<c>TranslatedStrings.ShowDiffForAllParentsTooltip</c>).</summary>
    public string ShowDiffForAllParentsToolTip => ViewStrings.Load<GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages.DiffViewerSettingsPageStrings>().ShowDiffForAllParentsTooltip.Text;

    /// <summary>Whether a selected node has nodes below it, for the tree items of the menu (<c>UpdateStatusOfTreeContextMenuItems</c>).</summary>
    public bool HasSelectedNodesWithChildren => SelectedNodes.Any(node => node.Children.Count > 0);

    /// <summary>Whether "Collapse root folders" is shown: the file tree with an expanded root.</summary>
    public bool CanCollapseRootFolders => IsFileTreeMode && Nodes.Any(node => node.IsExpanded);

    /// <summary>Searches the files with a git grep expression (as <c>FindInCommitFilesGitGrep</c>); an empty one ends the search.</summary>
    [RelayCommand]
    public void SearchGitGrep(string expression)
    {
        if (GitGrepText != expression)
        {
            // Searches, by OnGitGrepTextChanged.
            _searchingWithoutTyping = true;
            try
            {
                GitGrepText = expression;
            }
            finally
            {
                _searchingWithoutTyping = false;
            }

            return;
        }

        StartGitGrep(expression, delayMilliseconds: 0, addToHistory: true);
    }

    /// <summary>Shows or hides the git grep box (<c>SetFindInCommitFilesGitGrepVisibility</c>); a hidden box ends its search.</summary>
    public void SetGitGrepBoxVisible(bool visible)
    {
        if (!CanUseGitGrep)
        {
            return;
        }

        AppSettings.ShowFindInCommitFilesGitGrep.Value = visible;
        IsGitGrepBoxVisible = visible;
        if (!visible && !IsGitGrepDialogOpen && GitGrepText.Length > 0)
        {
            SearchGitGrep("");
        }
    }

    /// <summary>As <c>ShowFindInCommitFileGitGrepDialog</c>: the prompt with the text, else the searched expression.</summary>
    [RelayCommand]
    private void OpenGitGrepDialog()
    {
        if (!CanUseGitGrep)
        {
            return;
        }

        string text = GetSelectedText?.Invoke() is { Length: > 0 } selected ? selected : IsGitGrepActive ? GitGrepText : "";
        GitGrepDialogRequested?.Invoke(this, text);
    }

    /// <summary>The "Show 'Find in commit files using git-grep'" item (<c>ShowFindInCommitFilesGitGrep_Click</c>).</summary>
    [RelayCommand]
    private void ToggleGitGrepBox() => SetGitGrepBoxVisible(!IsGitGrepBoxVisible);

    /// <summary>
    ///  The git grep button (<c>FindInFilesGitGrep_ButtonClick</c>): shows the box and / or the prompt, as chosen, or hides them if
    ///  they are shown; the items choosing how also show them.
    /// </summary>
    [RelayCommand]
    private void ToggleGitGrep(GitGrepUsing? choose)
    {
        if (choose is { } chosen)
        {
            GitGrepUsing = chosen;
        }

        bool usingInputBox = GitGrepUsing != GitGrepUsing.Dialog;
        bool usingDialog = GitGrepUsing != GitGrepUsing.InputBox;
        bool isVisible = (!usingInputBox || IsGitGrepBoxVisible) && (!usingDialog || IsGitGrepDialogOpen);
        bool setVisible = choose is not null || !isVisible;

        SetGitGrepBoxVisible(setVisible && usingInputBox);
        if (setVisible && usingDialog)
        {
            GitGrepDialogRequested?.Invoke(this, "");
        }
        else
        {
            GitGrepDialogCloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void ClearGitGrep() => SearchGitGrep("");

    [RelayCommand]
    private void SetGitGrepOption(string option) => GitGrepUserArguments = option;

    /// <summary>
    ///  The difftools configured in git, for the submenus of the difftool items (<c>LoadCustomDifftools</c>); none without
    ///  several, the items then have no submenu.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> CustomDiffTools { get; set; } = [];

    /// <summary>"Disable this dropdown" of the submenus (the host disables them).</summary>
    public Action? DisableCustomDiffToolsAction { get; set; }

    /// <summary>Describes a revision for the captions of the difftool menu (<c>DescribeRevision</c>); the short hash by default.</summary>
    public Func<ObjectId, string>? DescribeRevision { get; set; }

    /// <summary>The caption "Second: B ..." of the difftool menu, if the selection has revisions (<c>tsmiSecondDiffCaption</c>).</summary>
    [ObservableProperty]
    public partial string? SecondDiffCaption { get; private set; }

    /// <summary>The caption "First: A ..." of the difftool menu (<c>tsmiFirstDiffCaption</c>).</summary>
    [ObservableProperty]
    public partial string? FirstDiffCaption { get; private set; }

    /// <summary>As <c>OpenWithDifftool_DropDownOpening</c>: the revisions of the selected files, or &lt;multiple&gt;.</summary>
    private void UpdateDifftoolCaptions()
    {
        List<GitRevision> seconds = [.. SelectedEntries.Select(entry => entry.SecondRevision).DistinctBy(revision => revision.ObjectId)];
        if (seconds.Count == 0)
        {
            SecondDiffCaption = null;
            FirstDiffCaption = null;
            return;
        }

        List<GitRevision> firsts = [.. SelectedEntries.Select(entry => entry.FirstRevision).OfType<GitRevision>().DistinctBy(revision => revision.ObjectId)];
        SecondDiffCaption = MenuStrings.SelectedRevision.Text + (Describe(seconds) ?? "");
        FirstDiffCaption = MenuStrings.FirstRevision.Text + (Describe(firsts) ?? "");

        string? Describe(List<GitRevision> revisions)
            => revisions.Count switch
            {
                1 => DescribeRevision?.Invoke(revisions[0].ObjectId) ?? revisions[0].ObjectId.ToShortString(),
                > 1 => MenuStrings.MultipleDescription.Text,
                _ => null,
            };
    }

    [RelayCommand]
    private void OpenWithCustomDifftool(DifftoolChoice choice) => MenuHost?.OpenWithDifftool(SelectedEntries, choice.Kind, choice.Tool);

    [RelayCommand]
    private void DiffWithRememberedCustom(string tool)
    {
        if (SelectedEntry is { } entry)
        {
            MenuHost?.DiffWithRemembered(entry, tool);
        }
    }

    [RelayCommand]
    private void DiffTwoSelectedCustom(string tool) => MenuHost?.DiffTwoSelected(SelectedEntries, FocusedEntry, tool);

    [RelayCommand]
    private void DisableCustomDiffTools() => DisableCustomDiffToolsAction?.Invoke();

    /// <summary>The refresh button: the artificial commits are read again (<c>RequestRefresh</c>).</summary>
    [RelayCommand]
    private void Refresh() => RefreshRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>As <c>EditGitIgnore_Click</c> and <c>EditLocallyIgnoredFiles_Click</c>.</summary>
    [RelayCommand]
    private void EditGitIgnore(bool localExcludes)
    {
        if (MenuHost?.EditGitIgnore(localExcludes) == true)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void ToggleDenseTree()
    {
        AppSettings.FileStatusMergeSingleItemWithFolder.Value = !Options.MergeSingleItemsWithFolder;
        Options = Options with { MergeSingleItemsWithFolder = !Options.MergeSingleItemsWithFolder };
    }

    [RelayCommand]
    private void ToggleShowGroupNodesInFlatList()
    {
        AppSettings.FileStatusShowGroupNodesInFlatList.Value = !Options.ShowGroupNodesInFlatList;
        Options = Options with { ShowGroupNodesInFlatList = !Options.ShowGroupNodesInFlatList };
    }

    /// <summary>
    ///  As <c>CollapseGroups_Click</c>: collapses the diff groups and the groups of the grouping, and selects the group of the
    ///  focused file; if none was expanded, expands the selected group instead.
    /// </summary>
    [RelayCommand]
    private void CollapseGroups()
    {
        bool isGroup(FileStatusNode node) => node.Group is not null || (node.Entry is null && node.FolderPath is null);
        bool collapsed = false;
        foreach (FileStatusNode node in Nodes)
        {
            IEnumerable<FileStatusNode> groups = node.Group is not null ? [.. node.Children.Where(isGroup), node] : isGroup(node) ? [node] : [];
            foreach (FileStatusNode group in groups.Where(group => group.IsExpanded))
            {
                group.IsExpanded = false;
                collapsed = true;
            }
        }

        if (collapsed)
        {
            FileStatusNode? focused = SelectedNodes.Count > 0 ? SelectedNodes[^1] : null;
            while (focused?.Parent is not null)
            {
                focused = focused.Parent;
            }

            if ((focused ?? Nodes.FirstOrDefault()) is { } root)
            {
                SetSelection([root]);
            }
        }
        else if (SelectedNodes.Count > 0)
        {
            SelectedNodes[^1].IsExpanded = true;
        }
    }

    /// <summary>As <c>SelectAll_Click</c>: the files below the selected nodes, which are expanded.</summary>
    [RelayCommand]
    private void SelectAll()
    {
        List<FileStatusNode> files = [];
        foreach (FileStatusNode node in SelectedNodes.ToList())
        {
            node.ExpandAll();
            files.AddRange(node.DescendantsAndSelf().Where(descendant => descendant.Entry is not null && descendant.Entry.Item != _noItemStatus));
        }

        SetSelection([.. files.Distinct()]);
    }

    /// <summary>As <c>CollapseRootFolders_Click</c>: the root of the selection is selected and the roots are collapsed.</summary>
    [RelayCommand]
    private void CollapseRootFolders()
    {
        if (SelectedNodes.Count > 0 && SelectedNodes[^1].Parent is { } parent)
        {
            while (parent.Parent is not null)
            {
                parent = parent.Parent;
            }

            SetSelection([parent]);
        }

        foreach (FileStatusNode node in Nodes)
        {
            node.IsExpanded = false;
        }
    }

    partial void OnGitGrepTextChanged(string value)
    {
        OnPropertyChanged(nameof(IsGitGrepActive));

        // As cboFindInCommitFilesGitGrep_TextUpdate: a delay for the keys typed.
        StartGitGrep(value, _searchingWithoutTyping ? 0 : GitGrepDelayMilliseconds, addToHistory: _searchingWithoutTyping);
    }

    partial void OnShowUnequalChangeChanged(bool value) => Update(updateCausedByFilter: true);

    partial void OnShowOnlyBChanged(bool value) => Update(updateCausedByFilter: true);

    partial void OnShowOnlyAChanged(bool value) => Update(updateCausedByFilter: true);

    partial void OnShowSameChangeChanged(bool value) => Update(updateCausedByFilter: true);

    partial void OnShowIgnoredFilesChanged(bool value) => RefreshRequested?.Invoke(this, EventArgs.Empty);

    partial void OnShowSkipWorktreeFilesChanged(bool value) => RefreshRequested?.Invoke(this, EventArgs.Empty);

    partial void OnShowAssumeUnchangedFilesChanged(bool value) => RefreshRequested?.Invoke(this, EventArgs.Empty);

    partial void OnShowUntrackedFilesChanged(bool value) => RefreshRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>The groups shown: those of the diffs and the git grep results, or only these in a searched file tree.</summary>
    private IReadOnlyList<FileStatusGroup> ShownGroups
        => _gitGrepGroup is not { } grep ? _groups
            : IsFileTreeMode ? [grep]
            : [.. _groups, grep];

    /// <summary>As the body of <c>FindInCommitFilesGitGrep</c>: git grep for the revision of the list, then the list is shown again.</summary>
    /// <param name="addToHistory">Whether the expression is added to the history: not while it is typed, as the history would change the text of the box.</param>
    private void StartGitGrep(string search, int delayMilliseconds, bool addToHistory = false)
    {
        if (GitGrep is not { } gitGrep)
        {
            return;
        }

        _gitGrepLoading?.Cancel();
        CancellationTokenSource loading = new();
        _gitGrepLoading = loading;
        CancellationToken cancellationToken = loading.Token;

        // The revision of the list: the second revision of its diffs (the selected commit), as the calculator's selectedRev.
        if (_groups.FirstOrDefault(group => !FileStatusTreeBuilder.IsGrepItemStatuses(group))?.SecondRevision is not { } revision)
        {
            _gitGrepGroup = null;
            return;
        }

        string grepArguments = search;
        if (!string.IsNullOrWhiteSpace(grepArguments) && !GrepStringRegex.IsMatch(grepArguments))
        {
            grepArguments = $@"-e ""{grepArguments.Replace(@"\\", @"\\\\")}""";
        }

        bool fileTreeMode = IsFileTreeMode && string.IsNullOrWhiteSpace(grepArguments);
        _ = RunAsync();
        return;

        async Task RunAsync()
        {
            FileStatusGroup? group;
            try
            {
                if (delayMilliseconds > 0)
                {
                    await Task.Delay(delayMilliseconds, cancellationToken);
                }

                // Without a search, no git grep group (the file tree shows its own files).
                group = string.IsNullOrWhiteSpace(grepArguments) ? null : await gitGrep(revision, grepArguments, fileTreeMode, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _gitGrepGroup = group;
            Update(updateCausedByFilter: false);
            await WaitForTreeAsync();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (SelectedNodes.Count == 0)
            {
                SelectFirstVisibleItem();
            }

            if (addToHistory)
            {
                AddGitGrepHistory(search);
            }
        }
    }

    /// <summary>As <c>AddToSearchFilter</c>: the expression first in the history (when the box is left or Enter pressed).</summary>
    public void AddGitGrepHistory(string expression)
    {
        if (string.IsNullOrEmpty(expression) || (GitGrepHistory.Count > 0 && GitGrepHistory[0] == expression))
        {
            return;
        }

        GitGrepHistory.Remove(expression);
        GitGrepHistory.Insert(0, expression);
        OnPropertyChanged(nameof(HasGitGrepHistory));
    }
}
