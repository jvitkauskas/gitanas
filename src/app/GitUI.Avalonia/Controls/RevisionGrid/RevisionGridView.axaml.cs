using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtensions.Extensibility.BuildServerIntegration;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.Controls.RevisionGrid;

/// <summary>
///  The Avalonia revision grid (port of <c>RevisionGridControl</c>'s grid; docs/avalonia-port/PLAN.md, phase 4):
///  the graph, the references and subject, the notes, the avatar, the author, the date, the commit id and the build
///  status of each revision, with the "RevisionGrid" hotkeys.
/// </summary>
public partial class RevisionGridView : UserControl, IHotkeyControl
{
    public static readonly StyledProperty<RevisionGraph?> GraphProperty =
        AvaloniaProperty.Register<RevisionGridView, RevisionGraph?>(nameof(Graph));

    public static readonly StyledProperty<RevisionGraphRenderer?> RendererProperty =
        AvaloniaProperty.Register<RevisionGridView, RevisionGraphRenderer?>(nameof(Renderer));

    public static readonly StyledProperty<RevisionGridViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<RevisionGridView, RevisionGridViewModel?>(nameof(ViewModel));

    // The columns, in the order of RevisionGridControl.
    private const int GraphColumn = 0;
    private const int NotesColumn = 2;
    private const int AvatarColumn = 3;
    private const int AuthorColumn = 4;
    private const int DateColumn = 5;
    private const int IdColumn = 6;
    private const int BuildStatusColumn = 7;

    private RevisionGridViewModel? _viewModel;
    private RevisionRefItem? _hoveredRef;
    private int _maxLaneCount = 1;
    private int _laneCountScannedTo;
    private readonly DispatcherTimer _quickSearchTimer = new();

