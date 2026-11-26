using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     JSON and CSV, both ways. An array of objects is a table: a column per name, in the order first met; a value
///     that's an object or an array goes in its cell as JSON. CSV is read back as an array of objects, the first row
///     their names; numbers, true, false and null become those values, the rest strings.
/// </summary>
public static partial class JsonCsv
{
    private const string ValueColumn = "value";

    /// <exception cref="FormatException">It isn't an array.</exception>
    public static string Write(JsonNodeModel node)
    {
        if (node.Kind != JsonNodeKind.Array)
        {
            throw new FormatException("Only an array can be a table: select or search for one.");
        }

        bool isTable = node.Items.Count > 0 && node.Items.All(item => item.Kind == JsonNodeKind.Object);
        List<string> columns = isTable
            ? [.. node.Items.SelectMany(item => item.Members.Select(member => member.Key)).Distinct()]
            : [ValueColumn];
        var csv = new StringBuilder();
        csv.Append(string.Join(',', columns.Select(Cell))).Append("\r\n");

        foreach (JsonNodeModel item in node.Items)
        {
            IEnumerable<string> cells = isTable
                ? columns.Select(column => item.Member(column) is JsonNodeModel value ? CellOf(value) : string.Empty)
                : [CellOf(item)];
            csv.Append(string.Join(',', cells)).Append("\r\n");
        }

        return csv.ToString();
    }

    /// <summary>Reads CSV separated by commas, semicolons (as Excel writes it in some countries) or tabs.</summary>
    /// <exception cref="FormatException">It has no rows, or a quote that never closes.</exception>
    public static JsonNodeModel Read(string text)
    {
        List<List<string>> rows = Rows(text, SeparatorOf(text));

        if (rows.Count == 0)
        {
            throw new FormatException("There's no CSV: the first row should have the names of the columns.");
        }

        List<string> names = rows[0];
        var array = new JsonNodeModel(JsonNodeKind.Array);

        foreach (List<string> row in rows.Skip(1))
        {
            var item = new JsonNodeModel(JsonNodeKind.Object);

            for (int i = 0; i < names.Count; i++)
            {
                item.Members.Add(KeyValuePair.Create(names[i], ValueOf(i < row.Count ? row[i] : string.Empty)));
            }

            array.Items.Add(item);
        }

        return array;
    }

    private static string CellOf(JsonNodeModel value) => value.Kind switch
    {
        JsonNodeKind.String => Cell(value.Text),
        JsonNodeKind.Null => string.Empty,
        _ => Cell(JsonTextWriter.Write(value, indented: false)),
    };

    private static string Cell(string text) =>
        text.IndexOfAny([',', '"', '\n', '\r']) < 0 && text.Trim().Length == text.Length
            ? text
            : $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static JsonNodeModel ValueOf(string cell) => cell switch
    {
        "true" => JsonNodeModel.True,
        "false" => JsonNodeModel.False,
        "null" => JsonNodeModel.Null,
        _ when NumberRegex().IsMatch(cell) => new JsonNodeModel(JsonNodeKind.Number)
        {
            Text = cell,
            Number = double.Parse(cell, CultureInfo.InvariantCulture),
        },
        _ => JsonNodeModel.String(cell),
    };

    // The one the first line has most of, outside quotes
    private static char SeparatorOf(string text)
    {
        int end = text.IndexOf('\n');
        string first = end < 0 ? text : text[..end];

        return new[] { ',', ';', '\t' }.MaxBy(separator => first.Count(c => c == separator));
    }

    private static List<List<string>> Rows(string text, char separator)
    {
        List<List<string>> rows = [];
        List<string> row = [];
        var cell = new StringBuilder();
        bool quoted = false;
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];

            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i += 2;

                    continue;
                }

                if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(c);
                }
            }
            else if (c == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (c == separator)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString());
                cell.Clear();
                AddRow(rows, row);
                row = [];
            }
            else
            {
                cell.Append(c);
            }

            i++;
        }

        if (quoted)
        {
            throw new FormatException("A quote in the CSV never closes.");
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            AddRow(rows, row);
        }

        return rows;
    }

    // Empty lines aren't rows
    private static void AddRow(List<List<string>> rows, List<string> row)
    {
        if (row.Count > 1 || row[0].Length > 0)
        {
            rows.Add(row);
        }
    }

    [GeneratedRegex(@"^-?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?$")]
    private static partial Regex NumberRegex();
}
