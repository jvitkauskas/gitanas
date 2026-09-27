using CommonTestUtils;
using GitCommands.UserRepositoryHistory;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the dashboard (the port of <c>Dashboard</c> and <c>UserRepositoriesList</c>).</summary>
[TestFixture]
public sealed class DashboardViewModelTests
{
    [Test]
    public async Task The_recent_repositories_come_first_then_the_categories_in_order_with_their_branches()
    {
        FakeDashboardHost host = new() { TileWidth = 240 };
        DashboardViewModel viewModel = Create(host);

        viewModel.Groups.Select(g => g.Header).Should().Equal("Recent repositories", "Personal", "Work");
        viewModel.Groups[0].IsRecent.Should().BeTrue();
        viewModel.Groups[0].Items.Select(i => i.Caption).Should().Equal("gitextensions", "missing", "tools");
        viewModel.Groups[1].Items.Select(i => i.Caption).Should().Equal("dotfiles");
        viewModel.Groups[2].Items.Select(i => i.Path).Should().Equal(TestPaths.Native(@"C:\work\api"), TestPaths.Native(@"C:\work\web"));
        viewModel.Groups[2].Items.Should().OnlyContain(i => i.IsFavourite && i.HasCategory);
        viewModel.TileWidth.Should().Be(240);

        await viewModel.LoadingStatuses;
        DashboardRepositoryItem gitExtensions = viewModel.Groups[0].Items[0];
        gitExtensions.BranchName.Should().Be("master");
        gitExtensions.IsInvalid.Should().BeFalse();
        viewModel.Groups[0].Items[1].IsInvalid.Should().BeTrue("the directory does not exist");
        viewModel.Groups[0].Items[1].BranchName.Should().BeEmpty();
    }

    [Test]
    public void The_search_filters_the_repositories_without_reading_the_history_again()
    {
        FakeDashboardHost host = new();
        DashboardViewModel viewModel = Create(host);

        viewModel.SearchText = "work";

        host.Loads.Should().Equal((string.Empty, true), ("work", false));
        viewModel.Groups.Select(g => g.Header).Should().Equal("Work");
        viewModel.Repositories.Select(r => r.Caption).Should().Equal("api", "web");

        viewModel.SearchText = "nothing";
        viewModel.Groups.Should().BeEmpty();
    }

    [Test]
    public void Opening_a_repository_switches_to_it_or_offers_to_remove_it_when_missing()
    {
        FakeDashboardHost host = new();
        DashboardViewModel viewModel = Create(host);

        viewModel.OpenCommand.Execute(viewModel.Groups[0].Items[0]);
        host.Opened.Should().Equal(TestPaths.Native(@"C:\src\gitextensions"));

        viewModel.OpenCommand.Execute(viewModel.Groups[0].Items[1]);
        host.InvalidRemoved.Should().Equal(TestPaths.Native(@"C:\gone\missing"));
        host.Opened.Should().HaveCount(1);
        host.Loads.Should().HaveCount(2, "the list is read again once the missing repository is removed");

        // Enter in the search box opens the first repository listed.
        viewModel.SearchText = "tools";
        viewModel.OpenFirst();
        host.Opened.Should().Equal(TestPaths.Native(@"C:\src\gitextensions"), TestPaths.Native(@"C:\src\tools"));
    }

    [Test]
    public async Task The_repository_menu_moves_it_between_categories_and_removes_it()
    {
        FakeDashboardHost host = new() { CategoryName = "New" };
        DashboardViewModel viewModel = Create(host);
        await viewModel.LoadingStatuses;

        DashboardRepositoryItem recent = viewModel.Groups[0].Items[0];
        IReadOnlyList<DashboardMenuItem> menu = viewModel.GetRepositoryMenu(recent);
        menu.Select(m => m.Header).Should().Equal("Show in folder", "-", "Categories", "-", "Remove project from the list", "Remove missing projects from the list");

        // Categories: (none) is disabled without a category, then the categories, a separator and "Add new...".
        IReadOnlyList<DashboardMenuItem> categories = menu[2].Children!;
        categories.Select(m => m.Header).Should().Equal("(none)", "Personal", "Work", "-", "Add new...");
        categories[0].IsEnabled.Should().BeFalse();
        categories[1].IsEnabled.Should().BeTrue();

        categories[2].Execute!();
        host.Assigned.Should().Equal((TestPaths.Native(@"C:\src\gitextensions"), "Work"));

        categories[4].Execute!();
        host.Prompts.Should().ContainSingle().Which.Original.Should().BeNull();
        host.Prompts[0].Categories.Should().Equal("Personal", "Work");
        host.Assigned[^1].Should().Be((TestPaths.Native(@"C:\src\gitextensions"), "New"));

        menu[0].Execute!();
        host.ShownInFolder.Should().Equal(TestPaths.Native(@"C:\src\gitextensions"));

        menu[4].Execute!();
        host.RemovedRecent.Should().Equal(TestPaths.Native(@"C:\src\gitextensions"));

        // A favourite is removed from the favourites, and its own category is disabled.
        DashboardRepositoryItem favourite = viewModel.Groups.Single(g => g.Category == "Work").Items[0];
        IReadOnlyList<DashboardMenuItem> favouriteMenu = viewModel.GetRepositoryMenu(favourite);
        favouriteMenu[2].Children!.Where(m => !m.IsSeparator).Select(m => (m.Header, m.IsEnabled)).Should().Equal(
            ("(none)", true), ("Personal", true), ("Work", false), ("Add new...", true));
        favouriteMenu[4].Execute!();
        host.RemovedFavourite.Should().Equal(TestPaths.Native(@"C:\work\api"));

        favouriteMenu[5].Execute!();
        host.MissingRemoved.Should().Be(1);
    }

