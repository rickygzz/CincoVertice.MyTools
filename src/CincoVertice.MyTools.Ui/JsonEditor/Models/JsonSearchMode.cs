namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>What the search box's text is.</summary>
public enum JsonSearchMode
{
    /// <summary>Text to find, ignoring case.</summary>
    Text,

    /// <summary>A JMESPath expression: the values it gives are found.</summary>
    JmesPath,
}
