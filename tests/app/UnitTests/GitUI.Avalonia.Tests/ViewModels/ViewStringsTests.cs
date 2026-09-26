using System.Xml.Linq;
using GitExtensions.Extensibility.Translations;
using GitExtensions.Extensibility.Translations.Xliff;
using GitExtensions.Plugins.CreateLocalBranches;
using GitExtensions.Plugins.DeleteUnusedBranches;
using GitExtensions.Plugins.FindLargeFiles;
using GitExtensions.Plugins.GitImpact;
using GitExtensions.Plugins.GitStatistics;
using GitExtensions.Plugins.Gource;
using GitExtensions.Plugins.ProxySwitcher;
using GitExtensions.Plugins.ReleaseNotesGenerator;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.CommandsDialogs.RepoHosting;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.Editor;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.SpellChecker;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUI.Presentation.UserControls.Blame;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.LeftPanel;
using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>
///  The ported views reuse the XLIFF ids of the WinForms forms they replace, so that existing translations keep
///  working. These tests pin every declared entry to the checked-in English.xlf.
/// </summary>
[TestFixture]
public sealed class ViewStringsTests
{
    private static readonly Lazy<Dictionary<(string Category, string Id), string>> _englishXlf = new(() => LoadEnglishXlf("English.xlf"));
    private static readonly Lazy<Dictionary<(string Category, string Id), string>> _englishPluginsXlf = new(() => LoadEnglishXlf("English.Plugins.xlf"));

