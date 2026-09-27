using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the merge conflicts dialog; ids match <c>FormResolveConflicts</c>.</summary>
public sealed class ResolveConflictsStrings : ViewStrings
{
    public ResolveConflictsStrings()
        : base("FormResolveConflicts")
    {
        Title = Add("$this", "Text", "Resolve merge conflicts");
        UnresolvedMergeConflicts = Add("label1", "Text", "Unresolved merge conflicts");
        FileNameHeader = Add("FileName", "HeaderText", "Filename");
        Merge = Add("merge", "Text", "Merge");
        SelectFile = Add("conflictDescription", "Text", "Select file");
        OpenMergeToolButton = Add("openMergeToolBtn", "Text", "Open in mergetool");
        StartMergeTool = Add("startMergetool", "Text", "Start mergetool");
        Rescan = Add("Rescan", "Text", "Rescan merge conflicts");
        Reset = Add("Reset", "Text", "&Reset");
        LocalCurrent = Add("labelLocalCurrent", "Text", "Local/current");
        Base = Add("labelBase", "Text", "Base");
        RemoteIncoming = Add("labelRemoteIncoming", "Text", "Remote/incoming");
        OpenMergeToolMenu = Add("OpenMergetool", "Text", "Open in mergetool");
        CustomMergeTool = Add("customMergetool", "Text", "Open in &mergetool");
        MarkAsSolved = Add("ContextMarkAsSolved", "Text", "Mark conflict as solved");
        ChooseLocal = Add("ContextChooseLocal", "Text", "Choose local");
        ChooseRemote = Add("ContextChooseRemote", "Text", "Choose remote");
        ChooseBase = Add("ContextChooseBase", "Text", "Choose base");
        OpenLocalWith = Add("ContextOpenLocalWith", "Text", "Open local with");
        OpenRemoteWith = Add("ContextOpenRemoteWith", "Text", "Open remote with");
        OpenBaseWith = Add("ContextOpenBaseWith", "Text", "Open base with");
        SaveLocalAs = Add("ContextSaveLocalAs", "Text", "Save local as");
        SaveRemoteAs = Add("ContextSaveRemoteAs", "Text", "Save remote as");
        SaveBaseAs = Add("ContextSaveBaseAs", "Text", "Save base as");
        Open = Add("openToolStripMenuItem", "Text", "Open");
        OpenWith = Add("openWithToolStripMenuItem", "Text", "Open With");
        ShowInFolder = Add("openFolderToolStripMenuItem", "Text", "Show in folder");
        FileHistory = Add("fileHistoryToolStripMenuItem", "Text", "File history");
        AbortCurrentOperation = Add("_abortCurrentOperation", "Text", "You can abort the current conflict resolution by resetting hard." + Environment.NewLine + "All changes since the last commit will be deleted." + Environment.NewLine + Environment.NewLine + "Do you want to reset the changes?");
        AllConflictsResolved = Add("_allConflictsResolved", "Text", "All merge conflicts are resolved, you can commit." + Environment.NewLine + "Do you want to commit now?");
        AllConflictsResolvedCaption = Add("_allConflictsResolvedCaption", "Text", "Commit");
        AllFilesFilter = Add("_allFilesFilter", "Text", "All files (*.*)");
        AreYouSureYouWantDeleteFiles = Add("_areYouSureYouWantDeleteFiles", "Text", "Are you sure you want to DELETE all changes?" + Environment.NewLine + Environment.NewLine + "This action cannot be made undone.");
        AreYouSureYouWantDeleteFilesCaption = Add("_areYouSureYouWantDeleteFilesCaption", "Text", "WARNING!");
        AskMergeConflictSolved = Add("_askMergeConflictSolved", "Text", "Is the merge conflict solved?");
        AskMergeConflictSolvedAfterCustomMergeScript = Add("_askMergeConflictSolvedAfterCustomMergeScript", "Text", "The merge conflict need to be solved and the result must be saved as:" + Environment.NewLine + "{0}" + Environment.NewLine + Environment.NewLine + "Is the merge conflict solved?");
        AskMergeConflictSolvedCaption = Add("_askMergeConflictSolvedCaption", "Text", "Conflict solved?");
        OpenInButton = Add("_button1Text", "Text", "Open in");
        ChangesLocalMergeTooltip = Add("_changesLocalMergeTooltip", "Text", "Changes from the current branch");
        ChangesLocalRebaseTooltip = Add("_changesLocalRebaseTooltip", "Text", "Changes from the branch you are rebasing onto");
        ChangesRemoteMergeTooltip = Add("_changesRemoteMergeTooltip", "Text", "Changes from the branch you are merging");
        ChangesRemoteRebaseTooltip = Add("_changesRemoteRebaseTooltip", "Text", "Changes from the branch you are rebasing");
        ChangesTakeOnlyLocalMergeTooltip = Add("_changesTakeOnlyLocalMergeTooltip", "Text", "Take only the changes from the current branch");
        ChangesTakeOnlyLocalRebaseTooltip = Add("_changesTakeOnlyLocalRebaseTooltip", "Text", "Take only the changes from the branch you are rebasing onto");
        ChangesTakeOnlyRemoteMergeTooltip = Add("_changesTakeOnlyRemoteMergeTooltip", "Text", "Take only the changes from the branch you are merging");
        ChangesTakeOnlyRemoteRebaseTooltip = Add("_changesTakeOnlyRemoteRebaseTooltip", "Text", "Take only the changes from the branch you are rebasing");
        ChooseBaseFileFailedText = Add("_chooseBaseFileFailedText", "Text", "Choose base file failed.");
        ChooseLocalButtonText = Add("_chooseLocalButtonText", "Text", "Choose local");
        ChooseLocalFileFailedText = Add("_chooseLocalFileFailedText", "Text", "Choose local file failed.");
        ChooseRemoteButtonText = Add("_chooseRemoteButtonText", "Text", "Choose remote");
        ChooseRemoteFileFailedText = Add("_chooseRemoteFileFailedText", "Text", "Choose remote file failed.");
        ContextChooseBaseTooltip = Add("_contextChooseBaseTooltip", "Text", "Take no changes and revert to base content!");
        ContextChooseLocalMergeText = Add("_contextChooseLocalMergeText", "Text", "Choose local/current (ours)");
        ContextChooseLocalRebaseText = Add("_contextChooseLocalRebaseText", "Text", "Choose local/current (theirs)");
        ContextChooseRemoteMergeText = Add("_contextChooseRemoteMergeText", "Text", "Choose remote/incoming (theirs)");
        ContextChooseRemoteRebaseText = Add("_contextChooseRemoteRebaseText", "Text", "Choose remote/incoming (ours)");
        CurrentFormatFilter = Add("_currentFormatFilter", "Text", "Current format (*.{0})");
        DeleteFileButtonText = Add("_deleteFileButtonText", "Text", "Delete file");
        Deleted = Add("_deleted", "Text", "deleted");
        ErrorStartingMergetool = Add("_errorStartingMergetool", "Text", "Error starting mergetool: {0}");
        FailureWhileOpenFile = Add("_failureWhileOpenFile", "Text", "Open temporary file failed.");
        FailureWhileSaveFile = Add("_failureWhileSaveFile", "Text", "Save file failed.");
        FileBinaryChooseLocalBaseRemote = Add("_fileBinaryChooseLocalBaseRemote", "Text", "File '{0}' appears to be binary." + Environment.NewLine + "Choose to keep the local '{1}', remote '{2}' or base file.");
        FileChangeLocallyAndRemotely = Add("_fileChangeLocallyAndRemotely", "Text", "The file has been changed both locally ({0}) and remotely ({1}). Merge the changes.");
        FileCreatedLocallyAndRemotely = Add("_fileCreatedLocallyAndRemotely", "Text", "A file with the same name has been created locally ({0}) and remotely ({1}). Choose the file you want to keep or merge the files.");
        FileCreatedLocallyAndRemotelyLong = Add("_fileCreatedLocallyAndRemotelyLong", "Text", "File '{0}' does not have a base revision." + Environment.NewLine + "A file with the same name has been created locally ({1}) and remotely ({2}) causing this conflict." + Environment.NewLine + Environment.NewLine + "Choose the file you want to keep, merge the files or delete the file?");
        FileDeletedLocallyAndRemotely = Add("_fileDeletedLocallyAndRemotely", "Text", "The file has been deleted both locally ({0}) and remotely ({1}).");
        FileDeletedLocallyAndModifiedRemotely = Add("_fileDeletedLocallyAndModifiedRemotely", "Text", "The file has been deleted locally ({0}) and modified remotely ({1}). Choose to delete the file or keep the modified version.");
        FileDeletedLocallyAndModifiedRemotelyLong = Add("_fileDeletedLocallyAndModifiedRemotelyLong", "Text", "File '{0}' does not have a local revision." + Environment.NewLine + "The file has been deleted locally ({1}) but modified remotely ({2})." + Environment.NewLine + Environment.NewLine + "Choose to delete the file or keep the modified version.");
        FileIsBinary = Add("_fileIsBinary", "Text", "The selected file appears to be a binary file." + Environment.NewLine + "Are you sure you want to open this file in {0}?");
        FileModifiedLocallyAndDeletedRemotely = Add("_fileModifiedLocallyAndDeletedRemotely", "Text", "The file has been modified locally ({0}) and deleted remotely ({1}). Choose to delete the file or keep the modified version.");
        FileModifiedLocallyAndDeletedRemotelyLong = Add("_fileModifiedLocallyAndDeletedRemotelyLong", "Text", "File '{0}' does not have a remote revision." + Environment.NewLine + "The file has been modified locally ({1}) but deleted remotely ({2})." + Environment.NewLine + Environment.NewLine + "Choose to delete the file or keep the modified version.");
        FileUnchangedAfterMerge = Add("_fileUnchangedAfterMerge", "Text", "The file has not been modified by the merge. Usually this means that the file has been saved to the wrong location." + Environment.NewLine + Environment.NewLine + "The merge conflict will not be marked as solved. Please try again.");
        FilesDeletedLocallyAndModifiedRemotelyLong = Add("_filesDeletedLocallyAndModifiedRemotelyLong", "Text", "'{0}' and {1} other out of {2} selected files do not have a local revision." + Environment.NewLine + "The files have been deleted locally, but modified remotely" + Environment.NewLine + Environment.NewLine + "Choose to delete the files or keep the modified versions.");
        FilesDeletedLocallyAndModifiedRemotelyLongNoOtherFilesSelected = Add("_filesDeletedLocallyAndModifiedRemotelyLongNoOtherFilesSelected", "Text", "'{0}' and {1} other selected file(s) do not have a local revision." + Environment.NewLine + "The files have been deleted locally, but modified remotely" + Environment.NewLine + Environment.NewLine + "Choose to delete the files or keep the modified versions.");
        FilesModifiedLocallyAndDeletedRemotelyLong = Add("_filesModifiedLocallyAndDeletedRemotelyLong", "Text", "'{0}' and {1} other out of {2} selected files do not have a remote revision." + Environment.NewLine + "The files have been modified locally, but deleted remotely." + Environment.NewLine + Environment.NewLine + "Choose to delete the files or keep the modified versions.");
        FilesModifiedLocallyAndDeletedRemotelyLongNoOtherFilesSelected = Add("_filesModifiedLocallyAndDeletedRemotelyLongNoOtherFilesSelected", "Text", "'{0}' and {1} other selected file(s) do not have a remote revision." + Environment.NewLine + "The files have been modified locally, but deleted remotely." + Environment.NewLine + Environment.NewLine + "Choose to delete the files or keep the modified versions.");
        KeepBaseButtonText = Add("_keepBaseButtonText", "Text", "Keep base file");
        KeepModifiedButtonText = Add("_keepModifiedButtonText", "Text", "Keep modified");
        NoBase = Add("_noBase", "Text", "no base");
        NoBaseFileMergeCaption = Add("_noBaseFileMergeCaption", "Text", "Merge");
        NoBaseRevision = Add("_noBaseRevision", "Text", "There is no base revision for '{0}'." + Environment.NewLine + "Fall back to 2-way merge?");
        NoMergeTool = Add("_noMergeTool", "Text", "There is no mergetool configured." + Environment.NewLine + "Please go to settings and set a mergetool!");
        NoMergeToolConfigured = Add("_noMergeToolConfigured", "Text", "The mergetool is not correctly configured." + Environment.NewLine + "Please go to settings and configure the mergetool!");
        OpenInMenu = Add("_openMergeToolItemText", "Text", "Open in");
        Ours = Add("_ours", "Text", "ours");
        ResetCaption = Add("_resetCaption", "Text", "Reset");
        SolveMergeConflictApplyToAllCheckBoxText = Add("_solveMergeConflictApplyToAllCheckBoxText", "Text", "Apply to '{0}' and {1} other file(s)");
        SolveMergeConflictDialogCaption = Add("_solveMergeConflictDialogCaption", "Text", "Solve merge conflict");
        StageFilename = Add("_stageFilename", "Text", "Stage '{0}'");
        Theirs = Add("_theirs", "Text", "theirs");
        UseCustomMergeScript = Add("_uskUseCustomMergeScript", "Text", "There is a custom merge script ({0}) for this file type." + Environment.NewLine + Environment.NewLine + "Do you want to use this custom merge script?");
        UseCustomMergeScriptCaption = Add("_uskUseCustomMergeScriptCaption", "Text", "Custom merge script");
        Help = Add("linkLabelHelp", "Text", "Help", category: "GotoUserManualControl");
        HelpTooltip = Add("_gotoUserManualControlTooltip", "Text", "Read more about this feature at {0}", category: "GotoUserManualControl");
    }

