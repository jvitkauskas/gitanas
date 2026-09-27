using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the settings dialog (port of <c>FormSettings</c> and its pages).</summary>
[TestFixture]
public sealed class SettingsDialogViewModelTests
{
    [Test]
    public void The_tree_shows_the_root_page_of_a_group_or_its_first_child()
    {
        (SettingsDialogViewModel viewModel, _, FakeSources sources) = Create();

        viewModel.Open(initialPageName: null);
        viewModel.SelectedPage!.Title.Should().Be("Detailed", "the Gitanas group has no page of its own here, its first child is shown");
        viewModel.Title.Should().Be("Settings - Detailed");

        viewModel.GotoPage("GitSettingsGroup");
        viewModel.SelectedPage!.Title.Should().Be("Git Settings", "the introduction is the root page of the group");
        viewModel.GotoPage("RecordingPage");
        viewModel.SelectedPage.Should().BeOfType<RecordingPage>();
        viewModel.Pages.OfType<RecordingPage>().Single().Shown.Should().Be(1);
        sources.Should().NotBeNull();
    }

    [Test]
    public void The_filter_highlights_the_pages_whose_title_or_texts_match()
    {
        (SettingsDialogViewModel viewModel, _, _) = Create();

        viewModel.Filter = "remote";
        Highlighted(viewModel).Should().Equal("Detailed");
        viewModel.Filter = "graph diagonals";
        Highlighted(viewModel).Should().Equal(["Detailed"], "spaces combine keywords");
        viewModel.Filter = "git";
        Highlighted(viewModel).Should().Equal("Gitanas", "Git");

        viewModel.SelectNextFoundCommand.Execute(null);
        viewModel.SelectedPage!.Title.Should().Be("Detailed", "the group found shows its first child");
        viewModel.Filter = "";
        Highlighted(viewModel).Should().BeEmpty();
    }

    [Test]
    public void A_distributed_page_edits_the_chosen_level_and_saves_it_when_switching()
    {
        (SettingsDialogViewModel viewModel, _, FakeSources sources) = Create();
        sources.Local.SetValue(GitCommands.Settings.DetailedSettings.AddMergeLogMessages.Name, "true");
        viewModel.Open("DetailedSettingsPage");
        DetailedSettingsPageViewModel page = (DetailedSettingsPageViewModel)viewModel.SelectedPage!;

        page.Levels.Select(l => l.Level).Should().Equal(SettingsLevel.Effective, SettingsLevel.Local, SettingsLevel.Distributed, SettingsLevel.Global);
        page.Level.Should().Be(SettingsLevel.Effective);
        page.IsReadOnly.Should().BeTrue();
        page.AddLogMessages.IsThreeState.Should().BeFalse();

        page.Levels[1].IsSelected = true;
        page.Level.Should().Be(SettingsLevel.Local);
        page.IsReadOnly.Should().BeFalse();
        page.AddLogMessages.IsThreeState.Should().BeTrue("a level can leave a setting unset");
        page.AddLogMessages.Value.Should().BeTrue();
        page.RemotesFromServer.Value.Should().BeNull("not set locally");
        page.IsRevisionGraphEnabled.Should().BeFalse("the graph settings are global");

        page.MessagesCount.Text = "x";
        page.MessagesCount.IsValid.Should().BeFalse();
        page.MessagesCount.Text = "7";
        page.RemotesFromServer.Value = false;
        page.Level = SettingsLevel.Global;

        sources.Local.GetValue(GitCommands.Settings.DetailedSettings.MergeLogMessagesCount.Name).Should().Be("7", "the local settings are saved when switching");
        sources.Local.GetValue(GitCommands.Settings.DetailedSettings.GetRemoteBranchesDirectlyFromRemote.Name).Should().Be("false");
        page.IsRevisionGraphEnabled.Should().BeTrue();
    }

    [Test]
    public void Ok_saves_all_the_pages_and_the_settings_sets_or_shows_the_error()
    {
        (SettingsDialogViewModel viewModel, FakeHost host, _) = Create();
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Open(null);

        host.Error = "access denied";
        viewModel.OkCommand.Execute(null);
        host.Errors.Should().Equal("Failed to save all settings: access denied");
        closed.Should().BeNull();

        host.Error = null;
        viewModel.OkCommand.Execute(null);
        viewModel.Pages.OfType<RecordingPage>().Single().Saved.Should().Be(2);
        host.SavedSets.Should().Be(1);
        closed.Should().BeTrue();
        viewModel.IsSaved.Should().BeTrue();
    }

