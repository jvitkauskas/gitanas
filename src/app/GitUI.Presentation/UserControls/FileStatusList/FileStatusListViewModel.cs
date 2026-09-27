using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.FileStatusList;

/// <summary>Strings of the file status list; ids match <c>FileStatusList</c>.</summary>
public sealed class FileStatusListStrings : ViewStrings
{
    public FileStatusListStrings()
        : base("FileStatusList")
    {
        NoFiles = Add("NoFiles", "Text", "No changes");
        LoadingFiles = Add("LoadingFiles", "Text", "Loading data...");
        FilterWatermark = Add("cboFilterComboBox", "Watermark", "Filter files using a regular expression...");
        FilterHistory = Add("filterHistoryButton", "ToolTipText", "Recent file filters and examples");
        FilterToolTipTitle = Add("FilterToolTip", "ToolTipTitle", "RegEx");
        AsTreeToolTip = Add("btnAsTree", "ToolTipText", "Toggle flat list / tree");
        ByPathToolTip = Add("btnByPath", "ToolTipText", "Group by file path");
        ByExtensionToolTip = Add("btnByExtension", "ToolTipText", "Group by file type (extension)");
        ByStatusToolTip = Add("btnByStatus", "ToolTipText", "Group by diff status");
        ExpandAll = Add("_expandAll", "Text", "E&xpand all");
        CollapseAll = Add("_collapseAll", "Text", "C&ollapse all");
    }

    public TranslatedText NoFiles { get; }

    public TranslatedText LoadingFiles { get; }

    public TranslatedText FilterWatermark { get; }

    public TranslatedText FilterHistory { get; }

    public TranslatedText FilterToolTipTitle { get; }

    public TranslatedText AsTreeToolTip { get; }

    public TranslatedText ByPathToolTip { get; }

    public TranslatedText ByExtensionToolTip { get; }

    public TranslatedText ByStatusToolTip { get; }

    public TranslatedText ExpandAll { get; }

    public TranslatedText CollapseAll { get; }
}

/// <summary>
///  View model of the file status list (port of the WinForms <c>FileStatusList</c>; docs/avalonia-port/PLAN.md, phase 5):
///  the files of one or more diffs as a tree, their selection and the filter.
/// </summary>
public sealed partial class FileStatusListViewModel : ObservableObject
{
    private readonly GitItemStatus _noItemStatus;
    private IReadOnlyList<FileStatusGroup> _groups = [];
    private Regex? _filterRegex;
    private CancellationTokenSource? _treeBuildCancellation;
    private bool _waitingForGroups;

    /// <summary>Completes when the current tree has been built and its selection restored.</summary>
    public Task TreeLoading { get; private set; } = Task.CompletedTask;
    private bool _updatingSelection;
    private Func<IReadOnlyList<FileStatusNode>, IReadOnlyList<FileStatusNode>>? _restoreSelection;

    /// <summary>Waits for the latest build, including a filter changed while an earlier build was running.</summary>
    public async Task WaitForTreeAsync()
    {
        Task current;
        do
        {
            current = TreeLoading;
#pragma warning disable VSTHRD003 // Tree builds use the caller's UI context; no synchronous join is made.
            await current;
#pragma warning restore VSTHRD003
        }
        while (current != TreeLoading);
    }