    public TranslatedText Title { get; }

    public TranslatedText UnresolvedMergeConflicts { get; }

    public TranslatedText FileNameHeader { get; }

    public TranslatedText Merge { get; }

    public TranslatedText SelectFile { get; }

    public TranslatedText OpenMergeToolButton { get; }

    public TranslatedText StartMergeTool { get; }

    public TranslatedText Rescan { get; }

    public TranslatedText Reset { get; }

    public TranslatedText LocalCurrent { get; }

    public TranslatedText Base { get; }

    public TranslatedText RemoteIncoming { get; }

    public TranslatedText OpenMergeToolMenu { get; }

    public TranslatedText CustomMergeTool { get; }

    public TranslatedText MarkAsSolved { get; }

    public TranslatedText ChooseLocal { get; }

    public TranslatedText ChooseRemote { get; }

    public TranslatedText ChooseBase { get; }

    public TranslatedText OpenLocalWith { get; }

    public TranslatedText OpenRemoteWith { get; }

    public TranslatedText OpenBaseWith { get; }

    public TranslatedText SaveLocalAs { get; }

    public TranslatedText SaveRemoteAs { get; }

    public TranslatedText SaveBaseAs { get; }

    public TranslatedText Open { get; }

    public TranslatedText OpenWith { get; }

    public TranslatedText ShowInFolder { get; }

    public TranslatedText FileHistory { get; }

    public TranslatedText AbortCurrentOperation { get; }

    public TranslatedText AllConflictsResolved { get; }

    public TranslatedText AllConflictsResolvedCaption { get; }

    public TranslatedText AllFilesFilter { get; }

    public TranslatedText AreYouSureYouWantDeleteFiles { get; }

    public TranslatedText AreYouSureYouWantDeleteFilesCaption { get; }

    public TranslatedText AskMergeConflictSolved { get; }

    public TranslatedText AskMergeConflictSolvedAfterCustomMergeScript { get; }

    public TranslatedText AskMergeConflictSolvedCaption { get; }

    public TranslatedText OpenInButton { get; }

    public TranslatedText ChangesLocalMergeTooltip { get; }

    public TranslatedText ChangesLocalRebaseTooltip { get; }

    public TranslatedText ChangesRemoteMergeTooltip { get; }

    public TranslatedText ChangesRemoteRebaseTooltip { get; }

    public TranslatedText ChangesTakeOnlyLocalMergeTooltip { get; }

    public TranslatedText ChangesTakeOnlyLocalRebaseTooltip { get; }

    public TranslatedText ChangesTakeOnlyRemoteMergeTooltip { get; }

    public TranslatedText ChangesTakeOnlyRemoteRebaseTooltip { get; }

    public TranslatedText ChooseBaseFileFailedText { get; }

    public TranslatedText ChooseLocalButtonText { get; }

    public TranslatedText ChooseLocalFileFailedText { get; }

    public TranslatedText ChooseRemoteButtonText { get; }

    public TranslatedText ChooseRemoteFileFailedText { get; }

    public TranslatedText ContextChooseBaseTooltip { get; }

    public TranslatedText ContextChooseLocalMergeText { get; }

    public TranslatedText ContextChooseLocalRebaseText { get; }

    public TranslatedText ContextChooseRemoteMergeText { get; }

    public TranslatedText ContextChooseRemoteRebaseText { get; }

    public TranslatedText CurrentFormatFilter { get; }

    public TranslatedText DeleteFileButtonText { get; }

    public TranslatedText Deleted { get; }

    public TranslatedText ErrorStartingMergetool { get; }

    public TranslatedText FailureWhileOpenFile { get; }

    public TranslatedText FailureWhileSaveFile { get; }

    public TranslatedText FileBinaryChooseLocalBaseRemote { get; }

    public TranslatedText FileChangeLocallyAndRemotely { get; }

    public TranslatedText FileCreatedLocallyAndRemotely { get; }

    public TranslatedText FileCreatedLocallyAndRemotelyLong { get; }

    public TranslatedText FileDeletedLocallyAndRemotely { get; }

    public TranslatedText FileDeletedLocallyAndModifiedRemotely { get; }

    public TranslatedText FileDeletedLocallyAndModifiedRemotelyLong { get; }

    public TranslatedText FileIsBinary { get; }

    public TranslatedText FileModifiedLocallyAndDeletedRemotely { get; }

    public TranslatedText FileModifiedLocallyAndDeletedRemotelyLong { get; }

    public TranslatedText FileUnchangedAfterMerge { get; }

    public TranslatedText FilesDeletedLocallyAndModifiedRemotelyLong { get; }

    public TranslatedText FilesDeletedLocallyAndModifiedRemotelyLongNoOtherFilesSelected { get; }

    public TranslatedText FilesModifiedLocallyAndDeletedRemotelyLong { get; }

