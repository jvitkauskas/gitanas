using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Presentation;

namespace GitUI.Avalonia.Controls.FlatTree;

/// <summary>
///  Shows a tree in a virtualizing <see cref="ListBox"/> (instead of a <c>TreeView</c>, which creates the controls of all the
///  expanded nodes): its visible nodes (<see cref="FlatTreeRows"/>) as rows with an indentation and an expander, the keys of
///  a tree (Left collapses or goes to the parent, Right expands or goes to the first child), a double click expanding or
///  collapsing a node with children, and the selection of the list kept in sync with the selected nodes of a view model.
/// </summary>
/// <remarks>
///  The rows use the <c>DataTemplate</c> of <see cref="FlatTreeRow"/> of the list (see <c>FlatTreeResources.axaml</c>), which
///  shows the node itself by the data templates of its type.
/// </remarks>
public sealed class FlatTreeList : IDisposable
{
    private readonly ListBox _list;
    private readonly FlatTreeRows _rows;
    private readonly Func<object, bool>? _toggleOnDoubleTap;
    private IList? _selectedNodes;
    private bool _syncing;

    public FlatTreeList(ListBox list, IEnumerable roots, FlatTreeAdapter adapter, Func<object, bool>? toggleOnDoubleTap = null)
    {
        _list = list;
        _rows = new FlatTreeRows(roots, adapter);
        _toggleOnDoubleTap = toggleOnDoubleTap;
        _list.ItemsSource = _rows.Rows;
        _list.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        _list.DoubleTapped += OnDoubleTapped;
        _list.SelectionChanged += OnListSelectionChanged;
        _rows.Rows.CollectionChanged += OnRowsChanged;
    }

    /// <summary>The visible rows (e.g. for tests).</summary>
    public FlatTreeRows Rows => _rows;

    /// <summary>The nodes of the selected rows, in the order of the list.</summary>
    public IEnumerable<object> SelectedNodes => _list.Selection.SelectedItems.OfType<FlatTreeRow>().Select(row => row.Node);

    /// <summary>
    ///  Keeps the selection of the list and <paramref name="selectedNodes"/> (a collection of nodes of a view model, which may
    ///  also select nodes itself) in sync. The nodes of rows hidden by collapsing their parent stay selected, as in a tree.
    /// </summary>
    public void SyncSelection(IList selectedNodes)
    {
        if (_selectedNodes is INotifyCollectionChanged previous)
        {
            previous.CollectionChanged -= OnSelectedNodesChanged;
        }

        _selectedNodes = selectedNodes;
        if (selectedNodes is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnSelectedNodesChanged;
        }

        SelectRowsOfSelectedNodes();
    }

    /// <summary>Selects the row of <paramref name="node"/> alone and scrolls it into view (a single selection).</summary>
    public void Select(object? node)
    {
        FlatTreeRow? row = node is null ? null : _rows.RowOf(node);
        _syncing = true;
        try
        {
            _list.SelectedItem = row;
        }
        finally
        {
            _syncing = false;
        }

        if (row is not null)
        {
            Dispatcher.UIThread.Post(() => _list.ScrollIntoView(row), DispatcherPriority.Background);
        }
    }

    /// <summary>The row of <paramref name="node"/>, if it is visible.</summary>
    public FlatTreeRow? RowOf(object node) => _rows.RowOf(node);

    /// <summary>The control of the row of <paramref name="node"/>, if it is visible and realized (e.g. to scroll it into view).</summary>
    public Control? ContainerOf(object node) => _rows.RowOf(node) is { } row ? _list.ContainerFromItem(row) : null;

    public void Dispose()
    {
        _list.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        _list.DoubleTapped -= OnDoubleTapped;
        _list.SelectionChanged -= OnListSelectionChanged;
        _rows.Rows.CollectionChanged -= OnRowsChanged;
        if (_selectedNodes is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged -= OnSelectedNodesChanged;
        }

        _list.ItemsSource = null;
        _rows.Dispose();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None || _list.SelectedItem is not FlatTreeRow row || e.Source is TextBox)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Right when row.HasChildren && !row.IsExpanded:
                row.IsExpanded = true;
                e.Handled = true;
                break;

