using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.JmesPath;

/// <param name="Kind">What it is.</param>
/// <param name="Position">Where it starts in the expression, from 0.</param>
/// <param name="Text">A name's text, unquoted.</param>
/// <param name="Number">A number's value.</param>
/// <param name="Value">A literal's value: `json` or 'raw string'.</param>
public readonly record struct JmesToken(
    JmesTokenKind Kind,
    int Position,
    string Text = "",
    int Number = 0,
    JsonNodeModel? Value = null);