    public TranslatedText FilesModifiedLocallyAndDeletedRemotelyLongNoOtherFilesSelected { get; }

    public TranslatedText KeepBaseButtonText { get; }

    public TranslatedText KeepModifiedButtonText { get; }

    public TranslatedText NoBase { get; }

    public TranslatedText NoBaseFileMergeCaption { get; }

    public TranslatedText NoBaseRevision { get; }

    public TranslatedText NoMergeTool { get; }

    public TranslatedText NoMergeToolConfigured { get; }

    public TranslatedText OpenInMenu { get; }

    public TranslatedText Ours { get; }

    public TranslatedText ResetCaption { get; }

    public TranslatedText SolveMergeConflictApplyToAllCheckBoxText { get; }

    public TranslatedText SolveMergeConflictDialogCaption { get; }

    public TranslatedText StageFilename { get; }

    public TranslatedText Theirs { get; }

    public TranslatedText UseCustomMergeScript { get; }

    public TranslatedText UseCustomMergeScriptCaption { get; }

    public TranslatedText Help { get; }

    public TranslatedText HelpTooltip { get; }
}

/// <summary>The hotkey commands of the merge conflicts dialog (<c>HotkeyCommands.ResolveConflicts</c>).</summary>
public enum ResolveConflictsHotkeyCommand
{
    Merge = 0,
    Rescan = 1,
    ChooseRemote = 2,
    ChooseLocal = 3,
    ChooseBase = 4,
}

/// <summary>What a conflicted path is in the working directory (<c>FormResolveConflicts.ItemType</c>).</summary>
public enum ConflictItemType
{
    File,
    Directory,
    Submodule,
}

/// <summary>A side of a conflict, as git names it for <c>GitModule.HandleConflictSelectSide</c>.</summary>
public enum ConflictSide
{
    Base,
    Local,
    Remote,
}

/// <summary>The choice in the "Solve merge conflict" task dialog (<c>ConflictResolutionPreference</c>).</summary>
public enum ConflictResolutionChoice
{
    None = 0,
    KeepLocal = 1,
    KeepRemote = 2,
    KeepBase = 3,
}

/// <summary>The "Solve merge conflict" task dialog (<c>CreateSolveMergeConflictTaskDialogPage</c>).</summary>
/// <param name="ApplyToAllText">The text of the "apply to all" check box; empty to hide it.</param>
public sealed record SolveConflictQuestion(
    string Text,
    string Heading,
    string Caption,
    string ApplyToAllText,
    string KeepLocalText,
    string KeepRemoteText,
    string KeepBaseText);

/// <summary>The answer to a <see cref="SolveConflictQuestion"/>.</summary>
/// <param name="ApplyToAll">Whether the "apply to all" check box is checked.</param>
public readonly record struct SolveConflictAnswer(ConflictResolutionChoice Choice, bool ApplyToAll);

/// <summary>A conflicted file of the list (a row of <c>ConflictedFiles</c>).</summary>
public sealed class ConflictItem(ConflictData data)
{
    public ConflictData Data { get; } = data;

    public string Filename => Data.Filename;

    public override string ToString() => Filename;
}

/// <summary>Operations of the merge conflicts dialog that need the host (git, the file system, the other dialogs).</summary>
public interface IResolveConflictsHost
{
    bool InTheMiddleOfRebase();

    bool InTheMiddleOfConflictedMerge();

    bool InTheMiddleOfPatch();

    /// <summary>The conflicted files (<c>GitModule.GetConflictsAsync</c>).</summary>
    IReadOnlyList<ConflictData> GetConflicts();

    /// <summary>As the start of <c>InitMergetool</c>: <c>merge.guitool</c> if git supports it, else <c>merge.tool</c>.</summary>
    string? GetMergeTool();

    /// <summary>The effective value of a git setting of the native git (e.g. <c>mergetool.kdiff3.cmd</c>).</summary>
    string? GetEffectiveSetting(string name);

    /// <summary><c>PathUtil.TryFindFullPath</c>: the full path of an executable, or <see langword="null"/> if not found.</summary>
    string? FindFullPath(string? path);

    /// <summary>
    ///  The path and the merge command of a tool Git Extensions knows (<c>DiffMergeToolConfigurationManager</c>), for a tool
    ///  git knows by its name only (<c>merge.tool</c> without <c>mergetool.&lt;tool&gt;.path</c>); <see langword="null"/> otherwise.
    /// </summary>
    (string Path, string Command)? GetKnownMergeTool(string tool) => null;

    /// <summary>The custom merge tools, for the "Open in mergetool" sub menu (<c>CustomDiffMergeToolProvider</c>).</summary>
    Task<IReadOnlyList<string>> GetCustomMergeToolsAsync(CancellationToken cancellationToken);

    /// <summary>As <c>GetItemType</c>.</summary>
    ConflictItemType GetItemType(string fileName);

    /// <summary><c>GitModule.HandleConflictSelectSide</c>: checks out a side and stages it; returns <see langword="false"/> on failure.</summary>
    bool ChooseSide(string fileName, ConflictSide side);

    /// <summary><c>git rm -- file</c>.</summary>
    void RemoveFile(string fileName);

    /// <summary>As <c>StageFile</c>: <c>git add -- file</c>, showing the error dialog if git fails.</summary>
    void StageFile(string fileName, string errorTitle);

    /// <summary>Runs <c>git mergetool</c> (for the file if given, else all) in the background, with a custom tool if given.</summary>
    Task RunMergeToolAsync(string? fileName, string? customTool);

    /// <summary>Opens the submodule conflict dialog (<c>FormMergeSubmodule</c>); returns whether it was accepted.</summary>
    bool MergeSubmodule(string fileName);

    /// <summary><c>GitModule.CheckoutConflictedFiles</c>: the temporary files of the sides.</summary>
    (string? BaseFile, string? LocalFile, string? RemoteFile) CheckoutConflictedFiles(ConflictData conflict);

    /// <summary>Deletes a temporary file, if it exists.</summary>
    void DeleteTemporaryFile(string? path);

    /// <summary><c>FileHelper.IsBinaryFileName</c>.</summary>
    bool IsBinaryFile(string fileName);

    /// <summary>The full path of a merge script of the <c>Diff-Scripts</c> folder (Windows only), or <see langword="null"/> if there is none.</summary>
    string? GetMergeScriptPath(string scriptName);

    /// <summary>The full native path of a file of the working directory.</summary>
    string GetFullPath(string fileName);

    /// <summary>The last write time of a file of the working directory, or <see langword="null"/> if it does not exist.</summary>
    DateTime? GetLastWriteTime(string fileName);

    /// <summary>As <c>UseMergeWithScript</c>: starts <c>wscript</c> with the merge script and the files.</summary>
    void StartMergeScript(string mergeScript, string filePath, string? remoteFile, string? localFile, string? baseFile);

    /// <summary>Runs the configured merge tool; returns its exit code, or <see langword="null"/> if it could not be started.</summary>
    Task<int?> RunMergeToolProcessAsync(string path, string arguments);

    /// <summary><c>GitModule.HandleConflictsSaveSide</c>: saves a side to a file; returns <see langword="false"/> on failure.</summary>
    bool SaveSide(string fileName, string targetFile, ConflictSide side);

    /// <summary>The temporary file for opening a side (<c>Path.GetTempPath() + file name</c>).</summary>
    string GetTemporaryPath(string fileName);

    /// <summary>As the save dialog of <c>SaveAs</c>: the file to save a side to, or <see langword="null"/> if cancelled.</summary>
    string? ChooseSaveFile(string fileName);

    /// <summary><c>OsShellUtil.Open</c>.</summary>
    void Open(string path);

    /// <summary><c>OsShellUtil.OpenAs</c>.</summary>
    void OpenWith(string path);

    /// <summary>Whether <see cref="OpenWith"/> lets the user choose the application (<c>OsShellUtil.CanOpenAs</c>, Windows only).</summary>
    bool CanOpenWith => true;

    /// <summary><c>OsShellUtil.SelectPathInFileExplorer</c>.</summary>
    void ShowInFolder(string path);

    /// <summary>Opens the file history (<c>StartFileHistoryDialog</c>).</summary>
    void ShowFileHistory(string fileName);

    /// <summary>The "solve merge conflict" task dialog, with its three command links and "apply to all" check box.</summary>
    SolveConflictAnswer AskSolveConflict(SolveConflictQuestion question);

    /// <summary>As the second question of <c>ShowAbortMessage</c> (suppressible by <c>DontConfirmSecondAbortConfirmation</c>).</summary>
    bool ConfirmDeleteAllChanges(string text, string caption);

    /// <summary><c>git reset --hard</c>.</summary>
    void ResetHard();

    /// <summary>As <c>UICommands.UpdateSubmodules</c>.</summary>
    void UpdateSubmodules();

    /// <summary>The commit question, suppressible by <c>DontConfirmCommitAfterConflictsResolved</c>.</summary>
    bool ConfirmCommit(string text, string caption);

    /// <summary>Opens the commit dialog (<c>StartCommitDialog</c>).</summary>
    void StartCommit();

    void OpenUrl(string url);
}

