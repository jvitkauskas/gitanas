using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.Utils;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Extensibility.Translations;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the appearance settings; ids match <c>AppearanceSettingsPage</c>.</summary>
public sealed class AppearanceSettingsPageStrings : ViewStrings
{
    public AppearanceSettingsPageStrings()
        : base("AppearanceSettingsPage")
    {
        Title = Add("$this", "Text", "Appearance");
        NoDictFile = Add("_noDictFile", "Text", "None");
        NoDictFilesFound = Add("_noDictFilesFound", "Text", "No dictionary files found in: {0}");
        NoImageServiceTooltip = Add("_noImageServiceTooltip", "Text", "A default image, if the provider has no image for the email address.\n\nClick this info icon for more details.");
        AvatarProviderTooltip = Add("_avatarProviderTooltip", "Text", "The avatar provider defines the source for user-defined avatar images.\nThe \"Default\" provider uses GitHub and Gravatar,\nthe \"Custom\" provider allows you to set custom provider URLs and\n\"None\" disables user-defined avatars.\n\nClick this info icon for more details.");
        General = Add("gbGeneral", "Text", "&General");
        ShowRelativeDate = Add("chkShowRelativeDate", "Text", "Show relative date instead of full date");
        ShowRepoCurrentBranch = Add("chkShowRepoCurrentBranch", "Text", "Show current branch names in the dashboard and the recent repositories dropdown menu");
        ShowCurrentBranchInVisualStudio = Add("chkShowCurrentBranchInVisualStudio", "Text", "Show current branch in Visual Studio");
        EnableAutoScale = Add("chkEnableAutoScale", "Text", "Auto scale user interface when high DPI is used");
        TruncateLongFilenames = Add("truncateLongFilenames", "Text", "Truncate long filenames");
        TruncateNone = Add("truncatePathMethod", "Item0", "None");
        TruncateCompact = Add("truncatePathMethod", "Item1", "Compact");
        TruncateTrimStart = Add("truncatePathMethod", "Item2", "Trim start");
        TruncateFileNameOnly = Add("truncatePathMethod", "Item3", "Filename only");
        AuthorImages = Add("gbAuthorImages", "Text", "&Author images");
        ShowAuthorAvatarInCommitGraph = Add("ShowAuthorAvatarInCommitGraph", "Text", "Show author's avatar column in the commit graph");
        ShowAuthorAvatarInCommitInfo = Add("ShowAuthorAvatarInCommitInfo", "Text", "Show author's avatar in the commit info view");
        CacheDays = Add("lblCacheDays", "Text", "Cache images (days)");
        AvatarProvider = Add("lblAvatarProvider", "Text", "Avatar provider");
        NoImageService = Add("lblNoImageService", "Text", "Fallback generated avatar style");
        CustomAvatarTemplate = Add("lblCustomAvatarTemplate", "Text", "Custom avatar template");
        ClearImageCache = Add("ClearImageCache", "Text", "Clear image cache");
        Languages = Add("gbLanguages", "Text", "&Language");
        Language = Add("lblLanguage", "Text", "Language (restart required)");
        HelpTranslate = Add("helpTranslate", "Text", "Help translate");
        SpellingDictionary = Add("lblSpellingDictionary", "Text", "Dictionary for spelling checker");
        DownloadDictionary = Add("downloadDictionary", "Text", "Download dictionary");
    }

    public TranslatedText Title { get; }

    public TranslatedText NoDictFile { get; }

    public TranslatedText NoDictFilesFound { get; }

    public TranslatedText NoImageServiceTooltip { get; }

    public TranslatedText AvatarProviderTooltip { get; }

    public TranslatedText General { get; }

    public TranslatedText ShowRelativeDate { get; }

    public TranslatedText ShowRepoCurrentBranch { get; }

    public TranslatedText ShowCurrentBranchInVisualStudio { get; }

    public TranslatedText EnableAutoScale { get; }

    public TranslatedText TruncateLongFilenames { get; }

    public TranslatedText TruncateNone { get; }

    public TranslatedText TruncateCompact { get; }

