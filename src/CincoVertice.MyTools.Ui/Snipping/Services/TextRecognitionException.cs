namespace CincoVertice.MyTools.Ui.Snipping.Services;

/// <summary>Text couldn't be read from an image at all; the message says why, for the user.</summary>
public sealed class TextRecognitionException : Exception
{
    public TextRecognitionException(string message)
        : base(message)
    {
    }

    public TextRecognitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
