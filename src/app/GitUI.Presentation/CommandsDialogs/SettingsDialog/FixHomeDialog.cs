using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog;

/// <summary>Strings of the HOME directory dialog; ids match <c>FormFixHome</c> and its <c>FolderBrowserButton</c>.</summary>
public sealed class FixHomeStrings : ViewStrings
{
    public FixHomeStrings()
        : base("FormFixHome")
    {
        Title = Add("$this", "Text", "Home");
        GitGlobalConfigNotFound = Add("_gitGlobalConfigNotFound", "Text",
            "The environment variable HOME does not point to a directory that contains the global git config file:" + System.Environment.NewLine +
            "\" {0} \"" + System.Environment.NewLine + System.Environment.NewLine + "Do you want Git Extensions to help locate the correct folder?");
        GitGlobalConfigNotFoundCaption = Add("_gitGlobalConfigNotFoundCaption", "Text", "Global config");
        Explanation = Add("label51", "Text", "The global config file located in the location stored environment variable %HOME%. \nBy default %HOME% will be set to %HOMEDRIVE%%HOMEPATH% if empty. \nChange the default behaviour only if you experience problems.");
        UnixExplanation = Add("_unixExplanation", "Text", "By default, HOME uses your user home directory. Choose a different directory only if Git is using the wrong home directory.");
        UnixGitConfigFoundHome = Add("_unixGitConfigFoundHome", "Text", "Located .gitconfig in $HOME ({0}). This setting has been chosen automatically.");
        Environment = Add("groupBox8", "Text", "Environment");
        DefaultHome = Add("defaultHome", "Text", "&Use default for HOME");
        UserProfileHome = Add("userprofileHome", "Text", "&Set HOME to USERPROFILE");
        OtherHome = Add("otherHome", "Text", "&Other");
        Ok = Add("ok", "Text", "OK");
        GitConfigFoundHome = Add("_gitconfigFoundHome", "Text", "Located .gitconfig in %HOME% ({0}). This setting has been chosen automatically.");
        GitConfigFoundHomeDrive = Add("_gitconfigFoundHomedrive", "Text", "Located .gitconfig in %HOMEDRIVE%%HOMEPATH% ({0}). This setting has been chosen automatically.");
        GitConfigFoundUserProfile = Add("_gitconfigFoundUserprofile", "Text", "Located .gitconfig in %USERPROFILE% ({0}). This setting has been chosen automatically.");
        GitConfigFoundPersonalFolder = Add("_gitconfigFoundPersonalFolder", "Text", "Located .gitconfig in personal folder ({0}). This setting has been chosen automatically.");
        NoHomeDirectorySpecified = Add("_noHomeDirectorySpecified", "Text", "Please enter a HOME directory.");
        HomeNotAccessible = Add("_homeNotAccessible", "Text", "The environment variable HOME points to a directory that is not accessible:\n\"{0}\"");
        Browse = Add("buttonBrowse", "Text", "&Browse...", category: "FolderBrowserButton");
    }

    /// <summary>The caption of the information messages, which <c>FormFixHome</c> does not translate.</summary>
    public const string InformationCaption = "Information";

    public TranslatedText Title { get; }

    /// <summary>The question at startup when HOME has no git configuration (<c>ShowIfUserWant</c>).</summary>
    public TranslatedText GitGlobalConfigNotFound { get; }

    public TranslatedText GitGlobalConfigNotFoundCaption { get; }

    public TranslatedText Explanation { get; }

    public TranslatedText UnixExplanation { get; }

    public TranslatedText UnixGitConfigFoundHome { get; }

    public TranslatedText Environment { get; }

    public TranslatedText DefaultHome { get; }

    public TranslatedText UserProfileHome { get; }

    public TranslatedText OtherHome { get; }

    public TranslatedText Ok { get; }

    public TranslatedText GitConfigFoundHome { get; }

    public TranslatedText GitConfigFoundHomeDrive { get; }

    public TranslatedText GitConfigFoundUserProfile { get; }

    public TranslatedText GitConfigFoundPersonalFolder { get; }

    public TranslatedText NoHomeDirectorySpecified { get; }

    public TranslatedText HomeNotAccessible { get; }

    public TranslatedText Browse { get; }
}

/// <summary>The HOME settings and the candidate directories the dialog starts from (as <c>FormFixHome.LoadSettings</c>).</summary>
/// <param name="CustomHomeDir"><c>AppSettings.CustomHomeDir</c>.</param>
/// <param name="UserProfileHomeDir"><c>AppSettings.UserProfileHomeDir</c>.</param>
/// <param name="DefaultHomeDir">The HOME directory used by default.</param>
/// <param name="UserHome">The HOME environment variable of the user.</param>
/// <param name="HomeDrivePath">%HOMEDRIVE%%HOMEPATH%.</param>
/// <param name="UserProfile">%USERPROFILE%.</param>
/// <param name="PersonalFolder">The personal (documents) folder.</param>
public sealed record FixHomeEnvironment(
    string CustomHomeDir,
    bool UserProfileHomeDir,
    string DefaultHomeDir,
    string? UserHome,
    string? HomeDrivePath,
    string? UserProfile,
    string? PersonalFolder);

