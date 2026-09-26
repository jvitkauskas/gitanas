using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the paths of git; ids match <c>GitSettingsPage</c>.</summary>
public sealed class GitSettingsPageStrings : ViewStrings
{
    public GitSettingsPageStrings()
        : base("GitSettingsPage")
    {
        Title = Add("$this", "Text", "Paths");
        EnvIsSetTo = Add("_envIsSetToString", "Text", "{0} is set to: {1}");
        EnvIsNotSet = Add("_envIsNotSetString", "Text", "{0} is not set.");
        Paths = Add("gbPaths", "Text", "Paths");
        PathsInfo = Add("label50", "Text", "Set the correct paths to Git for Windows. (WSL Git will automatically be used for WSL repositories.)");
        UnixPathsInfo = Add("_unixPathsInfo", "Text", "Set the command or path used to run Git.");
        UnixGitCommand = Add("_unixGitCommand", "Text", "Command used to run git");
        UnixGlobalConfigPath = Add("_unixGlobalConfigPath", "Text", "HOME identifies your home directory. Git uses it to locate ~/.gitconfig. GIT_CONFIG_GLOBAL can override the global configuration file.");
        GitCommand = Add("lblGitCommand", "Text", "Command used to run git (git.cmd or git.exe)");
        BrowseGitPath = Add("BrowseGitPath", "Text", "Browse");
        ShPath = Add("lblShPath", "Text", "Path to linux tools (sh). Leave empty when it is in the path.");
        BrowseLinuxToolsDir = Add("BrowseLinuxToolsDir", "Text", "Browse");
        DownloadGit = Add("downloadGitForWindows", "Text", "Download Git");
        Environment = Add("gbEnvironment", "Text", "Environment");
        GlobalConfigPath = Add("lblGlobalConfigPath", "Text", "By default, the global config file is located in the location stored in the environment variable %HOME%.\n(This can be overridden by setting %GIT_CONFIG_GLOBAL%.)\nIf empty, %HOME% will be set to %HOMEDRIVE%%HOMEPATH% by default.\nChange the default behaviour only if you experience problems.");
        HomeIsSetTo = Add("homeIsSetToLabel", "Text", "HOME is set to: {0}");
        ChangeHome = Add("ChangeHomeButton", "Text", "Change HOME");
    }

    public TranslatedText Title { get; }

    public TranslatedText EnvIsSetTo { get; }

    public TranslatedText EnvIsNotSet { get; }

    public TranslatedText Paths { get; }

    public TranslatedText PathsInfo { get; }

    public TranslatedText UnixPathsInfo { get; }

    public TranslatedText UnixGitCommand { get; }

    public TranslatedText UnixGlobalConfigPath { get; }

    public TranslatedText GitCommand { get; }

    public TranslatedText BrowseGitPath { get; }

    public TranslatedText ShPath { get; }

    public TranslatedText BrowseLinuxToolsDir { get; }

    public TranslatedText DownloadGit { get; }

    public TranslatedText Environment { get; }

    public TranslatedText GlobalConfigPath { get; }

    public TranslatedText HomeIsSetTo { get; }

    public TranslatedText ChangeHome { get; }
}

/// <summary>What <see cref="GitSettingsPageViewModel"/> needs from the application (<c>CheckSettingsLogic</c>, <c>FormFixHome</c>).</summary>
public interface IGitSettingsPageHost
{
    /// <summary>The command running git (<c>AppSettings.GitCommandValue</c>, stored in the registry).</summary>
    string GitCommandValue { get; set; }

    /// <summary>The directory of the linux tools (<c>AppSettings.LinuxToolsDir</c>).</summary>
    string LinuxToolsDir { get; set; }

    /// <summary>As <c>CheckSettingsLogic.SolveGitCommand</c>: finds a working git command, <paramref name="possibleNewPath"/> first.</summary>
    bool SolveGitCommand(string? possibleNewPath);

    /// <summary>As <c>CheckSettingsLogic.SolveLinuxToolsDir</c>.</summary>
    bool SolveLinuxToolsDir(string? possibleNewPath);

    /// <summary>
    ///  As the start of <c>GitSettingsPage.SettingsToPage</c>: sets the environment variables of git
    ///  (<c>EnvironmentConfiguration.SetEnvironmentVariables</c>) and returns <c>%GIT_CONFIG_GLOBAL%</c> and the HOME directory.
    /// </summary>
    (string? GitConfigGlobal, string HomeDir) GetGitEnvironment();

    /// <summary>Shows the dialog changing the HOME directory (<c>FormFixHome</c>).</summary>
    void ShowFixHome();

    void OpenUrl(string url);
}

/// <summary>Port of <c>GitSettingsPage</c> (global settings): the paths of git and of the linux tools, and the HOME directory.</summary>
public sealed partial class GitSettingsPageViewModel(GitSettingsPageStrings strings, IGitSettingsPageHost host, IFileDialogService fileDialogs) : SettingsPageViewModel
{
    /// <summary>The page of the manual linked by <c>downloadGitForWindows</c>.</summary>
    public const string DownloadGitUrl = "https://github.com/gitextensions/gitextensions/wiki/Application-Dependencies#git";