    /// <param name="fileNameOnlyFilter">Whether the filter matches only file names (<c>TruncatePathMethod.FileNameOnly</c>).</param>
    public FileStatusListViewModel(FileStatusListStrings strings, FileStatusTreeOptions? options = null, bool fileNameOnlyFilter = false)
    {
        Strings = strings;
        Options = options ?? new FileStatusTreeOptions();
        FileNameOnlyFilter = fileNameOnlyFilter;
        _noItemStatus = new GitItemStatus(name: $"- {strings.NoFiles.Text} -") { IsStatusOnly = true, ErrorMessage = string.Empty };
        SelectedNodes.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSelectedNodesWithChildren));
            if (!_updatingSelection)
            {
                OnSelectionChanged();
            }
        };
    }

    public FileStatusListStrings Strings { get; }

    public bool FileNameOnlyFilter { get; }

    /// <summary>How the files are sorted; changing it rebuilds the tree.</summary>
    [ObservableProperty]
    public partial FileStatusTreeOptions Options { get; set; }

    /// <summary>The root nodes.</summary>
    public ObservableCollection<FileStatusNode> Nodes { get; } = [];

    /// <summary>The selected nodes (the view keeps them in sync with its selection).</summary>
    public BatchObservableCollection<FileStatusNode> SelectedNodes { get; } = [];

    /// <summary>The selected files, in the order of the tree (as <c>FileStatusList.SelectedItems</c>).</summary>
    public IReadOnlyList<FileStatusEntry> SelectedEntries { get; private set; } = [];

    /// <summary>The selected file if exactly one is selected (as <c>FileStatusList.SelectedItem</c>).</summary>
    public FileStatusEntry? SelectedEntry => SelectedEntries.Count == 1 ? SelectedEntries[0] : null;

    /// <summary>All files shown (as <c>FileStatusList.AllItems</c>).</summary>
    public IEnumerable<FileStatusEntry> AllEntries => Nodes.SelectMany(n => n.DescendantsAndSelf()).Select(n => n.Entry).OfType<FileStatusEntry>().Where(e => e.Item != _noItemStatus);

    /// <summary>All the files, also those the filter hides (as <c>GitItemStatuses</c>).</summary>
    public IEnumerable<GitItemStatus> AllItems => _groups.SelectMany(g => g.Statuses);

    /// <summary>The regular expression the file names are filtered with.</summary>
    [ObservableProperty]
    public partial string Filter { get; set; } = "";

    /// <summary>The error of an invalid <see cref="Filter"/>, as its tooltip.</summary>
    [ObservableProperty]
    public partial string? FilterError { get; set; }

    /// <summary>Recent file filters and the examples offered by the WinForms filter dropdown.</summary>
    public ObservableCollection<string> FilterHistory { get; } = ["^(?!.*NotThisWord)", @"^(?!.*\bg?tests?/)"];

    /// <summary>Remembers a valid filter that matches files, when accepted or when its input loses focus.</summary>
    public void RememberFilter()
    {
        if (string.IsNullOrWhiteSpace(Filter) || FilterError is not null || !AllEntries.Any())
        {
            return;
        }

        FilterHistory.Remove(Filter);
        FilterHistory.Insert(0, Filter);
        while (FilterHistory.Count > 10)
        {
            FilterHistory.RemoveAt(FilterHistory.Count - 1);
        }
    }

    [RelayCommand]
    private void ApplyFilter(string expression)
    {
        Filter = expression;
        RememberFilter();
    }

    public bool IsFilterActive => _filterRegex is not null;

    /// <summary>Whether "No changes" is shown instead of the list (as <c>SetFileStatusListVisibility</c>).</summary>
    [ObservableProperty]
    public partial bool ShowNoFiles { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Whether the first file is selected when the files are set (as <c>SelectFirstItemOnSetItems</c>).</summary>
    public bool SelectFirstItemOnSetItems { get; init; } = true;

    /// <summary>
    ///  Whether the list shows all the files of a commit (the file tree tab, <c>isFileTreeMode</c> of <c>Bind</c>): the
    ///  folders stay collapsed unless filtered, and no "no files" text is shown.
    /// </summary>
    public bool IsFileTreeMode { get; init; }

    /// <summary>Raised when the selected files change (as <c>SelectedIndexChanged</c>).</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Raised when the files are set (as <c>DataSourceChanged</c>).</summary>
    public event EventHandler? DataSourceChanged;

    /// <summary>The text shown without files (as <c>SetNoFilesText</c>); "No changes" by default.</summary>
    public string NoFilesText
    {
        get => field ?? Strings.NoFiles.Text;
        init;
    }

    /// <summary>Raised when the selected files are activated (a double click, as the <c>DoubleClick</c> of the WinForms list).</summary>
    public event EventHandler? SelectionActivated;

    /// <summary>Activates the selected files (from the view).</summary>
    public void ActivateSelection()
    {
        if (SelectionActivated is not null)
        {
            SelectionActivated(this, EventArgs.Empty);
            return;
        }

        // As FileStatusListView_DoubleClick without a DoubleClick handler (and DiffFiles_DoubleClick): the submodule is opened
        // if set so, else the history of the file.
        if (SelectedEntry is not { } entry || MenuHost is not { } host || !entry.Item.IsTracked)
        {
            return;
        }

        if (entry.Item.IsSubmodule && host.OpenSubmoduleOnDoubleClick)
        {
            host.OpenSubmodule(entry);
        }
        else
        {
            host.ShowFileHistory(entry, selectedFolder: null, blame: false);
        }
    }

    /// <summary>As <c>DisableSubmoduleMenuItemBold</c>: the open submodule item is never bold (the commit dialog stages on a double click).</summary>
    public bool DisableSubmoduleMenuItemBold { get; init; }

    /// <summary>As the click on <c>_NO_TRANSLATE_openSubmoduleMenuItem</c>.</summary>
    [RelayCommand]
    private void OpenSubmodule()
    {
        if (SelectedEntry is { } entry)
        {
            MenuHost?.OpenSubmodule(entry);
        }
    }

    public bool IsFlatList => Options.SortType.ToString().EndsWith("Flat");

    /// <summary>As <c>btnByPath.Checked</c>: the files are grouped by their path.</summary>
    public bool IsByPath => Options.SortType is DiffListSortType.FilePath or DiffListSortType.FilePathFlat;

    /// <summary>As <c>btnByExtension.Checked</c>.</summary>
    public bool IsByExtension => Options.SortType is DiffListSortType.FileExtension or DiffListSortType.FileExtensionFlat;

    /// <summary>As <c>btnByStatus.Checked</c>.</summary>
    public bool IsByStatus => Options.SortType is DiffListSortType.FileStatus or DiffListSortType.FileStatusFlat;

    /// <summary>Shows "Loading data..." until the files are set.</summary>
    public void SetLoading()
    {
        CancelTreeBuild();
        _waitingForGroups = true;
        IsLoading = true;
        ShowNoFiles = false;
    }

    /// <summary>Shows the files of the diffs (as <c>FileStatusList.SetDiffs</c>).</summary>
    public void SetGroups(IReadOnlyList<FileStatusGroup> groups)
    {
        _groups = groups;
        _waitingForGroups = false;
        IsLoading = false;

        // As SetDiffsAsync with git grep: the search runs again for the new revision.
        _gitGrepGroup = null;
        Update(updateCausedByFilter: false);
        if (IsGitGrepActive)
        {
            StartGitGrep(GitGrepText, delayMilliseconds: 0);
        }
    }

    /// <summary>Shows the files of a diff between two revisions.</summary>
    public void SetDiff(GitRevision? firstRevision, GitRevision secondRevision, IReadOnlyList<GitItemStatus> items)
        => SetGroups([new FileStatusGroup(firstRevision, secondRevision, "", items)]);

    public void Clear() => SetGroups([]);

    /// <summary>Selects the files (as setting <c>SelectedItems</c>), expanding their parents (as <c>TreeView</c> shows a selected node).</summary>
    public void Select(Func<FileStatusEntry, bool> predicate)
    {
        List<FileStatusNode> nodes = [.. Nodes.SelectMany(n => n.DescendantsAndSelf()).Where(n => n.Entry is { } entry && predicate(entry))];
        foreach (FileStatusNode node in nodes)
        {
            for (FileStatusNode? parent = node.Parent; parent is not null; parent = parent.Parent)
            {
                parent.IsExpanded = true;
            }
        }

        SetSelection(nodes);
    }

    /// <summary>As <c>SelectFirstVisibleItem</c>: the first file, expanding its parents.</summary>
    public void SelectFirstVisibleItem()
    {
        FileStatusNode? first = Nodes.SelectMany(n => n.DescendantsAndSelf()).FirstOrDefault(n => n.Entry is not null && n.Entry.Item != _noItemStatus);
        SetSelection(first is null ? [] : [first]);
        for (FileStatusNode? parent = first?.Parent; parent is not null; parent = parent.Parent)
        {
            parent.IsExpanded = true;
        }
    }

    /// <summary>As <c>SelectNextItem</c>: the next or previous file, with <paramref name="loop"/> from the last to the first.</summary>
    public void SelectNextItem(bool backwards, bool loop = false)
    {
        List<FileStatusNode> files = [.. Nodes.SelectMany(n => n.DescendantsAndSelf()).Where(n => n.Entry is not null)];
        if (files.Count == 0)
        {
            return;
        }

        int index = SelectedNodes.Count == 0 ? -1 : files.IndexOf(SelectedNodes[^1]);
        int next = index + (backwards ? -1 : 1);
        next = index < 0 ? 0
            : loop ? (next + files.Count) % files.Count
            : Math.Clamp(next, 0, files.Count - 1);
        SetSelection([files[next]]);
    }

    [RelayCommand]
    private void ToggleFlatList()
    {
        // As btnAsTree: switch between the flat and the tree variant of the sorting.
        string sortType = Options.SortType.ToString();
        DiffListSortType toggled = Enum.Parse<DiffListSortType>(sortType.EndsWith("Flat") ? sortType[..^"Flat".Length] : sortType + "Flat");
        Options = Options with { SortType = toggled };
    }

    [RelayCommand]
    private void SortBy(DiffListSortType sortType)
        => Options = Options with { SortType = IsFlatList ? Enum.Parse<DiffListSortType>(sortType + "Flat") : sortType };

    /// <summary>As the items of "Sort and group by": the exact sorting.</summary>
    [RelayCommand]
    private void SetSortType(DiffListSortType sortType) => Options = Options with { SortType = sortType };

    /// <summary>The actions of the menu, if the list has one (the host of the dialog provides them).</summary>
    public IFileStatusListMenuHost? MenuHost
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(HasMenuHost));
        }
    }

    public FileStatusListMenuStrings MenuStrings { get; } = ViewStrings.Load<FileStatusListMenuStrings>();

    public CopyPathsStrings CopyPathsStrings { get; } = ViewStrings.Load<CopyPathsStrings>();

    /// <summary>The menu items for the selection, updated when the menu opens.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpenSubmoduleBold))]
    public partial FileStatusMenuState MenuState { get; private set; } = FileStatusMenuState.None;

    /// <summary>Whether "Open with Git Extensions" is bold, as the action of a double click.</summary>
    public bool IsOpenSubmoduleBold => MenuState.IsOpenSubmoduleDefault && !DisableSubmoduleMenuItemBold;

    public bool HasMenuHost => MenuHost is not null;

    /// <summary>The selected folder, if a single folder is selected (as <c>FileStatusList.SelectedFolder</c>).</summary>
    public RelativePath? SelectedFolder => SelectedNodes is [{ Entry: null, FolderPath: { } path }] ? path : null;

    /// <summary>Raised when an action of the menu changed the files (as <c>RequestRefresh</c>).</summary>
    public event EventHandler? RefreshRequested;

    /// <summary>Raised when the user changes the sorting, which the host keeps for all lists (<c>DiffListSortService</c>).</summary>
    public event EventHandler? SortTypeChanged;

    /// <summary>The file selected last (as <c>FocusedItem</c>).</summary>
    public FileStatusEntry? FocusedEntry => SelectedNodes.Count == 0 ? null : SelectedNodes[^1].Entry;

    /// <summary>
    ///  "Stage selected" of the dialog instead of the one of the host (the <c>stage</c> of <c>BindContextMenu</c>, e.g. the
    ///  staging of the commit dialog).
    /// </summary>
    public Action? StageSelectedAction { get; set; }

    /// <summary>"Unstage selected" of the dialog instead of the one of the host (the <c>unstage</c> of <c>BindContextMenu</c>).</summary>
    public Action? UnstageSelectedAction { get; set; }

    /// <summary>
    ///  "Blame" of the main window (the <c>blame</c> of <c>BindContextMenu</c>): the blame shown in the tab (checked) instead of
    ///  the dialog, if any.
    /// </summary>
    public Action? BlameAction { get; set; }

    /// <summary>Whether the blame of the selected file is shown instead of its diff (<c>tsmiBlame.Checked</c>).</summary>
    [ObservableProperty]
    public partial bool IsBlameShown { get; set; }

    /// <summary>"Show in file tree" of the main window (the <c>openInFileTreeTab_AsBlame</c> of <c>BindContextMenu</c>), if any.</summary>
    public Action? ShowInFileTreeAction { get; set; }

    /// <summary>"Filter file in grid" of the main window (the <c>filterFileInGrid</c> of <c>BindContextMenu</c>), if any.</summary>
    public Action? FilterFileInGridAction
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(HasFilterFileInGrid));
        }
    }

    public bool HasFilterFileInGrid => FilterFileInGridAction is not null;

    /// <summary>"Cherry pick changes" of the file viewer (the <c>cherryPickChanges</c> of <c>BindContextMenu</c>), if any.</summary>
    public Action? CherryPickChangesAction { get; set; }

    /// <summary>Whether the diff shown supports line patching (the <c>getSupportLinePatching</c> of <c>BindContextMenu</c>).</summary>
    public Func<bool>? GetSupportLinePatching { get; set; }

    /// <summary>As <c>ItemContextMenu_Opening</c> / <c>UpdateStatusOfMenuItems</c>.</summary>
    public void UpdateMenuState()
    {
        OnPropertyChanged(nameof(CanCollapseRootFolders));
        UpdateDifftoolCaptions();
        if (MenuHost is not { } host)
        {
            MenuState = FileStatusMenuState.None;
            return;
        }

        // The items of the dialogs binding them (BindContextMenu).
        FileStatusMenuState state = host.GetMenuState(SelectedEntries, SelectedFolder, FocusedEntry, GetSupportLinePatching?.Invoke() ?? false);
        MenuState = state with
        {
            ShowShowInFileTree = state.ShowShowInFileTree && ShowInFileTreeAction is not null && !IsFileTreeMode,
            CanFilterFileInGrid = state.CanFilterFileInGrid && FilterFileInGridAction is not null,
            ShowCherryPick = state.ShowCherryPick && CherryPickChangesAction is not null,
        };
    }

    [RelayCommand]
    private void StageFiles()
    {
        if (StageSelectedAction is { } stage)
        {
            stage();
            return;
        }

        MenuHost?.StageFiles(SelectedEntries);
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void UnstageFiles()
    {
        if (UnstageSelectedAction is { } unstage)
        {
            unstage();
            return;
        }

        MenuHost?.UnstageFiles(SelectedEntries);
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task ResetChunkOfFileAsync()
    {
        if (SelectedEntry is { } entry && MenuHost is { } host)
        {
            await host.ResetChunkOfFileAsync(entry);
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private async Task InteractiveAddAsync()
    {
        if (SelectedEntry is { } entry && MenuHost is { } host)
        {
            await host.InteractiveAddAsync(entry);
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void CherryPickChanges() => CherryPickChangesAction?.Invoke();

    [RelayCommand]
    private void RememberDiff(bool first)
    {
        if (SelectedEntry is { } entry)
        {
            MenuHost?.RememberDiff(entry, first);
        }
    }

    [RelayCommand]
    private void DiffWithRemembered()
    {
        if (SelectedEntry is { } entry)
        {
            MenuHost?.DiffWithRemembered(entry);
        }
    }

    [RelayCommand]
    private void DiffTwoSelected() => MenuHost?.DiffTwoSelected(SelectedEntries, FocusedEntry);

    [RelayCommand]
    private void OpenInVisualStudio()
    {
        if (SelectedEntry is { } entry)
        {
            MenuHost?.OpenInVisualStudio(entry);
        }
    }

    [RelayCommand]
    private void Move()
    {
        if (MenuHost?.Move(SelectedEntry, SelectedEntry is null ? SelectedFolder : null) == true)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void DeleteFiles()
    {
        if (MenuHost?.DeleteFiles(SelectedEntries) == true)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void ShowInFileTree() => ShowInFileTreeAction?.Invoke();

    [RelayCommand]
    private void FilterFileInGrid() => FilterFileInGridAction?.Invoke();

    /// <summary>As <c>FindFile_Click</c>: once the files are loaded, the file chosen among all files of the list is selected.</summary>
    [RelayCommand]
    private async Task FindFileAsync()
    {
        while (IsLoading)
        {
            await Task.Delay(100);
        }

        if (MenuHost?.FindFile([.. AllItems]) is { } item)
        {
            Select(entry => entry.Item == item);
        }
    }

    [RelayCommand]
    private void AddToIgnoreFile(bool localExclude)
    {
        if (MenuHost?.AddToIgnoreFile(SelectedEntries, SelectedFolder, localExclude) == true)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>As <c>SkipWorktree_Click</c>: the item toggles (<c>CheckOnClick</c>).</summary>
    [RelayCommand]
    private void ToggleSkipWorktree()
    {
        MenuHost?.SetSkipWorktree(SelectedEntries, !MenuState.IsSkipWorktree);
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>As <c>AssumeUnchanged_Click</c>: the item toggles (<c>CheckOnClick</c>).</summary>
    [RelayCommand]
    private void ToggleAssumeUnchanged()
    {
        MenuHost?.SetAssumeUnchanged(SelectedEntries, !MenuState.IsAssumeUnchanged);
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void StopTracking()
    {
        if (SelectedEntry is { } entry && MenuHost?.StopTracking(entry) == true)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void RunSubmoduleAction(SubmoduleMenuAction action)
    {
        if (MenuHost?.RunSubmoduleAction(SelectedEntries, action) == true)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void RunScript(FileStatusScript script) => MenuHost?.RunScript(script, SelectedEntries, SelectedFolder);

    /// <summary>As <c>SelectFirstGroupChangesIfFocused</c>: the files of the first diff (all files without diff groups).</summary>
    public void SelectFirstGroup()
    {
        if (Nodes.Count == 0)
        {
            return;
        }

        IEnumerable<FileStatusNode> nodes = Nodes[0].Group is not null ? Nodes[0].DescendantsAndSelf() : Nodes.SelectMany(n => n.DescendantsAndSelf());
        SetSelection([.. nodes.Where(n => n.Entry is not null && n.Entry.Item != _noItemStatus)]);
    }

    /// <summary>
    ///  As <c>RevisionDiffControl.RefreshArtificial</c>: the files are set again, keeping the selected files (else the file after
    ///  them, <c>StoreNextItemToSelect</c>; else the first file).
    /// </summary>
    public void RefreshGroups(IReadOnlyList<FileStatusGroup> groups)
    {
        HashSet<(string Name, ObjectId Id)> selected = [.. SelectedEntries.Select(Key)];
        List<FileStatusEntry> entries = [.. AllEntries];
        int lastSelected = entries.FindLastIndex(entry => selected.Contains(Key(entry)));
        (string Name, ObjectId Id)? next = lastSelected < 0 ? null
            : entries.Skip(lastSelected + 1).FirstOrDefault(entry => !selected.Contains(Key(entry))) is { } nextEntry ? Key(nextEntry)
            : null;

        _restoreSelection = files =>
        {
            List<FileStatusNode> nodes = [.. files.Where(node => selected.Contains(Key(node.Entry!)))];
            return nodes.Count > 0 || next is not { } nextKey ? nodes : [.. files.Where(node => Key(node.Entry!) == nextKey).Take(1)];
        };
        try
        {
            SetGroups(groups);
        }
        finally
        {
            _restoreSelection = null;
        }

        static (string Name, ObjectId Id) Key(FileStatusEntry entry) => (entry.Item.Name, entry.SecondRevision.ObjectId);
    }

    /// <summary>Selects the folder (as <c>SelectFileOrFolder</c> for a folder), expanding its parents.</summary>
    public void SelectFolder(RelativePath folder)
    {
        FileStatusNode? node = Nodes.SelectMany(n => n.DescendantsAndSelf()).FirstOrDefault(n => n.Entry is null && n.FolderPath?.Value == folder.Value);
        if (node is null)
        {
            return;
        }

        for (FileStatusNode? parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            parent.IsExpanded = true;
        }

        SetSelection([node]);
    }

    [RelayCommand]
    private void OpenWithDifftool(DifftoolKind kind) => MenuHost?.OpenWithDifftool(SelectedEntries, kind);

    [RelayCommand]
    private void OpenWorkingDirectoryFile(bool openWith)
    {
        if (SelectedEntry is { } entry)
        {
            MenuHost?.OpenWorkingDirectoryFile(entry, openWith);
        }
    }

    [RelayCommand]
    private void EditWorkingDirectoryFile()
    {
        if (SelectedEntry is { } entry && MenuHost?.EditWorkingDirectoryFile(entry) == true)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void OpenRevisionFile(bool openWith)
    {
        if (SelectedEntry is { } entry)
        {
            MenuHost?.OpenRevisionFile(entry, openWith);
        }
    }

    [RelayCommand]
    private void SaveAs() => MenuHost?.SaveAs(SelectedEntries);

    [RelayCommand]
    private void CopyPaths(CopyPathKind kind) => MenuHost?.CopyPaths(SelectedEntries, SelectedFolder, kind);

    [RelayCommand]
    private void ShowInFolder() => MenuHost?.ShowInFolder(SelectedEntries, SelectedFolder);

    [RelayCommand]
    private void ShowFileHistory(bool blame)
    {
        if (blame && BlameAction is not null)
        {
            BlameAction();

            // The check mark of the item as the action left it (a menu item toggles itself when clicked).
            OnPropertyChanged(nameof(IsBlameShown));
            return;
        }

        MenuHost?.ShowFileHistory(SelectedEntry, SelectedFolder, blame);
    }

    [RelayCommand]
    private void ResetFiles(bool toParent)
    {
        if (MenuHost?.ResetFiles(SelectedEntries, toParent) == true)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>As <c>ExpandAll_Click</c>: the selected nodes with nodes below them, else all the nodes.</summary>
    [RelayCommand]
    private void ExpandAll()
    {
        foreach (FileStatusNode node in NodesOfTreeItems())
        {
            node.ExpandAll();
        }
    }

    /// <summary>As <c>CollapseAll_Click</c>: the selected nodes with nodes below them, else all the nodes.</summary>
    [RelayCommand]
    private void CollapseAll()
    {
        foreach (FileStatusNode node in NodesOfTreeItems().SelectMany(n => n.DescendantsAndSelf()))
        {
            node.IsExpanded = false;
        }
    }

    private IReadOnlyList<FileStatusNode> NodesOfTreeItems()
        => HasSelectedNodesWithChildren ? [.. SelectedNodes.Where(node => node.Children.Count > 0)] : Nodes;

    [RelayCommand]
    private void ClearFilter() => Filter = "";

    partial void OnOptionsChanged(FileStatusTreeOptions oldValue, FileStatusTreeOptions newValue)
    {
        OnPropertyChanged(nameof(IsFlatList));
        OnPropertyChanged(nameof(IsByPath));
        OnPropertyChanged(nameof(IsByExtension));
        OnPropertyChanged(nameof(IsByStatus));
        if (oldValue is not null && oldValue.SortType != newValue.SortType)
        {
            SortTypeChanged?.Invoke(this, EventArgs.Empty);
        }

        Update(updateCausedByFilter: true);
    }

    partial void OnFilterChanged(string value)
    {
        // As StoreFilter: an invalid expression keeps the previous filter and is reported.
        if (string.IsNullOrEmpty(value))
        {
            _filterRegex = null;
            FilterError = null;
        }
        else
        {
            try
            {
                _filterRegex = new Regex(value, RegexOptions.IgnoreCase);
                FilterError = null;
            }
            catch (ArgumentException ex)
            {
                FilterError = ex.Message;
                return;
            }
        }

        OnPropertyChanged(nameof(IsFilterActive));
        Update(updateCausedByFilter: true);
    }

    /// <summary>As <c>UpdateFileStatusListView</c>.</summary>
    private void Update(bool updateCausedByFilter)
    {
        if (IsFileTreeMode && _waitingForGroups)
        {
            // The incoming revision will use the latest filter; do not rebuild the previous revision meanwhile.
            return;
        }

        HashSet<GitItemStatus>? previouslySelectedItems = updateCausedByFilter ? [.. SelectedEntries.Select(e => e.Item)] : null;

        IReadOnlyList<FileStatusGroup> groups = ShownGroups;
        CancelTreeBuild();
        FileStatusTreeOptions options = Options;
        Func<GitItemStatus, bool> filter = CreateFilterMatch();
        bool expand = !IsFileTreeMode || IsFilterActive || IsGitGrepActive;
        bool delayFilter = updateCausedByFilter && IsFilterActive;
        if (IsFileTreeMode && groups.Sum(group => group.Statuses.Count) >= 5000 && SynchronizationContext.Current is not null)
        {
            CancellationTokenSource cancellation = new();
            _treeBuildCancellation = cancellation;
            IsLoading = true;
            TreeLoading = BuildAsync();
            return;

            async Task BuildAsync()
            {
                try
                {
                    if (delayFilter)
                    {
                        await Task.Delay(100, cancellation.Token);
                    }

                    (List<FileStatusNode> nodes, _, bool filesPresent) = await Task.Run(
                        () => FileStatusTreeBuilder.Build(groups, options, Match, _noItemStatus, expand), cancellation.Token);
                    if (!cancellation.IsCancellationRequested)
                    {
                        ApplyTree(nodes, filesPresent, groups, updateCausedByFilter, previouslySelectedItems);
                        IsLoading = false;
                    }
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    // A new revision, filter or grouping superseded this tree.
                }
                finally
                {
                    if (ReferenceEquals(_treeBuildCancellation, cancellation))
                    {
                        _treeBuildCancellation = null;
                    }

                    cancellation.Dispose();
                }

                bool Match(GitItemStatus item)
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    return filter(item);
                }
            }
        }

        (List<FileStatusNode> immediateNodes, _, bool immediateFilesPresent) = FileStatusTreeBuilder.Build(groups, options, filter, _noItemStatus, expand);
        ApplyTree(immediateNodes, immediateFilesPresent, groups, updateCausedByFilter, previouslySelectedItems);
        if (IsFileTreeMode)
        {
            IsLoading = false;
        }
    }

    private void CancelTreeBuild()
    {
        _treeBuildCancellation?.Cancel();
        _treeBuildCancellation = null;
        TreeLoading = Task.CompletedTask;
    }

    private void ApplyTree(List<FileStatusNode> nodes, bool filesPresent, IReadOnlyList<FileStatusGroup> groups,
        bool updateCausedByFilter, HashSet<GitItemStatus>? previouslySelectedItems)
    {
        ShowNoFiles = !filesPresent && groups.Count <= 1 && !IsFileTreeMode && !IsGitGrepActive;

        Nodes.Clear();
        foreach (FileStatusNode node in nodes)
        {
            Nodes.Add(node);
        }

        if (Nodes.Count == 1 && Nodes[0].Children.Count == 0)
        {
            SetSelection([Nodes[0]]);
        }
        else if (!updateCausedByFilter && _restoreSelection is { } restore
            && restore([.. Nodes.SelectMany(n => n.DescendantsAndSelf()).Where(n => n.Entry is not null && n.Entry.Item != _noItemStatus)]) is { Count: > 0 } restored)
        {
            foreach (FileStatusNode node in restored)
            {
                for (FileStatusNode? parent = node.Parent; parent is not null; parent = parent.Parent)
                {
                    parent.IsExpanded = true;
                }
            }

            SetSelection(restored);
        }
        else if (!updateCausedByFilter && SelectFirstItemOnSetItems)
        {
            SelectFirstVisibleItem();
        }
        else if (previouslySelectedItems?.Count is > 0)
        {
            Select(entry => previouslySelectedItems.Contains(entry.Item));
        }
        else
        {
            SetSelection([]);
        }

        OnPropertyChanged(nameof(HasGroups));
        OnPropertyChanged(nameof(HasDiffABGroups));
        OnPropertyChanged(nameof(HasArtificialCommits));
        OnPropertyChanged(nameof(HasWorkTree));
        DataSourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>As <c>FileStatusList.IsFilterMatch</c>: the A/B diff status buttons and the filter.</summary>
    private Func<GitItemStatus, bool> CreateFilterMatch()
    {
        // Workers must not observe UI options changing in the middle of a build.
        Regex? regex = _filterRegex;
        bool onlyA = ShowOnlyA;
        bool onlyB = ShowOnlyB;
        bool unequal = ShowUnequalChange;
        bool same = ShowSameChange;
        return item =>
        {
            if (item.IsRangeDiff)
            {
                return true;
            }

            bool matchesStatus = item.DiffStatus switch
            {
                DiffBranchStatus.UnequalChange => unequal,
                DiffBranchStatus.OnlyBChange => onlyB,
                DiffBranchStatus.OnlyAChange => onlyA,
                DiffBranchStatus.SameChange => same,
                _ => true,
            };
            if (!matchesStatus || regex is null)
            {
                return matchesStatus;
            }

            string name = item.Name.TrimEnd('/');
            string? oldName = item.OldName;
            if (FileNameOnlyFilter)
            {
                name = Path.GetFileName(name);
                oldName = Path.GetFileName(oldName);
            }

            return regex.IsMatch(name) || (oldName is not null && regex.IsMatch(oldName));
        };
    }

    private void SetSelection(IReadOnlyList<FileStatusNode> nodes)
    {
        _updatingSelection = true;
        try
        {
            using IDisposable update = SelectedNodes.BeginUpdate();
            SelectedNodes.Clear();
            foreach (FileStatusNode node in nodes)
            {
                SelectedNodes.Add(node);
            }
        }
        finally
        {
            _updatingSelection = false;
        }

        OnSelectionChanged();
    }

    private void OnSelectionChanged()
    {
        // As SelectedItems: a single selected folder or group selects its files.
        IEnumerable<FileStatusNode> selected = SelectedNodes.Count == 1 && SelectedNodes[0].Entry is null
            ? SelectedNodes[0].DescendantsAndSelf()
            : SelectedNodes;
        IReadOnlyList<FileStatusEntry> entries = [.. selected.Select(n => n.Entry).OfType<FileStatusEntry>().Where(e => e.Item != _noItemStatus)];
        if (entries.SequenceEqual(SelectedEntries))
        {
            return;
        }

        SelectedEntries = entries;
        OnPropertyChanged(nameof(SelectedEntries));
        OnPropertyChanged(nameof(SelectedEntry));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
