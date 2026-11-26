namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>What a piece of JSON text is, for its color.</summary>
public enum JsonTokenKind
{
    /// <summary>{ } [ ] : ,</summary>
    Punctuation,

    /// <summary>A string before a colon: a member's name.</summary>
    Key,

    String,
    Number,

    /// <summary>true, false or null.</summary>
    Literal,

    /// <summary>// or /* */, which JSON with comments allows.</summary>
    Comment,

    /// <summary>What JSON doesn't allow there, as a word without quotes or a string not closed.</summary>
    Invalid,
}
