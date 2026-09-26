using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the checklist; ids match <c>ChecklistSettingsPage</c>.</summary>
public sealed class ChecklistSettingsPageStrings : ViewStrings
{
    public ChecklistSettingsPageStrings()
        : base("ChecklistSettingsPage")
    {
        Title = Add("$this", "Text", "Checklist");
        Intro = Add("label11", "Text", "The checklist below validates the basic settings needed for Git Extensions to work properly.");
        CheckAtStartup = Add("CheckAtStartup", "Text", "Check settings at startup (disables automatically if all settings are correct)");
        Rescan = Add("Rescan", "Text", "Save and rescan");
        Repair = Add("GitFound_Fix", "Text", "Repair");
        WrongGitVersion = Add("_wrongGitVersion", "Text", "Git found but version {0} is not supported. Upgrade to version {1} or later");
        NotRecommendedGitVersion = Add("_notRecommendedGitVersion", "Text", "Git found but version {0} is older than recommended. Upgrade to version {1} or later");
        GitVersionFound = Add("_gitVersionFound", "Text", "Git {0} is found on your computer.");
        SshClientNotFound = Add("_sshClientNotFound", "Text", "SSH client not found: {0}.");
        OtherSshClient = Add("_otherSshClient", "Text", "Other SSH client configured: {0}.");
        LinuxToolsSshNotFound = Add("_linuxToolsSshNotFound", "Text", "Linux tools (sh) not found. To solve this problem you can set the correct path in settings.");
        SolveGitCommandFailedCaption = Add("_solveGitCommandFailedCaption", "Text", "Locate git");
        GitCanBeRun = Add("_gitCanBeRun", "Text", "Git can be run using: {0}");
        GitCanBeRunCaption = Add("_gitCanBeRunCaption", "Text", "Locate git");
        SolveGitCommandFailed = Add("_solveGitCommandFailed", "Text", "The command to run git could not be determined automatically.\nPlease make sure that Git for Windows is installed or set the correct command manually.");
        ShellExtRegistered = Add("_shellExtRegistered", "Text", "Shell extensions registered properly.");
        ShellExtNoInstalled = Add("_shellExtNoInstalled", "Text", "Shell extensions are not installed. Run the installer to install the shell extensions.");
        ShellExtNeedsToBeRegistered = Add("_shellExtNeedsToBeRegistered", "Text", "{0} needs to be registered in order to use the shell extensions.");
        SolveGitCommandFailedUnix = Add("_solveGitCommandFailedUnix", "Text", "The command to run git could not be determined automatically.\nPlease install Git using your package manager or set the correct command manually.");
        RegistryKeyGitExtensionsMissing = Add("_registryKeyGitExtensionsMissing", "Text", @"Registry entry missing [Software\GitExtensions\InstallDir].");
        RegistryKeyGitExtensionsFaulty = Add("_registryKeyGitExtensionsFaulty", "Text", @"Invalid installation directory stored in [Software\GitExtensions\InstallDir].");
        RegistryKeyGitExtensionsCorrect = Add("_registryKeyGitExtensionsCorrect", "Text", "Git Extensions is properly registered.");
        PlinkPuttyGenPageantNotFound = Add("_plinkputtyGenpageantNotFound", "Text", "PuTTY is configured as SSH client but cannot find plink.exe, puttygen.exe or pageant.exe.");
        PuttyConfigured = Add("_puttyConfigured", "Text", "SSH client PuTTY is configured properly.");
        OpensshUsed = Add("_opensshUsed", "Text", "Default SSH client, OpenSSH, will be used. (commandline window will appear on pull, push and clone operations)");
        LanguageConfigured = Add("_languageConfigured", "Text", "The configured language is {0}.");
        NoLanguageConfigured = Add("_noLanguageConfigured", "Text", "There is no language configured for Git Extensions.");
        NoEmailSet = Add("_noEmailSet", "Text", "You need to configure a username and an email address.");
        EmailSet = Add("_emailSet", "Text", "A username and an email address are configured.");
        MergeToolXConfiguredNeedsCmd = Add("_mergeToolXConfiguredNeedsCmd", "Text", "{0} is configured as mergetool, this is a custom mergetool and needs a custom cmd to be configured.");
        MergeToolXConfigured = Add("_mergeToolXConfigured", "Text", "There is a mergetool configured: {0}");
        LinuxToolsSshFound = Add("_linuxToolsSshFound", "Text", "Linux tools (sh) found on your computer.");
        GitNotFound = Add("_gitNotFound", "Text", "Git not found. To solve this problem you can set the correct path in settings.");
        AdviceDiffToolConfiguration = Add("_adviceDiffToolConfiguration", "Text", "You should configure a diff tool to show file diff in external program.");
        DiffToolXConfigured = Add("_diffToolXConfigured", "Text", "There is a difftool configured: {0}");
        ConfigureMergeTool = Add("_configureMergeTool", "Text", "You need to configure merge tool in order to solve merge conflicts.");
        PuttyFoundAuto = Add("_puttyFoundAuto", "Text", "All paths needed for PuTTY could be automatically found and are set.");
        LinuxToolsShNotFound = Add("_linuxToolsShNotFound", "Text", "The path to linux tools (sh) could not be found automatically.\nPlease make sure there are linux tools installed (through Git for Windows or cygwin) or set the correct path manually.");
        LinuxToolsShNotFoundCaption = Add("_linuxToolsShNotFoundCaption", "Text", "Locate linux tools");
        ShCanBeRun = Add("_shCanBeRun", "Text", "Command sh can be run using: {0}sh");
        ShCanBeRunCaption = Add("_shCanBeRunCaption", "Text", "Locate linux tools");
        GcmDetectedCaption = Add("_gcmDetectedCaption", "Text", "Obsolete git-credential-winstore.exe detected");
    }

