using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog;

/// <summary>Strings of the settings dialog; ids match <c>FormSettings</c>, <c>SettingsPageHeader</c> and the groups.</summary>
public sealed class SettingsDialogStrings : ViewStrings
{
    public SettingsDialogStrings()
        : base("FormSettings")
    {
        Title = Add("$this", "Text", "Settings");
        CantSaveSettings = Add("_cantSaveSettings", "Text", "Failed to save all settings");
        Ok = Add("buttonOk", "Text", "OK");
        Cancel = Add("buttonCancel", "Text", "Cancel");
        Apply = Add("buttonApply", "Text", "Apply");
        Discard = Add("buttonDiscard", "Text", "Discard");
        InstantSaveNotice = Add("labelInstantSaveNotice", "Text", "Changes on the selected page will be saved instantly. \nTherefore the Cancel button does NOT revert any changes made.");
        SettingsSource = Add("label1", "Text", "Settings source:", category: "SettingsPageHeader");
        Effective = Add("EffectiveRB", "Text", "Effective", category: "SettingsPageHeader");
        Local = Add("LocalRB", "Text", "Local for current repository", category: "SettingsPageHeader");
        Distributed = Add("DistributedRB", "Text", "Distributed with current repository", category: "SettingsPageHeader");
        Global = Add("GlobalRB", "Text", "Global for all repositories", category: "SettingsPageHeader");
        System = Add("SystemRB", "Text", "System", category: "SettingsPageHeader");
        TypeToFind = Add("_settingsTypeToFind", "Text", "Type to find", category: "TranslatedStrings");
        GitGroup = Add("$this", "Title", "Git", category: "GitSettingsGroup");
        PluginsGroup = Add("$this", "Title", "Plugins", category: "PluginsSettingsGroup");
        GitExtensionsGroup = Add("$this", "Title", "Gitanas", category: "GitExtensionsSettingsGroup");
        Error = Add("_error", "Text", "Error", category: "TranslatedStrings");
    }

    public TranslatedText Title { get; }

    public TranslatedText CantSaveSettings { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }

    public TranslatedText Apply { get; }

    public TranslatedText Discard { get; }

    public TranslatedText InstantSaveNotice { get; }

    public TranslatedText SettingsSource { get; }

    public TranslatedText Effective { get; }

    public TranslatedText Local { get; }

    public TranslatedText Distributed { get; }

    public TranslatedText Global { get; }

    public TranslatedText System { get; }

    public TranslatedText TypeToFind { get; }

    public TranslatedText GitGroup { get; }

    public TranslatedText PluginsGroup { get; }

    public TranslatedText GitExtensionsGroup { get; }

    public TranslatedText Error { get; }
}

/// <summary>The settings a page edits (a level of <c>SettingsPageHeader</c>), in the order of the header.</summary>
public enum SettingsLevel
{
    Effective,
    Local,
    Distributed,
    Global,
    System,
}

/// <summary>A level of the header of a page (a radio button of <c>SettingsPageHeader</c>).</summary>
public sealed class SettingsLevelChoice : ObservableObject
{
    private readonly SettingsPageViewModel _page;

    public SettingsLevelChoice(SettingsPageViewModel page, SettingsLevel level, string text)
    {
        _page = page;
        Level = level;
        Text = text;
    }

    public SettingsLevel Level { get; }

    public string Text { get; }

    /// <summary>Whether the level is the one edited; checking it selects it.</summary>
    public bool IsSelected
    {
        get => _page.Level == Level;
        set
        {
            if (value)
            {
                _page.Level = Level;
            }
        }
    }

    /// <summary>As <c>arrowLocal</c> and the other arrows: shown before the levels combined into the effective settings.</summary>
    public bool ShowsArrow => Level != SettingsLevel.Effective;

    internal void OnLevelChanged() => OnPropertyChanged(nameof(IsSelected));
}

/// <summary>What the pages need from the dialog (<c>ISettingsPageHost</c>).</summary>
public interface ISettingsPageHost
{
    /// <summary>Selects the page of the WinForms page type name (as <c>GotoPage</c>).</summary>
    void GotoPage(string pageName);

    /// <summary>Saves the settings of all the pages (as <c>SaveAll</c>, used by the checklist).</summary>
    void SaveAll();

