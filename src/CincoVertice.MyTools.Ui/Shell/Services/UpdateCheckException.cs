namespace CincoVertice.MyTools.Ui.Shell.Services;

/// <summary>The latest release couldn't be found out: no network, no answer, or an answer that can't be read.</summary>
public sealed class UpdateCheckException : Exception
{
    public UpdateCheckException(string message)
        : base(message)
    {
    }

    public UpdateCheckException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
