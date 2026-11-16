namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>Something the editor offers to write at the caret.</summary>
/// <param name="Text">What the list shows, and what typing narrows it by.</param>
/// <param name="Detail">What it is or does, shown beside it.</param>
/// <param name="Insert">What choosing it writes, in place of what was typed of it.</param>
public sealed record CodeSuggestionModel(string Text, string Detail, string Insert)
{
    public CodeSuggestionModel(string text, string detail)
        : this(text, detail, text)
    {
    }
}
