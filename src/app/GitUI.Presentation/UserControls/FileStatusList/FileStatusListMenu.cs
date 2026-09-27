using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls.FileStatusList;

/// <summary>Strings of the menu of the file status list; ids match <c>FileStatusList</c>.</summary>
public sealed class FileStatusListMenuStrings : ViewStrings
{
    public FileStatusListMenuStrings()
        : base("FileStatusList")
    {
        OpenWithDifftool = Add("tsmiOpenWithDifftool", "Text", "Open with &difftool");
        DiffFirstToSelected = Add("tsmiDiffFirstToSelected", "Text", "&First -> Second");
        DiffFirstToLocal = Add("tsmiDiffFirstToLocal", "Text", "First -> &Working directory");
        DiffSelectedToLocal = Add("tsmiDiffSelectedToLocal", "Text", "&Second -> Working directory");
        OpenWorkingDirectoryFile = Add("tsmiOpenWorkingDirectoryFile", "Text", "&Open working directory file");
        OpenWorkingDirectoryFileWith = Add("tsmiOpenWorkingDirectoryFileWith", "Text", "Open working directory file with...");
        EditWorkingDirectoryFile = Add("tsmiEditWorkingDirectoryFile", "Text", "&Edit working directory file");
        OpenRevisionFile = Add("tsmiOpenRevisionFile", "Text", "Ope&n this revision (temp file)");
        OpenRevisionFileWith = Add("tsmiOpenRevisionFileWith", "Text", "Open this revision &with... (temp file)");
        SaveAs = Add("tsmiSaveAs", "Text", "S&ave selected as...");
        CopyPaths = Add("tsmiCopyPaths", "Text", "Copy &path(s)");
        ShowInFolder = Add("tsmiShowInFolder", "Text", "Show &in folder");
        FileHistory = Add("tsmiFileHistory", "Text", "File &history");
        Blame = Add("tsmiBlame", "Text", "&Blame");
        ResetFileTo = Add("tsmiResetFileTo", "Text", "&Reset file(s) to");
        FirstRevision = Add("_firstRevision", "Text", "First: A ");
        SelectedRevision = Add("_selectedRevision", "Text", "Second: B ");
        MultipleDescription = Add("_multipleDescription", "Text", "<multiple>");
        ResetSelectedChanges = Add("_resetSelectedChangesText", "Text", "Are you sure you want to reset all selected files to {0}?");
        SaveFileFilterAllFiles = Add("_saveFileFilterAllFiles", "Text", "All files");
        SaveFileFilterCurrentFormat = Add("_saveFileFilterCurrentFormat", "Text", "Current format");
        SortBy = Add("_sortByContextMenu", "Text", "&Sort and group by");
        GroupByFilePathTree = Add("tsmiGroupByFilePathTree", "Text", "Group by file &path - tree");
        GroupByFilePathFlat = Add("tsmiGroupByFilePathFlat", "Text", "Group by &file path - flat");
        GroupByFileExtensionTree = Add("tsmiGroupByFileExtensionTree", "Text", "Group by file &extension - tree");
        GroupByFileExtensionFlat = Add("tsmiGroupByFileExtensionFlat", "Text", "Group by file e&xtension - flat");
        GroupByFileStatusTree = Add("tsmiGroupByFileStatusTree", "Text", "Group by file &status - tree");
        GroupByFileStatusFlat = Add("tsmiGroupByFileStatusFlat", "Text", "Group by file s&tatus - flat");
        StageFile = Add("tsmiStageFile", "Text", "&Stage selected");
        UnstageFile = Add("tsmiUnstageFile", "Text", "&Unstage selected");
        ResetChunkOfFile = Add("tsmiResetChunkOfFile", "Text", "Reset chunk of file...");
        InteractiveAdd = Add("tsmiInteractiveAdd", "Text", "Interactive add...");
        CherryPickChanges = Add("tsmiCherryPickChanges", "Text", "Cherr&y pick changes");
        DiffTwoSelected = Add("tsmiDiffTwoSelected", "Text", "&Diff the selected files");
        RememberSecondRevDiff = Add("tsmiRememberSecondRevDiff", "Text", "&Remember Second for diff");
        RememberFirstRevDiff = Add("tsmiRememberFirstRevDiff", "Text", "R&emember First for diff");
        DiffSelectedWithRememberedFile = Add("_diffSelectedWithRememberedFile", "Text", "&Diff with \"{0}\"", category: "TranslatedStrings");
        OpenWithGitExtensions = Add("_openWithGitExtensions", "Text", "&Open with Gitanas", category: "TranslatedStrings");
        OpenInVisualStudio = Add("tsmiOpenInVisualStudio", "Text", "Open in &Visual Studio");
        Move = Add("tsmiMove", "Text", "Rena&me / move");
        NewName = Add("_newName", "Text", "New name");
        DeleteSelectedFilesCaption = Add("_deleteSelectedFilesCaption", "Text", "Delete");
        DeleteSelectedFiles = Add("_deleteSelectedFiles", "Text", "Are you sure you want to delete the selected file(s)?");
        DeleteFailed = Add("_deleteFailed", "Text", "Delete file failed");
        ShowInFileTree = Add("tsmiShowInFileTree", "Text", "Show in File &tree");
        FilterFileInGrid = Add("tsmiFilterFileInGrid", "Text", "Filter file in &grid");
        FindFile = Add("tsmiFindFile", "Text", "&Find file...");
        AddFileToGitIgnore = Add("tsmiAddFileToGitIgnore", "Text", "Add file to &.gitignore");
        AddFileToGitInfoExclude = Add("tsmiAddFileToGitInfoExclude", "Text", "Add file to .git/info/exclude");
        SkipWorktree = Add("tsmiSkipWorktree", "Text", "S&kip worktree");
        AssumeUnchanged = Add("tsmiAssumeUnchanged", "Text", "Assu&me unchanged");
        StopTracking = Add("tsmiStopTracking", "Text", "Stop tracking this file");
        StopTrackingFail = Add("_stopTrackingFail", "Text", "Fail to stop tracking the file '{0}'.");
        SkipWorktreeToolTip = Add("_skipWorktreeToolTip", "Text", "Hide already tracked files that will change but that you don't want to commit."
            + Environment.NewLine + "Suitable for some config files modified locally.");
        AssumeUnchangedToolTip = Add("_assumeUnchangedToolTip", "Text", "Tell git to not check the status of this file for performance benefits."
            + Environment.NewLine + "Use this feature when a file is big and never change."
            + Environment.NewLine + "Git will never check if the file has changed that will improve status check performance.");
        UpdateSubmodule = Add("tsmiUpdateSubmodule", "Text", "&Update submodule");
        ResetSubmoduleChanges = Add("tsmiResetSubmoduleChanges", "Text", "R&eset submodule changes");
        StashSubmoduleChanges = Add("tsmiStashSubmoduleChanges", "Text", "S&tash submodule changes");
        CommitSubmoduleChanges = Add("tsmiCommitSubmoduleChanges", "Text", "&Commit submodule changes");
        RunScript = Add("tsmiRunScript", "Text", "Run script");
    }

