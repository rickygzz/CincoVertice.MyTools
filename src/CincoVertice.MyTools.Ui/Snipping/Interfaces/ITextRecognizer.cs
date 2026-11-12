using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Interfaces;

/// <summary>Reads the text in an image (OCR).</summary>
public interface ITextRecognizer
{
    /// <param name="image">An encoded image, such as a PNG.</param>
    /// <returns>
    ///     The lines of text found, top to bottom, as read; empty when there are none. <see cref="Services.RecognizedText.Format"/>
    ///     makes them text to copy.
    /// </returns>
    /// <exception cref="Services.TextRecognitionException">When no text can be read at all, e.g. for want of a language.</exception>
    Task<IReadOnlyList<RecognizedLineModel>> ReadLinesAsync(byte[] image);
}
