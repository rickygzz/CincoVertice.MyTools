namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>A function scripts can call, for the editor's suggestions.</summary>
/// <param name="Name">As written in scripts: "clipboard.gettext", or "number" for a global one.</param>
/// <param name="Signature">How it's called, such as "dialog.ask(prompt, default)".</param>
/// <param name="Description">What it does, in a sentence.</param>
/// <param name="HasArguments">Whether it takes any, so the caret goes between its parentheses.</param>
public sealed record ScriptFunctionModel(string Name, string Signature, string Description, bool HasArguments)
{
    public string Module => Name.Contains('.') ? Name[..Name.IndexOf('.')] : string.Empty;

    public string ShortName => Name[(Name.IndexOf('.') + 1)..];
}
