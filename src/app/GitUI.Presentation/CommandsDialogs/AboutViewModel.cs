using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>
///  Actions of the about dialog that need the host application (browser, clipboard, other dialogs).
/// </summary>
public interface IAboutDialogHost
{
    void OpenUrl(string url);

    void ShowContributors();

    void CopyEnvironmentInfo();
}

/// <summary>
///  View model of the about dialog (port of <c>GitUI.CommandsDialogs.FormAbout</c>).
/// </summary>
public sealed partial class AboutViewModel : DialogViewModel
{
    public const string HomepageUrl = "https://github.com/jvitkauskas/gitanas";
    public const string IconsAuthorUrl = "http://p.yusukekamiyamane.com/";

    private readonly IAboutDialogHost _host;
    private readonly IReadOnlyList<string> _contributors;
    private readonly string _donationUrl;
    private readonly Random _random;
    private readonly string _thanksToContributorsText;

    public AboutViewModel(
        AboutStrings strings,
        string productName,
        string environmentInfo,
        IReadOnlyList<string> contributors,
        string donationUrl,
        IAboutDialogHost host,
        Random? random = null)
    {
        Strings = strings;
        ProductName = productName;
        EnvironmentInfo = environmentInfo;
        _contributors = contributors;
        _donationUrl = donationUrl;
        _host = host;
        _random = random ?? new Random();
        _thanksToContributorsText = string.Format(strings.ThanksToContributors.Text, contributors.Count);

        ThankNextContributor();
    }

    public AboutStrings Strings { get; }

    public string ProductName { get; }

    /// <summary>Environment details (versions, DPI...), one per line.</summary>
    public string EnvironmentInfo { get; }

    [ObservableProperty]
    public partial string ThanksToText { get; private set; } = "";

    /// <summary>
    ///  Shows another, randomly chosen contributor (the view calls this periodically).
    /// </summary>
    public void ThankNextContributor()
    {
        ThanksToText = _contributors.Count == 0
            ? _thanksToContributorsText
            : _thanksToContributorsText + _contributors[_random.Next(_contributors.Count)].Trim();
    }

    [RelayCommand]
    private void OpenHomepage() => _host.OpenUrl(HomepageUrl);

    [RelayCommand]
    private void OpenIconsAuthor() => _host.OpenUrl(IconsAuthorUrl);

    [RelayCommand]
    private void Donate() => _host.OpenUrl(_donationUrl);

    [RelayCommand]
    private void ShowContributors() => _host.ShowContributors();

    [RelayCommand]
    private void CopyEnvironmentInfo() => _host.CopyEnvironmentInfo();

    [RelayCommand]
    private void CloseDialog() => Close(accepted: true);
}
