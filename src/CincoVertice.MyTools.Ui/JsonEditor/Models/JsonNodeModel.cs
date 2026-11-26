using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>
///     A JSON value: one read from the text, which knows where it is in it and its place in the document, or one a
///     JMESPath expression made, which doesn't.
/// </summary>
public sealed class JsonNodeModel
{
    public static JsonNodeModel Null { get; } = new(JsonNodeKind.Null);

    public static JsonNodeModel True { get; } = new(JsonNodeKind.True);

    public static JsonNodeModel False { get; } = new(JsonNodeKind.False);

    public JsonNodeModel(JsonNodeKind kind)
    {
        Kind = kind;
    }

    public JsonNodeKind Kind { get; }

    /// <summary>A string's text, without its quotes and escapes; a number's as written.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>A number's value.</summary>
    public double Number { get; init; }

    /// <summary>An object's members, in their order.</summary>
    public List<KeyValuePair<string, JsonNodeModel>> Members { get; } = [];

    /// <summary>An array's items.</summary>
    public List<JsonNodeModel> Items { get; } = [];

    /// <summary>The object or array it's in, in the text; null for the document's root and for a value made.</summary>
    public JsonNodeModel? Parent { get; set; }

    /// <summary>Its name in the object it's in; null in an array.</summary>
    public string? Key { get; set; }

    /// <summary>Its place in the array it's in, from 0; null in an object.</summary>
    public int? Index { get; set; }

    /// <summary>Where it starts in the text; null for a value made.</summary>
    public DiffPosition? Start { get; set; }

    /// <summary>Where it ends in the text, just past its last character.</summary>
    public DiffPosition? End { get; set; }

    /// <summary>Where its name starts, in an object: a right click there is on it too.</summary>
    public DiffPosition? KeyStart { get; set; }

    public bool IsInText => Start is not null && End is not null;

    public static JsonNodeModel String(string text) => new(JsonNodeKind.String) { Text = text };

    public static JsonNodeModel FromNumber(double number) => new(JsonNodeKind.Number)
    {
        Number = number,
        Text = number.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
    };

    public static JsonNodeModel FromBool(bool value) => value ? True : False;

    /// <summary>A new array of these values, which keep their places in the text.</summary>
    public static JsonNodeModel ArrayOf(IEnumerable<JsonNodeModel> items)
    {
        var array = new JsonNodeModel(JsonNodeKind.Array);
        array.Items.AddRange(items);

        return array;
    }

    /// <returns>The member's value; null when there's no such member, or it isn't an object.</returns>
    public JsonNodeModel? Member(string key)
    {
        foreach (KeyValuePair<string, JsonNodeModel> member in Members)
        {
            if (member.Key == key)
            {
                return member.Value;
            }
        }

        return null;
    }
}
