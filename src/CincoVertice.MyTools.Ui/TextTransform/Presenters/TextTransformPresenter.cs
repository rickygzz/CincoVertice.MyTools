using CincoVertice.MyTools.Ui.TextTransform.Interfaces;
using CincoVertice.MyTools.Ui.TextTransform.Models;
using CincoVertice.MyTools.Ui.TextTransform.Services;

namespace CincoVertice.MyTools.Ui.TextTransform.Presenters;

/// <summary>
///     Transforms the text selected in any program: copies it, offers the transforms at the mouse pointer, and pastes
///     the one chosen over the selection. Nothing changes when nothing is selected, the menu is closed, or the text
///     isn't what the transform reads; the last two leave the selection as it was, and the user is told why.
/// </summary>
public sealed class TextTransformPresenter
{
    /// <summary>What the user is told when there's nothing to transform.</summary>
    public const string NothingSelected = "Select some text first, then press the hotkey again.";

    private readonly ITextTransformHost _host;

    public TextTransformPresenter(ITextTransformHost host)
    {
        _host = host;
    }

    public async Task RunAsync()
    {
        string? text = await _host.CopySelectionAsync();

        if (string.IsNullOrEmpty(text))
        {
            _host.ShowProblem(NothingSelected);

            return;
        }

        if (await _host.AskTransformAsync(TextTransformer.All) is not TextTransformKind kind)
        {
            return;
        }

        string result;

        try
        {
            result = TextTransformer.Apply(kind, text);
        }
        catch (FormatException ex)
        {
            _host.ShowProblem(ex.Message);

            return;
        }

        // Pasting the same text would only move the caret
        if (result != text)
        {
            await _host.PasteAsync(result);
        }
    }
}