    /// <summary>Loads the settings of all the pages again (as <c>LoadAll</c>, used by the checklist).</summary>
    void LoadAll();
}

/// <summary>
///  A settings page (port of <c>SettingsPageBase</c> and <c>SettingsPageWithHeader</c>): its settings are loaded when the
///  dialog opens and saved with OK or Apply; with a header, the level of the settings it edits can be chosen
///  (<c>SettingsPageHeader</c>, <c>DistributedSettingsPage</c>, <c>GitConfigBaseSettingsPage</c>).
/// </summary>
public abstract partial class SettingsPageViewModel : ObservableObject
{
    private readonly List<SettingValue> _values = [];
    private IReadOnlyDictionary<SettingsLevel, SettingsSource> _sources = new Dictionary<SettingsLevel, SettingsSource>();
    private bool _changingLevel;

    /// <summary>The title in the tree and in the title of the dialog (<c>GetTitle</c>).</summary>
    public abstract string Title { get; }

    /// <summary>The name of the WinForms page type (of its <c>SettingsPageReferenceByType</c>), to go to the page.</summary>
    public abstract string PageName { get; }

    /// <summary>As <c>IsInstantSavePage</c>: the page saves its changes at once, Cancel does not revert them.</summary>
    public virtual bool IsInstantSavePage => false;

    /// <summary>As <c>GetSearchKeywords</c>: the texts searched by the filter of the tree (by default, the title only).</summary>
    public virtual IEnumerable<string> SearchKeywords => [];

    /// <summary>The levels of the header (<c>SettingsPageHeader</c>); none without header (<c>SettingsPageBase</c>).</summary>
    public IReadOnlyList<SettingsLevelChoice> Levels { get; private set; } = [];

    public bool HasHeader => Levels.Count > 0;

    /// <summary>The level whose settings are edited.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnly), nameof(IsEffective))]
    public partial SettingsLevel Level { get; set; } = SettingsLevel.Global;

    /// <summary>As the arrows of the header, visible for the effective settings (combining the other levels).</summary>
    public bool IsEffective => Level == SettingsLevel.Effective;

    /// <summary>As <c>ReadOnly</c> of the header: the effective and the system settings are shown only.</summary>
    public bool IsReadOnly => Level is SettingsLevel.Effective or SettingsLevel.System;

    /// <summary>As <c>IsLoadingSettings</c>: true while the settings are shown.</summary>
    protected bool IsLoadingSettings { get; private set; }

    /// <summary>As <c>IsSettingsLoaded</c>.</summary>
    protected bool IsSettingsLoaded { get; private set; }

    /// <summary>The dialog, set when the page is added.</summary>
    protected ISettingsPageHost? PageHost { get; private set; }

    /// <summary>The settings of the level (<c>GetCurrentSettings</c>).</summary>
    protected SettingsSource CurrentSettings => _sources[Level];

    /// <summary>
    ///  Sets the levels the page can edit, with the level shown first: <see cref="SettingsLevel.Effective"/> when the page has
    ///  levels of the repository, as <c>ConfigureHeader</c>.
    /// </summary>
    public void Initialize(ISettingsPageHost pageHost, IReadOnlyDictionary<SettingsLevel, SettingsSource> sources, IReadOnlyDictionary<SettingsLevel, string> levelTexts)
    {
        PageHost = pageHost;
        _sources = sources;
        Levels = [.. sources.Keys.Order().Select(level => new SettingsLevelChoice(this, level, levelTexts[level]))];
        _changingLevel = true;
        try
        {
            Level = sources.ContainsKey(SettingsLevel.Effective) ? SettingsLevel.Effective : SettingsLevel.Global;
        }
        finally
        {
            _changingLevel = false;
        }
    }

    // As SetCurrentSettings: the settings of the previous level are saved, those of the new one shown.
    partial void OnLevelChanged(SettingsLevel oldValue, SettingsLevel newValue)
    {
        foreach (SettingsLevelChoice choice in Levels)
        {
            choice.OnLevelChanged();
        }

        if (_changingLevel || !_sources.ContainsKey(newValue))
        {
            return;
        }

        if (oldValue is not (SettingsLevel.Effective or SettingsLevel.System) && _sources.ContainsKey(oldValue))
        {
            PageToSettings(_sources[oldValue]);
        }

        LoadSettings();
    }

