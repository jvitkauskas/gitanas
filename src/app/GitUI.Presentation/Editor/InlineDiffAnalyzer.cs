using System.Diagnostics;

namespace GitUI.Presentation.Editor;

/// <summary>The kind of an in-line difference marker.</summary>
public enum InlineDiffMarkerKind
{
    /// <summary>A part of a removed line that is identical in the matching added line (shown dimmed).</summary>
    DimmedRemoved,

    /// <summary>A part of an added line that is identical in the matching removed line (shown dimmed).</summary>
    DimmedAdded,

    /// <summary>Where text was inserted, in the removed line (a zero-length anchor in the color of added text).</summary>
    InsertionAnchor,

    /// <summary>Where text was deleted, in the added line (a zero-length anchor in the color of removed text).</summary>
    DeletionAnchor,
}

/// <summary>An in-line difference marker; <see cref="Offset"/> is in the text of the diff.</summary>
public readonly record struct InlineDiffMarker(int Offset, int Length, InlineDiffMarkerKind Kind);

/// <summary>A range of the text of the diff.</summary>
[DebuggerDisplay("{Offset}+{Length}")]
public readonly record struct DiffTextSegment(int Offset, int Length);

/// <summary>
///  Matches the removed and added lines of the blocks of a diff and marks their identical parts, so that the differences
///  stand out. A port of the WinForms <c>DiffHighlightService.AddInlineDifferenceMarkers</c> (for patches without git's colors)
///  and <c>LinesMatcher</c> (docs/avalonia-port/PLAN.md, phase 3); keep it in sync.
/// </summary>
public static class InlineDiffAnalyzer
{
    // To skip the prefixes '-' / '+' (this is only for the normal patch format).
    private const int DiffContentOffset = 1;

    public static IReadOnlyList<InlineDiffMarker> Analyze(string text, IReadOnlyList<DiffLine> lines, CancellationToken cancellationToken = default)
    {
        List<InlineDiffMarker> markers = [];
        if (lines.Count == 0)
        {
            return markers;
        }

        DiffTextSegment[] segments = GetLineSegments(text);
        DiffLine[] diffLines = [.. lines.OrderBy(l => l.LineNumInDiff)];
        int index = 0;

        // Process the next blocks of removed / added lines and mark in-line differences.
        while (index < diffLines.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // git-diff presents the removed lines directly followed by the added in a "block".
            List<DiffTextSegment> linesRemoved = GetBlockOfLines(diffLines, segments, DiffLineKind.Minus, ref index, found: false);
            if (linesRemoved.Count == 0)
            {
                continue;
            }

            List<DiffTextSegment> linesAdded = GetBlockOfLines(diffLines, segments, DiffLineKind.Plus, ref index, found: true);
            if (linesAdded.Count == 0)
            {
                continue;
            }

            foreach ((DiffTextSegment lineRemoved, DiffTextSegment lineAdded) in DiffLinesMatcher.FindLinePairs(GetText, linesRemoved, linesAdded))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddDifferenceMarkers(markers, GetText, lineRemoved, lineAdded, DiffContentOffset);
            }
        }

        return markers;

