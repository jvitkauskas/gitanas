using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using static GitUI.AvaloniaTests.ViewModels.SettingsDialogViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>
///  View model tests of the settings pages of the plugins (<c>PluginSettingsPage</c>), the hotkeys, the scripts and the
///  checklist.
/// </summary>
[TestFixture]
public sealed class SettingsPagesBatchCTests
{
    [Test]
    public void A_plugin_page_edits_the_settings_of_the_plugin_at_the_chosen_level()
    {
        FakeSources sources = new();
        sources.Local.SetValue("plugin.Enabled", "false");
        sources.Effective.SetValue("plugin.Enabled", "false");
        sources.Global.SetValue("plugin.Count", "3");
        sources.Effective.SetValue("plugin.Count", "3");
        SettingsDialogViewModel dialog = new(new SettingsDialogStrings(), new FakeHost());
        PluginSettingsPageViewModel page = CreatePluginPage();
        dialog.AddPage(page, null, new byte[] { 1 }, sources.Distributed);
        dialog.Open("TestPlugin");

        dialog.SelectedPage.Should().BeSameAs(page, "the page is found by the name of the plugin type");
        dialog.Nodes.Single().Icon.Should().BeEquivalentTo(new byte[] { 1 }, "the image of the plugin");
        page.Title.Should().Be("Test plugin");
        page.HasNoSettings.Should().BeFalse();
        page.SearchKeywords.Should().Contain(["Enabled", "Count", "A note"]);
        BoolSettingValue enabled = (BoolSettingValue)page.Rows[0].Value!;
        IntSettingValue count = (IntSettingValue)page.Rows[1].Value!;
        StringSettingValue name = (StringSettingValue)page.Rows[2].Value!;
        ChoiceSettingValue choice = (ChoiceSettingValue)page.Rows[3].Value!;
        PasswordSettingValue password = (PasswordSettingValue)page.Rows[4].Value!;
        NumberTextSettingValue ratio = (NumberTextSettingValue)page.Rows[5].Value!;
        enabled.Value.Should().BeFalse("the effective settings are shown first");
        count.Value.Should().Be(3);
        name.Value.Should().Be("default");
        choice.Values.Should().Equal("a", "b");

        page.Level = SettingsLevel.Local;
        enabled.Value.Should().BeFalse();
        count.Value.Should().BeNull("not set locally");
        name.Value = "mine";
        choice.Value = "b";
        password.Value = StringSettingValue.EmptyStringValue;
        ratio.Text = 1.5.ToString();
        page.Level = SettingsLevel.Global;

        sources.Local.GetValue("plugin.Name").Should().Be("mine", "the settings of the plugin are prefixed by its container");
        sources.Local.GetValue("plugin.Choice").Should().Be("b");
        sources.Local.GetValue("plugin.Password").Should().Be("", "the empty string is entered as a token");
        sources.Local.GetValue("plugin.Ratio").Should().Be(1.5.ToString());

        page.Rows[6].IsPlainText.Should().BeTrue();
        bool activated = false;
        PluginSettingRow link = PluginSettingRow.ForText(null, "Generate", () => activated = true);
        link.IsLink.Should().BeTrue();
        link.Activate();
        activated.Should().BeTrue();

        new PluginSettingsPageViewModel(new(), new(), "Empty", "EmptyPlugin", s => s).HasNoSettings.Should().BeTrue("as labelNoSettings");
        new SettingValueStrings().StringPlaceholderText.Should().Contain(StringSettingValue.EmptyStringValue);
    }

    [Test]
    public void The_hotkeys_page_edits_the_keys_of_the_commands_and_saves_them()
    {
        FakeHotkeysHost host = new();
        HotkeysSettingsPageViewModel page = new(new HotkeysSettingsPageStrings(), host);
        SettingsDialogViewModel dialog = new(new SettingsDialogStrings(), new FakeHost());
        dialog.AddPage(page, null, "Hotkey", new FakeSources().GlobalOnly);
        dialog.Open("HotkeysSettingsPage");

        page.Settings.Select(s => s.Name).Should().Equal("Browse", "Commit");
        page.KeyText.Should().Be("None");
        page.ApplyCommand.CanExecute(null).Should().BeFalse();
        page.SelectedSettings = page.Settings[0];
        page.Commands.Select(c => c.Name).Should().Equal("Refresh", "Commit");
        page.SelectedCommand = page.Commands[0];
        page.KeyData.Should().Be(116);
        page.KeyText.Should().Be("K116");
        page.IsUsedKey.Should().BeTrue("the key is assigned already");

        page.KeyData = 117;
        page.IsUsedKey.Should().BeFalse();
        page.ApplyCommand.Execute(null);
        page.Commands[0].KeyText.Should().Be("K117");
        page.SelectedCommand = page.Commands[1];
        page.ClearCommand.Execute(null);
        page.Commands[1].KeyData.Should().Be(0);
        page.KeyText.Should().Be("None");

        dialog.OkCommand.Execute(null);
        host.Saved!.SelectMany(s => s.Commands).Select(c => c.KeyData).Should().Equal(117, 0, 13);

        page.ResetToDefaultsCommand.Execute(null);
        page.SelectedSettings.Should().BeNull();
        page.Settings[0].Commands[0].KeyData.Should().Be(116, "the preset keys");
    }

