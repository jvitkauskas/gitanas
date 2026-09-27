using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.UserRepositoryHistory;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.BrowseDialog;

/// <summary>Strings of the dashboard; ids match <c>Dashboard</c>.</summary>
public sealed class DashboardStrings : ViewStrings
{
    public DashboardStrings()
        : base("Dashboard")
    {
        CloneFork = Add("_cloneFork", "Text", "Clone {0} repository");
        CloneRepository = Add("_cloneRepository", "Text", "Clone repository");
        CreateRepository = Add("_createRepository", "Text", "Create new repository");
        Develop = Add("_develop", "Text", "Develop");
        Issues = Add("_issues", "Text", "Issues");
        OpenRepository = Add("_openRepository", "Text", "Open repository");
        Contribute = Add("lblContribute", "Text", "Contribute");
    }

    public TranslatedText CloneFork { get; }

    public TranslatedText CloneRepository { get; }

    public TranslatedText CreateRepository { get; }

    public TranslatedText Develop { get; }

    public TranslatedText Issues { get; }

    public TranslatedText OpenRepository { get; }

    public TranslatedText Contribute { get; }
}

/// <summary>Strings of the repositories of the dashboard; ids match <c>UserRepositoriesList</c>.</summary>
public sealed class UserRepositoriesListStrings : ViewStrings
{
    public UserRepositoriesListStrings()
        : base("UserRepositoriesList")
    {
        CannotOpenTheFolder = Add("_cannotOpenTheFolder", "Text", "Cannot open the folder");
        ClearRecentCategoryCaption = Add("_clearRecentCategoryCaption", "Text", "Clear recent repositories");
        ClearRecentCategoryQuestion = Add("_clearRecentCategoryQuestion", "Text", "Do you want to clear the list of recent repositories?\n\nThe action cannot be undone.");
        DeleteCategoryCaption = Add("_deleteCategoryCaption", "Text", "Delete Category");
        DeleteCategoryQuestion = Add("_deleteCategoryQuestion", "Text", "Do you want to delete category \"{0}\" with {1} repositories?\n\nThe action cannot be undone.");
        GroupActions = Add("_groupActions", "Text", "Actions");
        GroupRecentRepositories = Add("_groupRecentRepositories", "Text", "Recent repositories");
        SearchPlaceholder = Add("_repositorySearchPlaceholder", "Text", "Search repositories...");
        RecentRepositories = Add("lblRecentRepositories", "Text", "Recent repositories");
        Configure = Add("mnuConfigure", "Text", "Recent repositories &settings");
        Categories = Add("tsmiCategories", "Text", "Categories");
        CategoryAdd = Add("tsmiCategoryAdd", "Text", "Add new...");
        CategoryClear = Add("tsmiCategoryClear", "Text", "Clear all recent repositories");
        CategoryDelete = Add("tsmiCategoryDelete", "Text", "Delete category");
        CategoryNone = Add("tsmiCategoryNone", "Text", "(none)");
        CategoryRename = Add("tsmiCategoryRename", "Text", "Rename category");
        OpenFolder = Add("tsmiOpenFolder", "Text", "Show in folder");
        RemoveFromList = Add("tsmiRemoveFromList", "Text", "Remove project from the list");
        RemoveMissingFromList = Add("tsmiRemoveMissingReposFromList", "Text", "Remove missing projects from the list");
    }

    public TranslatedText CannotOpenTheFolder { get; }

    public TranslatedText ClearRecentCategoryCaption { get; }

    public TranslatedText ClearRecentCategoryQuestion { get; }

    public TranslatedText DeleteCategoryCaption { get; }

    public TranslatedText DeleteCategoryQuestion { get; }

    public TranslatedText GroupActions { get; }

    public TranslatedText GroupRecentRepositories { get; }

    public TranslatedText SearchPlaceholder { get; }

    public TranslatedText RecentRepositories { get; }

    public TranslatedText Configure { get; }

    public TranslatedText Categories { get; }

    public TranslatedText CategoryAdd { get; }

    public TranslatedText CategoryClear { get; }

    public TranslatedText CategoryDelete { get; }

    public TranslatedText CategoryNone { get; }

    public TranslatedText CategoryRename { get; }

    public TranslatedText OpenFolder { get; }

    public TranslatedText RemoveFromList { get; }

    public TranslatedText RemoveMissingFromList { get; }
}

