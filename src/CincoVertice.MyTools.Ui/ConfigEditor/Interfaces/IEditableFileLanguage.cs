using CincoVertice.Common.Utils.Highlighting;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;

/// <summary>What each kind of file's text means. Neither method changes anything or runs the file.</summary>
public interface IEditableFileLanguage
{
    IReadOnlyList<CodeSpan> Highlight(EditableFileKind kind, string text);

    IReadOnlyList<EditableFileProblemModel> Check(EditableFileKind kind, string text);
}
