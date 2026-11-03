using System.Text;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Services;

/// <summary>
///     Files as UTF-8, keeping their new lines and byte order mark. A file that isn't valid UTF-8 is refused, since
///     saving it would break its other characters. A save writes a temporary file first, so a failure can't leave half
///     a file.
/// </summary>
public sealed class EditableFileStore : IEditableFileStore
{
    private static readonly byte[] _bom = [0xEF, 0xBB, 0xBF];

    public EditableFileContentModel Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        bool hasBom = bytes.AsSpan().StartsWith(_bom);
        int start = hasBom ? _bom.Length : 0;
        string text;

        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes, start, bytes.Length - start);
        }
        catch (DecoderFallbackException ex)
        {
            throw new IOException($"{Path.GetFileName(path)} is not valid UTF-8.", ex);
        }

        string newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n"
            : text.Contains('\n') ? "\n"
            : text.Contains('\r') ? "\r"
            : Environment.NewLine;

        // The text box turns a lone \r into a new line too, which would look like a change
        return new EditableFileContentModel(text.Replace("\r\n", "\n").Replace('\r', '\n'), newLine, hasBom);
    }

    public void Write(string path, EditableFileContentModel content)
    {
        AtomicFile.WriteAllText(path, content.Text.Replace("\n", content.NewLine), new UTF8Encoding(content.HasBom));
    }
}