    /// <summary>As <c>OnPageShown</c>: the page is shown (again).</summary>
    public virtual void OnPageShown()
    {
    }

    /// <summary>As <c>LoadSettings</c>.</summary>
    public void LoadSettings()
    {
        IsLoadingSettings = true;
        try
        {
            SettingsToPage(_sources.Count > 0 ? CurrentSettings : null);
        }
        finally
        {
            IsLoadingSettings = false;
        }

        IsSettingsLoaded = true;
    }

    /// <summary>As <c>SaveSettings</c>: nothing is saved when read-only.</summary>
    public void SaveSettings()
    {
        if (!IsReadOnly || _sources.Count == 0)
        {
            PageToSettings(_sources.Count > 0 ? CurrentSettings : null);
        }
    }

    /// <summary>Adds a value loaded and saved with the settings of the level.</summary>
    protected T Add<T>(T value)
        where T : SettingValue
    {
        _values.Add(value);
        return value;
    }

    /// <summary>As <c>SettingsToPage</c>: shows the settings; <paramref name="settings"/> is null for a page without levels.</summary>
    protected virtual void SettingsToPage(SettingsSource? settings)
    {
        if (settings is null)
        {
            return;
        }

        foreach (SettingValue value in _values)
        {
            value.Load(settings);
        }
    }

    /// <summary>As <c>PageToSettings</c>: saves the page.</summary>
    protected virtual void PageToSettings(SettingsSource? settings)
    {
        if (settings is null)
        {
            return;
        }

        foreach (SettingValue value in _values)
        {
            value.Save(settings);
        }
    }
}

/// <summary>A page that groups others (<c>GroupSettingsPage</c>): selecting it shows its root page or its first child.</summary>
public sealed class GroupSettingsPageViewModel(string title, string pageName) : SettingsPageViewModel
{
    public override string Title => title;

    public override string PageName => pageName;
}

/// <summary>A node of the tree of the pages (<c>SettingsTreeViewUserControl</c>).</summary>
public sealed partial class SettingsTreeNode : ObservableObject
{
    public SettingsTreeNode(SettingsPageViewModel page, object? icon)
    {
        Page = page;
        Icon = icon;
    }

    /// <summary>The page, or the root page shown for a group (<c>asRoot</c>).</summary>
    public SettingsPageViewModel Page { get; set; }

    /// <summary>The group page, for a group node.</summary>
    public SettingsPageViewModel? Group { get; init; }

    public string Title => (Group ?? Page).Title;

    /// <summary>The image of the node, if any: the name of an asset of GitUI.Avalonia, or the PNG data of an image (of a plugin).</summary>
    public object? Icon { get; }

    public ObservableCollection<SettingsTreeNode> Children { get; } = [];

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>As <c>HighlightNode</c>: the page matches the filter.</summary>
    [ObservableProperty]
    public partial bool IsHighlighted { get; set; }

    public IEnumerable<SettingsTreeNode> DescendantsAndSelf()
        => Children.SelectMany(child => child.DescendantsAndSelf()).Prepend(this);
}

/// <summary>Saves the settings sets and shows the errors of the settings dialog.</summary>
public interface ISettingsDialogHost
{
    /// <summary>
    ///  As the end of <c>Save</c>: saves the git config and distributed settings sets, checks the HOME directory and saves the
    ///  application settings.
    /// </summary>
    /// <returns>The error, or <see langword="null"/> when saved (a <c>SaveSettingsException</c>).</returns>
    string? SaveSettingsSets();

    void ShowError(string heading, string text);

    /// <summary>
    ///  After each save (OK, Apply): whether the application is to restart for what was saved, e.g. another theme (the host
    ///  asks). The dialog then closes.
    /// </summary>
    bool ConfirmRestart() => false;
}