    public TranslatedText OpenWithDifftool { get; }

    public TranslatedText DiffFirstToSelected { get; }

    public TranslatedText DiffFirstToLocal { get; }

    public TranslatedText DiffSelectedToLocal { get; }

    public TranslatedText OpenWorkingDirectoryFile { get; }

    public TranslatedText OpenWorkingDirectoryFileWith { get; }

    public TranslatedText EditWorkingDirectoryFile { get; }

    public TranslatedText OpenRevisionFile { get; }

    public TranslatedText OpenRevisionFileWith { get; }

    public TranslatedText SaveAs { get; }

    public TranslatedText CopyPaths { get; }

    public TranslatedText ShowInFolder { get; }

    public TranslatedText FileHistory { get; }

    public TranslatedText Blame { get; }

    public TranslatedText ResetFileTo { get; }

    public TranslatedText FirstRevision { get; }

    public TranslatedText SelectedRevision { get; }

    public TranslatedText MultipleDescription { get; }

    public TranslatedText ResetSelectedChanges { get; }

    public TranslatedText SaveFileFilterAllFiles { get; }

    public TranslatedText SaveFileFilterCurrentFormat { get; }

    public TranslatedText SortBy { get; }

    public TranslatedText GroupByFilePathTree { get; }

    public TranslatedText GroupByFilePathFlat { get; }

