using System.Text;
using CommonTestUtils;
using GitCommands;
using GitCommands.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the second batch of phase 2 dialogs.</summary>
[TestFixture]
public sealed class Batch2ViewModelTests
{
    [Test]
    public void PuttyError_loading_a_key_retries_with_it()
    {
        PuttyErrorViewModel viewModel = new(new PuttyErrorStrings(), () => TestPaths.Native(@"C:\keys\id.ppk"));
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.LoadSshKeyCommand.Execute(null);

        closed.Should().BeTrue();
        viewModel.ShouldRetry.Should().BeTrue();
        viewModel.KeyPath.Should().Be(TestPaths.Native(@"C:\keys\id.ppk"));
    }

    [Test]
    public void PuttyError_cancelled_key_selection_keeps_the_dialog_open()
    {
        PuttyErrorViewModel viewModel = new(new PuttyErrorStrings(), () => null);
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.LoadSshKeyCommand.Execute(null);
        closed.Should().BeFalse();

        viewModel.CancelCommand.Execute(null);
        closed.Should().BeTrue();
        viewModel.ShouldRetry.Should().BeFalse();
        viewModel.KeyPath.Should().BeNull();
    }

    [Test]
    public void BuildServerCredentials_radio_properties_follow_the_authentication()
    {
        BuildServerCredentialsViewModel viewModel = new("https://ci.example.org");

        viewModel.Header.Should().Contain("https://ci.example.org");
        viewModel.IsGuest.Should().BeTrue();

        viewModel.IsBearerToken = true;
        viewModel.Authentication.Should().Be(BuildServerAuthentication.BearerToken);
        viewModel.IsGuest.Should().BeFalse();

        // Unchecking a radio button (which happens when another one is checked) must not change the authentication.
        viewModel.IsUsernameAndPassword = false;
        viewModel.Authentication.Should().Be(BuildServerAuthentication.BearerToken);
    }

    [Test]
    public void SelectMultipleBranches_returns_the_checked_branches_in_list_order()
    {
        SelectMultipleBranchesViewModel viewModel = new(new SelectMultipleBranchesStrings(), [(1, "main"), (2, "feature"), (3, "release")], ["release"]);

        viewModel.Branches.Select(b => b.IsChecked).Should().Equal(false, false, true);
        viewModel.Branches[0].IsChecked = true;

        viewModel.SelectedBranches.Should().Equal(1, 3);
    }

    [Test]
    public void ChooseTranslation_lists_English_first_then_languages_by_name()
    {
        IReadOnlyList<string> choices = ChooseTranslationViewModel.CreateChoices(["German", "Dutch", "English"]);

        choices.Should().Equal("English", "Dutch", "German");
    }

    [Test]
    public void ChooseTranslation_choosing_closes_with_the_language()
    {
        ChooseTranslationViewModel viewModel = new(new ChooseTranslationStrings(), ["English", "German"]);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Language = "German";
        closed.Should().BeNull();
        viewModel.ChooseCommand.Execute(null);

        closed.Should().BeTrue();
        viewModel.SelectedTranslation.Should().Be("German");
    }

    [Test]
    public void AvailableEncodings_moves_encodings_and_keeps_the_available_list_sorted()
    {
        Encoding latin1 = Encoding.Latin1;
        AvailableEncodingsViewModel viewModel = new(new AvailableEncodingsStrings(), [new UTF8Encoding(false), latin1]);

        viewModel.Available.Should().NotContain(e => e.WebName == latin1.WebName, "included encodings are not offered");
        viewModel.Available.Select(e => e.EncodingName).Should().BeInAscendingOrder(StringComparer.CurrentCulture);

        viewModel.SelectedIncluded = viewModel.Included[0];
        viewModel.ExcludeCommand.CanExecute(null).Should().BeFalse("UTF-8 is built in");

        viewModel.SelectedIncluded = latin1;
        viewModel.ExcludeCommand.Execute(null);
        viewModel.Included.Should().NotContain(latin1);
        viewModel.Available.Should().Contain(latin1);
        viewModel.Available.Select(e => e.EncodingName).Should().BeInAscendingOrder(StringComparer.CurrentCulture);

        viewModel.IncludeCommand.CanExecute(null).Should().BeFalse();
        viewModel.SelectedAvailable = latin1;
        viewModel.IncludeCommand.Execute(null);
        viewModel.Included.Should().Contain(latin1);
        viewModel.Available.Should().NotContain(latin1);
    }