    public RevisionGridView()
    {
        InitializeComponent();

        revisionsGrid.LoadingRow += (_, e) =>
        {
            _viewModel?.EnsureGraphCached(e.Row.Index + 50);
            UpdateRow(e.Row);
        };
        revisionsGrid.DoubleTapped += (_, e) => ActivateSelected(e.Source);

        // Before the grid, which moves to the next row on Enter.
        revisionsGrid.AddHandler(KeyDownEvent, OnGridKeyDown, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        revisionsGrid.HeadersVisibility = DataGridHeadersVisibility.None;
        revisionsGrid.SelectionChanged += (_, _) =>
        {
            _viewModel?.SetSelectedRows(revisionsGrid.SelectedItems.OfType<RevisionGridRow>());

            // The author to highlight may have changed.
            UpdateRealizedRows();
        };

        // As mainContextMenu: the menu of the selected revisions, built when opening; a right click selects the row first.
        revisionsGrid.ContextRequested += OnContextRequested;
        revisionsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        revisionsGrid.AddHandler(PointerReleasedEvent, OnGridPointerReleased, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // As OnGridViewCellMouseMove and OnGridViewCellMouseLeave: the ancestry of the hovered reference label is highlighted.
        revisionsGrid.PointerMoved += (_, e) => SetHoveredRef(GetRefItem(e.Source));
        revisionsGrid.PointerExited += (_, _) => SetHoveredRef(null);

        // Quick search (as RevisionGridControl with QuickSearchProvider).
        revisionsGrid.AddHandler(TextInputEvent, OnGridTextInput, handledEventsToo: true);
        _quickSearchTimer.Tick += (_, _) =>
        {
            _quickSearchTimer.Stop();
            _viewModel?.HideQuickSearch();
        };
    }

    /// <summary>Raised when a revision is double clicked or Enter is pressed on it.</summary>
    public event EventHandler<RevisionGridRow>? RevisionActivated;

    public RevisionGraph? Graph
    {
        get => GetValue(GraphProperty);
        private set => SetValue(GraphProperty, value);
    }

    public RevisionGraphRenderer? Renderer
    {
        get => GetValue(RendererProperty);
        private set => SetValue(RendererProperty, value);
    }

    /// <summary>The view model (the data context), for the bindings of the cells to the settings of the grid.</summary>
    public RevisionGridViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        private set => SetValue(ViewModelProperty, value);
    }

    /// <summary>The draw style of the graph (<c>RevisionGraphColumnProvider.RevisionGraphDrawStyle</c>).</summary>
    public RevisionGraphDrawStyle DrawStyle
        => _viewModel is null ? RevisionGraphDrawStyle.Normal
            : _viewModel.IsBranchHighlighted ? RevisionGraphDrawStyle.HighlightSelected
            : _viewModel.DrawNonRelativesGray ? RevisionGraphDrawStyle.DrawNonRelativesGray
            : RevisionGraphDrawStyle.Normal;

    /// <summary>As <c>ProcessHotkey</c>: the "RevisionGrid" hotkeys, while the grid has the focus.</summary>
    public bool ProcessHotkey(int keyData)
    {
        if (_viewModel?.Hotkeys.FirstOrDefault(h => h.KeyData == keyData) is not { } hotkey)
        {
            return false;
        }

        return _viewModel.ExecuteHotkey((RevisionGridCommand)hotkey.CommandCode);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Renderer = CreateRenderer();

        // The background of the rows of the highlighted author (AppColor.AuthoredHighlight of the theme, else its default).
        Resources["AuthoredRowBrush"] = AppColorResources.GetBrush(this, AppColor.AuthoredHighlight) ?? Brushes.Transparent;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.QuickSearchRestarted -= OnQuickSearchRestarted;
        _viewModel?.RelativesChanged -= OnRelativesChanged;
        _viewModel?.RowsSelectionRequested -= OnRowsSelectionRequested;
        _viewModel = DataContext as RevisionGridViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.QuickSearchRestarted += OnQuickSearchRestarted;
        _viewModel?.RelativesChanged += OnRelativesChanged;
        _viewModel?.RowsSelectionRequested += OnRowsSelectionRequested;
        ViewModel = _viewModel;
        Graph = _viewModel?.Graph;
        UpdateColumns();
        revisionsGrid.SelectionMode = _viewModel?.MultiSelect == true ? DataGridSelectionMode.Extended : DataGridSelectionMode.Single;
        _maxLaneCount = 1;
        _laneCountScannedTo = 0;
        base.OnDataContextChanged(e);
    }

    /// <summary>The rows selected in this order (the first one the base of a diff); the last one is shown.</summary>
    private void OnRowsSelectionRequested(object? sender, IReadOnlyList<RevisionGridRow> rows)
    {
        revisionsGrid.SelectedItems.Clear();
        foreach (RevisionGridRow row in rows)
        {
            revisionsGrid.SelectedItems.Add(row);
        }

        revisionsGrid.ScrollIntoView(rows[^1], column: null);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RevisionGridViewModel.CachedGraphRowCount):
                UpdateGraphColumnWidth();
                OnRelativesChanged(this, EventArgs.Empty);
                break;

            case nameof(RevisionGridViewModel.DrawNonRelativesGray) or nameof(RevisionGridViewModel.IsBranchHighlighted)
                or nameof(RevisionGridViewModel.HoverHighlightedIds):
                InvalidateGraph();
                break;

            case nameof(RevisionGridViewModel.DrawNonRelativesTextGray) or nameof(RevisionGridViewModel.HighlightAuthoredRevisions):
                UpdateRealizedRows();
                break;

            case nameof(RevisionGridViewModel.ShowGraphColumn) or nameof(RevisionGridViewModel.ShowAuthorColumn)
                or nameof(RevisionGridViewModel.ShowDateColumn) or nameof(RevisionGridViewModel.ShowIdColumn)
                or nameof(RevisionGridViewModel.ShowNotesColumn) or nameof(RevisionGridViewModel.ShowAvatarColumn)
                or nameof(RevisionGridViewModel.ShowBuildStatusColumn) or nameof(RevisionGridViewModel.ShowBuildStatusIcon)
                or nameof(RevisionGridViewModel.ShowBuildStatusText):
                UpdateColumns();
                UpdateRealizedRows();
                break;

            case nameof(RevisionGridViewModel.SelectedRow) when _viewModel?.SelectedRow is { } row:
                revisionsGrid.ScrollIntoView(row, null);
                break;
        }
    }

    /// <summary>Widens the graph column to the lanes laid out so far (the WinForms column also grows with them).</summary>
    private void UpdateGraphColumnWidth()
    {
        if (Graph is not { } graph || _viewModel is null)
        {
            return;
        }

        if (_viewModel.CachedGraphRowCount < _laneCountScannedTo)
        {
            // Reloaded.
            _maxLaneCount = 1;
            _laneCountScannedTo = 0;
        }

        // Rendering caps the width at MaxLanes. Beyond that, querying more rows only builds unused lane dictionaries.
        for (int index = _laneCountScannedTo; index < _viewModel.CachedGraphRowCount && _maxLaneCount < RevisionGraph.MaxLanes; index++)
        {
            if (graph.GetSegmentsForRow(index) is { } row)
            {
                _maxLaneCount = Math.Max(_maxLaneCount, row.GetLaneCount());
            }
        }

        _laneCountScannedTo = _viewModel.CachedGraphRowCount;

        revisionsGrid.Columns[GraphColumn].Width = new DataGridLength(RevisionGraphRenderer.GetWidth(_maxLaneCount) + 4);
    }

    private void InvalidateGraph()
    {
        foreach (RevisionGraphCell cell in revisionsGrid.GetVisualDescendants().OfType<RevisionGraphCell>())
        {
            cell.InvalidateVisual();
        }
    }

    // The relatives changed (more of the graph laid out, or a highlighted branch): the graph and the gray texts.
    private void OnRelativesChanged(object? sender, EventArgs e)
    {
        InvalidateGraph();
        UpdateRealizedRows();
    }

    private void UpdateRealizedRows()
    {
        foreach (DataGridRow row in revisionsGrid.GetVisualDescendants().OfType<DataGridRow>())
        {
            UpdateRow(row);
        }
    }

    // As RevisionDataGridView.OnCellPainting: the background of the highlighted author and the gray texts of the
    // non-relatives; the avatar is loaded when the row is shown.
    private void UpdateRow(DataGridRow row)
    {
        if (_viewModel is null || row.DataContext is not RevisionGridRow item)
        {
            return;
        }

        // Not for the selected rows, whose selection background shows (a :not(:selected) selector is not re-evaluated for the cells).
        row.Classes.Set("authored", _viewModel.IsAuthoredHighlight(item) && !revisionsGrid.SelectedItems.Contains(item));
        row.Classes.Set("nonRelative", _viewModel.IsTextGray(item.Index));
        _viewModel.RequestAvatar(item);
    }

    /// <summary>The context menu opened last, e.g. for tests.</summary>
    public ContextMenu? LastContextMenu { get; private set; }

    /// <summary>Opens the context menu of the selected revisions (as the menu key or a right click).</summary>
    public void OpenContextMenu()
    {
        if (_viewModel?.ContextMenuProvider is not { } provider)
        {
            return;
        }

        LastContextMenu = MenuModelRenderer.CreateContextMenu(provider());
        LastContextMenu.Open(revisionsGrid);
    }

    // The columns, as the column visibility of RevisionGridControl (the graph, author name, date and id columns).
    private void UpdateColumns()
    {
        if (_viewModel is null)
        {
            return;
        }

        revisionsGrid.Columns[GraphColumn].IsVisible = _viewModel.ShowGraphColumn;
        revisionsGrid.Columns[NotesColumn].IsVisible = _viewModel.ShowNotesColumn;
        revisionsGrid.Columns[AvatarColumn].IsVisible = _viewModel.ShowAvatarColumn;
        revisionsGrid.Columns[AuthorColumn].IsVisible = _viewModel.ShowAuthorColumn;
        revisionsGrid.Columns[DateColumn].IsVisible = _viewModel.ShowDateColumn;
        revisionsGrid.Columns[IdColumn].IsVisible = _viewModel.ShowIdColumn;

        // As BuildStatusColumnProvider.ApplySettings: the width of the icon only, else resizable for the text.
        DataGridColumn buildStatus = revisionsGrid.Columns[BuildStatusColumn];
        buildStatus.IsVisible = _viewModel.ShowBuildStatusColumn && (_viewModel.ShowBuildStatusIcon || _viewModel.ShowBuildStatusText);
        buildStatus.CanUserResize = _viewModel.ShowBuildStatusText;
        buildStatus.Width = new DataGridLength(_viewModel.ShowBuildStatusText ? 150 : 24);
    }

    /// <summary>The reference label under the pointer, if any.</summary>
    private static RevisionRefItem? GetRefItem(object? source)
        => (source as Visual)?.GetSelfAndVisualAncestors().OfType<Border>().FirstOrDefault(b => b.Classes.Contains("ref"))?.DataContext as RevisionRefItem;

    private void SetHoveredRef(RevisionRefItem? item)
    {
        if (_viewModel is null || ReferenceEquals(item, _hoveredRef))
        {
            return;
        }

        _hoveredRef = item;
        int rowIndex = item is null ? -1 : _viewModel.Rows.FirstOrDefault(r => r.Refs.Any(i => ReferenceEquals(i, item) || ReferenceEquals(i.Nested, item)))?.Index ?? -1;
        List<int> shown = [.. revisionsGrid.GetVisualDescendants().OfType<DataGridRow>().Where(r => r.IsVisible).Select(r => r.Index).Where(i => i >= 0)];
        int first = shown.Count == 0 ? 0 : shown.Min();
        int count = shown.Count == 0 ? 0 : shown.Max() - first + 1;
        _ = _viewModel.SetHoverReferenceAsync(item?.GitRef, rowIndex, first, count);
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (_viewModel?.ContextMenuProvider is null || ContextMenu is not null)
        {
            // The menu set by the window (e.g. the file history), if any.
            return;
        }

        e.Handled = true;
        if (_viewModel.SelectedRow is not null)
        {
            OpenContextMenu();
        }
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPointProperties properties = e.GetCurrentPoint(revisionsGrid).Properties;
        if (properties.IsXButton1Pressed || properties.IsXButton2Pressed)
        {
            // As OnGridViewMouseClick: the mouse back and forward buttons navigate.
            e.Handled = true;
            if (properties.IsXButton1Pressed)
            {
                _viewModel?.NavigateBackward();
            }
            else
            {
                _viewModel?.NavigateForward();
            }

            return;
        }

        if (properties.IsLeftButtonPressed
            && (e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<Control>().FirstOrDefault(c => c.Name == "buildStatus")?.DataContext is RevisionGridRow clicked
            && clicked.HasBuildReport)
        {
            // As OnGridViewCellMouseDown: a click on the build status opens its report.
            _viewModel?.OpenBuildReport(clicked);
        }

        if (!properties.IsRightButtonPressed)
        {
            return;
        }

        // As _rightClickedHitInfo: the menu of a right-clicked reference label is focused on it.
        _viewModel?.RefMenuRequest = GetRefItem(e.Source)?.GitRef is { } gitRef
            ? new RevisionGridRefMenuRequest(gitRef, e.KeyModifiers.HasFlag(KeyModifiers.Shift), e.KeyModifiers.HasFlag(KeyModifiers.Control))
            : null;

        if ((e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true)?.DataContext is RevisionGridRow row
            && !revisionsGrid.SelectedItems.Contains(row))
        {
            revisionsGrid.SelectedItem = row;
        }
    }

    // As OnGridViewMouseClick: Alt+click highlights the branch of the clicked revision.
    private void OnGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            Dispatcher.UIThread.Post(() => _viewModel?.HighlightSelectedBranch());
        }
    }

    private void OnQuickSearchRestarted(object? sender, EventArgs e)
    {
        _quickSearchTimer.Stop();
        _quickSearchTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, _viewModel?.QuickSearchTimeout ?? 4000));
        _quickSearchTimer.Start();
    }

    private void OnGridTextInput(object? sender, TextInputEventArgs e)
    {
        if (_viewModel is not null && !string.IsNullOrEmpty(e.Text) && !e.Text.Any(char.IsControl))
        {
            e.Handled = true;
            _viewModel.QuickSearchType(e.Text);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key, e.KeyModifiers)
        {
            case (Key.Enter, KeyModifiers.None):
                e.Handled = true;
                ActivateSelected(null);
                break;

            case (Key.Back, KeyModifiers.None) when _viewModel?.IsQuickSearchVisible == true:
                e.Handled = true;
                _viewModel.QuickSearchBackspace();
                break;

            case (Key.Escape, KeyModifiers.None) when _viewModel?.IsQuickSearchVisible == true:
                e.Handled = true;
                _viewModel.HideQuickSearch();
                break;

            case (Key.Down, KeyModifiers.Alt) or (Key.Up, KeyModifiers.Alt) when _viewModel is not null:
                e.Handled = true;
                _viewModel.QuickSearchNext(down: e.Key == Key.Down);
                break;

            case (Key.V, var modifiers) when modifiers == KeyMapping.CommandModifier && _viewModel is not null:
                e.Handled = true;
                _ = PasteIntoQuickSearchAsync(_viewModel);
                break;

            // As RevisionDataGridView.OnKeyDown: Ctrl+C copies the hashes of the selected commits (the DataGrid would copy
            // the text of its template cells, which is empty).
            case (Key.C, var modifiers) when modifiers == KeyMapping.CommandModifier && _viewModel is not null:
                e.Handled = true;
                _ = CopySelectedHashesAsync(_viewModel);
                break;
        }
    }

    private async Task CopySelectedHashesAsync(RevisionGridViewModel viewModel)
    {
        IReadOnlyList<GitRevision> selected = viewModel.GetSelectedRevisions(descending: true);
        if (selected.Count > 0 && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await AvaloniaClipboardBackend.SetTextAsync(clipboard, string.Join(Environment.NewLine, selected.Select(revision => revision.ObjectId)));
        }
    }

    private async Task PasteIntoQuickSearchAsync(RevisionGridViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard && await clipboard.TryGetTextAsync() is { Length: > 0 } text)
        {
            viewModel.QuickSearchPaste(text);
        }
    }

    private void ActivateSelected(object? source)
    {
        if (source is Visual visual && visual.FindAncestorOfType<DataGridRow>(includeSelf: true) is null)
        {
            // Not on a row, e.g. on the scroll bar.
            return;
        }

        // As OnGridViewDoubleClick: a label with a tracked (or tracking) branch goes to it.
        if (GetRefItem(source) is { } refItem && _viewModel?.GoToRelatedRef(refItem) == true)
        {
            return;
        }

        if (_viewModel?.SelectedRow is { } row)
        {
            RevisionActivated?.Invoke(this, row);
        }
    }

    /// <summary>The lane brushes of the theme, chosen as the WinForms <c>RevisionGraphLaneColor</c> does.</summary>
    private RevisionGraphRenderer CreateRenderer()
    {
        List<IBrush> laneBrushes = [];
        HashSet<Color> seen = [];
        foreach (string name in Enum.GetNames<AppColor>().Where(name => name.StartsWith(nameof(AppColor.GraphBranch1)[..^1], StringComparison.Ordinal)))
        {
            if (GetColor(name) is { } color && seen.Add(color))
            {
                laneBrushes.Add(new SolidColorBrush(color));
            }
        }

        const int minBranchColors = 4;
        if (laneBrushes.Count < minBranchColors)
        {
            laneBrushes = [Brushes.Cyan, Brushes.Magenta, Brushes.Yellow, Brushes.Lime];
        }

        IBrush nonRelative = GetColor(nameof(AppColor.GraphNonRelativeBranch)) is { } nonRelativeColor ? new SolidColorBrush(nonRelativeColor) : Brushes.LightGray;
        IBrush outline = this.TryFindResource("SystemControlForegroundBaseHighBrush", ActualThemeVariant, out object? foreground) && foreground is IBrush brush
            ? brush
            : Brushes.Black;
        return new RevisionGraphRenderer(laneBrushes, nonRelative, outline);

        Color? GetColor(string appColorName)
        {
            // The theme's color, as the host provides it; the default color otherwise (e.g. in tests).
            if (this.TryFindResource(ThemeColors.AppColorPrefix + appColorName, ActualThemeVariant, out object? resource) && resource is ISolidColorBrush themed)
            {
                return themed.Color.A == 0 ? null : themed.Color;
            }

            System.Drawing.Color fallback = AppColorDefaults.GetBy(Enum.Parse<AppColor>(appColorName));
            return fallback.IsEmpty ? null : Color.FromArgb(fallback.A, fallback.R, fallback.G, fallback.B);
        }
    }
}

