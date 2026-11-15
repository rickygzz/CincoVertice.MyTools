using System.Text;
using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Diff.Services;

/// <summary>
///     The text of one side being edited, as lines, with its caret, its selection (from <see cref="Anchor"/> to
///     <see cref="Caret"/>) and its undo. It always has a line, empty for an empty text. Typing one character after
///     another is one step of undo, until the caret moves.
/// </summary>
public sealed class DiffEditBuffer
{
    private readonly List<string> _lines = [string.Empty];
    private readonly List<Edit> _undo = [];
    private readonly List<Edit> _redo = [];

    // Whether the last edit was typing, which the next one typed right after it joins
    private bool _isTyping;

    public IReadOnlyList<string> Lines => _lines;

    public DiffPosition Caret { get; private set; }

    public DiffPosition Anchor { get; private set; }

    public bool HasSelection => Caret != Anchor;

    public DiffPosition SelectionStart => Caret < Anchor ? Caret : Anchor;

    public DiffPosition SelectionEnd => Caret < Anchor ? Anchor : Caret;

    /// <summary>Goes up at each change of the text.</summary>
    public int Version { get; private set; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public string SelectedText => TextBetween(SelectionStart, SelectionEnd).Replace("\n", Environment.NewLine);

    /// <summary>Shows a new text, the caret at its start, with nothing to undo.</summary>
    public void SetLines(IReadOnlyList<string> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);

        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
        }