/// <summary>
///  View model of the settings dialog (port of <c>FormSettings</c> and <c>SettingsTreeViewUserControl</c>;
///  docs/avalonia-port/PLAN.md, phase 6): the tree of the pages with its filter, the selected page with its header, OK,
///  Cancel and Apply.
/// </summary>
public sealed partial class SettingsDialogViewModel : DialogViewModel, ISettingsPageHost
{
    /// <summary>As <c>_lastSelectedSettingsPageType</c>: the page shown the next time the dialog opens.</summary>
    private static string? _lastSelectedPageName;

    private readonly ISettingsDialogHost _host;
    private readonly List<SettingsTreeNode> _foundNodes = [];

    public SettingsDialogViewModel(SettingsDialogStrings strings, ISettingsDialogHost host)
    {
        Strings = strings;
        _host = host;
        Title = strings.Title.Text;
        LevelTexts = new Dictionary<SettingsLevel, string>
        {
            [SettingsLevel.Effective] = strings.Effective.AccessKeyText,
            [SettingsLevel.Local] = strings.Local.AccessKeyText,
            [SettingsLevel.Distributed] = strings.Distributed.AccessKeyText,
            [SettingsLevel.Global] = strings.Global.AccessKeyText,
            [SettingsLevel.System] = strings.System.AccessKeyText,
        };
    }

    public SettingsDialogStrings Strings { get; }

    /// <summary>The texts of the levels of the headers.</summary>
    public IReadOnlyDictionary<SettingsLevel, string> LevelTexts { get; }

    public ObservableCollection<SettingsTreeNode> Nodes { get; } = [];

    public IEnumerable<SettingsPageViewModel> Pages => Nodes.SelectMany(n => n.DescendantsAndSelf()).Select(n => n.Page).Distinct();

    [ObservableProperty]
    public partial string Title { get; private set; }

    [ObservableProperty]
    public partial SettingsTreeNode? SelectedNode { get; set; }

    /// <summary>The page shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstantSavePage))]
    public partial SettingsPageViewModel? SelectedPage { get; private set; }

    /// <summary>As <c>labelInstantSaveNotice</c>.</summary>
    public bool IsInstantSavePage => SelectedPage?.IsInstantSavePage == true;

    /// <summary>The filter of the tree (<c>textBoxFind</c>).</summary>
    [ObservableProperty]
    public partial string Filter { get; set; } = "";

    /// <summary>Adds a page (as <c>AddSettingsPage</c>) below the page of <paramref name="parentPageName"/>, or as a root.</summary>
    /// <param name="asRoot">The page is shown for its parent group (the checklist for "Git Extensions").</param>
    public SettingsTreeNode AddPage(SettingsPageViewModel page, string? parentPageName, object? icon, IReadOnlyDictionary<SettingsLevel, SettingsSource> sources, bool asRoot = false)
    {
        page.Initialize(this, sources, LevelTexts);
        if (parentPageName is null)
        {
            SettingsTreeNode root = new(page, icon) { Group = page is GroupSettingsPageViewModel ? page : null };
            Nodes.Add(root);
            return root;
        }

        SettingsTreeNode parent = FindNode(parentPageName) ?? throw new ArgumentException("You have to add parent page first: " + parentPageName);
        if (asRoot)
        {
            parent.Page = page;
            return parent;
        }

        SettingsTreeNode node = new(page, icon);
        parent.Children.Add(node);
        return node;
    }

    private SettingsTreeNode? FindNode(string pageName)
        => Nodes.SelectMany(n => n.DescendantsAndSelf()).FirstOrDefault(n => n.Page.PageName == pageName || n.Group?.PageName == pageName);

    /// <summary>As <c>FormSettings_Shown</c>: the settings are loaded and the initial (or last) page shown.</summary>
    public void Open(string? initialPageName)
    {
        LoadAll();
        GotoPage(initialPageName ?? _lastSelectedPageName);

        // The page shown last may be missing (e.g. a plugin removed since): the first page is shown instead.
        if (SelectedNode is null)
        {
            GotoPage(null);
        }
    }

    /// <summary>As <c>GotoPage</c>: the first page without a name.</summary>
    public void GotoPage(string? pageName)
    {
        SettingsTreeNode? node = pageName is null ? Nodes.FirstOrDefault() : FindNode(pageName);
        if (node is not null)
        {
            // The node is shown, below its expanded parents.
            foreach (SettingsTreeNode parent in Nodes.SelectMany(n => n.DescendantsAndSelf()).Where(n => n.DescendantsAndSelf().Contains(node)))
            {
                parent.IsExpanded = true;
            }

            SelectedNode = node;
        }
    }

