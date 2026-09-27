using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of phase 2, batch 8: HOME directory, updates, worktrees and submodules.</summary>
[TestFixture]
public sealed class Batch8ViewModelTests
{
    internal static readonly FixHomeEnvironment Environment = new(
        CustomHomeDir: "",
        UserProfileHomeDir: false,
        DefaultHomeDir: @"C:\Users\me",
        UserHome: @"C:\home",
        HomeDrivePath: @"H:\me",
        UserProfile: @"C:\Users\me",
        PersonalFolder: @"C:\Users\me\Documents");

    [Test]
    public void FixHome_starts_from_the_settings()
    {
        Create(Environment).IsDefaultHome.Should().BeTrue();
        FixHomeViewModel userProfile = Create(Environment with { UserProfileHomeDir = true });
        userProfile.IsUserProfileHome.Should().Be(OperatingSystem.IsWindows());
        userProfile.IsDefaultHome.Should().Be(!OperatingSystem.IsWindows());

        FixHomeViewModel other = Create(Environment with { CustomHomeDir = @"D:\home", UserProfileHomeDir = true });
        other.IsOtherHome.Should().BeTrue();
        other.IsUserProfileHome.Should().BeFalse();
        other.OtherHomeDir.Should().Be(@"D:\home");
        other.DefaultHomeText.Should().Be(@"_Use default for HOME (C:\Users\me)");
    }

    [TestCase(@"C:\home", "default", "%HOME%")]
    [TestCase(@"H:\me", "default", "%HOMEDRIVE%%HOMEPATH%")]
    [TestCase(@"C:\Users\me", "userProfile", "%USERPROFILE%")]
    [TestCase(@"C:\Users\me\Documents", "other", "personal folder")]
    public void FixHome_chooses_the_first_location_with_a_global_config(string configLocation, string expectedChoice, string expectedMessage)
    {
        FakeFixHomeHost host = new() { ConfigLocations = [configLocation] };
        FakeMessageBoxes messageBoxes = new();
        FixHomeViewModel viewModel = Create(Environment with { CustomHomeDir = @"D:\custom" }, host, messageBoxes);

        viewModel.SelectLocatedGitConfig();

        if (!OperatingSystem.IsWindows() && (configLocation == Environment.HomeDrivePath || configLocation == Environment.UserProfile))
        {
            viewModel.IsOtherHome.Should().BeTrue();
            viewModel.OtherHomeDir.Should().Be(@"D:\custom", "Windows-only locations must not override the current choice");
            messageBoxes.Informations.Should().BeEmpty();
            return;
        }

        if (!OperatingSystem.IsWindows() && configLocation == Environment.UserHome)
        {
            expectedMessage = "$HOME";
        }

        (expectedChoice switch
        {
            "default" => viewModel.IsDefaultHome,
            "userProfile" => viewModel.IsUserProfileHome,
            _ => viewModel.IsOtherHome && viewModel.OtherHomeDir == configLocation,
        }).Should().BeTrue();
        messageBoxes.Informations.Should().ContainSingle().Which.Should().Contain(expectedMessage).And.Contain(configLocation);
    }

    [Test]
    public void FixHome_keeps_the_settings_without_a_located_config()
    {
        FakeMessageBoxes messageBoxes = new();
        FixHomeViewModel viewModel = Create(Environment with { CustomHomeDir = @"D:\custom" }, new FakeFixHomeHost(), messageBoxes);

        viewModel.SelectLocatedGitConfig();

        viewModel.IsOtherHome.Should().BeTrue();
        viewModel.OtherHomeDir.Should().Be(@"D:\custom");
        messageBoxes.Informations.Should().BeEmpty();
    }

