using CommunityToolkit.Mvvm.ComponentModel;

namespace GitUI.Presentation.Editor;

/// <summary>
///  View model of the Avalonia text editor (the editing mode of the WinForms <c>FileViewer</c>; docs/avalonia-port/PLAN.md,
///  phase 3): the text, whether it is read-only, and the file name that chooses the syntax highlighting.
/// </summary>
public sealed partial class TextEditorViewModel : ObservableObject
{
    private string _loadedText = "";
    private CancellationTokenSource? _inlineDiffCancellation;

    /// <summary>Completes when the current diff's optional word highlighting has been calculated.</summary>
    public Task InlineDiffLoading { get; private set; } = Task.CompletedTask;

    /// <summary>The text, as edited.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    public partial string Text { get; set; } = "";

    [ObservableProperty]
    public partial bool IsReadOnly { get; set; }

    /// <summary>Whether spaces, tabs and line ends are shown (<c>AppSettings.ShowNonPrintingChars</c>).</summary>
    [ObservableProperty]
    public partial bool ShowWhitespace { get; set; }

    /// <summary>Whether the line numbers are shown (a diff shows its own).</summary>
    [ObservableProperty]
    public partial bool ShowLineNumbers { get; set; } = true;

    /// <summary>The name of the file, whose extension chooses the syntax highlighting.</summary>
    [ObservableProperty]
    public partial string? FileName { get; set; }

    /// <summary>Reads the selected text of the view (the view sets it).</summary>
    public Func<string>? SelectedTextProvider { get; set; }

    /// <summary>The selected text of the editor, if a view shows it.</summary>
    public string SelectedText => SelectedTextProvider?.Invoke() ?? "";

    /// <summary>The line (1-based) to show and put the caret on when the text is loaded.</summary>
    [ObservableProperty]
    public partial int? LineToShow { get; set; }

    /// <summary>The offset of the caret, which the view reports and follows.</summary>
    [ObservableProperty]
    public partial int CaretOffset { get; set; }

    /// <summary>Raised to move the focus to the editor.</summary>
    public event EventHandler? FocusRequested;

    public void RequestFocus() => FocusRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Whether the text changed since it was loaded or saved.</summary>
    public bool HasChanges => Text != _loadedText;

    /// <summary>Raised when a text is loaded, which the view shows from its start (or <see cref="LineToShow"/>).</summary>
    public event EventHandler? TextLoaded;

    /// <summary>The lines of the diff shown (with their line numbers), or <see langword="null"/> for a plain text.</summary>
    public IReadOnlyList<DiffLine>? DiffLines { get; private set; }

    /// <summary>The in-line differences of the matching removed and added lines of the diff shown; empty for a plain text.</summary>
    public IReadOnlyList<InlineDiffMarker> InlineDiffMarkers { get; private set; } = [];

    /// <summary>The colors of git's output of the diff shown, or <see langword="null"/> if the diff is not colored by git.</summary>
    public GitColoring? GitColoring { get; private set; }

    /// <summary>What kind of diff is shown (see <see cref="DiffLines"/>).</summary>
    public DiffViewMode DiffMode { get; private set; }

    /// <summary>
    ///  Whether the line numbers of a diff have a column for the old file (not for grep results and range diffs, as the
    ///  <c>showLeftColumn</c> of <c>DiffViewerLineNumberControl.DisplayLineNum</c>).
    /// </summary>
    public bool ShowLeftLineNumbers { get; private set; } = true;

    /// <summary>
    ///  What the text shown is (the file name, the <c>contentIdentification</c> of <c>SetText</c>): showing the same content again
    ///  keeps the position (<see cref="PositionCache"/>).
    /// </summary>
    public string? ContentIdentification { get; private set; }

    /// <summary>The position kept for the same content, which the view captures and restores.</summary>
    public ViewPositionCache PositionCache { get; } = new();

    /// <summary>
    ///  The lines of context shown above the first change, which the caret goes to when a diff is loaded (as
    ///  <c>GoToFirstChange</c>), unless its position is kept; <see langword="null"/> to show the start.
    /// </summary>
    public int? FirstChangeContextLines { get; private set; }

    /// <summary>
    ///  As <c>ScrollToTop</c> and <c>ScrollToBottom</c>: where the next text loaded is shown (e.g. the next file of a continuous
    ///  scroll), whatever the position kept.
    /// </summary>
    public TextScrollRequest PendingScroll { get; set; }

    /// <summary>
    ///  As <c>SetFileLoader</c>: shows the next (or previous, for <see langword="true"/>) file, looping around, for a search
    ///  that goes on in it; returns what it shows once it is shown, <see langword="null"/> if there is no other file.
    /// </summary>
    public Func<bool, Task<object?>>? NextFileLoader { get; set; }

    /// <summary>The column of the vertical ruler, 0 for none (as <c>FileViewerInternal.VRulerPosition</c>).</summary>
    [ObservableProperty]
    public partial int VerticalRulerColumn { get; set; }