    private static IEnumerable<TestCaseData> AllViewStrings()
    {
        yield return new TestCaseData(new AboutStrings()).SetArgDisplayNames(nameof(AboutStrings));
        yield return new TestCaseData(new RenameBranchStrings()).SetArgDisplayNames(nameof(RenameBranchStrings));
        yield return new TestCaseData(new CommitTemplateSettingsStrings()).SetArgDisplayNames(nameof(CommitTemplateSettingsStrings));
        yield return new TestCaseData(new ProcessStrings()).SetArgDisplayNames(nameof(ProcessStrings));
        yield return new TestCaseData(new RemoteProcessStrings()).SetArgDisplayNames(nameof(RemoteProcessStrings));
        yield return new TestCaseData(new BrowseStrings()).SetArgDisplayNames(nameof(BrowseStrings));
        yield return new TestCaseData(new RevisionGridMenuStrings()).SetArgDisplayNames(nameof(RevisionGridMenuStrings));
        yield return new TestCaseData(new FilterToolBarStrings()).SetArgDisplayNames(nameof(FilterToolBarStrings));
        yield return new TestCaseData(new GpgInfoStrings()).SetArgDisplayNames(nameof(GpgInfoStrings));
        yield return new TestCaseData(new BrowseToolbarStrings()).SetArgDisplayNames(nameof(BrowseToolbarStrings));
        yield return new TestCaseData(new BrowseLayoutStrings()).SetArgDisplayNames(nameof(BrowseLayoutStrings));
        yield return new TestCaseData(new BrowseToolbarItemsStrings()).SetArgDisplayNames(nameof(BrowseToolbarItemsStrings));
        yield return new TestCaseData(new OutputHistoryStrings()).SetArgDisplayNames(nameof(OutputHistoryStrings));
        yield return new TestCaseData(new BrowsePluginStrings()).SetArgDisplayNames(nameof(BrowsePluginStrings));
        yield return new TestCaseData(new BuildReportStrings()).SetArgDisplayNames(nameof(BuildReportStrings));
        yield return new TestCaseData(new CommandlineHelpStrings()).SetArgDisplayNames(nameof(CommandlineHelpStrings));
        yield return new TestCaseData(new AddFilesStrings()).SetArgDisplayNames(nameof(AddFilesStrings));
        yield return new TestCaseData(new DonateStrings()).SetArgDisplayNames(nameof(DonateStrings));
        yield return new TestCaseData(new ResetChangesStrings()).SetArgDisplayNames(nameof(ResetChangesStrings));
        yield return new TestCaseData(new DeleteTagStrings()).SetArgDisplayNames(nameof(DeleteTagStrings));
        yield return new TestCaseData(new InitStrings()).SetArgDisplayNames(nameof(InitStrings));
        yield return new TestCaseData(new GoToLineStrings()).SetArgDisplayNames(nameof(GoToLineStrings));
        yield return new TestCaseData(new FilePromptStrings()).SetArgDisplayNames(nameof(FilePromptStrings));
        yield return new TestCaseData(new PuttyErrorStrings()).SetArgDisplayNames(nameof(PuttyErrorStrings));
        yield return new TestCaseData(new SelectMultipleBranchesStrings()).SetArgDisplayNames(nameof(SelectMultipleBranchesStrings));
        yield return new TestCaseData(new DialogBoxStrings()).SetArgDisplayNames(nameof(DialogBoxStrings));
        yield return new TestCaseData(new ChooseTranslationStrings()).SetArgDisplayNames(nameof(ChooseTranslationStrings));
        yield return new TestCaseData(new AvailableEncodingsStrings()).SetArgDisplayNames(nameof(AvailableEncodingsStrings));
        yield return new TestCaseData(new AddSubmoduleStrings()).SetArgDisplayNames(nameof(AddSubmoduleStrings));
        yield return new TestCaseData(new CleanupRepositoryStrings()).SetArgDisplayNames(nameof(CleanupRepositoryStrings));
        yield return new TestCaseData(new MergeSubmoduleStrings()).SetArgDisplayNames(nameof(MergeSubmoduleStrings));
        yield return new TestCaseData(new CreateWorktreeStrings()).SetArgDisplayNames(nameof(CreateWorktreeStrings));
        yield return new TestCaseData(new OpenDirectoryStrings()).SetArgDisplayNames(nameof(OpenDirectoryStrings));
        yield return new TestCaseData(new BranchSelectorStrings()).SetArgDisplayNames(nameof(BranchSelectorStrings));
        yield return new TestCaseData(new HelpImageStrings()).SetArgDisplayNames(nameof(HelpImageStrings));
        yield return new TestCaseData(new DeleteBranchStrings()).SetArgDisplayNames(nameof(DeleteBranchStrings));
        yield return new TestCaseData(new DeleteRemoteBranchStrings()).SetArgDisplayNames(nameof(DeleteRemoteBranchStrings));
        yield return new TestCaseData(new MergeBranchStrings()).SetArgDisplayNames(nameof(MergeBranchStrings));
        yield return new TestCaseData(new CommitSummaryStrings()).SetArgDisplayNames(nameof(CommitSummaryStrings));
        yield return new TestCaseData(new CherryPickStrings()).SetArgDisplayNames(nameof(CherryPickStrings));
        yield return new TestCaseData(new RevertCommitStrings()).SetArgDisplayNames(nameof(RevertCommitStrings));
        yield return new TestCaseData(new ResetCurrentBranchStrings()).SetArgDisplayNames(nameof(ResetCurrentBranchStrings));
        yield return new TestCaseData(new ResetAnotherBranchStrings()).SetArgDisplayNames(nameof(ResetAnotherBranchStrings));
        yield return new TestCaseData(new ArchiveStrings()).SetArgDisplayNames(nameof(ArchiveStrings));
        yield return new TestCaseData(new CreateBranchStrings()).SetArgDisplayNames(nameof(CreateBranchStrings));
        yield return new TestCaseData(new CreateTagStrings()).SetArgDisplayNames(nameof(CreateTagStrings));
        yield return new TestCaseData(new LocalRemoteBranchSelectorStrings()).SetArgDisplayNames(nameof(LocalRemoteBranchSelectorStrings));
        yield return new TestCaseData(new CheckoutRevisionStrings()).SetArgDisplayNames(nameof(CheckoutRevisionStrings));
        yield return new TestCaseData(new CompareToBranchStrings()).SetArgDisplayNames(nameof(CompareToBranchStrings));
        yield return new TestCaseData(new BisectStrings()).SetArgDisplayNames(nameof(BisectStrings));
        yield return new TestCaseData(new GoToCommitStrings()).SetArgDisplayNames(nameof(GoToCommitStrings));
        yield return new TestCaseData(new DashboardCategoryTitleStrings()).SetArgDisplayNames(nameof(DashboardCategoryTitleStrings));
        yield return new TestCaseData(new AddToGitIgnoreStrings()).SetArgDisplayNames(nameof(AddToGitIgnoreStrings));
        yield return new TestCaseData(new CheckoutBranchStrings()).SetArgDisplayNames(nameof(CheckoutBranchStrings));
        yield return new TestCaseData(new CloneStrings()).SetArgDisplayNames(nameof(CloneStrings));
        yield return new TestCaseData(new RevisionFilterStrings()).SetArgDisplayNames(nameof(RevisionFilterStrings));
        yield return new TestCaseData(new FixHomeStrings()).SetArgDisplayNames(nameof(FixHomeStrings));
        yield return new TestCaseData(new UpdatesStrings()).SetArgDisplayNames(nameof(UpdatesStrings));
        yield return new TestCaseData(new ManageWorktreeStrings()).SetArgDisplayNames(nameof(ManageWorktreeStrings));
        yield return new TestCaseData(new SubmodulesStrings()).SetArgDisplayNames(nameof(SubmodulesStrings));
        yield return new TestCaseData(new ReflogStrings()).SetArgDisplayNames(nameof(ReflogStrings));
        yield return new TestCaseData(new RecentReposSettingsStrings()).SetArgDisplayNames(nameof(RecentReposSettingsStrings));
        yield return new TestCaseData(new DashboardStrings()).SetArgDisplayNames(nameof(DashboardStrings));
        yield return new TestCaseData(new UserRepositoriesListStrings()).SetArgDisplayNames(nameof(UserRepositoriesListStrings));
        yield return new TestCaseData(new RemotesStrings()).SetArgDisplayNames(nameof(RemotesStrings));
        yield return new TestCaseData(new ChooseCommitStrings()).SetArgDisplayNames(nameof(ChooseCommitStrings));
        yield return new TestCaseData(new FormatPatchStrings()).SetArgDisplayNames(nameof(FormatPatchStrings));
        yield return new TestCaseData(new GitAttributesEditorStrings()).SetArgDisplayNames(nameof(GitAttributesEditorStrings));
        yield return new TestCaseData(new MailMapEditorStrings()).SetArgDisplayNames(nameof(MailMapEditorStrings));
        yield return new TestCaseData(new FileEditorStrings()).SetArgDisplayNames(nameof(FileEditorStrings));
        yield return new TestCaseData(new ViewPatchStrings()).SetArgDisplayNames(nameof(ViewPatchStrings));
        yield return new TestCaseData(new SparseWorkingCopyStrings()).SetArgDisplayNames(nameof(SparseWorkingCopyStrings));
        yield return new TestCaseData(new GitIgnoreStrings()).SetArgDisplayNames(nameof(GitIgnoreStrings));
        yield return new TestCaseData(new GitIgnoreModelStrings()).SetArgDisplayNames(nameof(GitIgnoreModelStrings));
        yield return new TestCaseData(new GitLocalExcludeModelStrings()).SetArgDisplayNames(nameof(GitLocalExcludeModelStrings));
        yield return new TestCaseData(new ChangeLogStrings()).SetArgDisplayNames(nameof(ChangeLogStrings));
        yield return new TestCaseData(new VerifyStrings()).SetArgDisplayNames(nameof(VerifyStrings));
        yield return new TestCaseData(new TextViewerStrings()).SetArgDisplayNames(nameof(TextViewerStrings));
        yield return new TestCaseData(new FileStatusListStrings()).SetArgDisplayNames(nameof(FileStatusListStrings));
        yield return new TestCaseData(new DiffStrings()).SetArgDisplayNames(nameof(DiffStrings));
        yield return new TestCaseData(new FileViewerStrings()).SetArgDisplayNames(nameof(FileViewerStrings));
        yield return new TestCaseData(new FileStatusListMenuStrings()).SetArgDisplayNames(nameof(FileStatusListMenuStrings));
        yield return new TestCaseData(new CopyPathsStrings()).SetArgDisplayNames(nameof(CopyPathsStrings));
        yield return new TestCaseData(new StashStrings()).SetArgDisplayNames(nameof(StashStrings));
        yield return new TestCaseData(new CommitInfoStrings()).SetArgDisplayNames(nameof(CommitInfoStrings));
        yield return new TestCaseData(new AvatarStrings()).SetArgDisplayNames(nameof(AvatarStrings));
        yield return new TestCaseData(new CommitDiffStrings()).SetArgDisplayNames(nameof(CommitDiffStrings));
        yield return new TestCaseData(new BlameStrings()).SetArgDisplayNames(nameof(BlameStrings));
        yield return new TestCaseData(new FileHistoryStrings()).SetArgDisplayNames(nameof(FileHistoryStrings));
        yield return new TestCaseData(new CommitStrings()).SetArgDisplayNames(nameof(CommitStrings));
        yield return new TestCaseData(new PushStrings()).SetArgDisplayNames(nameof(PushStrings));
        yield return new TestCaseData(new SettingsDialogStrings()).SetArgDisplayNames(nameof(SettingsDialogStrings));
        yield return new TestCaseData(new GitRootIntroductionPageStrings()).SetArgDisplayNames(nameof(GitRootIntroductionPageStrings));
        yield return new TestCaseData(new PluginRootIntroductionPageStrings()).SetArgDisplayNames(nameof(PluginRootIntroductionPageStrings));
        yield return new TestCaseData(new CommitDialogSettingsPageStrings()).SetArgDisplayNames(nameof(CommitDialogSettingsPageStrings));
        yield return new TestCaseData(new BlameViewerSettingsPageStrings()).SetArgDisplayNames(nameof(BlameViewerSettingsPageStrings));
        yield return new TestCaseData(new DetailedSettingsPageStrings()).SetArgDisplayNames(nameof(DetailedSettingsPageStrings));
        yield return new TestCaseData(new GeneralSettingsPageStrings()).SetArgDisplayNames(nameof(GeneralSettingsPageStrings));
        yield return new TestCaseData(new AppearanceSettingsPageStrings()).SetArgDisplayNames(nameof(AppearanceSettingsPageStrings));
        yield return new TestCaseData(new SortingSettingsPageStrings()).SetArgDisplayNames(nameof(SortingSettingsPageStrings));
        yield return new TestCaseData(new ColorsSettingsPageStrings()).SetArgDisplayNames(nameof(ColorsSettingsPageStrings));
        yield return new TestCaseData(new AppearanceFontsSettingsPageStrings()).SetArgDisplayNames(nameof(AppearanceFontsSettingsPageStrings));
        yield return new TestCaseData(new ConsoleStyleSettingsPageStrings()).SetArgDisplayNames(nameof(ConsoleStyleSettingsPageStrings));
        yield return new TestCaseData(new DiffViewerSettingsPageStrings()).SetArgDisplayNames(nameof(DiffViewerSettingsPageStrings));
        yield return new TestCaseData(new FormBrowseRepoSettingsPageStrings()).SetArgDisplayNames(nameof(FormBrowseRepoSettingsPageStrings));
        yield return new TestCaseData(new AdvancedSettingsPageStrings()).SetArgDisplayNames(nameof(AdvancedSettingsPageStrings));
        yield return new TestCaseData(new ConfirmationsSettingsPageStrings()).SetArgDisplayNames(nameof(ConfirmationsSettingsPageStrings));
        yield return new TestCaseData(new SpellCheckStrings()).SetArgDisplayNames(nameof(SpellCheckStrings));
        yield return new TestCaseData(new PullStrings()).SetArgDisplayNames(nameof(PullStrings));
        yield return new TestCaseData(new PatchGridStrings()).SetArgDisplayNames(nameof(PatchGridStrings));
        yield return new TestCaseData(new RebaseStrings()).SetArgDisplayNames(nameof(RebaseStrings));
        yield return new TestCaseData(new ApplyPatchStrings()).SetArgDisplayNames(nameof(ApplyPatchStrings));
        yield return new TestCaseData(new ResolveConflictsStrings()).SetArgDisplayNames(nameof(ResolveConflictsStrings));
        yield return new TestCaseData(new PluginSettingsPageStrings()).SetArgDisplayNames(nameof(PluginSettingsPageStrings));
        yield return new TestCaseData(new SettingValueStrings()).SetArgDisplayNames(nameof(SettingValueStrings));
        yield return new TestCaseData(new HotkeysSettingsPageStrings()).SetArgDisplayNames(nameof(HotkeysSettingsPageStrings));
        yield return new TestCaseData(new ScriptsSettingsPageStrings()).SetArgDisplayNames(nameof(ScriptsSettingsPageStrings));
        yield return new TestCaseData(new ChecklistSettingsPageStrings()).SetArgDisplayNames(nameof(ChecklistSettingsPageStrings));
        yield return new TestCaseData(new GitSettingsPageStrings()).SetArgDisplayNames(nameof(GitSettingsPageStrings));
        yield return new TestCaseData(new GitConfigSettingsPageStrings()).SetArgDisplayNames(nameof(GitConfigSettingsPageStrings));
        yield return new TestCaseData(new GitConfigAdvancedSettingsPageStrings()).SetArgDisplayNames(nameof(GitConfigAdvancedSettingsPageStrings));
        yield return new TestCaseData(new SshSettingsPageStrings()).SetArgDisplayNames(nameof(SshSettingsPageStrings));
        yield return new TestCaseData(new BuildServerIntegrationSettingsPageStrings()).SetArgDisplayNames(nameof(BuildServerIntegrationSettingsPageStrings));
        yield return new TestCaseData(new RevisionLinksSettingsPageStrings()).SetArgDisplayNames(nameof(RevisionLinksSettingsPageStrings));
        yield return new TestCaseData(new ShellExtensionSettingsPageStrings()).SetArgDisplayNames(nameof(ShellExtensionSettingsPageStrings));
        yield return new TestCaseData(new GitCommandLogStrings()).SetArgDisplayNames(nameof(GitCommandLogStrings));
        yield return new TestCaseData(new FindInCommitFilesGitGrepStrings()).SetArgDisplayNames(nameof(FindInCommitFilesGitGrepStrings));
        yield return new TestCaseData(new QuickItemSelectorStrings()).SetArgDisplayNames(nameof(QuickItemSelectorStrings));
        yield return new TestCaseData(new LogStrings()).SetArgDisplayNames(nameof(LogStrings));
        yield return new TestCaseData(new FindAndReplaceStrings()).SetArgDisplayNames(nameof(FindAndReplaceStrings));
        yield return new TestCaseData(new LeftPanelStrings()).SetArgDisplayNames(nameof(LeftPanelStrings));

        // The plugins' forms, whose strings are in English.Plugins.xlf (their assemblies are loaded from the Plugins folder).
        yield return new TestCaseData(new CreateLocalBranchesStrings()).SetArgDisplayNames(nameof(CreateLocalBranchesStrings));
        yield return new TestCaseData(new DeleteUnusedBranchesStrings()).SetArgDisplayNames(nameof(DeleteUnusedBranchesStrings));
        yield return new TestCaseData(new FindLargeFilesStrings()).SetArgDisplayNames(nameof(FindLargeFilesStrings));
        yield return new TestCaseData(new GourceStartStrings()).SetArgDisplayNames(nameof(GourceStartStrings));
        yield return new TestCaseData(new ProxySwitcherStrings()).SetArgDisplayNames(nameof(ProxySwitcherStrings));
        yield return new TestCaseData(new ReleaseNotesGeneratorStrings()).SetArgDisplayNames(nameof(ReleaseNotesGeneratorStrings));
        yield return new TestCaseData(new GitStatisticsStrings()).SetArgDisplayNames(nameof(GitStatisticsStrings));
        yield return new TestCaseData(new ImpactStrings()).SetArgDisplayNames(nameof(ImpactStrings));

        // The repository hosting dialogs of GitUI.
        yield return new TestCaseData(new ForkAndCloneStrings()).SetArgDisplayNames(nameof(ForkAndCloneStrings));
        yield return new TestCaseData(new CreatePullRequestStrings()).SetArgDisplayNames(nameof(CreatePullRequestStrings));
        yield return new TestCaseData(new ViewPullRequestsStrings()).SetArgDisplayNames(nameof(ViewPullRequestsStrings));
    }

