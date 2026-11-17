using CincoVertice.Common.Utils.Highlighting;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;

/// <summary>What each kind of file's text means. No method changes anything or runs the file.</summary>
public interface IEditableFileLanguage
{
    IReadOnlyList<CodeSpan> Highlight(EditableFileKind kind, string text);

    IReadOnlyList<EditableFileProblemModel> Check(EditableFileKind kind, string text);

    /// <summary>What to offer at the caret, or null for nothing.</summary>
    /// <param name="trigger">What asked: typing offers only what was started, or the next part.</param>
    CodeSuggestionsModel? Suggest(EditableFileKind kind, string text, int caret, SuggestionTrigger trigger);

    /// <summary>What to show when the pointer rests on a place of the text: what the name there is or does.</summary>
    /// <returns>The text to show, or null for nothing.</returns>
    string? Describe(EditableFileKind kind, string text, int position);
}
