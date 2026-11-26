namespace CincoVertice.MyTools.Ui.JsonEditor.JmesPath;

/// <summary>A JMESPath expression that can't be read or run, and where in it, from 0.</summary>
public sealed class JmesPathException(string message, int position) : Exception(message)
{
    public int Position { get; } = position;
}