    public TranslatedText TruncateTrimStart { get; }

    public TranslatedText TruncateFileNameOnly { get; }

    public TranslatedText AuthorImages { get; }

    public TranslatedText ShowAuthorAvatarInCommitGraph { get; }

    public TranslatedText ShowAuthorAvatarInCommitInfo { get; }

    public TranslatedText CacheDays { get; }

    public TranslatedText AvatarProvider { get; }

    public TranslatedText NoImageService { get; }

    public TranslatedText CustomAvatarTemplate { get; }

    public TranslatedText ClearImageCache { get; }

    public TranslatedText Languages { get; }

    public TranslatedText Language { get; }

    public TranslatedText HelpTranslate { get; }

    public TranslatedText SpellingDictionary { get; }

    public TranslatedText DownloadDictionary { get; }
}

/// <summary>What the appearance settings need from the application: the cache of the avatars (<c>AvatarService</c>).</summary>
public interface IAppearanceSettingsPageHost : ISettingsPageServices
{
    /// <summary>As <c>ClearImageCache_Click</c>: clears the cache of the avatars.</summary>
    void ClearAvatarCache();

    /// <summary>As the end of <c>PageToSettings</c> when the avatar settings changed: updates the provider and clears the cache.</summary>
    void UpdateAvatarProvider();
}

/// <summary>Port of <c>AppearanceSettingsPage</c> (global settings).</summary>
public sealed partial class AppearanceSettingsPageViewModel : SettingsPageWithServicesViewModel
{
    /// <summary>As <c>_translationsWikiURL</c>.</summary>
    public const string TranslationsWikiUrl = "https://github.com/gitextensions/gitextensions/wiki/Translations";

    /// <summary>As <c>_spellingWikiURL</c>.</summary>
    public const string SpellingWikiUrl = "https://github.com/gitextensions/gitextensions/wiki/Spelling";

    private readonly IAppearanceSettingsPageHost _host;

    public AppearanceSettingsPageViewModel(AppearanceSettingsPageStrings strings, IAppearanceSettingsPageHost host)
        : base(host)
    {
        Strings = strings;
        _host = host;
        TruncatePathMethods =
        [
            new(GitCommands.TruncatePathMethod.None, strings.TruncateNone.Text),
            new(GitCommands.TruncatePathMethod.Compact, strings.TruncateCompact.Text),
            new(GitCommands.TruncatePathMethod.TrimStart, strings.TruncateTrimStart.Text),
            new(GitCommands.TruncatePathMethod.FileNameOnly, strings.TruncateFileNameOnly.Text),
        ];
        TruncatePathMethod = TruncatePathMethods[0];
        AvatarProviders = [.. EnumHelper.GetValues<AvatarProvider>().Select(e => new SettingChoice<AvatarProvider>(e, e.GetDescription()))];
        AvatarFallbackTypes = [.. EnumHelper.GetValues<AvatarFallbackType>().Select(e => new SettingChoice<AvatarFallbackType>(e, e.GetDescription()))];
        AvatarProvider = AvatarProviders[0];
        AvatarFallbackType = AvatarFallbackTypes[0];
    }

    public AppearanceSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "AppearanceSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    [ObservableProperty]
    public partial bool ShowRelativeDate { get; set; }

    [ObservableProperty]
    public partial bool ShowRepoCurrentBranch { get; set; }

    [ObservableProperty]
    public partial bool ShowCurrentBranchInVisualStudio { get; set; }

    public bool CanConfigureVisualStudio => OperatingSystem.IsWindows();

    [ObservableProperty]
    public partial bool EnableAutoScale { get; set; }

    /// <summary>The items of <c>truncatePathMethod</c>.</summary>
    public IReadOnlyList<SettingChoice<TruncatePathMethod>> TruncatePathMethods { get; }

    [ObservableProperty]
    public partial SettingChoice<TruncatePathMethod>? TruncatePathMethod { get; set; }

    [ObservableProperty]
    public partial bool ShowAuthorAvatarInCommitGraph { get; set; }

    [ObservableProperty]
    public partial bool ShowAuthorAvatarInCommitInfo { get; set; }

