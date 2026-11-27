using CincoVertice.MyTools.Ui.RegexTester.Models;

namespace CincoVertice.MyTools.Ui.RegexTester.Services;

/// <summary>The regex tester's quick reference, in its order: the basics, then more.</summary>
public static class RegexReference
{
    public const string Basics = "Basics";
    public const string More = "More";

    public static IReadOnlyList<RegexReferenceModel> All { get; } =
    [
        new(Basics, ".", "Any character except newline"),
        new(Basics, @"\d", "Digit (0-9)"),
        new(Basics, @"\w", "Word character (a-z, A-Z, 0-9, _)"),
        new(Basics, @"\s", "Whitespace"),
        new(Basics, "[abc]", "Character class"),
        new(Basics, "^ / $", "Start / end (of each line, when ticked)"),
        new(Basics, "* / + / ?", "0+, 1+, 0 or 1"),
        new(Basics, "{n,m}", "Between n and m times"),
        new(Basics, "(group)", "Capture group"),
        new(Basics, "(?:group)", "Non-capturing group"),
        new(Basics, "a|b", "Alternation (a or b)"),

        new(More, @"\D / \W / \S", "Not a digit / word character / whitespace"),
        new(More, "[^abc]", "Any character but these"),
        new(More, "[a-z]", "A range of characters"),
        new(More, @"\b", "Word boundary"),
        new(More, @"\. / \\ / \(", "The character itself: a backslash before . \\ ( ) [ ] { } * + ? | ^ $"),
        new(More, @"\t / \n", "Tab / newline"),
        new(More, "{n} / {n,}", "Exactly n times / n times or more"),
        new(More, "*? / +?", "As few as possible (lazy)"),
        new(More, "(?<name>group)", "Named group"),
        new(More, @"\1 / \k<name>", "The same text as group 1 / the named group, again"),
        new(More, "(?=x) / (?!x)", "Followed by x / not followed by x"),
        new(More, "(?<=x) / (?<!x)", "Preceded by x / not preceded by x"),
        new(More, "$1 / ${name}", "In the replacement: group 1 / the named group"),
        new(More, "$0", "In the replacement: the whole match"),
    ];
}