/// <summary>Converters choosing the class of a reference "capsule" by its kind.</summary>
public sealed class RevisionRefKindConverter(RevisionRefKind kind) : IValueConverter
{
    public static RevisionRefKindConverter Branch { get; } = new(RevisionRefKind.Branch);

    public static RevisionRefKindConverter RemoteBranch { get; } = new(RevisionRefKind.RemoteBranch);

    public static RevisionRefKindConverter Tag { get; } = new(RevisionRefKind.Tag);

    public static RevisionRefKindConverter Stash { get; } = new(RevisionRefKind.Stash);

    public static RevisionRefKindConverter Superproject { get; } = new(RevisionRefKind.Superproject);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is RevisionRefKind k && k == kind;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Whether a build status is the one named by the parameter (the class choosing its color).</summary>
public sealed class BuildStatusClassConverter : IValueConverter
{
    public static BuildStatusClassConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is BuildStatus status && parameter is string name && status.ToString() == name;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>The image of an avatar (PNG data), decoded once for all the rows of its author.</summary>
public sealed class AvatarConverter : IValueConverter
{
    private static readonly ConditionalWeakTable<byte[], Bitmap?> _bitmaps = [];

    public static AvatarConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] png)
        {
            return null;
        }

        return _bitmaps.GetValue(png, static data =>
        {
            try
            {
                using MemoryStream stream = new(data);
                return new Bitmap(stream);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException or IOException)
            {
                return null;
            }
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