    public TranslatedText GroupByFileExtensionTree { get; }

    public TranslatedText GroupByFileExtensionFlat { get; }

    public TranslatedText GroupByFileStatusTree { get; }

    public TranslatedText GroupByFileStatusFlat { get; }

    public TranslatedText StageFile { get; }

    public TranslatedText UnstageFile { get; }

    public TranslatedText ResetChunkOfFile { get; }

    public TranslatedText InteractiveAdd { get; }

    public TranslatedText CherryPickChanges { get; }

    public TranslatedText DiffTwoSelected { get; }

    public TranslatedText RememberSecondRevDiff { get; }

    public TranslatedText RememberFirstRevDiff { get; }

    public TranslatedText DiffSelectedWithRememberedFile { get; }

    public TranslatedText OpenWithGitExtensions { get; }

    public TranslatedText OpenInVisualStudio { get; }

    public TranslatedText Move { get; }

    public TranslatedText NewName { get; }

    public TranslatedText DeleteSelectedFilesCaption { get; }

    public TranslatedText DeleteSelectedFiles { get; }

    public TranslatedText DeleteFailed { get; }

    public TranslatedText ShowInFileTree { get; }

    public TranslatedText FilterFileInGrid { get; }

    public TranslatedText FindFile { get; }

    public TranslatedText AddFileToGitIgnore { get; }

    public TranslatedText AddFileToGitInfoExclude { get; }

    public TranslatedText SkipWorktree { get; }

    public TranslatedText AssumeUnchanged { get; }

    public TranslatedText StopTracking { get; }

    public TranslatedText StopTrackingFail { get; }

    public TranslatedText SkipWorktreeToolTip { get; }

    public TranslatedText AssumeUnchangedToolTip { get; }

    public TranslatedText UpdateSubmodule { get; }

    public TranslatedText ResetSubmoduleChanges { get; }

    public TranslatedText StashSubmoduleChanges { get; }

    public TranslatedText CommitSubmoduleChanges { get; }

    public TranslatedText RunScript { get; }
}

/// <summary>Strings of the copy paths menu; ids match <c>CopyPathsToolStripMenuItem</c>, which is translated with <c>FormBrowse</c>.</summary>
public sealed class CopyPathsStrings : ViewStrings
{
    public CopyPathsStrings()
        : base("FormBrowse")
    {
        FullNative = Add("copyFullPathsNativeToolStripMenuItem", "Text", "Copy &full path(s) - native");
        FullWsl = Add("copyFullPathsWslToolStripMenuItem", "Text", "Copy full path(s) - &WSL");
        FullCygwin = Add("copyFullPathsCygwinToolStripMenuItem", "Text", "Copy full path(s) - &Cygwin");
        RelativeNative = Add("copyRelativePathsNativeToolStripMenuItem", "Text", "Copy relative path(s) - &native");
        RelativePosix = Add("copyRelativePathsPosixToolStripMenuItem", "Text", "Copy relative path(s) - &POSIX");
    }

    public TranslatedText FullNative { get; }

    public TranslatedText FullWsl { get; }

    public TranslatedText FullCygwin { get; }

    public TranslatedText RelativeNative { get; }

    public TranslatedText RelativePosix { get; }
}

/// <summary>The difftool comparisons of the menu (the WinForms <c>RevisionDiffKind</c>).</summary>
public enum DifftoolKind
{
    /// <summary>First (A) to second (B), <c>DiffAB</c>.</summary>
    FirstToSelected,

    /// <summary>First (A) to the working directory, <c>DiffALocal</c>.</summary>
    FirstToLocal,

    /// <summary>Second (B) to the working directory, <c>DiffBLocal</c>.</summary>
    SelectedToLocal,
}

