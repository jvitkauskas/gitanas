using GitCommands;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitExtUtils.GitUI.Theming;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>
///  View model tests of the settings pages General, Appearance (Sorting, Colors, Fonts, Console style), Advanced
///  (Confirmations), Browse repository window and Diff viewer. The pages read and write <c>AppSettings</c> in a temporary
///  settings file.
/// </summary>
[TestFixture]
public sealed class SettingsPagesBatchAViewModelTests
{
    [Test]
    public void General_enables_the_submodule_status_with_the_status_and_saves_the_limit_of_commits() => UsingTemporarySettings(() =>
    {
        AppSettings.MaxRevisionGraphCommits = 0;
        AppSettings.DefaultPullAction = GitPullAction.Default;
        AppSettings.ShowGitStatusInBrowseToolbar = false;
        AppSettings.ShowGitStatusForArtificialCommits = false;
        AppSettings.ShowSubmoduleStatus = true;
        AppSettings.UpdateSubmodulesOnCheckout = null;
        FakePagesHost host = new();
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs = new() { Folder = @"C:\repos" };
        GeneralSettingsPageViewModel page = new(new GeneralSettingsPageStrings(), [@"C:\src"], host, fileDialogs);

        page.LoadSettings();

        page.IsCommitsLimited.Should().BeFalse();
        page.DefaultPullAction!.Value.Should().Be(GitPullAction.None, "the default is shown as opening the pull dialog");
        page.PullActions.Select(a => a.Text).Should().Equal("Open pull dialog", "Pull - merge", "Pull - rebase", "Fetch", "Fetch all", "Fetch and prune all");
        page.IsShowSubmoduleStatusInBrowseEnabled.Should().BeFalse();
        page.ShowSubmoduleStatusInBrowse.Should().BeFalse("the submodule status needs the status of the files");
        page.UpdateModules.Should().BeNull("unset asks");

        page.ShowGitStatusInToolbar = true;
        page.IsShowSubmoduleStatusInBrowseEnabled.Should().BeTrue();
        page.ShowSubmoduleStatusInBrowse = true;
        page.IsCommitsLimited = true;
        page.MaxCommits = 5000;
        page.DefaultPullAction = page.PullActions[2];
        page.BrowseDefaultCloneDestinationCommand.Execute(null);
        page.DefaultCloneDestination.Should().Be(@"C:\repos");
        page.SaveSettings();

        AppSettings.MaxRevisionGraphCommits.Should().Be(5000);
        AppSettings.DefaultPullAction.Should().Be(GitPullAction.Rebase);
        AppSettings.ShowSubmoduleStatus.Should().BeTrue();
        AppSettings.DefaultCloneDestinationPath.Should().Be(@"C:\repos");
        AppSettings.UpdateSubmodulesOnCheckout.Should().BeNull();
        page.CloneDestinations.Should().Equal(@"C:\src");

        page.IsCommitsLimited = false;
        page.SaveSettings();
        AppSettings.MaxRevisionGraphCommits.Should().Be(0, "no limit");
    });

    [Test]
    public void Appearance_shows_the_custom_template_for_the_custom_provider_and_clears_the_cache_when_the_avatars_change() => UsingTemporarySettings(() =>
    {
        AppSettings.AvatarProvider = AvatarProvider.Default;
        AppSettings.TruncatePathMethod = TruncatePathMethod.TrimStart;
        AppSettings.Dictionary = "none";
        FakePagesHost host = new();
        AppearanceSettingsPageViewModel page = new(new AppearanceSettingsPageStrings(), host);

        // Stored in the registry: the page writes it only when changed, restored in case it did.
        bool showCurrentBranchInVisualStudio = AppSettings.ShowCurrentBranchInVisualStudio;
        try
        {
            page.LoadSettings();

            page.TruncatePathMethod!.Text.Should().Be("Trim start");
            page.AvatarProvider!.Value.Should().Be(AvatarProvider.Default);
            page.IsCustomAvatarTemplateVisible.Should().BeFalse();
            page.Languages[0].Should().Be("English");
            page.Dictionaries.Should().Equal("None");
            page.Dictionary.Should().Be("None");

            page.SaveSettings();
            host.AvatarProviderUpdates.Should().Be(0, "the avatar settings did not change");
            host.TranslatedStringsReinitialized.Should().Be(1);
            AppSettings.Dictionary.Should().Be("none");

            page.AvatarProvider = page.AvatarProviders.Single(p => p.Value == AvatarProvider.Custom);
            page.IsCustomAvatarTemplateVisible.Should().BeTrue();
            page.TruncatePathMethod = page.TruncatePathMethods[3];
            page.SaveSettings();
            host.AvatarProviderUpdates.Should().Be(1);
            AppSettings.AvatarProvider.Should().Be(AvatarProvider.Custom);
            AppSettings.TruncatePathMethod.Should().Be(TruncatePathMethod.FileNameOnly);

            page.ClearImageCacheCommand.Execute(null);
            page.OpenHelpTranslateCommand.Execute(null);
            page.OpenSettingsManualCommand.Execute("author-images-avatar-provider");
            host.AvatarCacheClears.Should().Be(1);
            host.Urls.Should().Equal(AppearanceSettingsPageViewModel.TranslationsWikiUrl, "settings#author-images-avatar-provider");
            AppSettings.ShowCurrentBranchInVisualStudio.Should().Be(showCurrentBranchInVisualStudio);
        }
        finally
        {
            if (AppSettings.ShowCurrentBranchInVisualStudio != showCurrentBranchInVisualStudio)
            {
                AppSettings.ShowCurrentBranchInVisualStudio = showCurrentBranchInVisualStudio;
            }
        }
    });