    /// <summary>
    ///  Shows a diff, read-only, with the added and removed lines colored and their in-line differences marked
    ///  (as <c>FileViewer.ViewFixedPatch</c> and <c>ViewPatch</c>).
    /// </summary>
    /// <param name="gitColors">The theme colors if the text is git's colored output (with ANSI escape sequences).</param>
    /// <param name="reverseGitColoring">Whether git colors the background (<c>AppSettings.ReverseGitColoring</c>).</param>
    public void LoadDiff(string text, IThemeColors? gitColors = null, bool reverseGitColoring = true)
        => LoadDiff(text, new DiffLoadOptions(gitColors, reverseGitColoring));

    /// <summary>
    ///  Shows a diff of any kind, read-only (as <c>FileViewerInternal.SetText</c> with the highlight service of the view mode):
    ///  its lines and line numbers, git's colors, and the in-line differences of the matching lines of a patch.
    /// </summary>
    public void LoadDiff(string text, DiffLoadOptions options)
    {
        CancelInlineDiff();
        IsReadOnly = true;
        bool deferInlineDiff = false;
        GitColoring? gitColoring = null;
        IReadOnlyList<DiffLine> diffLines;
        IReadOnlyList<InlineDiffMarker> inlineDiffMarkers = [];
        IThemeColors colors = options.GitColors ?? DefaultThemeColors.Instance;
        switch (options.Mode)
        {
            case DiffViewMode.Difftastic or DiffViewMode.Grep or DiffViewMode.RangeDiff:
                // As DifftasticHighlightService, GrepHighlightService and RangeDiffHighlightService: always with git's colors,
                // no in-line differences.
                AnalyzedDiff analyzed = options.Mode switch
                {
                    DiffViewMode.Difftastic => DifftasticAnalyzer.Analyze(text, options.DifftasticWidth, colors, options.ReverseGitColoring),
                    DiffViewMode.Grep => GrepAnalyzer.Analyze(text, colors),
                    _ => RangeDiffAnalyzer.Analyze(text, colors),
                };
                text = analyzed.Text;
                gitColoring = new GitColoring(analyzed.Segments, colors, options.ReverseGitColoring);
                diffLines = analyzed.Lines;
                if (options.Mode == DiffViewMode.Difftastic)
                {
                    // As SetText: difftastic sets the position (0 to hide).
                    VerticalRulerColumn = analyzed.VerticalRulerColumn;
                }

                break;

            default:
                // As PatchHighlightService and CombinedDiffHighlightService.
                if (options.GitColors is not null)
                {
                    (text, IReadOnlyList<ColoredSegment> segments) = AnsiEscapeParser.Parse(text, colors);
                    gitColoring = new GitColoring(segments, colors, options.ReverseGitColoring);
                }

                bool isCombinedDiff = options.Mode == DiffViewMode.CombinedDiff || DiffLinesAnalyzer.IsCombinedDiff(text);
                diffLines = DiffLinesAnalyzer.Analyze(text, isCombinedDiff, gitColoring, isGitWordDiff: options.IsGitWordDiff && !isCombinedDiff);

                // As DiffHighlightService.SetHighlighting: git colors the words of a git word diff.
                if (gitColoring is null || !options.IsGitWordDiff)
                {
                    // Keep small diffs immediate. Large diffs can be read while word highlighting is prepared.
                    deferInlineDiff = text.Length >= 128 * 1024 && SynchronizationContext.Current is not null;
                    if (!deferInlineDiff)
                    {
                        inlineDiffMarkers = InlineDiffAnalyzer.Analyze(text, diffLines);
                    }
                }

                break;
        }

        DiffMode = options.Mode;
        ShowLeftLineNumbers = options.Mode is not (DiffViewMode.Grep or DiffViewMode.RangeDiff);
        GitColoring = gitColoring;
        InlineDiffMarkers = inlineDiffMarkers;
        FirstChangeContextLines = options.FirstChangeContextLines;
        LoadText(text, options.HighlightingFileName, line: null, diffLines, options.ContentIdentification);
        if (deferInlineDiff)
        {
            CancellationTokenSource cancellation = new();
            _inlineDiffCancellation = cancellation;
            InlineDiffLoading = LoadInlineDiffAsync(text, diffLines, cancellation);
        }
    }