/// <summary>View model of the merge conflicts dialog (port of <c>FormResolveConflicts</c>).</summary>
public sealed partial class ResolveConflictsViewModel : DialogViewModel
{
    /// <summary>The merge scripts of the <c>Diff-Scripts</c> folder, by extension (<c>_mergeScripts</c>).</summary>
    private static readonly Dictionary<string, string> _mergeScripts = new()
    {
        { ".doc", "merge-doc.js" },
        { ".docx", "merge-doc.js" },
        { ".docm", "merge-doc.js" },
        { ".ods", "merge-ods.vbs" },
        { ".odt", "merge-ods.vbs" },
        { ".sxw", "merge-ods.vbs" },
    };

    private readonly IResolveConflictsHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly bool _offerCommit;
    private readonly Func<string> _manualUrl;
    private readonly string _errorCaption;
    private readonly string _warningCaption;
    private readonly CancellationTokenSource _customMergeToolsCancellation = new();

    private bool _thereWhereMergeConflicts;
    private bool _inTheMiddleOfRebase;
    private bool _isClosed;
    private string? _mergeToolCommand;
    private string? _mergeToolPath;
    private IReadOnlyList<ConflictItem> _selectedConflicts = [];

    // The state of the "solve merge conflict" questions over the selected files (as the fields of FormResolveConflicts).
    private ConflictResolutionChoice _solveMergeConflictDialogResult;
    private bool _solveMergeConflictApplyToAll;
    private string _solveMergeConflictDialogCheckboxText = "";
    private int _filesDeletedLocallyAndModifiedRemotelyCount;
    private int _filesModifiedLocallyAndDeletedRemotelyCount;
    private int _filesRemainedCount;
    private int _filesDeletedLocallyAndModifiedRemotelySolved;
    private int _filesModifiedLocallyAndDeletedRemotelySolved;
    private int _conflictItemsCount;

    /// <param name="offerCommit">Whether to offer to commit once all conflicts are resolved (the argument of <c>FormResolveConflicts</c>).</param>
    /// <param name="manualUrl">The URL of the section of the user manual (<c>gotoUserManualControl1</c>).</param>
    public ResolveConflictsViewModel(
        ResolveConflictsStrings strings,
        IResolveConflictsHost host,
        IMessageBoxService messageBoxes,
        bool offerCommit,
        Func<string> manualUrl,
        string errorCaption,
        string warningCaption)
    {
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
        _offerCommit = offerCommit;
        _manualUrl = manualUrl;
        _errorCaption = errorCaption;
        _warningCaption = warningCaption;

        OpenMergeToolButtonText = strings.OpenMergeToolButton.PlainText;
        OpenMergeToolMenuText = strings.OpenMergeToolMenu.PlainText;
        ChooseLocalText = strings.ChooseLocal.PlainText;
        ChooseRemoteText = strings.ChooseRemote.PlainText;
        LocalLabel = strings.LocalCurrent.Text;
        RemoteLabel = strings.RemoteIncoming.Text;
        ConflictDescription = strings.SelectFile.Text;
    }

    public ResolveConflictsStrings Strings { get; }

    public string HelpTooltip => string.Format(Strings.HelpTooltip.Text, _manualUrl());

    /// <summary>The conflicted files (<c>ConflictedFiles</c>).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<ConflictItem> Conflicts { get; private set; } = [];

    /// <summary>The file to select in the list (set by the view model after a refresh, and by the view).</summary>
    [ObservableProperty]
    public partial ConflictItem? SelectedConflict { get; set; }

    /// <summary>The selected files, in the order of the list; set by the view.</summary>
    public IReadOnlyList<ConflictItem> SelectedConflicts
    {
        get => _selectedConflicts;
        set
        {
            _selectedConflicts = [.. Conflicts.Where(value.Contains)];
            UpdateConflictedFilesMenu();
        }
    }

    /// <summary>The configured merge tool (<c>_mergetool</c>).</summary>
    [ObservableProperty]
    public partial string? MergeTool { get; private set; }

    /// <summary>The text of <c>openMergeToolBtn</c>: "Open in" and the merge tool.</summary>
    [ObservableProperty]
    public partial string OpenMergeToolButtonText { get; private set; }

    /// <summary>The text of <c>OpenMergetool</c>: "Open in" and the merge tool.</summary>
    [ObservableProperty]
    public partial string OpenMergeToolMenuText { get; private set; }

    /// <summary>The custom merge tools, listed under "Open in mergetool" if there are several (the first one is the default).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomMergeTools))]
    public partial IReadOnlyList<string> CustomMergeTools { get; private set; } = [];

    public bool HasCustomMergeTools => CustomMergeTools.Count > 1;

    [ObservableProperty]
    public partial string ChooseLocalText { get; private set; }

    [ObservableProperty]
    public partial string? ChooseLocalToolTip { get; private set; }

    [ObservableProperty]
    public partial string ChooseRemoteText { get; private set; }

    [ObservableProperty]
    public partial string? ChooseRemoteToolTip { get; private set; }

    public string ChooseBaseToolTip => Strings.ContextChooseBaseTooltip.Text;

    /// <summary><c>labelLocalCurrent</c>, with "ours" or "theirs".</summary>
    [ObservableProperty]
    public partial string LocalLabel { get; private set; }

    [ObservableProperty]
    public partial string? LocalLabelToolTip { get; private set; }

    /// <summary><c>labelRemoteIncoming</c>, with "theirs" or "ours".</summary>
    [ObservableProperty]
    public partial string RemoteLabel { get; private set; }

    [ObservableProperty]
    public partial string? RemoteLabelToolTip { get; private set; }

    [ObservableProperty]
    public partial string ConflictDescription { get; private set; }

    [ObservableProperty]
    public partial string BaseFileName { get; private set; } = "...";

    [ObservableProperty]
    public partial string LocalFileName { get; private set; } = "...";

    [ObservableProperty]
    public partial string RemoteFileName { get; private set; } = "...";

    /// <summary>As <c>SetAvailableCommands</c>: the commands on one file, enabled for a single selected file.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenLocal), nameof(CanOpenRemote), nameof(CanOpenBase))]
    public partial bool IsSingleFileSelected { get; private set; }

    /// <summary><c>OpenMergetool</c> and <c>openMergeToolBtn</c>: a single file and the merge tool path or command configured.</summary>
    [ObservableProperty]
    public partial bool CanOpenMergeTool { get; private set; }

    /// <summary>Whether files are selected: the menu does not open without (<c>ConflictedFilesContextMenu_Opening</c>).</summary>
    public bool HasSelection => SelectedConflicts.Count > 0;

    /// <summary>As <c>DisableInvalidEntriesInConflictedFilesContextMenu</c>: open / save the local side if there is one.</summary>
    /// <summary>Whether "Open With" is offered (<see cref="IResolveConflictsHost.CanOpenWith"/>).</summary>
    public bool CanOpenWith => _host.CanOpenWith;

    public bool CanOpenLocal => IsSingleFileSelected && !string.IsNullOrEmpty(SelectedConflicts[0].Data.Local.Filename);

    public bool CanOpenRemote => IsSingleFileSelected && !string.IsNullOrEmpty(SelectedConflicts[0].Data.Remote.Filename);

    public bool CanOpenBase => IsSingleFileSelected && !string.IsNullOrEmpty(SelectedConflicts[0].Data.Base.Filename);

    /// <summary>As <c>FormBusyScope</c>: the dialog is disabled while a merge tool runs.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;

    [ObservableProperty]
    public partial bool IsProgressVisible { get; private set; }

    [ObservableProperty]
    public partial int ProgressMaximum { get; private set; }

    [ObservableProperty]
    public partial int ProgressValue { get; private set; }

    /// <summary>As <c>OnRuntimeLoad</c> and <c>FormResolveConflicts_Load</c>.</summary>
    public void InitializeView()
    {
        _thereWhereMergeConflicts = _host.InTheMiddleOfConflictedMerge();
        InitMergeTool();
        Initialize();
        _ = LoadCustomMergeToolsAsync();
    }

    public override bool ExecuteHotkeyCommand(int commandCode)
    {
        if (IsBusy)
        {
            return false;
        }

        switch ((ResolveConflictsHotkeyCommand)commandCode)
        {
            case ResolveConflictsHotkeyCommand.Merge:
                MergeCommand.Execute(null);
                return true;
            case ResolveConflictsHotkeyCommand.Rescan:
                Rescan();
                return true;
            case ResolveConflictsHotkeyCommand.ChooseBase:
                ChooseBase();
                return true;
            case ResolveConflictsHotkeyCommand.ChooseLocal:
                ChooseLocal();
                return true;
            case ResolveConflictsHotkeyCommand.ChooseRemote:
                ChooseRemote();
                return true;
            default:
                return base.ExecuteHotkeyCommand(commandCode);
        }
    }

    /// <summary>As <c>Rescan_Click</c>.</summary>
    [RelayCommand]
    private void Rescan() => Initialize();

    /// <summary>As <c>merge_Click</c>, <c>OpenMergetool_Click</c>, the double click and Enter on the list: <c>OpenMergeTool</c>.</summary>
    [RelayCommand]
    private async Task MergeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            (IReadOnlyList<ConflictItem> _,
                IReadOnlyList<ConflictItem> filesDeletedLocallyAndModifiedRemotely,
                IReadOnlyList<ConflictItem> filesModifiedLocallyAndDeletedRemotely,
                IReadOnlyList<ConflictItem> filesRemaining) = GetConflicts();