    [TestCaseSource(nameof(AllViewStrings))]
    public void Every_entry_matches_English_xlf(ViewStrings strings)
    {
        RecordingTranslation recorded = new();

        ((ITranslate)strings).AddTranslationItems(recorded);

        // As TranslationApp: the strings of the plugins go to English.Plugins.xlf, the others to English.xlf.
        bool isPlugin = strings.GetType().Assembly.GetName().Name!.StartsWith("GitExtensions.Plugins.", StringComparison.Ordinal);
        (Dictionary<(string Category, string Id), string> xlf, string fileName) = isPlugin
            ? (_englishPluginsXlf.Value, "English.Plugins.xlf")
            : (_englishXlf.Value, "English.xlf");

        recorded.Items.Should().NotBeEmpty();
        foreach ((string category, string item, string property, string neutralValue) in recorded.Items)
        {
            string id = $"{item}.{property}";
            xlf.Should().ContainKey((category, id), $"'{category}/{id}' must exist in {fileName}");
            Normalize(xlf[(category, id)]).Should().Be(Normalize(neutralValue), $"the source text of '{category}/{id}'");
        }
    }

    [Test]
    public void TranslateItems_applies_translations_and_falls_back_to_neutral_text()
    {
        RenameBranchStrings strings = new();
        RecordingTranslation translation = new() { Translations = { [("FormRenameBranch", "label1", "Text")] = "Neuer Name" } };

        ((ITranslate)strings).TranslateItems(translation);

        strings.NewName.Text.Should().Be("Neuer Name");
        strings.Rename.Text.Should().Be("Rename");
        strings.NewName.NeutralText.Should().Be("New name");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Main_and_plugin_catalogs_preserve_German_translations_in_either_order(bool pluginsFirst)
    {
        string directory = Path.Combine(FindRepoRoot(), "src", "app", "GitUI", "Translation");
        TranslationFile main = TranslationSerializer.Deserialize(Path.Combine(directory, "German.xlf"))!;
        TranslationFile plugins = TranslationSerializer.Deserialize(Path.Combine(directory, "German.Plugins.xlf"))!;
        TranslationFile[] catalogs = pluginsFirst ? [plugins, main] : [main, plugins];

        // Catalogs cache missing entries, so also check a second window using the same files.
        for (int window = 0; window < 2; window++)
        {
            RenameBranchStrings rename = new();
            FindLargeFilesStrings largeFiles = new();
            foreach (TranslationFile catalog in catalogs)
            {
                ((ITranslate)rename).TranslateItems(catalog);
                ((ITranslate)largeFiles).TranslateItems(catalog);
            }

            rename.NewName.Text.Should().Be("Neuer Name");
            rename.Rename.Text.Should().Be("Umbenennen");
            largeFiles.Title.Text.Should().Be("Finde große Dateien");
        }
    }

    // English.xlf stores multi-line sources with whatever line endings git checked out.
    private static string Normalize(string text) => text.ReplaceLineEndings("\n");

    private static Dictionary<(string Category, string Id), string> LoadEnglishXlf(string fileName)
    {
        string path = Path.Combine(FindRepoRoot(), "src", "app", "GitUI", "Translation", fileName);
        XDocument document = XDocument.Load(path);
        XNamespace ns = document.Root!.Name.Namespace;

        return document.Descendants(ns + "file")
            .SelectMany(file => file.Descendants(ns + "trans-unit").Select(unit => (
                Category: (string)file.Attribute("original")!,
                Id: (string)unit.Attribute("id")!,
                Source: (string)unit.Element(ns + "source")!)))
            .ToDictionary(u => (u.Category, u.Id), u => u.Source);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GitExtensions.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found");
    }

    private sealed class RecordingTranslation : ITranslation
    {
        public List<(string Category, string Item, string Property, string NeutralValue)> Items { get; } = [];

        public Dictionary<(string Category, string Item, string Property), string> Translations { get; } = [];

        public void AddTranslationItem(string category, string item, string property, string neutralValue)
            => Items.Add((category, item, property, neutralValue));

        public string? TranslateItem(string category, string item, string property, Func<string?> provideDefaultValue)
            => Translations.TryGetValue((category, item, property), out string? value) ? value : provideDefaultValue();
    }
}
