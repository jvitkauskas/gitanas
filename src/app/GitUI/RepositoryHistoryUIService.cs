using System.Runtime.InteropServices;
using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.CommandsDialogs;
using Microsoft.VisualStudio.Threading;

namespace GitUI;

/// <summary>
///  An item of the menu of the recent or favourite repositories: a repository (opened by <see cref="Open"/>), a category of
///  repositories or a separator.
/// </summary>
/// <param name="Text">The text, with the access key of the number of the repository (e.g. "&amp;1: path").</param>
/// <param name="IsPinned">Whether the repository is pinned (shown with the pin image).</param>
/// <param name="BranchName">The current branch of the repository, if known (shown as the shortcut of the item).</param>
/// <param name="ToolTip">The full path of the repository, if the text shortens it.</param>
public sealed record RepositoryMenuItem(string Text, bool IsPinned = false, string? BranchName = null, string? ToolTip = null)
{
    /// <summary>A separator.</summary>
    public static RepositoryMenuItem Separator { get; } = new("-");

    /// <summary>The repositories of a category.</summary>
    public IReadOnlyList<RepositoryMenuItem> Children { get; init; } = [];

    /// <summary>Opens the repository: in this window, or in a new instance with Ctrl.</summary>
    public Action? Open { get; init; }

    public bool IsSeparator => ReferenceEquals(this, Separator);
}

/// <summary>
///  Represents a service for managing the git repository history.
/// </summary>
public interface IRepositoryHistoryUIService
{
    /// <summary>
    ///  Occurs whenever the git module changes.
    /// </summary>
    event EventHandler<GitModuleEventArgs> GitModuleChanged;

    /// <summary>
    ///  The "Favourite repositories" menu, by category.
    ///  Both the submenu to the WorkingDir button in Browse and menu in Dashboard.
    /// </summary>
    IReadOnlyList<RepositoryMenuItem> GetFavouriteRepositoriesMenu();

    /// <summary>
    ///  The "Recent repositories" menu.
    ///  Both the WorkingDir button in Browse and menu in Dashboard.
    /// </summary>
    IReadOnlyList<RepositoryMenuItem> GetRecentRepositoriesMenu();

    /// <summary>
    ///  Start updating the branch name cache.
    /// </summary>
    /// <param name="onlyIfEmpty">Start updating only if the cache is empty.</param>
    void TriggerBranchNameCacheUpdate(bool onlyIfEmpty = false);
}

internal sealed class RepositoryHistoryUIService : IRepositoryHistoryUIService
{
    private readonly IGitExecutorProvider _executorProvider;
    private readonly IRepositoryCurrentBranchNameCache _branchNameCache;
    private readonly IInvalidRepositoryRemover _invalidRepositoryRemover;
    private readonly CancellationTokenSequence _branchCacheSequence = new();
    private JoinableTask? _branchCacheUpdateTask;
    private bool _firstLoad = true;

    public event EventHandler<GitModuleEventArgs>? GitModuleChanged;

    internal RepositoryHistoryUIService(IGitExecutorProvider executorProvider, IRepositoryCurrentBranchNameCache branchNameCache, IInvalidRepositoryRemover invalidRepositoryRemover)
    {
        _executorProvider = executorProvider;
        _branchNameCache = branchNameCache;
        _invalidRepositoryRemover = invalidRepositoryRemover;
    }

    private RepositoryMenuItem CreateRepositoryItem(Repository repo, string? caption, int number, bool anchored = false)
    {
        string numberString = number switch { < 10 => $"&{number}", 10 => "1&0", _ => $"{number}" };
        return new RepositoryMenuItem(
            $"{numberString}: {caption}",
            IsPinned: anchored,
            BranchName: _branchNameCache.GetCachedBranchName(repo.Path),
            ToolTip: repo.Path != caption ? repo.Path : null)
        {
            Open = () => OpenRepo(repo.Path),
        };
    }

