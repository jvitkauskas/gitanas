using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>Avalonia port of <c>FormBrowse</c> (first part).</summary>
public partial class BrowseWindow : DialogWindow
{
    private readonly List<(MenuItem Item, BrowseSubmenu Submenu)> _submenus = [];
    private readonly HashSet<MenuItem> _submenuOwners = [];
    private readonly List<(MenuItem Item, BrowseSubmenu Submenu)> _modelSubmenus = [];
    private BrowseViewModel? _viewModel;
    private double _tabsHeight = 320;
    private double _commitInfoWidth = 490;
    private bool _isOpened;

    public BrowseWindow()
    {
        InitializeComponent();
        ApplyModernLayout();

        foreach (Control child in toolbar.Children)
        {
            child.PropertyChanged += OnToolbarChildPropertyChanged;
        }

        toolbar.ContextRequested += OnToolbarContextRequested;

        // As RevisionGridControl.OnGridViewDoubleClick: the commit and its diff in a window.
        revisionGrid.RevisionActivated += (_, _) => _viewModel?.RunCommand.Execute(BrowseCommand.ViewSelectedRevisions);
        diffPanel.HotkeyHandler = keyData => _viewModel?.ProcessRevisionDiffHotkey(keyData, fileTree: false) == true;
        treePanel.HotkeyHandler = keyData => _viewModel?.ProcessRevisionDiffHotkey(keyData, fileTree: true) == true;
        if (OperatingSystem.IsMacOS())
        {
            Activated += (_, _) => AttachMacOSApplicationMenu();
        }

        InitializeMacOSMenu();

        // As OnRuntimeLoad: the revisions are loaded once the window is shown.
        Opened += (_, _) =>
        {
            _isOpened = true;
            if (_viewModel is not null)
            {
                RestoreSplitters(_viewModel);
            }

            Dispatcher.UIThread.Post(() => _viewModel?.Initialize(SelectedId, FirstId));
        };
        Closing += (_, _) =>
        {
            if (_viewModel is not null && _isOpened)
            {
                SaveSplitters(_viewModel);
            }
        };
        tabs.SelectionChanged += (_, _) =>
        {
            if (_viewModel is not null && tabs.SelectedIndex >= 0)
            {
                _viewModel.SelectedTab = (BrowseTab)tabs.SelectedIndex;
            }
        };

        // As CopyToClipboard of OutputHistoryControllerBase: the selection, else the whole history.
        copyOutputHistoryItem.Click += (_, _) => _viewModel?.CopyOutputHistory(outputHistory.SelectedText);
        copyOutputHistoryPanelItem.Click += (_, _) => _viewModel?.CopyOutputHistory(outputHistoryPanel.SelectedText);

        // As Update of OutputHistoryControllerBase: the end of the history is shown.
        outputHistory.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                outputHistory.CaretIndex = outputHistory.Text?.Length ?? 0;
            }
        };

        // As toolStripButtonLevelUp_ButtonClick.
        submodulesButton.Click += (_, _) => _viewModel?.GoUpOrShowSubmodules();

        // As userShell_Click: the button runs the default shell, its drop down the others.
        userShellButton.Click += (_, _) => _viewModel?.RunDefaultShell();

        // As MouseUpHandler of WorkingDirectoryToolStripSplitButton: a right click starts the "Open repository" dialog.
        workingDirButton.AddHandler(PointerReleasedEvent, (_, e) =>
        {
            if (e.InitialPressMouseButton == global::Avalonia.Input.MouseButton.Right)
            {
                _viewModel?.RunCommand.Execute(BrowseCommand.Open);
                e.Handled = true;
            }
        }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // The Navigate and View menus show the settings of the grid: they are built again once a command ran.
        mainMenu.Closed += (_, _) => Dispatcher.UIThread.Post(RefreshModelSubmenus);

        // As the DropDownOpening of the recent and favourite repositories: their items are read when the Start menu opens.
        mainMenu.AddHandler(MenuItem.SubmenuOpenedEvent, (_, e) =>
        {
            if (e.Source is MenuItem opened && _submenuOwners.Contains(opened))
            {
                RefreshSubmenus();
            }
        });
    }

    /// <summary>The revision to select first (<c>BrowseArguments.SelectedId</c>).</summary>
    public ObjectId? SelectedId { get; init; }

    /// <summary>The revision selected before <see cref="SelectedId"/> (<c>BrowseArguments.FirstId</c>).</summary>
    public ObjectId? FirstId { get; init; }

    public RevisionGridView RevisionGrid => revisionGrid;

    public Menu MainMenu => mainMenu;

    public TabControl Tabs => tabs;

    public DashboardView Dashboard => dashboard;

    /// <summary>
    ///  Shows another view model, e.g. for another repository (as <c>SetGitModule</c>); it is initialized at once if the window
    ///  is already shown.
    /// </summary>
    /// <param name="selectedId">The revision to select (<c>FormBrowse.SetWorkingDir</c>), else the current one.</param>
    /// <param name="firstId">With <paramref name="selectedId"/>, the revision selected first.</param>
    public void ShowViewModel(BrowseViewModel viewModel, ObjectId? selectedId = null, ObjectId? firstId = null)
    {
        DataContext = viewModel;
        if (_isOpened)
        {
            viewModel.Initialize(selectedId, firstId);
        }
    }

    /// <summary>"About Git Extensions" and "Settings" of the application menu of macOS run the commands of this window.</summary>
    private void AttachMacOSApplicationMenu()
    {
        if (!OperatingSystem.IsMacOS() || _viewModel is not { Strings: { } strings })
        {
            return;
        }

        MacOSApplicationMenu.Attach(
            $"{strings.About.PlainText} {strings.Title.PlainText}",
            () => _viewModel?.RunCommand.Execute(BrowseCommand.About),
            strings.Settings.PlainText,
            () => _viewModel?.RunCommand.Execute(BrowseCommand.Settings));
    }

    /// <summary>Reads the items of the recent and favourite repositories menus again.</summary>
    public void RefreshSubmenus()
    {
        if (_viewModel is null)
        {
            return;
        }

        foreach ((MenuItem item, BrowseSubmenu submenu) in _submenus)
        {
            item.ItemsSource = _viewModel.GetSubmenuItems(submenu).Select(CreateItem).ToList();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel?.MenusChanged -= OnMenusChanged;
        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.FocusRequested -= OnFocusRequested;
        _viewModel?.SubmodulesMenuRequested -= OnSubmodulesMenuRequested;
        _viewModel = DataContext as BrowseViewModel;
        _viewModel?.MenusChanged += OnMenusChanged;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.FocusRequested += OnFocusRequested;
        _viewModel?.SubmodulesMenuRequested += OnSubmodulesMenuRequested;
        if (_viewModel is null)
        {
            return;
        }

        // Another repository (SetGitModule) keeps the selected tab, as the tab control of FormBrowse.
        if (tabs.SelectedIndex >= 0)
        {
            _viewModel.SelectedTab = (BrowseTab)tabs.SelectedIndex;
        }

        BuildMainMenu();
        workingDirButton.Flyout = CreateWorkingDirectoryFlyout();
        worktreesButton.Flyout = CreateFlyout(_viewModel.WorktreeItems);
        submodulesButton.Flyout = CreateFlyout(_viewModel.SubmoduleItems);
        commitInfoPositionButton.Flyout = CreateFlyout(_viewModel.CommitInfoPositionItems);
        ApplyLayout();
        userShellButton.Flyout = CreateFlyout(_viewModel.ShellItems);
        FillScriptsToolBar(_viewModel.ScriptItems);
        pullButton.Flyout = CreateFlyout(_viewModel.PullItems);
        stashButton.Flyout = CreateFlyout(_viewModel.StashItems);
        FillFetchPullShortcuts(_viewModel.FetchPullShortcuts);
        AdaptSeparatorsVisibility();
    }

    // As FillDropDown of WorkingDirectoryToolStripSplitButton: the search box, then the repositories and the commands.
    private MenuFlyout CreateWorkingDirectoryFlyout()
    {
        IReadOnlyList<BrowseMenuItem> items = _viewModel!.GetWorkingDirectoryItems();
        MenuFlyout flyout = CreateFlyout(items);

        // The recent repositories are filtered: the items between the favourites and the commands.
        int first = items.ToList().FindIndex(item => item.IsSeparator) + 1;
        int last = items.ToList().FindIndex(first, item => item.IsSeparator);
        List<(MenuItem Item, string Text)> repositories = [];
        for (int i = first; i < last; i++)
        {
            if (flyout.Items[i] is MenuItem menuItem)
            {
                repositories.Add((menuItem, items[i].Header.Replace("__", "_")));
            }
        }

        TextBox search = new()
        {
            Name = "repositorySearch",
            PlaceholderText = _viewModel.ToolbarStrings.RepositorySearch.Text,
            MinWidth = 250,
        };
        search.PropertyChanged += (_, e) =>
        {
            if (e.Property != TextBox.TextProperty)
            {
                return;
            }

            foreach ((MenuItem item, string text) in repositories)
            {
                item.IsVisible = string.IsNullOrWhiteSpace(search.Text) || text.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase);
            }
        };
        flyout.Items.Insert(0, new MenuItem { Header = search, StaysOpenOnClick = true });
        flyout.Items.Insert(1, new Separator());

        // Cleared when the drop down opens, and focused to type at once.
        flyout.Opened += (_, _) =>
        {
            search.Text = "";
            search.Focus();
        };
        return flyout;
    }

    // As InsertFetchPullShortcuts: an image button for each action of the pull menu, before the pull button.
    private void FillFetchPullShortcuts(IReadOnlyList<BrowseToolbarShortcut> shortcuts)
    {
        fetchPullShortcuts.Children.Clear();
        foreach (BrowseToolbarShortcut shortcut in shortcuts)
        {
            Button button = new()
            {
                Name = shortcut.Key,
                Classes = { "toolbar" },
                Command = _viewModel!.RunCommand,
                CommandParameter = shortcut.Command,
                Content = new Image
                {
                    Source = (global::Avalonia.Media.IImage?)SettingsIconConverter.Instance.Convert(shortcut.Icon, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture),
                    Width = 16,
                    Height = 16,
                },
            };
            ToolTip.SetTip(button, shortcut.ToolTip);
            button.Bind(IsVisibleProperty, new global::Avalonia.Data.Binding($"ToolbarItems[{shortcut.Key}]"));
            button.PropertyChanged += OnToolbarChildPropertyChanged;
            fetchPullShortcuts.Children.Add(button);
        }
    }

    // As AdaptSeparatorsVisibility: a separator only between shown items.
    private void AdaptSeparatorsVisibility()
    {
        bool itemBefore = false;
        Separator? pending = null;
        foreach (Control child in toolbar.Children)
        {
            if (child is Separator separator)
            {
                separator.IsVisible = false;
                if (itemBefore)
                {
                    pending ??= separator;
                }

                continue;
            }

            if (IsShown(child))
            {
                pending?.IsVisible = true;
                pending = null;
                itemBefore = true;
            }
        }

        static bool IsShown(Control control)
            => control.IsVisible && (control is not Panel panel || panel.Children.Any(IsShown));
    }

    private void OnToolbarChildPropertyChanged(object? sender, global::Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && sender is not Separator)
        {
            AdaptSeparatorsVisibility();
        }
    }

    // As the context menu of the toolbars (ShowToolStripContextMenu), on the free space of the toolbar.
    private void OnToolbarContextRequested(object? sender, global::Avalonia.Input.ContextRequestedEventArgs e)
    {
        if (_viewModel is null || e.Source != toolbar)
        {
            return;
        }

        ContextMenu menu = new() { ItemsSource = _viewModel.GetToolbarsMenuItems().Select(CreateItem).ToList() };
        menu.Open(toolbar);
        e.Handled = true;
    }

    // As LoadUserMenu: a button with the icon and the name of each script.
    private void FillScriptsToolBar(IReadOnlyList<BrowseMenuItem> scripts)
    {
        scriptsToolBar.Children.Clear();
        foreach (BrowseMenuItem script in scripts)
        {
            StackPanel content = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
            if (script.Icon is not null && SettingsIconConverter.Instance.Convert(script.Icon, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is global::Avalonia.Media.IImage icon)
            {
                content.Children.Add(new Image { Source = icon, Width = 16, Height = 16 });
            }

            content.Children.Add(new TextBlock { Text = script.Header.Replace("__", "_"), VerticalAlignment = VerticalAlignment.Center });
            Button button = new() { Content = content };
            button.Classes.Add("toolbar");
            button.Click += (_, _) => script.Invoke?.Invoke();
            scriptsToolBar.Children.Add(button);
        }
    }

    // The worktrees are read in the background (UpdateWorktreeToolStripVisibility).
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BrowseViewModel.WorktreeItems) && _viewModel is not null)
        {
            worktreesButton.Flyout = CreateFlyout(_viewModel.WorktreeItems);
        }

        if (e.PropertyName == nameof(BrowseViewModel.SubmoduleItems) && _viewModel is not null)
        {
            submodulesButton.Flyout = CreateFlyout(_viewModel.SubmoduleItems);
        }

        if (e.PropertyName == nameof(BrowseViewModel.PullItems) && _viewModel is not null)
        {
            pullButton.Flyout = CreateFlyout(_viewModel.PullItems);
        }

        if (e.PropertyName is nameof(BrowseViewModel.ShowSplitViewLayout) or nameof(BrowseViewModel.CommitInfoPosition) or nameof(BrowseViewModel.ShowTabs))
        {
            ApplyLayout();
        }

        // E.g. the hotkeys that show a tab (FocusDiff, FocusNextTab).
        if (e.PropertyName == nameof(BrowseViewModel.SelectedTab) && _viewModel is not null && tabs.SelectedIndex != (int)_viewModel.SelectedTab)
        {
            tabs.SelectedIndex = (int)_viewModel.SelectedTab;
        }
    }

    // As FormBrowse.CancelButtonClick: Escape does not close the main window.
    protected override void OnEscapePressed() => _viewModel?.CancelByEscape();

    // As RefreshSplitViewLayout and LayoutRevisionInfo: the tabs below the grid, and the commit info in its tab or beside the grid.
    private void ApplyLayout()
    {
        if (_viewModel is null)
        {
            return;
        }

        RowDefinition tabsRow = contentGrid.RowDefinitions[2];
        if (_viewModel.ShowTabs)
        {
            if (tabsRow.Height.Value == 0)
            {
                tabsRow.Height = new GridLength(_tabsHeight);
            }
        }
        else
        {
            if (tabsRow.Height.Value > 0)
            {
                _tabsHeight = tabsRow.Height.Value;
            }

            tabsRow.Height = new GridLength(0);
        }

        ContentControl? side = _viewModel.CommitInfoPosition switch
        {
            GitCommands.CommitInfoPosition.LeftwardFromList => leftCommitInfoHost,
            GitCommands.CommitInfoPosition.RightwardFromList => rightCommitInfoHost,
            _ => null,
        };
        Control? target = side ?? commitTab;
        if (commitInfoBorder.Parent != target)
        {
            // Moved from its tab or side to the other place.
            switch (commitInfoBorder.Parent)
            {
                case ContentControl contentControl:
                    contentControl.Content = null;
                    break;
            }

            if (side is null)
            {
                commitTab.Content = commitInfoBorder;
            }
            else
            {
                side.Content = commitInfoBorder;
            }
        }

        leftCommitInfoHost.IsVisible = leftCommitInfoSplitter.IsVisible = side == leftCommitInfoHost;
        rightCommitInfoHost.IsVisible = rightCommitInfoSplitter.IsVisible = side == rightCommitInfoHost;

        // The hidden side first: its width is kept for the shown one.
        (ColumnDefinition shownColumn, ColumnDefinition hiddenColumn) = side == leftCommitInfoHost
            ? (gridArea.ColumnDefinitions[0], gridArea.ColumnDefinitions[4])
            : (gridArea.ColumnDefinitions[4], gridArea.ColumnDefinitions[0]);
        SetSideWidth(hiddenColumn, shown: false);
        SetSideWidth(shownColumn, shown: side is not null);
    }

    // The width of the commit info beside the grid, kept when it moves to the other side or to its tab.
    private void SetSideWidth(ColumnDefinition column, bool shown)
    {
        if (shown)
        {
            column.Width = new GridLength(_commitInfoWidth);
        }
        else if (column.Width.Value > 0)
        {
            _commitInfoWidth = column.Width.Value;
            column.Width = new GridLength(0);
        }
    }

    private void OnSubmodulesMenuRequested(object? sender, EventArgs e) => submodulesButton.Flyout?.ShowAt(submodulesButton);

    // As FocusLeftPanel, RevisionGrid.Focus and ToolStripFilters.SetFocus of the hotkeys.
    private void OnFocusRequested(object? sender, BrowseFocusTarget target)
    {
        switch (target)
        {
            case BrowseFocusTarget.LeftPanel when leftPanel.IsVisible:
                leftPanel.Focus();
                break;
            case BrowseFocusTarget.RevisionGrid:
                revisionGrid.Focus();
                break;
            case BrowseFocusTarget.Filter:
                filterToolBar.FocusFilter();
                break;
            case BrowseFocusTarget.CommitInfo:
                commitInfo.Focus();
                break;
            case BrowseFocusTarget.OutputHistory:
                outputHistoryPanel.Focus();
                break;
        }
    }

    // As RegisterPlugins: the menus are built again, e.g. with the plugins once they are loaded.
    private void OnMenusChanged(object? sender, EventArgs e) => BuildMainMenu();

    private void BuildMainMenu()
    {
        _submenus.Clear();
        _submenuOwners.Clear();
        _modelSubmenus.Clear();
        mainMenu.ItemsSource = _viewModel!.Menus.Select(CreateItem).ToList();
        RefreshModelSubmenus();
        BuildMacOSMenu();
    }

    private void RefreshModelSubmenus()
    {
        if (_viewModel is null)
        {
            return;
        }

        foreach ((MenuItem item, BrowseSubmenu submenu) in _modelSubmenus)
        {
            List<Control> items = MenuModelRenderer.CreateItems(_viewModel.GetModelSubmenuItems(submenu));
            if (submenu == BrowseSubmenu.View)
            {
                if (items.Count > 0)
                {
                    items.Add(new Separator());
                }

                items.Add(CreateItem(_viewModel.ToolbarsMenu));
            }

            item.ItemsSource = items;
            item.IsVisible = items.Count > 0;
        }
    }

    private MenuFlyout CreateFlyout(IReadOnlyList<BrowseMenuItem> items)
    {
        // The items are created at once: items added when a MenuFlyout opens are not shown.
        MenuFlyout flyout = new();
        foreach (Control item in items.Select(CreateItem))
        {
            flyout.Items.Add(item);
        }

        return flyout;
    }

    private Control CreateItem(BrowseMenuItem item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        MenuItem menuItem = new() { Header = CreateHeader(item), IsEnabled = item.IsEnabled };
        if (item.IsChecked is bool isChecked)
        {
            menuItem.ToggleType = MenuItemToggleType.CheckBox;
            menuItem.IsChecked = isChecked;
        }

        if (item.Icon is not null && SettingsIconConverter.Instance.Convert(item.Icon, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is { } icon)
        {
            menuItem.Icon = new Image { Source = (global::Avalonia.Media.IImage)icon, Width = 16, Height = 16 };
        }

        if (item.ToolTip is { } toolTip)
        {
            ToolTip.SetTip(menuItem, toolTip);
        }

        if (item.Submenu is BrowseSubmenu.Navigate or BrowseSubmenu.View)
        {
            _modelSubmenus.Add((menuItem, item.Submenu.Value));
        }
        else if (item.Submenu is BrowseSubmenu submenu)
        {
            _submenus.Add((menuItem, submenu));
        }
        else if (item.Children is { } children)
        {
            menuItem.ItemsSource = children.Select(CreateItem).ToList();
            if (children.Any(c => c.Submenu is not null))
            {
                _submenuOwners.Add(menuItem);
            }
        }
        else if (item.Command is BrowseCommand command)
        {
            menuItem.Command = _viewModel!.RunCommand;
            menuItem.CommandParameter = command;
        }
        else if (item.Invoke is { } invoke)
        {
            menuItem.Click += (_, _) => invoke();
        }

        return menuItem;
    }

    // The header, with the text of ShortcutKeyDisplayString on the right (the branch of a recent repository).
    private static object CreateHeader(BrowseMenuItem item)
    {
        if (item.Shortcut is not { Length: > 0 } shortcut)
        {
            return item.Header;
        }

        Grid header = new() { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        header.Children.Add(new AccessText { Text = item.Header, VerticalAlignment = VerticalAlignment.Center });
        TextBlock shortcutText = new() { Text = shortcut, Opacity = 0.6, Margin = new global::Avalonia.Thickness(24, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(shortcutText, 1);
        header.Children.Add(shortcutText);
        return header;
    }
}