    [Test]
    public void The_scripts_page_adds_moves_deletes_and_saves_the_scripts()
    {
        FakeScriptsHost host = new();
        ScriptsSettingsPageViewModel page = new(new ScriptsSettingsPageStrings(), host);
        SettingsDialogViewModel dialog = new(new SettingsDialogStrings(), new FakeHost());
        dialog.AddPage(page, null, "Console", new FakeSources().GlobalOnly);
        dialog.Open("ScriptsSettingsPage");

        page.Scripts.Select(s => s.Name).Should().Equal("first", "second");
        page.SelectedScript.Should().BeSameAs(page.Scripts[0]);
        page.Icons.Select(i => i.Name).Should().Equal(["Bug", "Star"], "the images are loaded when the page is shown");
        page.SelectedIcon!.Name.Should().Be("Star");
        page.Scripts[0].Image.Should().Equal(2);
        page.Scripts[1].Icon.Should().BeNull("an unknown image is cleared once the images are known");
        page.MoveUpCommand.CanExecute(null).Should().BeFalse();
        page.MoveDownCommand.CanExecute(null).Should().BeTrue();

        page.SelectedIcon = page.Icons[0];
        page.Scripts[0].Icon.Should().Be("Bug");
        page.Scripts[0].Image.Should().Equal(1);
        page.MoveDownCommand.Execute(null);
        page.Scripts.Select(s => s.Name).Should().Equal("second", "first");
        page.MoveDownCommand.CanExecute(null).Should().BeFalse();

        page.AddCommand.Execute(null);
        ScriptItem added = page.SelectedScript!;
        added.Name.Should().Be("<New Script>");
        added.Enabled.Should().BeTrue();
        added.HotkeyCommandIdentifier.Should().Be(9006, "after the largest id");
        added.OnEvent = "BeforeCommit";
        added.CommandLine.Should().Be(" ");

        page.SelectedScript = page.Scripts[0];
        page.DeleteCommand.Execute(null);
        page.Scripts.Select(s => s.Name).Should().Equal("first", "<New Script>");

        (string Title, string Content)? help = null;
        page.ArgumentsHelpRequested += (_, h) => help = h;
        page.ShowArgumentsHelpCommand.Execute(null);
        help!.Value.Title.Should().Be("Arguments help");
        help.Value.Content.Should().Contain("{sHashes}");

        dialog.OkCommand.Execute(null);
        host.Saved!.Select(s => (s.Name, s.OnEvent)).Should().Equal(("first", "None"), ("<New Script>", "BeforeCommit"));
    }

    [Test]
    public void The_checklist_shows_the_checks_and_turns_the_check_at_startup_off_when_all_pass()
    {
        FakeChecklistHost host = new() { CheckAtStartup = true, FailingTranslationCheck = true };
        ChecklistSettingsPageViewModel page = new(new ChecklistSettingsPageStrings(), host);
        SettingsDialogViewModel dialog = new(new SettingsDialogStrings(), new FakeHost());
        dialog.AddPage(new GroupSettingsPageViewModel("Gitanas", "GitExtensionsSettingsGroup"), null, null, new FakeSources().None);
        dialog.AddPage(page, "GitExtensionsSettingsGroup", null, new FakeSources().GlobalOnly, asRoot: true);

        host.Results[ChecklistCheck.MergeTool] = new(ChecklistState.Unset, "no merge tool");
        host.Results[ChecklistCheck.GitFound] = new(ChecklistState.NotRecommended, "old git");
        dialog.Open(null);

        dialog.SelectedPage.Should().BeSameAs(page, "the checklist is the root page of Gitanas");
        dialog.IsInstantSavePage.Should().BeTrue();
        ChecklistItem gitFound = page.Items[0];
        gitFound.IsVisible.Should().BeTrue();
        gitFound.IsNotRecommended.Should().BeTrue();
        gitFound.IsRepairVisible.Should().BeTrue();
        page.Items.Single(i => i.Check == ChecklistCheck.GcmDetected).IsVisible.Should().BeFalse("no obsolete credential helper");
        page.Items.Single(i => i.Check == ChecklistCheck.DiffTool).IsRepairVisible.Should().BeFalse("the setting is set");
        page.CheckAtStartup.Should().BeTrue("not all checks pass");
        host.Errors.Should().Equal("broken");

        page.ClickCommand.Execute(page.Items.Single(i => i.Check == ChecklistCheck.GcmDetected));
        page.ClickCommand.Execute(gitFound);
        page.RepairCommand.Execute(page.Items.Single(i => i.Check == ChecklistCheck.MergeTool));
        host.Repaired.Should().Equal(ChecklistCheck.GitFound, ChecklistCheck.MergeTool);

        host.Results.Clear();
        host.FailingTranslationCheck = false;
        page.SaveAndRescanCommand.Execute(null);
        page.CheckAtStartup.Should().BeFalse("all checks pass");
        host.CheckAtStartup.Should().BeFalse();
        gitFound.IsSet.Should().BeTrue();

        page.CheckAtStartup = true;
        host.CheckAtStartup.Should().BeTrue("saved at once");
        host.EditorConfigured = false;
        page.CheckSettings().Should().BeFalse("the editor is checked, not shown");
    }