    public TranslatedText Title { get; }

    public TranslatedText Intro { get; }

    public TranslatedText CheckAtStartup { get; }

    public TranslatedText Rescan { get; }

    public TranslatedText Repair { get; }

    public TranslatedText WrongGitVersion { get; }

    public TranslatedText NotRecommendedGitVersion { get; }

    public TranslatedText GitVersionFound { get; }

    public TranslatedText SshClientNotFound { get; }

    public TranslatedText OtherSshClient { get; }

    public TranslatedText LinuxToolsSshNotFound { get; }

    public TranslatedText SolveGitCommandFailedCaption { get; }

    public TranslatedText GitCanBeRun { get; }

    public TranslatedText GitCanBeRunCaption { get; }

    public TranslatedText SolveGitCommandFailed { get; }

    public TranslatedText SolveGitCommandFailedUnix { get; }

    public TranslatedText ShellExtRegistered { get; }

    public TranslatedText ShellExtNoInstalled { get; }

    public TranslatedText ShellExtNeedsToBeRegistered { get; }

    public TranslatedText RegistryKeyGitExtensionsMissing { get; }

    public TranslatedText RegistryKeyGitExtensionsFaulty { get; }

    public TranslatedText RegistryKeyGitExtensionsCorrect { get; }

    public TranslatedText PlinkPuttyGenPageantNotFound { get; }

    public TranslatedText PuttyConfigured { get; }

    public TranslatedText OpensshUsed { get; }

    public TranslatedText LanguageConfigured { get; }

    public TranslatedText NoLanguageConfigured { get; }

    public TranslatedText NoEmailSet { get; }

    public TranslatedText EmailSet { get; }

    public TranslatedText MergeToolXConfiguredNeedsCmd { get; }

    public TranslatedText MergeToolXConfigured { get; }

    public TranslatedText LinuxToolsSshFound { get; }

    public TranslatedText GitNotFound { get; }

    public TranslatedText AdviceDiffToolConfiguration { get; }

    public TranslatedText DiffToolXConfigured { get; }

    public TranslatedText ConfigureMergeTool { get; }

    public TranslatedText PuttyFoundAuto { get; }