    [Test]
    public void Apply_asks_the_host_to_restart_after_the_save_and_closes_when_confirmed()
    {
        (SettingsDialogViewModel viewModel, FakeHost host, _) = Create();
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Open(null);

        viewModel.ApplyCommand.Execute(null);
        host.RestartQuestions.Should().Be(1);
        closed.Should().BeNull("no restart was confirmed");
        viewModel.IsRestartConfirmed.Should().BeFalse();

        host.Restart = true;
        viewModel.ApplyCommand.Execute(null);
        host.RestartQuestions.Should().Be(2);
        closed.Should().BeTrue();
        viewModel.IsRestartConfirmed.Should().BeTrue();
    }

    private static List<string> Highlighted(SettingsDialogViewModel viewModel)
        => [.. viewModel.Nodes.SelectMany(n => n.DescendantsAndSelf()).Where(n => n.IsHighlighted).Select(n => n.Title)];

    internal static (SettingsDialogViewModel ViewModel, FakeHost Host, FakeSources Sources) Create()
    {
        FakeHost host = new();
        FakeSources sources = new();
        SettingsDialogViewModel viewModel = new(new SettingsDialogStrings(), host);
        SettingsDialogStrings strings = viewModel.Strings;
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.GitExtensionsGroup.Text, "GitExtensionsSettingsGroup"), null, "GitanasLogo16", sources.None);
        viewModel.AddPage(new DetailedSettingsPageViewModel(new DetailedSettingsPageStrings()), "GitExtensionsSettingsGroup", "Settings", sources.Distributed);
        viewModel.AddPage(new RecordingPage(), "DetailedSettingsPage", "Blame", sources.GlobalOnly);
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.GitGroup.Text, "GitSettingsGroup"), null, "GitLogo16", sources.None);
        viewModel.AddPage(new IntroductionSettingsPageViewModel("Git Settings", "Select one of the subnodes", "GitRootIntroductionPage"), "GitSettingsGroup", null, sources.None, asRoot: true);
        return (viewModel, host, sources);
    }

    /// <summary>A page recording its loads, saves and showings.</summary>
    internal sealed class RecordingPage : SettingsPageViewModel
    {
        public override string Title => "Recording";

        public override string PageName => "RecordingPage";

        public int Saved { get; private set; }

        public int Shown { get; private set; }

        public override void OnPageShown() => Shown++;

        protected override void PageToSettings(SettingsSource? settings) => Saved++;
    }

    /// <summary>In-memory settings of each level.</summary>
    internal sealed class FakeSources
    {
        public MemorySettings Effective { get; } = new(SettingLevel.Effective);

        public MemorySettings Local { get; } = new(SettingLevel.Local);

        public MemorySettings DistributedLevel { get; } = new(SettingLevel.Distributed);

        public MemorySettings Global { get; } = new(SettingLevel.Global);

        public Dictionary<SettingsLevel, SettingsSource> None { get; } = [];

        public Dictionary<SettingsLevel, SettingsSource> GlobalOnly => new() { [SettingsLevel.Global] = Global };

        public Dictionary<SettingsLevel, SettingsSource> Distributed => new()
        {
            [SettingsLevel.Effective] = Effective,
            [SettingsLevel.Local] = Local,
            [SettingsLevel.Distributed] = DistributedLevel,
            [SettingsLevel.Global] = Global,
        };
    }

    internal sealed class MemorySettings : SettingsSource
    {
        private readonly Dictionary<string, string?> _values = [];

        public MemorySettings(SettingLevel level)
        {
            SettingLevel = level;
        }

        public override string? GetValue(string name) => _values.TryGetValue(name, out string? value) ? value : null;

        public override void SetValue(string name, string? value) => _values[name] = value;
    }

    internal sealed class FakeHost : ISettingsDialogHost
    {
        public string? Error { get; set; }

        public int SavedSets { get; private set; }

        public List<string> Errors { get; } = [];

        public string? SaveSettingsSets()
        {
            if (Error is null)
            {
                SavedSets++;
            }

            return Error;
        }

        public void ShowError(string heading, string text) => Errors.Add($"{heading}: {text}");

        public bool Restart { get; set; }

        public int RestartQuestions { get; private set; }

        public bool ConfirmRestart()
        {
            RestartQuestions++;
            return Restart;
        }
    }
}