/// <summary>Operations of the HOME directory dialog that need the host (file system, settings, environment).</summary>
public interface IFixHomeHost
{
    /// <summary>Whether <paramref name="path"/> contains a global git config (as <c>FormFixHome.HasGlobalGitConfig</c>).</summary>
    bool HasGlobalGitConfig(string? path);

    /// <summary>Saves the HOME settings, applies them to the environment and returns the resulting HOME directory.</summary>
    string? ApplyHome(string customHomeDir, bool userProfileHomeDir);

    bool DirectoryExists(string? path);
}

/// <summary>View model of the HOME directory dialog (port of <c>FormFixHome</c>).</summary>
public sealed partial class FixHomeViewModel : DialogViewModel
{
    private readonly FixHomeEnvironment _environment;
    private readonly IFixHomeHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private readonly string _errorCaption;

    public FixHomeViewModel(
        FixHomeStrings strings,
        FixHomeEnvironment environment,
        string errorCaption,
        IFixHomeHost host,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        _environment = environment;
        _errorCaption = errorCaption;
        _host = host;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;

        DefaultHomeText = $"{strings.DefaultHome.AccessKeyText} ({environment.DefaultHomeDir})";
        UserProfileHomeText = $"{strings.UserProfileHome.AccessKeyText} ({environment.UserProfile})";

        if (!string.IsNullOrEmpty(environment.CustomHomeDir))
        {
            IsOtherHome = true;
            OtherHomeDir = environment.CustomHomeDir;
        }
        else if (CanUseUserProfileHome && environment.UserProfileHomeDir)
        {
            IsUserProfileHome = true;
        }
        else
        {
            IsDefaultHome = true;
        }
    }

    public FixHomeStrings Strings { get; }

    public bool CanUseUserProfileHome => OperatingSystem.IsWindows();

    public string ExplanationText => (CanUseUserProfileHome ? Strings.Explanation : Strings.UnixExplanation).Text;

    public string DefaultHomeText { get; }

    public string UserProfileHomeText { get; }

    [ObservableProperty]
    public partial bool IsDefaultHome { get; set; }

    [ObservableProperty]
    public partial bool IsUserProfileHome { get; set; }

    [ObservableProperty]
    public partial bool IsOtherHome { get; set; }

    [ObservableProperty]
    public partial string OtherHomeDir { get; set; } = "";

    /// <summary>
    ///  Chooses the first candidate directory with a global git config and tells the user
    ///  (as <c>FormFixHome.LoadSettings</c>, when the dialog is shown).
    /// </summary>
    public void SelectLocatedGitConfig()
    {
        if (TryLocate(_environment.UserHome, CanUseUserProfileHome ? Strings.GitConfigFoundHome : Strings.UnixGitConfigFoundHome))
        {
            Choose(defaultHome: true);
        }
        else if (CanUseUserProfileHome && TryLocate(_environment.HomeDrivePath, Strings.GitConfigFoundHomeDrive))
        {
            Choose(defaultHome: true);
        }
        else if (CanUseUserProfileHome && TryLocate(_environment.UserProfile, Strings.GitConfigFoundUserProfile))
        {
            Choose(userProfileHome: true);
        }
        else if (TryLocate(_environment.PersonalFolder, Strings.GitConfigFoundPersonalFolder))
        {
            Choose(otherHome: true);
            OtherHomeDir = _environment.PersonalFolder ?? "";
        }

        bool TryLocate(string? path, TranslatedText message)
        {
            try
            {
                if (!_host.HasGlobalGitConfig(path))
                {
                    return false;
                }
            }
            catch
            {
                // Could be a security issue: let the user choose manually.
                return false;
            }

            _messageBoxes.ShowInformation(string.Format(message.Text, path), FixHomeStrings.InformationCaption);
            return true;
        }
    }

    private void Choose(bool defaultHome = false, bool userProfileHome = false, bool otherHome = false)
    {
        IsDefaultHome = defaultHome;
        IsUserProfileHome = userProfileHome;
        IsOtherHome = otherHome;
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        string? folder = await _fileDialogs.PickFolderAsync(CanUseUserProfileHome ? _environment.UserProfile : _environment.DefaultHomeDir);
        if (folder is not null)
        {
            OtherHomeDir = folder;
        }
    }

    [RelayCommand]
    private void Ok()
    {
        string customHomeDir = "";
        if (IsOtherHome)
        {
            if (string.IsNullOrEmpty(OtherHomeDir))
            {
                _messageBoxes.ShowError(Strings.NoHomeDirectorySpecified.Text, _errorCaption);
                return;
            }

            customHomeDir = OtherHomeDir;
        }

        string? home = _host.ApplyHome(customHomeDir, IsUserProfileHome);
        if (string.IsNullOrEmpty(home) || !_host.DirectoryExists(home))
        {
            _messageBoxes.ShowError(string.Format(Strings.HomeNotAccessible.Text, home), _errorCaption);
            return;
        }

        Close(accepted: true);
    }
}
