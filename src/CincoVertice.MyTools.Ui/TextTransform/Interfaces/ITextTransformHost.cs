using CincoVertice.MyTools.Ui.TextTransform.Models;

namespace CincoVertice.MyTools.Ui.TextTransform.Interfaces;

/// <summary>What transforming the text selected in any program needs from the desktop.</summary>
public interface ITextTransformHost
{
    /// <returns>The text selected in the window in front, copied as Ctrl+C does; null when nothing is selected.</returns>
    /// <remarks>The clipboard is as it was afterwards.</remarks>
    Task<string?> CopySelectionAsync();

    /// <summary>Shows the transforms at the mouse pointer.</summary>
    /// <returns>The one chosen; null when the menu is closed without a choice.</returns>
    Task<TextTransformKind?> AskTransformAsync(IReadOnlyList<TextTransformModel> transforms);

    /// <summary>Pastes the text over the selection, in the window it was copied from; the clipboard as it was after.</summary>
    Task PasteAsync(string text);

    /// <summary>Says what went wrong, without stopping anything: a tray notification.</summary>
    void ShowProblem(string message);
}