        string GetText(DiffTextSegment line)
            => line.Length < DiffContentOffset ? "" : text[(line.Offset + DiffContentOffset)..(line.Offset + line.Length)];
    }

    /// <summary>The lines of the text, without their line breaks (as the lines of the document).</summary>
    private static DiffTextSegment[] GetLineSegments(string text)
    {
        List<DiffTextSegment> segments = [];
        int start = 0;
        while (true)
        {
            int end = text.IndexOf('\n', start);
            int lineEnd = end < 0 ? text.Length : end;
            int length = lineEnd - start;
            if (length > 0 && text[lineEnd - 1] == '\r')
            {
                --length;
            }

            segments.Add(new DiffTextSegment(start, length));
            if (end < 0)
            {
                return [.. segments];
            }

            start = end + 1;
        }
    }

    /// <summary>As <c>DiffHighlightService.GetBlockOfLines</c>: the next block of lines of the kind, which may contain a few context lines.</summary>
    private static List<DiffTextSegment> GetBlockOfLines(DiffLine[] diffLines, DiffTextSegment[] segments, DiffLineKind kind, ref int index, bool found)
    {
        List<DiffTextSegment> result = [];
        int gapLines = 0;

        for (; index < diffLines.Length; ++index)
        {
            DiffLine diffLine = diffLines[index];
            if (diffLine.Kind != kind)
            {
                if (!found)
                {
                    // Start of block not found yet
                    continue;
                }

                const int maxGapLines = 5;
                if (diffLine.Kind == DiffLineKind.Context && gapLines < maxGapLines)
                {
                    // A gap of context lines, the block can be extended
                    ++gapLines;
                    continue;
                }

                // Block ended, no more to add (next start search here)
                break;
            }

            gapLines = 0;
            if (diffLine.IsMovedLine)
            {
                // Ignore this line, seem to be moved
                continue;
            }

            // In block, continue to add
            found = true;
            result.Add(segments[diffLine.LineNumInDiff - 1]);
        }

        return result;
    }

    /// <summary>As <c>DiffHighlightService.AddDifferenceMarkers</c> with a dimmed background (no git colors).</summary>
    internal static void AddDifferenceMarkers(List<InlineDiffMarker> markers, Func<DiffTextSegment, string> getText, DiffTextSegment lineRemoved, DiffTextSegment lineAdded, int beginOffset)
    {
        ReadOnlySpan<char> textRemoved = LimitLength(getText(lineRemoved).AsSpan());
        ReadOnlySpan<char> textAdded = LimitLength(getText(lineAdded).AsSpan());
        int offsetRemoved = lineRemoved.Offset + beginOffset;
        int offsetAdded = lineAdded.Offset + beginOffset;
        (int lengthIdenticalAtStart, int lengthIdenticalAtEnd) = AddDifferenceMarkers(markers, textRemoved, textAdded, offsetRemoved, offsetAdded);

        if (lengthIdenticalAtStart > 0)
        {
            markers.Add(new(offsetRemoved, lengthIdenticalAtStart, InlineDiffMarkerKind.DimmedRemoved));
            markers.Add(new(offsetAdded, lengthIdenticalAtStart, InlineDiffMarkerKind.DimmedAdded));
        }

        if (lengthIdenticalAtEnd > 0)
        {
            markers.Add(new(offsetRemoved + textRemoved.Length - lengthIdenticalAtEnd, lengthIdenticalAtEnd, InlineDiffMarkerKind.DimmedRemoved));
            markers.Add(new(offsetAdded + textAdded.Length - lengthIdenticalAtEnd, lengthIdenticalAtEnd, InlineDiffMarkerKind.DimmedAdded));
        }

        return;

        static ReadOnlySpan<char> LimitLength(ReadOnlySpan<char> text)
        {
            const int maxLength = 2000;
            return text.Length <= maxLength ? text : text[..maxLength];
        }
    }

    private static (int LengthIdenticalAtStart, int LengthIdenticalAtEnd) AddDifferenceMarkers(
        List<InlineDiffMarker> markers, ReadOnlySpan<char> textRemoved, ReadOnlySpan<char> textAdded, int offsetRemoved, int offsetAdded)
    {
        // See DiffHighlightService.AddDifferenceMarkers for an illustration of the algorithm.
        int lengthIdenticalAtStart = 0;
        int lengthIdenticalAtEnd = 0;

        int endRemoved = textRemoved.Length;
        int endAdded = textAdded.Length;
        if (endRemoved == endAdded && textRemoved.SequenceEqual(textAdded))
        {
            lengthIdenticalAtStart = endRemoved;
            return (lengthIdenticalAtStart, lengthIdenticalAtEnd);
        }

        (string? commonWord, int startIndexIdenticalRemoved, int startIndexIdenticalAdded) = DiffLinesMatcher.FindBestMatch(textRemoved.ToString(), textAdded.ToString());
        if (commonWord is not null)
        {
            int lengthIdentical = commonWord.Length;

            int startIndexRightPartRemoved = startIndexIdenticalRemoved + lengthIdentical;
            int startIndexRightPartAdded = startIndexIdenticalAdded + lengthIdentical;
            (int lengthIdenticalAtStartRightPart, lengthIdenticalAtEnd) = AddDifferenceMarkers(markers,
                textRemoved[startIndexRightPartRemoved..], textAdded[startIndexRightPartAdded..],
                offsetRemoved + startIndexRightPartRemoved, offsetAdded + startIndexRightPartAdded);
            lengthIdentical += lengthIdenticalAtStartRightPart;

            (lengthIdenticalAtStart, int lengthIdenticalAtLeftPartEnd) = AddDifferenceMarkers(markers,
                textRemoved[..startIndexIdenticalRemoved], textAdded[..startIndexIdenticalAdded],
                offsetRemoved, offsetAdded);
            lengthIdentical += lengthIdenticalAtLeftPartEnd;
            startIndexIdenticalRemoved -= lengthIdenticalAtLeftPartEnd;
            startIndexIdenticalAdded -= lengthIdenticalAtLeftPartEnd;

            // Join with the identical part at the start or the end, or dim the identical part.
            if (startIndexIdenticalRemoved == lengthIdenticalAtStart && startIndexIdenticalAdded == lengthIdenticalAtStart)
            {
                lengthIdenticalAtStart += lengthIdentical;
            }
            else if (startIndexIdenticalRemoved + lengthIdentical + lengthIdenticalAtEnd == endRemoved
                && startIndexIdenticalAdded + lengthIdentical + lengthIdenticalAtEnd == endAdded)
            {
                lengthIdenticalAtEnd += lengthIdentical;
            }
            else
            {
                markers.Add(new(offsetRemoved + startIndexIdenticalRemoved, lengthIdentical, InlineDiffMarkerKind.DimmedRemoved));
                markers.Add(new(offsetAdded + startIndexIdenticalAdded, lengthIdentical, InlineDiffMarkerKind.DimmedAdded));
            }
        }
        else
        {
            // Find the end of the identical part at the start.
            int minEnd = Math.Min(endRemoved, endAdded);
            while (lengthIdenticalAtStart < minEnd
                && textRemoved[lengthIdenticalAtStart] == textAdded[lengthIdenticalAtStart])
            {
                ++lengthIdenticalAtStart;
            }

            // Find the start of the identical part at the end.
            int startIndexIdenticalAtEndRemoved = endRemoved;
            int startIndexIdenticalAtEndAdded = endAdded;
            while (startIndexIdenticalAtEndRemoved > lengthIdenticalAtStart && startIndexIdenticalAtEndAdded > lengthIdenticalAtStart
                && textRemoved[startIndexIdenticalAtEndRemoved - 1] == textAdded[startIndexIdenticalAtEndAdded - 1])
            {
                --startIndexIdenticalAtEndRemoved;
                --startIndexIdenticalAtEndAdded;
                ++lengthIdenticalAtEnd;
            }

            int lengthDifferentRemoved = startIndexIdenticalAtEndRemoved - lengthIdenticalAtStart;
            int lengthDifferentAdded = startIndexIdenticalAtEndAdded - lengthIdenticalAtStart;
            if (lengthDifferentRemoved == 0 && lengthDifferentAdded > 0)
            {
                markers.Add(new(offsetRemoved + lengthIdenticalAtStart, 0, InlineDiffMarkerKind.InsertionAnchor));
            }
            else if (lengthDifferentRemoved > 0 && lengthDifferentAdded == 0)
            {
                markers.Add(new(offsetAdded + lengthIdenticalAtStart, 0, InlineDiffMarkerKind.DeletionAnchor));
            }
        }

        return (lengthIdenticalAtStart, lengthIdenticalAtEnd);
    }
}

