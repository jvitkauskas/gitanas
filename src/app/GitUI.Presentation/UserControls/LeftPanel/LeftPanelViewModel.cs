using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.Git;
using GitCommands.Utils;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.LeftPanel;

/// <summary>
///  View model of the left panel of the main window (port of <c>RepoObjectsTree</c>; docs/avalonia-port/PLAN.md, phase 7):
///  the trees of branches, remotes, worktrees, tags, submodules and stashes, beside the revision grid. The trees load in the
///  background when the grid loads, keeping their expanded nodes and the selection; a selected node selects its revision in
///  the grid.
/// </summary>
public sealed partial class LeftPanelViewModel : ObservableObject, IDisposable
{
    // As NativeTreeViewExplorerNavigationDecorator: a selection this soon after an arrow key does not select the revision.
    private const long KeyboardNavigationDelay = 500;

    private readonly Dictionary<LeftPanelTreeKind, LeftPanelTree> _trees;
    private readonly bool _initialized;
    private int _selectingSilently;
    private long _keyboardNavigationTime;
    private Lazy<IReadOnlyList<IGitRef>>? _refs;
    private CancellationTokenSource? _loadingMergedBranches;
    private List<LeftPanelNode>? _searchResult;
    private bool _searchCriteriaChanged;
    private bool _disposed;

    public LeftPanelViewModel(LeftPanelStrings strings, ILeftPanelHost host, ILeftPanelSettings settings, RevisionGridViewModel grid)
    {
        Strings = strings;
        Host = host;
        Settings = settings;
        Grid = grid;

        BranchesTree = new LocalBranchTree(this);
        RemotesTree = new RemoteBranchTree(this);
        WorktreesTree = new WorktreeTree(this);
        TagsTree = new TagTree(this);
        SubmodulesTree = new SubmoduleTree(this);
        StashesTree = new StashTree(this);
        _trees = new()
        {
            [LeftPanelTreeKind.Branches] = BranchesTree,
            [LeftPanelTreeKind.Remotes] = RemotesTree,
            [LeftPanelTreeKind.Worktrees] = WorktreesTree,
            [LeftPanelTreeKind.Tags] = TagsTree,
            [LeftPanelTreeKind.Submodules] = SubmodulesTree,
            [LeftPanelTreeKind.Stashes] = StashesTree,
        };

        FixInvalidTreeToPositionIndices();
        ShowBranches = settings.IsShown(LeftPanelTreeKind.Branches);
        ShowRemotes = settings.IsShown(LeftPanelTreeKind.Remotes);
        ShowWorktrees = settings.IsShown(LeftPanelTreeKind.Worktrees);
        ShowTags = settings.IsShown(LeftPanelTreeKind.Tags);
        ShowSubmodules = settings.IsShown(LeftPanelTreeKind.Submodules);
        ShowStashes = settings.IsShown(LeftPanelTreeKind.Stashes);
        ShowEnabledTrees();
        _initialized = true;

        // As FormBrowse: the trees are refreshed when the grid starts loading (RefreshRevisionsLoading), the visibility of
        // the references once it is loaded (RefreshRevisionsLoaded), the merged branches with the selection.
        grid.PropertyChanged += OnGridPropertyChanged;
        grid.SelectionChanged += OnGridSelectionChanged;
        host.SubmodulesUpdated += OnSubmodulesUpdated;
    }

    public LeftPanelStrings Strings { get; }

    internal ILeftPanelHost Host { get; }

    internal ILeftPanelSettings Settings { get; }

    public RevisionGridViewModel Grid { get; }

    public LocalBranchTree BranchesTree { get; }

    public RemoteBranchTree RemotesTree { get; }

    public WorktreeTree WorktreesTree { get; }

    public TagTree TagsTree { get; }

    public SubmoduleTree SubmodulesTree { get; }

    public StashTree StashesTree { get; }

    /// <summary>The shown trees, in their order (the root nodes of the tree view).</summary>
    public ObservableCollection<LeftPanelTree> Trees { get; } = [];

    /// <summary>The selected node of the tree view (<c>treeMain.SelectedNode</c>).</summary>
    [ObservableProperty]
    public partial LeftPanelNode? SelectedNode { get; set; }

    /// <summary>Whether the panel is shown (<c>toggleLeftPanel</c>); a hidden panel is not refreshed.</summary>
    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowBranches { get; set; }

    [ObservableProperty]
    public partial bool ShowRemotes { get; set; }

    [ObservableProperty]
    public partial bool ShowWorktrees { get; set; }

    [ObservableProperty]
    public partial bool ShowTags { get; set; }

    [ObservableProperty]
    public partial bool ShowSubmodules { get; set; }

    [ObservableProperty]
    public partial bool ShowStashes { get; set; }

    /// <summary>The text of the search box (<c>txtBranchCriterion</c>).</summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    /// <summary>The suggestions of the search box: the full paths of the references and the texts of the other nodes.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> SearchCandidates { get; private set; } = [];

    public IReadOnlyList<HotkeyBinding> Hotkeys => Host.Hotkeys;

    /// <summary>All the trees, shown or not.</summary>
    public IEnumerable<LeftPanelTree> AllTrees => _trees.Values;

    /// <summary>As <c>RefreshRevisionsLoading</c>: reloads the shown trees (e.g. when the grid is refreshed).</summary>
    public Task RefreshAsync()
    {
        if (!IsVisible || _disposed)
        {
            return Task.CompletedTask;
        }

        _refs = new Lazy<IReadOnlyList<IGitRef>>(Host.GetRefs, LazyThreadSafetyMode.ExecutionAndPublication);
        return Task.WhenAll(Trees.ToList().Select(tree => tree.ReloadAsync()));
    }