    [ObservableProperty]
    public partial decimal DaysToCacheImages { get; set; }

    /// <summary>The items of <c>AvatarProvider</c>, with the descriptions of the values.</summary>
    public IReadOnlyList<SettingChoice<AvatarProvider>> AvatarProviders { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomAvatarTemplateVisible))]
    public partial SettingChoice<AvatarProvider>? AvatarProvider { get; set; }

    /// <summary>As <c>ManageAvatarOptionsDisplay</c>: the template is shown for the custom provider only.</summary>
    public bool IsCustomAvatarTemplateVisible => AvatarProvider?.Value == GitCommands.AvatarProvider.Custom;

    /// <summary>The items of <c>_NO_TRANSLATE_NoImageService</c>.</summary>
    public IReadOnlyList<SettingChoice<AvatarFallbackType>> AvatarFallbackTypes { get; }

    [ObservableProperty]
    public partial SettingChoice<AvatarFallbackType>? AvatarFallbackType { get; set; }

    [ObservableProperty]
    public partial string CustomAvatarTemplate { get; set; } = "";

    /// <summary>The items of <c>Language</c>: English and the translations.</summary>
    public ObservableCollection<string> Languages { get; } = [];

    [ObservableProperty]
    public partial string? Language { get; set; }

    /// <summary>The items of <c>Dictionary</c>: none, then the dictionaries (listed when the list drops down).</summary>
    public ObservableCollection<string> Dictionaries { get; } = [];

    [ObservableProperty]
    public partial string? Dictionary { get; set; }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        ShowRelativeDate = AppSettings.RelativeDate;
        ShowRepoCurrentBranch = AppSettings.ShowRepoCurrentBranch;
        ShowCurrentBranchInVisualStudio = AppSettings.ShowCurrentBranchInVisualStudio;
        EnableAutoScale = AppSettings.EnableAutoScale;
        TruncatePathMethod = TruncatePathMethods.FirstOrDefault(m => m.Value == AppSettings.TruncatePathMethod) ?? TruncatePathMethods[0];

        DaysToCacheImages = Math.Clamp(AppSettings.AvatarImageCacheDays, 0, 400);
        ShowAuthorAvatarInCommitInfo = AppSettings.ShowAuthorAvatarInCommitInfo;
        ShowAuthorAvatarInCommitGraph = AppSettings.ShowAuthorAvatarColumn;
        AvatarProvider = AvatarProviders.FirstOrDefault(p => p.Value == AppSettings.AvatarProvider);
        AvatarFallbackType = AvatarFallbackTypes.FirstOrDefault(t => t.Value == AppSettings.AvatarFallbackType);
        CustomAvatarTemplate = AppSettings.CustomAvatarTemplate;

        Languages.Clear();
        Languages.Add("English");
        foreach (string translation in Translator.GetAllTranslations())
        {
            Languages.Add(translation);
        }

        Language = Languages.FirstOrDefault(l => string.Equals(l, AppSettings.Translation, StringComparison.OrdinalIgnoreCase));

        Dictionaries.Clear();
        Dictionaries.Add(Strings.NoDictFile.Text);
        Dictionary = Dictionaries[0];
        if (!AppSettings.Dictionary.Equals("none", StringComparison.InvariantCultureIgnoreCase))
        {
            string dictionaryFile = string.Concat(Path.Join(AppSettings.GetDictionaryDir(), AppSettings.Dictionary), ".dic");
            if (File.Exists(dictionaryFile))
            {
                Dictionaries.Add(AppSettings.Dictionary);
                Dictionary = AppSettings.Dictionary;
            }
        }

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.RelativeDate = ShowRelativeDate;
        AppSettings.ShowRepoCurrentBranch = ShowRepoCurrentBranch;
        if (CanConfigureVisualStudio && AppSettings.ShowCurrentBranchInVisualStudio != ShowCurrentBranchInVisualStudio)
        {
            // Stored in the registry, not in the settings: written only when changed.
            AppSettings.ShowCurrentBranchInVisualStudio = ShowCurrentBranchInVisualStudio;
        }

        AppSettings.EnableAutoScale = EnableAutoScale;
        AppSettings.TruncatePathMethod = TruncatePathMethod?.Value ?? GitCommands.TruncatePathMethod.None;

        GitCommands.AvatarProvider provider = AvatarProvider?.Value ?? AppSettings.AvatarProvider;
        GitCommands.AvatarFallbackType? fallbackType = AvatarFallbackType?.Value;
        bool shouldClearCache =
            AppSettings.AvatarProvider != provider
            || (fallbackType is not null && AppSettings.AvatarFallbackType != fallbackType)
            || AppSettings.CustomAvatarTemplate != CustomAvatarTemplate;

        AppSettings.ShowAuthorAvatarColumn = ShowAuthorAvatarInCommitGraph;
        AppSettings.ShowAuthorAvatarInCommitInfo = ShowAuthorAvatarInCommitInfo;
        AppSettings.AvatarImageCacheDays = (int)DaysToCacheImages;
        AppSettings.CustomAvatarTemplate = CustomAvatarTemplate;

        AppSettings.Translation = Language ?? "";
        Services.ReinitializeTranslatedStrings();

        AppSettings.AvatarProvider = provider;
        if (fallbackType is { } imageType)
        {
            AppSettings.AvatarFallbackType = imageType;
        }

        if (shouldClearCache)
        {
            _host.UpdateAvatarProvider();
        }

        AppSettings.Dictionary = Dictionaries.IndexOf(Dictionary ?? "") <= 0 ? "none" : Dictionary!;

        base.PageToSettings(settings);
    }

    /// <summary>As <c>Dictionary_DropDown</c>: the dictionaries are listed again, keeping the chosen one.</summary>
    public void RefreshDictionaries()
    {
        try
        {
            string? currentDictionary = Dictionary;
            string[] files = Directory.GetFiles(AppSettings.GetDictionaryDir(), "*.dic", SearchOption.TopDirectoryOnly);

            Dictionaries.Clear();
            Dictionaries.Add(Strings.NoDictFile.Text);
            foreach (string fileName in files)
            {
                Dictionaries.Add(new FileInfo(fileName).Name.Replace(".dic", ""));
            }

            Dictionary = Dictionaries.Contains(currentDictionary ?? "") ? currentDictionary : Dictionaries[0];
        }
        catch
        {
            Services.ShowError(string.Format(Strings.NoDictFilesFound.Text, AppSettings.GetDictionaryDir()));
        }
    }

    [RelayCommand]
    private void ClearImageCache() => _host.ClearAvatarCache();

    [RelayCommand]
    private void OpenHelpTranslate() => Services.OpenUrl(TranslationsWikiUrl);

    [RelayCommand]
    private void OpenDownloadDictionary() => Services.OpenUrl(SpellingWikiUrl);
}

