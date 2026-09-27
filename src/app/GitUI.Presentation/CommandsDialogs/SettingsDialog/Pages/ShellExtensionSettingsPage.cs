using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the shell extension settings; ids match <c>ShellExtensionSettingsPage</c>.</summary>
public sealed class ShellExtensionSettingsPageStrings : ViewStrings
{
    public ShellExtensionSettingsPageStrings()
        : base("ShellExtensionSettingsPage")
    {
        Title = Add("$this", "Text", "Shell extension");
        NoItems = Add("_noItems", "Text", "no items");
        MenuHelp = Add("_menuHelp", "Text", "* Checked: at top level for direct access\n* Intermediate: in a cascaded context menu\n* Unchecked: not added to the menu");
        ExplorerIntegration = Add("gbExplorerIntegration", "Text", "Windows Explorer integration");
        Register = Add("RegisterButton", "Text", "&Enable shell extension");
        Unregister = Add("UnregisterButton", "Text", "&Disable shell extension");
        CascadingMenu = Add("gbCascadingMenu", "Text", "Cascaded context menu");
        AlwaysShowAllCommands = Add("cbAlwaysShowAllCommands", "Text", "Always show all commands");
        MenuEntries = Add("lblMenuEntries", "Text", "Configuration of items in the context menu");
        Preview = Add("label1", "Text", "Context menu preview:");
    }

    public TranslatedText Title { get; }

    public TranslatedText NoItems { get; }

    public TranslatedText MenuHelp { get; }

    public TranslatedText ExplorerIntegration { get; }

    public TranslatedText Register { get; }

    public TranslatedText Unregister { get; }

    public TranslatedText CascadingMenu { get; }

    public TranslatedText AlwaysShowAllCommands { get; }

    public TranslatedText MenuEntries { get; }

    public TranslatedText Preview { get; }
}

/// <summary>What <see cref="ShellExtensionSettingsPageViewModel"/> needs from the application (<c>ShellExtensionManager</c>).</summary>
public interface IShellExtensionSettingsPageHost
{
    /// <summary>The items of the menu (<c>AppSettings.CascadeShellMenuItems</c>, stored in the registry).</summary>
    string CascadeShellMenuItems { get; set; }

    /// <summary>As <c>AppSettings.AlwaysShowAllCommands</c>, stored in the registry.</summary>
    bool AlwaysShowAllCommands { get; set; }

    /// <summary>Whether the files of the shell extension are installed.</summary>
    bool FilesExist();

    bool IsRegistered();

    /// <summary>Registers the shell extension (elevated); the errors are reported by the host.</summary>
    void Register();

    /// <summary>Unregisters the shell extension (elevated); the errors are reported by the host.</summary>
    void Unregister();

    void OpenUrl(string url);

    /// <summary>The URL of the section of the manual about the shell extension.</summary>
    string GetManualUrl();
}

/// <summary>
///  An item of the context menu of the shell extension (<c>_NO_TRANSLATE_chlMenuEntries</c>): checked at the top level,
///  indeterminate in the cascaded menu, unchecked not shown.
/// </summary>
public sealed partial class ShellMenuEntry(string text, Action changed) : ObservableObject
{
    public string Text { get; } = text;

    /// <summary>
    ///  <see langword="true"/> at the top level, <see langword="null"/> in the cascaded menu, <see langword="false"/> not in the
    ///  menu. A click cycles as <c>chlMenuEntries_ItemCheck</c>: checked, unchecked, indeterminate.
    /// </summary>
    [ObservableProperty]
    public partial bool? State { get; set; } = false;

    partial void OnStateChanged(bool? value) => changed();
}

/// <summary>Port of <c>ShellExtensionSettingsPage</c> (global settings, Windows only): the items of the context menu of Explorer.</summary>
public sealed partial class ShellExtensionSettingsPageViewModel : SettingsPageViewModel
{
    private const char CheckedInMenu = '0';
    private const char IndeterminateInSubMenu = '1';
    private const char UncheckedNotInMenu = '2';