/// <summary>The recent and favourite repositories matching the search (as <c>UserRepositoriesListController.PreRenderRepositories</c>).</summary>
/// <param name="TileWidth">
///  The width of the tiles from the width of the repositories combobox (as <c>UserRepositoriesList.GetTileSize</c>);
///  <see cref="double.NaN"/> to fit the longest caption.
/// </param>
public sealed record DashboardRepositories(IReadOnlyList<RecentRepoInfo> Recent, IReadOnlyList<RecentRepoInfo> Favourites, double TileWidth);

/// <summary>Whether a repository of the dashboard exists, and its current branch (empty if not shown).</summary>
public sealed record DashboardRepositoryStatus(bool IsValid, string BranchName);

/// <summary>The links of the dashboard (the handlers of <c>Dashboard</c>).</summary>
public enum DashboardLink
{
    CreateRepository,
    OpenRepository,
    CloneRepository,
    Develop,
    Issues,
}

/// <summary>What the dashboard needs from the application: the repository history, the dialogs and the main window.</summary>
public interface IDashboardHost
{
    /// <summary>The names of the git hosting plugins, which add a link each (<c>PluginRegistry.GitHosters</c>).</summary>
    IReadOnlyList<string> GitHosters { get; }

    /// <summary>The repositories whose path contains <paramref name="filter"/>; <paramref name="reload"/> reads the history again.</summary>
    DashboardRepositories LoadRepositories(string filter, bool reload);

    /// <summary>Whether the repository exists, and its branch (on a background thread, as <c>ShowRecentRepositories</c>).</summary>
    Task<DashboardRepositoryStatus> GetStatusAsync(string path, CancellationToken cancellationToken);

    bool IsValidGitWorkingDir(string path);

    /// <summary>Opens the repository in the main window (<c>GitModuleChanged</c>).</summary>
    void OpenRepository(string path);

    /// <summary>Offers to remove a missing repository from the history (<c>IInvalidRepositoryRemover</c>); returns whether any was removed.</summary>
    bool RemoveInvalidRepository(string path);

    /// <summary>Moves the repository to a category of the favourites, or out of them (<see langword="null"/>).</summary>
    void AssignCategory(Repository repository, string? category);

    void RemoveRecent(string path);

    void RemoveFavourite(string path);

    /// <summary>Removes the repositories that do not exist any more from the history.</summary>
    void RemoveMissingRepositories();

    void ShowInFolder(string path);

    /// <summary>Asks for the name of a category (<c>FormDashboardCategoryTitle</c>); <see langword="null"/> if cancelled.</summary>
    string? PromptCategoryName(IReadOnlyList<string> existingCategories, string? originalName);

    /// <summary>Asks a yes/no question, "No" by default.</summary>
    bool Confirm(string question, string caption);

    /// <summary>Tells that a dropped directory is not a git repository.</summary>
    void ShowInvalidRepository(string caption);

    /// <summary>Shows the recent repositories settings; returns whether they were saved.</summary>
    bool ShowRecentReposSettings();

    void Run(DashboardLink link);

    /// <summary>Clones a fork from the git hoster (<c>StartCloneForkFromHoster</c>).</summary>
    void CloneFork(int gitHoster);
}

/// <summary>A link of the start or the contribute panel of the dashboard.</summary>
public sealed record DashboardLinkItem(string Text, string Icon, IRelayCommand Command)
{
    /// <summary>Whether the icon is lightened on a dark theme (the <c>AdaptLightness</c> of <c>Dashboard.InitDashboardLayout</c>).</summary>
    public bool AdaptsLightness { get; init; }
}

/// <summary>An item of a context menu of the dashboard, or a separator (<see cref="Execute"/> null and no children).</summary>
public sealed record DashboardMenuItem(string Header, Action? Execute, string? Icon = null, bool IsEnabled = true, IReadOnlyList<DashboardMenuItem>? Children = null)
{
    public static DashboardMenuItem Separator { get; } = new("-", null);

    public bool IsSeparator => Execute is null && Children is null;
}

/// <summary>A repository tile of the dashboard.</summary>
public sealed partial class DashboardRepositoryItem : ObservableObject
{
    internal DashboardRepositoryItem(RecentRepoInfo info, bool isFavourite)
    {
        Repository = info.Repo;
        Caption = info.Caption ?? info.Repo.Path;
        IsFavourite = isFavourite;
    }

    public Repository Repository { get; }

    public string Path => Repository.Path;

