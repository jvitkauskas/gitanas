using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the SSH settings; ids match <c>SshSettingsPage</c>.</summary>
public sealed class SshSettingsPageStrings : ViewStrings
{
    public SshSettingsPageStrings()
        : base("SshSettingsPage")
    {
        Title = Add("$this", "Text", "SSH");
        SshClient = Add("groupBox1", "Text", "Specify which ssh client to use");
        Putty = Add("Putty", "Text", "PuTTY");
        OpenSsh = Add("OpenSSH", "Text", "OpenSSH");
        Other = Add("Other", "Text", "Other ssh client");
        OtherSshBrowse = Add("OtherSshBrowse", "Text", "Browse");
        SshInfo = Add("label18", "Text", "OpenSSH is a commandline tool. PuTTY is more userfriendly to use for windows users,\nbut requires the PuTTY authentication client to run  in the background.\nWhen OpenSSH is used, command line dialogs are shown!\n");
        ConfigurePutty = Add("groupBox2", "Text", "Configure PuTTY");
        PlinkPath = Add("label15", "Text", "Path to plink");
        PlinkBrowse = Add("PlinkBrowse", "Text", "Browse");
        PuttygenPath = Add("label16", "Text", "Path to puttygen");
        PuttygenBrowse = Add("PuttygenBrowse", "Text", "Browse");
        PageantPath = Add("label17", "Text", "Path to pageant");
        PageantBrowse = Add("PageantBrowse", "Text", "Browse");
        AutostartPageant = Add("AutostartPageant", "Text", "Automatically start authentication client when a private key is configured for a remote");
        SelectFile = Add("_selectFile", "Text", "Select file", category: "CommonLogic");
    }

    public TranslatedText Title { get; }

    public TranslatedText SshClient { get; }

    public TranslatedText Putty { get; }

    public TranslatedText OpenSsh { get; }

    public TranslatedText Other { get; }

    public TranslatedText OtherSshBrowse { get; }

    public TranslatedText SshInfo { get; }

    public TranslatedText ConfigurePutty { get; }

    public TranslatedText PlinkPath { get; }

    public TranslatedText PlinkBrowse { get; }

    public TranslatedText PuttygenPath { get; }

    public TranslatedText PuttygenBrowse { get; }

    public TranslatedText PageantPath { get; }

    public TranslatedText PageantBrowse { get; }

    public TranslatedText AutostartPageant { get; }

    /// <summary>The title of the file pickers (<c>CommonLogic.SelectFile</c>).</summary>
    public TranslatedText SelectFile { get; }
}

/// <summary>What <see cref="SshSettingsPageViewModel"/> needs from the application.</summary>
public interface ISshSettingsPageHost
{
    /// <summary>The ssh client used by git (<c>AppSettings.SshPath</c>, stored in the registry): empty for OpenSSH.</summary>
    string SshPath { get; set; }

    /// <summary>The directories where PuTTY may be installed (<c>GetPuttyLocations</c>), in the order searched.</summary>
    IEnumerable<string> GetPuttyLocations();

    /// <summary>As <c>GitSshHelpers.SetGitSshEnvironmentVariable</c>: the ssh client used by git.</summary>
    void SetGitSshEnvironmentVariable(string path);
}

/// <summary>Port of <c>SshSettingsPage</c> (global settings): the ssh client and the paths of PuTTY.</summary>
public sealed partial class SshSettingsPageViewModel(SshSettingsPageStrings strings, ISshSettingsPageHost host, IFileDialogService fileDialogs) : SettingsPageViewModel
{
    public SshSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "SshSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>As <c>Putty</c>, checked by default; <c>groupBox2</c> is shown with it.</summary>
    [ObservableProperty]
    public partial bool IsPutty { get; set; } = true;

    /// <summary>Whether PuTTY is offered: it is Windows-only (docs/avalonia-port/CROSS-PLATFORM.md, phase 4).</summary>
    public bool CanUsePutty { get; } = OperatingSystem.IsWindows();

    [ObservableProperty]
    public partial bool IsOpenSsh { get; set; }

    [ObservableProperty]
    public partial bool IsOther { get; set; }

    [ObservableProperty]
    public partial string OtherSsh { get; set; } = "";

    [ObservableProperty]
    public partial string PlinkPath { get; set; } = "";

    [ObservableProperty]
    public partial string PuttygenPath { get; set; } = "";

    [ObservableProperty]
    public partial string PageantPath { get; set; } = "";