    /// <summary>The items of <c>_NO_TRANSLATE_chlMenuEntries</c>, in the order of <c>AppSettings.CascadeShellMenuItems</c>.</summary>
    private static readonly string[] _menuEntryTexts =
    [
        "Add files...",
        "Apply patch...",
        "Open repository",
        "Create branch...",
        "Checkout branch...",
        "Checkout revision...",
        "Clone...",
        "Commit...",
        "Create new repository...",
        "Open with difftool",
        "File history",
        "Pull/Fetch...",
        "Push...",
        "Reset file changes..",
        "Revert",
        "Settings",
        "View stash",
        "View changes",
    ];

    private readonly IShellExtensionSettingsPageHost _host;

    public ShellExtensionSettingsPageViewModel(ShellExtensionSettingsPageStrings strings, IShellExtensionSettingsPageHost host)
    {
        Strings = strings;
        _host = host;
        MenuEntries = [.. _menuEntryTexts.Select(text => new ShellMenuEntry(text, UpdatePreview))];
        UpdateRegistrationStatus();
        UpdatePreview();
    }

    public ShellExtensionSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "ShellExtensionSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    public IReadOnlyList<ShellMenuEntry> MenuEntries { get; }

    [ObservableProperty]
    public partial bool AlwaysShowAllCommands { get; set; }

    /// <summary>As <c>gbExplorerIntegration.Enabled</c>: the files of the shell extension are installed.</summary>
    [ObservableProperty]
    public partial bool IsExplorerIntegrationEnabled { get; private set; }

    /// <summary>As <c>RegisterButton.Enabled</c>.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    public partial bool CanRegister { get; private set; }

    /// <summary>As <c>labelPreview</c>.</summary>
    [ObservableProperty]
    public partial string Preview { get; private set; } = "";

    protected override void SettingsToPage(SettingsSource? settings)
    {
        string cascadeShellMenuItems = _host.CascadeShellMenuItems;
        for (int i = 0; i < cascadeShellMenuItems.Length && i < MenuEntries.Count; i++)
        {
            switch (cascadeShellMenuItems[i])
            {
                case CheckedInMenu:
                    MenuEntries[i].State = true;
                    break;
                case IndeterminateInSubMenu:
                    MenuEntries[i].State = null;
                    break;
                case UncheckedNotInMenu:
                    MenuEntries[i].State = false;
                    break;
            }
        }

        AlwaysShowAllCommands = _host.AlwaysShowAllCommands;

        UpdatePreview();

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        string cascadeShellMenuItems = new([.. MenuEntries.Select(entry => entry.State switch
        {
            null => IndeterminateInSubMenu,
            true => CheckedInMenu,
            false => UncheckedNotInMenu,
        })]);

        // Saving another page must not create or overwrite shell-extension registry settings.
        if (cascadeShellMenuItems != _host.CascadeShellMenuItems)
        {
            _host.CascadeShellMenuItems = cascadeShellMenuItems;
        }

        if (AlwaysShowAllCommands != _host.AlwaysShowAllCommands)
        {
            _host.AlwaysShowAllCommands = AlwaysShowAllCommands;
        }

        base.PageToSettings(settings);
    }

    private void UpdatePreview()
    {
        string topLevel = "";
        string cascaded = "";
        foreach (ShellMenuEntry entry in MenuEntries)
        {
            switch (entry.State)
            {
                case true:
                    topLevel += "GitExt " + entry.Text + "\n";
                    break;
                case null:
                    cascaded += "       " + entry.Text + "\n";
                    break;
            }
        }

        string preview = topLevel;
        if (!string.IsNullOrWhiteSpace(cascaded))
        {
            preview += "Gitanas > \n" + cascaded;
        }
        else if (string.IsNullOrWhiteSpace(topLevel))
        {
            preview += $"({Strings.NoItems.Text})";
        }

        Preview = preview;
    }

    private void UpdateRegistrationStatus()
    {
        IsExplorerIntegrationEnabled = _host.FilesExist();
        CanRegister = !_host.IsRegistered();
    }

    /// <summary>As <c>RegisterButton_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(CanRegister))]
    private void Register()
    {
        _host.Register();
        UpdateRegistrationStatus();
    }

    /// <summary>As <c>UnregisterButton_Click</c>.</summary>
    [RelayCommand]
    private void Unregister()
    {
        _host.Unregister();
        UpdateRegistrationStatus();
    }

    /// <summary>As <c>menuHelp_Click</c>.</summary>
    [RelayCommand]
    private void OpenHelp() => _host.OpenUrl(_host.GetManualUrl());
}
