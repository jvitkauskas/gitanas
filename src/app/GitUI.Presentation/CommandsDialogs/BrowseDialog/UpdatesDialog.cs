using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.BrowseDialog;

/// <summary>Strings of the check for updates dialog; ids match <c>FormUpdates</c>.</summary>
public sealed class UpdatesStrings : ViewStrings
{
    public UpdatesStrings()
        : base("FormUpdates")
    {
        Title = Add("$this", "Text", "Check for update");
        Searching = Add("UpdateLabel", "Text", "Searching for updates");
        NewVersionAvailable = Add("_newVersionAvailable", "Text", "There is a new version {0} of Gitanas available");
        NoUpdatesFound = Add("_noUpdatesFound", "Text", "No updates found");
        DownloadingUpdate = Add("_downloadingUpdate", "Text", "Downloading update...");
        ErrorHeading = Add("_errorHeading", "Text", "Download Failed");
        ErrorMessage = Add("_errorMessage", "Text", "Failed to download an update.");
        UpdateNow = Add("btnUpdateNow", "Text", "&Update Now");
        ChangeLog = Add("linkChangeLog", "Text", "Show release notes and change &log");
        DirectDownload = Add("linkDirectDownload", "Text", "&Direct Download");
        RequiredRuntime = Add("linkRequiredDotNetRuntime", "Text", "Required: .NET {0} Desktop Runtime {1} or later {2}.x");
        RequiredRuntimeToolTip = Add("linkRequiredDotNetRuntime", "ToolTipText", "Download latest .NET Desktop Runtime. See docs on how to install .NET runtime without administrative privileges.");
    }

    public TranslatedText Title { get; }

    public TranslatedText Searching { get; }

    public TranslatedText NewVersionAvailable { get; }

    public TranslatedText NoUpdatesFound { get; }

    public TranslatedText DownloadingUpdate { get; }

    public TranslatedText ErrorHeading { get; }

    public TranslatedText ErrorMessage { get; }

    public TranslatedText UpdateNow { get; }

    public TranslatedText ChangeLog { get; }

    public TranslatedText DirectDownload { get; }

    public TranslatedText RequiredRuntime { get; }

    public TranslatedText RequiredRuntimeToolTip { get; }
}

/// <summary>A newer version found by the update check.</summary>
/// <param name="NewVersion">The version of the update.</param>
/// <param name="UpdateUrl">The installer of the update, for the architecture of the OS.</param>
/// <param name="RequiredNetRuntimeVersion">The .NET Desktop Runtime the update requires, if known.</param>
public sealed record AvailableUpdate(string NewVersion, string UpdateUrl, Version? RequiredNetRuntimeVersion);

/// <summary>The text of the required .NET runtime link: the version is the link.</summary>
public sealed record RequiredRuntimeLink(string Before, string Link, string After);

/// <summary>Operations of the check for updates dialog that need the host.</summary>
public interface IUpdatesHost
{
    void OpenUrl(string url);

    /// <summary>
    ///  Downloads and starts the installer, then exits the application; reports the error message if the download fails.
    /// </summary>
    void DownloadAndInstall(string updateUrl, Action<string> reportDownloadFailure);
}

/// <summary>View model of the check for updates dialog (port of <c>FormUpdates</c>).</summary>
public sealed partial class UpdatesViewModel : DialogViewModel
{
    public const string ReleasesUrl = "https://github.com/jvitkauskas/gitanas/releases";
    public const string LocalRuntimeHelpUrl = "https://github.com/gitextensions/gitextensions/wiki/.NET-Desktop-Runtime";

    private readonly bool _isPortable;
    private readonly string _architecture;
    private readonly IReadOnlyList<Version> _installedNetRuntimes;
    private readonly IUpdatesHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private string _updateUrl = "";

    /// <param name="architecture">The architecture of the OS in lowercase (e.g. x64, arm64).</param>
    /// <param name="installedNetRuntimes">The installed .NET Desktop Runtime versions.</param>
    public UpdatesViewModel(
        UpdatesStrings strings,
        bool isPortable,
        string architecture,
        IReadOnlyList<Version> installedNetRuntimes,
        IUpdatesHost host,
        IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _isPortable = isPortable;
        _architecture = architecture;
        _installedNetRuntimes = installedNetRuntimes;
        _host = host;
        _messageBoxes = messageBoxes;
        Status = strings.Searching.Text;
        IsBusy = true;
    }

