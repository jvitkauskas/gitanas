using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands.Settings;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the build server integration settings; ids match <c>BuildServerIntegrationSettingsPage</c>.</summary>
public sealed class BuildServerIntegrationSettingsPageStrings : ViewStrings
{
    public BuildServerIntegrationSettingsPageStrings()
        : base("BuildServerIntegrationSettingsPage")
    {
        Title = Add("$this", "Text", "Build server integration");
        None = Add("_noneItem", "Text", "None");
        Info = Add("labelBuildServerSettingsInfo", "Text", "Gitanas can integrate with build servers to supply per-commit Continuous Integration information.");
        EnableIntegration = Add("checkBoxEnableBuildServerIntegration", "Text", "Enable build server integration");
        ShowBuildResultPage = Add("checkBoxShowBuildResultPage", "Text", "Show build result page");
        BuildServerType = Add("labelBuildServerType", "Text", "Build server type");
    }

    public TranslatedText Title { get; }

    public TranslatedText None { get; }

    public TranslatedText Info { get; }

    public TranslatedText EnableIntegration { get; }

    public TranslatedText ShowBuildResultPage { get; }

    public TranslatedText BuildServerType { get; }
}

/// <summary>
///  The settings control of a build server plugin (an <c>IBuildServerSettingsUserControl</c>, a WinForms control embedded in
///  the page as a child window).
/// </summary>
public interface IBuildServerSettingsControl : IEmbeddedNativeView, IDisposable
{
    /// <summary>The height the control needs, in device-independent pixels.</summary>
    double PreferredHeight { get; }

    void LoadSettings(SettingsSource buildServerConfig);

    void SaveSettings(SettingsSource buildServerConfig);
}

/// <summary>
///  The settings of a build server declared by its plugin (an <c>IBuildServerSettingsProvider</c>, plugin API v2), shown as the
///  settings of the plugins (<see cref="Page"/>), rather than by a WinForms control.
/// </summary>
public sealed class BuildServerPluginSettings
{
    private readonly Func<SettingsSource, SettingsSource> _getSettingsToLoad;
    private readonly Func<SettingsSource, string?> _validate;
    private readonly Action<string> _showError;

    /// <param name="page">The rows of the settings.</param>
    /// <param name="getSettingsToLoad">The settings to load the rows from, for the settings of the build server (with its suggested values).</param>
    /// <param name="validate">Checks the values being saved: the error to show, in which case nothing is saved.</param>
    /// <param name="showError">Shows the error of <paramref name="validate"/>.</param>
    public BuildServerPluginSettings(PluginSettingsPageViewModel page, Func<SettingsSource, SettingsSource> getSettingsToLoad, Func<SettingsSource, string?> validate, Action<string> showError)
    {
        Page = page;
        _getSettingsToLoad = getSettingsToLoad;
        _validate = validate;
        _showError = showError;
    }

    public PluginSettingsPageViewModel Page { get; }

    public void LoadSettings(SettingsSource buildServerConfig) => Page.LoadValues(_getSettingsToLoad(buildServerConfig));

    /// <summary>Saves the settings, unless they are invalid (then the error is shown); returns whether they were saved.</summary>
    public bool SaveSettings(SettingsSource buildServerConfig)
    {
        if (_validate(Page.GetEditedValues()) is { } error)
        {
            _showError(error);
            return false;
        }

        Page.SaveValues(buildServerConfig);
        return true;
    }
}

/// <summary>What <see cref="BuildServerIntegrationSettingsPageViewModel"/> needs from the application (the build server plugins).</summary>
public interface IBuildServerIntegrationSettingsPageHost
{
    /// <summary>The types of the build server plugins (with the reason when one cannot be loaded), found in the background.</summary>
    Task<IReadOnlyList<string>> GetBuildServerTypesAsync();

    /// <summary>
    ///  As <c>CreateBuildServerSettingsUserControl</c>: the settings control of the build server type, initialized with the
    ///  repository; <see langword="null"/> without repository or plugin.
    /// </summary>
    IBuildServerSettingsControl? CreateSettingsControl(string buildServerType);

    /// <summary>
    ///  Plugin API v2: the settings the plugin of the build server type declares, initialized with the repository; used rather
    ///  than <see cref="CreateSettingsControl"/>. <see langword="null"/> without repository, or when the plugin has none.
    /// </summary>
    BuildServerPluginSettings? CreatePluginSettings(string buildServerType) => null;
}

/// <summary>
///  Port of <c>BuildServerIntegrationSettingsPage</c> (a <c>DistributedSettingsPage</c>): the build server of the repository,
///  with the settings control of its plugin.
/// </summary>
public sealed partial class BuildServerIntegrationSettingsPageViewModel : SettingsPageViewModel, IDisposable
{
    private readonly IBuildServerIntegrationSettingsPageHost _host;
    private SettingsSource? _pendingSettings;
    private bool _showingSettings;
    private bool _isSettingsShown;

