using System.Globalization;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using CincoVertice.Common.Application.Calculator.Functions;
using CincoVertice.MyTools.Ui.TextTransform.Models;

namespace CincoVertice.MyTools.Ui.TextTransform.Services;

/// <summary>
///     What the text transforms do to a text. Line endings stay as the text has them (\r\n, or \n), and so does a
///     line break at its end; a text of one line gets \r\n where a transform makes lines, as JSON pretty-printed.
/// </summary>
public static class TextTransformer
{
    /// <summary>The transforms, in the menu's order.</summary>
    public static IReadOnlyList<TextTransformModel> All { get; } =
    [
        new(TextTransformKind.Upper, "&UPPER CASE", 0),
        new(TextTransformKind.Lower, "&lower case", 0),
        new(TextTransformKind.Title, "&Title Case", 0),
        new(TextTransformKind.TrimLines, "T&rim lines", 1),
        new(TextTransformKind.SortLines, "&Sort lines", 1),
        new(TextTransformKind.RemoveDuplicateLines, "Remove &duplicate lines", 1),
        new(TextTransformKind.Base64Encode, "&Base64 encode", 2),
        new(TextTransformKind.Base64Decode, "B&ase64 decode", 2),
        new(TextTransformKind.UrlEncode, "URL &encode", 2),
        new(TextTransformKind.UrlDecode, "URL de&code", 2),
        new(TextTransformKind.JsonPrettyPrint, "&JSON pretty-print", 3),
        new(TextTransformKind.JsonMinify, "JSON &minify", 3),
    ];

    private static readonly JsonDocumentOptions _jsonReading = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    // Accents and other letters as themselves, not as \u escapes
    private static readonly JsonSerializerOptions _pretty = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions _minified = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <exception cref="FormatException">
    ///     The text isn't what the transform reads, such as base64 or JSON; the message says what's wrong, for the user.
    /// </exception>
    public static string Apply(TextTransformKind kind, string text)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;

        return kind switch
        {
            TextTransformKind.Upper => text.ToUpper(culture),
            TextTransformKind.Lower => text.ToLower(culture),

            // Lowercase first: a word in capitals would stay so, taken for an acronym
            TextTransformKind.Title => culture.TextInfo.ToTitleCase(text.ToLower(culture)),
            TextTransformKind.TrimLines => OnLines(text, TrimLines),
            TextTransformKind.SortLines => OnLines(text, lines => [.. lines
                .OrderBy(line => line, StringComparer.Create(culture, ignoreCase: true))
                .ThenBy(line => line, StringComparer.Ordinal)]),
            TextTransformKind.RemoveDuplicateLines => OnLines(text, lines => [.. lines.Distinct(StringComparer.Ordinal)]),
            TextTransformKind.Base64Encode => Encodings.ToBase64(text),
            TextTransformKind.Base64Decode => Read(() => Encodings.TextFromBase64(text)),
            TextTransformKind.UrlEncode => Uri.EscapeDataString(text),
            TextTransformKind.UrlDecode => WebUtility.UrlDecode(text),
            TextTransformKind.JsonPrettyPrint => Json(text, _pretty),
            TextTransformKind.JsonMinify => Json(text, _minified),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    // Each line without spaces at its ends, and no blank lines before the first or after the last
    private static List<string> TrimLines(IReadOnlyList<string> lines)
    {
        List<string> trimmed = [.. lines.Select(line => line.Trim())];
        int first = trimmed.FindIndex(line => line.Length > 0);
        int last = trimmed.FindLastIndex(line => line.Length > 0);

        return first < 0 ? [] : trimmed[first..(last + 1)];
    }

    private static string OnLines(string text, Func<IReadOnlyList<string>, List<string>> change)
    {
        string newLine = NewLineOf(text);
        bool endsWithNewLine = text.EndsWith('\n') || text.EndsWith('\r');
        List<string> lines = [.. text.ReplaceLineEndings("\n").Split('\n')];

        if (endsWithNewLine)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        string joined = string.Join(newLine, change(lines));

        return endsWithNewLine && joined.Length > 0 ? joined + newLine : joined;
    }

    private static string Json(string text, JsonSerializerOptions options)
    {
        JsonNode? node;

        try
        {
            node = JsonNode.Parse(text, documentOptions: _jsonReading);
        }
        catch (JsonException ex)
        {
            string where = ex.LineNumber is long line
                ? $" at line {line + 1}, character {(ex.BytePositionInLine ?? 0) + 1}"
                : string.Empty;

            throw new FormatException($"That isn't JSON{where}.");
        }

        string json = node?.ToJsonString(options) ?? "null";
        string newLine = NewLineOf(text);
        json = json.ReplaceLineEndings(newLine);

        return text.TrimEnd(' ', '\t').EndsWith('\n') ? json + newLine : json;
    }

    private static string Read(Func<string> decode)
    {
        try
        {
            return decode();
        }
        catch (ArithmeticException ex)
        {
            throw new FormatException(ex.Message + ".");
        }
    }

    // \n when the text uses it alone, else Windows' \r\n
    private static string NewLineOf(string text) =>
        text.Contains('\n') && !text.Contains("\r\n", StringComparison.Ordinal) ? "\n" : "\r\n";
}