    public string Caption { get; }

    /// <summary>Whether it is shown in a category (else in the recent repositories).</summary>
    public bool IsFavourite { get; }

    /// <summary>Whether it has a category, marked with a star (as <c>listView1_DrawItem</c>).</summary>
    public bool HasCategory => !string.IsNullOrWhiteSpace(Repository.Category);

    /// <summary>The current branch, once read.</summary>
    [ObservableProperty]
    public partial string BranchName { get; internal set; } = "";

    /// <summary>Whether the repository is missing (shown with the error folder).</summary>
    [ObservableProperty]
    public partial bool IsInvalid { get; internal set; }
}

/// <summary>A group of tiles: the recent repositories, or a category of the favourites.</summary>
public sealed class DashboardGroup(string header, string? category, IReadOnlyList<DashboardRepositoryItem> items)
{
    public string Header { get; } = header;

    /// <summary>The category, <see langword="null"/> for the recent repositories.</summary>
    public string? Category { get; } = category;

    public bool IsRecent => Category is null;

    public IReadOnlyList<DashboardRepositoryItem> Items { get; } = items;
}

/// <summary>
///  Port of <c>Dashboard</c> and <c>UserRepositoriesList</c> (the start page of the main window without a repository): the
///  start and contribute links, and the recent and favourite repositories by category, with the search and the menus.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private static readonly StringComparer _groupHeaderComparer = StringComparer.CurrentCulture;

    private readonly IDashboardHost _host;
    private CancellationTokenSource? _loadingStatuses;
    private bool _hasInvalidRepositories;

    public DashboardViewModel(DashboardStrings strings, UserRepositoriesListStrings listStrings, IDashboardHost host)
    {
        Strings = strings;
        ListStrings = listStrings;
        _host = host;
        ContributeLinks =
        [
            new(strings.Develop.Text, "Develop", new RelayCommand(() => _host.Run(DashboardLink.Develop))) { AdaptsLightness = true },
            new(strings.Issues.Text, "Bug", new RelayCommand(() => _host.Run(DashboardLink.Issues))),
        ];
        StartLinks = CreateStartLinks();
    }

    public DashboardStrings Strings { get; }

    public UserRepositoriesListStrings ListStrings { get; }

    /// <summary>The links to create, open and clone a repository (<c>flpnlStart</c>).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<DashboardLinkItem> StartLinks { get; private set; }

    /// <summary>The links of the contribute panel (<c>flpnlContribute</c>).</summary>
    public IReadOnlyList<DashboardLinkItem> ContributeLinks { get; }

    public ObservableCollection<DashboardGroup> Groups { get; } = [];

    /// <summary>The search of the repositories by path (<c>textBoxSearch</c>), as typed.</summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    /// <summary>The width of the tiles; <see cref="double.NaN"/> to fit the longest caption (measured by the view).</summary>
    [ObservableProperty]
    public partial double TileWidth { get; private set; } = double.NaN;

    /// <summary>The loading of the branches and of the missing repositories of the tiles (e.g. for tests).</summary>
    public Task LoadingStatuses { get; private set; } = Task.CompletedTask;

    /// <summary>All the listed repositories, in the order of the groups.</summary>
    public IEnumerable<DashboardRepositoryItem> Repositories => Groups.SelectMany(g => g.Items);

    /// <summary>The links of the Start panel read again, e.g. with the git hosters of the plugins once they are loaded.</summary>
    public void RefreshStartLinks() => StartLinks = CreateStartLinks();

    /// <summary>As <c>Dashboard.RefreshContent</c>: the links and the repositories, read again.</summary>
    [RelayCommand]
    public void Refresh()
    {
        StartLinks = CreateStartLinks();
        ShowRecentRepositories(reload: true);
    }

    /// <summary>As <c>UserRepositoriesList.ShowRecentRepositories</c>.</summary>
    public void ShowRecentRepositories(bool reload = true)
    {
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loadingStatuses?.Cancel();
#pragma warning restore VSTHRD103
        _loadingStatuses = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadingStatuses.Token;

        DashboardRepositories repositories = _host.LoadRepositories(SearchText, reload);
        TileWidth = repositories.TileWidth;
        _hasInvalidRepositories = false;

        List<DashboardRepositoryItem> recent = [.. repositories.Recent.Select(r => new DashboardRepositoryItem(r, isFavourite: false))];
        List<DashboardRepositoryItem> favourites = [.. repositories.Favourites.Select(r => new DashboardRepositoryItem(r, isFavourite: true))];

        Groups.Clear();

        // The recent repositories first, then the categories in alphabetical order (an empty group is not shown).
        if (recent.Count > 0)
        {
            Groups.Add(new DashboardGroup(ListStrings.GroupRecentRepositories.Text, category: null, recent));
        }

        foreach (string category in repositories.Recent.Concat(repositories.Favourites)
            .Select(r => r.Repo.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(_groupHeaderComparer)
            .OrderBy(c => c)
            .Cast<string>())
        {
            List<DashboardRepositoryItem> items = [.. favourites.Where(f => _groupHeaderComparer.Equals(f.Repository.Category, category))];
            if (items.Count > 0)
            {
                Groups.Add(new DashboardGroup(category, category, items));
            }
        }

        LoadingStatuses = LoadStatusesAsync([.. recent, .. favourites], cancellationToken);
    }

    /// <summary>As <c>TryOpenRepository</c>: opens a repository, or offers to remove it if it is missing.</summary>
    [RelayCommand]
    public void Open(DashboardRepositoryItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (_host.IsValidGitWorkingDir(item.Path))
        {
            _host.OpenRepository(item.Path);
            return;
        }

        if (_host.RemoveInvalidRepository(item.Path))
        {
            ShowRecentRepositories();
        }
    }

    /// <summary>As Enter in the search box: opens the first listed repository.</summary>
    public void OpenFirst() => Open(Repositories.FirstOrDefault());

    /// <summary>As <c>OnDragDrop</c>: opens a dropped directory if it is a repository.</summary>
    public void OpenDroppedDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return;
        }

        if (!_host.IsValidGitWorkingDir(path))
        {
            _host.ShowInvalidRepository(ListStrings.CannotOpenTheFolder.Text);
            return;
        }

        _host.OpenRepository(path);
    }

    /// <summary>"Recent repositories settings" of the Dashboard menu (<c>mnuConfigure_Click</c>).</summary>
    [RelayCommand]
    public void ConfigureRecentRepositories()
    {
        if (_host.ShowRecentReposSettings())
        {
            ShowRecentRepositories();
        }
    }

    /// <summary>The context menu of a tile (<c>contextMenuStripRepository</c>, as <c>contextMenuStrip_Opening</c>).</summary>
    public IReadOnlyList<DashboardMenuItem> GetRepositoryMenu(DashboardRepositoryItem item)
    {
        UserRepositoriesListStrings s = ListStrings;
        List<DashboardMenuItem> menu =
        [
            new(s.OpenFolder.AccessKeyText, () => _host.ShowInFolder(item.Path), "BrowseFileExplorer"),
            DashboardMenuItem.Separator,
            new(s.Categories.AccessKeyText, null, Children: GetCategoriesMenu(item)),
            DashboardMenuItem.Separator,
            new(s.RemoveFromList.AccessKeyText, () => RemoveFromList(item)),
        ];

        if (_hasInvalidRepositories)
        {
            menu.Add(new(s.RemoveMissingFromList.AccessKeyText, RemoveMissingRepositories));
        }

        return menu;
    }

    /// <summary>The menu of the "Actions" link of a group (<c>contextMenuStripCategory</c>, as <c>ListView1_GroupTaskLinkClick</c>).</summary>
    public IReadOnlyList<DashboardMenuItem> GetGroupMenu(DashboardGroup group)
    {
        UserRepositoriesListStrings s = ListStrings;
        return group.IsRecent
            ? [new(s.CategoryClear.AccessKeyText, ClearRecentRepositories, "CleanupRepo")]
            : [
                new(s.CategoryRename.AccessKeyText, () => RenameCategory(group), "FileStatusModified"),
                new(s.CategoryDelete.AccessKeyText, () => DeleteCategory(group), "StarRemove"),
            ];
    }

    partial void OnSearchTextChanged(string value) => ShowRecentRepositories(reload: false);

    // As tsmiCategories_DropDownOpening.
    private IReadOnlyList<DashboardMenuItem> GetCategoriesMenu(DashboardRepositoryItem item)
    {
        UserRepositoriesListStrings s = ListStrings;
        string? current = item.Repository.Category;
        List<string> categories = GetCategories();
        List<DashboardMenuItem> menu = [];
        if (categories.Count > 0)
        {
            menu.Add(new(s.CategoryNone.AccessKeyText, () => AssignCategory(item, null), IsEnabled: !string.IsNullOrWhiteSpace(current) && s.CategoryNone.Text != current));
            menu.AddRange(categories.Select(category => new DashboardMenuItem(TranslatedText.ToAccessKeyText(category.Replace("&", "&&")), () => AssignCategory(item, category), IsEnabled: category != current)));
            menu.Add(DashboardMenuItem.Separator);
        }

        menu.Add(new(s.CategoryAdd.AccessKeyText, () => AddCategory(item), "BulletAdd", IsEnabled: s.CategoryAdd.Text != current));
        return menu;
    }

    private List<string> GetCategories()
        => [.. Repositories
            .Select(r => r.Repository.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Cast<string>()
            .OrderBy(c => c)
            .Distinct()];

    private void AssignCategory(DashboardRepositoryItem item, string? category)
    {
        _host.AssignCategory(item.Repository, category);
        ShowRecentRepositories();
    }

    private void AddCategory(DashboardRepositoryItem item)
    {
        if (_host.PromptCategoryName(GetCategories(), originalName: null) is { } category)
        {
            AssignCategory(item, category);
        }
    }

    private void RemoveFromList(DashboardRepositoryItem item)
    {
        if (item.IsFavourite)
        {
            _host.RemoveFavourite(item.Path);
        }
        else
        {
            _host.RemoveRecent(item.Path);
        }

        ShowRecentRepositories();
    }

    private void RemoveMissingRepositories()
    {
        _host.RemoveMissingRepositories();
        ShowRecentRepositories();
    }

    private void RenameCategory(DashboardGroup group)
    {
        List<string> categories = GetCategories();
        categories.Remove(group.Category!);
        if (_host.PromptCategoryName(categories, group.Category) is { } name)
        {
            UpdateCategoryName(group.Category, name);
        }
    }

    private void DeleteCategory(DashboardGroup group)
    {
        string question = string.Format(ListStrings.DeleteCategoryQuestion.Text, group.Category, group.Items.Count);
        if (_host.Confirm(question, ListStrings.DeleteCategoryCaption.Text))
        {
            UpdateCategoryName(group.Category, null);
        }
    }

    // As tsmiCategoryClear_Click: the listed repositories are removed from the recent ones.
    private void ClearRecentRepositories()
    {
        List<DashboardRepositoryItem> repositories = [.. Repositories];
        string question = string.Format(ListStrings.ClearRecentCategoryQuestion.Text, repositories.Count);
        if (!_host.Confirm(question, ListStrings.ClearRecentCategoryCaption.Text))
        {
            return;
        }

        foreach (DashboardRepositoryItem repository in repositories)
        {
            _host.RemoveRecent(repository.Path);
        }

        ShowRecentRepositories();
    }

    private void UpdateCategoryName(string? originalName, string? newName)
    {
        foreach (DashboardRepositoryItem repository in Repositories.Where(r => r.Repository.Category == originalName).ToList())
        {
            _host.AssignCategory(repository.Repository, newName);
        }

        ShowRecentRepositories();
    }

    private IReadOnlyList<DashboardLinkItem> CreateStartLinks()
        =>
        [
            new(Strings.CreateRepository.Text, "RepoCreate", new RelayCommand(() => _host.Run(DashboardLink.CreateRepository))),
            new(Strings.OpenRepository.Text, "RepoOpen", new RelayCommand(() => _host.Run(DashboardLink.OpenRepository))),
            new(Strings.CloneRepository.Text, "CloneRepoGit", new RelayCommand(() => _host.Run(DashboardLink.CloneRepository))),
            .. _host.GitHosters.Select((name, index) => new DashboardLinkItem(string.Format(Strings.CloneFork.Text, name), "CloneRepoGitHub", new RelayCommand(() => _host.CloneFork(index)))),
        ];

    // As the background part of ShowRecentRepositories: the branch of each repository, or the error folder if it is missing.
    private async Task LoadStatusesAsync(IReadOnlyList<DashboardRepositoryItem> items, CancellationToken cancellationToken)
    {
        foreach (DashboardRepositoryItem item in items)
        {
            DashboardRepositoryStatus status;
            try
            {
                status = await _host.GetStatusAsync(item.Path, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (status.IsValid)
            {
                item.BranchName = status.BranchName;
            }
            else
            {
                item.IsInvalid = true;
                _hasInvalidRepositories = true;
            }
        }
    }
}