            case Key.Right when row.HasChildren:
                // To the first child.
                int index = _rows.Rows.IndexOf(row);
                if (index + 1 < _rows.Rows.Count)
                {
                    MoveTo(_rows.Rows[index + 1]);
                }

                e.Handled = true;
                break;

            case Key.Left when row.HasChildren && row.IsExpanded:
                row.IsExpanded = false;
                e.Handled = true;
                break;

            case Key.Left when _rows.ParentOf(row) is { } parent:
                MoveTo(parent);
                e.Handled = true;
                break;
        }
    }

    private void MoveTo(FlatTreeRow row)
    {
        _list.SelectedItem = row;
        _list.ScrollIntoView(row);
        (_list.ContainerFromItem(row) as InputElement)?.Focus(NavigationMethod.Directional);
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Not on the expander, which expands or collapses already.
        if ((e.Source as global::Avalonia.Visual)?.FindAncestorOfType<ToggleButton>(includeSelf: true) is not null
            || (e.Source as global::Avalonia.Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true)?.DataContext is not FlatTreeRow { HasChildren: true } row
            || _toggleOnDoubleTap?.Invoke(row.Node) == false)
        {
            return;
        }

        row.IsExpanded = !row.IsExpanded;
    }

    private void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncing || _selectedNodes is null)
        {
            return;
        }

        _syncing = true;
        try
        {
            using IDisposable? update = (_selectedNodes as IBatchObservableCollection)?.BeginUpdate();
            foreach (FlatTreeRow row in e.RemovedItems.OfType<FlatTreeRow>())
            {
                // A row removed by collapsing its parent: its node stays selected.
                if (_rows.RowOf(row.Node) is not null)
                {
                    _selectedNodes.Remove(row.Node);
                }
            }

            HashSet<object> selected = new(_selectedNodes.Cast<object>(), ReferenceEqualityComparer.Instance);
            foreach (FlatTreeRow row in e.AddedItems.OfType<FlatTreeRow>())
            {
                if (selected.Add(row.Node))
                {
                    _selectedNodes.Add(row.Node);
                }
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnSelectedNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_syncing)
        {
            SelectRowsOfSelectedNodes();
        }
    }

    // The rows shown again (a parent expanded) are selected again if their nodes are.
    private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_syncing && _selectedNodes is { Count: > 0 } && e.Action == NotifyCollectionChangedAction.Add
            && e.NewItems!.OfType<FlatTreeRow>().Any(row => _selectedNodes.Contains(row.Node)))
        {
            Dispatcher.UIThread.Post(SelectRowsOfSelectedNodes, DispatcherPriority.Background);
        }
    }

    private void SelectRowsOfSelectedNodes()
    {
        if (_selectedNodes is null)
        {
            return;
        }

        _syncing = true;
        try
        {
            HashSet<FlatTreeRow> rows = [];
            FlatTreeRow? lastVisible = null;
            foreach (object node in _selectedNodes)
            {
                if (_rows.RowOf(node) is { } row)
                {
                    rows.Add(row);
                    lastVisible = row;
                }
            }

            _list.Selection.BeginBatchUpdate();
            try
            {
                _list.Selection.Clear();
                for (int index = 0; index < _rows.Rows.Count; index++)
                {
                    if (!rows.Contains(_rows.Rows[index]))
                    {
                        continue;
                    }

                    int start = index;
                    while (index + 1 < _rows.Rows.Count && rows.Contains(_rows.Rows[index + 1]))
                    {
                        ++index;
                    }

                    _list.Selection.SelectRange(start, index);
                }
            }
            finally
            {
                _list.Selection.EndBatchUpdate();
            }

            if (lastVisible is { } last)
            {
                // Let the virtualizing panel lay out the replacement rows before jumping to a distant selection.
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        if (ReferenceEquals(_rows.RowOf(last.Node), last) && _selectedNodes.Contains(last.Node))
                        {
                            _list.ScrollIntoView(last);
                        }
                    },
                    DispatcherPriority.Background);
            }
        }
        finally
        {
            _syncing = false;
        }
    }
}
