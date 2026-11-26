using System.Text;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     Turns almost-JSON into JSON, as logs, JavaScript and Python print it: strings in single quotes, names without
///     quotes, a comma too many before a closing bracket or missing between two values, comments, and Python's and
///     JavaScript's True, False, None, undefined, NaN and Infinity. The rest is kept as written, line breaks too.
/// </summary>
public static class JsonRepair
{
    private enum Last
    {
        None,
        Open,
        Colon,
        Comma,
        Value,
    }

    /// <returns>The text repaired, and what was changed, counted: "2 names quoted", or none.</returns>
    public static (string Text, IReadOnlyList<string> Fixes) Repair(string text)
    {
        var output = new StringBuilder(text.Length + 16);
        Dictionary<string, (string One, int Count)> fixes = [];
        Last last = Last.None;
        int valueEnd = -1;
        int commaAt = -1;
        int i = 0;

        void Fix(string one, string many) => fixes[many] = (one, fixes.GetValueOrDefault(many).Count + 1);

        // A value right after another one: the comma between them was left out
        void BeforeValue()
        {
            if (last == Last.Value && valueEnd >= 0)
            {
                output.Insert(valueEnd, ',');
                Fix("missing comma added", "missing commas added");
            }
        }

        void AfterValue()
        {
            last = Last.Value;
            valueEnd = output.Length;
        }

        while (i < text.Length)
        {
            char c = text[i];

            if (char.IsWhiteSpace(c))
            {
                output.Append(c);
                i++;
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] is '/' or '*')
            {
                i = SkipComment(text, i, output);
                Fix("comment removed", "comments removed");
            }
            else if (c is '"' or '\'')
            {
                BeforeValue();
                i = CopyString(text, i, output);

                if (c == '\'')
                {
                    Fix("string put in double quotes", "strings put in double quotes");
                }

                AfterValue();
            }
            else if (c is '{' or '[')
            {
                BeforeValue();
                output.Append(c);
                last = Last.Open;
                i++;
            }
            else if (c is '}' or ']')
            {
                if (last == Last.Comma && commaAt >= 0)
                {
                    output.Remove(commaAt, 1);
                    Fix("comma before a closing bracket removed", "commas before a closing bracket removed");
                }

                output.Append(c);
                AfterValue();
                i++;
            }
            else if (c == ':')
            {
                output.Append(c);
                last = Last.Colon;
                i++;
            }
            else if (c == ',')
            {
                if (last is Last.Comma or Last.Open)
                {
                    Fix("extra comma removed", "extra commas removed");
                }
                else
                {
                    commaAt = output.Length;
                    output.Append(c);
                    last = Last.Comma;
                }

                i++;
            }
            else if (char.IsDigit(c) || c is '.' || (c is '-' or '+' && i + 1 < text.Length && text[i + 1] is var next
                && (char.IsDigit(next) || next == '.')))
            {
                BeforeValue();
                i = CopyNumber(text, i, output, Fix);
                AfterValue();
            }
            else if (char.IsLetter(c) || c is '_' or '$'
                || (c == '-' && i + 1 < text.Length && char.IsLetter(text[i + 1])))
            {
                BeforeValue();
                int start = i;
                i++;

                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '$'))
                {
                    i++;
                }

                string word = text[start..i];

                if (NextSignificant(text, i) == ':' && !word.StartsWith('-'))
                {
                    output.Append(JsonTextWriter.Quoted(word));
                    Fix("name put in quotes", "names put in quotes");
                }
                else
                {
                    output.Append(LiteralOf(word, Fix));
                }

                AfterValue();
            }
            else
            {
                output.Append(c);
                i++;
            }
        }

        return (
            output.ToString(),
            [.. fixes.Select(fix => fix.Value.Count == 1 ? $"1 {fix.Value.One}" : $"{fix.Value.Count} {fix.Key}")]);
    }

    private static string LiteralOf(string word, Action<string, string> fix)
    {
        switch (word)
        {
            case "true" or "false" or "null":
                return word;
            case "True" or "TRUE":
                fix("Python or JavaScript value made JSON", "Python and JavaScript values made JSON");
                return "true";
            case "False" or "FALSE":
                fix("Python or JavaScript value made JSON", "Python and JavaScript values made JSON");
                return "false";
            case "None" or "NULL" or "Null" or "nil" or "undefined" or "NaN" or "Infinity" or "-Infinity":
                fix("Python or JavaScript value made JSON", "Python and JavaScript values made JSON");
                return "null";
            default:
                fix("word put in quotes", "words put in quotes");
                return JsonTextWriter.Quoted(word);
        }
    }

    // A string as JSON writes it: from single quotes, " inside it escaped and \' unescaped
    private static int CopyString(string text, int i, StringBuilder output)
    {
        char quote = text[i];
        output.Append('"');
        i++;

        while (i < text.Length && text[i] != quote && text[i] != '\n')
        {
            char c = text[i];

            if (c == '\\' && i + 1 < text.Length)
            {
                char escaped = text[i + 1];
                output.Append(escaped == '\'' ? "'" : $"\\{escaped}");
                i += 2;

                continue;
            }

            output.Append(c == '"' ? "\\\"" : c.ToString());
            i++;
        }

        output.Append('"');

        // Past the closing quote; an unclosed string ends at its line's end
        return i < text.Length && text[i] == quote ? i + 1 : i;
    }

    private static int CopyNumber(string text, int i, StringBuilder output, Action<string, string> fix)
    {
        if (text[i] == '+')
        {
            fix("number written as JSON", "numbers written as JSON");
            i++;
        }
        else if (text[i] == '-')
        {
            output.Append('-');
            i++;
        }

        if (i < text.Length && text[i] == '.')
        {
            fix("number written as JSON", "numbers written as JSON");
            output.Append('0');
        }

        while (i < text.Length && (char.IsDigit(text[i]) || text[i] is '.' or 'e' or 'E' or '+' or '-'))
        {
            output.Append(text[i]);
            i++;
        }

        return i;
    }

    // Comments go; a line comment's line break stays
    private static int SkipComment(string text, int i, StringBuilder output)
    {
        if (text[i + 1] == '/')
        {
            int end = text.IndexOf('\n', i);

            return end < 0 ? text.Length : end;
        }

        int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
        int stop = close < 0 ? text.Length : close + 2;

        // The lines a block comment took stay, so the lines after it don't move
        output.Append('\n', text.AsSpan(i, stop - i).Count('\n'));

        return stop;
    }

    private static char NextSignificant(string text, int i)
    {
        while (i < text.Length && char.IsWhiteSpace(text[i]))
        {
            i++;
        }

        return i < text.Length ? text[i] : '\0';
    }
}