    [Test]
    public void The_group_menus_clear_the_recent_repositories_and_rename_or_delete_a_category()
    {
        FakeDashboardHost host = new() { CategoryName = "Office" };
        DashboardViewModel viewModel = Create(host);

        IReadOnlyList<DashboardMenuItem> recentMenu = viewModel.GetGroupMenu(viewModel.Groups[0]);
        recentMenu.Select(m => m.Header).Should().Equal("Clear all recent repositories");
        host.ConfirmAnswer = false;
        recentMenu[0].Execute!();
        host.RemovedRecent.Should().BeEmpty("the user did not confirm");
        host.ConfirmAnswer = true;
        recentMenu[0].Execute!();
        host.RemovedRecent.Should().HaveCount(6, "every listed repository is removed from the recent ones");

        DashboardGroup work = viewModel.Groups.Single(g => g.Category == "Work");
        IReadOnlyList<DashboardMenuItem> categoryMenu = viewModel.GetGroupMenu(work);
        categoryMenu.Select(m => m.Header).Should().Equal("Rename category", "Delete category");

        categoryMenu[0].Execute!();
        host.Prompts[^1].Original.Should().Be("Work");
        host.Prompts[^1].Categories.Should().Equal("Personal");
        host.Assigned.Should().Equal((TestPaths.Native(@"C:\work\api"), "Office"), (TestPaths.Native(@"C:\work\web"), "Office"));

        host.Assigned.Clear();
        categoryMenu[1].Execute!();
        host.Confirmations[^1].Should().Be(("Do you want to delete category \"Work\" with 2 repositories?\n\nThe action cannot be undone.", "Delete Category"));
        host.Assigned.Should().Equal((TestPaths.Native(@"C:\work\api"), null), (TestPaths.Native(@"C:\work\web"), null));
    }

    [Test]
    public void The_links_run_their_action_and_a_link_per_git_hoster_clones_a_fork()
    {
        FakeDashboardHost host = new() { GitHosters = ["GitHub"] };
        DashboardViewModel viewModel = Create(host);

        viewModel.StartLinks.Select(l => (l.Text, l.Icon)).Should().Equal(
            ("Create new repository", "RepoCreate"), ("Open repository", "RepoOpen"), ("Clone repository", "CloneRepoGit"), ("Clone GitHub repository", "CloneRepoGitHub"));
        viewModel.ContributeLinks.Select(l => l.Text).Should().Equal("Develop", "Issues");

        viewModel.StartLinks[1].Command.Execute(null);
        viewModel.StartLinks[3].Command.Execute(null);
        viewModel.ContributeLinks[1].Command.Execute(null);
        host.Links.Should().Equal(DashboardLink.OpenRepository, DashboardLink.Issues);
        host.Forks.Should().Equal(0);
    }

    [Test]
    public void A_dropped_directory_is_opened_only_if_it_is_a_repository()
    {
        FakeDashboardHost host = new();
        DashboardViewModel viewModel = Create(host);
        string directory = Path.GetTempPath();

        viewModel.OpenDroppedDirectory(directory);
        host.InvalidMessages.Should().Equal("Cannot open the folder");
        host.Opened.Should().BeEmpty();

        host.ValidPaths.Add(directory);
        viewModel.OpenDroppedDirectory(directory);
        host.Opened.Should().Equal(directory);
    }