    [Test]
    public void FixHome_requires_a_directory_for_other_and_applies_the_choice()
    {
        FakeFixHomeHost host = new();
        FakeMessageBoxes messageBoxes = new();
        FixHomeViewModel viewModel = Create(Environment, host, messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.IsDefaultHome = false;
        viewModel.IsOtherHome = true;
        viewModel.OkCommand.Execute(null);
        messageBoxes.Errors.Should().Equal("Please enter a HOME directory.");
        host.Applied.Should().BeEmpty();

        viewModel.OtherHomeDir = @"D:\home";
        viewModel.OkCommand.Execute(null);
        host.Applied.Should().Equal((@"D:\home", false));
        closed.Should().BeTrue();
    }

    [Test]
    public void FixHome_reports_an_inaccessible_HOME()
    {
        FakeFixHomeHost host = new() { ResultingHome = @"Z:\missing" };
        FakeMessageBoxes messageBoxes = new();
        FixHomeViewModel viewModel = Create(Environment, host, messageBoxes);
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.IsDefaultHome = false;
        viewModel.IsUserProfileHome = true;
        viewModel.OkCommand.Execute(null);

        host.Applied.Should().Equal(("", true));
        messageBoxes.Errors.Should().ContainSingle().Which.Should().Contain(@"""Z:\missing""");
        closed.Should().BeFalse();
    }

    [Test]
    public async Task FixHome_browses_from_the_user_profile()
    {
        FakeFileDialogs fileDialogs = new() { Folder = @"E:\home" };
        FixHomeViewModel viewModel = Create(Environment, fileDialogs: fileDialogs);

        await viewModel.BrowseCommand.ExecuteAsync(null);

        fileDialogs.StartDirectories.Should().Equal(@"C:\Users\me");
        viewModel.OtherHomeDir.Should().Be(@"E:\home");
    }

    [Test]
    public void Updates_searches_then_reports_no_update()
    {
        UpdatesViewModel viewModel = CreateUpdates(isPortable: false);
        viewModel.Status.Should().Be("Searching for updates");
        viewModel.IsBusy.Should().BeTrue();

        viewModel.ReportSearchResult(null);

        viewModel.Status.Should().Be("No updates found");
        viewModel.IsBusy.Should().BeFalse();
        viewModel.IsUpdateFound.Should().BeFalse();
        viewModel.CanUpdateNow.Should().BeFalse();
    }

    [Test]
    public void Updates_offers_the_update_and_links_a_missing_runtime()
    {
        FakeUpdatesHost host = new();
        UpdatesViewModel viewModel = CreateUpdates(isPortable: false, host, installed: [new Version(8, 0, 10), new Version(10, 0, 1)]);

        viewModel.ReportSearchResult(new AvailableUpdate("6.1.0", "https://example.org/setup-arm64-6.1.msi", new Version(10, 0, 5)));

        viewModel.Status.Should().Be("There is a new version 6.1.0 of Gitanas available");
        viewModel.IsUpdateFound.Should().BeTrue();
        viewModel.IsChangeLogVisible.Should().BeTrue();
        viewModel.CanUpdateNow.Should().BeTrue();
        viewModel.RequiredRuntimeText.Should().Be(new RequiredRuntimeLink("Required: .NET 10.0 Desktop Runtime ", "10.0.5", " or later 10.x"));
        viewModel.RuntimeDownloadUrl.Should().Contain("arch=arm64").And.Contain("apphost_version=10.0.5");

        viewModel.DirectDownloadCommand.Execute(null);
        viewModel.OpenRuntimeDownloadCommand.Execute(null);
        viewModel.OpenChangeLogCommand.Execute(null);
        host.OpenedUrls.Should().Equal("https://example.org/setup-arm64-6.1.msi", viewModel.RuntimeDownloadUrl, UpdatesViewModel.ReleasesUrl);
    }

    [Test]
    public void Updates_hides_the_runtime_link_when_the_installed_runtime_suffices()
    {
        UpdatesViewModel viewModel = CreateUpdates(isPortable: true, installed: [new Version(10, 0, 7)]);

        viewModel.ReportSearchResult(new AvailableUpdate("6.1.0", "https://example.org/setup.msi", new Version(10, 0, 5)));

        viewModel.RequiredRuntimeText.Should().BeNull();
        viewModel.CanUpdateNow.Should().BeFalse("a portable installation is downloaded, not installed");
    }

    [Test]
    public void Updates_portable_direct_download_opens_the_releases()
    {
        FakeUpdatesHost host = new();
        UpdatesViewModel viewModel = CreateUpdates(isPortable: true, host);
        viewModel.ReportSearchResult(new AvailableUpdate("6.1.0", "https://example.org/setup.msi", null));

        viewModel.DirectDownloadCommand.Execute(null);

        host.OpenedUrls.Should().Equal(UpdatesViewModel.ReleasesUrl);
    }

    [Test]
    public void Updates_update_now_downloads_and_reports_a_failure()
    {
        FakeUpdatesHost host = new();
        FakeMessageBoxes messageBoxes = new();
        UpdatesViewModel viewModel = CreateUpdates(isPortable: false, host, messageBoxes: messageBoxes);
        viewModel.ReportSearchResult(new AvailableUpdate("6.1.0", "https://example.org/setup.msi", null));

        viewModel.UpdateNowCommand.Execute(null);

        host.Downloads.Should().Equal("https://example.org/setup.msi");
        viewModel.Status.Should().Be("Downloading update...");
        viewModel.IsBusy.Should().BeTrue();
        viewModel.IsChangeLogVisible.Should().BeFalse();
        viewModel.UpdateNowCommand.CanExecute(null).Should().BeFalse();

        host.ReportFailure!("timeout");
        messageBoxes.Errors.Should().Equal($"Failed to download an update.{System.Environment.NewLine}timeout");
    }

    [Test]
    public void ManageWorktree_acts_only_on_other_existing_worktrees()
    {
        FakeWorktreeHost host = new()
        {
            Worktrees =
            [
                new GitWorktree(@"C:\repo", GitWorktreeHeadType.Branch, "a1", "main", IsDeleted: false),
                new GitWorktree(@"C:\wt1", GitWorktreeHeadType.Branch, "b2", "feature", IsDeleted: false),
                new GitWorktree(@"C:\gone", GitWorktreeHeadType.Detached, "c3", null, IsDeleted: true),
            ],
            Current = @"C:\wt1",
        };
        ManageWorktreeViewModel viewModel = new(new ManageWorktreeStrings(), @"C:\wt1", host);

        viewModel.SelectedWorktree.Should().Be(host.Worktrees[0]);
        viewModel.OpenCommand.CanExecute(null).Should().BeTrue();
        viewModel.DeleteCommand.CanExecute(null).Should().BeFalse("the main worktree cannot be deleted");
        viewModel.PruneCommand.CanExecute(null).Should().BeTrue();

        viewModel.SelectedWorktree = viewModel.Worktrees[1];
        viewModel.OpenCommand.CanExecute(null).Should().BeFalse("it is the opened worktree");

        viewModel.SelectedWorktree = viewModel.Worktrees[2];
        viewModel.OpenCommand.CanExecute(null).Should().BeFalse("it is deleted");
    }

    [Test]
    public void ManageWorktree_opens_deletes_prunes_and_creates()
    {
        FakeWorktreeHost host = new()
        {
            Worktrees =
            [
                new GitWorktree(@"C:\repo", GitWorktreeHeadType.Branch, "a1", "main", IsDeleted: false),
                new GitWorktree(@"C:\wt1", GitWorktreeHeadType.Branch, "b2", "feature", IsDeleted: false),
            ],
            Current = @"C:\repo",
        };
        ManageWorktreeViewModel viewModel = new(new ManageWorktreeStrings(), @"C:\repo", host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.PruneCommand.CanExecute(null).Should().BeFalse();

        viewModel.SelectedWorktree = viewModel.Worktrees[1];
        viewModel.DeleteCommand.Execute(null);
        host.Deleted.Should().Equal(@"C:\wt1");

        viewModel.CreateCommand.Execute(null);
        host.Created.Should().Equal(@"C:\repo");
        viewModel.ShouldRefreshRevisionGrid.Should().BeTrue();

        viewModel.SelectedWorktree = viewModel.Worktrees[1];
        viewModel.OpenCommand.Execute(null);
        host.Switched.Should().Equal(@"C:\wt1");
        closed.Should().BeTrue();
    }

    [Test]
    public void Submodules_reload_keeps_the_selected_submodule()
    {
        FakeSubmodulesHost host = new() { Submodules = [Submodule("a"), Submodule("b")] };
        SubmodulesViewModel viewModel = new(new SubmodulesStrings(), host, new FakeMessageBoxes());

        viewModel.Reload();
        viewModel.Submodules.Select(s => s.Name).Should().Equal("a", "b");
        viewModel.IsLoading.Should().BeFalse();
        viewModel.SelectedSubmodule = viewModel.Submodules[1];

        host.Submodules = [Submodule("a"), Submodule("b") with { Status = "Modified" }];
        viewModel.SynchronizeCommand.Execute(null);

        host.Synchronized.Should().Equal("path/b");
        viewModel.SelectedSubmodule.Should().Be(host.Submodules[1]);
    }

    [Test]
    public void Submodules_update_all_without_a_selection_and_remove_after_confirmation()
    {
        FakeSubmodulesHost host = new() { Submodules = [Submodule("a")] };
        FakeMessageBoxes messageBoxes = new() { ConfirmResult = false };
        SubmodulesViewModel viewModel = new(new SubmodulesStrings(), host, messageBoxes);
        viewModel.Reload();

        viewModel.UpdateCommand.Execute(null);
        host.Updated.Should().Equal("");
        viewModel.RemoveCommand.CanExecute(null).Should().BeFalse();

        viewModel.SelectedSubmodule = viewModel.Submodules[0];
        viewModel.RemoveCommand.Execute(null);
        messageBoxes.Confirmations.Should().Equal("Are you sure you want remove the selected submodule?");
        host.Removed.Should().BeEmpty();

        messageBoxes.ConfirmResult = true;
        viewModel.RemoveCommand.Execute(null);
        host.Removed.Should().Equal(("a", "path/a"));
    }

    private static SubmoduleItem Submodule(string name) => new(name, "Up to date", $"https://example.org/{name}.git", $"path/{name}", "0123456789", "main");

    private static FixHomeViewModel Create(FixHomeEnvironment environment, FakeFixHomeHost? host = null, FakeMessageBoxes? messageBoxes = null, FakeFileDialogs? fileDialogs = null)
        => new(new FixHomeStrings(), environment, "Error", host ?? new FakeFixHomeHost(), messageBoxes ?? new FakeMessageBoxes(), fileDialogs ?? new FakeFileDialogs());

    private static UpdatesViewModel CreateUpdates(bool isPortable, FakeUpdatesHost? host = null, IReadOnlyList<Version>? installed = null, FakeMessageBoxes? messageBoxes = null)
        => new(new UpdatesStrings(), isPortable, "arm64", installed ?? [], host ?? new FakeUpdatesHost(), messageBoxes ?? new FakeMessageBoxes());

    private sealed class FakeFixHomeHost : IFixHomeHost
    {
        public IReadOnlyList<string> ConfigLocations { get; init; } = [];

        public string ResultingHome { get; init; } = Path.GetTempPath();

        public List<(string CustomHomeDir, bool UserProfileHomeDir)> Applied { get; } = [];

        public bool HasGlobalGitConfig(string? path) => path is not null && ConfigLocations.Contains(path);

        public string? ApplyHome(string customHomeDir, bool userProfileHomeDir)
        {
            Applied.Add((customHomeDir, userProfileHomeDir));
            return customHomeDir.Length > 0 ? customHomeDir : ResultingHome;
        }

        public bool DirectoryExists(string? path) => path is not null && !path.StartsWith("Z:", StringComparison.Ordinal);
    }

    private sealed class FakeUpdatesHost : IUpdatesHost
    {
        public List<string> OpenedUrls { get; } = [];

        public List<string> Downloads { get; } = [];

        public Action<string>? ReportFailure { get; private set; }

        public void OpenUrl(string url) => OpenedUrls.Add(url);

        public void DownloadAndInstall(string updateUrl, Action<string> reportDownloadFailure)
        {
            Downloads.Add(updateUrl);
            ReportFailure = reportDownloadFailure;
        }
    }

    private sealed class FakeWorktreeHost : IManageWorktreeHost
    {
        public List<GitWorktree> Worktrees { get; init; } = [];

        public string Current { get; init; } = "";

        public List<string> Deleted { get; } = [];

        public List<string> Switched { get; } = [];

        public List<string> Created { get; } = [];

        public IReadOnlyList<GitWorktree> LoadWorktrees() => [.. Worktrees];

        public bool IsCurrentWorktree(string path) => path == Current;

        public void Prune() => Worktrees.RemoveAll(w => w.IsDeleted);

        public bool Delete(string path)
        {
            Deleted.Add(path);
            return true;
        }

        public bool Switch(string path)
        {
            Switched.Add(path);
            return true;
        }

        public bool Create(string mainWorktreePath)
        {
            Created.Add(mainWorktreePath);
            return true;
        }
    }

    private sealed class FakeSubmodulesHost : ISubmodulesHost
    {
        public IReadOnlyList<SubmoduleItem> Submodules { get; set; } = [];

        public List<string> Synchronized { get; } = [];

        public List<string> Updated { get; } = [];

        public List<(string Name, string LocalPath)> Removed { get; } = [];

        public void LoadSubmodules(Action<SubmoduleItem> report, Action completed)
        {
            foreach (SubmoduleItem submodule in Submodules)
            {
                report(submodule);
            }

            completed();
        }

        public void Add()
        {
        }

        public void Synchronize(string localPath) => Synchronized.Add(localPath);

        public void Update(string localPath) => Updated.Add(localPath);

        public void Remove(string name, string localPath) => Removed.Add((name, localPath));

        public void Pull(string localPath)
        {
        }
    }
}
