using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.BuildServerIntegration;
using GitExtensions.Extensibility.Git;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>The kind of a reference shown in the message column, which chooses its color.</summary>
public enum RevisionRefKind
{
    Branch,
    RemoteBranch,
    Tag,
    Other,

    /// <summary>The label of a stash (its reflog selector) or of the autostash (as <c>MessageColumnProvider</c>).</summary>
    Stash,

    /// <summary>
    ///  In a submodule, a reference of the superproject to the commit, or the commit checked out (or in conflict) in the
    ///  superproject (the dashed labels of <c>DrawSuperprojectRefs</c> and <c>DrawSuperprojectInfo</c>).
    /// </summary>
    Superproject,
}

/// <summary>A reference shown before the subject (as the WinForms revision grid's ref "capsules").</summary>
/// <param name="Name">The name as shown, e.g. <c>main</c>, <c>origin/main</c> or <c>v1.0</c>.</param>
/// <param name="Kind">The kind of the reference.</param>
/// <param name="IsCurrentBranch">Whether it is the checked out branch (shown in bold).</param>
public sealed record RevisionRefItem(string Name, RevisionRefKind Kind, bool IsCurrentBranch)
{
    /// <summary>The reference (none for a stash label), for its menu and the hover highlighting of its ancestry.</summary>
    public IGitRef? GitRef { get; init; }

    /// <summary>
    ///  The label nestled after it (as <c>DrawBranchWithNestledRemote</c>): the remote branch the local branch tracks, or the
    ///  ahead / behind counts of the tracked (or tracking) branch.
    /// </summary>
    public RevisionRefItem? Nested { get; init; }

    /// <summary>Whether the label stands for a branch of another commit (the dashed <c>NestledVirtualRef</c>).</summary>
    public bool IsVirtual { get; init; }

    /// <summary>Whether the tracked remote branch is gone (in bold).</summary>
    public bool IsGone { get; init; }

    /// <summary>The branch a double click selects (as <c>GoToRelatedRef</c>), if any.</summary>
    public string? RelatedRefCompleteName { get; init; }

    /// <summary>The local branch whose deletion a double click offers, as its tracked remote branch is gone.</summary>
    public string? GoneLocalBranch { get; init; }

    /// <summary>The tooltip of the label (<c>TryGetToolTip</c> with it highlighted), if any.</summary>
    public string? ToolTip { get; init; }

    /// <summary>Whether the label is hovered and double clicked.</summary>
    public bool IsInteractive => GitRef is not null || RelatedRefCompleteName is not null || GoneLocalBranch is not null;

    // Compared as shown, not by the reference object.
    public bool Equals(RevisionRefItem? other)
        => other is not null && Name == other.Name && Kind == other.Kind && IsCurrentBranch == other.IsCurrentBranch
            && IsVirtual == other.IsVirtual && IsGone == other.IsGone && Equals(Nested, other.Nested);

    public override int GetHashCode() => HashCode.Combine(Name, Kind, IsCurrentBranch, IsVirtual, Nested);
}

/// <summary>How the revisions are shown (the <c>AppSettings</c> of the WinForms columns).</summary>
/// <param name="RelativeDate">Whether dates are relative (<c>AppSettings.RelativeDate</c>).</param>
/// <param name="ShowAuthorDate">Whether the author date rather than the commit date is shown (<c>AppSettings.ShowAuthorDate</c>).</param>
/// <param name="QuickSearchLabel">The text before the quick search string (<c>TranslatedStrings.SearchingFor</c>).</param>
/// <param name="QuickSearchTimeout">How long the quick search string is kept after typing (<c>AppSettings.RevisionGridQuickSearchTimeout</c>).</param>
/// <param name="ShowRemoteBranches">As <c>AppSettings.ShowRemoteBranches</c>: the remote branches are shown as references.</param>
/// <param name="ShowTags">As <c>AppSettings.ShowTags</c>: the tags are shown as references.</param>
/// <param name="ShowRevisionGridTooltips">As <c>AppSettings.ShowRevisionGridTooltips</c>: the labels always have a tooltip.</param>
public sealed record RevisionGridDisplayOptions(bool RelativeDate, bool ShowAuthorDate, string QuickSearchLabel = "Searching for: ", int QuickSearchTimeout = 4000, bool ShowRemoteBranches = true, bool ShowTags = true, bool ShowCommitBody = false, bool ShowRevisionGridTooltips = false);

