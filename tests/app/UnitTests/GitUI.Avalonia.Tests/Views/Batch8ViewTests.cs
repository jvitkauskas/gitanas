using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of phase 2, batch 8: HOME directory, worktrees, submodules and the help text window.</summary>
[TestFixture]
public sealed class Batch8ViewTests : HeadlessTest
{
    private static readonly GitWorktree[] _worktrees =
    [
        new(@"C:\repo", GitWorktreeHeadType.Branch, "0123456789abcdef0123456789abcdef01234567", "main", IsDeleted: false),
        new(@"C:\worktrees\feature", GitWorktreeHeadType.Branch, "89abcdef0123456789abcdef0123456789abcdef", "feature", IsDeleted: false),
        new(@"C:\worktrees\gone", GitWorktreeHeadType.Detached, "fedcba9876543210fedcba9876543210fedcba98", null, IsDeleted: true),
    ];

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(
            new FixHomeWindow
            {
                DataContext = new FixHomeViewModel(new FixHomeStrings(), Batch8ViewModelTests.Environment, "Error", new NoConfigHost(), new FakeMessageBoxes(), new FakeFileDialogs()),
            },
            $"fix-home-{theme}");

        Capture(new ManageWorktreeWindow { DataContext = new ManageWorktreeViewModel(new ManageWorktreeStrings(), @"C:\repo", new WorktreeHost()) }, $"manage-worktree-{theme}");

        SubmodulesViewModel submodules = new(new SubmodulesStrings(), new SubmodulesHost(), new FakeMessageBoxes());
        SubmodulesWindow submodulesWindow = Show(new SubmodulesWindow { DataContext = submodules });
        submodules.SelectedSubmodule = submodules.Submodules[0];
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(submodulesWindow.CaptureRenderedFrame(), $"submodules-{theme}");
        submodulesWindow.Close();

        Capture(
            new SimpleHelpDisplayWindow { DataContext = new SimpleHelpDisplayViewModel("Arguments help", "{sHashes}\n{sTag}\n{sBranch}\n{sLocalBranch}\n{sRemoteBranch}") },
            $"simple-help-display-{theme}");
    });

    [Test]
    public Task ManageWorktree_strikes_out_deleted_worktrees() => OnUiThreadAsync(() =>
    {
        ManageWorktreeWindow window = Show(new ManageWorktreeWindow { DataContext = new ManageWorktreeViewModel(new ManageWorktreeStrings(), @"C:\repo", new WorktreeHost()) });

        DataGridRow[] rows = [.. window.GetVisualDescendants().OfType<DataGridRow>()];
        rows.Should().HaveCount(3);
        rows.Select(row => row.Classes.Contains("deleted")).Should().Equal(false, false, true);
        rows[2].GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == @"C:\worktrees\gone").TextDecorations.Should().BeEquivalentTo(TextDecorations.Strikethrough);

        DataGrid grid = window.FindControl<DataGrid>("worktreesGrid")!;
        grid.Columns.Select(c => c.Header).Should().Equal("Path", "Type", "Branch", "SHA-1");
        SaveScreenshot(window.CaptureRenderedFrame(), "manage-worktree-deleted");
        window.Close();
    });

    [Test]
    public Task Submodules_details_show_the_selected_submodule() => OnUiThreadAsync(() =>
    {
        SubmodulesViewModel viewModel = new(new SubmodulesStrings(), new SubmodulesHost(), new FakeMessageBoxes());
        SubmodulesWindow window = Show(new SubmodulesWindow { DataContext = viewModel });
        viewModel.Submodules.Should().HaveCount(2, "the list loads once the window is shown");

        viewModel.SelectedSubmodule = viewModel.Submodules[1];
        Dispatcher.UIThread.RunJobs();

        window.FindControl<TextBox>("nameTextBox")!.Text.Should().Be("themes");
        window.FindControl<TextBox>("localPathTextBox")!.Text.Should().Be("externals/themes");
        window.FindControl<TextBox>("commitTextBox")!.IsReadOnly.Should().BeTrue();
        window.FindControl<Button>("removeButton")!.IsEffectivelyEnabled.Should().BeTrue();
        window.Close();
    });

    private static T Show<T>(T window)
        where T : DialogWindow
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Capture(DialogWindow window, string name)
    {
        Show(window);
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }

    private sealed class NoConfigHost : IFixHomeHost
    {
        public bool HasGlobalGitConfig(string? path) => false;

        public string? ApplyHome(string customHomeDir, bool userProfileHomeDir) => null;

        public bool DirectoryExists(string? path) => false;
    }

    private sealed class WorktreeHost : IManageWorktreeHost
    {
        public IReadOnlyList<GitWorktree> LoadWorktrees() => _worktrees;

        public bool IsCurrentWorktree(string path) => path == @"C:\repo";

        public void Prune()
        {
        }

        public bool Delete(string path) => false;

        public bool Switch(string path) => false;

        public bool Create(string mainWorktreePath) => false;
    }

    private sealed class SubmodulesHost : ISubmodulesHost
    {
        public void LoadSubmodules(Action<SubmoduleItem> report, Action completed)
        {
            report(new SubmoduleItem("conemu", "Up to date", "https://github.com/gitextensions/conemu.git", "externals/conemu", "0123456789abcdef0123456789abcdef01234567", "master"));
            report(new SubmoduleItem("themes", "Modified", "https://github.com/gitextensions/themes.git", "externals/themes", "89abcdef0123456789abcdef0123456789abcdef", "main"));
            completed();
        }

        public void Add()
        {
        }

        public void Synchronize(string localPath)
        {
        }

        public void Update(string localPath)
        {
        }

        public void Remove(string name, string localPath)
        {
        }

        public void Pull(string localPath)
        {
        }
    }
}