    private void ChangeWorkingDir(string path)
    {
        GitModule module = new(_executorProvider, path);
        if (module.IsValidGitWorkingDir())
        {
            GitModuleChanged?.Invoke(this, new GitModuleEventArgs(module));
            return;
        }

        _invalidRepositoryRemover.ShowDeleteInvalidRepositoryDialog(path);
    }

    private void OpenRepo(string repoPath)
    {
        if (!IsOnlyControlKeyDown())
        {
            ChangeWorkingDir(repoPath);
            return;
        }

        GitUICommands.LaunchBrowse(repoPath);
    }

    public IReadOnlyList<RepositoryMenuItem> GetFavouriteRepositoriesMenu()
    {
        JoinableTask? branchCacheUpdateTask = _branchCacheUpdateTask;
        if (branchCacheUpdateTask is not null && branchCacheUpdateTask.IsCompleted)
        {
            try
            {
                branchCacheUpdateTask.Join();
            }
            catch (OperationCanceledException)
            {
                // OK
            }
        }

        IList<Repository> repositoryHistory = ThreadHelper.JoinableTaskFactory.Run(
            RepositoryHistoryManager.Locals.LoadFavouriteHistoryAsync);

        if (repositoryHistory.Count < 1)
        {
            return [];
        }

        return GetFavouriteRepositoriesMenu(repositoryHistory);
    }

    private IReadOnlyList<RepositoryMenuItem> GetFavouriteRepositoriesMenu(in IList<Repository> repositoryHistory)
    {
        List<RecentRepoInfo> pinnedRepos = [];
        List<RecentRepoInfo> allRecentRepos = [];

        RecentRepoSplitter splitter = new()
        {
            MeasureFont = OperatingSystem.IsWindowsVersionAtLeast(6, 1) ? SystemFonts.MenuFont?.ToFontDescriptor() : AppSettings.Font,
        };

        splitter.SplitRecentRepos(repositoryHistory, pinnedRepos, allRecentRepos);

        return [.. pinnedRepos.Union(allRecentRepos).GroupBy(k => k.Repo.Category).OrderBy(k => k.Key).Select(repos =>
        {
            int number = 0;
            return new RepositoryMenuItem(repos.Key ?? "")
            {
                Children = [.. repos.Select(r => CreateRepositoryItem(r.Repo, r.Caption, ++number))],
            };
        })];
    }

    public IReadOnlyList<RepositoryMenuItem> GetRecentRepositoriesMenu()
    {
        JoinableTask? branchCacheUpdateTask = _branchCacheUpdateTask;
        if (branchCacheUpdateTask is not null && branchCacheUpdateTask.IsCompleted)
        {
            try
            {
                branchCacheUpdateTask.Join();
            }
            catch (OperationCanceledException)
            {
                // OK
            }
        }

        List<RecentRepoInfo> pinnedRepos = [];
        List<RecentRepoInfo> allRecentRepos = [];

        IList<Repository> repositoryHistory = ThreadHelper.JoinableTaskFactory.Run(
            RepositoryHistoryManager.Locals.LoadRecentHistoryAsync);

        if (repositoryHistory.Count < 1)
        {
            return [];
        }

        RecentRepoSplitter splitter = new()
        {
            MeasureFont = OperatingSystem.IsWindowsVersionAtLeast(6, 1) ? SystemFonts.MenuFont?.ToFontDescriptor() : AppSettings.Font,
        };

        splitter.SplitRecentRepos(repositoryHistory, pinnedRepos, allRecentRepos);

        List<RepositoryMenuItem> items = [];
        int number = 0;
        foreach (RecentRepoInfo repo in CollectionsMarshal.AsSpan(pinnedRepos))
        {
            items.Add(CreateRepositoryItem(repo.Repo, repo.Caption, ++number, repo.Anchored));
        }

        if (allRecentRepos.Count > 0)
        {
            if (pinnedRepos.Count > 0)
            {
                items.Add(RepositoryMenuItem.Separator);
            }

            foreach (RecentRepoInfo repo in CollectionsMarshal.AsSpan(allRecentRepos))
            {
                items.Add(CreateRepositoryItem(repo.Repo, repo.Caption, ++number, repo.Anchored));
            }
        }

        return items;
    }

