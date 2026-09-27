using System.Drawing.Imaging;
using GitCommands;
using GitCommands.Git;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.ScriptsEngine;
using GitUI.Shells;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>The buttons of the main toolbar of the Avalonia main window.</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseToolbarHost, IBrowseScriptsHost
    {
        /// <summary>As <c>GitModuleForm.ExecuteCommand</c>: the enabled script of the hotkey command.</summary>
        public bool RunScriptOfHotkey(int commandCode, ScriptSelection selection)
        {
            if (_commands.GetRequiredService<IScriptsManager>().GetScripts().FirstOrDefault(script => script.Enabled && script.HotkeyCommandIdentifier == commandCode) is not { } script)
            {
                return false;
            }

            RunToolbarScript(script, new ScriptOptionsProvider(() => selection.SelectedFiles, () => selection.LineNumber, () => selection.ColumnNumber));
            return true;
        }

        /// <summary>As <c>WorkingDirectoryToolStripSplitButton.RefreshContent</c>: the caption of the recent repository menus.</summary>
        public string WorkingDirectoryCaption
        {
            get
            {
                string path = Module.WorkingDir;
                if (string.IsNullOrWhiteSpace(path))
                {
                    return "";
                }

                IList<Repository> recentRepositoryHistory = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistory.LoadRecentHistoryAsync);
                List<RecentRepoInfo> pinnedRepos = [];
                RecentRepoSplitter splitter = new() { MeasureFont = AppSettings.Font };
                splitter.SplitRecentRepos(recentRepositoryHistory, pinnedRepos, pinnedRepos);
                RecentRepoInfo? info = pinnedRepos.Find(e => e.Repo.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
                return PathUtil.GetDisplayPath(info?.Caption ?? path);
            }
        }

        /// <summary>As <c>toolStripWorktrees_DropDownOpening</c>: the worktrees, then create, prune and manage.</summary>
        public async Task<BrowseWorktreeMenu> GetWorktreeMenuAsync()
        {
            if (!Module.IsValidGitWorkingDir())
            {
                return new BrowseWorktreeMenu(0, []);
            }

            IGitModule module = Module;
            await TaskScheduler.Default;
            IReadOnlyList<GitWorktree> worktrees = module.GetWorktrees();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string currentWorkingDir = module.WorkingDir.TrimEnd(Path.DirectorySeparatorChar);
            List<BrowseMenuItem> items = [.. worktrees.Select(worktree =>
            {
                bool isCurrent = string.Equals(worktree.Path.TrimEnd(Path.DirectorySeparatorChar), currentWorkingDir, StringComparison.OrdinalIgnoreCase);
                string displayName = worktree.GetDisplayName(Path.GetFileName(worktree.Path.TrimEnd(Path.DirectorySeparatorChar)));
                return new BrowseMenuItem(displayName.Replace("_", "__"), null, "WorkTree")
                {
                    IsChecked = isCurrent,
                    IsEnabled = !isCurrent && !worktree.IsDeleted,
                    Invoke = () => OpenWorktree(worktree.Path),
                };
            })];
            items.Add(BrowseMenuItem.Separator);
            string mainPath = worktrees.Count > 0 ? worktrees[0].Path : module.WorkingDir;
            items.Add(new BrowseMenuItem(TranslatedStrings.CreateWorktree, null, "WorkTree") { Invoke = () => RunAndRefresh(owner => _commands.WorktreeCreate(owner, mainPath)) });
            items.Add(new BrowseMenuItem(TranslatedStrings.PruneWorktrees, null) { Invoke = () => RunAndRefresh(owner => _commands.StartCommandLineProcessDialog(owner, command: null, "worktree prune")) });
            items.Add(new BrowseMenuItem(TranslatedStrings.ManageWorktrees, BrowseCommand.ManageWorktrees));
            return new BrowseWorktreeMenu(worktrees.Count, items);

            void RunAndRefresh(Func<IWin32Window, bool> action) => AvaloniaUi.RunInHostContext(() =>
            {
                if (action(Owner))
                {
                    RepositoryChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        // As WorktreeToolStripMenuItem_Click: the worktree is shown in this window.
        private void OpenWorktree(string path) => AvaloniaUi.RunInHostContext(() =>
        {
            if (!Directory.Exists(path))
            {
                MessageBoxes.ShowError(Owner, string.Format(TranslatedStrings.WorktreeDirectoryNotFound, path), TranslatedStrings.Error);
                return;
            }

            _session.SetWorkingDir(Path.GetFullPath(path));
        });

        /// <summary>As <c>FillUserShells</c>: the shells with an executable, the default one (Git bash) first.</summary>
        public IReadOnlyList<BrowseShell> GetShells()
        {
            List<BrowseShell> shells = [.. _commands.GetRequiredService<IShellProvider>().GetShells()
                .Where(shell => shell.HasExecutable)
                .Select(shell => new BrowseShell(shell.Name, EmbeddedIcons.ForAvalonia(shell.Icon), shell))];
            int defaultShell = shells.FindIndex(shell => string.Equals(shell.Name, BashShell.ShellName, StringComparison.InvariantCultureIgnoreCase));
            if (defaultShell > 0)
            {
                BrowseShell shell = shells[defaultShell];
                shells.RemoveAt(defaultShell);
                shells.Insert(0, shell);
            }

            return shells;
        }

        /// <summary>As <c>LoadUserMenu</c>: the enabled scripts shown in the user menu bar.</summary>
        public IReadOnlyList<BrowseMenuItem> GetToolbarScripts()
            => [.. _commands.GetRequiredService<IScriptsManager>().GetScripts()
                .Where(script => script.Enabled && script.OnEvent == ScriptEvent.ShowInUserMenuBar)
                .Select(script => new BrowseMenuItem((script.Name ?? "").Replace("_", "__"), null, script.GetIcon()) { Invoke = () => RunToolbarScript(script) })];

        // As ExecuteCommand of a script: run with the owner of the window, the grid refreshed if the script asks for it.
        private void RunToolbarScript(ScriptInfo script, ScriptOptionsProvider? options = null) => AvaloniaUi.RunInHostContext(() =>
        {
            if (_commands.GetRequiredService<IScriptsRunner>().RunScript(script, Owner, _commands, options ?? new ScriptOptionsProvider(() => [], () => null, () => null)))
            {
                RepositoryChanged?.Invoke(this, EventArgs.Empty);
            }
        });

        public bool ShellsRunInConsole => !OperatingSystem.IsWindows();

        /// <summary>As <c>userShell_Click</c>.</summary>
        public void RunShell(BrowseShell shell) => AvaloniaUi.RunInHostContext(() =>
        {
            IShellDescriptor descriptor = (IShellDescriptor)shell.Shell;
            try
            {
                Executable executable = new(descriptor.ExecutablePath!, Module.WorkingDir);
                executable.Start(createWindow: true, throwOnErrorExit: false); // throwOnErrorExit would redirect the output
            }
            catch (Exception exception)
            {
                MessageBoxes.FailedToRunShell(Owner, descriptor.Name, exception);
            }
        });
    }
}
