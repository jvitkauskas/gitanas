using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Search;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The Avalonia text editor on AvaloniaEdit (the editing mode of the WinForms <c>FileViewer</c>; docs/avalonia-port/PLAN.md,
///  phase 3): line numbers, syntax highlighting by file extension, whitespace markers and search.
/// </summary>
/// <remarks>
///  The search is AvaloniaEdit's <see cref="SearchPanel"/> (match case, whole words, regular expressions, highlighting of all
///  matches, replace in editable texts), with the behaviour of <c>FindAndReplaceForm</c>: Ctrl+F (Ctrl+H to replace) starts with
///  the selected text or the word at the caret, and F3 (Shift+F3) finds the next (previous) match also once the panel is closed.
/// </remarks>
public partial class TextEditorView : UserControl
{
    private TextEditorViewModel? _viewModel;
    private bool _updatingText;
    private bool _updatingCaret;
    private readonly DarkThemeHighlightingAdapter _darkThemeAdapter = new();
    private readonly DiffLineNumberMargin _diffLineNumbers = new();
    private readonly DiffColorizer _diffColorizer = new();
    private TextMateColorizer? _textMate;
    private DiffBrushes? _diffBrushes;
    private DiffBackgroundRenderer? _diffBackground;
    private DiffAnchorRenderer? _diffAnchors;
    private readonly OccurrenceRenderer _occurrences = new();
    private IReadOnlyList<DiffLine>? _shownDiffLines;
    private int _scrollVersion;
    private string? _capturedForText;

    private static readonly FindAndReplaceStrings _findStrings = ViewStrings.Load<FindAndReplaceStrings>();

    static TextEditorView()
    {
        SearchPanelLocalization.Apply(_findStrings);
    }

    public TextEditorView()
    {
        InitializeComponent();
        Search = SearchPanel.Install(editor);
        UseFindNextInSearchPanel();

        // Before the text area handles the keys (the search commands of AvaloniaEdit).
        editor.AddHandler(KeyDownEvent, OnEditorPreviewKeyDown, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        editor.Document.Changed += OnDocumentChanged;
        editor.DocumentChanged += (_, e) =>
        {
            if (e.OldDocument is not null)
            {
                e.OldDocument.Changed -= OnDocumentChanged;
            }

            if (e.NewDocument is not null)
            {
                e.NewDocument.Changed += OnDocumentChanged;
            }

            _textMate?.Invalidate(fromLine: 1);
        };
        ActualThemeVariantChanged += (_, _) =>
        {
            if (_viewModel is not null)
            {
                ApplyOptions();
            }
        };
        editor.TextArea.Caret.PositionChanged += (_, _) =>
        {
            if (_viewModel is not null && !_updatingCaret)
            {
                _updatingCaret = true;
                try
                {
                    _viewModel.CaretOffset = editor.CaretOffset;
                }
                finally
                {
                    _updatingCaret = false;
                }
            }
        };

        // As SelectionManagerSelectionChanged: the occurrences of the selected text are highlighted.
        editor.TextArea.TextView.BackgroundRenderers.Add(_occurrences);
        editor.TextArea.SelectionChanged += (_, _) => UpdateOccurrences();
        editor.TextChanged += (_, _) =>
        {
            if (!_updatingText && _viewModel is not null)
            {
                _updatingText = true;
                try
                {
                    _viewModel.Text = editor.Text;
                }
                finally
                {
                    _updatingText = false;
                }
            }
        };
    }

    /// <summary>The AvaloniaEdit editor, e.g. for tests.</summary>
    public AvaloniaEdit.TextEditor Editor => editor;

    /// <summary>The search panel of the editor (as <c>FindAndReplaceForm</c>).</summary>
    public SearchPanel Search { get; }

    /// <summary>
    ///  As <c>FindAndReplaceForm.ShowFor</c>: opens the search panel with the text selected on one line, or else the word at the
    ///  caret, in replace mode only if the text is editable. A selection of several lines is searched only (<c>SetScanRegion</c>),
    ///  with the previous search.
    /// </summary>
    public void OpenSearch(bool replace)
    {
        ScanRegion = null;
        string text = editor.TextArea.Selection switch
        {
            { IsEmpty: true } => TextSearch.GetWordAt(editor.Document.Text, editor.CaretOffset),
            { IsMultiline: false } selection => selection.GetText(),
            _ => "",
        };
        if (editor.TextArea.Selection is { IsEmpty: false, IsMultiline: true, SurroundingSegment: { } region })
        {
            ScanRegion = (region.Offset, region.EndOffset);
        }

        Search.IsReplaceMode = replace && !editor.IsReadOnly;
        Search.Open();
        if (text.Length > 0)
        {
            Search.SearchPattern = text;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                // As FindAndReplaceForm.UpdateTitleBar: "(selection only)" in the label of the search box (the panel has no title).
                if (Search.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(box => box.Name == "PART_searchTextBox") is { } searchBox)
                {
                    searchBox.PlaceholderText = _findStrings.GetSearchLabel(selectionOnly: ScanRegion is not null);
                }

                Search.Reactivate();
            },
            DispatcherPriority.Input);
    }

    /// <summary>The part of the text searched only (a selection of several lines when the search was opened), if any.</summary>
    public (int Start, int End)? ScanRegion { get; private set; }

    /// <summary>
    ///  As <c>FindAndReplaceForm.FindNextAsync</c> (F3, Shift+F3, and the search panel): selects the next (or previous) match,
    ///  from the caret and looping around; once the panel is closed, with its last search and options. In a scan region, only
    ///  its matches (the region is dropped once the caret is out of it and the panel closed). With a <see cref="TextEditorViewModel.NextFileLoader"/>,
    ///  the search goes on in the next (or previous) files instead of looping around, while the panel is open.
    /// </summary>
    /// <returns>
    ///  <see langword="false"/> if the panel is closed and nothing was found, or there is nothing to search for; the panel is then
    ///  opened, which shows it (instead of the message box of <c>FindAndReplaceForm</c>).
    /// </returns>
    public bool FindNext(bool backward)
    {
        if (ScanRegion is { } scanRegion && !Search.IsOpened && (editor.CaretOffset < scanRegion.Start || editor.CaretOffset > scanRegion.End))
        {
            // The user moved out of the region once the search was closed (while it is open, the incremental search of the
            // panel may select a match out of it).
            ScanRegion = null;
        }

        List<ISearchResult>? results = FindAll();
        Func<bool, Task<object?>>? loadNextFile = Search.IsOpened && ScanRegion is null ? _viewModel?.NextFileLoader : null;
        if (results is { Count: 0 } && loadNextFile is not null)
        {
            _ = FindInNextFilesAsync(backward, loadNextFile);
            return true;
        }

        if (results is not { Count: > 0 })
        {
            if (Search.IsOpened)
            {
                return true;
            }

            Search.IsReplaceMode = false;
            Search.Open();
            Dispatcher.UIThread.Post(Search.Reactivate, DispatcherPriority.Input);
            return false;
        }

        int caret = editor.CaretOffset;
        ISearchResult? result = backward
            ? results.LastOrDefault(r => r.Offset < caret - editor.SelectionLength)
            : results.FirstOrDefault(r => r.Offset >= caret);
        if (result is null && loadNextFile is not null)
        {
            _ = FindInNextFilesAsync(backward, loadNextFile);
            return true;
        }

        Select(result ?? (backward ? results[^1] : results[0]));
        return true;
    }

    /// <summary>As the multi-file search of <c>FindNextAsync</c>: the first (or last) match of the next files that have one.</summary>
    private async Task FindInNextFilesAsync(bool backward, Func<bool, Task<object?>> loadNextFile)
    {
        HashSet<object> searched = [];
        while (await loadNextFile(backward) is { } file && searched.Add(file))
        {
            if (FindAll() is { Count: > 0 } results)
            {
                Select(backward ? results[^1] : results[0]);
                return;
            }
        }
    }

    /// <summary>The matches of the search of the panel (in the scan region, if any); <see langword="null"/> without a valid search.</summary>
    private List<ISearchResult>? FindAll()
    {
        string pattern = Search.SearchPattern ?? "";
        if (pattern.Length == 0)
        {
            return null;
        }

        ISearchStrategy strategy;
        try
        {
            strategy = SearchStrategyFactory.Create(pattern, ignoreCase: !Search.MatchCase, Search.WholeWords, Search.UseRegex ? SearchMode.RegEx : SearchMode.Normal);
        }
        catch (SearchPatternException)
        {
            return null;
        }

        (int start, int end) = ScanRegion ?? (0, editor.Document.TextLength);
        return [.. strategy.FindAll(editor.Document, start, end - start).Where(r => r.Length > 0 && r.EndOffset <= end)];
    }

    /// <summary>As <c>SelectResult</c>: the match is selected, the caret at its end (where the next search starts).</summary>
    private void Select(ISearchResult result)
    {
        editor.Select(result.Offset, result.Length);
        editor.TextArea.Caret.Offset = result.EndOffset;
        editor.TextArea.Caret.BringCaretToView();
    }

    /// <summary>
    ///  The find next / previous of the search panel (its buttons, Enter and F3) search as <see cref="FindNext"/>: in the scan
    ///  region, and on in the next files.
    /// </summary>
    private void UseFindNextInSearchPanel()
    {
        Replace(SearchCommands.FindNext, backward: false);
        Replace(SearchCommands.FindPrevious, backward: true);

        void Replace(global::AvaloniaEdit.RoutedCommand command, bool backward)
        {
            foreach (global::AvaloniaEdit.RoutedCommandBinding binding in Search.CommandBindings.Where(b => b.Command == command).ToList())
            {
                Search.CommandBindings.Remove(binding);
            }

            Search.CommandBindings.Add(new global::AvaloniaEdit.RoutedCommandBinding(
                command,
                (_, e) =>
                {
                    FindNext(backward);
                    e.Handled = true;
                },
                (_, e) =>
                {
                    e.CanExecute = true;
                    e.Handled = true;
                }));
        }
    }

    /// <summary>The offsets of the occurrences of the selected text, e.g. for tests.</summary>
    public IReadOnlyList<int> Occurrences => _occurrences.Offsets;

    /// <summary>
    ///  As <c>GoToNextOccurrence</c> and <c>GoToPreviousOccurrence</c>: the next (or previous) occurrence of the selected text,
    ///  from the start of the selection. It is selected (the WinForms viewer only moved the caret; AvaloniaEdit drops a
    ///  selection the caret leaves, and the occurrences with it).
    /// </summary>
    public void GoToOccurrence(bool backwards)
    {
        int from = editor.TextArea.Selection.IsEmpty ? editor.CaretOffset : editor.SelectionStart;
        int target = backwards
            ? _occurrences.Offsets.LastOrDefault(o => o < from, -1)
            : _occurrences.Offsets.FirstOrDefault(o => o > from, -1);
        if (target >= 0)
        {
            editor.Select(target, _occurrences.Length);
            editor.TextArea.Caret.BringCaretToView();
        }
    }

    private void UpdateOccurrences()
    {
        _occurrences.Brush ??= AppColorResources.GetBrush(this, AppColor.HighlightAllOccurences);
        string selected = editor.TextArea.Selection.IsEmpty ? "" : editor.TextArea.Selection.GetText();
        if (_occurrences.Update(editor.Document.Text, selected))
        {
            editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
        }
    }

    /// <summary>The first line in view (1-based).</summary>
    /// <remarks>Fractional line heights can round an exact line boundary back into the preceding line in the height tree.</remarks>
    public int FirstVisibleLine
        => editor.TextArea.TextView.GetDocumentLineByVisualTop(editor.VerticalOffset + 0.000001)?.LineNumber ?? 1;

    /// <summary>The number of lines in view.</summary>
    public int VisibleLineCount
        => (int)(editor.TextArea.TextView.Bounds.Height / Math.Max(1, editor.TextArea.TextView.DefaultLineHeight));

    /// <summary>
    ///  As setting <c>FirstVisibleLine</c>: the line (1-based) is shown at the top, once the layout knows the new text if it
    ///  does not yet.
    /// </summary>
    public void ScrollToFirstVisibleLine(int line)
        => ScrollTo(() => editor.TextArea.TextView.GetVisualTopByDocumentLine(Math.Clamp(line, 1, Math.Max(1, editor.Document.LineCount))));

    private void ScrollTo(Func<double> getOffset)
    {
        int version = ++_scrollVersion;
        double offset = getOffset();
        SetVerticalOffset(offset);
        if (Math.Abs(editor.VerticalOffset - offset) > 0.5)
        {
            // Again once the extent of a new text is measured, unless something else scrolled meanwhile.
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (version == _scrollVersion)
                    {
                        SetVerticalOffset(getOffset());
                    }
                },
                DispatcherPriority.Loaded);
        }
    }

    /// <summary>The offset of the scroll viewer of the editor (its <c>ScrollToVerticalOffset</c> does not scroll).</summary>
    private void SetVerticalOffset(double offset)
    {
        if (editor.FindDescendantOfType<ScrollViewer>() is { } scrollViewer)
        {
            scrollViewer.Offset = scrollViewer.Offset.WithY(Math.Max(0, offset));
        }
    }

    private void CapturePosition()
    {
        _viewModel!.PositionCache.Capture(GetPosition(), editor.Document.LineCount, _shownDiffLines);
        _capturedForText = _viewModel.Text;
    }

    private ViewerPosition GetPosition()
    {
        int caretLine = editor.TextArea.Caret.Line;
        int firstVisibleLine = FirstVisibleLine;
        return new ViewerPosition(caretLine, editor.TextArea.Caret.Column, firstVisibleLine,
            CaretVisible: caretLine >= firstVisibleLine && caretLine < firstVisibleLine + VisibleLineCount);
    }

    /// <summary>
    ///  As <c>GoToFirstChange</c>: the caret on the first change, shown below its lines of context unless it is in view
    ///  already.
    /// </summary>
    private void GoToFirstChange(int contextLines)
    {
        if (_viewModel!.GetChangeLine(0, backwards: false) is not int line)
        {
            return;
        }

        editor.TextArea.Caret.Line = line;
        editor.TextArea.Caret.Column = 1;
        int firstVisibleLine = FirstVisibleLine;
        if (line < firstVisibleLine || line >= firstVisibleLine + VisibleLineCount)
        {
            ScrollToFirstVisibleLine(Math.Max(1, line - contextLines - 1));
        }
    }

    /// <summary>The keys of <c>FindAndReplaceForm</c>, unless they are typed in the search panel (which handles them itself).</summary>
    private void OnEditorPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if ((e.Source as global::Avalonia.Visual)?.FindAncestorOfType<SearchPanel>(includeSelf: true) is { } panel)
        {
            // Enter in the search box and F3 search as FindNext; the other keys are the panel's (e.g. Enter to replace).
            bool inSearchBox = e.Source is TextBox box && ReferenceEquals(box, panel.GetVisualDescendants().OfType<TextBox>().FirstOrDefault());
            if ((e.Key == Key.Enter && inSearchBox) || e.Key == Key.F3)
            {
                FindNext(backward: e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
            }

            return;
        }

        // Cmd+F on macOS, where Cmd+H hides the application: Cmd+Option+F replaces there, as in Xcode.
        KeyModifiers command = KeyMapping.CommandModifier;
        switch (e.Key, e.KeyModifiers)
        {
            case (Key.F, var modifiers) when modifiers == command:
                OpenSearch(replace: false);
                e.Handled = true;
                break;
            case (Key.H, var modifiers) when modifiers == command && !OperatingSystem.IsMacOS() && !editor.IsReadOnly:
            case (Key.F, var macOSModifiers) when macOSModifiers == (command | KeyModifiers.Alt) && OperatingSystem.IsMacOS() && !editor.IsReadOnly:
                OpenSearch(replace: true);
                e.Handled = true;
                break;
            case (Key.F3, KeyModifiers.None):
                FindNext(backward: false);
                e.Handled = true;
                break;
            case (Key.F3, KeyModifiers.Shift):
                FindNext(backward: true);
                e.Handled = true;
                break;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.TextLoaded -= OnTextLoaded;
        _viewModel?.FocusRequested -= OnFocusRequested;
        _viewModel = DataContext as TextEditorViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.TextLoaded += OnTextLoaded;
        _viewModel?.FocusRequested += OnFocusRequested;
        _viewModel?.SelectedTextProvider = () => editor.SelectedText;

        if (_viewModel is not null)
        {
            ApplyOptions();
            OnTextLoaded(this, EventArgs.Empty);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TextEditorViewModel.InlineDiffMarkers):
                ShowDiff(_viewModel!.DiffLines);
                break;

            case nameof(TextEditorViewModel.Text) when !_updatingText:
                // Before the text is replaced (a text loaded replaces it before TextLoaded).
                CapturePosition();
                SetText(_viewModel!.Text);
                break;

            case nameof(TextEditorViewModel.CaretOffset) when !_updatingCaret:
                _updatingCaret = true;
                try
                {
                    editor.CaretOffset = Math.Clamp(_viewModel!.CaretOffset, 0, editor.Document.TextLength);
                }
                finally
                {
                    _updatingCaret = false;
                }

                break;

            case nameof(TextEditorViewModel.ShowLineNumbers):
                editor.ShowLineNumbers = _viewModel!.DiffLines is null && _viewModel.ShowLineNumbers;
                break;

            case nameof(TextEditorViewModel.IsReadOnly) or nameof(TextEditorViewModel.ShowWhitespace) or nameof(TextEditorViewModel.FileName):
                ApplyOptions();
                break;

            case nameof(TextEditorViewModel.VerticalRulerColumn):
                ApplyVerticalRuler();
                break;
        }
    }

    /// <summary>As <c>FileViewerInternal.VRulerPosition</c>: a vertical line after the column, none for 0.</summary>
    private void ApplyVerticalRuler()
    {
        int column = _viewModel!.VerticalRulerColumn;
        editor.Options.ShowColumnRulers = column > 0;
        editor.Options.ColumnRulerPositions = column > 0 ? [column] : [];
    }

    private void OnFocusRequested(object? sender, EventArgs e) => editor.TextArea.Focus();

    private void ApplyOptions()
    {
        TextEditorViewModel viewModel = _viewModel!;
        editor.IsReadOnly = viewModel.IsReadOnly;
        editor.Options.ShowSpaces = viewModel.ShowWhitespace;
        editor.Options.ShowTabs = viewModel.ShowWhitespace;
        editor.Options.ShowEndOfLine = viewModel.ShowWhitespace;

        // The TextMate grammar of the language, else the highlighting definition of AvaloniaEdit (fewer languages).
        IList<IVisualLineTransformer> transformers = editor.TextArea.TextView.LineTransformers;
        if (_textMate is not null)
        {
            transformers.Remove(_textMate);
            _textMate = null;
        }

        bool isDarkTheme = ActualThemeVariant == ThemeVariant.Dark;
        _textMate = viewModel.FileName is { } highlightedFile ? TextMateColorizer.TryCreate(highlightedFile, isDarkTheme) : null;
        editor.SyntaxHighlighting = _textMate is null && viewModel.FileName is { } fileName && Path.GetExtension(fileName) is { Length: > 0 } extension
            ? HighlightingManager.Instance.GetDefinitionByExtension(extension)
            : null;
        if (_textMate is not null)
        {
            _textMate.SetDiff(viewModel.DiffLines, DiffLinesAnalyzer.IsCombinedDiff(editor.Text));
            transformers.Insert(0, _textMate);
        }

        _diffColorizer.KeepsSyntaxColors = _textMate is not null;

        // After the highlighting, which setting it may have added.
        transformers.Remove(_darkThemeAdapter);
        if (editor.SyntaxHighlighting is not null && isDarkTheme)
        {
            transformers.Add(_darkThemeAdapter);
        }

        // Git's colors of a diff come last, over the syntax highlighting (as the markers of the WinForms viewer).
        if (transformers.Remove(_diffColorizer))
        {
            transformers.Add(_diffColorizer);
        }

        ApplyVerticalRuler();
    }

    private void OnDocumentChanged(object? sender, DocumentChangeEventArgs e)
    {
        if (_textMate is null)
        {
            return;
        }

        // The lines after the change are tokenized again from the new state of the changed line.
        _textMate.Invalidate(editor.Document.GetLineByOffset(Math.Min(e.Offset, editor.Document.TextLength)).LineNumber);
        editor.TextArea.TextView.Redraw();
    }

    private void OnTextLoaded(object? sender, EventArgs e)
    {
        TextEditorViewModel viewModel = _viewModel!;

        // As SetText of FileViewerInternal: the position of the content shown until now, kept for the same content.
        if (!ReferenceEquals(_capturedForText, viewModel.Text) || editor.Text != viewModel.Text)
        {
            CapturePosition();
        }

        _capturedForText = null;
        _scrollVersion++;
        SetText(viewModel.Text);
        ShowDiff(viewModel.DiffLines);
        _shownDiffLines = viewModel.DiffLines;
        UpdateOccurrences();
        TextScrollRequest scroll = viewModel.PendingScroll;
        viewModel.PendingScroll = TextScrollRequest.None;
        ViewerPosition? restored = viewModel.PositionCache.Restore(viewModel.ContentIdentification, editor.Document.LineCount, viewModel.DiffLines, VisibleLineCount);

        // Show the requested line with the caret on it (as ViewPrivateAsync with a line).
        if (viewModel.LineToShow is int line && line > 0 && line <= editor.Document.LineCount)
        {
            editor.TextArea.Caret.Line = line;
            editor.ScrollToLine(line);
            return;
        }

        if (restored is { } position)
        {
            editor.TextArea.Caret.Line = position.CaretLine;
            editor.TextArea.Caret.Column = position.CaretColumn;
            ScrollToFirstVisibleLine(position.FirstVisibleLine);
        }
        else
        {
            editor.TextArea.Caret.Offset = 0;
            editor.ScrollToHome();
            SetVerticalOffset(0);
        }

        // The kept position counts if the caret is below the header of a patch (FirstLineAfterHeader).
        bool hasPatchHeader = viewModel.DiffLines is not null && viewModel.DiffMode is DiffViewMode.Diff or DiffViewMode.FixedDiff or DiffViewMode.CombinedDiff;
        bool positionSet = restored is { } kept && kept.CaretLine > (hasPatchHeader ? 6 : 1);
        if (scroll != TextScrollRequest.None)
        {
            // As ScrollToTop and ScrollToBottom (the continuous scroll into the previous or next file).
            ScrollTo(() => scroll == TextScrollRequest.Top ? 0 : Math.Max(0, editor.TextArea.TextView.DocumentHeight - editor.ViewportHeight));
            positionSet = true;
        }

        if (!positionSet && viewModel.FirstChangeContextLines is int contextLines)
        {
            GoToFirstChange(contextLines);
        }
    }

    /// <summary>
    ///  A diff shows its line numbers in the old and new file (on the colors of the kinds of the lines, as
    ///  <c>DiffViewerLineNumberControl</c>), its added and removed lines colored (by git, or as
    ///  <c>DiffHighlightService.HighlightAddedAndDeletedLines</c>) and the in-line differences of the matching lines.
    /// </summary>
    private void ShowDiff(IReadOnlyList<DiffLine>? lines)
    {
        TextView textView = editor.TextArea.TextView;
        bool isDiff = lines is not null;
        editor.ShowLineNumbers = !isDiff && _viewModel!.ShowLineNumbers;
        editor.TextArea.LeftMargins.Remove(_diffLineNumbers);
        if (_diffBackground is not null)
        {
            textView.BackgroundRenderers.Remove(_diffBackground);
        }

        if (_diffAnchors is not null)
        {
            textView.BackgroundRenderers.Remove(_diffAnchors);
        }

        textView.LineTransformers.Remove(_diffColorizer);
        _textMate?.SetDiff(lines, isDiff && DiffLinesAnalyzer.IsCombinedDiff(editor.Text));
        if (!isDiff)
        {
            return;
        }

        TextEditorViewModel viewModel = _viewModel!;
        _diffBrushes ??= CreateDiffBrushes();
        editor.TextArea.LeftMargins.Insert(0, _diffLineNumbers);
        _diffLineNumbers.Brushes = new DiffMarginBrushes(_diffBrushes.Added, _diffBrushes.Removed, _diffBrushes.Header);
        _diffLineNumbers.ShowLeftColumn = viewModel.ShowLeftLineNumbers;
        _diffLineNumbers.Lines = lines!;

        _diffBackground ??= new DiffBackgroundRenderer(_diffBrushes.Added, _diffBrushes.Removed, _diffBrushes.Header);
        _diffAnchors ??= new DiffAnchorRenderer(_diffBrushes.AddedAnchor, _diffBrushes.RemovedAnchor);
        if (viewModel.GitColoring is null)
        {
            _diffBackground.Lines = lines!;
            textView.BackgroundRenderers.Add(_diffBackground);
        }

        // As DiffHighlightService.AddInlineDifferenceMarkers: git's colors on the text are dimmed unless git colors the background.
        bool dimBackground = viewModel.GitColoring is not { Reverse: false };
        _diffColorizer.Update(viewModel.GitColoring, viewModel.InlineDiffMarkers, dimBackground ? _diffBrushes.DimmedBack : _diffBrushes.DimmedFore);
        textView.LineTransformers.Add(_diffColorizer);
        _diffAnchors.Markers = viewModel.InlineDiffMarkers;
        textView.BackgroundRenderers.Add(_diffAnchors);
        textView.Redraw();
    }

    /// <summary>
    ///  The colors of a diff, as <c>DiffHighlightService</c>: the lines without git's colors (<c>HighlightAddedAndDeletedLines</c>),
    ///  and their identical parts twice dimmed on the background, or once dimmed on the text (<c>CreateDimmedMarker</c>).
    /// </summary>
    private DiffBrushes CreateDiffBrushes()
    {
        Color? addedBack = AppColorResources.GetColor(this, AppColor.AnsiTerminalGreenBackNormal);
        Color? removedBack = AppColorResources.GetColor(this, AppColor.AnsiTerminalRedBackNormal);
        Color? addedFore = AppColorResources.GetColor(this, AppColor.AnsiTerminalGreenForeBold);
        Color? removedFore = AppColorResources.GetColor(this, AppColor.AnsiTerminalRedForeBold);
        Color background = AppColorResources.GetColor(this, AppColor.EditorBackground) ?? Colors.White;
        return new DiffBrushes(
            Added: ToBrush(addedBack),
            Removed: ToBrush(removedBack),
            Header: AppColorResources.GetBrush(this, AppColor.DiffSection),
            DimmedBack: new InlineDiffBrushes(ToBrush(Dim(Dim(addedBack))), ToBrush(Dim(Dim(removedBack))), AddedFore: null, RemovedFore: null),
            DimmedFore: new InlineDiffBrushes(new SolidColorBrush(background), new SolidColorBrush(background), ToBrush(Dim(addedFore)), ToBrush(Dim(removedFore))),
            AddedAnchor: ToBrush(addedFore),
            RemovedAnchor: ToBrush(removedFore));

        Color? Dim(Color? color) => color is { } c ? AppColorResources.Dim(c, background) : null;

        static IBrush? ToBrush(Color? color) => color is { } c ? new SolidColorBrush(c) : null;
    }

    private sealed record DiffBrushes(
        IBrush? Added,
        IBrush? Removed,
        IBrush? Header,
        InlineDiffBrushes DimmedBack,
        InlineDiffBrushes DimmedFore,
        IBrush? AddedAnchor,
        IBrush? RemovedAnchor);

    private void SetText(string text)
    {
        if (editor.Text == text)
        {
            return;
        }

        _updatingText = true;
        try
        {
            editor.Text = text;
        }
        finally
        {
            _updatingText = false;
        }
    }
}
