using CommonTestUtils;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.HelperDialogs;
using GitUIPluginInterfaces.BuildServerIntegration;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The second batch of phase 2 dialogs, shown from their WinForms entry points with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartCleanupRepositoryDialog_previews_the_untracked_files()
    {
        File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "untracked.txt"), "content");
        string? output = null;

        DriveDialogs(
            window =>
            {
                CleanupRepositoryViewModel viewModel = (CleanupRepositoryViewModel)window.DataContext!;
                viewModel.PreviewCommand.Execute(null);
                output = viewModel.Output;
                Capture(window, "cleanup-repository");
                window.Close();
            },
            AcknowledgeWhenDone);

        _commands.StartCleanupRepositoryDialog(_owner).Should().BeTrue();

        output.Should().Contain("untracked.txt");
        File.Exists(Path.Combine(_referenceRepository.Module.WorkingDir, "untracked.txt")).Should().BeTrue("a preview does not remove files");
    }

    [Test]
    public void Create_worktree_dialog_creates_a_worktree_with_a_new_branch()
    {
        string mainWorktree = _referenceRepository.Module.WorkingDir.TrimEnd(Path.DirectorySeparatorChar);
        string directory = Path.Combine(Path.GetTempPath(), $"ge-avalonia-worktree-{Guid.NewGuid():N}");
        string? processResult = null;
        try
        {
            DriveDialogs(
                window =>
                {
                    CreateWorktreeViewModel viewModel = (CreateWorktreeViewModel)window.DataContext!;
                    viewModel.IsCreateNewBranch = true;
                    viewModel.NewBranchName = "worktree-branch";
                    viewModel.WorktreeDirectory = directory;
                    Capture(window, "create-worktree");
                    viewModel.CreateCommand.Execute(null);

                    // Stays open if git failed.
                    window.Close();
                },
                window =>
                {
                    ProcessViewModel viewModel = (ProcessViewModel)window.DataContext!;
                    WhenDone(viewModel, () =>
                    {
                        processResult = $"{viewModel.Status} {viewModel.Title}";
                        Capture(window, "create-worktree-process");
                        viewModel.AcknowledgeCommand.Execute(null);
                    });
                });

            AvaloniaDialogs.TryShowCreateWorktree(_owner, _commands, mainWorktree, out string? worktreeDirectory).Should().BeTrue();

            processResult.Should().StartWith(nameof(ProcessStatus.Succeeded));
            worktreeDirectory.Should().Be(directory);
            File.Exists(Path.Combine(directory, ".git")).Should().BeTrue();
            _referenceRepository.Module.GetRefs(RefsFilter.Heads).Select(r => r.Name).Should().Contain("worktree-branch");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }

            _referenceRepository.Module.GitExecutable.GetOutput("worktree prune");
        }
    }

    [Test]
    public void OpenModule_opens_the_chosen_repository()
    {
        string workingDir = _referenceRepository.Module.WorkingDir;

        DriveNextDialog(window =>
        {
            OpenDirectoryViewModel viewModel = (OpenDirectoryViewModel)window.DataContext!;
            viewModel.Directory = workingDir;
            Capture(window, "open-directory");
            viewModel.OpenCommand.Execute(null);
        });

        AvaloniaDialogs.TryShowOpenDirectory(
            _owner, _commands.GetRequiredService<IGitExecutorProvider>(), _referenceRepository.Module, out IGitModule? module).Should().BeTrue();

        module!.WorkingDir.Should().Be(workingDir);
    }

    [Test]
    public void AskForKey_returns_retry()
    {
        DriveNextDialog(window =>
        {
            Capture(window, "putty-error");
            ((PuttyErrorViewModel)window.DataContext!).RetryCommand.Execute(null);
        });

        AvaloniaDialogs.TryShowPuttyError(_owner, out bool retry, out string? keyPath);
        retry.Should().BeTrue();
        keyPath.Should().BeNull();
    }

    [Test]
    public void Build_server_credentials_are_updated_when_accepted()
    {
        BuildServerCredentials credentials = new() { BuildServerCredentialsType = BuildServerCredentialsType.Guest };

        DriveNextDialog(window =>
        {
            BuildServerCredentialsViewModel viewModel = (BuildServerCredentialsViewModel)window.DataContext!;
            viewModel.IsBearerToken = true;
            viewModel.BearerToken = "token";
            viewModel.OkCommand.Execute(null);
        });

        AvaloniaDialogs.TryShowBuildServerCredentials(_owner, "https://ci.example.org", credentials, out bool accepted).Should().BeTrue();

        accepted.Should().BeTrue();
        credentials.BuildServerCredentialsType.Should().Be(BuildServerCredentialsType.BearerToken);
        credentials.BearerToken.Should().Be("token");
    }

    [Test]
    public void Settings_and_helper_dialogs_open_from_their_entry_points()
    {
        _referenceRepository.CreateBranch("feature", _referenceRepository.CommitHash!);
        IReadOnlyList<IGitRef> branches = _referenceRepository.Module.GetRefs(RefsFilter.Heads);

        DriveNextDialog(window =>
        {
            SelectMultipleBranchesViewModel viewModel = (SelectMultipleBranchesViewModel)window.DataContext!;
            viewModel.Branches.Single(b => b.Text == "feature").IsChecked = true;
            viewModel.OkCommand.Execute(null);
        });
        AvaloniaDialogs.TrySelectMultipleBranches(_owner, branches, [], out IReadOnlyList<IGitRef> selected).Should().BeTrue();
        selected.Select(b => b.Name).Should().Equal("feature");

        DriveNextDialog(window =>
        {
            AvailableEncodingsViewModel viewModel = (AvailableEncodingsViewModel)window.DataContext!;
            viewModel.Available.Should().NotBeEmpty();
            Capture(window, "available-encodings");
            viewModel.CancelCommand.Execute(null);
        });
        AvaloniaDialogs.TryShowAvailableEncodings(_owner, out bool accepted).Should().BeTrue();
        accepted.Should().BeFalse();

        string translation = AppSettings.Translation;
        try
        {
            DriveNextDialog(window =>
            {
                ChooseTranslationViewModel viewModel = (ChooseTranslationViewModel)window.DataContext!;
                viewModel.Translations[0].Should().Be("English");
                Capture(window, "choose-translation");
                viewModel.Language = viewModel.Translations[0];
                viewModel.ChooseCommand.Execute(null);
            });
            AvaloniaStartupDialogs.TryShowChooseTranslation().Should().BeTrue();
            AppSettings.Translation.Should().Be("English");
        }
        finally
        {
            AppSettings.Translation = translation;
        }

        using ReferenceRepository submoduleSource = new();
        IReadOnlyList<string>? remoteBranches = null;
        DriveNextDialog(window =>
        {
            AddSubmoduleViewModel viewModel = (AddSubmoduleViewModel)window.DataContext!;
            viewModel.Directory = submoduleSource.Module.WorkingDir;
            viewModel.LoadRemoteBranchesCommand.Execute(null);
            remoteBranches = viewModel.RemoteBranches;
            Capture(window, "add-submodule");
            window.Close();
        });
        AvaloniaDialogs.TryShowAddSubmodule(_owner, _commands).Should().BeTrue();
        remoteBranches.Should().Contain("master");
    }
}