/// <summary>How paths are copied (the items of <c>CopyPathsToolStripMenuItem</c>).</summary>
public enum CopyPathKind
{
    FullNative,
    FullWsl,
    FullCygwin,
    RelativeNative,
    RelativePosix,
}

/// <summary>The submodule items of the menu (<c>tsmiUpdateSubmodule</c>, <c>tsmiResetSubmoduleChanges</c>, ...).</summary>
public enum SubmoduleMenuAction
{
    Update,
    Reset,
    Stash,
    Commit,
}

/// <summary>A user script of the menu (<c>AddUserScripts</c>).</summary>
/// <param name="Name">The name of the script.</param>
/// <param name="Id">The identifier of the script for the host (its hotkey command).</param>
/// <param name="IsDirect">Whether it is shown in the menu itself (<c>ScriptEvent.ShowInFileList</c>), else under "Run script".</param>
public sealed record FileStatusScript(string Name, int Id, bool IsDirect);

/// <summary>
///  Which items of the menu are shown and enabled for the selection (as <c>FileStatusList.UpdateStatusOfMenuItems</c> decides
///  with <c>RevisionDiffController</c>).
/// </summary>
public sealed record FileStatusMenuState
{
    public static FileStatusMenuState None { get; } = new();

    /// <summary>Whether a submodule is selected, which "Open with Git Extensions" opens (<c>_NO_TRANSLATE_openSubmoduleMenuItem</c>).</summary>
    public bool ShowOpenSubmodule { get; init; }

    /// <summary>Whether a double click opens the submodule, which the item is bold for (<c>AppSettings.OpenSubmoduleDiffInSeparateWindow</c>).</summary>
    public bool IsOpenSubmoduleDefault { get; init; }

    public bool CanOpenWithDifftool { get; init; }

    public bool CanDiffFirstToSelected { get; init; }

    public bool CanDiffFirstToLocal { get; init; }

    public bool CanDiffSelectedToLocal { get; init; }

    /// <summary>Whether the comparisons to the working directory are shown (not for the working directory itself).</summary>
    public bool ShowDiffToLocal { get; init; } = true;

    public bool ShowOpenWorkingDirectoryFile { get; init; }

    public bool ShowEditWorkingDirectoryFile { get; init; }

    public bool ShowOpenRevisionFile { get; init; }

    public bool CanOpenRevisionFile { get; init; }

    /// <summary>Whether the system lets the user choose the application of a file ("Open ... with", Windows only).</summary>
    public bool CanOpenWith { get; init; }

    public bool ShowOpenWorkingDirectoryFileWith => ShowOpenWorkingDirectoryFile && CanOpenWith;

    public bool ShowOpenRevisionFileWith => ShowOpenRevisionFile && CanOpenWith;

    public bool ShowSaveAs { get; init; }

    public bool CanCopyPaths { get; init; }

    public bool ShowShowInFolder { get; init; }

    public bool CanShowInFolder { get; init; }

    public bool CanShowFileHistory { get; init; }

    public bool CanBlame { get; init; }

    public bool CanResetFileTo => ResetToSelectedText is not null || ResetToParentText is not null;

    /// <summary>The text of "reset to the second (B) revision", <see langword="null"/> if not possible.</summary>
    public string? ResetToSelectedText { get; init; }

    /// <summary>The text of "reset to the first (A) revision", <see langword="null"/> if not possible.</summary>
    public string? ResetToParentText { get; init; }

    /// <summary>Whether the submodule items are shown (<c>ShouldShowSubmoduleMenus</c>).</summary>
    public bool ShowSubmoduleItems { get; init; }

    public bool ShowStage { get; init; }

    public bool ShowUnstage { get; init; }

    /// <summary>Whether "Reset chunk of file" and "Interactive add" are shown (a single file of the working directory).</summary>
    public bool ShowResetChunkAndInteractiveAdd { get; init; }

    /// <summary>Whether "Cherry pick changes" is shown (<c>ShouldShowMenuCherryPick</c>, where the list can cherry-pick).</summary>
    public bool ShowCherryPick { get; init; }

    /// <summary>Whether the remember items of the difftool menu are shown (a single file).</summary>
    public bool ShowRememberDiff { get; init; }

