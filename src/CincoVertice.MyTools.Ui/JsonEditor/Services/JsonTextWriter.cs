using System.Text;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>A value as JSON text: laid out on lines, indented by two spaces, or on one line.</summary>
public static class JsonTextWriter
{
    public static string Write(JsonNodeModel node, bool indented)
    {
        var text = new StringBuilder();
        Write(text, node, indented, 0);

        return text.ToString();
    }

    private static void Write(StringBuilder text, JsonNodeModel node, bool indented, int depth)
    {
        switch (node.Kind)
        {
            case JsonNodeKind.Object:
                WriteAll(
                    text,
                    '{',
                    '}',
                    node.Members,
                    (member, d) =>
                    {
                        text.Append(Quoted(member.Key)).Append(indented ? ": " : ":");
                        Write(text, member.Value, indented, d);
                    },
                    indented,
                    depth);
                break;
            case JsonNodeKind.Array:
                WriteAll(text, '[', ']', node.Items, (item, d) => Write(text, item, indented, d), indented, depth);
                break;
            case JsonNodeKind.String:
                text.Append(Quoted(node.Text));
                break;
            case JsonNodeKind.Number:
                text.Append(node.Text);
                break;
            case JsonNodeKind.True:
                text.Append("true");
                break;
            case JsonNodeKind.False:
                text.Append("false");
                break;
            default:
                text.Append("null");
                break;
        }
    }

    private static void WriteAll<T>(
        StringBuilder text,
        char open,
        char close,
        IReadOnlyList<T> parts,
        Action<T, int> write,
        bool indented,
        int depth)
    {
        text.Append(open);

        if (parts.Count == 0)
        {
            text.Append(close);

            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                text.Append(',');
            }

            if (indented)
            {
                text.Append('\n').Append(' ', (depth + 1) * 2);
            }

            write(parts[i], depth + 1);
        }

        if (indented)
        {
            text.Append('\n').Append(' ', depth * 2);
        }

        text.Append(close);
    }

    /// <returns>The text as a JSON string, in quotes, with what must be escaped escaped.</returns>
    public static string Quoted(string value)
    {
        var quoted = new StringBuilder(value.Length + 2).Append('"');

        foreach (char c in value)
        {
            quoted.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\b' => "\\b",
                '\f' => "\\f",
                < ' ' => $"\\u{(int)c:x4}",
                _ => c.ToString(),
            });
        }

        return quoted.Append('"').ToString();
    }
}