    public TranslatedText LinuxToolsShNotFound { get; }

    public TranslatedText LinuxToolsShNotFoundCaption { get; }

    public TranslatedText ShCanBeRun { get; }

    public TranslatedText ShCanBeRunCaption { get; }

    public TranslatedText GcmDetectedCaption { get; }
}

/// <summary>The checks of the checklist, in the order of the page (a button and its Repair button each).</summary>
public enum ChecklistCheck
{
    GitFound,
    UserNameSet,
    MergeTool,
    DiffTool,
    ShellExtensionsRegistered,
    GitBinFound,
    GitExtensionsInstall,
    SshConfig,
    Translation,
    GcmDetected,
}

/// <summary>The result of a check: as <c>RenderSettingSet</c> (green), <c>RenderSettingUnset</c> (red), <c>RenderSettingNotRecommended</c> (yellow).</summary>
public enum ChecklistState
{
    Set,
    Unset,
    NotRecommended,
}

/// <summary>A result shown by the checklist: its state and its text.</summary>
public sealed record ChecklistResult(ChecklistState State, string Text)
{
    /// <summary>Whether the check passes (<see langword="true"/> for a set setting).</summary>
    public bool IsValid => State == ChecklistState.Set;
}

/// <summary>A check of the checklist: its button, coloured by the state, and its Repair button shown when not set.</summary>
public sealed partial class ChecklistItem(ChecklistCheck check) : ObservableObject
{
    public ChecklistCheck Check { get; } = check;

    /// <summary>Whether the check was done (the buttons are hidden before, and for the checks of another platform).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepairVisible))]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepairVisible), nameof(IsSet), nameof(IsUnset), nameof(IsNotRecommended))]
    public partial ChecklistState State { get; set; }

    public bool IsSet => State == ChecklistState.Set;

    public bool IsUnset => State == ChecklistState.Unset;

    public bool IsNotRecommended => State == ChecklistState.NotRecommended;

    [ObservableProperty]
    public partial string Text { get; set; } = "";

    public bool IsRepairVisible => IsVisible && State != ChecklistState.Set;

    internal void Show(ChecklistResult? result)
    {
        IsVisible = result is not null;
        if (result is not null)
        {
            State = result.State;
            Text = result.Text;
        }
    }
}

/// <summary>The checks and the repairs of the checklist, done by the application (<c>CheckSettingsLogic</c> and others).</summary>
public interface IChecklistSettingsHost
{
    /// <summary>Starts a round of checks (as the <c>DiffMergeToolConfigurationManager</c> created by <c>CheckSettings</c>).</summary>
    void BeginChecks();

    /// <summary>Checks a setting; null when not checked (hidden), e.g. a Windows check on another platform or no obsolete GCM.</summary>
    ChecklistResult? Check(ChecklistCheck check);

    /// <summary>
    ///  Whether the check counts for the result without being shown (<c>CheckEditorTool</c>: the global editor is set).
    /// </summary>
    bool IsEditorConfigured();

    /// <summary>Repairs the setting of a check (the click of its button or its Repair button).</summary>
    void Repair(ChecklistCheck check, IChecklistActions actions);

    /// <summary>Shows the error of a check.</summary>
    void ShowError(string text);

    /// <summary>As <c>AppSettings.CheckSettings</c>.</summary>
    bool CheckAtStartup { get; set; }
}

/// <summary>What a repair can do on the page and the dialog.</summary>
public interface IChecklistActions
{
    /// <summary>As <c>SaveAndRescan_Click</c>: saves and reloads all the pages, checks again.</summary>
    void SaveAndRescan();

    /// <summary>Checks again.</summary>
    void Rescan();

    void LoadAll();

    void GotoPage(string pageName);

    /// <summary>As <c>SshSettingsPage.AutoFindPuttyPaths</c>: whether the paths of PuTTY were found and set.</summary>
    bool AutoFindPuttyPaths();
}

