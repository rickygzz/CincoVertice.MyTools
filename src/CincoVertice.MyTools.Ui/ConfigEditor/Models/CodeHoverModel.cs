namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>The pointer rests on a character of the code: the presenter fills in what to show about it.</summary>
public sealed class CodeHoverModel(int position)
{
    /// <summary>The character's index in the code.</summary>
    public int Position { get; } = position;

    /// <summary>What to show; null for nothing.</summary>
    public string? Text { get; set; }
}