    public UpdatesStrings Strings { get; }

    [ObservableProperty]
    public partial string Status { get; private set; }

    /// <summary>Whether the search or the download is in progress.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUpdateNow))]
    public partial bool IsUpdateFound { get; private set; }

    [ObservableProperty]
    public partial bool IsChangeLogVisible { get; private set; }

    /// <summary>A portable installation is updated by downloading it; the installer is started otherwise.</summary>
    public bool CanUpdateNow => IsUpdateFound && !_isPortable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateNowCommand))]
    public partial bool IsDownloading { get; private set; }

    /// <summary>
    ///  The text of the required .NET runtime link before, of and after its link (as the <c>LinkArea</c> of
    ///  <c>FormUpdates.DisplayNetRuntimeLink</c>), or <see langword="null"/> if the installed runtime suffices.
    /// </summary>
    [ObservableProperty]
    public partial RequiredRuntimeLink? RequiredRuntimeText { get; private set; }

    public string RuntimeDownloadUrl { get; private set; } = "";

    /// <summary>Shows the result of the search: the update found, or <see langword="null"/> (also when it failed).</summary>
    public void ReportSearchResult(AvailableUpdate? update)
    {
        IsBusy = false;
        if (update is null)
        {
            _updateUrl = "";
            Status = Strings.NoUpdatesFound.Text;
            return;
        }

        _updateUrl = update.UpdateUrl;
        Status = string.Format(Strings.NewVersionAvailable.Text, update.NewVersion);
        IsChangeLogVisible = true;
        if (update.RequiredNetRuntimeVersion is { } required && IsRuntimeUpdateRequired(required, _installedNetRuntimes))
        {
            ShowRequiredRuntime(required);
        }

        IsUpdateFound = true;
    }

    /// <summary>Whether no installed runtime of the major version of <paramref name="required"/> is recent enough.</summary>
    public static bool IsRuntimeUpdateRequired(Version required, IEnumerable<Version> installed)
        => !installed.Any(version => version.Major == required.Major && version >= required);

    private void ShowRequiredRuntime(Version required)
    {
        string versionText1 = required.ToString(fieldCount: 2);
        string versionText2 = required.ToString(fieldCount: 3);
        string versionText3 = required.ToString(fieldCount: 1);
        string text = string.Format(Strings.RequiredRuntime.Text, versionText1, versionText2, versionText3);

        int start = text.IndexOf(versionText2, StringComparison.Ordinal);
        RequiredRuntimeText = start < 0
            ? new RequiredRuntimeLink(text, "", "")
            : new RequiredRuntimeLink(text[..start], versionText2, text[(start + versionText2.Length)..]);

        // The aka.ms/dotnet-core-applaunch URL expects the architecture and RID in lowercase (e.g. x64, arm64).
        RuntimeDownloadUrl = $"https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch={_architecture}&rid=win-{_architecture}&apphost_version={versionText2}&gui=true";
    }

    [RelayCommand]
    private void OpenChangeLog() => _host.OpenUrl(ReleasesUrl);

    [RelayCommand]
    private void DirectDownload() => _host.OpenUrl(_isPortable ? ReleasesUrl : _updateUrl);

    [RelayCommand]
    private void OpenRuntimeDownload()
    {
        if (!string.IsNullOrWhiteSpace(RuntimeDownloadUrl))
        {
            _host.OpenUrl(RuntimeDownloadUrl);
        }
    }

    [RelayCommand]
    private void OpenRuntimeHelp() => _host.OpenUrl(LocalRuntimeHelpUrl);

    private bool CanExecuteUpdateNow() => !IsDownloading;

    [RelayCommand(CanExecute = nameof(CanExecuteUpdateNow))]
    private void UpdateNow()
    {
        IsChangeLogVisible = false;
        IsBusy = true;
        IsDownloading = true;
        Status = Strings.DownloadingUpdate.Text;

        _host.DownloadAndInstall(
            _updateUrl,
            error => _messageBoxes.ShowError(Strings.ErrorMessage.Text + Environment.NewLine + error, Strings.ErrorHeading.Text));
    }
}
