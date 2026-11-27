using System.Text;
using CincoVertice.MyTools.Ui.RegexTester.Models;

namespace CincoVertice.MyTools.Ui.RegexTester.Services;

/// <summary>
///     Writes a part chosen in the Add menu (<see cref="RegexPieces"/>) into the pattern, where its caret or selection
///     is: around the part selected, else with a placeholder selected to type over.
/// </summary>
public static class RegexPatternBuilder
{
    // Characters with a meaning of their own outside a class, written with a backslash to stand for themselves
    private const string Special = @"\*+?|{}[]()^$.#";

    public static RegexEditModel Insert(RegexPieceRequestModel request)
    {
        RegexPieceModel piece = request.Piece;
        RegexEditModel pattern = Clamped(request.Pattern);
        string selected = pattern.Text.Substring(pattern.SelectionStart, pattern.SelectionLength);

        if (piece.IsSampleText)
        {
            return Replaced(pattern, Escape(request.SampleSelection), pick: null);
        }

        if (piece.IsQuantifier)
        {
            string target = selected.Length == 0 || IsOneToken(selected) ? selected : $"(?:{selected})";

            return Replaced(pattern, target + piece.Template, piece.Pick, pickAfter: target.Length);
        }

        int valueAt = piece.Template.IndexOf(RegexPieceModel.ValueSign, StringComparison.Ordinal);

        if (valueAt < 0)
        {
            return Replaced(pattern, piece.Template, piece.Pick);
        }

        string before = piece.Template[..valueAt];
        string after = piece.Template[(valueAt + RegexPieceModel.ValueSign.Length)..];

        if (selected.Length > 0)
        {
            return Replaced(pattern, before + selected + after, piece.Pick);
        }

        // Nothing selected: the placeholder is, to type over
        string placeholder = piece.Placeholder ?? string.Empty;
        int start = pattern.SelectionStart;

        return new RegexEditModel(
            pattern.Text[..start] + before + placeholder + after + pattern.Text[start..],
            start + before.Length,
            placeholder.Length);
    }

    /// <summary>The text as a pattern matching it as it is: its special characters, tabs and new lines escaped.</summary>
    public static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length);

        foreach (char c in text)
        {
            switch (c)
            {
                case '\n':
                    escaped.Append(@"\n");
                    break;
                case '\r':
                    escaped.Append(@"\r");
                    break;
                case '\t':
                    escaped.Append(@"\t");
                    break;
                default:
                    if (Special.Contains(c))
                    {
                        escaped.Append('\\');
                    }

                    escaped.Append(c);
                    break;
            }
        }

        return escaped.ToString();
    }

    /// <summary>
    ///     Whether a count after this part applies to all of it: a character, an escape as \d or \p{L}, a class or a
    ///     group. Otherwise it applies only to its last character, so the part needs a group around it.
    /// </summary>
    public static bool IsOneToken(string part)
    {
        if (part.Length == 0)
        {
            return false;
        }

        if (part.Length == 1)
        {
            return !Special.Contains(part[0]) || part == ".";
        }

        return part[0] switch
        {
            '\\' => part.Length == 2
                || (part.Length > 4 && part[1] is 'p' or 'P' && part[2] == '{' && part.IndexOf('}') == part.Length - 1),
            '[' => ClassEnd(part) == part.Length - 1,
            '(' => GroupEnd(part) == part.Length - 1,
            _ => false,
        };
    }

    // Where the class that starts the part ends; -1 if it doesn't. A class doesn't nest, and a ] first in it is a
    // character of it.
    private static int ClassEnd(string part)
    {
        int first = part.Length > 1 && part[1] == '^' ? 2 : 1;

        for (int i = first; i < part.Length; i++)
        {
            if (part[i] == '\\')
            {
                i++;
            }
            else if (part[i] == ']' && i > first)
            {
                return i;
            }
        }

        return -1;
    }

    // Where the group that starts the part ends, skipping escaped characters and classes; -1 if it doesn't
    private static int GroupEnd(string part)
    {
        int depth = 0;

        for (int i = 0; i < part.Length; i++)
        {
            switch (part[i])
            {
                case '\\':
                    i++;
                    break;
                case '[':
                    int end = ClassEnd(part[i..]);

                    if (end < 0)
                    {
                        return -1;
                    }

                    i += end;
                    break;
                case '(':
                    depth++;
                    break;
                case ')' when --depth == 0:
                    return i;
            }
        }

        return -1;
    }

    private static RegexEditModel Clamped(RegexEditModel pattern)
    {
        int start = Math.Clamp(pattern.SelectionStart, 0, pattern.Text.Length);
        int length = Math.Clamp(pattern.SelectionLength, 0, pattern.Text.Length - start);

        return pattern with { SelectionStart = start, SelectionLength = length };
    }

    // The selection replaced by what's written, then the pick selected in it (looked for from pickAfter), or else the
    // caret after it
    private static RegexEditModel Replaced(RegexEditModel pattern, string written, string? pick, int pickAfter = 0)
    {
        int start = pattern.SelectionStart;
        string text = pattern.Text[..start] + written + pattern.Text[(start + pattern.SelectionLength)..];
        int pickAt = pick is null ? -1 : written.IndexOf(pick, pickAfter, StringComparison.Ordinal);

        return pickAt >= 0
            ? new RegexEditModel(text, start + pickAt, pick!.Length)
            : new RegexEditModel(text, start + written.Length, 0);
    }
}
