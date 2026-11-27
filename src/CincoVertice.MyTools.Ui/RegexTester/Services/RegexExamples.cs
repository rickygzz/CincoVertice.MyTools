using CincoVertice.MyTools.Ui.RegexTester.Models;

namespace CincoVertice.MyTools.Ui.RegexTester.Services;

/// <summary>The regex tester's Examples menu, in its order: a submenu per group.</summary>
public static class RegexExamples
{
    public const string Web = "Contacts and the web";
    public const string Numbers = "Dates, times and numbers";
    public const string Codes = "Codes";
    public const string Text = "Text and lines";

    public static IReadOnlyList<RegexExampleModel> All { get; } =
    [
        new(
            Web,
            "Email address",
            "An address as name@domain.com; the name may have dots, + and -",
            @"[\w.+-]+@[\w-]+(?:\.[\w-]+)+",
            "Write to ann.lee@example.com or bob+news@mail.co.uk.\nNot an address: @nobody"),
        new(
            Web,
            "Web address (URL)",
            "http:// or https://, then everything up to a space, but a period or comma ending a sentence",
            @"https?://\S+?(?=[.,;:!?)]*(?:\s|$))",
            "See https://example.com/docs?page=2 and http://test.org.\nNot one: ftp://files.net"),
        new(
            Web,
            "Phone number",
            "10 digits, the first 3 in parentheses or not, with spaces, dots or dashes between the parts or not",
            @"\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}",
            "Call (555) 123-4567, 555.987.6543 or 5551112222."),

        new(
            Numbers,
            "Date, year first: YYYY-MM-DD",
            "Groups year, month and day; the replacement writes it day first",
            @"\b(?<year>\d{4})-(?<month>0[1-9]|1[0-2])-(?<day>0[1-9]|[12]\d|3[01])\b",
            "Due 2026-11-26, paid 2026-12-03. Not a date: 2026-13-40",
            "${day}/${month}/${year}"),
        new(
            Numbers,
            "Date, day first: DD/MM/YYYY",
            "Groups day, month and year; the day and month may have one digit",
            @"\b(?<day>0?[1-9]|[12]\d|3[01])/(?<month>0?[1-9]|1[0-2])/(?<year>\d{4})\b",
            "Born 5/3/1990, married 21/09/2015. Not a date: 32/13/2020",
            "${year}-${month}-${day}"),
        new(
            Numbers,
            "Time: HH:MM or HH:MM:SS",
            "A time of a 24-hour clock, the seconds optional",
            @"\b(?:[01]?\d|2[0-3]):[0-5]\d(?::[0-5]\d)?\b",
            "Open 9:00 to 17:30, last call at 23:59:59. Not a time: 25:00"),
        new(
            Numbers,
            "Number",
            "A whole or decimal number, negative or not",
            @"-?\b\d+(?:\.\d+)?\b",
            "Took 12 steps, -3.5 degrees, 0.75 of it, 1000 times."),
        new(
            Numbers,
            "Amount of money",
            "$, then digits with commas between thousands, and cents or not; the replacement keeps only the digits",
            @"\$\s?(?<amount>\d{1,3}(?:,\d{3})*(?:\.\d{2})?)",
            "Paid $3,332,000.00 and $ 45.10, owes $7.",
            "${amount}"),
        new(
            Numbers,
            "Percentage",
            "A number followed by %",
            @"-?\d+(?:\.\d+)?\s?%",
            "Up 12%, down 3.5 %, rate of -0.25%."),

        new(
            Codes,
            "IP address (IPv4)",
            "Four numbers from 0 to 255, with dots between",
            @"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b",
            "Server 192.168.1.10, gateway 10.0.0.1. Not one: 300.1.2.3"),
        new(
            Codes,
            "GUID",
            "32 hexadecimal digits in groups of 8, 4, 4, 4 and 12",
            @"\b[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}\b",
            "Id 3f2504e0-4f89-11d3-9a0c-0305e82c3301 of the order."),
        new(
            Codes,
            "Color: #RGB or #RRGGBB",
            "A # then 3 or 6 hexadecimal digits",
            @"#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{3})\b",
            "color: #fff; background: #1E90FF; not one: #12345"),
        new(
            Codes,
            "HTML tag with its content",
            "An opening tag and its own closing tag: group 1 the tag's name, group 2 what's between",
            @"<(\w+)[^>]*>(.*?)</\1>",
            "<p class=\"x\">Hello <b>you</b></p> and <i>this</i>"),

        new(
            Text,
            "Repeated word",
            "A word written twice in a row; the replacement writes it once",
            @"\b(\w+)\s+\1\b",
            "This is is a test of the the repeated words.",
            "$1",
            IgnoreCase: true),
        new(
            Text,
            "Text in quotes",
            "Text between double quotes, with \\\" inside allowed: group 1 is the text",
            "\"((?:[^\"\\\\]|\\\\.)*)\"",
            "Say \"hello\" and \"a \\\"quoted\\\" word\"."),
        new(
            Text,
            "Name = value line",
            "A line as the app's config files have: groups name and value",
            @"^\s*(?<name>[\w.]+)\s*=\s*(?<value>.*?)\s*$",
            "SpeedMin = 5\nSpeedMax=20\n# a comment\nOpenJsonEditor = Ctrl+Alt+J",
            "${name}: ${value}",
            Multiline: true),
        new(
            Text,
            "Spaces at the end of lines",
            "Spaces and tabs before each line's end",
            @"[ \t]+$",
            "first line   \nsecond line\t\nthird line",
            Multiline: true),
        new(
            Text,
            "Blank lines",
            "Lines with nothing or only spaces, with their new line",
            @"^[ \t]*\n",
            "one\n\n   \ntwo\n\nthree",
            Multiline: true),
        new(
            Text,
            "A line starting with a word",
            "Each whole line starting with ERROR or WARNING: group 1 is which",
            @"^(ERROR|WARNING)\b.*$",
            "ERROR disk full\nINFO started\nWARNING low memory\nINFO stopped",
            Multiline: true),
    ];
}
