using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.CommandsDialogs.BrowseDialog.DashboardControl;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The dashboard of the Avalonia main window (docs/avalonia-port/PLAN.md, phase 7): the repository history and the dialogs
///  of <c>Dashboard</c> and <c>UserRepositoriesList</c>.
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The history of the local repositories of the dashboard and the main window, instead of the user's (tests).</summary>
    internal static ILocalRepositoryManager? RepositoryHistoryForTests { get; set; }

    private static ILocalRepositoryManager RepositoryHistory => RepositoryHistoryForTests ?? RepositoryHistoryManager.Locals;

    /// <summary>
    ///  As <c>UserRepositoriesListController</c> (with the history of <see cref="RepositoryHistory"/>), the handlers of
    ///  <c>Dashboard</c> and <c>UserRepositoriesList</c>, and <c>InvalidRepositoryRemover</c> owned by the main window.
    /// </summary>
    private sealed class DashboardHost(IGitUICommands commands, BrowseWindow window, BrowseSession session) : IDashboardHost
    {
        private readonly IRepositoryCurrentBranchNameCache _branchNameCache = commands.GetService<IRepositoryCurrentBranchNameCache>()
            ?? new RepositoryCurrentBranchNameCache(new RepositoryCurrentBranchNameProvider(commands.GetRequiredService<IGitExecutorProvider>()));

        // The unfiltered history, for a fast search.
        private IList<Repository>? _allRecentRepositories;
        private IList<Repository>? _allFavouriteRepositories;

        public IReadOnlyList<string> GitHosters => [.. PluginRegistry.GitHosters.Select(hoster => hoster.Name ?? "")];

        private NativeWindowOwner Owner => new(window);

        public DashboardRepositories LoadRepositories(string filter, bool reload)
        {
            if (reload)
            {
                // As ClearCache: the branch names are shared with the repositories menus.
                _allRecentRepositories = null;
                _allFavouriteRepositories = null;
                _branchNameCache.InvalidateAll();
            }

            // As PreRenderRepositories.
            RecentRepoSplitter splitter = new()
            {
                MeasureFont = AppSettings.Font,
                MaxTopRepositories = AppSettings.MaxTopRepositories,
                RecentReposComboMinWidth = AppSettings.RecentReposComboMinWidth,
                ShorteningStrategy = AppSettings.ShorteningRecentRepoPathStrategy,
                SortRecentRepos = AppSettings.SortRecentRepos,
                SortTopRepos = AppSettings.SortTopRepos,
            };

            _allRecentRepositories ??= ThreadHelper.JoinableTaskFactory.Run(RepositoryHistory.LoadRecentHistoryAsync);
            _allFavouriteRepositories ??= ThreadHelper.JoinableTaskFactory.Run(RepositoryHistory.LoadFavouriteHistoryAsync);
            IReadOnlyList<RecentRepoInfo> recent = Split(_allRecentRepositories);
            IReadOnlyList<RecentRepoInfo> favourites = Split(_allFavouriteRepositories);
            return new DashboardRepositories(recent, favourites, GetTileWidth());

            IReadOnlyList<RecentRepoInfo> Split(IList<Repository> repositories)
            {
                List<RecentRepoInfo> topRepos = [];
                List<RecentRepoInfo> recentRepos = [];
                splitter.SplitRecentRepos(Filter(repositories, filter), topRepos, recentRepos);
                return [.. topRepos.Union(recentRepos)];
            }

            static IList<Repository> Filter(IList<Repository> repositories, string pattern)
                => pattern.Length == 0 ? repositories : [.. repositories.Where(r => r.Path.Contains(pattern, StringComparison.CurrentCultureIgnoreCase))];
        }

        public Task<DashboardRepositoryStatus> GetStatusAsync(string path, CancellationToken cancellationToken)
            => Task.Run(
                () =>
                {
                    bool isValid = GitModule.IsValidGitWorkingDir(path);
                    string branchName = isValid && AppSettings.ShowRepoCurrentBranch && !GitModule.IsBareRepository(path)
                        ? _branchNameCache.GetCurrentBranchName(path)
                        : "";
                    return new DashboardRepositoryStatus(isValid, branchName);
                },
                cancellationToken);

        public bool IsValidGitWorkingDir(string path) => GitModule.IsValidGitWorkingDir(path);

        public void OpenRepository(string path)
            => session.SetGitModule(new GitModule(commands.GetRequiredService<IGitExecutorProvider>(), path));

        // As InvalidRepositoryRemover.ShowDeleteInvalidRepositoryDialog.
        public bool RemoveInvalidRepository(string path) => AvaloniaUi.RunInHostContext(() =>
        {
            int invalidPathCount = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistory.LoadRecentHistoryAsync)
                .Count(repo => !GitModule.IsValidGitWorkingDir(repo.Path));

            TaskDialogPage page = new()
            {
                Heading = TranslatedStrings.DirectoryInvalidRepository,
                Caption = TranslatedStrings.Open,
                Icon = TaskDialogIcon.Error,
                Buttons = { TaskDialogButton.Cancel },
                AllowCancel = true,
                SizeToContent = true,
            };
            TaskDialogCommandLinkButton removeSelected = new(TranslatedStrings.RemoveSelectedInvalidRepository);
            page.Buttons.Add(removeSelected);
            TaskDialogCommandLinkButton removeAll = new(string.Format(TranslatedStrings.RemoveAllInvalidRepositories, invalidPathCount));
            if (invalidPathCount > 1)
            {
                page.Buttons.Add(removeAll);
            }

            TaskDialogButton result = TaskDialog.ShowDialog(Owner, page);
            if (result == removeSelected)
            {
                ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistory.RemoveRecentAsync(path));
                return true;
            }

            if (result == removeAll)
            {
                RemoveMissingRepositories();
                return true;
            }

            return false;
        });

        public void AssignCategory(Repository repository, string? category)
            => ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistory.AssignCategoryAsync(repository, category));

        public void RemoveRecent(string path) => ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistory.RemoveRecentAsync(path));

        public void RemoveFavourite(string path) => ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistory.RemoveFavouriteAsync(path));

        public void RemoveMissingRepositories()
            => ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistory.RemoveInvalidRepositoriesAsync(GitModule.IsValidGitWorkingDir));

        public void ShowInFolder(string path) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenWithFileExplorer(path));

        // As UserRepositoriesList.PromptCategoryName.
        public string? PromptCategoryName(IReadOnlyList<string> existingCategories, string? originalName) => AvaloniaUi.RunInHostContext(() =>
        {
            return TryShowDashboardCategoryTitle(Owner, existingCategories, originalName, out string? name) ? name : null;
        });

        public bool Confirm(string question, string caption) => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.Show(Owner, question, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes);

        public void ShowInvalidRepository(string caption) => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.Show(Owner, TranslatedStrings.DirectoryInvalidRepository, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1));

        public bool ShowRecentReposSettings() => AvaloniaUi.RunInHostContext(() =>
        {
            return TryShowRecentReposSettings(Owner, out bool saved) && saved;
        });

        public void Run(DashboardLink link) => AvaloniaUi.RunInHostContext(() =>
        {
            switch (link)
            {
                case DashboardLink.CreateRepository:
                    commands.StartInitializeDialog(Owner, commands.Module.WorkingDir, OnGitModuleChanged);
                    break;
                case DashboardLink.OpenRepository:
                    if (TryShowOpenDirectory(Owner, commands.GetRequiredService<IGitExecutorProvider>(), currentModule: null, out IGitModule? module) && module is not null)
                    {
                        session.SetGitModule(module);
                    }

                    break;
                case DashboardLink.CloneRepository:
                    commands.StartCloneDialog(Owner, null, false, OnGitModuleChanged);
                    break;
                case DashboardLink.Develop:
                    OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/jvitkauskas/gitanas");
                    break;
                case DashboardLink.Donate:
                    OsShellUtil.OpenUrlInDefaultBrowser(DonationUrl);
                    break;
                case DashboardLink.Translate:
                    OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/jvitkauskas/gitanas/blob/main/CONTRIBUTING.md");
                    break;
                case DashboardLink.Issues:
                    UserEnvironmentInformation.CopyInformation();
                    OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/jvitkauskas/gitanas/issues");
                    break;
            }
        });

        public void CloneFork(int gitHoster) => AvaloniaUi.RunInHostContext(() =>
        {
            if (gitHoster < PluginRegistry.GitHosters.Count)
            {
                commands.StartCloneForkFromHoster(Owner, PluginRegistry.GitHosters[gitHoster], OnGitModuleChanged);
            }
        });

        // As GetTileSize with the width of the repositories combobox; else the view measures the longest caption.
        private static double GetTileWidth() => AppSettings.RecentReposComboMinWidth >= 1 ? AppSettings.RecentReposComboMinWidth + 50 : double.NaN;

        private void OnGitModuleChanged(object? sender, GitModuleEventArgs e) => session.SetGitModule(e.GitModule);
    }
}
