using CincoVertice.MyTools.Ui.Calculator.Models;
using CincoVertice.Common.Application.Calculator;
using CincoVertice.Common.Application.Calculator.Functions;
using CincoVertice.Common.Application.Calculator.Models;
using CincoVertice.MyTools.Ui.Calculator.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Calculator;

public class CalculatorSuggesterTests
{
    // In winter, so Mexico City is UTC-6 and São Paulo UTC-3 whatever the date the tests run
    private static readonly DateTime _now = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static CalculatorSuggestionsModel? Suggest(string expression) =>
        CalculatorSuggester.Suggest(expression, expression.Length, _now);

    private static List<string> Texts(string expression) =>
        [.. Suggest(expression)?.Items.Select(item => item.Text) ?? []];

    [Theory]
    [InlineData("10:30 ")]
    [InlineData("3 pm ")]
    [InlineData("9:00 am ")]
    [InlineData("now ")]
    [InlineData("10:00 to ")]
    public void AfterATime_EveryPlace(string expression)
    {
        // Act
        List<string> texts = Texts(expression);

        // Assert
        Assert.Equal("Mexico City (UTC-6)", texts[0]);
        Assert.Contains("local (this computer's time zone)", texts);
        Assert.Contains("UTC (or UTC-5, UTC+5:30)", texts);
    }

    [Theory]
    [InlineData("10:30 sao", "São Paulo (UTC-3)")]
    [InlineData("10:30 SÃO", "São Paulo (UTC-3)")]
    [InlineData("10:30 cdmx", "Mexico City (cdmx, UTC-6)")]
    [InlineData("10:30 new_y", "New York (UTC-5)")]
    [InlineData("10:30 Mexico City to tok", "Tokyo (UTC+9)")]
    public void APlaceBeingTyped_IsFoundByAnyOfItsNames_InAnyCase_WithoutAccents(string expression, string expected)
    {
        // Act / Assert
        Assert.Equal([expected], Texts(expression));
    }

    [Fact]
    public void APlace_ReplacesWhatsTyped_AndTheFirstOneWritesTo()
    {
        // Act
        CalculatorSuggestionsModel from = Suggest("10:30 mex")!;
        CalculatorSuggestionsModel to = Suggest("10:30 Mexico City to ind")!;

        // Assert
        Assert.Equal((6, 3, "Mexico City to "), (from.Start, from.Length, from.Items[0].Insert));
        Assert.Equal((21, 3, "India"), (to.Start, to.Length, to.Items[0].Insert));
    }

    [Fact]
    public void AfterA12HourTime_APIsFirstPm()
    {
        // Act
        List<string> texts = Texts("10:30 p");

        // Assert: a 24-hour time can't have it
        Assert.Equal("pm", texts[0]);
        Assert.Contains("Paris (UTC+1)", texts);
        Assert.DoesNotContain("pm", Texts("15:30 p"));
    }

    [Theory]
    [InlineData("10:30 kg")]
    [InlineData("10:30 India ")]
    [InlineData("10:30")]
    public void AfterATime_NothingButPlaces(string expression)
    {
        // Act / Assert: a word there isn't a unit, and a whole place waits for "to"
        Assert.Null(Suggest(expression));
    }

    [Theory]
    [InlineData("10 ")]
    [InlineData(" 2.5 ")]
    public void AfterASingleNumberAndASpace_EveryUnit(string expression)
    {
        // Act
        CalculatorSuggestionsModel suggestions = Suggest(expression)!;

        // Assert
        Assert.Equal("nm (nanometer, length)", suggestions.Items[0].Text);
        Assert.Equal("nm to ", suggestions.Items[0].Insert);
        Assert.Contains(suggestions.Items, item => item.Text == "KiB (data)");
    }

    [Theory]
    [InlineData("10")]
    [InlineData("1 + 2 ")]
    [InlineData("max(1, 5) ")]
    [InlineData("2+2")]
    public void UnitsAfterOtherThanASingleNumber_WaitForALetter(string expression)
    {
        // Act / Assert
        Assert.Null(Suggest(expression));
    }

    [Fact]
    public void AUnitBeingTyped_IsFoundByItsSymbolFirst_ThenByItsOtherNames()
    {
        // Act
        List<string> byName = Texts("(1 + 2) kilo");
        List<string> byLetter = Texts("1 + 2 K");

        // Assert
        Assert.Equal(["km (kilometer, length)", "kg (kilogram, mass)", "kB (kilobyte, data)"], byName);
        Assert.Equal("km (kilometer, length)", byLetter[0]);
        Assert.Contains("K (kelvin, temperature)", byLetter);
    }

