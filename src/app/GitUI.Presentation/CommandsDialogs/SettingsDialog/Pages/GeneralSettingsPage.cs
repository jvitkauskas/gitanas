using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the general settings; ids match <c>GeneralSettingsPage</c>.</summary>
public sealed class GeneralSettingsPageStrings : ViewStrings
{
    public GeneralSettingsPageStrings()
        : base("GeneralSettingsPage")
    {
        Title = Add("$this", "Text", "General");
        OpenPullDialog = Add("_openPullDialog", "Text", "Open pull dialog");
        PullMerge = Add("_pullMerge", "Text", "Pull - merge");
        PullRebase = Add("_pullRebase", "Text", "Pull - rebase");
        Fetch = Add("_fetch", "Text", "Fetch");
        FetchAll = Add("_fetchAll", "Text", "Fetch all");
        FetchAndPruneAll = Add("_fetchAndPruneAll", "Text", "Fetch and prune all");
        Performance = Add("groupBoxPerformance", "Text", "Performance");
        ShowGitStatusInToolbar = Add("chkShowGitStatusInToolbar", "Text", "Show number of changed files on commit button");
        ShowGitStatusForArtificialCommits = Add("chkShowGitStatusForArtificialCommits", "Text", "Show number of changed files for artificial commits");
        ShowSubmoduleStatusInBrowse = Add("chkShowSubmoduleStatusInBrowse", "Text", "Show submodule status in browse menu");
        ShowStashCountInBrowseWindow = Add("chkShowStashCountInBrowseWindow", "Text", "Show stash count on status bar in browse window");
        ShowAheadBehindDataInBrowseWindow = Add("chkShowAheadBehindDataInBrowseWindow", "Text", "Show ahead and behind information on status bar in browse window");
        CheckForUncommittedChangesInCheckoutBranch = Add("chkCheckForUncommittedChangesInCheckoutBranch", "Text", "Check for uncommitted changes in checkout branch dialog");
        CommitsLimit = Add("lblCommitsLimit", "Text", "Limit number of commits to be loaded");
        Behaviour = Add("groupBoxBehaviour", "Text", "Behaviour");
        CloseProcessDialog = Add("chkCloseProcessDialog", "Text", "Close Process dialog when process succeeds");
        ShowGitCommandLine = Add("chkShowGitCommandLine", "Text", "Show console window when executing git process");
        UseHistogramDiffAlgorithm = Add("chkUseHistogramDiffAlgorithm", "Text", "Use histogram diff algorithm");
        StashUntrackedFiles = Add("chkStashUntrackedFiles", "Text", "Include untracked files in autostash");
        UpdateModules = Add("chkUpdateModules", "Text", "Update submodules on checkout");
        FollowRenamesInFileHistory = Add("chkFollowRenamesInFileHistory", "Text", "Follow renames in file history");
        FollowRenamesInFileHistoryExact = Add("chkFollowRenamesInFileHistoryExact", "Text", "Follow exact renames and copies only");
        StartWithRecentWorkingDir = Add("chkStartWithRecentWorkingDir", "Text", "Open last working directory on startup");
        DefaultCloneDestination = Add("lblDefaultCloneDestination", "Text", "Default clone destination");
        Browse = Add("btnDefaultDestinationBrowse", "Text", "Browse");
        DefaultPullAction = Add("lblDefaultPullAction", "Text", "Default pull action");
        QuickSearchTimeout = Add("lblQuickSearchTimeout", "Text", "Revision grid quick search timeout [ms]");
        Telemetry = Add("groupBoxTelemetry", "Text", "Telemetry");
        TelemetryEnabled = Add("chkTelemetry", "Text", "Yes, I allow telemetry!");
        TelemetryPrivacyLink = Add("llblTelemetryPrivacyLink", "Text", "Why and what is captured?");
    }

    public TranslatedText Title { get; }

    public TranslatedText OpenPullDialog { get; }

    public TranslatedText PullMerge { get; }

    public TranslatedText PullRebase { get; }

    public TranslatedText Fetch { get; }

    public TranslatedText FetchAll { get; }

    public TranslatedText FetchAndPruneAll { get; }

    public TranslatedText Performance { get; }

    public TranslatedText ShowGitStatusInToolbar { get; }

    public TranslatedText ShowGitStatusForArtificialCommits { get; }

    public TranslatedText ShowSubmoduleStatusInBrowse { get; }

    public TranslatedText ShowStashCountInBrowseWindow { get; }

    public TranslatedText ShowAheadBehindDataInBrowseWindow { get; }

    public TranslatedText CheckForUncommittedChangesInCheckoutBranch { get; }

    public TranslatedText CommitsLimit { get; }

    public TranslatedText Behaviour { get; }

    public TranslatedText CloseProcessDialog { get; }

    public TranslatedText ShowGitCommandLine { get; }

    public TranslatedText UseHistogramDiffAlgorithm { get; }

    public TranslatedText StashUntrackedFiles { get; }

    public TranslatedText UpdateModules { get; }

    public TranslatedText FollowRenamesInFileHistory { get; }

