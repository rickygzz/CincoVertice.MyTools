using CincoVertice.MyTools.Ui.RegexTester.Models;

namespace CincoVertice.MyTools.Ui.RegexTester.Services;

/// <summary>The parts the regex tester's Add menu offers, in its order: first the menu's own, then each submenu's.</summary>
public static class RegexPieces
{
    public const string Characters = "Characters";
    public const string HowMany = "How many times";
    public const string Groups = "Groups and choices";
    public const string Around = "Before or after";
    public const string Edges = "Edges";

    public static IReadOnlyList<RegexPieceModel> All { get; } =
    [
        new("", "Starts with…", "The text, or a line, starts with this", "^…", "text"),
        new("", "Ends with…", "The text, or a line, ends with this", "…$", "text"),
        new("", "Is exactly…", "The whole text, or a whole line, is this", "^…$", "text"),
        new("", "Line containing…", "Each whole line that has this in it", "^.*….*$", "text", IsPerLine: true),
        new(
            "",
            "Line not containing…",
            "Each whole line that doesn't have this in it",
            "^(?!.*…).*$",
            "text",
            IsPerLine: true),
        new("", "Whole word…", "This, but not inside a longer word", @"\b…\b", "word"),
        new(
            "",
            "Text selected in the sample",
            "The text selected on the left, as it is: its special characters escaped",
            "",
            IsSampleText: true),

        new(Characters, "A digit", "0 to 9", @"\d"),
        new(Characters, "A letter", "Any letter, accented ones too", @"\p{L}"),
        new(Characters, "A letter, digit or _", "A character of a word", @"\w"),
        new(Characters, "A space", "A space, a tab or a new line", @"\s"),
        new(Characters, "Any character", "Any one character but a new line", "."),
        new(Characters, "Not a digit", "Any character but 0 to 9", @"\D"),
        new(Characters, "Not a space", "Any character but a space, a tab or a new line", @"\S"),
        new(Characters, "One of…", "One of these characters; a-z is a range", "[…]", "abc"),
        new(Characters, "None of…", "Any character but these; a-z is a range", "[^…]", "abc"),

        new(HowMany, "One or more", "What's before it, once or more", "+", IsQuantifier: true),
        new(HowMany, "Zero or more", "What's before it, any number of times, or not at all", "*", IsQuantifier: true),
        new(HowMany, "Optional", "What's before it, once or not at all", "?", IsQuantifier: true),
        new(HowMany, "Exactly n times", "What's before it, this number of times", "{3}", Pick: "3", IsQuantifier: true),
        new(
            HowMany,
            "Between n and m times",
            "What's before it, at least the first number of times and at most the second",
            "{2,5}",
            Pick: "2,5",
            IsQuantifier: true),
        new(
            HowMany,
            "At least n times",
            "What's before it, this number of times or more",
            "{2,}",
            Pick: "2",
            IsQuantifier: true),

        new(Groups, "Group", "Keeps this as a group of its own: $1 in the replacement", "(…)", "text"),
        new(
            Groups,
            "Named group",
            "Keeps this as a group with a name: ${name} in the replacement",
            "(?<name>…)",
            "text",
            Pick: "name"),
        new(Groups, "Either…or", "This or the other", "(?:…|other)", "this", Pick: "other"),

        new(Around, "Followed by…", "Only where this comes after, without matching it", "(?=…)", "text"),
        new(Around, "Not followed by…", "Only where this doesn't come after", "(?!…)", "text"),
        new(Around, "Preceded by…", "Only where this comes before, without matching it", "(?<=…)", "text"),
        new(Around, "Not preceded by…", "Only where this doesn't come before", "(?<!…)", "text"),

        new(Edges, "Start of the text", "The start of the text, or of each line with ^ and $ at each line", "^"),
        new(Edges, "End of the text", "The end of the text, or of each line with ^ and $ at each line", "$"),
        new(Edges, "Edge of a word", "Between a character of a word and one that isn't", @"\b"),
    ];
}