    [Theory]
    [InlineData("today + 30 ", "d (days)", "wk (weeks)", "mo (months)", "yr (years)")]
    [InlineData("today + 30 m", "mo (months)")]
    [InlineData("2026-12-25 - 3w", "wk (weeks)")]
    [InlineData("TODAY + 1 Y", "yr (years)")]
    public void AfterANumberWithADate_TheLengthsOfTimeToMoveItBy(string expression, params string[] expected)
    {
        // Act
        CalculatorSuggestionsModel suggestions = Suggest(expression)!;

        // Assert: the unit alone, not a conversion
        Assert.Equal(expected, suggestions.Items.Select(item => item.Text));
        Assert.DoesNotContain(suggestions.Items, item => item.Insert.Contains(" to ", StringComparison.Ordinal));
    }

    [Fact]
    public void AfterTo_UnitsOfTheSameKind_ButNotTheSameUnit()
    {
        // Act
        CalculatorSuggestionsModel suggestions = Suggest("5 kg to ")!;
        List<string> afterAnUnknownUnit = Texts("5 zz to po");

        // Assert
        Assert.Equal("mg", suggestions.Items[0].Insert);
        Assert.DoesNotContain(suggestions.Items, item => item.Insert == "kg");
        Assert.All(suggestions.Items, item => Assert.EndsWith("mass)", item.Text));
        Assert.Equal(["lb (pound, mass)"], afterAnUnknownUnit);
    }

    [Theory]
    [InlineData("s", "sin(x): sine, in radians", "sqrt(x): square root")]
    [InlineData("SQ", "sqrt(x): square root")]
    [InlineData("2 * (ro", "round(x, decimals): rounds; decimals are optional")]
    [InlineData("max(1, mi", "min(a, ...): smallest")]
    [InlineData("x = fl", "floor(x): rounds down")]
    [InlineData("p", "pi: 3.14159...")]
    [InlineData("tod", "today: today's date")]
    [InlineData("e", "e: 2.71828...", "exp(x): e to the power x")]
    public void WhereAValueStarts_TheFunctionsAndConstants_WhoseNameStartsWithWhatsTyped(
        string expression,
        params string[] expected)
    {
        // Act / Assert
        Assert.Equal(expected, Texts(expression));
    }

    [Theory]
    [InlineData("sq", 0, 2, "sqrt(")]
    [InlineData("1 + ipc", 4, 3, "ipconfig()")]
    [InlineData("id = gu", 5, 2, "guid()")]
    [InlineData("an", 0, 2, "ans")]
    public void AFunction_WritesItsParenthesis(string expression, int start, int length, string insert)
    {
        // Act
        CalculatorSuggestionsModel suggestions = Suggest(expression)!;

        // Assert
        Assert.Equal((start, length, insert), (suggestions.Start, suggestions.Length, suggestions.Items[0].Insert));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1 + ")]
    [InlineData("sqrt(")]
    [InlineData("len(\"s")]
    [InlineData("zz")]
    public void WithoutLettersWhereAValueStarts_NoFunctions(string expression)
    {
        // Act / Assert
        Assert.Null(Suggest(expression));
    }

    [Fact]
    public void AfterANumber_UnitsComeBeforeFunctions()
    {
        // Act / Assert: 2 s is two seconds, never a function
        Assert.All(Texts("2 s"), text => Assert.DoesNotContain("(x)", text));
    }

    [Fact]
    public void TheNamesGivenValues_AreOffered_WithTheirValues_CutShortWhenLong()
    {
        // Arrange
        var names = new Dictionary<string, CalculatorValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["Total"] = CalculatorValue.FromNumber(1234.5m),
            ["tip"] = CalculatorValue.FromText(new string('a', 50)),
            [ExpressionCalculator.LastAnswerName] = CalculatorValue.FromNumber(7),
        };

        // Act
        List<string> texts =
            [.. CalculatorSuggester.Suggest("2 + t", 5, _now, names)!.Items.Select(item => item.Text)];
        string answer = CalculatorSuggester.Suggest("an", 2, _now, names)!.Items.Single().Text;

        // Assert
        Assert.Equal(
            ["tan(x): tangent, in radians", $"tip: {new string('a', 37)}...", "today: today's date", "Total: 1,234.5"],
            texts);
        Assert.Equal("ans: the last answer, 7", answer);
    }

    [Fact]
    public void EveryFunctionOfTheCalculator_IsOffered()
    {
        // Arrange
        IEnumerable<string> functions = BuiltIns.Functions.Keys.Concat(BuiltIns.TextFunctions);

        // Act / Assert
        Assert.Equal(
            functions.Order(StringComparer.OrdinalIgnoreCase),
            CalculatorFunctionHelp.Functions.Keys.Order(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void WithTheCaretBeforeTheEnd_Nothing()
    {
        // Act / Assert
        Assert.Null(CalculatorSuggester.Suggest("10 km", 2, _now));
    }
}
