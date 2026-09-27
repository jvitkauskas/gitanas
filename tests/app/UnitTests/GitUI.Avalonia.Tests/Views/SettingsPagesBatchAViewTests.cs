using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
///  Headless view tests of the settings pages General, Appearance (Sorting, Colors, Fonts, Console style), Advanced
///  (Confirmations), Browse repository window and Diff viewer, shown in the settings dialog.
/// </summary>
[TestFixture]
public sealed class SettingsPagesBatchAViewTests : HeadlessTest
{
    private static readonly (string PageName, Type ViewType, string Screenshot)[] _pages =
    [
        ("GeneralSettingsPage", typeof(GeneralSettingsPageView), "general"),
        ("AppearanceSettingsPage", typeof(AppearanceSettingsPageView), "appearance"),
        ("SortingSettingsPage", typeof(SortingSettingsPageView), "sorting"),
        ("ColorsSettingsPage", typeof(ColorsSettingsPageView), "colors"),
        ("AppearanceFontsSettingsPage", typeof(AppearanceFontsSettingsPageView), "fonts"),
        ("ConsoleStyleSettingsPage", typeof(ConsoleStyleSettingsPageView), "console-style"),
        ("AdvancedSettingsPage", typeof(AdvancedSettingsPageView), "advanced"),
        ("ConfirmationsSettingsPage", typeof(ConfirmationsSettingsPageView), "confirmations"),
        ("FormBrowseRepoSettingsPage", typeof(FormBrowseRepoSettingsPageView), "browse-repository"),
        ("DiffViewerSettingsPage", typeof(DiffViewerSettingsPageView), "diff-viewer"),
    ];

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
        SettingsPagesBatchAViewModelTests.UsingTemporarySettings(() =>
        {
            UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
            SettingsDialogViewModel viewModel = Create(out _);
            SettingsWindow window = new() { DataContext = viewModel, Height = 900 };
            window.Show();
            viewModel.Open(null);
            ColorsSettingsPageViewModel colors = viewModel.Pages.OfType<ColorsSettingsPageViewModel>().Single();
            colors.PopulateThemeMenu([ThemeId.WindowsAppColorModeId, ThemeId.DefaultLight, ThemeId.DefaultDark]);
            colors.SelectedThemeId = ThemeId.DefaultDark;
            colors.LabelRestartIsNeededVisible = true;

            foreach ((string pageName, Type viewType, string screenshot) in _pages)
            {
                viewModel.GotoPage(pageName);
                Dispatcher.UIThread.RunJobs();

                viewModel.SelectedPage!.PageName.Should().Be(pageName);
                window.PageContent.GetLogicalDescendants().Where(viewType.IsInstanceOfType).Should().ContainSingle($"{pageName} is shown by its view");
                SaveScreenshot(window.CaptureRenderedFrame(), $"settings-{screenshot}-{theme}");
            }

            window.Close();
        }));

    [Test]
    public Task The_pages_are_in_the_tree_below_their_parents() => OnUiThreadAsync(() =>
        SettingsPagesBatchAViewModelTests.UsingTemporarySettings(() =>
        {
            SettingsDialogViewModel viewModel = Create(out _);

            SettingsTreeNode gitExtensions = viewModel.Nodes[0];
            gitExtensions.Children.Select(n => n.Title).Should().Equal("General", "Appearance", "Advanced", "Detailed");
            gitExtensions.Children[1].Children.Select(n => n.Title).Should().Equal("Sorting", "Colors", "Fonts", "Console style");
            gitExtensions.Children[2].Children.Select(n => n.Title).Should().Equal("Confirmations");
            gitExtensions.Children[1].Children.Select(n => n.Icon).Should().Equal("SortBy", "Colors", "Font", "Console");
        }));

    [Test]
    public Task General_enables_the_limit_of_commits_with_its_check_box() => OnUiThreadAsync(() =>
        SettingsPagesBatchAViewModelTests.UsingTemporarySettings(() =>
        {
            GitCommands.AppSettings.MaxRevisionGraphCommits = 0;
            SettingsDialogViewModel viewModel = Create(out _);
            SettingsWindow window = new() { DataContext = viewModel };
            window.Show();
            viewModel.Open("GeneralSettingsPage");
            Dispatcher.UIThread.RunJobs();

            NumericUpDown maxCommits = window.PageContent.GetLogicalDescendants().OfType<NumericUpDown>().Single(n => n.Name == "maxCommits");
            CheckBox commitsLimit = window.PageContent.GetLogicalDescendants().OfType<CheckBox>().Single(c => c.Name == "commitsLimit");
            maxCommits.IsEnabled.Should().BeFalse();

            commitsLimit.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            maxCommits.IsEnabled.Should().BeTrue();
            window.Close();
        }));

    [Test]
    public Task Appearance_shows_the_custom_avatar_template_for_the_custom_provider_only() => OnUiThreadAsync(() =>
        SettingsPagesBatchAViewModelTests.UsingTemporarySettings(() =>
        {
            GitCommands.AppSettings.AvatarProvider = GitCommands.AvatarProvider.Default;
            SettingsDialogViewModel viewModel = Create(out FakeHosts hosts);
            SettingsWindow window = new() { DataContext = viewModel };
            window.Show();
            viewModel.Open("AppearanceSettingsPage");
            Dispatcher.UIThread.RunJobs();

            AppearanceSettingsPageViewModel page = (AppearanceSettingsPageViewModel)viewModel.SelectedPage!;
            TextBox template = window.PageContent.GetLogicalDescendants().OfType<TextBox>().Single(t => t.Name == "customAvatarTemplate");
            template.IsVisible.Should().BeFalse();

            page.AvatarProvider = page.AvatarProviders.Single(p => p.Value == GitCommands.AvatarProvider.Custom);
            Dispatcher.UIThread.RunJobs();
            template.IsVisible.Should().BeTrue();

            window.PageContent.GetLogicalDescendants().OfType<Button>().Single(b => b.Name == "avatarProviderHelp").Command!.Execute("author-images-avatar-provider");
            hosts.Pages.Urls.Should().Equal("settings#author-images-avatar-provider");
            window.Close();
        }));

    /// <summary>The settings dialog with the pages of the batch, as <c>AddSettingsPages</c>.</summary>
    private static SettingsDialogViewModel Create(out FakeHosts hosts)
    {
        SettingsPagesBatchAViewModelTests.FakePagesHost pages = new();
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs = new();
        hosts = new FakeHosts(pages);
        SettingsDialogViewModel viewModel = new(new SettingsDialogStrings(), new SettingsDialogViewModelTests.FakeHost());
        SettingsDialogViewModelTests.FakeSources sources = new();
        SettingsDialogStrings strings = viewModel.Strings;
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.GitExtensionsGroup.Text, "GitExtensionsSettingsGroup"), null, "GitanasLogo16", sources.None);
        Add(new GeneralSettingsPageViewModel(new GeneralSettingsPageStrings(), [@"C:\src"], pages, fileDialogs), "GitExtensionsSettingsGroup", "GeneralSettings");
        Add(new AppearanceSettingsPageViewModel(new AppearanceSettingsPageStrings(), pages), "GitExtensionsSettingsGroup", "Appearance");
        Add(new SortingSettingsPageViewModel(new SortingSettingsPageStrings(), pages), "AppearanceSettingsPage", "SortBy");
        Add(new ColorsSettingsPageViewModel(new ColorsSettingsPageStrings(), pages), "AppearanceSettingsPage", "Colors");
        Add(new AppearanceFontsSettingsPageViewModel(new AppearanceFontsSettingsPageStrings(), pages), "AppearanceSettingsPage", "Font");
        Add(new ConsoleStyleSettingsPageViewModel(new ConsoleStyleSettingsPageStrings(), pages), "AppearanceSettingsPage", "Console");
        Add(new AdvancedSettingsPageViewModel(new AdvancedSettingsPageStrings(), pages), "GitExtensionsSettingsGroup", "AdvancedSettings");
        Add(new ConfirmationsSettingsPageViewModel(new ConfirmationsSettingsPageStrings()), "AdvancedSettingsPage", "BisectGood");
        viewModel.AddPage(new DetailedSettingsPageViewModel(new DetailedSettingsPageStrings()), "GitExtensionsSettingsGroup", "Settings", sources.GlobalOnly);
        Add(new FormBrowseRepoSettingsPageViewModel(new FormBrowseRepoSettingsPageStrings(), pages), "DetailedSettingsPage", "BranchFolder");
        Add(new DiffViewerSettingsPageViewModel(new DiffViewerSettingsPageStrings(), pages), "DetailedSettingsPage", "Diff");
        return viewModel;

        void Add(SettingsPageViewModel page, string parent, string icon) => viewModel.AddPage(page, parent, icon, sources.GlobalOnly);
    }

    private sealed record FakeHosts(SettingsPagesBatchAViewModelTests.FakePagesHost Pages);
}