    public TranslatedText FollowRenamesInFileHistoryExact { get; }

    public TranslatedText StartWithRecentWorkingDir { get; }

    public TranslatedText DefaultCloneDestination { get; }

    public TranslatedText Browse { get; }

    public TranslatedText DefaultPullAction { get; }

    public TranslatedText QuickSearchTimeout { get; }

    public TranslatedText Telemetry { get; }

    public TranslatedText TelemetryEnabled { get; }

    public TranslatedText TelemetryPrivacyLink { get; }
}

/// <summary>A choice of a combo box: a value and its text.</summary>
public sealed record SettingChoice<T>(T Value, string Text)
{
    public override string ToString() => Text;
}

/// <summary>Port of <c>GeneralSettingsPage</c> (global settings).</summary>
public sealed partial class GeneralSettingsPageViewModel : SettingsPageWithServicesViewModel
{
    /// <summary>As <c>LlblTelemetryPrivacyLink_LinkClicked</c>.</summary>
    public const string TelemetryPrivacyUrl = "https://github.com/gitextensions/gitextensions/blob/master/setup/assets/PrivacyPolicy.md";

    private readonly IFileDialogService _fileDialogs;

    /// <param name="recentCloneDestinations">
    ///  The parent directories of the recent repositories (<c>cbDefaultCloneDestination</c>, filled from the history of the
    ///  local repositories).
    /// </param>
    public GeneralSettingsPageViewModel(GeneralSettingsPageStrings strings, IReadOnlyList<string> recentCloneDestinations, ISettingsPageServices services, IFileDialogService fileDialogs)
        : base(services)
    {
        Strings = strings;
        _fileDialogs = fileDialogs;
        CloneDestinations = recentCloneDestinations;
        PullActions =
        [
            new(GitPullAction.None, strings.OpenPullDialog.Text),
            new(GitPullAction.Merge, strings.PullMerge.Text),
            new(GitPullAction.Rebase, strings.PullRebase.Text),
            new(GitPullAction.Fetch, strings.Fetch.Text),
            new(GitPullAction.FetchAll, strings.FetchAll.Text),
            new(GitPullAction.FetchPruneAll, strings.FetchAndPruneAll.Text),
        ];
        DefaultPullAction = PullActions[0];
    }

    public GeneralSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "GeneralSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    public bool CanShowGitConsoleWindow => OperatingSystem.IsWindows();

    public IReadOnlyList<string> CloneDestinations { get; }

    /// <summary>The items of <c>cboDefaultPullAction</c>.</summary>
    public IReadOnlyList<SettingChoice<GitPullAction>> PullActions { get; }

    [ObservableProperty]
    public partial bool ShowGitStatusInToolbar { get; set; }

    [ObservableProperty]
    public partial bool ShowGitStatusForArtificialCommits { get; set; }

    [ObservableProperty]
    public partial bool ShowSubmoduleStatusInBrowse { get; set; }

    /// <summary>As <c>chkShowSubmoduleStatusInBrowse.Enabled</c>: with the number of changed files only.</summary>
    [ObservableProperty]
    public partial bool IsShowSubmoduleStatusInBrowseEnabled { get; private set; }

    [ObservableProperty]
    public partial bool ShowStashCountInBrowseWindow { get; set; }

    [ObservableProperty]
    public partial bool ShowAheadBehindDataInBrowseWindow { get; set; }

    [ObservableProperty]
    public partial bool CheckForUncommittedChangesInCheckoutBranch { get; set; }

    /// <summary>As <c>lblCommitsLimit</c>, the check box enabling <see cref="MaxCommits"/>.</summary>
    [ObservableProperty]
    public partial bool IsCommitsLimited { get; set; }

    [ObservableProperty]
    public partial decimal MaxCommits { get; set; }

    [ObservableProperty]
    public partial bool CloseProcessDialog { get; set; }

    [ObservableProperty]
    public partial bool ShowGitCommandLine { get; set; }

    [ObservableProperty]
    public partial bool UseHistogramDiffAlgorithm { get; set; }

    [ObservableProperty]
    public partial bool StashUntrackedFiles { get; set; }

    /// <summary>As the three-state <c>chkUpdateModules</c>: unset asks.</summary>
    [ObservableProperty]
    public partial bool? UpdateModules { get; set; }

    [ObservableProperty]
    public partial bool FollowRenamesInFileHistory { get; set; }

    [ObservableProperty]
    public partial bool FollowRenamesInFileHistoryExact { get; set; }

    [ObservableProperty]
    public partial bool StartWithRecentWorkingDir { get; set; }

    [ObservableProperty]
    public partial string DefaultCloneDestination { get; set; } = "";

    [ObservableProperty]
    public partial SettingChoice<GitPullAction>? DefaultPullAction { get; set; }

    [ObservableProperty]
    public partial decimal QuickSearchTimeout { get; set; } = 1000;

    [ObservableProperty]
    public partial bool TelemetryEnabled { get; set; }

    partial void OnShowGitStatusInToolbarChanged(bool value) => SetSubmoduleStatus();

