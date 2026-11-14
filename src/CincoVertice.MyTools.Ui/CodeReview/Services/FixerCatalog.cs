namespace CincoVertice.MyTools.Ui.CodeReview.Services;

/// <summary>
///     Each fixer's name in words and what it does, for the menu where they're turned on and off. A test checks that
///     every fixer has one, so a new fixer comes with its words.
/// </summary>
public static class FixerCatalog
{
    private static readonly Dictionary<string, (string Title, string Description)> _fixers = new()
    {
        ["BlankLine"] = ("Blank lines", "Leaves at most one blank line in a row, and none before a }."),
        ["BraceIndentation"] = (
            "Indentation by brackets",
            "Indents every line by the brackets around it, 4 spaces per level."),
        ["BracePlacement"] = (
            "Braces on their own lines",
            "Puts block braces on lines of their own: \"if (x) {\" becomes \"if (x)\" and \"{\"."),
        ["BraceSpace"] = ("Spaces inside braces", "get{return x;} becomes get { return x; }."),
        ["CommentSpace"] = ("Space after //", "Puts a space after // and ///: //comment becomes // comment."),
        ["EmptyXmlComment"] = (
            "Empty XML comments",
            "Removes empty tags such as <returns></returns>, and /// blocks with no text."),
        ["FileScopedNamespace"] = (
            "File-scoped namespace",
            "Turns namespace X { ... } into namespace X; and moves its body one level out."),
        ["ForbiddenStartToken"] = (
            "Separators at line starts",
            "Moves a , ; ) = or => at the start of a line to the end of the line before."),
        ["Indentation"] = (
            "Indentation",
            "Indents with 4 spaces per level, at most one level deeper than the line before."),
        ["InvisibleCharacter"] = (
            "Invisible characters",
            "Replaces Unicode spaces with a normal space and removes invisible characters."),
        ["KeywordParenthesisSpace"] = (
            "Space after keywords",
            "One space between a keyword and its parenthesis: if(x) becomes if (x)."),
        ["MissingBraces"] = ("Missing braces", "Adds braces to the body of if, else, for, foreach and while."),
        ["MultipleSpace"] = (
            "Double spaces",
            "Replaces two or more spaces in code with one; indentation and comments are kept."),
        ["ParenthesisSpace"] = ("Spaces inside parentheses", "Read( a ) becomes Read(a)."),
        ["TrailingNewLine"] = ("New line at the end", "Ends the file with exactly one new line."),
        ["TrailingOperator"] = (
            "Operators at line ends",
            "Moves an operator at the end of a line, such as + or &&, to the start of the next line."),
        ["TrailingWhitespace"] = ("Trailing whitespace", "Removes spaces and tabs at the end of every line."),
        ["TypographicCharacter"] = (
            "Typographic characters",
            "Replaces curly quotes, dashes, the minus sign and the ellipsis with ASCII, in code and comments only."),
    };

    /// <summary>Whether the catalog has words for the fixer.</summary>
    public static bool Knows(string name) => _fixers.ContainsKey(name);

    /// <returns>Its name in words; for a fixer the catalog doesn't know, its name split into words.</returns>
    public static string TitleOf(string name) =>
        _fixers.TryGetValue(name, out var fixer) ? fixer.Title : Words(name);

    /// <returns>What it does; empty for a fixer the catalog doesn't know.</returns>
    public static string DescriptionOf(string name) =>
        _fixers.TryGetValue(name, out var fixer) ? fixer.Description : string.Empty;

    // "BracePlacement" is "Brace placement"
    private static string Words(string name) =>
        string.Concat(name.Select((character, i) =>
            i > 0 && char.IsUpper(character) ? " " + char.ToLowerInvariant(character) : character.ToString()));
}
