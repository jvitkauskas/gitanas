using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using GitCommands;
using GitUI.Avalonia.Controls.FlatTree;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.Avalonia.Controls.FileStatusList;

/// <summary>
///  The Avalonia file status list (port of the WinForms <c>FileStatusList</c>; docs/avalonia-port/PLAN.md, phase 5):
///  the files as a tree with their status icons, multiple selection, the filter and the sorting.
/// </summary>
public partial class FileStatusListView : UserControl
{
    /// <summary>How the list reads the nodes of the tree.</summary>
    private static readonly FlatTreeAdapter NodeAdapter = new(
        GetChildren: node => ((FileStatusNode)node).Children,
        ChildrenPropertyName: nameof(FileStatusNode.Children),
        IsExpandedPropertyName: nameof(FileStatusNode.IsExpanded),
        IsExpanded: node => ((FileStatusNode)node).IsExpanded,
        SetExpanded: (node, isExpanded) => ((FileStatusNode)node).IsExpanded = isExpanded);

    private FlatTreeList? _tree;

    // The items of the toolbar hidden by the user (Settings > Toolbar), over the visibility they have otherwise.
    private readonly Dictionary<Control, IDisposable?> _hiddenToolbarItems = [];

    public FileStatusListView()
    {
        InitializeComponent();

        // As ItemContextMenu_Opening: the items for the selection.
        treeMenu.Opening += (_, _) =>
        {
            if (DataContext is FileStatusListViewModel viewModel)
            {
                viewModel.UpdateMenuState();
                InsertDirectScripts(viewModel);
                FillCustomDiffTools(viewModel);
            }
        };

        CreateToolbarMenu();

        // As AddToSearchFilter: the expression in the history once the box is left or Enter pressed.
        gitGrepBox.LostFocus += (_, _) => (DataContext as FileStatusListViewModel)?.AddGitGrepHistory(gitGrepBox.Text ?? "");
        gitGrepBox.AddHandler(
            KeyDownEvent,
            (_, e) =>
            {
                if (e.Key == global::Avalonia.Input.Key.Enter)
                {
                    (DataContext as FileStatusListViewModel)?.AddGitGrepHistory(gitGrepBox.Text ?? "");
                }
            },
            global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // The expressions searched before, searched again when chosen.
        FreshMenuFlyout.ShowOnClick(
            gitGrepHistoryButton,
            () => DataContext is FileStatusListViewModel viewModel
                ? [.. viewModel.GitGrepHistory.Select(expression => new MenuItem { Header = expression, Command = viewModel.SearchGitGrepCommand, CommandParameter = expression })]
                : [],
            PlacementMode.BottomEdgeAlignedRight);

        filterTextBox.LostFocus += (_, _) => (DataContext as FileStatusListViewModel)?.RememberFilter();
        filterTextBox.AddHandler(
            KeyDownEvent,
            (_, e) =>
            {
                if (e.Key == global::Avalonia.Input.Key.Enter && e.KeyModifiers == global::Avalonia.Input.KeyModifiers.None)
                {
                    (DataContext as FileStatusListViewModel)?.RememberFilter();
                    e.Handled = true;
                }
                else if (e.Key == global::Avalonia.Input.Key.F4 && e.KeyModifiers == global::Avalonia.Input.KeyModifiers.None)
                {
                    // Alt+Down belongs to the containing dialog's next-file/change navigation.
                    ShowFilterHistory();
                    e.Handled = true;
                }
            },
            global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        filterHistoryButton.Click += (_, _) => ShowFilterHistory();

        // As SetFindInCommitFilesGitGrepVisibilityImpl: the box has the focus when it is shown.
        gitGrepBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == IsVisibleProperty && e.NewValue is true)
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => gitGrepBox.Focus(), global::Avalonia.Threading.DispatcherPriority.Input);
            }
        };

        // A double click on a file activates the selection (a folder expands instead, by the FlatTreeList).
        filesTree.DoubleTapped += (_, e) =>
        {
            if (DataContext is FileStatusListViewModel viewModel
                && (e.Source as global::Avalonia.StyledElement)?.DataContext is FileStatusNode { Entry: not null } or FlatTreeRow { Node: FileStatusNode { Entry: not null } })
            {
                viewModel.ActivateSelection();
            }
        };
    }

    private void ShowFilterHistory()
    {
        if (DataContext is not FileStatusListViewModel viewModel)
        {
            return;
        }

        viewModel.RememberFilter();
        MenuFlyout? flyout = FreshMenuFlyout.Show(
            filterHistoryButton,
            viewModel.FilterHistory.Select(expression => new MenuItem
            {
                Header = expression.Replace("_", "__"),
                Command = viewModel.ApplyFilterCommand,
                CommandParameter = expression,
            }),
            PlacementMode.BottomEdgeAlignedRight);
        if (flyout is not null)
        {
            flyout.Closed += (_, _) => filterTextBox.Focus();
        }
    }

    /// <summary>The context menu, e.g. for tests.</summary>
    public ContextMenu Menu => treeMenu;

    /// <summary>The toolbar, e.g. for tests.</summary>
    public StackPanel Toolbar => toolbar;

    /// <summary>
    ///  As the Toolbar item of the settings (<c>UpdateToolbar</c>): an item for each item of the toolbar, which hides or shows it,
    ///  saved as <c>FileStatusList.Toolbar.Visibility.&lt;name&gt;</c>. The settings button itself stays.
    /// </summary>
    private void CreateToolbarMenu()
    {
        foreach (Control item in toolbar.Children)
        {
            string settingsKey = $"FileStatusList.Toolbar.Visibility.{item.Name}";
            bool visible = AppSettings.GetBool(settingsKey, defaultValue: true);
            MenuItem menuItem = new()
            {
                Header = ToolTip.GetTip(item) is { } tip ? tip : item.Name,
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = visible,
                IsEnabled = item != btnSettings,
                Icon = (item as ContentControl)?.Content is Image { Source: { } source } ? new Image { Source = source, Width = 16, Height = 16 }
                    : (item as ContentControl)?.Content is Panel panel && panel.Children.OfType<Image>().FirstOrDefault() is { Source: { } first } ? new Image { Source = first, Width = 16, Height = 16 }
                    : null,
            };

            // The tooltips are bound: the header follows them.
            item.PropertyChanged += (_, e) =>
            {
                if (e.Property == ToolTip.TipProperty && e.NewValue is { } tip)
                {
                    menuItem.Header = tip;
                }
            };
            menuItem.Click += (_, _) =>
            {
                bool show = menuItem.IsChecked;
                AppSettings.SetBool(settingsKey, show ? null : false);
                SetToolbarItemHidden(item, !show);
            };
            toolbarMenuItem.Items.Add(menuItem);
            SetToolbarItemHidden(item, !visible);
        }
    }

    private void SetToolbarItemHidden(Control item, bool hidden)
    {
        if (_hiddenToolbarItems.Remove(item, out IDisposable? hiding))
        {
            hiding?.Dispose();
        }

        if (hidden)
        {
            _hiddenToolbarItems[item] = item.SetValue(Visual.IsVisibleProperty, false, BindingPriority.Animation);
        }
    }

    /// <summary>The list of the visible nodes of the tree, e.g. for tests.</summary>
    public ListBox Tree => filesTree;

    /// <summary>The visible nodes of the tree and the selection, e.g. for tests.</summary>
    public FlatTreeList? FlatTree => _tree;

    /// <summary>
    ///  As <c>LoadCustomDifftools</c>: the difftool items have a submenu with the difftools configured in git, the first (the
    ///  default) bold, and "Disable this dropdown".
    /// </summary>
    private void FillCustomDiffTools(FileStatusListViewModel viewModel)
    {
        (MenuItem Item, Func<string, (System.Windows.Input.ICommand Command, object Parameter)> Open)[] items =
        [
            (diffFirstToSelectedMenuItem, tool => (viewModel.OpenWithCustomDifftoolCommand, new DifftoolChoice(DifftoolKind.FirstToSelected, tool))),
            (diffSelectedToLocalMenuItem, tool => (viewModel.OpenWithCustomDifftoolCommand, new DifftoolChoice(DifftoolKind.SelectedToLocal, tool))),
            (diffFirstToLocalMenuItem, tool => (viewModel.OpenWithCustomDifftoolCommand, new DifftoolChoice(DifftoolKind.FirstToLocal, tool))),
            (diffWithRememberedMenuItem, tool => (viewModel.DiffWithRememberedCustomCommand, tool)),
            (diffTwoSelectedMenuItem, tool => (viewModel.DiffTwoSelectedCustomCommand, tool)),
        ];
        foreach ((MenuItem item, Func<string, (System.Windows.Input.ICommand Command, object Parameter)> open) in items)
        {
            item.Items.Clear();
            if (viewModel.CustomDiffTools.Count <= 1)
            {
                continue;
            }

            for (int index = 0; index < viewModel.CustomDiffTools.Count; index++)
            {
                string tool = viewModel.CustomDiffTools[index];
                (System.Windows.Input.ICommand command, object parameter) = open(tool);
                item.Items.Add(new MenuItem
                {
                    Header = tool,
                    Command = command,
                    CommandParameter = parameter,
                    FontWeight = index == 0 ? global::Avalonia.Media.FontWeight.Bold : global::Avalonia.Media.FontWeight.Normal,
                });
            }

            item.Items.Add(new Separator());
            item.Items.Add(new MenuItem { Header = viewModel.ToolbarStrings.DisableCustomDiffTools.Text, Command = viewModel.DisableCustomDiffToolsCommand });
        }
    }

    // As AddUserScripts: the scripts of ScriptEvent.ShowInFileList follow "Run script" in the menu itself, the others are under it.
    private void InsertDirectScripts(FileStatusListViewModel viewModel)
    {
        foreach (MenuItem item in treeMenu.Items.OfType<MenuItem>().Where(item => item.Tag is FileStatusScript).ToList())
        {
            treeMenu.Items.Remove(item);
        }

        runScriptMenuItem.Items.Clear();
        int index = treeMenu.Items.IndexOf(runScriptMenuItem);
        foreach (FileStatusScript script in viewModel.MenuState.Scripts)
        {
            MenuItem item = new()
            {
                Header = script.Name.Replace("_", "__"),
                Tag = script,
                Command = viewModel.RunScriptCommand,
                CommandParameter = script,
            };
            if (script.IsDirect)
            {
                treeMenu.Items.Insert(++index, item);
            }
            else
            {
                runScriptMenuItem.Items.Add(item);
            }
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // The list adds and removes the nodes the user selects, and follows the selection of the view model.
        _tree?.Dispose();
        _tree = null;
        if (DataContext is FileStatusListViewModel viewModel)
        {
            _tree = new FlatTreeList(filesTree, viewModel.Nodes, NodeAdapter);
            _tree.SyncSelection(viewModel.SelectedNodes);
        }
    }
}