    private bool _showingPaths;

    public string PathsInfoText => (OperatingSystem.IsWindows() ? Strings.PathsInfo : Strings.UnixPathsInfo).Text;

    public string GitCommandText => (OperatingSystem.IsWindows() ? Strings.GitCommand : Strings.UnixGitCommand).AccessKeyText;

    public string GlobalConfigPathText => (OperatingSystem.IsWindows() ? Strings.GlobalConfigPath : Strings.UnixGlobalConfigPath).Text;

    private static string EnvironmentVariableText(string name) => OperatingSystem.IsWindows() ? $"%{name}%" : $"${name}";

    public GitSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "GitSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>As <c>GitPath</c>.</summary>
    [ObservableProperty]
    public partial string GitPath { get; set; } = "";

    /// <summary>Whether the directory of the linux tools is shown: the tools of Git for Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 3).</summary>
    public bool ShowLinuxToolsDir { get; } = OperatingSystem.IsWindows();

    /// <summary>As <c>LinuxToolsDir</c>.</summary>
    [ObservableProperty]
    public partial string LinuxToolsDir { get; set; } = "";

    /// <summary>As <c>homeIsSetToLabel</c>.</summary>
    [ObservableProperty]
    public partial string HomeIsSetTo { get; private set; } = "";

    public override void OnPageShown() => ShowPaths();

    protected override void SettingsToPage(SettingsSource? settings)
    {
        (string? gitConfigGlobal, string homeDir) = host.GetGitEnvironment();
        string envName = "GIT_CONFIG_GLOBAL";
        string? envValue = gitConfigGlobal;
        string additionalText = "";
        if (envValue is null)
        {
            additionalText = $"    ({string.Format(Strings.EnvIsNotSet.Text, EnvironmentVariableText(envName))})";
            envValue = homeDir;
            envName = "HOME";
        }

        HomeIsSetTo = string.Format(Strings.EnvIsSetTo.Text, EnvironmentVariableText(envName), envValue) + additionalText;

        ShowPaths();

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        host.GitCommandValue = GitPath;
        host.LinuxToolsDir = LinuxToolsDir;

        base.PageToSettings(settings);
    }

    private void ShowPaths()
    {
        _showingPaths = true;
        try
        {
            GitPath = host.GitCommandValue;
            LinuxToolsDir = host.LinuxToolsDir;
        }
        finally
        {
            _showingPaths = false;
        }
    }

    // As GitPath_TextChanged: a path typed or pasted is validated (and saved in the settings if it works). Only edits of the
    // user do it, not the paths shown from the settings.
    partial void OnGitPathChanged(string value)
    {
        if (!_showingPaths)
        {
            host.SolveGitCommand(value.Trim());
        }
    }

    // As LinuxToolsDir_TextChanged.
    partial void OnLinuxToolsDirChanged(string value)
    {
        if (!_showingPaths)
        {
            host.SolveLinuxToolsDir(value.Trim());
        }
    }

    /// <summary>As <c>BrowseGitPath_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowseGitPathAsync()
    {
        host.SolveGitCommand(GitPath.Trim());

        string? path = await fileDialogs.PickFileAsync(
            "",
            FileDialogFilter.Parse(OperatingSystem.IsWindows() ? "Git.cmd (git.cmd)|git.cmd|Git.exe (git.exe)|git.exe|Git (git)|git" : "Git (git)|git|All files (*)|*"),
            GetDirectory(host.GitCommandValue));
        if (path is not null)
        {
            GitPath = path;
        }
    }

    /// <summary>As <c>BrowseLinuxToolsDir_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowseLinuxToolsDirAsync()
    {
        host.SolveLinuxToolsDir(LinuxToolsDir.Trim());

        if (await fileDialogs.PickFolderAsync(host.LinuxToolsDir) is { } path)
        {
            LinuxToolsDir = path;
        }
    }

    /// <summary>As <c>downloadGitForWindows_LinkClicked</c>.</summary>
    [RelayCommand]
    private void DownloadGit() => host.OpenUrl(OperatingSystem.IsWindows() ? DownloadGitUrl : "https://git-scm.com/install/");

    /// <summary>As <c>ChangeHomeButton_Click</c>: the settings are saved, HOME changed, and the settings loaded again.</summary>
    [RelayCommand]
    private void ChangeHome()
    {
        PageHost?.SaveAll();
        host.ShowFixHome();
        PageHost?.LoadAll();
    }

    /// <summary>The directory of a file, as the <c>FileName</c> of an <c>OpenFileDialog</c> opens it.</summary>
    internal static string? GetDirectory(string? path)
    {
        try
        {
            return string.IsNullOrWhiteSpace(path) ? null : Path.GetDirectoryName(path);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