    public void TriggerBranchNameCacheUpdate(bool onlyIfEmpty = false)
    {
        // Race condition for OnLoad vs OnRevisionsLoaded
        // (onlyIfEmpty: true by OnLoad, false by OnRevisionsLoaded)
        bool skipUpdate;
        if (_branchNameCache.IsEmpty)
        {
            // first OnLoad or OnRevisionsLoaded, mark cache as non empty
            skipUpdate = false;
            const string invalidPath = ":::invalid:::";
            _branchNameCache.UpdateCache(invalidPath, "");
        }
        else if (_firstLoad)
        {
            // cache exists so either Dashbord filled it or 'other trigger' started
            skipUpdate = true;

            // suppress second load if OnLoad is first
            // (if OnRevisionsLoaded is first load will be done twice but the the load is very quick).
            _firstLoad = onlyIfEmpty;
        }
        else
        {
            // Following OnRevisionsLoaded
            skipUpdate = onlyIfEmpty;
        }

        if (skipUpdate)
        {
            return;
        }

        _branchCacheUpdateTask = ThreadHelper.JoinableTaskFactory.RunAsync(UpdateBranchNameCacheAsync);

        return;

        async Task UpdateBranchNameCacheAsync()
        {
            CancellationToken cancellationToken = _branchCacheSequence.Next();
            IList<Repository> recentHistory = await RepositoryHistoryManager.Locals.LoadRecentHistoryAsync();
            IList<Repository> favouriteHistory = await RepositoryHistoryManager.Locals.LoadFavouriteHistoryAsync();

            string[] paths = [.. recentHistory
                .Concat(favouriteHistory)
                .Select(r => r.Path)
                .Distinct(StringComparer.InvariantCulture)];

            if (paths.Length > 0)
            {
                UpdateBranchNamesCache(paths, cancellationToken);
            }

            return;

            void UpdateBranchNamesCache(IReadOnlyList<string> paths, CancellationToken cancellationToken)
            {
                const int MaxBranchNameFetchParallelism = 4;
                paths
                    .AsParallel()
                    .WithCancellation(cancellationToken)
                    .WithDegreeOfParallelism(Math.Min(MaxBranchNameFetchParallelism, Math.Max(1, Environment.ProcessorCount / 2)))
                    .ForAll(path => _ = _branchNameCache.GetUpdatedBranchName(path));
            }
        }
    }

    /// <summary>Whether Ctrl is the only modifier key pressed (as the WinForms <c>Control.ModifierKeys == Keys.Control</c>).</summary>
    private static bool IsOnlyControlKeyDown()
    {
        static bool IsDown(int virtualKey) => (GetKeyState(virtualKey) & 0x8000) != 0;

        const int VK_SHIFT = 0x10;
        const int VK_CONTROL = 0x11;
        const int VK_MENU = 0x12;

        // Polling the global keyboard state is Windows-only. Other platforms open the repository in this window.
        return OperatingSystem.IsWindows() && IsDown(VK_CONTROL) && !IsDown(VK_SHIFT) && !IsDown(VK_MENU);
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(RepositoryHistoryUIService service)
    {
        internal RepositoryMenuItem CreateRepositoryItem(Repository repo, string? caption, int number)
            => service.CreateRepositoryItem(repo, caption, number);

        internal IReadOnlyList<RepositoryMenuItem> GetFavouriteRepositoriesMenu(in IList<Repository> repositoryHistory)
            => service.GetFavouriteRepositoriesMenu(repositoryHistory);
    }
}
