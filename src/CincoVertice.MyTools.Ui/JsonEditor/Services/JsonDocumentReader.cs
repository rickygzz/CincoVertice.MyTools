using System.Globalization;
using System.Text;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     Reads JSON text as it's typed, valid or not: each line's colored pieces, the sections that collapse (from the
///     brackets, so they work even where the JSON is broken), and the values, up to the first thing wrong, which is
///     said and placed. Comments (// and /* */) are allowed, as in JSON with comments, and so is a comma before a
///     closing bracket.
/// </summary>
public static class JsonDocumentReader
{
    public static JsonDocumentModel Read(IReadOnlyList<string> lines)
    {
        List<Piece> pieces = Tokenize(lines);
        MarkKeys(pieces);

        List<List<JsonTokenModel>> tokens = [.. lines.Select(_ => new List<JsonTokenModel>())];

        foreach (Piece piece in pieces)
        {
            tokens[piece.Line].Add(new JsonTokenModel(piece.Kind, piece.Column, piece.Length));
        }

        var parser = new Parser(pieces.Where(piece => piece.Kind != JsonTokenKind.Comment).ToList(), lines);
        JsonNodeModel? root = parser.ParseDocument();

        return new JsonDocumentModel(tokens, Folds(pieces), root, parser.Error, parser.ErrorAt);
    }

    private static List<Piece> Tokenize(IReadOnlyList<string> lines)
    {
        List<Piece> pieces = [];
        bool inComment = false;

        for (int line = 0; line < lines.Count; line++)
        {
            string text = lines[line];
            int i = 0;

            while (i < text.Length)
            {
                char c = text[i];
                int start = i;

                if (inComment)
                {
                    int close = text.IndexOf("*/", i, StringComparison.Ordinal);
                    i = close < 0 ? text.Length : close + 2;
                    inComment = close < 0;
                    pieces.Add(new Piece(JsonTokenKind.Comment, line, start, i - start, text));

                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    i++;

                    continue;
                }

                JsonTokenKind kind;

                if (c is '{' or '}' or '[' or ']' or ':' or ',')
                {
                    kind = JsonTokenKind.Punctuation;
                    i++;
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    kind = JsonTokenKind.Comment;
                    i = text.Length;
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    kind = JsonTokenKind.Comment;
                    int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = close < 0 ? text.Length : close + 2;
                    inComment = close < 0;
                }
                else if (c == '"')
                {
                    i = StringEnd(text, i);
                    kind = i > start + 1 && text[i - 1] == '"' && !IsEscaped(text, i - 1, start)
                        ? JsonTokenKind.String
                        : JsonTokenKind.Invalid;
                }
                else if (c == '-' || char.IsAsciiDigit(c))
                {
                    while (i < text.Length
                        && (char.IsAsciiDigit(text[i]) || text[i] is '-' or '+' or '.' or 'e' or 'E'))
                    {
                        i++;
                    }

                    kind = IsNumber(text[start..i]) ? JsonTokenKind.Number : JsonTokenKind.Invalid;
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    {
                        i++;
                    }

                    kind = text[start..i] is "true" or "false" or "null"
                        ? JsonTokenKind.Literal
                        : JsonTokenKind.Invalid;
                }
                else
                {
                    kind = JsonTokenKind.Invalid;
                    i++;
                }

                pieces.Add(new Piece(kind, line, start, i - start, text));
            }
        }

        return pieces;
    }

    // Past the closing quote, or the line's end when there's none
    private static int StringEnd(string text, int start)
    {
        int i = start + 1;

        while (i < text.Length)
        {
            if (text[i] == '\\')
            {
                i += 2;
            }
            else if (text[i] == '"')
            {
                return i + 1;
            }
            else
            {
                i++;
            }
        }

        return text.Length;
    }

    private static bool IsEscaped(string text, int quote, int start)
    {
        int backslashes = 0;

        for (int i = quote - 1; i > start && text[i] == '\\'; i--)
        {
            backslashes++;
        }

        return backslashes % 2 == 1;
    }

    private static bool IsNumber(string text) =>
        System.Text.RegularExpressions.Regex.IsMatch(text, @"^-?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?$");

    // A string followed by a colon is a member's name
    private static void MarkKeys(List<Piece> pieces)
    {
        List<int> code = [.. Enumerable.Range(0, pieces.Count).Where(i => pieces[i].Kind != JsonTokenKind.Comment)];

        for (int i = 0; i + 1 < code.Count; i++)
        {
            Piece piece = pieces[code[i]];

            if (piece.Kind == JsonTokenKind.String && pieces[code[i + 1]].Is(':'))
            {
                pieces[code[i]] = piece with { Kind = JsonTokenKind.Key };
            }
        }
    }

    // Each { or [ whose closing bracket is on a later line, with how many keys or items it holds
    private static List<JsonFoldModel> Folds(List<Piece> pieces)
    {
        List<JsonFoldModel> folds = [];
        Stack<(Piece Open, int Items, bool HasValue)> open = new();

        foreach (Piece piece in pieces.Where(piece => piece.Kind != JsonTokenKind.Comment))
        {
            if (piece.Is('{') || piece.Is('['))
            {
                MarkValue(open);
                open.Push((piece, 0, false));
            }
            else if (piece.Is('}') || piece.Is(']'))
            {
                if (open.Count == 0)
                {
                    continue;
                }

                (Piece start, int items, bool hasValue) = open.Pop();
                int count = items + (hasValue ? 1 : 0);

                if (piece.Line > start.Line)
                {
                    string summary = start.Is('{')
                        ? $"{{ {count} {(count == 1 ? "key" : "keys")} }}"
                        : $"[ {count} {(count == 1 ? "item" : "items")} ]";
                    folds.Add(new JsonFoldModel(start.Line, piece.Line, summary));
                }
            }
            else if (piece.Is(','))
            {
                if (open.Count > 0)
                {
                    (Piece start, int items, bool hasValue) = open.Pop();
                    open.Push((start, items + (hasValue ? 1 : 0), false));
                }
            }
            else if (!piece.Is(':') && piece.Kind != JsonTokenKind.Key)
            {
                MarkValue(open);
            }
        }

        return [.. folds.OrderBy(fold => fold.StartLine).ThenByDescending(fold => fold.EndLine)];
    }

    private static void MarkValue(Stack<(Piece Open, int Items, bool HasValue)> open)
    {
        if (open.Count > 0 && !open.Peek().HasValue)
        {
            (Piece start, int items, _) = open.Pop();
            open.Push((start, items, true));
        }
    }

    private readonly record struct Piece(JsonTokenKind Kind, int Line, int Column, int Length, string LineText)
    {
        public string Text => LineText.Substring(Column, Length);

        public DiffPosition Start => new(Line, Column);

        public DiffPosition End => new(Line, Column + Length);

        public bool Is(char punctuation) =>
            Kind == JsonTokenKind.Punctuation && LineText[Column] == punctuation;
    }

    private sealed class Parser(List<Piece> pieces, IReadOnlyList<string> lines)
    {
        private int _next;

        public string? Error { get; private set; }

        public DiffPosition ErrorAt { get; private set; }

        public JsonNodeModel? ParseDocument()
        {
            if (pieces.Count == 0)
            {
                return null;
            }

            JsonNodeModel? root = ParseValue();

            if (root is not null && Error is null && _next < pieces.Count)
            {
                Fail("Expected the end of the text after the value", pieces[_next].Start);
            }

            return root;
        }

        private JsonNodeModel? ParseValue()
        {
            if (_next >= pieces.Count)
            {
                Fail("Expected a value", EndOfText);

                return null;
            }

            Piece piece = pieces[_next++];

            if (piece.Is('{'))
            {
                return ParseObject(piece);
            }

            if (piece.Is('['))
            {
                return ParseArray(piece);
            }

            JsonNodeModel? value = piece.Kind switch
            {
                JsonTokenKind.String or JsonTokenKind.Key => JsonNodeModel.String(Unescape(piece.Text)),
                JsonTokenKind.Number => new JsonNodeModel(JsonNodeKind.Number)
                {
                    Text = piece.Text,
                    Number = double.Parse(piece.Text, CultureInfo.InvariantCulture),
                },
                JsonTokenKind.Literal => new JsonNodeModel(piece.Text switch
                {
                    "true" => JsonNodeKind.True,
                    "false" => JsonNodeKind.False,
                    _ => JsonNodeKind.Null,
                }),
                _ => null,
            };

            if (value is null)
            {
                Fail(piece.Kind == JsonTokenKind.Invalid && piece.Text.StartsWith('"')
                    ? "The string isn't closed"
                    : $"Expected a value, not {Describe(piece)}", piece.Start);

                return null;
            }

            value.Start = piece.Start;
            value.End = piece.End;

            return value;
        }

        private JsonNodeModel ParseObject(Piece open)
        {
            var node = new JsonNodeModel(JsonNodeKind.Object) { Start = open.Start };

            while (Error is null)
            {
                if (_next >= pieces.Count)
                {
                    Fail($"Expected }} to close the object of line {open.Line + 1}", EndOfText);

                    break;
                }

                Piece piece = pieces[_next];

                if (piece.Is('}'))
                {
                    _next++;
                    node.End = piece.End;

                    break;
                }

                if (piece.Kind != JsonTokenKind.Key)
                {
                    bool isString = piece.Kind == JsonTokenKind.String;
                    Fail(
                        isString ? "Expected : after the name" : $"Expected a name in quotes, not {Describe(piece)}",
                        isString ? piece.End : piece.Start);

                    break;
                }

                _next += 2;
                string key = Unescape(piece.Text);
                JsonNodeModel? value = ParseValue();

                if (value is null)
                {
                    break;
                }

                value.Parent = node;
                value.Key = key;
                value.KeyStart = piece.Start;
                node.Members.Add(new KeyValuePair<string, JsonNodeModel>(key, value));

                if (!Separator('}', "object", open))
                {
                    break;
                }
            }

            return node;
        }

        private JsonNodeModel ParseArray(Piece open)
        {
            var node = new JsonNodeModel(JsonNodeKind.Array) { Start = open.Start };

            while (Error is null)
            {
                if (_next >= pieces.Count)
                {
                    Fail($"Expected ] to close the array of line {open.Line + 1}", EndOfText);

                    break;
                }

                if (pieces[_next].Is(']'))
                {
                    node.End = pieces[_next++].End;

                    break;
                }

                JsonNodeModel? value = ParseValue();

                if (value is null)
                {
                    break;
                }

                value.Parent = node;
                value.Index = node.Items.Count;
                node.Items.Add(value);

                if (!Separator(']', "array", open))
                {
                    break;
                }
            }

            return node;
        }

        // After a member or an item: a comma, or the closing bracket, left for the loop to take
        private bool Separator(char close, string what, Piece open)
        {
            if (_next >= pieces.Count)
            {
                Fail($"Expected {close} to close the {what} of line {open.Line + 1}", EndOfText);

                return false;
            }

            Piece piece = pieces[_next];

            if (piece.Is(','))
            {
                _next++;

                return true;
            }

            if (piece.Is(close))
            {
                return true;
            }

            Fail($"Expected , or {close}, not {Describe(piece)}", piece.Start);

            return false;
        }

        private DiffPosition EndOfText => new(lines.Count - 1, lines[^1].Length);

        private void Fail(string message, DiffPosition at)
        {
            if (Error is null)
            {
                Error = message;
                ErrorAt = at;
            }
        }

        private static string Describe(Piece piece) => piece.Kind == JsonTokenKind.Punctuation
            ? piece.Text
            : $"'{(piece.Length > 20 ? piece.Text[..20] + "…" : piece.Text)}'";

        private static string Unescape(string quoted)
        {
            var text = new StringBuilder(quoted.Length);

            for (int i = 1; i < quoted.Length - 1; i++)
            {
                char c = quoted[i];

                if (c != '\\' || i + 1 >= quoted.Length - 1)
                {
                    text.Append(c);

                    continue;
                }

                char escaped = quoted[++i];

                if (escaped == 'u' && i + 4 < quoted.Length && int.TryParse(
                    quoted.AsSpan(i + 1, 4),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out int code))
                {
                    text.Append((char)code);
                    i += 4;

                    continue;
                }

                text.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    'b' => '\b',
                    'f' => '\f',
                    _ => escaped,
                });
            }

            return text.ToString();
        }
    }
}
