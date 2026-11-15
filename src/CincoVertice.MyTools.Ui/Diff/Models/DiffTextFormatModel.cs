using System.Text;

namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>
///     How a text was written, so saving it writes it the same way: its encoding (with its byte order mark or not),
///     its line break (the first one it has) and whether its last line ends with one.
/// </summary>
public sealed record DiffTextFormatModel(Encoding Encoding, string LineBreak, bool EndsWithLineBreak)
{
    /// <summary>For text pasted or typed: UTF-8 without a byte order mark, Windows line breaks.</summary>
    public static DiffTextFormatModel Default { get; } = new(new UTF8Encoding(false), "\r\n", false);

    public static DiffTextFormatModel Of(string text, Encoding encoding)
    {
        int index = text.IndexOfAny(['\r', '\n']);
        string lineBreak = index < 0 ? "\r\n"
            : text[index] == '\n' ? "\n"
            : index + 1 < text.Length && text[index + 1] == '\n' ? "\r\n"
            : "\r";
        bool endsWithLineBreak = text.Length > 0 && text[^1] is '\r' or '\n';

        return new DiffTextFormatModel(encoding, lineBreak, endsWithLineBreak);
    }

    /// <summary>The editable lines as one text; a final empty line represents a trailing line break.</summary>
    public string Join(IReadOnlyList<string> lines)
    {
        string text = string.Join(LineBreak, lines);

        return text;
    }
}