    void ISettingsPageHost.GotoPage(string pageName) => GotoPage(pageName);

    // As OnSettingsPageSelected: a group without its own page shows its first child.
    partial void OnSelectedNodeChanged(SettingsTreeNode? value)
    {
        if (value is null)
        {
            return;
        }

        if (value.Page is GroupSettingsPageViewModel && value.Children.FirstOrDefault() is { } first)
        {
            GotoPage(first.Page.PageName);
            return;
        }

        SettingsPageViewModel page = value.Page;
        _lastSelectedPageName = page.PageName;
        SelectedPage = page;
        Title = $"{Strings.Title.Text} - {page.Title}";
        page.OnPageShown();
    }

    // As textBoxFind_TextChanged: the pages whose title or keywords match are highlighted (spaces combine as "and").
    partial void OnFilterChanged(string value)
    {
        _foundNodes.Clear();
        List<SettingsTreeNode> all = [.. Nodes.SelectMany(n => n.DescendantsAndSelf())];
        foreach (SettingsTreeNode node in all)
        {
            node.IsHighlighted = false;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        string searchFor = value.ToLowerInvariant();
        string[] keywords = searchFor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (SettingsTreeNode node in all)
        {
            SettingsPageViewModel page = node.Page;
            bool matches = node.Title.Contains(searchFor, StringComparison.InvariantCultureIgnoreCase)
                || page.Title.Contains(searchFor, StringComparison.InvariantCultureIgnoreCase)
                || keywords.All(keyword => page.SearchKeywords.Any(k => k.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)));
            if (matches && !_foundNodes.Contains(node))
            {
                _foundNodes.Add(node);
                node.IsHighlighted = true;
                ExpandParents(node);
            }
        }

        void ExpandParents(SettingsTreeNode node)
        {
            foreach (SettingsTreeNode parent in all.Where(p => p.Children.Contains(node)))
            {
                parent.IsExpanded = true;
                ExpandParents(parent);
            }
        }
    }

    /// <summary>As <c>textBoxFind_KeyUp</c> with Enter: the next highlighted page is selected (cycling).</summary>
    [RelayCommand]
    private void SelectNextFound()
    {
        if (_foundNodes.Count == 0)
        {
            return;
        }

        int index = SelectedNode is null ? -1 : _foundNodes.IndexOf(SelectedNode);
        SelectedNode = _foundNodes[index == -1 || index + 1 == _foundNodes.Count ? 0 : index + 1];
    }

    /// <summary>As <c>LoadSettings</c>.</summary>
    public void LoadAll()
    {
        foreach (SettingsPageViewModel page in Pages)
        {
            page.LoadSettings();
        }
    }

    public void SaveAll() => Save();

    /// <summary>As <c>Save</c>.</summary>
    public bool Save()
    {
        foreach (SettingsPageViewModel page in Pages)
        {
            page.SaveSettings();
        }

        if (_host.SaveSettingsSets() is { } error)
        {
            _host.ShowError(Strings.CantSaveSettings.Text, error);
            return false;
        }

        IsSaved = true;
        if (_host.ConfirmRestart())
        {
            IsRestartConfirmed = true;
        }

        return true;
    }

    /// <summary>Whether a save asked to restart the application (<see cref="ISettingsDialogHost.ConfirmRestart"/>).</summary>
    public bool IsRestartConfirmed { get; private set; }

    /// <summary>As <c>_saved</c>: the settings were saved (the dialog result is OK).</summary>
    public bool IsSaved { get; private set; }

    [RelayCommand]
    private void Ok()
    {
        if (Save())
        {
            Close(accepted: true);
        }
    }

    [RelayCommand]
    private void Apply()
    {
        if (Save() && IsRestartConfirmed)
        {
            Close(accepted: true);
        }
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: IsSaved);

    /// <summary>As <c>buttonDiscard_Click</c> (shown in debug builds): the saved settings are shown again.</summary>
    [RelayCommand]
    private void Discard() => LoadAll();
}