        Caret = Anchor = default;
        _undo.Clear();
        _redo.Clear();
        _isTyping = false;
        Version++;
    }

    /// <param name="extend">Whether the selection grows to it, rather than the caret moving alone.</param>
    public void MoveTo(DiffPosition position, bool extend)
    {
        Caret = Clamp(position);

        if (!extend)
        {
            Anchor = Caret;
        }

        _isTyping = false;
    }

    public void Select(DiffPosition anchor, DiffPosition caret)
    {
        Anchor = Clamp(anchor);
        Caret = Clamp(caret);
        _isTyping = false;
    }

    public void SelectAll() => Select(default, new DiffPosition(_lines.Count - 1, _lines[^1].Length));

    /// <summary>Selects the word at a place: letters, digits and _, or spaces, or one other character.</summary>
    public void SelectWord(DiffPosition position)
    {
        position = Clamp(position);
        string line = _lines[position.Line];

        if (line.Length == 0)
        {
            MoveTo(position, false);

            return;
        }

        int at = Math.Min(position.Column, line.Length - 1);
        int kind = KindOf(line[at]);
        int start = at;
        int end = at + 1;

        if (kind != OtherKind)
        {
            while (start > 0 && KindOf(line[start - 1]) == kind)
            {
                start--;
            }

            while (end < line.Length && KindOf(line[end]) == kind)
            {
                end++;
            }
        }

        Select(new DiffPosition(position.Line, start), new DiffPosition(position.Line, end));
    }

    /// <summary>Types text in place of the selection; line breaks of any kind start new lines.</summary>
    public void Insert(string text)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        bool isTyping = !HasSelection && text.Length == 1 && text != "\n";
        Replace(SelectionStart, SelectionEnd, text, isTyping);
    }

    /// <summary>Backspace: the selection, or the character, or with <paramref name="word"/> the word, before.</summary>
    public void DeleteBackward(bool word)
    {
        if (HasSelection)
        {
            Replace(SelectionStart, SelectionEnd, string.Empty, false);
        }
        else if (Caret != default)
        {
            Replace(word ? WordLeft(Caret) : CharLeft(Caret), Caret, string.Empty, false);
        }
    }

    /// <summary>Delete: the selection, or the character, or with <paramref name="word"/> the word, after.</summary>
    public void DeleteForward(bool word)
    {
        if (HasSelection)
        {
            Replace(SelectionStart, SelectionEnd, string.Empty, false);
        }
        else if (Caret != End)
        {
            Replace(Caret, word ? WordRight(Caret) : CharRight(Caret), string.Empty, false);
        }
    }

    public bool Undo()
    {
        if (_undo.Count == 0)
        {
            return false;
        }

        Edit edit = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        Apply(edit.Start, EndOf(edit.Start, edit.Inserted), edit.Removed);
        _redo.Add(edit);
        Anchor = edit.AnchorBefore;
        Caret = edit.CaretBefore;
        _isTyping = false;

        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0)
        {
            return false;
        }

        Edit edit = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        DiffPosition end = Apply(edit.Start, EndOf(edit.Start, edit.Removed), edit.Inserted);
        _undo.Add(edit);
        Caret = Anchor = end;
        _isTyping = false;

        return true;
    }

    public DiffPosition End => new(_lines.Count - 1, _lines[^1].Length);

    public DiffPosition Clamp(DiffPosition position)
    {
        int line = Math.Clamp(position.Line, 0, _lines.Count - 1);

        return new DiffPosition(line, Math.Clamp(position.Column, 0, _lines[line].Length));
    }

    public DiffPosition CharLeft(DiffPosition position)
    {
        if (position.Column > 0)
        {
            string line = _lines[position.Line];
            int column = position.Column - 1;

            // Both halves of a character outside the BMP
            if (column > 0 && char.IsLowSurrogate(line[column]) && char.IsHighSurrogate(line[column - 1]))
            {
                column--;
            }

            return position with { Column = column };
        }

        return position.Line > 0 ? new DiffPosition(position.Line - 1, _lines[position.Line - 1].Length) : position;
    }

    public DiffPosition CharRight(DiffPosition position)
    {
        string line = _lines[position.Line];

        if (position.Column < line.Length)
        {
            int column = position.Column + 1;

            if (column < line.Length && char.IsLowSurrogate(line[column]) && char.IsHighSurrogate(line[column - 1]))
            {
                column++;
            }

            return position with { Column = column };
        }

        return position.Line < _lines.Count - 1 ? new DiffPosition(position.Line + 1, 0) : position;
    }

    /// <summary>The start of the word before: spaces skipped, then letters and digits, or other characters.</summary>
    public DiffPosition WordLeft(DiffPosition position)
    {
        if (position.Column == 0)
        {
            return CharLeft(position);
        }

        string line = _lines[position.Line];
        int column = position.Column;

        while (column > 0 && KindOf(line[column - 1]) == SpaceKind)
        {
            column--;
        }

        if (column > 0)
        {
            int kind = KindOf(line[column - 1]);

            while (column > 0 && KindOf(line[column - 1]) == kind)
            {
                column--;
            }
        }

        return position with { Column = column };
    }

    /// <summary>The start of the word after: the rest of this word skipped, then spaces.</summary>
    public DiffPosition WordRight(DiffPosition position)
    {
        string line = _lines[position.Line];

        if (position.Column >= line.Length)
        {
            return CharRight(position);
        }

        int column = position.Column;
        int kind = KindOf(line[column]);

        while (column < line.Length && KindOf(line[column]) == kind)
        {
            column++;
        }

        while (column < line.Length && KindOf(line[column]) == SpaceKind)
        {
            column++;
        }

        return position with { Column = column };
    }

    private const int SpaceKind = 0;
    private const int WordKind = 1;
    private const int OtherKind = 2;

    private static int KindOf(char c) =>
        char.IsWhiteSpace(c) ? SpaceKind : char.IsLetterOrDigit(c) || c == '_' ? WordKind : OtherKind;

    private static DiffPosition EndOf(DiffPosition start, string text)
    {
        int lastBreak = text.LastIndexOf('\n');

        return lastBreak < 0
            ? start with { Column = start.Column + text.Length }
            : new DiffPosition(start.Line + text.Count(c => c == '\n'), text.Length - lastBreak - 1);
    }

    private void Replace(DiffPosition start, DiffPosition end, string text, bool isTyping)
    {
        string removed = TextBetween(start, end);

        if (removed.Length == 0 && text.Length == 0)
        {
            return;
        }

        // Typed right after what was typed before: the same step of undo
        if (isTyping && _isTyping && _undo.Count > 0 && _undo[^1] is { Removed.Length: 0 } last
            && EndOf(last.Start, last.Inserted) == start && !last.Inserted.Contains('\n'))
        {
            _undo[^1] = last with { Inserted = last.Inserted + text };
        }
        else
        {
            _undo.Add(new Edit(start, removed, text, Caret, Anchor));
        }

        _redo.Clear();
        Caret = Anchor = Apply(start, end, text);
        _isTyping = isTyping;
    }

    // Puts text in place of what's between two places; returns the end of what was put
    private DiffPosition Apply(DiffPosition start, DiffPosition end, string text)
    {
        string before = _lines[start.Line][..start.Column];
        string after = _lines[end.Line][end.Column..];
        string[] parts = text.Split('\n');
        parts[0] = before + parts[0];
        int lastColumn = parts[^1].Length;
        DiffPosition position = parts.Length == 1
            ? new DiffPosition(start.Line, parts[0].Length)
            : new DiffPosition(start.Line + parts.Length - 1, lastColumn);
        parts[^1] += after;

        _lines.RemoveRange(start.Line, end.Line - start.Line + 1);
        _lines.InsertRange(start.Line, parts);
        Version++;

        return position;
    }

    private string TextBetween(DiffPosition start, DiffPosition end)
    {
        if (start.Line == end.Line)
        {
            return _lines[start.Line][start.Column..end.Column];
        }

        var text = new StringBuilder(_lines[start.Line][start.Column..]);

        for (int line = start.Line + 1; line < end.Line; line++)
        {
            text.Append('\n').Append(_lines[line]);
        }

        return text.Append('\n').Append(_lines[end.Line][..end.Column]).ToString();
    }

    /// <summary>A change to undo: at <see cref="Start"/>, <see cref="Removed"/> became <see cref="Inserted"/>.</summary>
    private sealed record Edit(
        DiffPosition Start,
        string Removed,
        string Inserted,
        DiffPosition CaretBefore,
        DiffPosition AnchorBefore);
}
