using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitExtUtils.GitUI.Theming;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the colors settings; ids match <c>ColorsSettingsPage</c>.</summary>
public sealed class ColorsSettingsPageStrings : ViewStrings
{
    public ColorsSettingsPageStrings()
        : base("ColorsSettingsPage")
    {
        Title = Add("$this", "Text", "Colors");
        FormatBuiltinThemeName = Add("FormatBuiltinThemeName", "Text", "{0}");
        FormatUserDefinedThemeName = Add("FormatUserDefinedThemeName", "Text", "{0}, user-defined");
        SystemColorModeThemeName = Add("SystemColorModeThemeName", "Text", "System color mode");
        RevisionGraph = Add("gbRevisionGraph", "Text", "Revision graph");
        MulticolorBranches = Add("MulticolorBranches", "Text", "Multicolor branches");
        DrawAlternateBackColor = Add("chkDrawAlternateBackColor", "Text", "Draw alternate background");
        DrawNonRelativesGray = Add("DrawNonRelativesGray", "Text", "Draw non relatives graph gray");
        DrawNonRelativesTextGray = Add("DrawNonRelativesTextGray", "Text", "Draw non relatives text gray");
        HighlightAuthored = Add("chkHighlightAuthored", "Text", "Highlight authored revisions");
        FillRefLabels = Add("chkFillRefLabels", "Text", "Fill git ref labels");
        Theme = Add("gbTheme", "Text", "Theme");
        RestartNeeded = Add("lblRestartNeeded", "Text", "Restart required to apply changes");
        OpenThemeFolder = Add("sbOpenThemeFolder", "Text", "Open theme folder");
        ApplicationFolder = Add("tsmiApplicationFolder", "Text", "Application folder");
        UserFolder = Add("tsmiUserFolder", "Text", "User folder");
        Colorblind = Add("chkColorblind", "Text", "Colorblind");
        UseSystemVisualStyle = Add("chkUseSystemVisualStyle", "Text", "Use system-defined visual style (looks bad with dark colors)");
    }

    public TranslatedText Title { get; }

    public TranslatedText FormatBuiltinThemeName { get; }

    public TranslatedText FormatUserDefinedThemeName { get; }

    /// <summary>The name of the theme that follows the light or dark mode of the system, off Windows ("Windows app color mode" there).</summary>
    public TranslatedText SystemColorModeThemeName { get; }

    public TranslatedText RevisionGraph { get; }

    public TranslatedText MulticolorBranches { get; }

    public TranslatedText DrawAlternateBackColor { get; }

    public TranslatedText DrawNonRelativesGray { get; }

    public TranslatedText DrawNonRelativesTextGray { get; }

    public TranslatedText HighlightAuthored { get; }

    public TranslatedText FillRefLabels { get; }

    public TranslatedText Theme { get; }

    public TranslatedText RestartNeeded { get; }

    public TranslatedText OpenThemeFolder { get; }

    public TranslatedText ApplicationFolder { get; }

    public TranslatedText UserFolder { get; }

    public TranslatedText Colorblind { get; }

    public TranslatedText UseSystemVisualStyle { get; }
}

/// <summary>The page as <c>ColorsSettingsPageController</c> sees it (as <c>IColorsSettingsPage</c>).</summary>
public interface IColorsSettingsPageView
{
    ThemeId SelectedThemeId { get; set; }

    string[] SelectedThemeVariations { get; set; }

    bool UseSystemVisualStyle { get; set; }

    bool LabelRestartIsNeededVisible { get; set; }

    bool IsChoosingVisualStyleEnabled { get; set; }

    void ShowThemeLoadingErrorMessage(ThemeId themeId, string[] variations, Exception ex);

    void PopulateThemeMenu(IEnumerable<ThemeId> themeIds);
}

/// <summary>The logic of the theme settings (<c>ColorsSettingsPageController</c>).</summary>
public interface IColorsSettingsPageController
{
    void ShowThemeSettings();

    void ApplyThemeSettings();

    void HandleSelectedThemeChanged();

    void HandleUseSystemVisualStyleChanged();

    void HandleUseColorblindVariationChanged();

    void ShowAppThemesDirectory();

    void ShowUserThemesDirectory();
}

/// <summary>What the colors settings need from the application: the controller of the themes.</summary>
public interface IColorsSettingsPageHost : ISettingsPageServices
{
    /// <summary>Creates the controller of the page (<c>new ColorsSettingsPageController(page, new ThemeRepository(), ...)</c>).</summary>
    IColorsSettingsPageController CreateController(IColorsSettingsPageView page);
}

/// <summary>A theme of <c>_NO_TRANSLATE_cbSelectTheme</c> (<c>FormattedThemeId</c>).</summary>
public sealed record ThemeChoice(ThemeId ThemeId, string Text)
{
    public override string ToString() => Text;
}

/// <summary>Port of <c>ColorsSettingsPage</c> (global settings), using the logic of <c>ColorsSettingsPageController</c>.</summary>
public sealed partial class ColorsSettingsPageViewModel : SettingsPageWithServicesViewModel, IColorsSettingsPageView
{
    private readonly IColorsSettingsPageController _controller;

    public ColorsSettingsPageViewModel(ColorsSettingsPageStrings strings, IColorsSettingsPageHost host)
        : base(host)
    {
        Strings = strings;
        _controller = host.CreateController(this);
    }