/// <summary>Strings of the sorting settings; ids match <c>SortingSettingsPage</c>.</summary>
public sealed class SortingSettingsPageStrings : ViewStrings
{
    public SortingSettingsPageStrings()
        : base("SortingSettingsPage")
    {
        Title = Add("$this", "Text", "Sorting");
        RevisionSortWarningTooltip = Add("_revisionSortWarningTooltip", "Text", "Sorting revisions may delay rendering of the revision graph.");
        PrioBranchNamesTooltip = Add("_prioBranchNamesTooltip", "Text", "Regex to prioritize branch names in the left panel and commit info.\nThe branches matching the pattern will be shown before the others.\nSeparate the priorities with ';'.");
        PrioRemoteNamesTooltip = Add("_prioRemoteNamesTooltip", "Text", "Regex to prioritize remote names in the left panel and commit info.\nThe remotes matching the pattern will be shown before the others.\nSeparate the priorities with ';'.");
        Sorting = Add("gbGeneral", "Text", "Sorting");
        RevisionsSortBy = Add("lblRevisionsSortBy", "Text", "Sort revisions by");
        BranchesSortBy = Add("lblBranchesSortBy", "Text", "Sort branches by");
        BranchesOrder = Add("lblBranchesOrder", "Text", "Order branches");
        PrioBranchNames = Add("lblPrioBranchNames", "Text", "Prioritized branches");
        PrioRemoteNames = Add("lblPrioRemoteNames", "Text", "Prioritized remotes");
    }