    public bool CanRememberSecondRevDiff { get; init; }

    public bool CanRememberFirstRevDiff { get; init; }

    /// <summary>Whether "Diff the selected files" is shown (two files).</summary>
    public bool ShowDiffTwoSelected { get; init; }

    public bool CanDiffTwoSelected { get; init; }

    /// <summary>The text of "Diff with the remembered file", <see langword="null"/> if hidden.</summary>
    public string? DiffWithRememberedText { get; init; }

    public bool CanDiffWithRemembered { get; init; }

    /// <summary>Whether the separator before the remember items is shown (one or two files).</summary>
    public bool ShowDifftoolRememberSeparator => ShowRememberDiff || ShowDiffTwoSelected;

    public bool ShowOpenInVisualStudio { get; init; }

    public bool ShowMove { get; init; }

    /// <summary>The text of the delete item (for the number of files), <see langword="null"/> if hidden.</summary>
    public string? DeleteFileText { get; init; }

    public bool ShowDeleteFile => DeleteFileText is not null;

    /// <summary>Whether "Show in file tree" is shown (<c>ShouldShowMenuShowInFileTree</c>, where the list can show it).</summary>
    public bool ShowShowInFileTree { get; init; }

    /// <summary>Whether "Filter file in grid" is enabled (<c>ShouldShowMenuFileHistory</c>, where the list can filter).</summary>
    public bool CanFilterFileInGrid { get; init; }

    public bool ShowFindFile { get; init; }

    /// <summary>Whether the files can be added to .gitignore and .git/info/exclude.</summary>
    public bool ShowIgnore { get; init; }

    /// <summary>Whether "Skip worktree" and "Assume unchanged" are shown.</summary>
    public bool ShowSkipWorktreeAndAssumeUnchanged { get; init; }

    /// <summary>Whether "Skip worktree" is checked (a selected file is skip-worktree).</summary>
    public bool IsSkipWorktree { get; init; }

    /// <summary>Whether "Assume unchanged" is checked (a selected file is assumed unchanged).</summary>
    public bool IsAssumeUnchanged { get; init; }

    public bool ShowStopTracking { get; init; }

    public bool ShowIgnoreSeparator => ShowIgnore || ShowStopTracking;

    /// <summary>The user scripts (<c>AddUserScripts</c>).</summary>
    public IReadOnlyList<FileStatusScript> Scripts { get; init; } = [];

    /// <summary>Whether "Run script" has scripts (not shown in the menu itself).</summary>
    public bool HasRunScriptItems => Scripts.Any(script => !script.IsDirect);

    public bool HasScripts => Scripts.Count > 0;
}

/// <summary>The actions of the menu of the file status list that need the host (git, the shell, the other dialogs).</summary>
public interface IFileStatusListMenuHost
{
    /// <param name="selectedFolder">The selected folder, if a single folder is selected (as <c>FileStatusList.SelectedFolder</c>).</param>
    /// <param name="focused">The file selected last (as <c>FocusedItem</c>), the second of "Diff the selected files".</param>
    /// <param name="supportLinePatching">Whether the diff of the file supports line patching (<c>FileViewer.SupportLinePatching</c>).</param>
    FileStatusMenuState GetMenuState(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, FileStatusEntry? focused, bool supportLinePatching);

    /// <param name="customTool">The difftool of the submenu, else the default one.</param>
    void OpenWithDifftool(IReadOnlyList<FileStatusEntry> selected, DifftoolKind kind, string? customTool = null);

    void OpenWorkingDirectoryFile(FileStatusEntry entry, bool openWith);

    /// <summary>Edits the file in the file editor; returns whether the list should be refreshed.</summary>
    bool EditWorkingDirectoryFile(FileStatusEntry entry);

    void OpenRevisionFile(FileStatusEntry entry, bool openWith);

    void SaveAs(IReadOnlyList<FileStatusEntry> selected);

    void CopyPaths(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, CopyPathKind kind);

    void ShowInFolder(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder);

    void ShowFileHistory(FileStatusEntry? entry, RelativePath? selectedFolder, bool blame);