/// <summary>A row of the revision grid: a revision and its row in the <see cref="RevisionGraph"/>.</summary>
public sealed partial class RevisionGridRow : ObservableObject
{
    private readonly DateTime _date;
    private readonly bool _relativeDate;
    private string? _formattedDate;

    /// <param name="aheadBehind">The ahead / behind data by local branch (<c>IAheadBehindDataProvider.GetData</c>), if shown.</param>
    public RevisionGridRow(int index, GitRevision revision, RevisionGridDisplayOptions options, string? currentBranch, IReadOnlyList<RevisionRefItem>? superprojectRefs = null, IReadOnlyDictionary<string, GitCommands.Git.AheadBehindData>? aheadBehind = null)
    {
        Index = index;
        Revision = revision;
        ShortId = revision.IsArtificial ? "" : revision.ObjectId.ToShortString();
        _date = options.ShowAuthorDate ? revision.AuthorDate : revision.CommitDate;
        _relativeDate = options.RelativeDate;
        List<RevisionRefItem> refs = RevisionRefLabels.Build(revision, options, currentBranch, aheadBehind);

        // As MessageColumnProvider.OnCellPainting: after the references, the label of a stash (its reflog selector without
        // "refs/"), or the autostash with its subject as label (and no message).
        if (revision.IsAutostash)
        {
            refs.Add(new RevisionRefItem(revision.Subject, RevisionRefKind.Stash, IsCurrentBranch: false));
        }
        else if (revision.ReflogSelector is { Length: > 5 } reflogSelector)
        {
            refs.Add(new RevisionRefItem(reflogSelector[5..], RevisionRefKind.Stash, IsCurrentBranch: false));
        }

        // After the references (and the stash label), the ones of the superproject.
        refs.AddRange(superprojectRefs ?? []);
        Refs = refs;

        // As DrawCommitMessage with ShowCommitBodyInRevisionGrid: the other lines of the message after the subject.
        if (options.ShowCommitBody && revision.Body?.Split('\n', StringSplitOptions.RemoveEmptyEntries) is { Length: > 1 } lines)
        {
            Body = string.Concat(lines.Skip(1).Select(line => " " + line.TrimEnd('\r')));
        }

        // As NotesColumnProvider: the first line of the notes, all of them as tooltip.
        Notes = revision.Notes?.IndexOf('\n') is int eolIndex and >= 0 ? revision.Notes[..eolIndex] : revision.Notes ?? "";
        UpdateBuildStatus();
        revision.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GitRevision.BuildStatus))
            {
                UpdateBuildStatus();
            }
        };
    }

    public int Index { get; }

    public GitRevision Revision { get; }

    public ObjectId ObjectId => Revision.ObjectId;

    /// <summary>The subject; none for the autostash, whose subject is its label.</summary>
    public string Subject => Revision.IsAutostash ? "" : Revision.Subject;

    /// <summary>The working directory or the index (drawn in a box, as <c>DrawArtificialRevision</c>).</summary>
    public bool IsArtificial => Revision.IsArtificial;

    /// <summary>As <c>MultilineIndicator</c>: the message has more than its subject (the "···" at the end of the message).</summary>
    public bool HasMultiLineMessage => !Revision.IsArtificial && Revision.HasMultiLineMessage;

    /// <summary>As the tooltip of <c>MessageColumnProvider</c>: the whole message of a commit with more than its subject.</summary>
    public string? MessageToolTip => HasMultiLineMessage ? Revision.Body ?? Revision.Subject : null;

    public string AuthorName => Revision.Author ?? "";

    /// <summary>The body of the message, shown after the subject (<c>ShowCommitBodyInRevisionGrid</c>); empty if not shown.</summary>
    public string Body { get; } = "";

    // The grid only reads dates for visible rows; formatting the entire history delays loading large repositories.
    public string Date => _formattedDate ??= FormatDate(_date, _relativeDate);

    public string ShortId { get; }

    public IReadOnlyList<RevisionRefItem> Refs { get; }

    /// <summary>The first line of the git notes (the notes column).</summary>
    public string Notes { get; }

    /// <summary>The git notes, as the tooltip of the notes column; none without notes.</summary>
    public string? NotesToolTip => string.IsNullOrEmpty(Revision.Notes) ? null : Revision.Notes;

    /// <summary>The tooltip of the author and the avatar (the author and the committer); none for the artificial commits.</summary>
    public string? AuthorToolTip => AuthorToolTipProvider?.Invoke(this);

    internal Func<RevisionGridRow, string?>? AuthorToolTipProvider { get; init; }

    /// <summary>The author's avatar (PNG data), once loaded for the avatar column.</summary>
    [ObservableProperty]
    public partial byte[]? Avatar { get; set; }

    /// <summary>Whether the author is the one of the selected revision (in bold, as <c>AuthorRevisionHighlighting</c>).</summary>
    [ObservableProperty]
    public partial bool IsAuthorHighlighted { get; set; }

    /// <summary>The build status (<c>GitRevision.BuildStatus</c>) of the build server integration, if any.</summary>
    [ObservableProperty]
    public partial BuildInfo? BuildStatus { get; private set; }

    /// <summary>The symbol of the build status (as <c>BuildInfo.StatusSymbol</c>); empty without one.</summary>
    public string BuildStatusSymbol => BuildStatus?.StatusSymbol ?? "";

    /// <summary>The description of the build status (the text of <c>BuildStatusColumnProvider</c>).</summary>
    public string BuildStatusDescription => BuildStatus?.Description ?? "";

    /// <summary>As <c>BuildStatusColumnProvider.TryGetToolTip</c>.</summary>
    public string? BuildStatusToolTip => BuildStatus is { } status ? status.Tooltip ?? status.Description : null;

    /// <summary>Whether the build status has a report to open (the hand cursor of the WinForms column).</summary>
    public bool HasBuildReport => !string.IsNullOrWhiteSpace(BuildStatus?.Url);

    private void UpdateBuildStatus()
    {
        BuildStatus = Revision.BuildStatus;
        OnPropertyChanged(nameof(BuildStatusSymbol));
        OnPropertyChanged(nameof(BuildStatusDescription));
        OnPropertyChanged(nameof(BuildStatusToolTip));
        OnPropertyChanged(nameof(HasBuildReport));
    }

    /// <summary>As <c>DateColumnProvider.FormatDate</c>.</summary>
    public static string FormatDate(DateTime date, bool relative)
    {
        if (date == DateTime.MinValue || date == DateTime.MaxValue)
        {
            return "";
        }

        return relative ? LocalizationHelpers.GetRelativeDateString(DateTime.Now, date, displayWeeks: false) : date.ToString("G");
    }
}