    [ObservableProperty]
    public partial bool AutostartPageant { get; set; } = true;

    protected override void SettingsToPage(SettingsSource? settings)
    {
        PlinkPath = AppSettings.Plink;
        PuttygenPath = AppSettings.Puttygen;
        PageantPath = AppSettings.Pageant;
        AutostartPageant = AppSettings.AutoStartPageant;

        // As GitSshHelpers.IsPlink for the ssh client used by git.
        string sshPath = host.SshPath;
        if (string.IsNullOrEmpty(sshPath))
        {
            IsOpenSsh = true;
        }
        else if (CanUsePutty && sshPath.EndsWith("plink.exe", StringComparison.CurrentCultureIgnoreCase))
        {
            IsPutty = true;
        }
        else
        {
            OtherSsh = sshPath;
            IsOther = true;
        }

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.Plink = PlinkPath;
        AppSettings.Puttygen = PuttygenPath;
        AppSettings.Pageant = PageantPath;
        AppSettings.AutoStartPageant = AutostartPageant;

        string path = IsOpenSsh ? "" : IsPutty ? PlinkPath : OtherSsh;

        // Set persistent settings as well as the env var used by Git.
        host.SetGitSshEnvironmentVariable(path);
        host.SshPath = path;

        base.PageToSettings(settings);
    }

    // The radio buttons are exclusive (as they are in their group box).
    partial void OnIsPuttyChanged(bool value)
    {
        if (value)
        {
            IsOpenSsh = false;
            IsOther = false;

            // As Putty_CheckedChanged.
            AutoFindPuttyPaths();
        }
    }

    partial void OnIsOpenSshChanged(bool value)
    {
        if (value)
        {
            IsPutty = false;
            IsOther = false;
        }
    }

    partial void OnIsOtherChanged(bool value)
    {
        if (value)
        {
            IsPutty = false;
            IsOpenSsh = false;
        }
    }

    /// <summary>As <c>AutoFindPuttyPaths</c>: the missing paths of PuTTY are searched where it may be installed.</summary>
    /// <returns>Whether all the paths are found.</returns>
    public bool AutoFindPuttyPaths()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        return host.GetPuttyLocations().Any(AutoFindPuttyPathsInDir);
    }

    private bool AutoFindPuttyPathsInDir(string installdir)
    {
        if (!installdir.EndsWith('\\'))
        {
            installdir += '\\';
        }

        if (!File.Exists(PlinkPath))
        {
            if (File.Exists(installdir + "plink.exe"))
            {
                PlinkPath = installdir + "plink.exe";
            }
            else if (File.Exists(installdir + "TortoisePlink.exe"))
            {
                PlinkPath = installdir + "TortoisePlink.exe";
            }
        }

        if (!File.Exists(PuttygenPath) && File.Exists(installdir + "puttygen.exe"))
        {
            PuttygenPath = installdir + "puttygen.exe";
        }

        if (!File.Exists(PageantPath) && File.Exists(installdir + "pageant.exe"))
        {
            PageantPath = installdir + "pageant.exe";
        }

        return File.Exists(PlinkPath) && File.Exists(PuttygenPath) && File.Exists(PageantPath);
    }

    /// <summary>As <c>OtherSshBrowse_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowseOtherSshAsync()
        => OtherSsh = await SelectFileAsync(OperatingSystem.IsWindows() ? "Executable file (*.exe)|*.exe" : "ssh|*", OtherSsh);

    /// <summary>As <c>PuttyBrowse_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowsePlinkAsync()
        => PlinkPath = await SelectFileAsync(
            "Plink (plink.exe)|plink.exe|TortoiseGitPLink (tortoisegitplink.exe)|tortoisegitplink.exe|TortoisePlink.exe (tortoiseplink.exe)|tortoiseplink.exe",
            PlinkPath);

    /// <summary>As <c>PuttygenBrowse_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowsePuttygenAsync()
        => PuttygenPath = await SelectFileAsync("PuttyGen (puttygen.exe)|puttygen.exe", PuttygenPath);

    /// <summary>As <c>PageantBrowse_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowsePageantAsync()
        => PageantPath = await SelectFileAsync("PAgeant (pageant.exe)|pageant.exe", PageantPath);

    // As CommonLogic.SelectFile: the previous path is kept when cancelled.
    private async Task<string> SelectFileAsync(string filter, string previous)
        => await fileDialogs.PickFileAsync(Strings.SelectFile.Text, FileDialogFilter.Parse(filter), ".") ?? previous;
}
