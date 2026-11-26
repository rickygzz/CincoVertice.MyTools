using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     A value as YAML, in block style, indented by two spaces. A string is plain when YAML reads it back as the same
///     string, and in double quotes otherwise: a number, true, a date, or one with : or # in it.
/// </summary>
public static partial class JsonYamlWriter
{
    // Words YAML 1.1 readers take for true, false or null
    private static readonly HashSet<string> _reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "false", "null", "yes", "no", "on", "off", "y", "n", "~",
    };

    public static string Write(JsonNodeModel node)
    {
        var yaml = new StringBuilder();

        if (node.Kind is JsonNodeKind.Object or JsonNodeKind.Array && Count(node) > 0)
        {
            WriteBlock(yaml, node, 0);
        }
        else
        {
            yaml.Append(Scalar(node)).Append('\n');
        }

        return yaml.ToString();
    }

    private static void WriteBlock(StringBuilder yaml, JsonNodeModel node, int depth)
    {
        string indent = new(' ', depth * 2);

        if (node.Kind == JsonNodeKind.Object)
        {
            foreach ((string key, JsonNodeModel value) in node.Members)
            {
                yaml.Append(indent).Append(Text(key)).Append(':');
                WriteValue(yaml, value, depth);
            }

            return;
        }

        foreach (JsonNodeModel item in node.Items)
        {
            yaml.Append(indent).Append('-');

            // An object in a list starts on the dash's line, its other keys under its first
            if (item.Kind == JsonNodeKind.Object && item.Members.Count > 0)
            {
                var inner = new StringBuilder();
                WriteBlock(inner, item, depth + 1);
                yaml.Append(' ').Append(inner.ToString()[((depth + 1) * 2)..]);
            }
            else
            {
                WriteValue(yaml, item, depth);
            }
        }
    }

    private static void WriteValue(StringBuilder yaml, JsonNodeModel value, int depth)
    {
        if (value.Kind is JsonNodeKind.Object or JsonNodeKind.Array && Count(value) > 0)
        {
            yaml.Append('\n');
            WriteBlock(yaml, value, depth + 1);
        }
        else
        {
            yaml.Append(' ').Append(Scalar(value)).Append('\n');
        }
    }

    private static int Count(JsonNodeModel node) =>
        node.Kind == JsonNodeKind.Object ? node.Members.Count : node.Items.Count;

    private static string Scalar(JsonNodeModel node) => node.Kind switch
    {
        JsonNodeKind.Object => "{}",
        JsonNodeKind.Array => "[]",
        JsonNodeKind.String => Text(node.Text),
        _ => JsonTextWriter.Write(node, indented: false),
    };

    // Plain when that reads back as this string; JSON's double quotes are YAML's too
    private static string Text(string text) =>
        PlainRegex().IsMatch(text) && !_reserved.Contains(text)
        && !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
        && !DateRegex().IsMatch(text)
            ? text
            : JsonTextWriter.Quoted(text);

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_ .\-/()]*$")]
    private static partial Regex PlainRegex();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}")]
    private static partial Regex DateRegex();
}