    public TranslatedText Title { get; }

    public TranslatedText RevisionSortWarningTooltip { get; }

    public TranslatedText PrioBranchNamesTooltip { get; }

    public TranslatedText PrioRemoteNamesTooltip { get; }

    public TranslatedText Sorting { get; }

    public TranslatedText RevisionsSortBy { get; }

    public TranslatedText BranchesSortBy { get; }

    public TranslatedText BranchesOrder { get; }

    public TranslatedText PrioBranchNames { get; }

    public TranslatedText PrioRemoteNames { get; }
}

/// <summary>Port of <c>SortingSettingsPage</c> (global settings).</summary>
public sealed partial class SortingSettingsPageViewModel(SortingSettingsPageStrings strings, ISettingsPageServices services) : SettingsPageWithServicesViewModel(services)
{
    public SortingSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "SortingSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>The items of <c>_NO_TRANSLATE_cmbRevisionsSortBy</c>, with the descriptions of the values.</summary>
    public IReadOnlyList<string> RevisionSortOrders { get; } = [.. EnumHelper.GetValues<RevisionSortOrder>().Select(e => e.GetDescription())];

    /// <summary>The items of <c>_NO_TRANSLATE_cmbBranchesSortBy</c>.</summary>
    public IReadOnlyList<string> BranchesSortBys { get; } = [.. EnumHelper.GetValues<GitRefsSortBy>().Select(e => e.GetDescription())];

    /// <summary>The items of <c>_NO_TRANSLATE_cmbBranchesOrder</c>.</summary>
    public IReadOnlyList<string> BranchesOrders { get; } = [.. EnumHelper.GetValues<GitRefsSortOrder>().Select(e => e.GetDescription())];

    /// <summary>As the index of <c>_NO_TRANSLATE_cmbRevisionsSortBy</c>: the value of the <see cref="RevisionSortOrder"/>.</summary>
    [ObservableProperty]
    public partial int RevisionSortOrderIndex { get; set; }

    [ObservableProperty]
    public partial int BranchesSortByIndex { get; set; }

    [ObservableProperty]
    public partial int BranchesOrderIndex { get; set; }

    [ObservableProperty]
    public partial string PrioritizedBranchNames { get; set; } = "";

    [ObservableProperty]
    public partial string PrioritizedRemoteNames { get; set; } = "";

    protected override void SettingsToPage(SettingsSource? settings)
    {
        RevisionSortOrderIndex = (int)AppSettings.RevisionSortOrder.Value;
        BranchesOrderIndex = (int)AppSettings.RefsSortOrder;
        BranchesSortByIndex = (int)AppSettings.RefsSortBy;
        PrioritizedBranchNames = AppSettings.PrioritizedBranchNames;
        PrioritizedRemoteNames = AppSettings.PrioritizedRemoteNames;

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.RevisionSortOrder.Value = (RevisionSortOrder)RevisionSortOrderIndex;
        AppSettings.RevisionSortOrder.Save();
        AppSettings.RefsSortOrder = (GitRefsSortOrder)BranchesOrderIndex;
        AppSettings.RefsSortBy = (GitRefsSortBy)BranchesSortByIndex;
        AppSettings.PrioritizedBranchNames = PrioritizedBranchNames;
        AppSettings.PrioritizedRemoteNames = PrioritizedRemoteNames;

        Services.ReinitializeTranslatedStrings();

        base.PageToSettings(settings);
    }

    [RelayCommand]
    private void OpenRevisionSortOrderHelp() => Services.OpenUserManual("settings", "sorting-sort-author-date");

    [RelayCommand]
    private void OpenPrioBranchNamesHelp() => Services.OpenUserManual("settings", "sorting-sort-prioritized-branches");

    [RelayCommand]
    private void OpenPrioRemoteNamesHelp() => Services.OpenUserManual("settings", "sorting-sort-prioritized-remotes");
}
