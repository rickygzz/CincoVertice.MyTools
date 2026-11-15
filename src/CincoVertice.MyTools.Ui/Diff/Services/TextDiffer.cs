using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Diff.Services;

/// <summary>
///     Compares two texts line by line (<see cref="MyersDiff"/>), into rows side by side. Where lines were removed and
///     others added in their place, they pair up into changed rows, the first removed with the first added, and so on;
///     the words that differ in a pair are marked. Lines end at \r\n, \n or \r alike, so line endings never differ.
/// </summary>
public static class TextDiffer
{
    /// <summary>Lines with more words than this are marked whole when changed, rather than word by word.</summary>
    public const int MaxWordsCompared = 4000;

    public static DiffResultModel Compare(string left, string right, DiffOptionsModel options)
    {
        string[] leftLines = SplitLines(left);
        string[] rightLines = SplitLines(right);

        // Each different line gets a number, lines equal as the options say the same one
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        int[] a = [.. leftLines.Select(line => IdOf(Normalized(line, options), ids))];
        int[] b = [.. rightLines.Select(line => IdOf(Normalized(line, options), ids))];
        (bool[] removed, bool[] added) = MyersDiff.Compare(a, b);

        var rows = new List<DiffRowModel>(Math.Max(a.Length, b.Length));
        var hunks = new List<DiffHunkModel>();
        int addedCount = 0;
        int removedCount = 0;
        int changedCount = 0;
        int i = 0;
        int j = 0;

        while (i < a.Length || j < b.Length)
        {
            if (i < a.Length && j < b.Length && !removed[i] && !added[j])
            {
                rows.Add(new DiffRowModel(DiffRowKind.Same, Line(i, leftLines[i]), Line(j, rightLines[j])));
                i++;
                j++;

                continue;
            }

            // A change: the lines removed here, then those added
            int firstRow = rows.Count;
            int removedFrom = i;
            int addedFrom = j;

            while (i < a.Length && removed[i])
            {
                i++;
            }

            while (j < b.Length && added[j])
            {
                j++;
            }

            int pairs = Math.Min(i - removedFrom, j - addedFrom);

            for (int n = 0; n < pairs; n++)
            {
                int l = removedFrom + n;
                int r = addedFrom + n;
                (IReadOnlyList<DiffRange> leftChanges, IReadOnlyList<DiffRange> rightChanges) =
                    ChangedWords(leftLines[l], rightLines[r], options);
                rows.Add(new DiffRowModel(
                    DiffRowKind.Changed,
                    new DiffLineModel(l + 1, leftLines[l], leftChanges),
                    new DiffLineModel(r + 1, rightLines[r], rightChanges)));
            }

            for (int l = removedFrom + pairs; l < i; l++)
            {
                rows.Add(new DiffRowModel(DiffRowKind.Removed, Line(l, leftLines[l]), null));
            }

            for (int r = addedFrom + pairs; r < j; r++)
            {
                rows.Add(new DiffRowModel(DiffRowKind.Added, null, Line(r, rightLines[r])));
            }

            changedCount += pairs;
            removedCount += i - removedFrom - pairs;
            addedCount += j - addedFrom - pairs;

            if (rows.Count > firstRow)
            {
                hunks.Add(new DiffHunkModel(firstRow, rows.Count - firstRow));
            }
        }

        return new DiffResultModel(rows, hunks, addedCount, removedCount, changedCount);
    }

    /// <summary>
    ///     The lines of a text, without their breaks. A break at the very end ends the last line, rather than starting
    ///     an empty one; an empty text has no lines.
    /// </summary>
    public static string[] SplitLines(string text)
    {
        var lines = new List<string>();
        int start = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] is '\r' or '\n')
            {
                lines.Add(text[start..i]);

                if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }
        }

        if (start < text.Length)
        {
            lines.Add(text[start..]);
        }

        return [.. lines];
    }

    /// <summary>
    ///     The words of two lines that differ: runs of letters, digits and _, runs of spaces, and each other character.
    ///     Ignoring whitespace, spaces never count.
    /// </summary>
    public static (IReadOnlyList<DiffRange> Left, IReadOnlyList<DiffRange> Right) ChangedWords(
        string left,
        string right,
        DiffOptionsModel options)
    {
        List<DiffRange> leftWords = Words(left, options);
        List<DiffRange> rightWords = Words(right, options);

        if (leftWords.Count > MaxWordsCompared || rightWords.Count > MaxWordsCompared)
        {
            return (Whole(left), Whole(right));
        }

        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        int[] a = [.. leftWords.Select(word => IdOf(Normalized(Text(left, word), options), ids))];
        int[] b = [.. rightWords.Select(word => IdOf(Normalized(Text(right, word), options), ids))];
        (bool[] removed, bool[] added) = MyersDiff.Compare(a, b);

        return (Merged(left, leftWords, removed), Merged(right, rightWords, added));
    }

    private static string Text(string line, DiffRange range) => line.Substring(range.Start, range.Length);

    private static DiffLineModel Line(int index, string text) => new(index + 1, text, []);

    private static int IdOf(string text, Dictionary<string, int> ids)
    {
        if (!ids.TryGetValue(text, out int id))
        {
            id = ids.Count;
            ids.Add(text, id);
        }

        return id;
    }

    private static string Normalized(string text, DiffOptionsModel options)
    {
        if (options.IgnoreWhitespace)
        {
            text = string.Concat(text.Where(c => !char.IsWhiteSpace(c)));
        }

        return options.IgnoreCase ? text.ToUpperInvariant() : text;
    }

    private static List<DiffRange> Words(string line, DiffOptionsModel options)
    {
        var words = new List<DiffRange>();
        int i = 0;

        while (i < line.Length)
        {
            int start = i;
            char c = line[i];

            if (IsWordChar(c))
            {
                while (i < line.Length && IsWordChar(line[i]))
                {
                    i++;
                }
            }
            else if (char.IsWhiteSpace(c))
            {
                while (i < line.Length && char.IsWhiteSpace(line[i]))
                {
                    i++;
                }

                if (options.IgnoreWhitespace)
                {
                    continue;
                }
            }
            else
            {
                // A character outside the BMP is two chars, which stay together
                i += char.IsHighSurrogate(c) && i + 1 < line.Length ? 2 : 1;
            }

            words.Add(new DiffRange(start, i - start));
        }

        return words;
    }

    private static bool IsBlank(string line, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            if (!char.IsWhiteSpace(line[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static IReadOnlyList<DiffRange> Whole(string line) =>
        line.Length == 0 ? [] : [new DiffRange(0, line.Length)];

    // The changed words next to each other, or with only spaces between, as one range
    private static List<DiffRange> Merged(string line, List<DiffRange> words, bool[] changed)
    {
        var ranges = new List<DiffRange>();

        for (int i = 0; i < words.Count; i++)
        {
            if (!changed[i])
            {
                continue;
            }

            DiffRange word = words[i];

            if (ranges.Count > 0 && IsBlank(line, ranges[^1].End, word.Start))
            {
                ranges[^1] = new DiffRange(ranges[^1].Start, word.End - ranges[^1].Start);
            }
            else
            {
                ranges.Add(word);
            }
        }

        return ranges;
    }
}
