using GitCommands;
using GitCommands.Config;
using GitCommands.DiffMergeTools;
using GitCommands.Git;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the merge conflicts dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>Shows the Avalonia port of <c>FormResolveConflicts</c> (the arguments of its constructor).</summary>
    public static bool TryShowResolveConflicts(IWin32Window? owner, IGitUICommands commands, bool offerCommit)
    {
        ShowDialog(
            () =>
            {
                ResolveConflictsWindow window = new();
                ResolveConflictsStrings strings = ViewStrings.Load<ResolveConflictsStrings>();
                window.Hotkeys = LoadHotkeys(commands, HotkeyCommands.ResolveConflictsSettingsName);
                window.DataContext = new ResolveConflictsViewModel(
                    strings,
                    new ResolveConflictsHost(commands, window, strings),
                    new MessageBoxService(window),
                    offerCommit,
                    () => UserManual.UserManual.UrlFor("modify_history", "handle-merge-conflicts"),
                    TranslatedStrings.Error,
                    TranslatedStrings.Warning);
                return window;
            },
            owner,
            positionName: "FormResolveConflicts");
        return true;
    }

    /// <summary>The git, file and dialog operations of <c>FormResolveConflicts</c>.</summary>
    private sealed class ResolveConflictsHost(IGitUICommands commands, DialogWindow window, ResolveConflictsStrings strings) : IResolveConflictsHost
    {
        /// <summary>As <c>ToolDelay</c> of <c>LoadCustomMergetools</c>.</summary>
        private const int CustomMergeToolsDelay = 500;

        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        private IFullPathResolver FullPathResolver => new FullPathResolver(() => Module.WorkingDir);

        /// <summary>All the merge tool settings are read from the native ("Windows") git, as <c>InitMergetool</c>.</summary>
        private EffectiveGitConfigSettings NativeSettings => field ??= new EffectiveGitConfigSettings(NativeGit);

        private Executable NativeGit => field ??= new Executable(AppSettings.GitCommand, Module.WorkingDir);

        public bool InTheMiddleOfRebase() => Module.InTheMiddleOfRebase();

        public bool InTheMiddleOfConflictedMerge() => Module.InTheMiddleOfConflictedMerge();

        public bool InTheMiddleOfPatch() => Module.InTheMiddleOfPatch();

        public IReadOnlyList<ConflictData> GetConflicts() => ThreadHelper.JoinableTaskFactory.Run(() => Module.GetConflictsAsync());

        public string? GetMergeTool()
        {
            string? mergeTool = null;
            if (GitVersion.CurrentVersion(NativeGit).SupportGuiMergeTool)
            {
                mergeTool = NativeSettings.GetValue(SettingKeyString.MergeToolKey);
            }

            // Fallback and older Git
            return string.IsNullOrEmpty(mergeTool) ? NativeSettings.GetValue(SettingKeyString.MergeToolNoGuiKey) : mergeTool;
        }

        public string? GetEffectiveSetting(string name) => NativeSettings.GetValue(name);

        public string? FindFullPath(string? path) => PathUtil.TryFindFullPath(path!, out string? fullPath) ? fullPath : null;

        public (string Path, string Command)? GetKnownMergeTool(string tool)
        {
            DiffMergeToolConfiguration config = new DiffMergeToolConfigurationManager(() => NativeSettings).LoadDiffMergeToolConfig(tool, null);
            return string.IsNullOrEmpty(config.MergeCommand) ? null : (config.Path, config.MergeCommand);
        }

        public async Task<IReadOnlyList<string>> GetCustomMergeToolsAsync(CancellationToken cancellationToken)
            => [.. await CustomDiffMergeToolCache.MergeToolCache.GetToolsAsync(Module, CustomMergeToolsDelay, cancellationToken)];

        public ConflictItemType GetItemType(string fileName)
        {
            string? fullName = FullPathResolver.Resolve(fileName);
            if (Directory.Exists(fullName) && !File.Exists(fullName))
            {
                return Module.IsSubmodule(fileName.Trim()) ? ConflictItemType.Submodule : ConflictItemType.Directory;
            }

            return ConflictItemType.File;
        }

        public bool ChooseSide(string fileName, ConflictSide side) => Module.HandleConflictSelectSide(fileName, ToGitSide(side));

        public void RemoveFile(string fileName)
        {
            GitArgumentBuilder args = new("rm")
            {
                "--",
                fileName.QuoteNE()
            };
            Module.GitExecutable.GetOutput(args);
        }

        public void StageFile(string fileName, string errorTitle)
        {
            GitArgumentBuilder args = new("add")
            {
                "--",
                fileName.QuoteNE()
            };
            string output = Module.GitExecutable.GetOutput(args);
            if (!string.IsNullOrWhiteSpace(output))
            {
                AvaloniaUi.RunInHostContext(() => ProcessDialogs.ShowErrorDialog(Owner, commands, errorTitle, errorTitle, output));
            }
        }

        public Task RunMergeToolAsync(string? fileName, string? customTool)
        {
            Directory.SetCurrentDirectory(Module.WorkingDir);
            return Task.Run(() => Module.RunMergeTool(fileName ?? "", customTool));
        }

        public bool MergeSubmodule(string fileName) => AvaloniaUi.RunInHostContext(() =>
        {
            return TryShowMergeSubmodule(Owner, commands, fileName, out bool accepted) && accepted;
        });

        public (string? BaseFile, string? LocalFile, string? RemoteFile) CheckoutConflictedFiles(ConflictData conflict) => Module.CheckoutConflictedFiles(conflict);

        public void DeleteTemporaryFile(string? path)
        {
            if (path is not null && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public bool IsBinaryFile(string fileName) => FileHelper.IsBinaryFileName(Module, fileName);

        public string? GetMergeScriptPath(string scriptName)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            string directory = Path.Join(Path.GetDirectoryName(ApplicationInfo.ExecutablePath)!, "Diff-Scripts").EnsureTrailingPathSeparator();
            string path = Path.Join(directory, scriptName);
            return Directory.Exists(directory) && File.Exists(path) ? path : null;
        }

        public string GetFullPath(string fileName) => (FullPathResolver.Resolve(fileName) ?? "").ToNativePath();

        public DateTime? GetLastWriteTime(string fileName)
            => FullPathResolver.Resolve(fileName) is { } path && File.Exists(path) ? File.GetLastWriteTime(path) : null;

        public void StartMergeScript(string mergeScript, string filePath, string? remoteFile, string? localFile, string? baseFile)
        {
            ArgumentBuilder args =
            [
                mergeScript.Quote(),
                filePath.ToNativePath().Quote(),
                (remoteFile ?? "").ToNativePath().Quote(),
                (localFile ?? "").ToNativePath().Quote(),
                (baseFile ?? "").ToNativePath().Quote()
            ];

            new Executable("wscript", Module.WorkingDir).Start(args);
        }

        public async Task<int?> RunMergeToolProcessAsync(string path, string arguments)
        {
            try
            {
                ExecutionResult result = await new Executable(path, Module.WorkingDir).ExecuteAsync(arguments, throwOnErrorExit: false);
                return result.ExitCode;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool SaveSide(string fileName, string targetFile, ConflictSide side) => Module.HandleConflictsSaveSide(fileName, targetFile, ToGitSide(side));

        public string GetTemporaryPath(string fileName) => Path.GetTempPath() + PathUtil.GetFileName(fileName);

        /// <summary>As the <c>SaveFileDialog</c> of <c>SaveAs</c>.</summary>
        public string? ChooseSaveFile(string fileName) => AvaloniaUi.RunInHostContext(() =>
        {
            using SaveFileDialog fileDialog = new()
            {
                FileName = PathUtil.GetFileName(fileName),
                InitialDirectory = FullPathResolver.Resolve(Path.GetDirectoryName(fileName)),
                AddExtension = true
            };
            string ext = Path.GetExtension(fileDialog.FileName);
            fileDialog.DefaultExt = ext;
            fileDialog.Filter = string.Format(strings.CurrentFormatFilter.Text, ext) + "|*." + ext + "|" + strings.AllFilesFilter.Text + "|*.*";
            return fileDialog.ShowDialog(Owner) == DialogResult.OK ? fileDialog.FileName : null;
        });

        public void Open(string path) => OsShellUtil.Open(path);

        public void OpenWith(string path) => OsShellUtil.OpenAs(path);

        public bool CanOpenWith => OsShellUtil.CanOpenAs;

        public void ShowInFolder(string path) => OsShellUtil.SelectPathInFileExplorer(path.ToNativePath());

        public void ShowFileHistory(string fileName) => AvaloniaUi.RunInHostContext(() => commands.StartFileHistoryDialog(Owner, fileName));

        /// <summary>As <c>CreateSolveMergeConflictTaskDialogPage</c>.</summary>
        public SolveConflictAnswer AskSolveConflict(SolveConflictQuestion question) => AvaloniaUi.RunInHostContext(() =>
        {
            TaskDialogPage page = new()
            {
                Text = question.Text,
                Heading = question.Heading,
                Caption = question.Caption,
                Buttons = { TaskDialogButton.Cancel },
                Icon = TaskDialogIcon.Error,
                AllowCancel = true,
                SizeToContent = true
            };

            if (!string.IsNullOrEmpty(question.ApplyToAllText))
            {
                page.Verification = new TaskDialogVerificationCheckBox { Text = question.ApplyToAllText };
            }

            TaskDialogCommandLinkButton keepLocal = new(question.KeepLocalText);
            TaskDialogCommandLinkButton keepRemote = new(question.KeepRemoteText);
            TaskDialogCommandLinkButton keepBase = new(question.KeepBaseText);
            page.Buttons.Add(keepLocal);
            page.Buttons.Add(keepRemote);
            page.Buttons.Add(keepBase);

            TaskDialogButton result = TaskDialog.ShowDialog(Owner.Handle, page);
            ConflictResolutionChoice choice = result == keepLocal ? ConflictResolutionChoice.KeepLocal
                : result == keepRemote ? ConflictResolutionChoice.KeepRemote
                : result == keepBase ? ConflictResolutionChoice.KeepBase
                : ConflictResolutionChoice.None;
            return new SolveConflictAnswer(choice, page.Verification?.Checked ?? false);
        });

        public bool ConfirmDeleteAllChanges(string text, string caption) => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.ConfirmSuppressible(Owner, text, caption, AppSettings.DontConfirmSecondAbortConfirmation, icon: TaskDialogIcon.Warning));

        public void ResetHard() => Module.Reset(ResetMode.Hard);

        public void UpdateSubmodules() => AvaloniaUi.RunInHostContext(() => commands.UpdateSubmodules(Owner));

        public bool ConfirmCommit(string text, string caption) => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.ConfirmSuppressible(Owner, text, caption, AppSettings.DontConfirmCommitAfterConflictsResolved));

        public void StartCommit() => AvaloniaUi.RunInHostContext(() => commands.StartCommitDialog(Owner));

        public void OpenUrl(string url) => OsShellUtil.OpenUrlInDefaultBrowser(url);

        private static string ToGitSide(ConflictSide side) => side switch
        {
            ConflictSide.Base => "BASE",
            ConflictSide.Local => "LOCAL",
            _ => "REMOTE",
        };
    }
}