    internal IReadOnlyList<IGitRef> GetAllRefs() => (_refs ??= new Lazy<IReadOnlyList<IGitRef>>(Host.GetRefs, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    /// <summary>As <c>ClickNode</c> / <c>SelectNode</c>: the multi-selection (underlined nodes) of a click on a node.</summary>
    /// <param name="multiple">Toggles the node in the multi-selection (a click with Ctrl), else only the node is selected.</param>
    /// <param name="includingDescendants">Toggles the descendants too (with Shift).</param>
    /// <param name="rightButton">A right click does not change the multi-selection of a selected node (it opens the menu).</param>
    public void ClickNode(LeftPanelNode node, bool multiple, bool includingDescendants, bool rightButton = false)
    {
        if (rightButton && node.IsMultiSelected)
        {
            return;
        }

        if (multiple)
        {
            Select(node, !node.IsMultiSelected, includingDescendants);
            return;
        }

        foreach (LeftPanelNode selected in GetMultiSelectedNodes().ToList())
        {
            selected.IsMultiSelected = false;
        }

        node.IsMultiSelected = true;

        static void Select(LeftPanelNode node, bool select, bool includingDescendants)
        {
            node.IsMultiSelected = select;
            if (includingDescendants)
            {
                foreach (LeftPanelNode child in node.Children)
                {
                    Select(child, select, includingDescendants);
                }
            }
        }
    }

    /// <summary>
    ///  As <c>NativeTreeViewExplorerNavigationDecorator.OnNodeMouseClick</c>: a clicked node is selected, and its revision
    ///  selected in the grid again if the node was selected already.
    /// </summary>
    /// <param name="alternate">
    ///  A click with Alt: the related branch of a branch (its tracked or tracking branch) is selected in the grid (as
    ///  <c>BaseBranchLeafNode.SelectRevision</c>).
    /// </param>
    public void SelectByClick(LeftPanelNode node, bool alternate = false)
    {
        _keyboardNavigationTime = 0;
        IsAlternateSelection = alternate;
        try
        {
            if (SelectedNode == node)
            {
                OnSelectedNodeChanged(node);
            }
            else
            {
                SelectedNode = node;
            }
        }
        finally
        {
            IsAlternateSelection = false;
        }
    }

    /// <summary>Whether the node is selected with Alt (<c>Control.ModifierKeys.HasFlag(Keys.Alt)</c>).</summary>
    internal bool IsAlternateSelection { get; private set; }

    /// <summary>
    ///  As the explorer navigation of the WinForms tree: the arrow keys move the selection without selecting the revisions in
    ///  the grid (for a moment); Space and Enter then select it (<see cref="ActivateSelectedNode"/>).
    /// </summary>
    public void NavigateByKeyboard() => _keyboardNavigationTime = Environment.TickCount64;

    /// <summary>Selects the revision of the selected node in the grid (Space or Enter in the tree).</summary>
    public void ActivateSelectedNode()
    {
        _keyboardNavigationTime = 0;
        OnSelectedNodeChanged(SelectedNode);
    }

    /// <summary>As <c>OnNodeDoubleClick</c>: e.g. checks out a branch, opens a stash or a submodule.</summary>
    public void DoubleClickNode(LeftPanelNode? node) => node?.OnDoubleClick();

    /// <summary>As <c>SelectGitRef</c> (<c>SelectInLeftPanel</c> of the grid): selects the node of a reference.</summary>
    public bool SelectGitRef(string gitRef)
    {
        if (Trees.SelectMany(tree => tree.Descendants()).OfType<LeftPanelRevisionNode>().FirstOrDefault(node => node.FullPath == gitRef) is not { } node)
        {
            return false;
        }

        ClickNode(node, multiple: false, includingDescendants: false);
        ExpandPathTo(node);
        SelectedNode = node;
        return true;
    }

    /// <summary>
    ///  As <c>SelectInLeftPanel</c> of FormBrowse (the "select in left panel" of the grid's menu): the panel is shown, the node
    ///  of the reference (e.g. <c>main</c>, <c>origin/main</c> or <c>v1.0</c>) selected and focused.
    /// </summary>
    public void SelectInLeftPanel(string gitRef)
    {
        IsVisible = true;
        SelectGitRef(gitRef);
        FocusRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when the tree should get the focus.</summary>
    public event EventHandler? FocusRequested;

    /// <summary>The multi-selected (underlined) nodes of the shown trees, the trees included (<c>GetSelectedNodes</c>).</summary>
    public IEnumerable<LeftPanelNode> GetMultiSelectedNodes() => Trees.SelectMany(tree => tree.SelfAndDescendants()).Where(node => node.IsMultiSelected);

    /// <summary>As <c>ExecuteCommand</c>: the hotkeys of the left panel.</summary>
    public bool ExecuteHotkey(LeftPanelHotkeyCommand command)
    {
        switch (command)
        {
            case LeftPanelHotkeyCommand.Delete:
                SelectedNode?.OnDelete();
                return true;
            case LeftPanelHotkeyCommand.Rename:
                SelectedNode?.OnRename();
                return true;
            case LeftPanelHotkeyCommand.Search:
                Search();
                return true;
            case LeftPanelHotkeyCommand.MultiSelect:
            case LeftPanelHotkeyCommand.MultiSelectWithChildren:
                if (SelectedNode is { } node)
                {
                    ClickNode(node, multiple: true, includingDescendants: command == LeftPanelHotkeyCommand.MultiSelectWithChildren);
                }

                return true;
            default:
                return false;
        }
    }

    /// <summary>As <c>btnCollapseAll_Click</c>: collapses all nodes.</summary>
    [RelayCommand]
    public void CollapseAll()
    {
        foreach (LeftPanelNode node in Trees.SelectMany(tree => tree.SelfAndDescendants()))
        {
            node.IsExpanded = false;
        }
    }

    /// <summary>
    ///  As <c>DoSearch</c>: the nodes containing the text are highlighted, and each search selects the next one; a changed text
    ///  starts a new search.
    /// </summary>
    [RelayCommand]
    public void Search()
    {
        if (_searchCriteriaChanged && _searchResult?.Count is > 0)
        {
            _searchCriteriaChanged = false;
            ClearSearchResult();
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return;
            }
        }

        if (_searchResult is not { Count: > 0 } && !string.IsNullOrWhiteSpace(SearchText))
        {
            _searchCriteriaChanged = false;
            _searchResult = SearchTree(SearchText);
        }

        if (_searchResult is not [LeftPanelNode first, ..])
        {
            return;
        }

        _searchResult.RemoveAt(0);
        _searchResult.Add(first);
        ExpandPathTo(first);
        SelectedNode = first;

        List<LeftPanelNode> SearchTree(string text)
        {
            Queue<LeftPanelNode> queue = new(Trees);
            List<LeftPanelNode> result = [];
            while (queue.Count != 0)
            {
                LeftPanelNode node = queue.Dequeue();
                string searched = node is LeftPanelRevisionNode revisionNode ? revisionNode.FullPath : node.Text;
                if (searched.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                {
                    node.IsSearchMatch = true;
                    result.Add(node);
                }

                foreach (LeftPanelNode child in node.Children)
                {
                    queue.Enqueue(child);
                }
            }

            return result;
        }
    }

    /// <summary>As <c>ReorderTreeNode</c>: swaps the tree with the previous (or next) shown tree.</summary>
    public void MoveTree(LeftPanelTree tree, bool up)
    {
        Dictionary<LeftPanelTree, int> treeToIndex = GetTreeToPositionIndex();
        Dictionary<int, LeftPanelTree> indexToTree = treeToIndex.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        int currentIndex = treeToIndex[tree];
        int swapIndex = currentIndex;
        do
        {
            swapIndex = up ? swapIndex - 1 : swapIndex + 1;
            if (swapIndex < 0 || swapIndex >= treeToIndex.Count)
            {
                return;
            }
        }
        while (!indexToTree[swapIndex].IsAttached);

        LeftPanelTree swapWithTree = indexToTree[swapIndex];
        treeToIndex[tree] = swapIndex;
        treeToIndex[swapWithTree] = currentIndex;
        SaveTreeToPositionIndex(treeToIndex);
        SortTrees();
    }

    /// <summary>The items of the context menu for the selection (as <c>contextMenu_Opening</c>); empty if none applies.</summary>
    public IReadOnlyList<LeftPanelMenuItem> GetContextMenu()
    {
        LeftPanelNode[] selectedNodes = [.. GetMultiSelectedNodes()];
        bool single = selectedNodes.Length == 1;
        LeftPanelNode? node = SelectedNode;
        LeftPanelStrings s = Strings;
        List<LeftPanelMenuItem> items = [];

        if (single && node is LeftPanelBranchNode or StashNode && ((LeftPanelRevisionNode)node).Visible
            && CreateCopyItems() is { Count: > 0 } copyItems)
        {
            items.Add(new(s.CopyToClipboard.AccessKeyText, Icon: "CopyToClipboard", Children: copyItems));
        }

        if (selectedNodes.Any(IsRefNode))
        {
            items.Add(new(s.FilterForSelected.AccessKeyText, FilterForSelected, "ShowThisBranchOnly", s.FilterForSelectedToolTip.Text));
        }

        // The grid has no View > Show all branches yet: the filter is reset here.
        if (Host.IsBranchFilterActive)
        {
            items.Add(new(s.ShowAllBranches.Text, () => Host.FilterRevisionGrid(null), "BranchLocal"));
        }

        if (single && node is LocalBranchNode localBranch)
        {
            // The current branch can be renamed and branched from.
            bool notCurrent = !localBranch.IsCurrent;
            items.Add(LeftPanelMenuItem.Separator);
            items.Add(Item(s.BranchCheckout, LeftPanelAction.CheckoutBranch, "BranchCheckout", s.BranchCheckoutToolTip, notCurrent));
            items.Add(Item(s.Merge, LeftPanelAction.MergeBranch, "Merge", s.BranchMergeToolTip, notCurrent));
            items.Add(Item(s.BranchRebase, LeftPanelAction.RebaseOnBranch, "Rebase", s.BranchRebaseToolTip, notCurrent));
            items.Add(Item(s.CreateBranch, LeftPanelAction.CreateBranchFromBranch, "Branch", s.BranchCreateToolTip));
            items.Add(Item(s.Reset, LeftPanelAction.ResetToBranch, "ResetCurrentBranchToHere", s.BranchResetToolTip, notCurrent));
            items.Add(LeftPanelMenuItem.Separator);
            items.Add(Item(s.Rename, LeftPanelAction.RenameBranch, "Renamed", s.BranchRenameToolTip));
            items.Add(Item(s.BranchDelete, LeftPanelAction.DeleteBranch, "BranchDelete", s.LocalBranchDeleteToolTip, notCurrent));
        }

        if (single && node is RemoteBranchNode)
        {
            items.Add(LeftPanelMenuItem.Separator);
            items.Add(Item(s.RemoteBranchCheckout, LeftPanelAction.CheckoutRemoteBranch, "BranchCheckout", s.BranchCheckoutToolTip));
            items.Add(Item(s.Merge, LeftPanelAction.MergeRemoteBranch, "Merge", s.BranchMergeToolTip));
            items.Add(Item(s.RemoteBranchRebase, LeftPanelAction.RebaseOnRemoteBranch, "Rebase", s.BranchRebaseToolTip));
            items.Add(Item(s.CreateBranch, LeftPanelAction.CreateBranchFromRemoteBranch, "Branch", s.BranchCreateToolTip));
            items.Add(Item(s.Reset, LeftPanelAction.ResetToRemoteBranch, "ResetCurrentBranchToHere", s.BranchResetToolTip));
            items.Add(LeftPanelMenuItem.Separator);
            items.Add(Item(s.RemoteBranchDelete, LeftPanelAction.DeleteRemoteBranch, "BranchDelete", s.RemoteBranchDeleteToolTip));
        }

        if (single && node is TagNode)
        {
            items.Add(LeftPanelMenuItem.Separator);
            items.Add(Item(s.TagCheckout, LeftPanelAction.CheckoutTag, "BranchCheckout", s.TagCheckoutToolTip));
            items.Add(Item(s.Merge, LeftPanelAction.MergeTag, "Merge", s.TagMergeToolTip));
            items.Add(Item(s.TagRebase, LeftPanelAction.RebaseOnTag, "Rebase", s.TagRebaseToolTip));
            items.Add(Item(s.CreateBranch, LeftPanelAction.CreateBranchFromTag, "Branch", s.TagCreateToolTip));
            items.Add(Item(s.Reset, LeftPanelAction.ResetToTag, "ResetCurrentBranchToHere", s.TagResetToolTip));
            items.Add(LeftPanelMenuItem.Separator);
            items.Add(Item(s.TagDelete, LeftPanelAction.DeleteTag, "BranchDelete", s.TagDeleteToolTip));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is RemoteBranchTree)
        {
            items.Add(Item(s.ManageRemotesFromRoot, LeftPanelAction.ManageRemotes, "Remotes", s.ManageRemotesFromRootToolTip));
            items.Add(Item(s.FetchAllRemotes, LeftPanelAction.FetchAllRemotes, "PullFetchAll"));
            items.Add(Item(s.FetchAndPruneAllRemotes, LeftPanelAction.FetchAndPruneAllRemotes, "PullFetchPruneAll"));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is RemoteRepoNode remote)
        {
            items.Add(Item(s.ManageRemotes, LeftPanelAction.ManageRemotes, "Remotes", s.ManageRemotesToolTip));
            if (!remote.Enabled)
            {
                items.Add(Item(s.EnableRemote, LeftPanelAction.EnableRemote, "EyeOpened"));
                items.Add(Item(s.EnableRemoteAndFetch, LeftPanelAction.EnableRemoteAndFetch, "RemoteEnableAndFetch"));
            }
            else
            {
                items.Add(Item(s.DisableRemote, LeftPanelAction.DisableRemote, "EyeClosed"));
                items.Add(Item(s.FetchRemote, LeftPanelAction.FetchRemote, "PullFetch"));
                items.Add(Item(s.FetchAndPruneRemote, LeftPanelAction.FetchAndPruneRemote, "PullFetchPrune"));
            }

            if (remote.IsRemoteUrlUsingHttp)
            {
                items.Add(Item(s.OpenRemoteUrl, LeftPanelAction.OpenRemoteUrl, "Globe", s.OpenRemoteUrlToolTip));
            }
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is SubmoduleNode submodule)
        {
            bool isBare = Host.IsBareRepository;
            if (!submodule.IsCurrent)
            {
                items.Add(Item(s.OpenSubmodule, LeftPanelAction.OpenSubmodule, "FolderOpen", s.OpenSubmoduleToolTip));
            }

            items.Add(Item(s.OpenSubmoduleInNewInstance, LeftPanelAction.OpenSubmoduleInNewInstance, "GitanasLogo16", s.OpenSubmoduleInNewInstanceToolTip));
            if (!isBare && submodule.IsCurrent)
            {
                items.Add(Item(s.ManageSubmodules, LeftPanelAction.ManageSubmodules, "SubmodulesManage", s.ManageSubmodulesToolTip));
            }

            items.Add(Item(s.UpdateSubmodule, LeftPanelAction.UpdateSubmodule, "SubmodulesUpdate", s.UpdateSubmoduleToolTip));
            if (!isBare && submodule.IsCurrent)
            {
                items.Add(Item(s.SynchronizeSubmodules, LeftPanelAction.SynchronizeSubmodules, "SubmodulesSync", s.SynchronizeSubmodulesToolTip));
            }

            if (!isBare)
            {
                items.Add(Item(s.ResetSubmodule, LeftPanelAction.ResetSubmodule, "ResetWorkingDirChanges", s.ResetSubmoduleToolTip));
                items.Add(Item(s.StashSubmodule, LeftPanelAction.StashSubmodule, "Stash", s.StashSubmoduleToolTip));
                items.Add(Item(s.CommitSubmodule, LeftPanelAction.CommitSubmodule, "RepoStateDirtySubmodules", s.CommitSubmoduleToolTip));
            }
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is RemoteBranchNode)
        {
            items.Add(Item(s.FetchAndCheckout, LeftPanelAction.FetchAndCheckoutRemoteBranch, "BranchCheckout", s.FetchAndCheckoutToolTip));
            items.Add(Item(s.FetchAndMerge, LeftPanelAction.FetchAndMergeRemoteBranch, "Pull", s.FetchAndMergeToolTip));
            items.Add(Item(s.FetchAndRebase, LeftPanelAction.FetchAndRebaseOnRemoteBranch, "Rebase", s.FetchAndRebaseToolTip));
            items.Add(Item(s.FetchAndCreateBranch, LeftPanelAction.FetchAndCreateBranchFromRemoteBranch, "Branch", s.FetchAndCreateBranchToolTip));
            items.Add(Item(s.FetchBranch, LeftPanelAction.FetchRemoteBranch, "Stage", s.FetchBranchToolTip));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is BranchPathNode)
        {
            items.Add(Item(s.CreateBranchInFolder, LeftPanelAction.CreateBranchInFolder, "BranchCreate", s.CreateBranchInFolderToolTip));
            items.Add(Item(s.DeleteAllBranches, LeftPanelAction.DeleteAllBranchesInFolder, "BranchDelete", s.DeleteAllBranchesToolTip));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is StashTree)
        {
            items.Add(Item(s.StashAll, LeftPanelAction.StashAll));
            items.Add(Item(s.StashStaged, LeftPanelAction.StashStaged));
            items.Add(Item(s.ManageStashes, LeftPanelAction.ManageStashes));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is StashNode && !Host.IsBareRepository)
        {
            items.Add(Item(s.OpenStash, LeftPanelAction.OpenStash, toolTip: s.OpenStashToolTip));
            items.Add(Item(s.ApplyStash, LeftPanelAction.ApplyStash, toolTip: s.ApplyStashToolTip));
            items.Add(Item(s.PopStash, LeftPanelAction.PopStash, toolTip: s.PopStashToolTip));
            items.Add(Item(s.DropStash, LeftPanelAction.DropStash, toolTip: s.DropStashToolTip));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is WorktreeTree)
        {
            items.Add(Item(s.CreateWorktree, LeftPanelAction.CreateWorktree));
            items.Add(Item(s.PruneWorktrees, LeftPanelAction.PruneWorktrees));
            items.Add(Item(s.ManageWorktrees, LeftPanelAction.ManageWorktrees));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is WorktreeNode worktree)
        {
            // Shown for any worktree, but the current and deleted ones cannot be opened or deleted, nor the main one deleted.
            bool canActOnWorktree = worktree is { IsCurrent: false, Worktree.IsDeleted: false };
            items.Add(Item(s.OpenWorktree, LeftPanelAction.OpenWorktree, "FolderOpen", s.OpenWorktreeToolTip, canActOnWorktree));
            items.Add(Item(s.DeleteWorktree, LeftPanelAction.DeleteWorktree, toolTip: s.DeleteWorktreeToolTip, isEnabled: canActOnWorktree && !worktree.Worktree.IsMain));
            items.Add(LeftPanelMenuItem.Separator);
            items.Add(Item(s.CopyWorktreePath, LeftPanelAction.CopyWorktreePath, "CopyToClipboard", s.CopyWorktreePathToolTip));
            items.Add(Item(s.ShowWorktreeInFolder, LeftPanelAction.ShowWorktreeInFolder, "BrowseFileExplorer", s.ShowWorktreeInFolderToolTip, Host.DirectoryExists(worktree.Worktree.Path)));
        }

        items.Add(LeftPanelMenuItem.Separator);
        LeftPanelNode[] parents = [.. selectedNodes.Where(n => n.HasChildren)];
        if (parents.Length > 0)
        {
            items.Add(new(s.Collapse.AccessKeyText, () => Collapse(parents), "CollapseAll", s.CollapseToolTip.Text, parents.Any(n => n.IsExpanded)));
            items.Add(new(s.Expand.AccessKeyText, () => ExpandAll(parents), "ExpandAll", s.ExpandToolTip.Text, parents.Any(n => !n.IsExpanded)));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is LeftPanelTree tree)
        {
            int index = Trees.IndexOf(tree);
            items.Add(new(s.MoveUp.AccessKeyText, () => MoveTree(tree, up: true), "ArrowUp", s.MoveUpToolTip.Text, index > 0));
            items.Add(new(s.MoveDown.AccessKeyText, () => MoveTree(tree, up: false), "ArrowDown", s.MoveDownToolTip.Text, index >= 0 && index < Trees.Count - 1));
        }

        items.Add(LeftPanelMenuItem.Separator);
        if (single && node is not null && IsRefNode(node))
        {
            items.Add(new(s.SortBy.AccessKeyText, Icon: "SortBy", Children:
                [.. Enum.GetValues<GitRefsSortBy>().Select(value => new LeftPanelMenuItem(
                    TranslatedText.ToAccessKeyText(value.GetDescription()), () => SetRefsSortBy(value), IsChecked: Settings.RefsSortBy == value))]));

            // Refs sorted by git (Default) have no order.
            if (Settings.RefsSortBy != GitRefsSortBy.Default)
            {
                items.Add(new(s.SortOrder.AccessKeyText, Icon: "SortBy", Children:
                    [.. Enum.GetValues<GitRefsSortOrder>().Select(value => new LeftPanelMenuItem(
                        TranslatedText.ToAccessKeyText(value.GetDescription()), () => SetRefsSortOrder(value), IsChecked: Settings.RefsSortOrder == value))]));
            }
        }

        // As AddUserScripts for a visible local branch: "Run script" with the scripts not in the grid's menu, then the others.
        if (single && node is LocalBranchNode { Visible: true } && Host.GetScripts() is { Count: > 0 } scripts)
        {
            items.Add(LeftPanelMenuItem.Separator);
            List<LeftPanelMenuItem> others = [.. scripts.Where(script => !script.IsDirect).Select(ScriptItem)];
            if (others.Count > 0)
            {
                items.Add(new(s.RunScript.AccessKeyText, Icon: "Console", Children: others));
            }

            items.AddRange(scripts.Where(script => script.IsDirect).Select(ScriptItem));

            LeftPanelMenuItem ScriptItem(LeftPanelScript script) => new(script.Name, () => Host.RunScript(script.Id), Image: script.Icon);
        }

        // Without enabled items, the menu does not open.
        List<LeftPanelMenuItem> result = ToggleSeparators(items);
        return result.Any(item => !item.IsSeparator && item.IsEnabled) ? result : [];

        LeftPanelMenuItem Item(TranslatedText text, LeftPanelAction action, string? icon = null, TranslatedText? toolTip = null, bool isEnabled = true)
        {
            LeftPanelNode? target = node;
            return new(text.AccessKeyText, () => Run(action, target), icon, toolTip?.Text, isEnabled);
        }
    }

    /// <summary>Runs an operation of the node (or of the tree) through the host.</summary>
    internal bool Run(LeftPanelAction action, LeftPanelNode? node)
    {
        bool done = Host.Run(action, node);
        if (done && action is LeftPanelAction.CreateWorktree or LeftPanelAction.DeleteWorktree)
        {
            _ = WorktreesTree.ReloadAsync();
        }

        return done;
    }

    /// <summary>As <c>GoToRevision(string)</c>: the revision of a reference (its complete or short name) in the grid.</summary>
    internal void GoToRef(string gitRef)
    {
        if ((GetAllRefs().FirstOrDefault(r => r.CompleteName == gitRef) ?? GetAllRefs().FirstOrDefault(r => r.Name == gitRef)) is { ObjectId: { } objectId })
        {
            GoToRevision(objectId);
        }
    }

    /// <summary>Selects the revision in the grid; tells if it is not there (as <c>GoToRef</c>).</summary>
    internal void GoToRevision(ObjectId objectId)
    {
        if (!Grid.SelectRevision(objectId) && !Grid.IsLoading)
        {
            Host.ShowRevisionNotInGrid(objectId);
        }
    }

    internal void SetSelectedNodeSilently(LeftPanelNode? node)
    {
        _selectingSilently++;
        try
        {
            SelectedNode = node;
        }
        finally
        {
            _selectingSilently--;
        }
    }

    internal void ReportSubmoduleDirectoryMissing(string directory, string submoduleName) => Host.ShowSubmoduleDirectoryMissing(directory, submoduleName);

    /// <summary>
    ///  As <c>FillTreeViewNode</c>: shows the loaded nodes of the tree, keeping the expanded nodes (except the first time), the
    ///  selected node (unless its revision is hidden) and the multi-selection.
    /// </summary>
    internal void Fill(LeftPanelTree tree, IReadOnlyList<LeftPanelNode> nodes)
    {
        bool firstTime = tree.IsFirstLoad;
        HashSet<string> expanded = firstTime ? [] : [.. tree.Descendants().Where(n => n.IsExpanded).Select(n => n.KeyPath)];
        HashSet<string> multiSelected = [.. tree.Descendants().Where(n => n.IsMultiSelected).Select(n => n.KeyPath)];
        string? selectedPath = SelectedNode is { } selected && selected.Tree == tree && selected != tree ? selected.KeyPath : null;
        _selectingSilently++;
        try
        {
            foreach (LeftPanelNode node in nodes)
            {
                node.Parent = tree;
            }

            foreach (LeftPanelNode node in nodes.SelectMany(n => n.SelfAndDescendants()))
            {
                node.ApplyTextAndStyle();
                node.IsExpanded = expanded.Contains(node.KeyPath);
                node.IsMultiSelected = multiSelected.Contains(node.KeyPath);
            }

            tree.Children = nodes;
            if (selectedPath is not null)
            {
                LeftPanelNode? node = tree.Descendants().FirstOrDefault(n => n.KeyPath == selectedPath);
                SelectedNode = node is LeftPanelRevisionNode { Visible: false } ? null : node;
            }

            tree.PostFill(firstTime);
            UpdateVisibility(tree);
        }
        finally
        {
            _selectingSilently--;
        }

        ExpandPathTo(SelectedNode);
        tree.IsFirstLoad = false;
        _searchResult = null;
        UpdateSearchCandidates();
    }

    public void Dispose()
    {
        _disposed = true;
        Grid.PropertyChanged -= OnGridPropertyChanged;
        Grid.SelectionChanged -= OnGridSelectionChanged;
        Host.SubmodulesUpdated -= OnSubmodulesUpdated;
        foreach (LeftPanelTree tree in _trees.Values)
        {
            tree.CancelLoading();
        }

#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loadingMergedBranches?.Cancel();
#pragma warning restore VSTHRD103
    }

    partial void OnSelectedNodeChanged(LeftPanelNode? value)
    {
        // As OnNodeSelected: the revision of the node is selected in the grid, unless it is hidden from the grid, or the
        // selection moved with the arrow keys.
        if (_selectingSilently > 0
            || value is null or LeftPanelRevisionNode { Visible: false }
            || Environment.TickCount64 - _keyboardNavigationTime < KeyboardNavigationDelay)
        {
            return;
        }

        value.OnSelected();
    }

    partial void OnIsVisibleChanged(bool value)
    {
        // As toggleLeftPanel_Click: the panel is refreshed when shown.
        if (value)
        {
            _ = RefreshAsync();
        }
    }

    partial void OnSearchTextChanged(string value) => _searchCriteriaChanged = true;

    partial void OnShowBranchesChanged(bool value) => SetTreeShown(LeftPanelTreeKind.Branches, value);

    partial void OnShowRemotesChanged(bool value) => SetTreeShown(LeftPanelTreeKind.Remotes, value);

    partial void OnShowWorktreesChanged(bool value) => SetTreeShown(LeftPanelTreeKind.Worktrees, value);

    partial void OnShowTagsChanged(bool value) => SetTreeShown(LeftPanelTreeKind.Tags, value);

    partial void OnShowSubmodulesChanged(bool value) => SetTreeShown(LeftPanelTreeKind.Submodules, value);

    partial void OnShowStashesChanged(bool value) => SetTreeShown(LeftPanelTreeKind.Stashes, value);

    private static bool IsRefNode(LeftPanelNode node) => node is LocalBranchNode or RemoteBranchNode or TagNode;

    private static void Collapse(IEnumerable<LeftPanelNode> nodes)
    {
        foreach (LeftPanelNode node in nodes)
        {
            node.IsExpanded = false;
        }
    }

    private static void ExpandAll(IEnumerable<LeftPanelNode> nodes)
    {
        foreach (LeftPanelNode node in nodes.SelectMany(n => n.SelfAndDescendants()).Where(n => n.HasChildren))
        {
            node.IsExpanded = true;
        }
    }

    private static void ExpandPathTo(LeftPanelNode? node)
    {
        for (LeftPanelNode? parent = node?.Parent; parent is not null; parent = parent.Parent)
        {
            parent.IsExpanded = true;
        }
    }

    /// <summary>As <c>ContextMenuExtensions.ToggleSeparators</c>: no leading, trailing or consecutive separators.</summary>
    private static List<LeftPanelMenuItem> ToggleSeparators(List<LeftPanelMenuItem> items)
    {
        List<LeftPanelMenuItem> result = [];
        foreach (LeftPanelMenuItem item in items)
        {
            if (!item.IsSeparator || (result.Count > 0 && !result[^1].IsSeparator))
            {
                result.Add(item);
            }
        }

        if (result.Count > 0 && result[^1].IsSeparator)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }

    private void FilterForSelected()
    {
        IEnumerable<string> refPaths = GetMultiSelectedNodes().Where(IsRefNode).Cast<LeftPanelRevisionNode>().Select(node => node.FullPath);
        Host.FilterRevisionGrid(string.Join(" ", refPaths));
    }

    /// <summary>As <c>CopyContextMenuItem</c>: the branches, tags, hash, message, author and dates of the selected revisions.</summary>
    private List<LeftPanelMenuItem> CreateCopyItems()
    {
        IReadOnlyList<GitRevision> revisions = Grid.GetSelectedRevisionsLatestSelectedFirst();
        List<LeftPanelMenuItem> items = [];
        if (revisions.Count == 0)
        {
            return items;
        }

        int itemNumber = 0;
        AddRefs(Strings.Branches.Text, [.. revisions.SelectMany(r => r.Refs).Where(r => r.IsHead || r.IsRemote).Select(r => r.Name)], "Branch");
        AddRefs(Strings.Tags.Text, [.. revisions.SelectMany(r => r.Refs).Where(r => r.IsTag).Select(r => r.Name)], "Tag");

        int count = revisions.Count;
        AddItem(ResourceManager.TranslatedStrings.GetCommitHash(count), r => r.Guid, "CommitId", 'C');
        AddItem(ResourceManager.TranslatedStrings.GetMessage(count), r => r.Body ?? r.Subject, "Message", 'M');
        AddItem(ResourceManager.TranslatedStrings.GetAuthor(count), r => $"{r.Author} <{r.AuthorEmail}>", "Author", 'A');
        if (count == 1 && revisions[0].AuthorDate == revisions[0].CommitDate)
        {
            AddItem(ResourceManager.TranslatedStrings.Date, r => r.AuthorDate.ToString(), "Date", 'D');
        }
        else
        {
            AddItem(ResourceManager.TranslatedStrings.GetAuthorDate(count), r => r.AuthorDate.ToString(), "Date", 'T');
            AddItem(ResourceManager.TranslatedStrings.GetCommitDate(count), r => r.CommitDate.ToString(), "Date", 'D');
        }

        return items;

        void AddRefs(string caption, IReadOnlyList<string> names, string icon)
        {
            if (names.Count == 0)
            {
                return;
            }

            items.Add(new(TranslatedText.ToAccessKeyText(caption.Replace("&", "&&")), IsEnabled: false));
            foreach (string name in names)
            {
                string text = ++itemNumber > 10 ? name.Replace("&", "&&") : $"&{itemNumber % 10}:   {name.Replace("&", "&&")}";
                items.Add(new(TranslatedText.ToAccessKeyText(text), () => Host.CopyToClipboard(name), icon));
            }

            items.Add(LeftPanelMenuItem.Separator);
        }

        void AddItem(string displayText, Func<GitRevision, string> extractText, string icon, char hotkey)
        {
            string[] texts = [.. revisions.Select(extractText).Distinct()];
            displayText = (displayText + ":   " + string.Join(", ", texts.Select(t => t.SubstringUntil('\n'))).ShortenTo(40)).Replace("&", "&&");
            int position = displayText.IndexOf(hotkey.ToString(), StringComparison.InvariantCultureIgnoreCase);
            if (position >= 0)
            {
                displayText = displayText.Insert(position, "&");
            }

            string textToCopy = string.Join("\n", texts);
            items.Add(new(TranslatedText.ToAccessKeyText(displayText.TrimEnd('\r', '\n')), () => Host.CopyToClipboard(textToCopy), icon));
        }
    }

    private void SetRefsSortBy(GitRefsSortBy value)
    {
        Settings.RefsSortBy = value;
        ResortRefs();
    }

    private void SetRefsSortOrder(GitRefsSortOrder value)
    {
        Settings.RefsSortOrder = value;
        ResortRefs();
    }

    /// <summary>As <c>ResortRefs</c>: the trees of references are loaded again, sorted as set.</summary>
    private void ResortRefs()
    {
        _refs = new Lazy<IReadOnlyList<IGitRef>>(Host.GetRefs, LazyThreadSafetyMode.ExecutionAndPublication);
        _ = BranchesTree.ReloadAsync();
        _ = RemotesTree.ReloadAsync();
        _ = TagsTree.ReloadAsync();
    }

    private void SetTreeShown(LeftPanelTreeKind kind, bool shown)
    {
        if (!_initialized)
        {
            return;
        }

        Settings.SetShown(kind, shown);
        ClearSearchResult();
        LeftPanelTree tree = _trees[kind];
        if (shown)
        {
            AddTree(tree);
            if (IsVisible)
            {
                _ = tree.ReloadAsync();
            }
        }
        else
        {
            RemoveTree(tree);
        }

        UpdateSearchCandidates();
    }

    private void ShowEnabledTrees()
    {
        foreach (LeftPanelTree tree in _trees.Values.Where(tree => Settings.IsShown(tree.Kind)))
        {
            AddTree(tree);
        }
    }

    private void AddTree(LeftPanelTree tree)
    {
        if (tree.IsAttached)
        {
            return;
        }

        tree.IsAttached = true;
        Dictionary<LeftPanelTree, int> treeToIndex = GetTreeToPositionIndex();
        int index = Trees.TakeWhile(t => treeToIndex[t] < treeToIndex[tree]).Count();
        Trees.Insert(index, tree);
    }

    private void RemoveTree(LeftPanelTree tree)
    {
        tree.CancelLoading();
        tree.IsAttached = false;
        Trees.Remove(tree);
        if (SelectedNode?.Tree == tree)
        {
            SetSelectedNodeSilently(null);
        }
    }

    private void SortTrees()
    {
        Dictionary<LeftPanelTree, int> treeToIndex = GetTreeToPositionIndex();
        List<LeftPanelTree> sorted = [.. Trees.OrderBy(tree => treeToIndex[tree])];
        LeftPanelNode? selected = SelectedNode;
        _selectingSilently++;
        try
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                int current = Trees.IndexOf(sorted[i]);
                if (current != i)
                {
                    Trees.Move(current, i);
                }
            }

            SelectedNode = selected;
        }
        finally
        {
            _selectingSilently--;
        }
    }

    /// <summary>As <c>FixInvalidTreeToPositionIndices</c>: the positions are made 0-based and sequential, keeping their order.</summary>
    private void FixInvalidTreeToPositionIndices()
    {
        Dictionary<LeftPanelTree, int> treeToIndex = GetTreeToPositionIndex();
        int i = 0;
        foreach (KeyValuePair<LeftPanelTree, int> kvp in treeToIndex.OrderBy(kvp => kvp.Value).ToList())
        {
            treeToIndex[kvp.Key] = i++;
        }

        SaveTreeToPositionIndex(treeToIndex);
    }

    private Dictionary<LeftPanelTree, int> GetTreeToPositionIndex() => _trees.Values.ToDictionary(tree => tree, tree => Settings.GetIndex(tree.Kind));

    private void SaveTreeToPositionIndex(Dictionary<LeftPanelTree, int> treeToIndex)
    {
        foreach ((LeftPanelTree tree, int index) in treeToIndex)
        {
            // Only the changed positions are written.
            if (Settings.GetIndex(tree.Kind) != index)
            {
                Settings.SetIndex(tree.Kind, index);
            }
        }
    }

    private void ClearSearchResult()
    {
        foreach (LeftPanelNode node in _searchResult ?? [])
        {
            node.IsSearchMatch = false;
        }

        _searchResult = null;
    }

    /// <summary>As <c>CollectFilterCandidates</c>: the full paths of the references (not the folders) and the texts of the other nodes.</summary>
    private void UpdateSearchCandidates()
    {
        SearchCandidates = [.. Trees
            .SelectMany(tree => tree.Descendants())
            .Select(node => node is LeftPanelRevisionNode revisionNode ? revisionNode.HasChildren ? null : revisionNode.FullPath : node.Text)
            .OfType<string>()
            .Distinct()];
    }

    /// <summary>As <c>UpdateVisibility</c>: the references whose revision is not in the grid are grayed out.</summary>
    private void UpdateVisibility(LeftPanelTree tree)
    {
        if (Grid.IsLoading || !tree.IsAttached)
        {
            return;
        }

        foreach (LeftPanelRevisionNode node in tree.Descendants().OfType<LeftPanelRevisionNode>().Where(node => !node.ObjectId.IsZero))
        {
            node.Visible = Grid.GetRevision(node.ObjectId) is not null;
        }
    }

    private void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RevisionGridViewModel.IsLoading))
        {
            return;
        }

        if (Grid.IsLoading)
        {
            _ = RefreshAsync();
        }
        else if (IsVisible)
        {
            foreach (LeftPanelTree tree in Trees)
            {
                UpdateVisibility(tree);
            }
        }
    }

    private void OnGridSelectionChanged(object? sender, EventArgs e) => _ = UpdateMergedBranchesAsync();

    /// <summary>
    ///  As <c>RepoObjectsTree.SelectionChanged</c>: the branches merged into the revision selected in the grid get the merged
    ///  icon (unless it was selected through the tree).
    /// </summary>
    private async Task UpdateMergedBranchesAsync()
    {
        IReadOnlyList<GitRevision> selectedRevisions = Grid.GetSelectedRevisionsLatestSelectedFirst();
        ObjectId selectedNodeObjectId = SelectedNode switch
        {
            LeftPanelBranchNode branch => branch.ObjectId,
            TagNode tag => tag.ObjectId,
            _ => default,
        };
        if (!IsVisible
            || (selectedRevisions.Count == 0 && SelectedNode is null)
            || (selectedRevisions.Count == 1 && selectedRevisions[0].ObjectId == selectedNodeObjectId))
        {
            return;
        }

#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loadingMergedBranches?.Cancel();
#pragma warning restore VSTHRD103
        _loadingMergedBranches = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadingMergedBranches.Token;

        GitRevision? selectedRevision = selectedRevisions.Count > 0 ? selectedRevisions[0] : null;
        HashSet<string> mergedBranches = [];
        if (selectedRevision is not null)
        {
            try
            {
                mergedBranches = [.. await Host.GetMergedBranchesAsync(selectedRevision.IsArtificial ? "HEAD" : selectedRevision.Guid, cancellationToken)];
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                Trace.WriteLine($"Failed to get the merged branches: {ex}");
                return;
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        foreach (IGitRef gitRef in selectedRevision?.Refs ?? [])
        {
            mergedBranches.Remove(gitRef.CompleteName);
        }

        foreach (LocalBranchNode node in BranchesTree.Descendants().OfType<LocalBranchNode>())
        {
            node.IsMerged = mergedBranches.Contains(GitRefName.RefsHeadsPrefix + node.FullPath);
        }

        foreach (RemoteBranchNode node in RemotesTree.Descendants().OfType<RemoteBranchNode>())
        {
            node.IsMerged = mergedBranches.Contains(GitRefName.RefsRemotesPrefix + node.FullPath);
        }
    }

    private void OnSubmodulesUpdated(object? sender, LeftPanelSubmodules e) => _ = SubmodulesTree.OnSubmodulesUpdatedAsync(e);
}