/// <summary>The rows of the grid; added in batches with a single reset notification, as revisions arrive in batches.</summary>
public sealed class RevisionGridRowList : ObservableCollection<RevisionGridRow>
{
    public void AddRange(IEnumerable<RevisionGridRow> rows)
    {
        bool added = false;
        foreach (RevisionGridRow row in rows)
        {
            Items.Add(row);
            added = true;
        }

        if (added)
        {
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

/// <summary>Operations of the revision grid that need the host (git, threads).</summary>
public interface IRevisionGridHost
{
    /// <summary>The checked out branch, whose reference is shown in bold (empty if detached).</summary>
    string CurrentBranch { get; }

    /// <summary>
    ///  Reads the revisions in the background and adds them to <paramref name="graph"/> (with their references),
    ///  reporting on the UI thread after each batch and when done (with the error, if reading failed).
    /// </summary>
    void LoadRevisions(RevisionGraph graph, Action reportBatch, Action<Exception?> completed, CancellationToken cancellationToken);

    /// <summary>Whether the revision matches the quick search criteria (<c>IGitRevisionTester.Matches</c>).</summary>
    bool MatchesQuickSearch(GitRevision revision, string criteria);

    /// <summary>Runs <paramref name="work"/> in the background, then <paramref name="then"/> on the UI thread.</summary>
    void RunInBackground(Action work, Action then);

    /// <summary>
    ///  The email of the user (<c>user.email</c>), whose revisions are highlighted when none is selected
    ///  (<c>AuthorRevisionHighlighting</c>).
    /// </summary>
    string UserEmail => "";

    /// <summary>The avatar of an author (PNG data) for the avatar column (<c>IAvatarProvider.GetAvatarAsync</c>).</summary>
    Task<byte[]?> GetAvatarAsync(string email, string? name, int size) => Task.FromResult<byte[]?>(null);

    /// <summary>The tooltip of the author and avatar columns (<c>AuthorNameColumnProvider.GetAuthorAndCommiterToolTip</c>).</summary>
    string GetAuthorToolTip(GitRevision revision) => $"{revision.Author} <{revision.AuthorEmail}>";

    /// <summary>
    ///  The revisions to highlight in the graph when hovering the label of <paramref name="gitRef"/> in row
    ///  <paramref name="rowIndex"/> (<c>HoverHighlightCalculator</c>, debounced); none to clear. Cancelled (by a later call,
    ///  or if the highlight did not change) with an <see cref="OperationCanceledException"/>.
    /// </summary>
    Task<IReadOnlySet<ObjectId>?> GetHoverHighlightAsync(RevisionGraph graph, IGitRef? gitRef, int rowIndex, int firstVisibleRow, int visibleRowCount)
        => Task.FromResult<IReadOnlySet<ObjectId>?>(null);

    /// <summary>Opens a URL in the browser (the build report of a build status).</summary>
    void OpenUrl(string url)
    {
    }

    /// <summary>In a submodule, the labels of the superproject for the revision (read with the revisions).</summary>
    IReadOnlyList<RevisionRefItem> GetSuperprojectRefs(GitRevision revision) => [];

    /// <summary>
    ///  The ahead / behind data by local branch for the labels (<c>SetAheadBehindDataProvider</c>), <see langword="null"/> if
    ///  not shown (<c>AppSettings.ShowAheadBehindData</c>).
    /// </summary>
    IReadOnlyDictionary<string, GitCommands.Git.AheadBehindData>? GetAheadBehindData() => null;

    /// <summary>Raised on the UI thread when the cache of the avatars was cleared (<c>IAvatarCacheCleaner.CacheCleared</c>).</summary>
    event EventHandler? AvatarsCleared
    {
        add { }
        remove { }
    }

    /// <summary>As the <c>handleGone</c> of <c>GoToRelatedRef</c>: offers to delete the branch whose remote is gone.</summary>
    void DeleteBranch(string branchName)
    {
    }
}

/// <summary>
///  View model of the Avalonia revision grid (port of <c>RevisionGridControl</c>; docs/avalonia-port/PLAN.md, phase 4):
///  the revisions in graph order, with the <see cref="RevisionGraph"/> layout computed ahead of the visible rows.
/// </summary>
public sealed partial class RevisionGridViewModel : ObservableObject, IDisposable
{
    private readonly IRevisionGridHost _host;
    private RevisionGridDisplayOptions _options;
    private CancellationTokenSource? _loadCancellation;
    private ObjectId? _toBeSelected;
    private ObjectId? _firstToBeSelected;
    private bool _isCaching;
    private int _cacheRequestedTo = -1;
    private bool _disposed;
    private IReadOnlyDictionary<string, GitCommands.Git.AheadBehindData>? _aheadBehind;
    private bool _aheadBehindRead;

    public RevisionGridViewModel(IRevisionGridHost host, RevisionGridDisplayOptions options)
    {
        _host = host;
        _options = options;
        _host.AvatarsCleared += OnAvatarsCleared;
    }

    private void OnAvatarsCleared(object? sender, EventArgs e) => ClearAvatars();

    /// <summary>How the revisions are shown; setting it loads them again (as the view settings of the grid).</summary>
    public RevisionGridDisplayOptions DisplayOptions
    {
        get => _options;
        set
        {
            _options = value;
            Load(SelectedRow?.ObjectId);
        }
    }

    /// <summary>The layout of the graph, which the graph column draws from.</summary>
    public RevisionGraph Graph { get; } = new();

    public RevisionGridRowList Rows { get; } = [];

    [ObservableProperty]
    public partial RevisionGridRow? SelectedRow { get; set; }

    /// <summary>Whether several revisions can be selected (<c>RevisionGridControl.MultiSelect</c>).</summary>
    public bool MultiSelect { get; init; }

    /// <summary>The selected rows, in the order selected; the view reports them.</summary>
    public IReadOnlyList<RevisionGridRow> SelectedRows { get; private set; } = [];

    /// <summary>Raised when <see cref="SelectedRows"/> changed.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    ///  Sets the selected rows (from the view), in the order they were selected: the rows still selected keep their place, the
    ///  new ones come last (the grid lists its selection in the order of the rows).
    /// </summary>
    public void SetSelectedRows(IEnumerable<RevisionGridRow> rows)
    {
        List<RevisionGridRow> selected = [.. rows];
        HashSet<RevisionGridRow> stillSelected = [.. selected];
        HashSet<RevisionGridRow> previous = [.. SelectedRows];
        SelectedRows = [.. SelectedRows.Where(stillSelected.Contains), .. selected.Where(row => !previous.Contains(row))];
        UpdateAuthorHighlight();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///  The selected revisions, newest first or (<paramref name="descending"/>) oldest first, as
    ///  <c>RevisionGridControl.GetSelectedRevisions</c> sorts them by row.
    /// </summary>
    public IReadOnlyList<GitRevision> GetSelectedRevisions(bool descending)
    {
        IEnumerable<RevisionGridRow> rows = SelectedRows.Count > 0 ? SelectedRows : SelectedRow is { } row ? [row] : [];
        return [.. (descending ? rows.OrderByDescending(r => r.Index) : rows.OrderBy(r => r.Index)).Select(r => r.Revision)];
    }

    /// <summary>
    ///  The selected revisions, the latest selected first, as <c>RevisionGridControl.GetSelectedRevisions()</c> without a
    ///  direction returns the selected rows of the WinForms grid.
    /// </summary>
    public IReadOnlyList<GitRevision> GetSelectedRevisionsLatestSelectedFirst()
    {
        IEnumerable<RevisionGridRow> rows = SelectedRows.Count > 0 ? SelectedRows.Reverse() : SelectedRow is { } row ? [row] : [];
        return [.. rows.Select(r => r.Revision)];
    }

    /// <summary>The revision if it is listed (<c>RevisionGridControl.GetRevision</c>).</summary>
    public GitRevision? GetRevision(ObjectId objectId)
        => Graph.TryGetNode(objectId, out RevisionGraphRevision? node) ? node.GitRevision : null;

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    /// <summary>The number of rows whose graph is laid out; later rows are drawn once they are.</summary>
    [ObservableProperty]
    public partial int CachedGraphRowCount { get; private set; }

    /// <summary>Raised when revisions were loaded, after the preselected revision (if any) was selected.</summary>
    public event EventHandler? Loaded;

    /// <summary>(Re)loads the revisions, selecting <paramref name="toBeSelected"/> once it is loaded.</summary>
    /// <param name="firstSelected">
    ///  With <paramref name="toBeSelected"/>, the revision selected first (as <c>FirstId</c>: the first commit of a diff).
    /// </param>
    public void Load(ObjectId? toBeSelected = null, ObjectId? firstSelected = null)
    {
        // E.g. the repository changed after the window of the grid closed.
        if (_disposed)
        {
            return;
        }

        _loadCancellation?.Cancel();
        _loadCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadCancellation.Token;

        _toBeSelected = toBeSelected;
        _firstToBeSelected = toBeSelected is null ? null : firstSelected;
        _aheadBehind = null;
        _aheadBehindRead = false;
        Graph.Clear();
        Rows.Clear();
        CachedGraphRowCount = 0;
        _cacheRequestedTo = -1;
        IsLoading = true;

        // As PerformRefreshRevisions: the highlighted branch (until refresh) and the hover highlight are reset.
        IsBranchHighlighted = false;
        HoverHighlightedIds = null;

        _host.LoadRevisions(
            Graph,
            reportBatch: () =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    AddNewRows();
                }
            },
            completed: _ =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Graph.LoadingCompleted();

                // Revisions inserted before the rows already shown (the artificial commits when HEAD is filtered out)
                // move the rows: they are created again.
                if (Rows.Where((row, index) => Graph.GetNodeForRow(index)?.GitRevision != row.Revision).Any())
                {
                    RevisionGridRow? selected = SelectedRow;
                    Rows.Clear();
                    AddNewRows();
                    SelectedRow = selected is null ? null : Rows.FirstOrDefault(row => row.ObjectId == selected.ObjectId);
                }

                AddNewRows();
                IsLoading = false;
                if (_toBeSelected is { } id)
                {
                    // As GetToBeSelectedRevisions: FirstId, then SelectedId.
                    if (_firstToBeSelected is { } first)
                    {
                        _firstToBeSelected = null;
                        SelectRevisions([first, id]);
                    }
                    else
                    {
                        SelectRevision(id);
                    }
                }

                Loaded?.Invoke(this, EventArgs.Empty);
            },
            cancellationToken);
    }

    private void AddNewRows()
    {
        int count = Graph.Count;
        List<RevisionGridRow> rows = [];
        string currentBranch = _host.CurrentBranch;

        // As GetAheadBehind: read once for each load, when the first labels are shown.
        if (!_aheadBehindRead)
        {
            _aheadBehind = _host.GetAheadBehindData();
            _aheadBehindRead = true;
        }

        for (int index = Rows.Count; index < count; index++)
        {
            if (Graph.GetNodeForRow(index)?.GitRevision is { } revision)
            {
                rows.Add(new RevisionGridRow(index, revision, _options, currentBranch, _host.GetSuperprojectRefs(revision), _aheadBehind) { IsAuthorHighlighted = IsAuthorHighlightedFor(revision), AuthorToolTipProvider = GetAuthorToolTip, Changes = revision.IsArtificial ? GetArtificialCommitChanges(revision.ObjectId) : null });
            }
        }

        RevisionGridRow? selected = SelectedRow;
        Rows.AddRange(rows);
        SelectedRow = selected;

        if (_toBeSelected is { } id && SelectedRow is null && Graph.TryGetRowIndex(id, out int rowIndex) && rowIndex < Rows.Count)
        {
            SelectedRow = Rows[rowIndex];
        }

        // Lay out the first page right away.
        EnsureGraphCached(Math.Max(_cacheRequestedTo, 50));
    }

    /// <summary>Selects the revision; returns <see langword="false"/> if it is not listed.</summary>
    public bool SelectRevision(ObjectId objectId)
    {
        if (Graph.TryGetRowIndex(objectId, out int index) && index < Rows.Count)
        {
            SelectedRow = Rows[index];
            return true;
        }

        return false;
    }

    /// <summary>
    ///  Lays out the graph to <paramref name="lastVisibleIndex"/> and a page beyond, in the background
    ///  (as <c>RevisionDataGridView.UpdateVisibleRowRangeInternalAsync</c>).
    /// </summary>
    public void EnsureGraphCached(int lastVisibleIndex)
    {
        _cacheRequestedTo = Math.Max(_cacheRequestedTo, lastVisibleIndex);
        if (_isCaching || Graph.Count == 0 || CachedGraphRowCount > Math.Min(_cacheRequestedTo, Graph.Count - 1))
        {
            return;
        }

        _isCaching = true;
        int cacheTo = _cacheRequestedTo * 2;
        _host.RunInBackground(
            () => Graph.CacheTo(currentRowIndex: Graph.GetCachedCount(), lastToCacheRowIndex: cacheTo),
            () =>
            {
                _isCaching = false;
                int cached = Graph.GetCachedCount();
                if (cached != CachedGraphRowCount)
                {
                    CachedGraphRowCount = cached;
                    EnsureGraphCached(_cacheRequestedTo);
                }
            });
    }

    // Quick search: typing in the grid selects the next revision matching the typed text (as QuickSearchProvider).

    private string _lastQuickSearch = "";

    /// <summary>The typed quick search string; empty when not searching.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuickSearchLabel))]
    public partial string QuickSearchText { get; private set; } = "";

    [ObservableProperty]
    public partial bool IsQuickSearchVisible { get; private set; }

    /// <summary>Whether a revision matches the quick search string (the label is red otherwise).</summary>
    [ObservableProperty]
    public partial bool IsQuickSearchMatched { get; private set; } = true;

    public string QuickSearchLabel => _options.QuickSearchLabel + QuickSearchText;

    /// <summary>How long the quick search string is kept after typing, in milliseconds.</summary>
    public int QuickSearchTimeout => _options.QuickSearchTimeout;

    /// <summary>Raised when the quick search string changed, so that the view restarts its timeout.</summary>
    public event EventHandler? QuickSearchRestarted;

    /// <summary>Adds typed characters (lowercase, as <c>QuickSearchProvider.OnKeyPress</c>) to the quick search string.</summary>
    public void QuickSearchType(string text) => UpdateQuickSearch(QuickSearchText + text.ToLowerInvariant());

    /// <summary>Adds pasted text to the quick search string.</summary>
    public void QuickSearchPaste(string text) => UpdateQuickSearch(QuickSearchText + text);

    /// <summary>Removes the last character; returns <see langword="false"/> (and ends the search) if nothing would remain.</summary>
    public bool QuickSearchBackspace()
    {
        if (QuickSearchText.Length > 1)
        {
            UpdateQuickSearch(QuickSearchText[..^1]);
            return true;
        }

        HideQuickSearch();
        return false;
    }

    public void HideQuickSearch()
    {
        QuickSearchText = "";
        IsQuickSearchVisible = false;
    }

    /// <summary>Selects the next (or previous) revision matching the last quick search string.</summary>
    public void QuickSearchNext(bool down)
    {
        int currentIndex = SelectedRow?.Index ?? -1;
        int nextIndex = currentIndex < 0 ? 0 : down ? currentIndex + 1 : currentIndex - 1;
        QuickSearchText = _lastQuickSearch;
        FindNextMatch(nextIndex, QuickSearchText, reverse: !down);
        IsQuickSearchVisible = true;
        QuickSearchRestarted?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateQuickSearch(string text)
    {
        QuickSearchText = text;
        FindNextMatch(Math.Max(SelectedRow?.Index ?? 0, 0), text, reverse: false);
        _lastQuickSearch = text;
        IsQuickSearchVisible = true;
        QuickSearchRestarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>As <c>QuickSearchProvider.FindNextMatch</c>: searches from the start index, wrapping around.</summary>
    private void FindNextMatch(int startIndex, string criteria, bool reverse)
    {
        int count = Rows.Count;
        if (count == 0)
        {
            return;
        }

        if (startIndex < 0 || startIndex >= count)
        {
            startIndex = reverse ? count - 1 : 0;
        }

        for (int step = 0; step < count; step++)
        {
            int index = reverse ? (startIndex - step + count) % count : (startIndex + step) % count;
            if (_host.MatchesQuickSearch(Rows[index].Revision, criteria))
            {
                IsQuickSearchMatched = true;
                SelectedRow = Rows[index];
                return;
            }
        }

        IsQuickSearchMatched = false;
    }

    public void Dispose()
    {
        // Not disposed: the revision reader in the background may still use its token.
        _disposed = true;
        _host.AvatarsCleared -= OnAvatarsCleared;
        _loadCancellation?.Cancel();
    }
}