    partial void OnShowGitStatusForArtificialCommitsChanged(bool value) => SetSubmoduleStatus();

    private void SetSubmoduleStatus()
    {
        IsShowSubmoduleStatusInBrowseEnabled = ShowGitStatusInToolbar || ShowGitStatusForArtificialCommits;
        ShowSubmoduleStatusInBrowse = IsShowSubmoduleStatusInBrowseEnabled && ShowSubmoduleStatusInBrowse;
    }

    /// <summary>As <c>DefaultCloneDestinationBrowseClick</c>.</summary>
    [RelayCommand]
    private async Task BrowseDefaultCloneDestinationAsync()
    {
        if (await _fileDialogs.PickFolderAsync(DefaultCloneDestination) is { } path)
        {
            DefaultCloneDestination = path;
        }
    }

    [RelayCommand]
    private void OpenTelemetryPrivacy() => Services.OpenUrl(TelemetryPrivacyUrl);

    protected override void SettingsToPage(SettingsSource? settings)
    {
        CheckForUncommittedChangesInCheckoutBranch = AppSettings.CheckForUncommittedChangesInCheckoutBranch;
        StartWithRecentWorkingDir = AppSettings.StartWithRecentWorkingDir;
        UseHistogramDiffAlgorithm = AppSettings.UseHistogramDiffAlgorithm;
        QuickSearchTimeout = Math.Clamp(AppSettings.RevisionGridQuickSearchTimeout, 100, 1000000);
        FollowRenamesInFileHistory = AppSettings.FollowRenamesInFileHistory;
        StashUntrackedFiles = AppSettings.IncludeUntrackedFilesInAutoStash;
        UpdateModules = AppSettings.UpdateSubmodulesOnCheckout;
        ShowStashCountInBrowseWindow = AppSettings.ShowStashCount;
        ShowAheadBehindDataInBrowseWindow = AppSettings.ShowAheadBehindData;
        ShowGitStatusInToolbar = AppSettings.ShowGitStatusInBrowseToolbar;
        ShowGitStatusForArtificialCommits = AppSettings.ShowGitStatusForArtificialCommits;
        ShowSubmoduleStatusInBrowse = AppSettings.ShowSubmoduleStatus;
        IsCommitsLimited = AppSettings.MaxRevisionGraphCommits != 0;
        MaxCommits = Math.Clamp(AppSettings.MaxRevisionGraphCommits, 0, 1000000);
        CloseProcessDialog = AppSettings.CloseProcessDialog;
        ShowGitCommandLine = AppSettings.ShowGitCommandLine;
        DefaultCloneDestination = AppSettings.DefaultCloneDestinationPath;
        GitPullAction pullAction = AppSettings.DefaultPullAction != GitPullAction.Default ? AppSettings.DefaultPullAction : GitPullAction.None;
        DefaultPullAction = PullActions.FirstOrDefault(a => a.Value == pullAction) ?? PullActions[0];
        FollowRenamesInFileHistoryExact = AppSettings.FollowRenamesInFileHistoryExactOnly;
        SetSubmoduleStatus();

        TelemetryEnabled = AppSettings.TelemetryEnabled ?? false;

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.CheckForUncommittedChangesInCheckoutBranch = CheckForUncommittedChangesInCheckoutBranch;
        AppSettings.StartWithRecentWorkingDir = StartWithRecentWorkingDir;
        AppSettings.UseHistogramDiffAlgorithm = UseHistogramDiffAlgorithm;
        AppSettings.IncludeUntrackedFilesInAutoStash = StashUntrackedFiles;
        AppSettings.UpdateSubmodulesOnCheckout = UpdateModules;
        AppSettings.FollowRenamesInFileHistory = FollowRenamesInFileHistory;
        AppSettings.ShowGitStatusInBrowseToolbar = ShowGitStatusInToolbar;
        AppSettings.ShowGitStatusForArtificialCommits = ShowGitStatusForArtificialCommits;
        AppSettings.CloseProcessDialog = CloseProcessDialog;
        if (CanShowGitConsoleWindow)
        {
            AppSettings.ShowGitCommandLine = ShowGitCommandLine;
        }

        AppSettings.MaxRevisionGraphCommits = IsCommitsLimited ? (int)MaxCommits : 0;
        AppSettings.RevisionGridQuickSearchTimeout = (int)QuickSearchTimeout;
        AppSettings.ShowStashCount = ShowStashCountInBrowseWindow;
        AppSettings.ShowAheadBehindData = ShowAheadBehindDataInBrowseWindow;
        AppSettings.ShowSubmoduleStatus = ShowSubmoduleStatusInBrowse;

        AppSettings.DefaultCloneDestinationPath = DefaultCloneDestination;
        AppSettings.DefaultPullAction = (DefaultPullAction ?? PullActions[0]).Value;
        AppSettings.FollowRenamesInFileHistoryExactOnly = FollowRenamesInFileHistoryExact;

        AppSettings.TelemetryEnabled = TelemetryEnabled;

        base.PageToSettings(settings);
    }
}
