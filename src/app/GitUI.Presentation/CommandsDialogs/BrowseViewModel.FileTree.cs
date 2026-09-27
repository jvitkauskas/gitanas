using GitCommands;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>What the file tree tab needs from the application.</summary>
public interface IBrowseFileTreeHost
{
    /// <summary>
    ///  All the files of the revision, also of the artificial commits (as <c>FileStatusDiffCalculator.GetGrepItemStatuses</c>
    ///  in file tree mode).
    /// </summary>
    Task<FileStatusGroup> GetTreeFilesAsync(GitRevision revision, CancellationToken cancellationToken);
}

/// <summary>The file tree tab of the main window (<c>TreeTabPage</c>, the <c>RevisionDiffControl</c> in file tree mode).</summary>
public sealed partial class BrowseViewModel
{
    private IBrowseFileTreeHost? _fileTreeHost;
    private CancellationTokenSource? _loadingTree;
    private GitRevision? _treeRevision;
    private bool _treeUpToDate;

    /// <summary>All the files of the selected revision.</summary>
    public FileStatusListViewModel? FileTree { get; private set; }

    /// <summary>The file selected in the file tree, as a file (<c>forceFileView</c>).</summary>
    public FileViewerViewModel? TreeViewer { get; private set; }

    private void InitializeFileTree(IFileViewerHost fileViewerHost, FileStatusListStrings strings, FileStatusTreeOptions options)
    {
        _fileTreeHost = _host as IBrowseFileTreeHost;
        if (_fileTreeHost is null)
        {
            return;
        }

        // As Bind with isFileTreeMode: the tree is sorted by path, not by the sorting of the diff lists.
        FileTree = new FileStatusListViewModel(strings, new FileStatusTreeOptions(MergeSingleItemsWithFolder: options.MergeSingleItemsWithFolder))
        {
            IsFileTreeMode = true,
            SelectFirstItemOnSetItems = false,
        };
        TreeViewer = new FileViewerViewModel(fileViewerHost);
        FileTree.SelectionChanged += (_, _) => ShowTreeFile();
    }

    // As FormBrowse.FillFileTree: the tree is loaded when its tab is shown, once for each selected revision.
    private void UpdateFileTree(bool revisionChanged)
    {
        if (revisionChanged)
        {
            IReadOnlyList<GitRevision> selected = Grid.GetSelectedRevisionsLatestSelectedFirst();
            _treeRevision = selected.Count == 0 ? null : selected[0];
            _treeUpToDate = false;
        }

        if (FileTree is null || _treeUpToDate || SelectedTab != BrowseTab.FileTree)
        {
            return;
        }

        _treeUpToDate = true;
        _ = LoadFileTreeAsync(_treeRevision);
    }

    private async Task LoadFileTreeAsync(GitRevision? revision)
    {
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loadingTree?.Cancel();
#pragma warning restore VSTHRD103
        _loadingTree = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadingTree.Token;
        FileStatusListViewModel fileTree = FileTree!;

        // As RevisionDiffControl: the selected file stays selected in the next revision, if it has it (or the file to show in the tree).
        string? selectedPath = _pendingTreePath ?? fileTree.SelectedEntry?.Item.Name;
        bool isFolder = _pendingTreePath is not null && _pendingTreeFolder;
        if (revision is null)
        {
            fileTree.SetGroups([]);
            return;
        }

        fileTree.SetLoading();
        FileStatusGroup files;
        try
        {
            files = await _fileTreeHost!.GetTreeFilesAsync(revision, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        fileTree.SetGroups([files]);
        await fileTree.WaitForTreeAsync();
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        _pendingTreePath = null;
        _pendingTreeFolder = false;
        if (selectedPath is not null)
        {
            SelectInFileTree(selectedPath, isFolder);
        }

        if (fileTree.SelectedEntry is null)
        {
            ShowTreeFile();
        }
    }

    // As ShowSelectedFile in file tree mode: the file, or its blame while "Blame" is checked (kept for the next files).
    private void ShowTreeFile()
    {
        int? line = _pendingTreeLine;
        _pendingTreeLine = null;
        if (FileTree!.SelectedEntry is { } entry && !entry.Item.IsStatusOnly)
        {
            if (FileTree.IsBlameShown && TreeBlame is not null && FileTree.SelectedFolder is null)
            {
                IsTreeBlameVisible = true;
                _ = LoadBlameAsync(TreeBlame, entry, line);
                return;
            }

            IsTreeBlameVisible = false;
            _ = TreeViewer!.ShowFileAsync(entry.Item, entry.SecondRevision.ObjectId);
        }
        else
        {
            IsTreeBlameVisible = false;
            _ = TreeViewer!.ShowChangesAsync(null);
        }
    }

    partial void OnSelectedTabChanged(BrowseTab value)
    {
        UpdateFileTree(revisionChanged: false);
        UpdateGpgInfo(revisionChanged: false);
        UpdateConsole();
        UpdateBuildReport(revisionChanged: false);
    }
}