    [Test]
    public void Sorting_saves_the_orders_by_their_index() => UsingTemporarySettings(() =>
    {
        AppSettings.RefsSortBy = GitRefsSortBy.Default;
        AppSettings.PrioritizedBranchNames = "main";
        FakePagesHost host = new();
        SortingSettingsPageViewModel page = new(new SortingSettingsPageStrings(), host);

        page.LoadSettings();

        page.PrioritizedBranchNames.Should().Be("main");
        page.RevisionSortOrders.Should().HaveCount(Enum.GetValues<RevisionSortOrder>().Length);
        page.BranchesSortByIndex.Should().Be((int)GitRefsSortBy.Default);

        page.BranchesSortByIndex = (int)GitRefsSortBy.committerdate;
        page.RevisionSortOrderIndex = (int)RevisionSortOrder.Topology;
        page.PrioritizedRemoteNames = "origin";
        RevisionSortOrder revisionSortOrder = AppSettings.RevisionSortOrder.Value;
        try
        {
            page.SaveSettings();

            AppSettings.RefsSortBy.Should().Be(GitRefsSortBy.committerdate);
            AppSettings.RevisionSortOrder.Value.Should().Be(RevisionSortOrder.Topology);
            AppSettings.PrioritizedRemoteNames.Should().Be("origin");
        }
        finally
        {
            // A runtime setting, kept in memory for the other tests.
            AppSettings.RevisionSortOrder.Value = revisionSortOrder;
        }

        page.OpenPrioRemoteNamesHelpCommand.Execute(null);
        host.Urls.Should().Equal("settings#sorting-sort-prioritized-remotes");
    });

    [Test]
    public void Colors_is_driven_by_the_controller_of_the_themes()
    {
        FakePagesHost host = new();
        ColorsSettingsPageViewModel page = new(new ColorsSettingsPageStrings(), host);
        FakeColorsController controller = host.ColorsController!;

        page.PopulateThemeMenu([ThemeId.DefaultLight, new ThemeId("solarized")]);
        page.Themes.Select(t => t.Text).Should().Equal("light", "solarized, user-defined");

        page.SelectedThemeId = new ThemeId("solarized");
        page.SelectedTheme!.Text.Should().Be("solarized, user-defined");
        controller.Calls.Should().Equal(nameof(IColorsSettingsPageController.HandleSelectedThemeChanged));

        page.SelectedThemeId = new ThemeId("deleted");
        host.Errors.Should().Equal("Theme not found: deleted, user-defined");
        page.SelectedThemeId.Should().Be(ThemeId.DefaultLight, "the first theme is chosen instead");

        controller.Calls.Clear();
        page.SelectedThemeVariations = [ThemeVariations.Colorblind];
        page.IsColorblind.Should().BeTrue();
        page.SelectedThemeVariations.Should().Equal(ThemeVariations.Colorblind);
        page.UseSystemVisualStyle = true;
        page.OpenUserThemesFolderCommand.Execute(null);
        controller.Calls.Should().Equal(
            nameof(IColorsSettingsPageController.HandleUseColorblindVariationChanged),
            nameof(IColorsSettingsPageController.HandleUseSystemVisualStyleChanged),
            nameof(IColorsSettingsPageController.ShowUserThemesDirectory));
    }