    public ColorsSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "ColorsSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    [ObservableProperty]
    public partial bool MulticolorBranches { get; set; }

    [ObservableProperty]
    public partial bool DrawAlternateBackColor { get; set; }

    [ObservableProperty]
    public partial bool DrawNonRelativesGray { get; set; }

    [ObservableProperty]
    public partial bool DrawNonRelativesTextGray { get; set; }

    [ObservableProperty]
    public partial bool HighlightAuthored { get; set; }

    [ObservableProperty]
    public partial bool FillRefLabels { get; set; }

    /// <summary>The items of <c>_NO_TRANSLATE_cbSelectTheme</c>.</summary>
    public ObservableCollection<ThemeChoice> Themes { get; } = [];

    [ObservableProperty]
    public partial ThemeChoice? SelectedTheme { get; set; }

    /// <summary>As <c>chkColorblind</c>.</summary>
    [ObservableProperty]
    public partial bool IsColorblind { get; set; }

    /// <summary>As <c>chkUseSystemVisualStyle</c>.</summary>
    [ObservableProperty]
    public partial bool UseSystemVisualStyle { get; set; }

    /// <summary>As <c>lblRestartNeeded.Visible</c>.</summary>
    [ObservableProperty]
    public partial bool LabelRestartIsNeededVisible { get; set; }

    /// <summary>As <c>chkUseSystemVisualStyle.Enabled</c>.</summary>
    [ObservableProperty]
    public partial bool IsChoosingVisualStyleEnabled { get; set; } = true;

    public ThemeId SelectedThemeId
    {
        get => SelectedTheme?.ThemeId ?? ThemeId.DefaultLight;
        set
        {
            ThemeChoice? theme = Themes.FirstOrDefault(t => t.ThemeId == value);
            if (theme is null)
            {
                // Handle case when selected theme is missing gracefully, e.g. deleted after it was chosen.
                string name = Format(value);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    Services.ShowError($"Theme not found: {name}");
                }

                theme = Themes.FirstOrDefault();
            }

            SelectedTheme = theme;
        }
    }

    public string[] SelectedThemeVariations
    {
        get => IsColorblind ? [ThemeVariations.Colorblind] : ThemeVariations.None;
        set => IsColorblind = value.Contains(ThemeVariations.Colorblind);
    }

    public void PopulateThemeMenu(IEnumerable<ThemeId> themeIds)
    {
        Themes.Clear();
        foreach (ThemeId themeId in themeIds)
        {
            Themes.Add(new ThemeChoice(themeId, Format(themeId)));
        }
    }

    public void ShowThemeLoadingErrorMessage(ThemeId themeId, string[] variations, Exception ex)
    {
        Trace.WriteLine($"Failed to load theme {themeId.Name}: {ex}");
        string variationsStr = string.Concat(variations.Select(v => "." + v));
        AppSettings.ThemeId = ThemeId.DefaultLight;
        Services.ShowError($"Failed to load theme {Format(themeId)}{variationsStr}: {ex.Message}"
            + $"{Environment.NewLine}{Environment.NewLine}See also https://github.com/gitextensions/gitextensions/wiki/Dark-Mode");
    }

    // As FormattedThemeId.ToString; off Windows the theme of the color mode of the system is not named after Windows.
    private string Format(ThemeId themeId)
        => themeId == ThemeId.WindowsAppColorModeId && !OperatingSystem.IsWindows()
            ? Strings.SystemColorModeThemeName.Text
            : string.Format(themeId.IsBuiltin ? Strings.FormatBuiltinThemeName.Text : Strings.FormatUserDefinedThemeName.Text, themeId.Name);

    partial void OnSelectedThemeChanged(ThemeChoice? value) => _controller.HandleSelectedThemeChanged();

    partial void OnUseSystemVisualStyleChanged(bool value) => _controller.HandleUseSystemVisualStyleChanged();

    partial void OnIsColorblindChanged(bool value) => _controller.HandleUseColorblindVariationChanged();

    [RelayCommand]
    private void OpenApplicationThemesFolder() => _controller.ShowAppThemesDirectory();

    [RelayCommand]
    private void OpenUserThemesFolder() => _controller.ShowUserThemesDirectory();

    protected override void SettingsToPage(SettingsSource? settings)
    {
        MulticolorBranches = AppSettings.MulticolorBranches;
        DrawAlternateBackColor = AppSettings.RevisionGraphDrawAlternateBackColor;
        DrawNonRelativesGray = AppSettings.RevisionGraphDrawNonRelativesGray;
        DrawNonRelativesTextGray = AppSettings.RevisionGraphDrawNonRelativesTextGray;
        HighlightAuthored = AppSettings.HighlightAuthoredRevisions;
        FillRefLabels = AppSettings.FillRefLabels;
        _controller.ShowThemeSettings();

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.MulticolorBranches = MulticolorBranches;
        AppSettings.RevisionGraphDrawAlternateBackColor = DrawAlternateBackColor;
        AppSettings.RevisionGraphDrawNonRelativesGray = DrawNonRelativesGray;
        AppSettings.RevisionGraphDrawNonRelativesTextGray = DrawNonRelativesTextGray;
        AppSettings.HighlightAuthoredRevisions = HighlightAuthored;
        AppSettings.FillRefLabels = FillRefLabels;
        _controller.ApplyThemeSettings();

        base.PageToSettings(settings);
    }
}