            _solveMergeConflictApplyToAll = false;
            foreach (ConflictItem conflict in filesDeletedLocallyAndModifiedRemotely)
            {
                ProgressValue++;
                await ResolveItemConflictAsync(conflict.Data);
            }

            _solveMergeConflictApplyToAll = false;
            foreach (ConflictItem conflict in filesModifiedLocallyAndDeletedRemotely)
            {
                ProgressValue++;
                await ResolveItemConflictAsync(conflict.Data);
            }

            // Hide the "apply to all" check box.
            _solveMergeConflictDialogCheckboxText = "";
            _solveMergeConflictApplyToAll = false;
            foreach (ConflictItem conflict in filesRemaining)
            {
                ProgressValue++;
                await ResolveItemConflictAsync(conflict.Data);
            }
        }
        finally
        {
            _solveMergeConflictApplyToAll = false;
            IsProgressVisible = false;
            IsBusy = false;
            Initialize();
        }
    }

    /// <summary>As <c>Mergetool_Click</c>: <c>git mergetool</c> on all the conflicts.</summary>
    [RelayCommand]
    private async Task StartMergeToolAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _host.RunMergeToolAsync(fileName: null, customTool: null);
        }
        finally
        {
            IsBusy = false;
        }

        Initialize();
    }

    /// <summary>As <c>customMergetool_Click</c>: <c>git mergetool</c> on the selected files, with the given tool or the default one.</summary>
    [RelayCommand]
    private async Task CustomMergeToolAsync(string? customTool)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            foreach (ConflictItem conflict in SelectedConflicts)
            {
                await _host.RunMergeToolAsync(conflict.Filename, customTool);
                Initialize();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>As <c>Reset_Click</c> and <c>ShowAbortMessage</c>.</summary>
    [RelayCommand]
    private void Reset()
    {
        if (_messageBoxes.Confirm(Strings.AbortCurrentOperation.Text, Strings.ResetCaption.Text)
            && _host.ConfirmDeleteAllChanges(Strings.AreYouSureYouWantDeleteFiles.Text, Strings.AreYouSureYouWantDeleteFilesCaption.Text))
        {
            _host.ResetHard();
            CloseDialog();
        }
    }

    /// <summary>As <c>ContextChooseBase_Click</c>.</summary>
    [RelayCommand]
    private void ChooseBase()
        => ChooseForSelection(conflict =>
        {
            if (CheckForBaseRevision(conflict))
            {
                ChooseSide(conflict.Base.Filename, ConflictSide.Base);
            }
        });

    /// <summary>As <c>ContextChooseLocal_Click</c>.</summary>
    [RelayCommand]
    private void ChooseLocal()
        => ChooseForSelection(conflict =>
        {
            if (CheckForLocalRevision(conflict))
            {
                ChooseSide(conflict.Filename, ConflictSide.Local);
            }
        });

    /// <summary>As <c>ContextChooseRemote_Click</c>.</summary>
    [RelayCommand]
    private void ChooseRemote()
        => ChooseForSelection(conflict =>
        {
            if (CheckForRemoteRevision(conflict))
            {
                ChooseSide(conflict.Filename, ConflictSide.Remote);
            }
        });

    /// <summary>As <c>ContextMarkAsSolved_Click</c>.</summary>
    [RelayCommand]
    private void MarkAsSolved()
    {
        if (IsBusy)
        {
            return;
        }

        foreach (ConflictItem conflict in SelectedConflicts)
        {
            StageFile(conflict.Filename);
        }

        Initialize();
    }

    /// <summary>As <c>ContextOpenLocalWith_Click</c>.</summary>
    [RelayCommand]
    private void OpenLocalWith() => OpenSideWith(ConflictSide.Local);

    /// <summary>As <c>ContextOpenRemoteWith_Click</c>.</summary>
    [RelayCommand]
    private void OpenRemoteWith() => OpenSideWith(ConflictSide.Remote);

    /// <summary>As <c>ContextOpenBaseWith_Click</c>.</summary>
    [RelayCommand]
    private void OpenBaseWith() => OpenSideWith(ConflictSide.Base);

    /// <summary>As <c>ContextSaveLocalAs_Click</c>.</summary>
    [RelayCommand]
    private void SaveLocalAs() => SaveAs(ConflictSide.Local);

    /// <summary>As <c>ContextSaveRemoteAs_Click</c>.</summary>
    [RelayCommand]
    private void SaveRemoteAs() => SaveAs(ConflictSide.Remote);

    /// <summary>As <c>ContextSaveBaseAs_Click</c>.</summary>
    [RelayCommand]
    private void SaveBaseAs() => SaveAs(ConflictSide.Base);

    /// <summary>As <c>openToolStripMenuItem_Click</c>.</summary>
    [RelayCommand]
    private void Open()
    {
        if (SingleSelection() is { } conflict)
        {
            _host.Open(_host.GetFullPath(conflict.Filename));
        }
    }

    /// <summary>As <c>openWithToolStripMenuItem_Click</c>.</summary>
    [RelayCommand]
    private void OpenWith()
    {
        if (SingleSelection() is { } conflict)
        {
            _host.OpenWith(_host.GetFullPath(conflict.Filename));
        }
    }

    /// <summary>As <c>openFolderToolStripMenuItem_Click</c>.</summary>
    [RelayCommand]
    private void ShowInFolder()
    {
        if (SingleSelection() is { } conflict)
        {
            _host.ShowInFolder(_host.GetFullPath(conflict.Filename));
        }
    }

    /// <summary>As <c>fileHistoryToolStripMenuItem_Click</c>.</summary>
    [RelayCommand]
    private void FileHistory()
    {
        if (SingleSelection() is { } conflict)
        {
            _host.ShowFileHistory(conflict.Filename);
        }
    }

    [RelayCommand]
    private void OpenHelp() => _host.OpenUrl(_manualUrl());

    /// <summary>As <c>Initialize</c>: lists the conflicts, keeps the selected row, and offers to commit once all are resolved.</summary>
    private void Initialize()
    {
        if (_isClosed)
        {
            return;
        }

        _inTheMiddleOfRebase = _host.InTheMiddleOfRebase();

        int oldSelectedRow = 0;
        bool isLastRow = false;
        if (SelectedConflicts.Count > 0)
        {
            oldSelectedRow = IndexOf(Conflicts, SelectedConflicts[0]);
            isLastRow = Conflicts.Count - 1 == oldSelectedRow;
        }

        List<ConflictItem> conflicts = [.. _host.GetConflicts().Select(conflict => new ConflictItem(conflict))];

        // Sorted by name, as SortableConflictDataList.
        conflicts.Sort((x, y) => string.Compare(x.Filename, y.Filename, StringComparison.Ordinal));
        Conflicts = conflicts;

        // If the last row was previously selected, select the last row again.
        if (isLastRow && oldSelectedRow >= conflicts.Count)
        {
            oldSelectedRow = Math.Max(0, conflicts.Count - 1);
        }

        // As with the data binding, the first row is selected if the previous one is gone.
        ConflictItem? selected = conflicts.Count > oldSelectedRow ? conflicts[oldSelectedRow] : conflicts.FirstOrDefault();
        SelectedConflict = selected;
        SelectedConflicts = selected is null ? [] : [selected];

        OpenMergeToolMenuText = $"{Strings.OpenInMenu.Text} {MergeTool}";
        OpenMergeToolButtonText = $"{Strings.OpenInButton.Text} {MergeTool}";

        if (_inTheMiddleOfRebase)
        {
            ChooseLocalText = Strings.ContextChooseLocalRebaseText.Text;
            ChooseLocalToolTip = Strings.ChangesTakeOnlyLocalRebaseTooltip.Text;
            LocalLabel = WithSuffix(Strings.LocalCurrent.Text, Strings.Theirs.Text);
            LocalLabelToolTip = Strings.ChangesLocalRebaseTooltip.Text;

            ChooseRemoteText = Strings.ContextChooseRemoteRebaseText.Text;
            ChooseRemoteToolTip = Strings.ChangesTakeOnlyRemoteRebaseTooltip.Text;
            RemoteLabel = WithSuffix(Strings.RemoteIncoming.Text, Strings.Ours.Text);
            RemoteLabelToolTip = Strings.ChangesRemoteRebaseTooltip.Text;
        }
        else
        {
            ChooseLocalText = Strings.ContextChooseLocalMergeText.Text;
            ChooseLocalToolTip = Strings.ChangesTakeOnlyLocalMergeTooltip.Text;
            LocalLabel = WithSuffix(Strings.LocalCurrent.Text, Strings.Ours.Text);
            LocalLabelToolTip = Strings.ChangesLocalMergeTooltip.Text;

            ChooseRemoteText = Strings.ContextChooseRemoteMergeText.Text;
            ChooseRemoteToolTip = Strings.ChangesTakeOnlyRemoteMergeTooltip.Text;
            RemoteLabel = WithSuffix(Strings.RemoteIncoming.Text, Strings.Theirs.Text);
            RemoteLabelToolTip = Strings.ChangesRemoteMergeTooltip.Text;
        }

        if (!_host.InTheMiddleOfConflictedMerge() && _thereWhereMergeConflicts)
        {
            _host.UpdateSubmodules();

            if (!_host.InTheMiddleOfPatch() && !_inTheMiddleOfRebase && _offerCommit
                && _host.ConfirmCommit(Strings.AllConflictsResolved.Text, Strings.AllConflictsResolvedCaption.Text))
            {
                _host.StartCommit();
            }

            CloseDialog();
        }

        static int IndexOf(IReadOnlyList<ConflictItem> items, ConflictItem item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (ReferenceEquals(items[i], item))
                {
                    return i;
                }
            }

            return 0;
        }

        // As DisplayWithSuffixUpdater.UpdateSuffixWithinParenthesis on the designer text.
        static string WithSuffix(string text, string suffix) => $"{text} ({suffix})";
    }

    /// <summary>As <c>InitMergetool</c>.</summary>
    private bool InitMergeTool()
    {
        MergeTool = _host.GetMergeTool();
        if (string.IsNullOrEmpty(MergeTool))
        {
            _messageBoxes.ShowError(Strings.NoMergeTool.Text, _errorCaption);
            return false;
        }

        _mergeToolCommand = _host.GetEffectiveSetting($"mergetool.{MergeTool}.cmd");
        _mergeToolPath = _host.GetEffectiveSetting($"mergetool.{MergeTool}.path");

        // A tool configured by its name only, which Git Extensions knows (e.g. merge.tool=meld of git on Linux).
        if (string.IsNullOrEmpty(_mergeToolPath) && string.IsNullOrEmpty(_mergeToolCommand) && _host.GetKnownMergeTool(MergeTool) is { } knownTool)
        {
            _mergeToolPath = knownTool.Path;
            _mergeToolCommand = knownTool.Command;
        }

        // Temporary compatibility with GE <3.3
        if (MergeTool == "kdiff3")
        {
            if (string.IsNullOrEmpty(_mergeToolPath))
            {
                _mergeToolPath = "kdiff3";
            }

            if (string.IsNullOrEmpty(_mergeToolCommand))
            {
                _mergeToolCommand = "\"$BASE\" \"$LOCAL\" \"$REMOTE\" -o \"$MERGED\"";
            }
        }

        if (OperatingSystem.IsWindows() && _mergeToolCommand is not null)
        {
            // This only works when on Windows....
            const string executablePattern = ".exe";
            int idx = _mergeToolCommand.IndexOf(executablePattern, StringComparison.Ordinal);
            if (idx >= 0)
            {
                _mergeToolPath = _mergeToolCommand[..(idx + executablePattern.Length + 1)].Trim('\"', ' ');
                _mergeToolCommand = _mergeToolCommand[(idx + executablePattern.Length + 1)..];
            }
        }
        else if (_mergeToolCommand is not null)
        {
            (_mergeToolPath, _mergeToolCommand) = SplitUnixMergeToolCommand(_mergeToolPath, _mergeToolCommand);
        }

        if (_host.FindFullPath(_mergeToolPath) is not { } fullPath)
        {
            _messageBoxes.ShowWarning(Strings.NoMergeToolConfigured.Text, _warningCaption);
            return false;
        }

        _mergeToolPath = fullPath;
        return true;
    }

    /// <summary>
    ///  Off Windows, the program and the arguments of the command of a merge tool of git's config (the Windows code above
    ///  finds the program by its <c>.exe</c>): the command that Git Extensions configures starts with the quoted path of the
    ///  tool (<c>"/usr/bin/meld" "$LOCAL" …</c>), which is then the program rather than the first argument; a pipe
    ///  through <c>cat</c> at its end (for <c>opendiff</c>, which waits only when its output is a pipe) is left out,
    ///  since the program is run with its output read, which is a pipe too.
    /// </summary>
    internal static (string? Path, string Command) SplitUnixMergeToolCommand(string? path, string command)
    {
        const string pipeThroughCat = "| cat";
        string arguments = command.Trim();
        if (arguments.EndsWith(pipeThroughCat, StringComparison.Ordinal))
        {
            arguments = arguments[..^pipeThroughCat.Length].TrimEnd();
        }

        if (arguments.StartsWith('"') && arguments.IndexOf('"', 1) is int end and > 1)
        {
            string program = arguments[1..end];
            if (string.IsNullOrEmpty(path) || program == path)
            {
                return (program, arguments[(end + 1)..].TrimStart());
            }
        }

        return (path, arguments);
    }

    /// <summary>As <c>LoadCustomMergetools</c>: the custom merge tools of the sub menu.</summary>
    private async Task LoadCustomMergeToolsAsync()
    {
        try
        {
            CustomMergeTools = await _host.GetCustomMergeToolsAsync(_customMergeToolsCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // The dialog was closed.
        }
    }

    /// <summary>As <c>UpdateConflictedFilesMenu</c>, <c>HandleMultipleSelect</c> and <c>HandleSingleSelect</c>.</summary>
    private void UpdateConflictedFilesMenu()
    {
        BaseFileName = LocalFileName = RemoteFileName = "";
        IsSingleFileSelected = SelectedConflicts.Count == 1;

        // Disable extra GE processing if path or cmd is not set.
        bool mergeToolExtrasConfigured = !string.IsNullOrWhiteSpace(_mergeToolPath) || !string.IsNullOrWhiteSpace(_mergeToolCommand);
        CanOpenMergeTool = IsSingleFileSelected && mergeToolExtrasConfigured;
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanOpenLocal));
        OnPropertyChanged(nameof(CanOpenRemote));
        OnPropertyChanged(nameof(CanOpenBase));

        if (!IsSingleFileSelected)
        {
            return;
        }

        ConflictData item = SelectedConflicts[0].Data;
        bool baseFileExists = !string.IsNullOrEmpty(item.Base.Filename);
        bool localFileExists = !string.IsNullOrEmpty(item.Local.Filename);
        bool remoteFileExists = !string.IsNullOrEmpty(item.Remote.Filename);

        string remoteSide = GetRemoteSideString();
        string localSide = GetLocalSideString();

        ConflictDescription = (baseFileExists, localFileExists, remoteFileExists) switch
        {
            (true, true, true) => string.Format(Strings.FileChangeLocallyAndRemotely.Text, localSide, remoteSide),
            (false, true, true) => string.Format(Strings.FileCreatedLocallyAndRemotely.Text, localSide, remoteSide),
            (true, false, true) => string.Format(Strings.FileDeletedLocallyAndModifiedRemotely.Text, localSide, remoteSide),
            (true, true, false) => string.Format(Strings.FileModifiedLocallyAndDeletedRemotely.Text, localSide, remoteSide),
            (true, false, false) => string.Format(Strings.FileDeletedLocallyAndRemotely.Text, localSide, remoteSide),

            // Do not retain the description of the previously selected file.
            _ => string.Empty
        };

        string baseFileName = baseFileExists ? item.Base.Filename : Strings.NoBase.Text;
        string localFileName = localFileExists ? item.Local.Filename : Strings.Deleted.Text;
        string remoteFileName = remoteFileExists ? item.Remote.Filename : Strings.Deleted.Text;

        if (_host.GetItemType(item.Filename) == ConflictItemType.Submodule)
        {
            baseFileName += GetShortHash(item.Base);
            localFileName += GetShortHash(item.Local);
            remoteFileName += GetShortHash(item.Remote);
        }

        BaseFileName = baseFileName;
        LocalFileName = localFileName;
        RemoteFileName = remoteFileName;
    }

    private string GetShortHash(ConflictedFileData item)
        => $"@{(item.ObjectId.IsZero ? Strings.Deleted.Text : item.ObjectId.ToShortString())}";

    private string GetRemoteSideString() => _inTheMiddleOfRebase ? Strings.Ours.Text : Strings.Theirs.Text;

    private string GetLocalSideString() => _inTheMiddleOfRebase ? Strings.Theirs.Text : Strings.Ours.Text;

    private ConflictItem? SingleSelection() => SelectedConflicts.Count == 1 ? SelectedConflicts[0] : null;

    /// <summary>As <c>GetConflicts</c>: the selected files by kind of conflict, and the counters of the questions; starts the progress bar.</summary>
    private (IReadOnlyList<ConflictItem> Conflicts,
        IReadOnlyList<ConflictItem> FilesDeletedLocallyAndModifiedRemotely,
        IReadOnlyList<ConflictItem> FilesModifiedLocallyAndDeletedRemotely,
        IReadOnlyList<ConflictItem> FilesRemaining) GetConflicts()
    {
        IReadOnlyList<ConflictItem> conflicts = SelectedConflicts;
        _conflictItemsCount = conflicts.Count;
        ProgressMaximum = _conflictItemsCount;
        ProgressValue = 0;
        IsProgressVisible = true;

        List<ConflictItem> filesDeletedLocallyAndModifiedRemotely = [];
        List<ConflictItem> filesModifiedLocallyAndDeletedRemotely = [];
        List<ConflictItem> filesRemaining = [];
        foreach (ConflictItem conflict in conflicts)
        {
            ConflictData data = conflict.Data;
            if (string.IsNullOrEmpty(data.Local.Filename) && !string.IsNullOrEmpty(data.Remote.Filename))
            {
                filesDeletedLocallyAndModifiedRemotely.Add(conflict);
            }
            else if (!string.IsNullOrEmpty(data.Local.Filename) && string.IsNullOrEmpty(data.Remote.Filename))
            {
                filesModifiedLocallyAndDeletedRemotely.Add(conflict);
            }
            else
            {
                filesRemaining.Add(conflict);
            }
        }

        _filesDeletedLocallyAndModifiedRemotelyCount = filesDeletedLocallyAndModifiedRemotely.Count;
        _filesModifiedLocallyAndDeletedRemotelyCount = filesModifiedLocallyAndDeletedRemotely.Count;
        _filesRemainedCount = filesRemaining.Count;
        _filesDeletedLocallyAndModifiedRemotelySolved = _filesDeletedLocallyAndModifiedRemotelyCount;
        _filesModifiedLocallyAndDeletedRemotelySolved = _filesModifiedLocallyAndDeletedRemotelyCount;

        return (conflicts, filesDeletedLocallyAndModifiedRemotely, filesModifiedLocallyAndDeletedRemotely, filesRemaining);
    }

    /// <summary>The loop of the choose commands over the selected files, with the progress bar.</summary>
    private void ChooseForSelection(Action<ConflictData> choose)
    {
        if (IsBusy)
        {
            return;
        }

        _solveMergeConflictApplyToAll = false;
        try
        {
            foreach (ConflictItem conflict in GetConflicts().Conflicts)
            {
                choose(conflict.Data);
                ProgressValue++;
            }
        }
        finally
        {
            _solveMergeConflictApplyToAll = false;
            IsProgressVisible = false;
            Initialize();
        }
    }

    /// <summary>As <c>ResolveItemConflictAsync</c>.</summary>
    private async Task ResolveItemConflictAsync(ConflictData item)
    {
        switch (_host.GetItemType(item.Filename))
        {
            case ConflictItemType.Submodule:
                if (_host.MergeSubmodule(item.Filename))
                {
                    StageFile(item.Filename);
                }

                break;
            case ConflictItemType.File:
                await ResolveFilesConflictAsync(item);
                break;
        }
    }

    /// <summary>As <c>ResolveFilesConflictAsync</c>.</summary>
    private async Task ResolveFilesConflictAsync(ConflictData item)
    {
        (string? baseFile, string? localFile, string? remoteFile) = _host.CheckoutConflictedFiles(item);

        try
        {
            if (!CheckForLocalRevision(item) || !CheckForRemoteRevision(item))
            {
                return;
            }

            if (TryMergeWithScript(item.Filename, baseFile, localFile, remoteFile))
            {
                return;
            }

            if (_host.IsBinaryFile(item.Local.Filename)
                && !_messageBoxes.Confirm(string.Format(Strings.FileIsBinary.Text, MergeTool), _warningCaption, defaultNo: true))
            {
                BinaryFilesChooseLocalBaseRemote(item);
                return;
            }

            if (string.IsNullOrWhiteSpace(_mergeToolCommand) || string.IsNullOrWhiteSpace(_mergeToolPath))
            {
                // The merge tool is set, but its arguments cannot be manipulated.
                // git-mergetool does not provide an exit status: do not stage.
                await _host.RunMergeToolAsync(item.Filename, customTool: null);
                return;
            }

            string arguments = _mergeToolCommand;

            // Check if there is a base file. If not, ask the user to fall back to a 2-way merge.
            // git doesn't support 2-way merges, but the arguments can be adjusted for some tools.
            if (item.Base.Filename is null)
            {
                bool? twoWayMerge = _messageBoxes.ConfirmWithCancel(string.Format(Strings.NoBaseRevision.Text, item.Filename), Strings.NoBaseFileMergeCaption.Text);
                if (twoWayMerge is null)
                {
                    return;
                }

                if (twoWayMerge == true)
                {
                    arguments = Use2WayMerge(arguments);
                }
            }

            arguments = arguments.Replace("$BASE", baseFile)
                .Replace("$LOCAL", localFile)
                .Replace("$REMOTE", remoteFile)
                .Replace("$MERGED", item.Filename);

            // The timestamp of the file before the merge: an extra check that the merge was successful.
            DateTime lastWriteTimeBeforeMerge = _host.GetLastWriteTime(item.Filename) ?? DateTime.Now;

            int? exitCode = await _host.RunMergeToolProcessAsync(_mergeToolPath, arguments);
            if (exitCode is null)
            {
                _messageBoxes.ShowError(string.Format(Strings.ErrorStartingMergetool.Text, _mergeToolPath), Strings.NoBaseFileMergeCaption.Text);
                return;
            }

            DateTime lastWriteTimeAfterMerge = _host.GetLastWriteTime(item.Filename) ?? lastWriteTimeBeforeMerge;
            bool modified = lastWriteTimeBeforeMerge != lastWriteTimeAfterMerge;

            // A success and a changed timestamp: the merge was done.
            if (exitCode == 0 && modified)
            {
                StageFile(item.Filename);
            }

            // Exit code 1 with the file changed, or exit code 0 with the file unchanged: ask whether the conflict is solved.
            if (((exitCode == 1 && modified) || (exitCode == 0 && !modified))
                && _messageBoxes.Confirm(Strings.AskMergeConflictSolved.Text, Strings.AskMergeConflictSolvedCaption.Text))
            {
                StageFile(item.Filename);
            }
        }
        finally
        {
            _host.DeleteTemporaryFile(baseFile);
            _host.DeleteTemporaryFile(localFile);
            _host.DeleteTemporaryFile(remoteFile);
        }
    }

    /// <summary>As <c>Use2WayMerge</c>.</summary>
    private string Use2WayMerge(string arguments)
    {
        switch (MergeTool!.ToLowerInvariant())
        {
            case "kdiff3":
            case "diffmerge":
            case "smerge":
                return arguments.Replace("\"$BASE\"", "");
            case "tortoisemerge":
                return arguments.Replace("-base:\"$BASE\"", "").Replace("/base:\"$BASE\"", "")
                    .Replace("mine:\"$LOCAL\"", "base:\"$LOCAL\"");
            default:
                return arguments;
        }
    }

    /// <summary>As <c>TryMergeWithScript</c>.</summary>
    private bool TryMergeWithScript(string fileName, string? baseFile, string? localFile, string? remoteFile)
    {
        try
        {
            string? extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (extension is null || extension.Length <= 1
                || !_mergeScripts.TryGetValue(extension, out string? mergeScript)
                || _host.GetMergeScriptPath(mergeScript) is not { } mergeScriptPath)
            {
                return false;
            }

            if (_messageBoxes.Confirm(string.Format(Strings.UseCustomMergeScript.Text, mergeScript), Strings.UseCustomMergeScriptCaption.Text))
            {
                UseMergeWithScript(fileName, mergeScriptPath, baseFile, localFile, remoteFile);
                return true;
            }
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError("Merge using script failed.\n" + ex, _errorCaption);
        }

        return false;
    }

    /// <summary>As <c>UseMergeWithScript</c>.</summary>
    private void UseMergeWithScript(string fileName, string mergeScript, string? baseFile, string? localFile, string? remoteFile)
    {
        // The timestamp of the file before the merge: an extra check that the merge was successful.
        string filePath = _host.GetFullPath(fileName);
        DateTime lastWriteTimeBeforeMerge = _host.GetLastWriteTime(fileName) ?? DateTime.Now;

        _host.StartMergeScript(mergeScript, filePath, remoteFile, localFile, baseFile);

        if (_messageBoxes.Confirm(string.Format(Strings.AskMergeConflictSolvedAfterCustomMergeScript.Text, filePath), Strings.AskMergeConflictSolvedCaption.Text))
        {
            DateTime lastWriteTimeAfterMerge = _host.GetLastWriteTime(fileName) ?? lastWriteTimeBeforeMerge;

            // The file is not modified: do not stage it and warn.
            if (lastWriteTimeBeforeMerge == lastWriteTimeAfterMerge)
            {
                _messageBoxes.ShowInformation(Strings.FileUnchangedAfterMerge.Text, "Information");
            }
            else
            {
                StageFile(fileName);
            }
        }

        Initialize();
        _host.DeleteTemporaryFile(baseFile);
        _host.DeleteTemporaryFile(remoteFile);
        _host.DeleteTemporaryFile(localFile);
    }

    /// <summary>As <c>OpenSolveMergeConflictDialogAndExecuteSelectedMergeAction</c>: asks, unless "apply to all" was checked.</summary>
    private void AskAndSolve(Action<ConflictResolutionChoice> selectedMergeAction, string text, string heading, string applyToAllText,
        string keepLocalText, string keepRemoteText, string keepBaseText)
    {
        if (!_solveMergeConflictApplyToAll)
        {
            SolveConflictAnswer answer = _host.AskSolveConflict(new SolveConflictQuestion(
                text, heading, Strings.SolveMergeConflictDialogCaption.Text, applyToAllText, keepLocalText, keepRemoteText, keepBaseText));
            _solveMergeConflictDialogResult = answer.Choice;
            _solveMergeConflictApplyToAll = answer.ApplyToAll;
        }

        selectedMergeAction(_solveMergeConflictDialogResult);
    }

    /// <summary>As <c>BinaryFilesChooseLocalBaseRemote</c>.</summary>
    private void BinaryFilesChooseLocalBaseRemote(ConflictData item)
        => AskAndSolve(
            choice =>
            {
                switch (choice)
                {
                    case ConflictResolutionChoice.KeepLocal:
                        ChooseSide(item.Filename, ConflictSide.Local);
                        break;
                    case ConflictResolutionChoice.KeepRemote:
                        ChooseSide(item.Filename, ConflictSide.Remote);
                        break;
                    case ConflictResolutionChoice.KeepBase:
                        ChooseSide(item.Filename, ConflictSide.Base);
                        break;
                }
            },
            string.Format(Strings.FileBinaryChooseLocalBaseRemote.Text, item.Local.Filename, GetLocalSideString(), GetRemoteSideString()),
            item.Filename,
            _solveMergeConflictDialogCheckboxText,
            $"{Strings.ChooseLocalButtonText.Text} ({GetLocalSideString()})",
            $"{Strings.ChooseRemoteButtonText.Text} ({GetRemoteSideString()})",
            Strings.KeepBaseButtonText.Text);

    /// <summary>As <c>CheckForBaseRevision</c>: <see langword="true"/> if there is a base; otherwise asks to keep a side or delete the file.</summary>
    private bool CheckForBaseRevision(ConflictData item)
    {
        if (!string.IsNullOrEmpty(item.Base.Filename))
        {
            return true;
        }

        AskAndSolve(
            choice =>
            {
                switch (choice)
                {
                    case ConflictResolutionChoice.KeepLocal:
                        ChooseSide(item.Filename, ConflictSide.Local);
                        break;
                    case ConflictResolutionChoice.KeepRemote:
                        ChooseSide(item.Filename, ConflictSide.Remote);
                        break;
                    case ConflictResolutionChoice.KeepBase:
                        // Delete
                        _host.RemoveFile(item.Filename);
                        break;
                }
            },
            string.Format(Strings.FileCreatedLocallyAndRemotelyLong.Text, item.Filename, GetLocalSideString(), GetRemoteSideString()),
            item.Filename,
            _solveMergeConflictDialogCheckboxText,
            $"{Strings.ChooseLocalButtonText.Text} ({GetLocalSideString()})",
            $"{Strings.ChooseRemoteButtonText.Text} ({GetRemoteSideString()})",
            Strings.DeleteFileButtonText.Text);

        return false;
    }

    /// <summary>As <c>CheckForLocalRevision</c>: <see langword="true"/> if there is a local side; otherwise asks to delete or keep the file.</summary>
    private bool CheckForLocalRevision(ConflictData item)
    {
        if (!string.IsNullOrEmpty(item.Local.Filename))
        {
            return true;
        }

        string dialogText = "";
        if (!_solveMergeConflictApplyToAll && _filesDeletedLocallyAndModifiedRemotelySolved == 1)
        {
            dialogText = string.Format(Strings.FileDeletedLocallyAndModifiedRemotelyLong.Text, item.Filename, GetLocalSideString(), GetRemoteSideString());
        }

        if (!_solveMergeConflictApplyToAll && _filesDeletedLocallyAndModifiedRemotelySolved > 1)
        {
            // The dialog names the current file, hence the other files are one less.
            dialogText = _filesModifiedLocallyAndDeletedRemotelyCount == 0 && _filesRemainedCount == 0
                ? string.Format(Strings.FilesDeletedLocallyAndModifiedRemotelyLongNoOtherFilesSelected.Text, item.Filename, _filesDeletedLocallyAndModifiedRemotelySolved - 1)
                : string.Format(Strings.FilesDeletedLocallyAndModifiedRemotelyLong.Text, item.Filename, _filesDeletedLocallyAndModifiedRemotelySolved - 1, _conflictItemsCount);
        }

        AskAndSolve(
            choice =>
            {
                switch (choice)
                {
                    case ConflictResolutionChoice.KeepLocal:
                        // Delete
                        _host.RemoveFile(item.Filename);
                        break;
                    case ConflictResolutionChoice.KeepRemote:
                        ChooseSide(item.Filename, ConflictSide.Remote);
                        break;
                    case ConflictResolutionChoice.KeepBase:
                        ChooseSide(item.Filename, ConflictSide.Base);
                        break;
                }

                _filesDeletedLocallyAndModifiedRemotelySolved--;
            },
            dialogText,
            item.Filename,
            _filesDeletedLocallyAndModifiedRemotelySolved > 1 ? string.Format(Strings.SolveMergeConflictApplyToAllCheckBoxText.Text, item.Filename, _filesDeletedLocallyAndModifiedRemotelySolved - 1) : "",
            $"{Strings.DeleteFileButtonText.Text} ({GetLocalSideString()})",
            $"{Strings.KeepModifiedButtonText.Text} ({GetRemoteSideString()})",
            $"{Strings.KeepBaseButtonText.Text} ({GetLocalSideString()})");

        return false;
    }

    /// <summary>As <c>CheckForRemoteRevision</c>: <see langword="true"/> if there is a remote side; otherwise asks to keep or delete the file.</summary>
    private bool CheckForRemoteRevision(ConflictData item)
    {
        if (!string.IsNullOrEmpty(item.Remote.Filename))
        {
            return true;
        }

        string dialogText = "";
        if (!_solveMergeConflictApplyToAll && _filesModifiedLocallyAndDeletedRemotelySolved == 1)
        {
            dialogText = string.Format(Strings.FileModifiedLocallyAndDeletedRemotelyLong.Text, item.Filename, GetLocalSideString(), GetRemoteSideString());
        }

        if (!_solveMergeConflictApplyToAll && _filesModifiedLocallyAndDeletedRemotelySolved > 1)
        {
            // The dialog names the current file, hence the other files are one less.
            dialogText = _filesDeletedLocallyAndModifiedRemotelyCount == 0 && _filesRemainedCount == 0
                ? string.Format(Strings.FilesModifiedLocallyAndDeletedRemotelyLongNoOtherFilesSelected.Text, item.Filename, _filesModifiedLocallyAndDeletedRemotelySolved - 1)
                : string.Format(Strings.FilesModifiedLocallyAndDeletedRemotelyLong.Text, item.Filename, _filesModifiedLocallyAndDeletedRemotelySolved - 1, _conflictItemsCount);
        }

        AskAndSolve(
            choice =>
            {
                switch (choice)
                {
                    case ConflictResolutionChoice.KeepLocal:
                        ChooseSide(item.Filename, ConflictSide.Local);
                        break;
                    case ConflictResolutionChoice.KeepRemote:
                        // Delete
                        _host.RemoveFile(item.Filename);
                        break;
                    case ConflictResolutionChoice.KeepBase:
                        ChooseSide(item.Filename, ConflictSide.Base);
                        break;
                }

                _filesModifiedLocallyAndDeletedRemotelySolved--;
            },
            dialogText,
            item.Filename,
            _filesModifiedLocallyAndDeletedRemotelySolved > 1 ? string.Format(Strings.SolveMergeConflictApplyToAllCheckBoxText.Text, item.Filename, _filesModifiedLocallyAndDeletedRemotelySolved - 1) : "",
            $"{Strings.KeepModifiedButtonText.Text} ({GetLocalSideString()})",
            $"{Strings.DeleteFileButtonText.Text} ({GetRemoteSideString()})",
            Strings.KeepBaseButtonText.Text);

        return false;
    }

    /// <summary>As <c>ChooseBaseOnConflict</c>, <c>ChooseLocalOnConflict</c> and <c>ChooseRemoteOnConflict</c>.</summary>
    private void ChooseSide(string fileName, ConflictSide side)
    {
        if (!_host.ChooseSide(fileName, side))
        {
            TranslatedText failed = side switch
            {
                ConflictSide.Base => Strings.ChooseBaseFileFailedText,
                ConflictSide.Local => Strings.ChooseLocalFileFailedText,
                _ => Strings.ChooseRemoteFileFailedText,
            };
            _messageBoxes.ShowError(failed.Text, _errorCaption);
        }
    }

    private void StageFile(string fileName) => _host.StageFile(fileName, string.Format(Strings.StageFilename.Text, fileName));

    /// <summary>As <c>OpenSideWith</c>: saves the side to a temporary file and opens it with a chosen program.</summary>
    private void OpenSideWith(ConflictSide side)
    {
        if (SingleSelection() is not { } conflict)
        {
            return;
        }

        string fileName = _host.GetTemporaryPath(conflict.Filename);
        if (!_host.SaveSide(conflict.Filename, fileName, side))
        {
            _messageBoxes.ShowError(Strings.FailureWhileOpenFile.Text, _errorCaption);
        }

        _host.OpenWith(fileName);
    }

    /// <summary>As <c>SaveAs</c>.</summary>
    private void SaveAs(ConflictSide side)
    {
        if (SingleSelection() is not { } conflict || _host.ChooseSaveFile(conflict.Filename) is not { } fileName)
        {
            return;
        }

        if (!_host.SaveSide(conflict.Filename, fileName, side))
        {
            _messageBoxes.ShowError(Strings.FailureWhileSaveFile.Text, _errorCaption);
        }
    }

    private void CloseDialog()
    {
        _isClosed = true;
        _customMergeToolsCancellation.Cancel();
        Close(true);
    }

    public override bool CanClose()
    {
        _customMergeToolsCancellation.Cancel();
        return base.CanClose();
    }
}