/// <summary>A port of the WinForms <c>LinesMatcher</c> on <see cref="DiffTextSegment"/>; keep it in sync.</summary>
internal static class DiffLinesMatcher
{
    internal static IEnumerable<(DiffTextSegment RemovedLine, DiffTextSegment AddedLine)> FindLinePairs(
        Func<DiffTextSegment, string> getText, IReadOnlyList<DiffTextSegment> removedLines, IReadOnlyList<DiffTextSegment> addedLines)
    {
        int numberOfCombinations = removedLines.Count * addedLines.Count;
        if (numberOfCombinations < 1)
        {
            yield break;
        }

        // Do not try to match more lines than usually visible at the same time, because it costs O(n^2) operations
        const int maxCombinations = 100 * 100;
        if (numberOfCombinations == 1 || numberOfCombinations > maxCombinations)
        {
            int minCount = Math.Min(removedLines.Count, addedLines.Count);
            for (int i = 0; i < minCount; ++i)
            {
                yield return (removedLines[i], addedLines[i]);
            }

            yield break;
        }

        LineData[] removed = [.. removedLines.Select(line => new LineData(line, getText(line)))];
        LineData[] added = [.. addedLines.Select(line => new LineData(line, getText(line)))];

        foreach ((DiffTextSegment, DiffTextSegment) linePair in FindLinePairs(removed, added))
        {
            yield return linePair;
        }
    }