/// <summary>
///  Port of <c>ChecklistSettingsPage</c> (the root of the Git Extensions settings, saved instantly): the checks of the basic
///  settings, each with a repair.
/// </summary>
public sealed partial class ChecklistSettingsPageViewModel : SettingsPageViewModel, IChecklistActions
{
    private readonly IChecklistSettingsHost _host;
    private bool _loadingCheckAtStartup;

    public ChecklistSettingsPageViewModel(ChecklistSettingsPageStrings strings, IChecklistSettingsHost host)
    {
        Strings = strings;
        _host = host;
        Items = [.. Enum.GetValues<ChecklistCheck>().Select(check => new ChecklistItem(check))];
    }

    public ChecklistSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "ChecklistSettingsPage";

    public override bool IsInstantSavePage => true;

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    public IReadOnlyList<ChecklistItem> Items { get; }

    /// <summary>As <c>CheckAtStartup</c>.</summary>
    [ObservableProperty]
    public partial bool CheckAtStartup { get; set; }

    /// <summary>The paths of PuTTY found by the SSH page (its <c>AutoFindPuttyPaths</c>), set when both pages are added.</summary>
    public Func<bool>? PuttyPathsFinder { get; set; }

    public override void OnPageShown() => CheckSettings();

    /// <summary>As <c>CheckSettings</c>: all the checks; returns whether all pass.</summary>
    public bool CheckSettings()
    {
        _host.BeginChecks();
        bool isValid = true;
        foreach (ChecklistItem item in Items)
        {
            // As PerformChecks: a failing check is reported and skipped.
            try
            {
                ChecklistResult? result = _host.Check(item.Check);
                item.Show(result);
                isValid &= result?.IsValid ?? true;
            }
            catch (Exception ex)
            {
                _host.ShowError(ex.Message);
            }

            // CheckEditorTool, not shown, after CheckGlobalUserSettingsValid.
            if (item.Check == ChecklistCheck.UserNameSet)
            {
                isValid &= _host.IsEditorConfigured();
            }
        }

        // As IsCheckAtStartupChecked: the check at startup turns off once all is valid.
        bool checkAtStartup = _host.CheckAtStartup;
        if (isValid && checkAtStartup)
        {
            _host.CheckAtStartup = false;
            checkAtStartup = false;
        }

        _loadingCheckAtStartup = true;
        try
        {
            CheckAtStartup = checkAtStartup;
        }
        finally
        {
            _loadingCheckAtStartup = false;
        }

        return isValid;
    }

    partial void OnCheckAtStartupChanged(bool value)
    {
        if (!_loadingCheckAtStartup)
        {
            _host.CheckAtStartup = value;
        }
    }

    /// <summary>The click of the Repair button of a check.</summary>
    [RelayCommand]
    private void Repair(ChecklistItem item) => _host.Repair(item.Check, this);

    /// <summary>The click of the button of a check, a repair too, but for the obsolete credential helper (a link to its fix only).</summary>
    [RelayCommand]
    private void Click(ChecklistItem item)
    {
        if (item.Check != ChecklistCheck.GcmDetected)
        {
            _host.Repair(item.Check, this);
        }
    }

    /// <summary>As <c>SaveAndRescan_Click</c>.</summary>
    [RelayCommand]
    public void SaveAndRescan()
    {
        PageHost?.SaveAll();
        PageHost?.LoadAll();
        CheckSettings();
    }

    void IChecklistActions.Rescan() => CheckSettings();

    void IChecklistActions.LoadAll() => PageHost?.LoadAll();

    void IChecklistActions.GotoPage(string pageName) => PageHost?.GotoPage(pageName);

    bool IChecklistActions.AutoFindPuttyPaths() => PuttyPathsFinder?.Invoke() ?? false;

    protected override void SettingsToPage(SettingsSource? settings)
    {
        _loadingCheckAtStartup = true;
        try
        {
            CheckAtStartup = _host.CheckAtStartup;
        }
        finally
        {
            _loadingCheckAtStartup = false;
        }

        base.SettingsToPage(settings);
    }
}