    [Test]
    public void Colors_loads_and_applies_the_theme_settings_with_the_controller() => UsingTemporarySettings(() =>
    {
        AppSettings.FillRefLabels = true;
        FakePagesHost host = new();
        ColorsSettingsPageViewModel page = new(new ColorsSettingsPageStrings(), host);

        page.LoadSettings();
        page.FillRefLabels.Should().BeTrue();
        page.FillRefLabels = false;
        page.SaveSettings();

        AppSettings.FillRefLabels.Should().BeFalse();
        host.ColorsController!.Calls.Should().Equal(nameof(IColorsSettingsPageController.ShowThemeSettings), nameof(IColorsSettingsPageController.ApplyThemeSettings));
    });

    [Test]
    public void Fonts_keeps_the_font_when_the_dialog_is_cancelled_and_saves_the_chosen_ones() => UsingTemporarySettings(() =>
    {
        FakePagesHost host = new();
        AppearanceFontsSettingsPageViewModel page = new(new AppearanceFontsSettingsPageStrings(), host);

        page.LoadSettings();
        page.CodeFont!.Text.Should().Be("Consolas, 10");
        page.CodeFont.DisplaySize.Should().BeApproximately(13.33, 0.01, "points are shown in device independent pixels");

        page.ChangeCodeFontCommand.Execute(null);
        page.CodeFont!.Text.Should().Be("Consolas, 10", "the dialog was cancelled");

        host.PickedFont = FakePagesHost.Font("Cascadia Code", 11.6f);
        page.ChangeCodeFontCommand.Execute(null);
        page.CodeFont!.Text.Should().Be(OperatingSystem.IsMacOS() ? $"Cascadia Code, {11.6:0.##}" : "Cascadia Code, 12", "rounded as WinForms, with the decimals on macOS");
        host.PickedKinds.Should().Equal(SettingsFontKind.Code, SettingsFontKind.Code);
        page.SaveSettings();

        host.Fonts[SettingsFontKind.Code]!.FamilyName.Should().Be("Cascadia Code");
        host.Fonts[SettingsFontKind.Application]!.FamilyName.Should().Be("Segoe UI");
    });

    [Test]
    public void ConsoleStyle_lists_the_themes_of_the_emulator_and_resets_the_font() => UsingTemporarySettings(() =>
    {
        AppSettings.ConsoleEmulatorName.Value = "windows-terminal";
        AppSettings.ConEmuStyle.Value = "campbell";
        FakePagesHost host = new() { Fonts = { [SettingsFontKind.Console] = FakePagesHost.Font("Lucida Console", 12) } };
        ConsoleStyleSettingsPageViewModel page = new(new ConsoleStyleSettingsPageStrings(), host);

        page.LoadSettings();

        page.ConsoleEmulator!.DisplayName.Should().Be("Windows Terminal");
        page.Styles.Should().Equal("Default", "Campbell", "One Half Dark");
        page.StyleIndex.Should().Be(1, "the saved style is found ignoring the case");
        page.IsStyleEnabled.Should().BeTrue();
        page.ConsoleFontText.Should().Be("Lucida Console, 12");
        page.HasConsoleFont.Should().BeTrue();

        page.ConsoleEmulator = page.ConsoleEmulators[0];
        page.Styles.Should().Equal("Default");
        page.IsStyleEnabled.Should().BeFalse("ConEmu has no themes here");
        page.ResetConsoleFontCommand.Execute(null);
        page.ConsoleFontText.Should().Be("Console Default");
        page.HasConsoleFont.Should().BeFalse();
        page.SaveSettings();

        AppSettings.ConsoleEmulatorName.Value.Should().Be("ConEmu");
        AppSettings.ConEmuStyle.Value.Should().Be("Default");
        host.Fonts[SettingsFontKind.Console].Should().BeNull();
    });

