using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using static GitUI.AvaloniaTests.ViewModels.SettingsDialogViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SettingsPagesBatchCTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the plugin, hotkeys, scripts and checklist settings pages.</summary>
[TestFixture]
public sealed class SettingsPagesBatchCViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        FakeChecklistHost checklistHost = new();
        checklistHost.Results[ChecklistCheck.MergeTool] = new(ChecklistState.Unset, "You need to configure merge tool in order to solve merge conflicts.");
        checklistHost.Results[ChecklistCheck.GitFound] = new(ChecklistState.NotRecommended, "Git found but version 2.30 is older than recommended.");
        checklistHost.Results[ChecklistCheck.Translation] = new(ChecklistState.Set, "The configured language is English.");
        checklistHost.Results[ChecklistCheck.GcmDetected] = new(ChecklistState.Unset, "Obsolete git-credential-winstore.exe detected");
        FakeSources sources = new();
        SettingsDialogViewModel viewModel = new(new SettingsDialogStrings(), new FakeHost());
        viewModel.AddPage(new GroupSettingsPageViewModel("Gitanas", "GitExtensionsSettingsGroup"), null, "GitanasLogo16", sources.None);
        viewModel.AddPage(new ChecklistSettingsPageViewModel(new ChecklistSettingsPageStrings(), checklistHost), "GitExtensionsSettingsGroup", null, sources.GlobalOnly, asRoot: true);
        viewModel.AddPage(new ScriptsSettingsPageViewModel(new ScriptsSettingsPageStrings(), new FakeScriptsHost()), "GitExtensionsSettingsGroup", "Console", sources.GlobalOnly);
        viewModel.AddPage(new HotkeysSettingsPageViewModel(new HotkeysSettingsPageStrings(), new FakeHotkeysHost()), "GitExtensionsSettingsGroup", "Hotkey", sources.GlobalOnly);
        viewModel.AddPage(new GroupSettingsPageViewModel("Plugins", "PluginsSettingsGroup"), null, "Plugin", sources.None);
        viewModel.AddPage(CreatePluginPage(), "PluginsSettingsGroup", new byte[] { 1 }, sources.Distributed);
        SettingsWindow window = new() { DataContext = viewModel };
        window.Show();
        viewModel.Open("GitExtensionsSettingsGroup");
        Dispatcher.UIThread.RunJobs();

        window.PageContent.GetLogicalDescendants().OfType<ChecklistSettingsPageView>().Should().ContainSingle("the checklist is the root of Gitanas");
        window.PageContent.GetLogicalDescendants().OfType<Button>().Select(b => b.Content).OfType<string>().Should().Contain(["Repair", "Save and rescan"]);
        SaveScreenshot(window.CaptureRenderedFrame(), $"settings-checklist-{theme}");

        viewModel.GotoPage("ScriptsSettingsPage");
        Dispatcher.UIThread.RunJobs();
        window.PageContent.GetLogicalDescendants().OfType<ScriptsSettingsPageView>().Single().ScriptsGrid.ItemsSource.Should().NotBeNull();
        SaveScreenshot(window.CaptureRenderedFrame(), $"settings-scripts-{theme}");

        viewModel.GotoPage("HotkeysSettingsPage");
        Dispatcher.UIThread.RunJobs();
        HotkeysSettingsPageViewModel hotkeys = (HotkeysSettingsPageViewModel)viewModel.SelectedPage!;
        hotkeys.SelectedSettings = hotkeys.Settings[0];
        hotkeys.SelectedCommand = hotkeys.Commands[0];
        Dispatcher.UIThread.RunJobs();
        HotkeysSettingsPageView hotkeysView = window.PageContent.GetLogicalDescendants().OfType<HotkeysSettingsPageView>().Single();
        hotkeysView.MappingsGrid.Columns.Select(c => c.Header).Should().Equal("Command", "Key");
        SaveScreenshot(window.CaptureRenderedFrame(), $"settings-hotkeys-{theme}");

        // As TextboxHotkey: a modifier alone is ignored, the key typed with its modifiers is the hotkey.
        hotkeysView.HotkeyBox.Focus();
        window.KeyPress(OperatingSystem.IsMacOS() ? Key.LWin : Key.LeftCtrl, TestKeys.Command, OperatingSystem.IsMacOS() ? PhysicalKey.MetaLeft : PhysicalKey.ControlLeft, null);
        hotkeys.KeyData.Should().Be(116);
        window.KeyPress(Key.K, TestKeys.Command, PhysicalKey.K, "k");
        hotkeys.KeyData.Should().Be(0x20000 | 'K', "Ctrl+K as Keys (Cmd+K on macOS)");

        viewModel.GotoPage("TestPlugin");
        Dispatcher.UIThread.RunJobs();
        window.PageContent.GetLogicalDescendants().OfType<PluginSettingsPageView>().Should().ContainSingle();
        window.PageContent.GetLogicalDescendants().OfType<CheckBox>().Should().ContainSingle("the bool setting");
        window.PageContent.GetLogicalDescendants().OfType<NumericUpDown>().Should().ContainSingle("the int setting");
        window.PageContent.GetLogicalDescendants().OfType<ComboBox>().Should().ContainSingle("the choice setting");
        SaveScreenshot(window.CaptureRenderedFrame(), $"settings-plugin-{theme}");
        window.Close();
    });
}