    public BuildServerIntegrationSettingsPageViewModel(BuildServerIntegrationSettingsPageStrings strings, IBuildServerIntegrationSettingsPageHost host)
    {
        Strings = strings;
        _host = host;

        // As Init: the plugins are found in the background, the page is enabled then.
        _ = LoadBuildServerTypesAsync();
    }

    public BuildServerIntegrationSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "BuildServerIntegrationSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>As <c>BuildServerType</c>: "None", then the types of the plugins.</summary>
    public ObservableCollection<string> BuildServerTypes { get; } = [];

    /// <summary>As the controls enabled when the plugins are found.</summary>
    [ObservableProperty]
    public partial bool IsLoaded { get; private set; }

    [ObservableProperty]
    public partial string? SelectedBuildServerType { get; set; }

    /// <summary>As <c>checkBoxEnableBuildServerIntegration</c> (three-state: unset).</summary>
    [ObservableProperty]
    public partial bool? IntegrationEnabled { get; set; }

    /// <summary>As <c>checkBoxShowBuildResultPage</c> (three-state: unset).</summary>
    [ObservableProperty]
    public partial bool? ShowBuildResultPage { get; set; }

    /// <summary>As the control of <c>buildServerSettingsPanel</c>: the settings of the selected build server.</summary>
    [ObservableProperty]
    public partial IBuildServerSettingsControl? SettingsControl { get; private set; }

    /// <summary>Plugin API v2: the settings of the selected build server declared by its plugin, rather than its <see cref="SettingsControl"/>.</summary>
    [ObservableProperty]
    public partial BuildServerPluginSettings? PluginSettings { get; private set; }

    private async Task LoadBuildServerTypesAsync()
    {
        IReadOnlyList<string> buildServerTypes = await _host.GetBuildServerTypesAsync();
        BuildServerTypes.Add(Strings.None.Text);
        foreach (string buildServerType in buildServerTypes)
        {
            BuildServerTypes.Add(buildServerType);
        }

        IsLoaded = true;

        if (_pendingSettings is { } settings)
        {
            _pendingSettings = null;
            ShowSettings(settings);
        }
    }

    // As SettingsToPage: the settings are shown once the plugins are found.
    protected override void SettingsToPage(SettingsSource? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (IsLoaded)
        {
            ShowSettings(settings);
        }
        else
        {
            _pendingSettings = settings;
        }
    }

    private void ShowSettings(SettingsSource settings)
    {
        _showingSettings = true;
        try
        {
            IntegrationEnabled = BuildServerSettings.IntegrationEnabled[settings];
            ShowBuildResultPage = BuildServerSettings.ShowBuildResultPage[settings];
            string? serverName = BuildServerSettings.ServerName[settings];
            SelectedBuildServerType = serverName is not null && BuildServerTypes.Contains(serverName) ? serverName : Strings.None.Text;
        }
        finally
        {
            _showingSettings = false;
        }

        ActivateBuildServerSettingsControl(settings);
        _isSettingsShown = true;
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Not before the settings are shown, which would unset them.
        if (!_isSettingsShown)
        {
            return;
        }

        BuildServerSettings.ServerName[settings] = GetSelectedBuildServerType();
        BuildServerSettings.IntegrationEnabled[settings] = IntegrationEnabled;
        BuildServerSettings.ShowBuildResultPage[settings] = ShowBuildResultPage;
        SettingsControl?.SaveSettings(BuildServerSettings.GetSettingsSource(settings));
        PluginSettings?.SaveSettings(BuildServerSettings.GetSettingsSource(settings));

        base.PageToSettings(settings);
    }

    // As BuildServerType_SelectedIndexChanged.
    partial void OnSelectedBuildServerTypeChanged(string? value)
    {
        if (!_showingSettings && IsLoaded)
        {
            ActivateBuildServerSettingsControl(CurrentSettings);
        }
    }

    private void ActivateBuildServerSettingsControl(SettingsSource settings)
    {
        IBuildServerSettingsControl? previous = SettingsControl;
        SettingsControl = null;
        PluginSettings = null;
        previous?.Dispose();

        if (GetSelectedBuildServerType() is not { } buildServerType)
        {
            return;
        }

        if (_host.CreatePluginSettings(buildServerType) is { } pluginSettings)
        {
            pluginSettings.LoadSettings(BuildServerSettings.GetSettingsSource(settings));
            PluginSettings = pluginSettings;
        }
        else if (_host.CreateSettingsControl(buildServerType) is { } control)
        {
            control.LoadSettings(BuildServerSettings.GetSettingsSource(settings));
            SettingsControl = control;
        }
    }

    private string? GetSelectedBuildServerType()
        => SelectedBuildServerType is null || SelectedBuildServerType == BuildServerTypes.FirstOrDefault() ? null : SelectedBuildServerType;

    public void Dispose()
    {
        SettingsControl?.Dispose();
        SettingsControl = null;
    }
}