    [Test]
    public void DiffViewer_saves_the_settings_and_the_view_settings_as_default() => UsingTemporarySettings(() =>
    {
        AppSettings.DiffVerticalRulerPosition = 80;
        FakePagesHost host = new();
        DiffViewerSettingsPageViewModel page = new(new DiffViewerSettingsPageStrings(), host);

        page.LoadSettings();
        page.VerticalRulerPosition.Should().Be(80);
        page.Strings.ShowDiffForAllParents.Text.Should().Be("Show file differences for all parents in browse dialog", "the text of TranslatedStrings replaces the one of the page");

        page.VerticalRulerPosition = 100;
        page.UseGitColoring = true;
        page.UseGEThemeGitColoring = true;
        page.SaveSettings();
        page.SaveCurrentViewSettingsAsDefaultCommand.Execute(null);

        AppSettings.DiffVerticalRulerPosition.Should().Be(100);
        AppSettings.UseGitColoring.Value.Should().BeTrue();
        AppSettings.ReverseGitColoring.Value.Should().BeTrue();
        host.ViewSettingsSavedAsDefault.Should().Be(1);
    });

    [Test]
    public void FormBrowseRepo_refuses_a_shell_without_executable_and_shows_the_output_history_panel() => UsingTemporarySettings(() =>
    {
        AppSettings.ConEmuTerminal.Value = "bash";
        AppSettings.ShowOutputHistoryAsTab.Value = true;
        AppSettings.OutputHistoryDepth.Value = 20;
        FakePagesHost host = new();
        FormBrowseRepoSettingsPageViewModel page = new(new FormBrowseRepoSettingsPageStrings(), host);

        page.OutputHistoryTooltip.Should().EndWith("using the hotkey Ctrl+Alt+O.");
        page.LoadSettings();
        page.Shells.Select(s => s.Name).Should().Equal("bash", "pwsh");
        page.Shell!.Name.Should().Be("bash");
        page.OutputHistoryDepth.Should().Be(20);

        page.Shell = page.Shells[1];
        host.ShellsNotFound.Should().Be(1);
        page.Shell.Name.Should().Be("bash", "a shell without executable cannot be chosen");

        page.ShowOutputHistoryAsTab = false;
        page.SaveSettings();

        AppSettings.ConEmuTerminal.Value.Should().Be("bash");
        AppSettings.ShowOutputHistoryAsTab.Value.Should().BeFalse();
        AppSettings.OutputHistoryPanelVisible.Value.Should().BeTrue("the output history is shown as a panel");
    });

    [Test]
    public void Advanced_saves_the_symbol_normalising_branch_names() => UsingTemporarySettings(() =>
    {
        AppSettings.AutoNormaliseSymbol = "-";
        FakePagesHost host = new();
        AdvancedSettingsPageViewModel page = new(new AdvancedSettingsPageStrings(), host);

        page.LoadSettings();
        page.AutoNormaliseSymbol!.Text.Should().Be("-");
        page.AutoNormaliseSymbols.Select(s => s.Text).Should().Equal("_", "-", "(none)");

        page.AutoNormaliseSymbol = page.AutoNormaliseSymbols[2];
        page.SaveSettings();
        AppSettings.AutoNormaliseSymbol.Should().Be("");

        page.OpenSettingsManualCommand.Execute("general-auto-normalise-branch-name");
        host.Urls.Should().Equal("settings#general-auto-normalise-branch-name");
    });

    [Test]
    public void Confirmations_are_the_inverse_of_the_dont_confirm_settings() => UsingTemporarySettings(() =>
    {
        AppSettings.DontConfirmAmend.Value = true;
        AppSettings.ConfirmBranchCheckout.Value = true;
        AppSettings.AutoPopStashAfterPull = null;
        AppSettings.AutoPopStashAfterCheckoutBranch = true;
        AppSettings.DontConfirmUpdateSubmodulesOnCheckout = false;
        ConfirmationsSettingsPageViewModel page = new(new ConfirmationsSettingsPageStrings());

        page.LoadSettings();

        page.Amend.Should().BeFalse();
        page.BranchCheckout.Should().BeTrue();
        page.AutoPopStashAfterPull.Should().BeNull("unset asks");
        page.AutoPopStashAfterCheckout.Should().BeFalse("popped automatically");
        page.UpdateModules.Should().BeTrue();

        page.Amend = true;
        page.AutoPopStashAfterPull = true;
        page.UpdateModules = null;
        page.SaveSettings();

        AppSettings.DontConfirmAmend.Value.Should().BeFalse();
        AppSettings.AutoPopStashAfterPull.Should().BeFalse();
        AppSettings.DontConfirmUpdateSubmodulesOnCheckout.Should().BeNull();
    });