    [Test]
    public void The_recent_repositories_settings_reload_the_list_once_saved()
    {
        FakeDashboardHost host = new();
        DashboardViewModel viewModel = Create(host);

        viewModel.ConfigureRecentRepositoriesCommand.Execute(null);
        host.Loads.Should().HaveCount(1, "not saved");

        host.SettingsSaved = true;
        viewModel.ConfigureRecentRepositoriesCommand.Execute(null);
        host.Loads.Should().HaveCount(2);
    }

    internal static DashboardViewModel Create(FakeDashboardHost host)
    {
        DashboardViewModel viewModel = new(new DashboardStrings(), new UserRepositoriesListStrings(), host);
        viewModel.Refresh();
        return viewModel;
    }

    /// <summary>A repository history in memory: three recent repositories (one missing) and three favourites in two categories.</summary>
    internal sealed class FakeDashboardHost : IDashboardHost
    {
        private readonly List<Repository> _recent =
        [
            new(TestPaths.Native(@"C:\src\gitextensions")),
            new(TestPaths.Native(@"C:\gone\missing")),
            new(TestPaths.Native(@"C:\src\tools")),
        ];

        private readonly List<Repository> _favourites =
        [
            new(TestPaths.Native(@"C:\work\web")) { Category = "Work" },
            new(TestPaths.Native(@"C:\home\dotfiles")) { Category = "Personal" },
            new(TestPaths.Native(@"C:\work\api")) { Category = "Work" },
        ];

        public FakeDashboardHost()
        {
            ValidPaths = [.. _recent.Concat(_favourites).Select(r => r.Path).Where(p => !p.Contains("gone"))];
        }

        public IReadOnlyList<string> GitHosters { get; set; } = [];

        /// <summary>The width of the repositories combobox, or NaN to fit the captions.</summary>
        public double TileWidth { get; init; } = double.NaN;

        public HashSet<string> ValidPaths { get; }

        public string? CategoryName { get; init; }

        public bool ConfirmAnswer { get; set; } = true;

        public bool SettingsSaved { get; set; }

        public List<(string Filter, bool Reload)> Loads { get; } = [];

        public List<string> Opened { get; } = [];

        public List<string> InvalidRemoved { get; } = [];

        public List<(string Path, string? Category)> Assigned { get; } = [];

        public List<string> RemovedRecent { get; } = [];

        public List<string> RemovedFavourite { get; } = [];

        public int MissingRemoved { get; private set; }

        public List<string> ShownInFolder { get; } = [];

        public List<(IReadOnlyList<string> Categories, string? Original)> Prompts { get; } = [];

        public List<(string Question, string Caption)> Confirmations { get; } = [];

        public List<string> InvalidMessages { get; } = [];

        public List<DashboardLink> Links { get; } = [];

        public List<int> Forks { get; } = [];

        public DashboardRepositories LoadRepositories(string filter, bool reload)
        {
            Loads.Add((filter, reload));
            return new DashboardRepositories(Select(_recent), Select(_favourites.OrderBy(r => r.Path)), TileWidth);

            IReadOnlyList<RecentRepoInfo> Select(IEnumerable<Repository> repositories)
                => [.. repositories
                    .Where(r => r.Path.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                    .Select(r => new RecentRepoInfo(r, topRepo: false, anchored: false) { Caption = Path.GetFileName(r.Path) })];
        }

        public Task<DashboardRepositoryStatus> GetStatusAsync(string path, CancellationToken cancellationToken)
            => Task.FromResult(ValidPaths.Contains(path) ? new DashboardRepositoryStatus(true, "master") : new DashboardRepositoryStatus(false, ""));

        public bool IsValidGitWorkingDir(string path) => ValidPaths.Contains(path);

        public void OpenRepository(string path) => Opened.Add(path);

        public bool RemoveInvalidRepository(string path)
        {
            InvalidRemoved.Add(path);
            return true;
        }

        public void AssignCategory(Repository repository, string? category) => Assigned.Add((repository.Path, category));

        public void RemoveRecent(string path) => RemovedRecent.Add(path);

        public void RemoveFavourite(string path) => RemovedFavourite.Add(path);

        public void RemoveMissingRepositories() => MissingRemoved++;

        public void ShowInFolder(string path) => ShownInFolder.Add(path);

        public string? PromptCategoryName(IReadOnlyList<string> existingCategories, string? originalName)
        {
            Prompts.Add((existingCategories, originalName));
            return CategoryName;
        }

        public bool Confirm(string question, string caption)
        {
            Confirmations.Add((question, caption));
            return ConfirmAnswer;
        }

        public void ShowInvalidRepository(string caption) => InvalidMessages.Add(caption);

        public bool ShowRecentReposSettings() => SettingsSaved;

        public void Run(DashboardLink link) => Links.Add(link);

        public void CloneFork(int gitHoster) => Forks.Add(gitHoster);
    }
}