    private static IEnumerable<(DiffTextSegment RemovedLine, DiffTextSegment AddedLine)> FindLinePairs(LineData[] removed, LineData[] added)
    {
        (int removedIndex, int addedIndex) = FindBestMatch(removed, added);

        if (removedIndex > 0 && addedIndex > 0)
        {
            foreach ((DiffTextSegment, DiffTextSegment) linePair in FindLinePairs(removed[0..removedIndex], added[0..addedIndex]))
            {
                yield return linePair;
            }
        }

        yield return (removed[removedIndex].Line, added[addedIndex].Line);

        ++removedIndex;
        ++addedIndex;
        if (removedIndex < removed.Length && addedIndex < added.Length)
        {
            foreach ((DiffTextSegment, DiffTextSegment) linePair in FindLinePairs(removed[removedIndex..], added[addedIndex..]))
            {
                yield return linePair;
            }
        }
    }

    private static (int RemovedIndex, int AddedIndex) FindBestMatch(LineData[] removed, LineData[] added)
    {
        // first, search longest match of trimmed lines, i.e. detect indented lines
        (LineData longestMatchingRemoved, int matchingAddedIndex)
            = removed.Select(r => (r, addedIndex: Array.FindIndex(added, a => a.Trimmed == r.Trimmed)))
                     .MaxBy(pair => pair.addedIndex < 0 ? -1 : pair.r.Trimmed.Length);
        if (matchingAddedIndex >= 0)
        {
            return (Array.IndexOf(removed, longestMatchingRemoved), matchingAddedIndex);
        }

        // then match lines whose common words have the maximum summed-up length
        int removedMaxScoreIndex = 0;
        int addedMaxScoreIndex = 0;
        float maxScore = -1;
        foreach ((int removedIndex, int addedIndex) in GetAllCombinations(removed.Length, added.Length))
        {
            float score = GetWordMatchScore(removed[removedIndex], added[addedIndex]);
            if (maxScore < score)
            {
                maxScore = score;
                removedMaxScoreIndex = removedIndex;
                addedMaxScoreIndex = addedIndex;
                if (maxScore == 1)
                {
                    return (removedMaxScoreIndex, addedMaxScoreIndex);
                }
            }
        }

        const float insignificantWordMatchScore = 0.1f;
        return maxScore <= insignificantWordMatchScore ? (0, 0) : (removedMaxScoreIndex, addedMaxScoreIndex);

        static float GetWordMatchScore(LineData r, LineData a)
        {
            if (r.Words.Count == 0 || a.Words.Count == 0)
            {
                return -1;
            }

            return (float)r.Words.Intersect(a.Words).Sum(w => w.Length) / Math.Max(r.WordsTotalLength, a.WordsTotalLength);
        }
    }

    internal static (string? CommonWord, int StartIndexRemoved, int StartIndexAdded) FindBestMatch(string textRemoved, string textAdded)
    {
        (string Word, int StartIndex) notFound = ("", -1);
        (string Word, int StartIndex)[] wordsRemoved = [.. GetWords(textRemoved)];
        (string Word, int StartIndex)[] wordsAdded = [.. GetWords(textAdded)];
        (string? commonWord, int startIndexOfCommonWordAdded) = wordsAdded
            .IntersectBy(wordsRemoved.Select(SelectWord), SelectWord)
            .Union([notFound])
            .MaxBy(pair => pair.Word.Length);
        if (startIndexOfCommonWordAdded != notFound.StartIndex)
        {
            return (commonWord, wordsRemoved.First(pair => pair.Word == commonWord).StartIndex, startIndexOfCommonWordAdded);
        }

        (string Word, int StartIndex)[] subwordsRemoved = [.. GetSubwords(wordsRemoved)];
        (commonWord, startIndexOfCommonWordAdded) = GetSubwords(wordsAdded)
            .IntersectBy(subwordsRemoved.Select(SelectWord), SelectWord)
            .Union([notFound])
            .MaxBy(pair => pair.Word.Length);
        if (startIndexOfCommonWordAdded != notFound.StartIndex)
        {
            return (commonWord, subwordsRemoved.First(pair => pair.Word == commonWord).StartIndex, startIndexOfCommonWordAdded);
        }

        return (null, 0, 0);
    }