    /// <summary>Runs <paramref name="action"/> with <c>AppSettings</c> in a temporary settings file.</summary>
    internal static void UsingTemporarySettings(Action action)
    {
        string directory = Path.Combine(Path.GetTempPath(), "GitUI.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "GitExtensions.settings");
        File.WriteAllText(path, @"<?xml version=""1.0"" encoding=""utf-8""?><dictionary />");
        try
        {
            using GitExtSettingsCache cache = GitExtSettingsCache.Create(path, useSharedCache: false);
            AppSettings.UsingContainer(new DistributedSettings(lowerPriority: null, cache, SettingLevel.Global), action);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>The application for the pages: recorded links, messages and calls; in-memory fonts; fixed emulators and shells.</summary>
    internal sealed class FakePagesHost : IAppearanceSettingsPageHost, IColorsSettingsPageHost, IConsoleStyleSettingsPageHost, IDiffViewerSettingsPageHost, IFormBrowseRepoSettingsPageHost
    {
        public List<string> Urls { get; } = [];

        public List<string> Errors { get; } = [];

        public int TranslatedStringsReinitialized { get; private set; }

        public int AvatarCacheClears { get; private set; }

        public int AvatarProviderUpdates { get; private set; }

        public int ViewSettingsSavedAsDefault { get; private set; }

        public int ShellsNotFound { get; private set; }

        public FakeColorsController? ColorsController { get; private set; }

        public Dictionary<SettingsFontKind, SettingsFont?> Fonts { get; } = new()
        {
            [SettingsFontKind.Application] = Font("Segoe UI", 9),
            [SettingsFontKind.Code] = Font("Consolas", 10),
            [SettingsFontKind.Commit] = Font("Segoe UI", 9),
            [SettingsFontKind.Monospace] = Font("Consolas", 9),
            [SettingsFontKind.Console] = null,
        };

        /// <summary>The font chosen in the font dialog; none cancels it.</summary>
        public SettingsFont? PickedFont { get; set; }

        public List<SettingsFontKind> PickedKinds { get; } = [];

        public static SettingsFont Font(string family, float size, bool bold = false) => new(family + size, family, size, bold, IsItalic: false);

        public void OpenUrl(string url) => Urls.Add(url);

        public void OpenUserManual(string subFolder, string anchorName) => Urls.Add($"{subFolder}#{anchorName}");

        public void ReinitializeTranslatedStrings() => TranslatedStringsReinitialized++;

        public void ShowError(string text) => Errors.Add(text);

        public void ClearAvatarCache() => AvatarCacheClears++;

        public void UpdateAvatarProvider() => AvatarProviderUpdates++;

        public IColorsSettingsPageController CreateController(IColorsSettingsPageView page) => ColorsController = new FakeColorsController();

        public IReadOnlyList<ConsoleEmulatorChoice> ConsoleEmulators { get; } =
        [
            new("ConEmu", "ConEmu", []),
            new("windows-terminal", "Windows Terminal", ["Campbell", "One Half Dark"]),
        ];

        public SettingsFont? GetFont(SettingsFontKind kind) => Fonts[kind];

        public void SetFont(SettingsFontKind kind, SettingsFont? font) => Fonts[kind] = font;

        public SettingsFont? PickFont(SettingsFontKind kind, SettingsFont? current)
        {
            PickedKinds.Add(kind);
            return PickedFont;
        }

        public void SaveCurrentViewSettingsAsDefault() => ViewSettingsSavedAsDefault++;

        public IReadOnlyList<ShellChoice> GetShells() => [new("bash", HasExecutable: true), new("pwsh", HasExecutable: false)];

        public string FocusOutputHistoryHotkey => "Ctrl+Alt+O";

        public void ShowShellNotFound() => ShellsNotFound++;
    }

    /// <summary>Records the calls of the colors page to its controller.</summary>
    internal sealed class FakeColorsController : IColorsSettingsPageController
    {
        public List<string> Calls { get; } = [];

        public void ShowThemeSettings() => Calls.Add(nameof(ShowThemeSettings));

        public void ApplyThemeSettings() => Calls.Add(nameof(ApplyThemeSettings));

        public void HandleSelectedThemeChanged() => Calls.Add(nameof(HandleSelectedThemeChanged));

        public void HandleUseSystemVisualStyleChanged() => Calls.Add(nameof(HandleUseSystemVisualStyleChanged));

        public void HandleUseColorblindVariationChanged() => Calls.Add(nameof(HandleUseColorblindVariationChanged));

        public void ShowAppThemesDirectory() => Calls.Add(nameof(ShowAppThemesDirectory));

        public void ShowUserThemesDirectory() => Calls.Add(nameof(ShowUserThemesDirectory));
    }
}
