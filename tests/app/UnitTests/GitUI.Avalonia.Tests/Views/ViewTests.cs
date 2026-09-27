using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitCommands.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
///  Runs the real XAML of the ported dialogs in Avalonia's headless platform: bindings, keyboard and rendering.
///  Screenshots are written to <c>&lt;test work dir&gt;/screenshots</c>.
/// </summary>
[TestFixture]
public sealed class ViewTests : HeadlessTest
{
    [Test]
    public Task CommitTemplateSettings_binds_the_selected_template() => OnUiThreadAsync(() =>
    {
        CommitTemplateSettingsViewModel viewModel = CreateCommitTemplateSettingsViewModel();
        CommitTemplateSettingsWindow window = Show(new CommitTemplateSettingsWindow { DataContext = viewModel });

        window.FindControl<ComboBox>("comboBoxCommitTemplates")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();

        window.FindControl<TextBox>("textBoxCommitTemplateName")!.Text.Should().Be("Ticket");
        window.FindControl<CheckBox>("checkBoxRegexEnabled")!.IsChecked.Should().BeTrue();

        TextBox nameBox = window.FindControl<TextBox>("textBoxCommitTemplateName")!;
        nameBox.Focus();
        nameBox.CaretIndex = nameBox.Text!.Length;
        window.KeyTextInput("!");
        viewModel.Templates[1].DisplayName.Should().Be("2 : Ticket!");
    });

    [Test]
    public Task NumericUpDown_is_two_way_bound_to_int() => OnUiThreadAsync(() =>
    {
        CommitTemplateSettingsViewModel viewModel = CreateCommitTemplateSettingsViewModel();
        CommitTemplateSettingsWindow window = Show(new CommitTemplateSettingsWindow { DataContext = viewModel });
        NumericUpDown numeric = window.FindControl<NumericUpDown>("numericMaxFirstLineLength")!;

        numeric.Value.Should().Be(72);
        numeric.Value = 50;
        Dispatcher.UIThread.RunJobs();

        viewModel.MaxFirstLineLength.Should().Be(50);
    });

    [Test]
    public Task Escape_closes_a_dialog_as_cancelled() => OnUiThreadAsync(() =>
    {
        CommitTemplateSettingsWindow window = Show(new CommitTemplateSettingsWindow { DataContext = CreateCommitTemplateSettingsViewModel() });
        bool closed = false;
        window.Closed += (_, _) => closed = true;
        window.FindControl<TextBox>("textBoxCommitTemplateName")!.Focus();

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        closed.Should().BeTrue();
        window.DialogResult.Should().BeFalse();
    });

    [Test]
    public Task Enter_in_the_branch_name_renames() => OnUiThreadAsync(() =>
    {
        List<string> renamedTo = [];
        RenameBranchViewModel viewModel = new(
            new RenameBranchStrings(), "feature/old", new FakeBranchNameNormaliser(), new GitBranchNameOptions("_"), autoNormalise: true,
            (_, newName) =>
            {
                renamedTo.Add(newName);
                return true;
            });
        RenameBranchWindow window = Show(new RenameBranchWindow { DataContext = viewModel });

        TextBox name = window.FindControl<TextBox>("branchNameTextBox")!;
        name.Focus();
        name.SelectAll();
        window.KeyTextInput("feature/new");
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        renamedTo.Should().Equal("feature/new");
        window.DialogResult.Should().BeTrue();
    });

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        CommitTemplateSettingsViewModel commitSettings = CreateCommitTemplateSettingsViewModel();
        commitSettings.SelectedTemplate = commitSettings.Templates[1];
        CommitTemplateSettingsWindow commitWindow = Show(new CommitTemplateSettingsWindow { DataContext = commitSettings });
        SaveScreenshot(commitWindow.CaptureRenderedFrame(), $"commit-template-settings-{theme}");
        commitWindow.FindControl<TabControl>("tabControl")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(commitWindow.CaptureRenderedFrame(), $"commit-template-settings-validation-{theme}");

        RenameBranchWindow renameWindow = Show(new RenameBranchWindow
        {
            DataContext = new RenameBranchViewModel(new RenameBranchStrings(), "feature/ABC-123-login", new FakeBranchNameNormaliser(), new GitBranchNameOptions("_"), true, (_, _) => true),
        });
        SaveScreenshot(renameWindow.CaptureRenderedFrame(), $"rename-branch-{theme}");

        AboutWindow aboutWindow = Show(new AboutWindow
        {
            DataContext = new AboutViewModel(
                new AboutStrings(), "Gitanas", "Gitanas 33.33.33\nBuild 0123456789\nGit 2.55.0", ["Alice"], "https://example.org", new NullAboutHost()),
        });
        SaveScreenshot(aboutWindow.CaptureRenderedFrame(), $"about-{theme}");
    });

    [Test]
    public Task CaptionFirst_check_box_draws_a_check_mark() => OnUiThreadAsync(() =>
    {
        StackPanel panel = new() { Margin = new(8), Spacing = 8 };
        panel.Children.Add(new CheckBox { Content = "Normal", IsChecked = true });
        panel.Children.Add(new CheckBox { Content = "Caption first", IsChecked = true, Classes = { "captionFirst" } });
        Window window = Show(new Window { Width = 240, Height = 110, Content = panel });

        SaveScreenshot(window.CaptureRenderedFrame(), "check-box-caption-first");
    });

    private static T Show<T>(T window)
        where T : Window
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static CommitTemplateSettingsViewModel CreateCommitTemplateSettingsViewModel()
        => new(
            new CommitTemplateSettingsStrings(),
            new CommitTemplateSettingsViewModelTests.InMemoryStore(new CommitMessageSettings
            {
                MaxFirstLineLength = 72,
                Templates = [new CommitTemplate("Bug fix", "fix: ", false), new CommitTemplate("Ticket", "{{([A-Z]+-\\d+)}}: ", true)],
            }));

    private sealed class NullAboutHost : IAboutDialogHost
    {
        public void OpenUrl(string url)
        {
        }

        public void ShowContributors()
        {
        }

        public void CopyEnvironmentInfo()
        {
        }
    }
}