    /// <summary>
    ///  Iterates all combinations of indices - starting with (0,0), (1,0), (0,1), (2,0), (1,1), ...
    /// </summary>
    internal static IEnumerable<(int FirstIndex, int SecondIndex)> GetAllCombinations(int firstEnd, int secondEnd)
    {
        // upper left half including principal diagonal
        for (int diagonalIndex = 0; diagonalIndex < firstEnd; ++diagonalIndex)
        {
            int diagonalEnd = Math.Min(diagonalIndex + 1, secondEnd);
            for (int secondIndex = 0; secondIndex < diagonalEnd; ++secondIndex)
            {
                yield return (FirstIndex: diagonalIndex - secondIndex, secondIndex);
            }
        }

        // lower right half
        for (int diagonalIndex = 1; diagonalIndex < secondEnd; ++diagonalIndex)
        {
            int diagonalEnd = Math.Min(firstEnd + diagonalIndex, secondEnd);
            for (int secondIndex = diagonalIndex; secondIndex < diagonalEnd; ++secondIndex)
            {
                yield return (FirstIndex: firstEnd - 1 + diagonalIndex - secondIndex, secondIndex);
            }
        }
    }

    internal static IEnumerable<(string Word, int StartIndex)> GetSubwords(string word)
    {
        int endIndex = word.Length;
        if (endIndex == 0)
        {
            yield break;
        }

        int startIndex = 0;
        bool previousUpper = char.IsUpper(word[0]);
        for (int index = 0; index < endIndex; ++index)
        {
            bool currentUpper = char.IsUpper(word[index]);
            if (previousUpper != currentUpper)
            {
                previousUpper = currentUpper;
                if (currentUpper)
                {
                    // emit previous word, but no single '_'
                    if (!(index == 1 && !char.IsLetterOrDigit(word[0])))
                    {
                        yield return (word[startIndex..index], startIndex);
                    }

                    startIndex = index;
                }
            }

            // end word at '_', but join preceding '_' to first word
            if (index > 0 && !char.IsLetterOrDigit(word[index]))
            {
                if (startIndex < index && char.IsLetterOrDigit(word[index - 1]))
                {
                    yield return (word[startIndex..index], startIndex);
                }

                startIndex = index + 1;
                previousUpper = true;
            }
        }

        if (startIndex < endIndex && !(endIndex == 1 && !char.IsLetterOrDigit(word[0])))
        {
            yield return (word[startIndex..endIndex], startIndex);
        }
    }

    internal static IEnumerable<(string Word, int StartIndex)> GetSubwords(IEnumerable<(string Word, int StartIndex)> words)
    {
        foreach ((string Word, int StartIndex) word in words)
        {
            foreach ((string Word, int StartIndex) subword in GetSubwords(word.Word))
            {
                yield return (subword.Word, subword.StartIndex + word.StartIndex);
            }
        }
    }

    internal static IEnumerable<(string Word, int StartIndex)> GetWords(string text)
    {
        int length = text.Length;
        int start = 0;
        while (true)
        {
            for (; ; ++start)
            {
                if (start >= length)
                {
                    // no (more) word found
                    yield break;
                }

                if (IsWordChar(text[start]))
                {
                    break;
                }
            }

            for (int end = start + 1; ; ++end)
            {
                if (end >= length || !IsWordChar(text[end]))
                {
                    yield return (text[start..end], start);
                    start = end + 1;
                    break;
                }
            }
        }
    }

    /// <summary>As ICSharpCode's <c>TextUtilities.IsLetterDigitOrUnderscore</c>.</summary>
    internal static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    internal static string SelectWord((string Word, int StartIndex) pair) => pair.Word;

    [DebuggerDisplay("{Line.Offset}: {Trimmed}")]
    private readonly struct LineData
    {
        internal DiffTextSegment Line { get; }
        internal string Trimmed { get; }
        internal IReadOnlySet<string> Words { get; }
        internal int WordsTotalLength { get; }

        internal LineData(DiffTextSegment line, string text)
        {
            Line = line;
            Trimmed = text.Trim();
            Words = GetWords(Trimmed).Select(SelectWord).ToHashSet();
            WordsTotalLength = Words.Sum(w => w.Length);
        }
    }
}
