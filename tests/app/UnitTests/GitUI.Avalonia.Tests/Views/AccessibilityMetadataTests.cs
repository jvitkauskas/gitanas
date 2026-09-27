using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using GitCommands.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

[TestFixture]
public sealed class AccessibilityMetadataTests : HeadlessTest
{
    [Test]
    public Task Names_and_help_follow_translated_text_and_tooltips() => OnUiThreadAsync(() =>
    {
        AccessText text = new() { Text = "_Commit" };
        Button captioned = new() { Content = new StackPanel { Children = { new Image(), text } } };
        Button iconOnly = new() { Name = "fetch", Content = new Image() };
        ToolTip.SetTip(iconOnly, "Fetch changes");
        TextBox input = new() { Name = "branchName" };
        StackPanel panel = new() { Children = { captioned, iconOnly, input } };
        AccessibleNames.Apply(panel);
        AccessibleNames.Apply(panel); // Opening the same view must not replace existing bindings.

        AutomationProperties.GetName(captioned).Should().Be("Commit");
        AutomationProperties.GetName(iconOnly).Should().Be("Fetch changes");
        AutomationProperties.GetAutomationId(input).Should().Be("branchName");
        text.Text = "_Commit && push";
        ToolTip.SetTip(iconOnly, "Änderungen abrufen");
        AutomationProperties.GetName(captioned).Should().Be("Commit && push");
        AutomationProperties.GetName(iconOnly).Should().Be("Änderungen abrufen");
        AutomationProperties.GetHelpText(iconOnly).Should().Be("Änderungen abrufen");
    });

    [Test]
    public Task Explicit_accessibility_metadata_is_preserved() => OnUiThreadAsync(() =>
    {
        Button button = new() { Name = "internalName", Content = new Image() };
        ToolTip.SetTip(button, "Default tip");
        AutomationProperties.SetName(button, "Explicit name");
        AutomationProperties.SetAutomationId(button, "public-id");
        AutomationProperties.SetHelpText(button, "Explicit help");
        AccessibleNames.Apply(button);
        AutomationProperties.GetName(button).Should().Be("Explicit name");
        AutomationProperties.GetAutomationId(button).Should().Be("public-id");
        AutomationProperties.GetHelpText(button).Should().Be("Explicit help");
    });

    [Test]
    public Task Rename_dialog_supports_forward_and_reverse_keyboard_navigation() => OnUiThreadAsync(() =>
    {
        RenameBranchWindow window = new()
        {
            DataContext = new RenameBranchViewModel(new RenameBranchStrings(), "old", new FakeBranchNameNormaliser(),
                new GitBranchNameOptions("_"), true, (_, _) => true),
        };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            TextBox input = window.FindControl<TextBox>("branchNameTextBox")!;
            Button rename = window.FindControl<Button>("renameButton")!;
            input.Text = "new";
            input.Focus();
            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            rename.IsFocused.Should().BeTrue();
            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Shift);
            Dispatcher.UIThread.RunJobs();
            input.IsFocused.Should().BeTrue();
            AutomationProperties.GetAutomationId(input).Should().Be("branchNameTextBox");
        }
        finally
        {
            window.Close();
        }
    });
}