    [Test]
    public void AddSubmodule_suggests_the_local_path_and_requires_both_paths()
    {
        List<string> runs = [];
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        AddSubmoduleViewModel viewModel = new(
            new AddSubmoduleStrings(), [], _ => ["main", "dev"], arguments => runs.Add(arguments.ToString()), messageBoxes, new SmallDialogViewModelTests.FakeFileDialogs());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.AddSubmoduleCommand.Execute(null);
        messageBoxes.Errors.Should().Equal(viewModel.Strings.RemoteAndLocalPathRequired.Text);
        closed.Should().BeNull();

        viewModel.Directory = "https://github.com/gitextensions/gitextensions.git";
        viewModel.LocalPath.Should().Be("gitextensions");

        viewModel.LoadRemoteBranchesCommand.Execute(null);
        viewModel.RemoteBranches.Should().Equal("main", "dev");

        viewModel.Branch = "dev";
        viewModel.Force = true;
        viewModel.AddSubmoduleCommand.Execute(null);

        runs.Should().Equal(Commands.AddSubmodule("https://github.com/gitextensions/gitextensions.git", "gitextensions", "dev", force: true).ToString());
        closed.Should().BeTrue();
    }

    [Test]
    public void CleanupRepository_builds_the_clean_commands()
    {
        List<string> runs = [];
        CleanupRepositoryViewModel viewModel = new(
            new CleanupRepositoryStrings(), TestPaths.Native(@"C:\repo\"), TestPaths.Native(@"C:\repo\.git\"), path: null,
            arguments =>
            {
                runs.Add(arguments.ToString());
                return "Would remove a.txt\n";
            },
            new ProcessViewModelTests.FakeMessageBoxes(), new SmallDialogViewModelTests.FakeFileDialogs());

        viewModel.IsRemoveIgnored = true;
        viewModel.IsIncludeFilterEnabled = true;
        viewModel.IncludePaths = "src\r\n\r\nmy docs";
        viewModel.IsExcludeFilterEnabled = true;
        viewModel.ExcludePaths = TestPaths.Native(@"bin\my file.txt");
        viewModel.CleanSubmodules = true;

        viewModel.GetIncludePathArgument().Should().Be("\"src\" \"my docs\"");
        viewModel.GetExcludePathArgument().Should().Be("--exclude=bin/my?file.txt");

        viewModel.PreviewCommand.Execute(null);

        runs.Should().Equal(
            Commands.Clean(CleanMode.OnlyIgnored, dryRun: true, directories: true, "\"src\" \"my docs\"", "--exclude=bin/my?file.txt").ToString(),
            Commands.CleanSubmodules(CleanMode.OnlyIgnored, dryRun: true, directories: true, "\"src\" \"my docs\"").ToString());
        viewModel.Output.Should().Contain("Would remove a.txt");
    }

    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public void CleanupRepository_cleans_only_when_confirmed(bool confirm, int expectedRuns)
    {
        int runs = 0;
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new() { ConfirmResult = confirm };
        CleanupRepositoryViewModel viewModel = new(
            new CleanupRepositoryStrings(), TestPaths.Native(@"C:\repo\"), TestPaths.Native(@"C:\repo\.git\"), path: null,
            _ =>
            {
                runs++;
                return "";
            },
            messageBoxes, new SmallDialogViewModelTests.FakeFileDialogs());

        viewModel.CleanupCommand.Execute(null);

        messageBoxes.Confirmations.Should().ContainSingle();
        runs.Should().Be(expectedRuns);
    }

    [Test]
    public void CleanupRepository_path_argument_sets_both_filters()
    {
        CleanupRepositoryViewModel viewModel = new(
            new CleanupRepositoryStrings(), TestPaths.Native(@"C:\repo\"), TestPaths.Native(@"C:\repo\.git\"), path: "sub", _ => "",
            new ProcessViewModelTests.FakeMessageBoxes(), new SmallDialogViewModelTests.FakeFileDialogs());

        viewModel.IsIncludeFilterEnabled.Should().BeTrue();
        viewModel.IncludePaths.Should().Be("sub");
        viewModel.IsExcludeFilterEnabled.Should().BeTrue();
        viewModel.ExcludePaths.Should().Be("sub");
        viewModel.Mode.Should().Be(CleanMode.All);
        viewModel.RemoveDirectories.Should().BeTrue();
    }

    [Test]
    public async Task CleanupRepository_adds_exclude_paths_relative_to_the_working_directory()
    {
        CleanupRepositoryViewModel viewModel = new(
            new CleanupRepositoryStrings(), TestPaths.Native(@"C:\repo\"), TestPaths.Native(@"C:\repo\.git\"), path: null, _ => "",
            new ProcessViewModelTests.FakeMessageBoxes(), new SmallDialogViewModelTests.FakeFileDialogs { Files = [TestPaths.Native(@"C:\repo\bin\a.dll")] });

        await viewModel.AddExcludePathCommand.ExecuteAsync(null);
        await viewModel.AddExcludePathCommand.ExecuteAsync(null);

        viewModel.IsExcludeFilterEnabled.Should().BeTrue();
        string excludePath = TestPaths.Native(@"bin\a.dll");
        viewModel.ExcludePaths.Should().Be($"{excludePath}{Environment.NewLine}{excludePath}");
    }

    [Test]
    public void MergeSubmodule_shows_deleted_sides_and_stages()
    {
        FakeMergeSubmoduleHost host = new() { CurrentCheckout = "abc123" };
        MergeSubmoduleViewModel viewModel = new(new MergeSubmoduleStrings(), "sub", baseCommit: null, localCommit: "1111", remoteCommit: "2222", host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Base.Should().Be("deleted");
        viewModel.Local.Should().Be("1111");
        viewModel.Current.Should().Be("abc123");
        viewModel.CanCheckoutBranch.Should().BeFalse("the base was deleted");

        host.CurrentCheckout = "def456";
        viewModel.RefreshCommand.Execute(null);
        viewModel.Current.Should().Be("def456");

        viewModel.StageCurrentCommand.Execute(null);
        host.Calls.Should().Equal("stage");
        closed.Should().BeTrue();
    }

    [TestCase(true, new[] { "checkout 1111 2222", "stage" }, true)]
    [TestCase(false, new[] { "checkout 1111 2222" }, null)]
    public void MergeSubmodule_checkout_branch_stages_when_checked_out(bool checkedOut, string[] expectedCalls, bool? expectedClosed)
    {
        FakeMergeSubmoduleHost host = new() { CheckoutResult = checkedOut };
        MergeSubmoduleViewModel viewModel = new(new MergeSubmoduleStrings(), "sub", "0000", "1111", "2222", host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.CanCheckoutBranch.Should().BeTrue();
        viewModel.CheckoutBranchCommand.Execute(null);

        host.Calls.Should().Equal(expectedCalls);
        closed.Should().Be(expectedClosed);
    }

    [Test]
    public void CreateWorktree_suggests_a_directory_per_branch()
    {
        CreateWorktreeViewModel viewModel = CreateWorktree(["main", "feature/x"], currentBranch: "main", (_, _) => true);

        viewModel.Branches.Should().Equal("feature/x");
        viewModel.IsCheckoutExistingBranch.Should().BeTrue();
        viewModel.SelectedBranch.Should().Be("feature/x");
        viewModel.WorktreeDirectory.Should().Be(Path.Join(Path.GetTempPath(), "does-not-exist", "repo") + "_feature_x");

        viewModel.IsCreateNewBranch = true;
        viewModel.NewBranchName = "fix it";
        viewModel.WorktreeDirectory.Should().EndWith("repo_fix it");
    }

    [Test]
    public void CreateWorktree_without_other_branches_creates_a_new_branch()
    {
        CreateWorktreeViewModel viewModel = CreateWorktree(["main"], currentBranch: "main", (_, _) => true);

        viewModel.CanCheckoutExistingBranch.Should().BeFalse();
        viewModel.IsCreateNewBranch.Should().BeTrue();
        viewModel.CreateCommand.CanExecute(null).Should().BeFalse("the branch name is empty");

        viewModel.NewBranchName = "main";
        viewModel.CreateCommand.CanExecute(null).Should().BeFalse("the branch exists");

        viewModel.NewBranchName = "new";
        viewModel.CreateCommand.CanExecute(null).Should().BeTrue();
    }

    [TestCase(true, true)]
    [TestCase(false, null)]
    public void CreateWorktree_creates_with_a_normalised_new_branch(bool succeeds, bool? expectedClosed)
    {
        (string Directory, string Branch)? created = null;
        CreateWorktreeViewModel viewModel = CreateWorktree(["main"], currentBranch: "main", (directory, branch) =>
        {
            created = (directory, branch);
            return succeeds;
        });
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.NewBranchName = "fix it";
        viewModel.CreateCommand.Execute(null);

        created.Should().Be((viewModel.WorktreeDirectory, "-b fix_it"));
        closed.Should().Be(expectedClosed);
    }

    [Test]
    public void CreateWorktree_rejects_a_non_empty_directory()
    {
        CreateWorktreeViewModel.IsTargetFolderValid(Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString())).Should().BeTrue();
        CreateWorktreeViewModel.IsTargetFolderValid(Path.GetDirectoryName(typeof(Batch2ViewModelTests).Assembly.Location)).Should().BeFalse();
        CreateWorktreeViewModel.IsTargetFolderValid(" ").Should().BeFalse();
    }

    [Test]
    public void OpenDirectory_suggests_directories()
    {
        string parent = Path.GetTempPath();
        string workingDir = Path.Join(parent, "repo");

        OpenDirectoryViewModel.GetDirectories(TestPaths.Native(@"C:\clones"), workingDir, [TestPaths.Native(@"C:\a\"), TestPaths.Native(@"C:\clones\")], TestPaths.Native(@"C:\recent"), TestPaths.Native(@"C:\home"))
            .Should().Equal(TestPaths.Native(@"C:\clones\"), parent.EnsureTrailingPathSeparator(), TestPaths.Native(@"C:\a\"));

        OpenDirectoryViewModel.GetDirectories(null, null, [], TestPaths.Native(@"C:\recent"), TestPaths.Native(@"C:\home"))
            .Should().Equal(TestPaths.Native(@"C:\recent\"), TestPaths.Native(@"C:\home\"));
    }

    [Test]
    public void OpenDirectory_opens_or_reports_an_invalid_repository()
    {
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        List<string> opened = [];
        OpenDirectoryViewModel viewModel = new(
            new OpenDirectoryStrings(), [TestPaths.Native(@"C:\first\"), TestPaths.Native(@"C:\second\")], "Error",
            path =>
            {
                opened.Add(path);
                return path == TestPaths.Native(@"C:\repo");
            },
            messageBoxes, new SmallDialogViewModelTests.FakeFileDialogs());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Directory.Should().Be(TestPaths.Native(@"C:\first\"));
        viewModel.OpenCommand.Execute(null);
        messageBoxes.Errors.Should().Equal(viewModel.Strings.OpenFailed.Text);
        closed.Should().BeNull();

        viewModel.Directory = $"  {TestPaths.Native(@"C:\repo")} ";
        viewModel.OpenCommand.Execute(null);
        opened.Should().Equal(TestPaths.Native(@"C:\first\"), TestPaths.Native(@"C:\repo"));
        closed.Should().BeTrue();
    }

    [Test]
    public void OpenDirectory_goes_up_to_the_parent_directory()
    {
        string directory = Path.GetDirectoryName(typeof(Batch2ViewModelTests).Assembly.Location)!;
        OpenDirectoryViewModel viewModel = new(
            new OpenDirectoryStrings(), [directory], "Error", _ => false,
            new ProcessViewModelTests.FakeMessageBoxes(), new SmallDialogViewModelTests.FakeFileDialogs());

        viewModel.GoUpCommand.CanExecute(null).Should().BeTrue();
        viewModel.GoUpCommand.Execute(null);

        viewModel.Directory.Should().Be(Path.GetDirectoryName(directory)!.EnsureTrailingPathSeparator());

        viewModel.Directory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        viewModel.GoUpCommand.CanExecute(null).Should().BeFalse("the directory does not exist");
    }

    private static CreateWorktreeViewModel CreateWorktree(IReadOnlyList<string> branches, string currentBranch, Func<string, string, bool> create)
        => new(
            new CreateWorktreeStrings(),
            branches,
            currentBranch,
            Path.Join(Path.GetTempPath(), "does-not-exist", "repo"),
            new FakeBranchNameNormaliser(),
            new GitBranchNameOptions("_"),
            autoNormalise: true,
            create,
            new SmallDialogViewModelTests.FakeFileDialogs());

    internal sealed class FakeMergeSubmoduleHost : IMergeSubmoduleHost
    {
        public string? CurrentCheckout { get; set; }

        public bool CheckoutResult { get; init; }

        public List<string> Calls { get; } = [];

        public string? GetCurrentCheckout() => CurrentCheckout;

        public void StageSubmodule() => Calls.Add("stage");

        public void OpenSubmodule() => Calls.Add("open");

        public bool CheckoutBranch(string localCommit, string remoteCommit)
        {
            Calls.Add($"checkout {localCommit} {remoteCommit}");
            return CheckoutResult;
        }
    }
}
