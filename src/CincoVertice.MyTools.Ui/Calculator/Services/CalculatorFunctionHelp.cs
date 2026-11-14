using CincoVertice.Common.Application.Calculator;
using CincoVertice.Common.Application.Calculator.Functions;

namespace CincoVertice.MyTools.Ui.Calculator.Services;

/// <summary>
///     How each of the calculator's functions and constants is written and what it gives, for the suggestions while
///     typing. Every function of Common's <see cref="BuiltIns"/> needs a line here; a test checks.
/// </summary>
public static class CalculatorFunctionHelp
{
    /// <summary>Each function: how it's written, and what it gives.</summary>
    public static readonly IReadOnlyDictionary<string, (string Usage, string Meaning)> Functions =
        new Dictionary<string, (string Usage, string Meaning)>(StringComparer.OrdinalIgnoreCase)
        {
            ["abs"] = ("abs(x)", "absolute value"),
            ["sqrt"] = ("sqrt(x)", "square root"),
            ["round"] = ("round(x, decimals)", "rounds; decimals are optional"),
            ["floor"] = ("floor(x)", "rounds down"),
            ["ceil"] = ("ceil(x)", "rounds up"),
            ["min"] = ("min(a, ...)", "smallest"),
            ["max"] = ("max(a, ...)", "largest"),
            ["sin"] = ("sin(x)", "sine, in radians"),
            ["cos"] = ("cos(x)", "cosine, in radians"),
            ["tan"] = ("tan(x)", "tangent, in radians"),
            ["ln"] = ("ln(x)", "natural log"),
            ["log"] = ("log(x)", "base-10 log"),
            ["exp"] = ("exp(x)", "e to the power x"),
            ["len"] = ("len(\"text\")", "length of a text"),
            ["find"] = ("find(\"part\", \"text\", start)", "position of the part; start is optional"),
            [BuiltIns.IpConfig] = ("ipconfig()", "IP address and gateway"),
        };

    /// <summary>The names that stand for a value: the constants, the last answer and today's date.</summary>
    public static readonly IReadOnlyList<(string Name, string Meaning)> Values =
    [
        ("pi", "3.14159..."),
        ("e", "2.71828..."),
        (ExpressionCalculator.LastAnswerName, "the last answer"),
        (ExpressionCalculator.TodayName, "today's date"),
    ];
}