    internal static PluginSettingsPageViewModel CreatePluginPage()
    {
        PluginSettingsPageViewModel page = new(new PluginSettingsPageStrings(), new SettingValueStrings(), "Test plugin", "TestPlugin", settings => new PrefixedSettings("plugin.", settings));
        page.AddRow(PluginSettingRow.ForValue("Enabled", new BoolSettingValue(new BoolSetting("Enabled", "Enabled", true))));
        page.AddRow(PluginSettingRow.ForValue("Count", new IntSettingValue(new NumberSetting<int>("Count", "Count", 10))));
        page.AddRow(PluginSettingRow.ForValue("Name", new StringSettingValue(new StringSetting("Name", "Name", "default"))));
        page.AddRow(PluginSettingRow.ForValue("Choice", new ChoiceSettingValue(new ChoiceSetting("Choice", "Choice", ["a", "b"], "a"))));
        page.AddRow(PluginSettingRow.ForValue("Password", new PasswordSettingValue(new PasswordSetting("Password", "Password", ""))));
        page.AddRow(PluginSettingRow.ForValue("Ratio", new NumberTextSettingValue<double>(new NumberSetting<double>("Ratio", "Ratio", 1.0))));
        page.AddRow(PluginSettingRow.ForText(null, "A note"));
        return page;
    }

    /// <summary>As <c>GitPluginSettingsContainer</c>: the names prefixed.</summary>
    private sealed class PrefixedSettings(string prefix, SettingsSource settings) : SettingsSource
    {
        public override SettingLevel SettingLevel
        {
            get => settings.SettingLevel;
            init => throw new InvalidOperationException();
        }

        public override string? GetValue(string name) => settings.GetValue(prefix + name);

        public override void SetValue(string name, string? value) => settings.SetValue(prefix + name, value);
    }

    internal sealed class FakeHotkeysHost : IHotkeysSettingsHost
    {
        public IReadOnlyList<HotkeySettingsGroup>? Saved { get; private set; }

        public IReadOnlyList<HotkeySettingsGroup> LoadSettings() => CreateDefaultSettings();

        public IReadOnlyList<HotkeySettingsGroup> CreateDefaultSettings()
            =>
            [
                new("Browse", [new HotkeyItem(1, "Refresh", 116, ToText), new HotkeyItem(2, "Commit", 0, ToText)]),
                new("Commit", [new HotkeyItem(1, "Commit", 13, ToText)]),
            ];

        public void SaveSettings(IReadOnlyList<HotkeySettingsGroup> settings) => Saved = settings;

        public bool IsUsedKey(int keyData) => keyData == 116;

        public string ToText(int keyData) => $"K{keyData}";
    }

    internal sealed class FakeScriptsHost : IScriptsSettingsHost
    {
        public IReadOnlyList<ScriptItem>? Saved { get; private set; }

        public int MinimumUserScriptId => 9000;

        public IReadOnlyList<string> EventNames { get; } = ["None", "BeforeCommit"];

        public IReadOnlyList<ScriptItem> LoadScripts()
            =>
            [
                new ScriptItem { Name = "first", HotkeyCommandIdentifier = 9005, Icon = "Star" },
                new ScriptItem { Name = "second", HotkeyCommandIdentifier = 9001, Icon = "Missing", Command = "git", Arguments = "status" },
            ];

        public void SaveScripts(IReadOnlyList<ScriptItem> scripts) => Saved = scripts;

        public IReadOnlyList<ScriptIconChoice> LoadIcons() => [new("Bug", [1]), new("Star", [2])];

        public byte[]? GetFileIcon(string path) => [3];
    }

    internal sealed class FakeChecklistHost : IChecklistSettingsHost
    {
        public Dictionary<ChecklistCheck, ChecklistResult> Results { get; } = [];

        public List<ChecklistCheck> Repaired { get; } = [];

        public List<string> Errors { get; } = [];

        public bool EditorConfigured { get; set; } = true;

        public bool FailingTranslationCheck { get; set; }

        public bool CheckAtStartup { get; set; }

        public void BeginChecks()
        {
        }

        public ChecklistResult? Check(ChecklistCheck check)
        {
            if (check == ChecklistCheck.Translation && FailingTranslationCheck)
            {
                throw new InvalidOperationException("broken");
            }

            return Results.TryGetValue(check, out ChecklistResult? result) ? result
                : check == ChecklistCheck.GcmDetected ? null
                : new(ChecklistState.Set, check.ToString());
        }

        public bool IsEditorConfigured() => EditorConfigured;

        public void Repair(ChecklistCheck check, IChecklistActions actions) => Repaired.Add(check);

        public void ShowError(string text) => Errors.Add(text);
    }
}