    private async Task LoadInlineDiffAsync(string text, IReadOnlyList<DiffLine> lines, CancellationTokenSource cancellation)
    {
        try
        {
            IReadOnlyList<InlineDiffMarker> markers = await Task.Run(() => InlineDiffAnalyzer.Analyze(text, lines, cancellation.Token), cancellation.Token);
            if (!cancellation.IsCancellationRequested)
            {
                InlineDiffMarkers = markers;
                OnPropertyChanged(nameof(InlineDiffMarkers));
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // A different file or diff replaced this one.
        }
        finally
        {
            if (ReferenceEquals(_inlineDiffCancellation, cancellation))
            {
                _inlineDiffCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private void CancelInlineDiff()
    {
        _inlineDiffCancellation?.Cancel();
        _inlineDiffCancellation = null;
        InlineDiffLoading = Task.CompletedTask;
    }

    /// <summary>Loads a text, which is unchanged afterwards (as <c>FileViewer.TextLoaded</c>).</summary>
    /// <param name="contentIdentification">What the text is, to keep the position when it is shown again (<see cref="ContentIdentification"/>).</param>
    public void Load(string text, string? fileName = null, int? line = null, string? contentIdentification = null)
    {
        CancelInlineDiff();
        GitColoring = null;
        InlineDiffMarkers = [];
        DiffMode = DiffViewMode.Diff;
        ShowLeftLineNumbers = true;
        FirstChangeContextLines = null;
        LoadText(text, fileName, line, diffLines: null, contentIdentification);
    }

    private void LoadText(string text, string? fileName, int? line, IReadOnlyList<DiffLine>? diffLines, string? contentIdentification)
    {
        ContentIdentification = contentIdentification;
        DiffLines = diffLines;
        FileName = fileName;
        LineToShow = line;
        _loadedText = text;
        Text = text;
        OnPropertyChanged(nameof(HasChanges));
        TextLoaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///  As <c>GoToNextChange</c> and <c>GoToPreviousChange</c>: the first line of the next (or previous) block of added or
    ///  removed lines (<c>IsSearchMatch</c>; for a range diff, of commit headers) from <paramref name="currentLine"/> (1-based),
    ///  or <see langword="null"/> if there is none (or no diff is shown).
    /// </summary>
    public int? GetChangeLine(int currentLine, bool backwards)
    {
        if (DiffLines is not { } lines)
        {
            return null;
        }

        DiffViewMode mode = DiffMode;
        HashSet<int> changed = [.. lines.Where(l => mode.IsSearchMatch(l.Kind)).Select(l => l.LineNumInDiff)];
        List<int> starts = [.. changed.Where(line => !changed.Contains(line - 1)).Order()];
        return backwards
            ? starts.LastOrDefault(line => line < currentLine) is int previous and > 0 ? previous : null
            : starts.FirstOrDefault(line => line > currentLine) is int next and > 0 ? next : null;
    }

    /// <summary>
    ///  As <c>CurrentFileLine</c> and <c>CurrentFileColumn</c> of the viewer: the line (1-based; in a diff, of the new file, else
    ///  of the old one) and the column (0-based) at the caret, <see langword="null"/> on a line of a diff without line number.
    /// </summary>
    public (int Line, int Column)? GetCurrentFilePosition()
    {
        int caret = Math.Clamp(CaretOffset, 0, Text.Length);
        int lineStart = caret == 0 ? 0 : Text.LastIndexOf('\n', caret - 1) + 1;
        int line = 1 + Text.AsSpan(0, lineStart).Count('\n');
        int column = caret - lineStart;
        if (DiffLines is null)
        {
            return (line, column);
        }

        return DiffLines.FirstOrDefault(diffLine => diffLine.LineNumInDiff == line) switch
        {
            { RightLineNumber: not DiffLine.NotApplicable } diffLine => (diffLine.RightLineNumber, column),
            { LeftLineNumber: not DiffLine.NotApplicable } diffLine => (diffLine.LeftLineNumber, column),
            _ => null,
        };
    }

    /// <summary>Marks the current text as saved.</summary>
    public void MarkSaved()
    {
        _loadedText = Text;
        OnPropertyChanged(nameof(HasChanges));
    }
}

/// <summary>How <see cref="TextEditorViewModel.LoadDiff(string, DiffLoadOptions)"/> shows a diff.</summary>
/// <param name="GitColors">The theme colors if the text is git's colored output (always for difftastic, grep and range diffs).</param>
/// <param name="ReverseGitColoring">Whether git colors the background (<c>AppSettings.ReverseGitColoring</c>).</param>
/// <param name="Mode">The kind of diff.</param>
/// <param name="IsGitWordDiff">Whether a colored patch is a git word diff (<c>DiffDisplayAppearance.GitWordDiff</c>).</param>
/// <param name="HighlightingFileName">The file whose syntax highlighting the diff gets (<c>ShowSyntaxHighlightingInDiff</c>), if any.</param>
/// <param name="DifftasticWidth">The width of the output of difftastic (<c>DFT_WIDTH</c>).</param>
/// <param name="ContentIdentification">What the diff is (the file name), to keep the position when it is shown again.</param>
/// <param name="FirstChangeContextLines">The lines of context above the first change the caret goes to (<c>GoToFirstChange</c>), if any.</param>
public sealed record DiffLoadOptions(
    IThemeColors? GitColors = null,
    bool ReverseGitColoring = true,
    DiffViewMode Mode = DiffViewMode.Diff,
    bool IsGitWordDiff = false,
    string? HighlightingFileName = null,
    int DifftasticWidth = 80,
    string? ContentIdentification = null,
    int? FirstChangeContextLines = null);

/// <summary>Where the next text loaded is shown (<c>ScrollToTop</c>, <c>ScrollToBottom</c>).</summary>
public enum TextScrollRequest
{
    None,
    Top,
    Bottom,
}
