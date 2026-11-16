namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>What asked for suggestions, which decides whether a word not started yet gets them.</summary>
public enum SuggestionTrigger
{
    /// <summary>A word, a "." or an "=" typed: only what was started, or the next part, is offered.</summary>
    Typing,

    /// <summary>Enter started a line: config files offer their names, none selected, so Enter again goes on.</summary>
    NewLine,

    /// <summary>Ctrl+Space: everything that fits there.</summary>
    Asked,
}