    /// <summary>
    ///  As <c>OpenSubmoduleAsync</c>: the submodule in a new instance of the application, with the commits of its diff
    ///  selected.
    /// </summary>
    void OpenSubmodule(FileStatusEntry entry);

    /// <summary>Whether a double click opens a submodule (<c>AppSettings.OpenSubmoduleDiffInSeparateWindow</c>), else its history.</summary>
    bool OpenSubmoduleOnDoubleClick { get; }

    /// <summary>Resets the files after a confirmation; returns whether the list should be refreshed.</summary>
    bool ResetFiles(IReadOnlyList<FileStatusEntry> selected, bool toParent);

    /// <summary>Stages the files of the working directory (as <c>StageFile_Click</c>).</summary>
    void StageFiles(IReadOnlyList<FileStatusEntry> selected);

    /// <summary>Unstages the files of the index (as <c>UnstageFile_Click</c>).</summary>
    void UnstageFiles(IReadOnlyList<FileStatusEntry> selected);

    /// <summary>As <c>ResetChunkOfFile_Click</c>: <c>git checkout -p</c> in a console.</summary>
    Task ResetChunkOfFileAsync(FileStatusEntry entry);

    /// <summary>As <c>InteractiveAdd_Click</c>: <c>git add -p</c> in a console.</summary>
    Task InteractiveAddAsync(FileStatusEntry entry);

    /// <summary>As <c>RememberFirstRevDiff_Click</c> (<paramref name="first"/>) and <c>RememberSecondRevDiff_Click</c>.</summary>
    void RememberDiff(FileStatusEntry entry, bool first);

    /// <summary>As <c>DiffWithRemembered_Click</c>.</summary>
    void DiffWithRemembered(FileStatusEntry entry, string? customTool = null);

    /// <summary>As <c>DiffTwoSelected_Click</c>.</summary>
    void DiffTwoSelected(IReadOnlyList<FileStatusEntry> selected, FileStatusEntry? focused, string? customTool = null);

    /// <summary>As <c>OpenInVisualStudio_Click</c>.</summary>
    void OpenInVisualStudio(FileStatusEntry entry);

    /// <summary>Renames or moves the file or folder (as <c>Move_Click</c>); returns whether the list should be refreshed.</summary>
    bool Move(FileStatusEntry? entry, RelativePath? selectedFolder);

    /// <summary>Deletes the files after a confirmation (as <c>DeleteFile_Click</c>); returns whether the list should be refreshed.</summary>
    bool DeleteFiles(IReadOnlyList<FileStatusEntry> selected);

    /// <summary>As <c>FindFile_Click</c>: the file chosen in the search window among the files of the list.</summary>
    GitItemStatus? FindFile(IReadOnlyList<GitItemStatus> candidates);

    /// <summary>As <c>EditGitIgnore_Click</c> and <c>EditLocallyIgnoredFiles_Click</c>; returns whether the list should be refreshed.</summary>
    bool EditGitIgnore(bool localExcludes);

    /// <summary>As <c>AddFileToIgnoreFile</c>; returns whether the list should be refreshed.</summary>
    bool AddToIgnoreFile(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, bool localExclude);

    /// <summary>As <c>SkipWorktree_Click</c>.</summary>
    void SetSkipWorktree(IReadOnlyList<FileStatusEntry> selected, bool skipWorktree);

    /// <summary>As <c>AssumeUnchanged_Click</c>.</summary>
    void SetAssumeUnchanged(IReadOnlyList<FileStatusEntry> selected, bool assumeUnchanged);

    /// <summary>As <c>StopTracking_Click</c>; returns whether the list should be refreshed.</summary>
    bool StopTracking(FileStatusEntry entry);

    /// <summary>The submodule items; returns whether the list should be refreshed.</summary>
    bool RunSubmoduleAction(IReadOnlyList<FileStatusEntry> selected, SubmoduleMenuAction action);

    /// <summary>Runs the script with the selected files (as <c>ExecuteCommand</c> of a script).</summary>
    void RunScript(FileStatusScript script, IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder);
}
