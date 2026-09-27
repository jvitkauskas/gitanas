using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Avalonia.ScriptsEngine;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.ScriptsEngine;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the first batch of small dialogs (phase 2).</summary>
[TestFixture]
public sealed class SmallDialogViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(new CommandlineHelpWindow { DataContext = new CommandlineHelpViewModel(new CommandlineHelpStrings(), "[path]\nbrowse [path]\ncommit [--quiet]\npush [--quiet]") }, $"commandline-help-{theme}");
        Capture(new AddFilesWindow { DataContext = new AddFilesViewModel(new AddFilesStrings(), "*.cs", _ => true) }, $"add-files-{theme}");
        Capture(new ContributorsWindow { DataContext = new ContributorsViewModel("Alice, Bob", "Carol, Dan", "Eve", "Frank") }, $"contributors-{theme}");
        Capture(new ResetChangesWindow { DataContext = new ResetChangesViewModel(new ResetChangesStrings(), true, true, null) }, $"reset-changes-{theme}");
        Capture(
            new DeleteTagWindow { DataContext = new DeleteTagViewModel(new DeleteTagStrings(), ["v1.0", "v2.0"], "v2.0", ["origin"], "origin", () => "https://manual", new NullDeleteTagHost()) },
            $"delete-tag-{theme}");
        Capture(
            new InitWindow
            {
                DataContext = new InitViewModel(
                    new InitStrings(), ["C:\\repos\\a"], "C:\\repos\\new", "Error", new NullInitHost(), new ProcessViewModelTests.FakeMessageBoxes(), new SmallDialogViewModelTests.FakeFileDialogs()),
            },
            $"init-{theme}");
        Capture(new GoToLineWindow { DataContext = new GoToLineViewModel(new GoToLineStrings(), 250) }, $"go-to-line-{theme}");
        Capture(new SimplePromptWindow { DataContext = new SimplePromptViewModel("Script", "Ticket", "ABC-1") }, $"simple-prompt-{theme}");
        Capture(new FilePromptWindow { DataContext = new FilePromptViewModel(new FilePromptStrings(), new SmallDialogViewModelTests.FakeFileDialogs()) }, $"file-prompt-{theme}");
    });

    [Test]
    public Task ResetChanges_Enter_cancels() => OnUiThreadAsync(() =>
    {
        ResetChangesViewModel viewModel = new(new ResetChangesStrings(), true, true, null);
        ResetChangesWindow window = Show(new ResetChangesWindow { DataContext = viewModel });

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        viewModel.SelectedAction.Should().Be(ResetChangesAction.Cancel);
        window.DialogResult.Should().BeFalse();
    });

    [Test]
    public Task GoToLine_typed_number_is_bounded() => OnUiThreadAsync(() =>
    {
        GoToLineViewModel viewModel = new(new GoToLineStrings(), 50);
        GoToLineWindow window = Show(new GoToLineWindow { DataContext = viewModel });
        NumericUpDown upDown = window.FindControl<NumericUpDown>("lineNumberUpDown")!;

        upDown.Value = 42;
        Dispatcher.UIThread.RunJobs();
        viewModel.LineNumber.Should().Be(42);

        upDown.Maximum.Should().Be(50);
        upDown.Value = 500;
        Dispatcher.UIThread.RunJobs();
        viewModel.LineNumber.Should().Be(50, "the maximum is the number of lines");
        upDown.Value.Should().Be(50);
    });

    [Test]
    public Task SimplePrompt_Escape_clears_the_selection_before_cancelling() => OnUiThreadAsync(() =>
    {
        SimplePromptViewModel viewModel = new("Title", "Label", "some text");
        SimplePromptWindow window = Show(new SimplePromptWindow { DataContext = viewModel });
        TextBox input = window.FindControl<TextBox>("inputTextBox")!;
        bool closed = false;
        window.Closed += (_, _) => closed = true;
        input.Focus();
        input.SelectAll();

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        closed.Should().BeFalse();
        input.SelectionStart.Should().Be(input.SelectionEnd);

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        closed.Should().BeTrue();
    });

    [Test]
    public Task Init_radio_buttons_select_the_repository_type() => OnUiThreadAsync(() =>
    {
        InitViewModel viewModel = new(
            new InitStrings(), [], "C:\\repos\\new", "Error", new NullInitHost(), new ProcessViewModelTests.FakeMessageBoxes(), new SmallDialogViewModelTests.FakeFileDialogs());
        InitWindow window = Show(new InitWindow { DataContext = viewModel });

        window.FindControl<RadioButton>("personalRadioButton")!.IsChecked.Should().BeTrue();
        window.FindControl<RadioButton>("centralRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        viewModel.IsCentral.Should().BeTrue();

        window.FindControl<RadioButton>("personalRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        viewModel.IsCentral.Should().BeFalse();
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

    private sealed class NullDeleteTagHost : IDeleteTagHost
    {
        public void DeleteLocalTag(string tagName)
        {
        }

        public void DeleteRemoteTag(string remote, string tagName)
        {
        }

        public void OpenUrl(string url)
        {
        }
    }

    private sealed class NullInitHost : IInitRepositoryHost
    {
        public bool FileExists(string path) => false;

        public string Init(string directory, bool central) => "";

        public void OnRepositoryCreated(string directory)
        {
        }
    }
}
