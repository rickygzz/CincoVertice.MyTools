using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CincoVertice.Common.Application.Calculator;
using CincoVertice.Common.Application.Calculator.Conversions;
using CincoVertice.Common.Application.Calculator.TimeZones;
using CincoVertice.MyTools.Ui.Calculator.Models;

namespace CincoVertice.MyTools.Ui.Calculator.Services;

/// <summary>
///     What the calculator offers while an expression is typed, from what's before the caret at its end:
///     <list type="bullet">
///         <item>after a time (<c>10:30 </c>, <c>3 pm </c>, <c>now </c>), the places to convert it from, then after
///         <c>to</c> the places to convert it to;</item>
///         <item>after a number, the units, then after <c>to</c> the units of the same kind.</item>
///     </list>
///     Choosing a first place or unit writes <c>to</c> after it, so the second ones follow. Only a single number shows
///     every unit as soon as a space follows it; after anything else, the units wait for a letter, so they don't pop up
///     at every space of a sum.
/// </summary>
public static partial class CalculatorSuggester
{
    private const string To = " to ";

    /// <param name="expression">What's typed.</param>
    /// <param name="caret">Where the caret is; there are suggestions only at the end.</param>
    /// <param name="utcNow">The time now, for each place's UTC offset, which changes with daylight saving time.</param>
    /// <returns>Null when there's nothing to offer.</returns>
    public static CalculatorSuggestionsModel? Suggest(string expression, int caret, DateTime utcNow)
    {
        if (caret != expression.Length)
        {
            return null;
        }

        Match time = TimeRegex().Match(expression);

        // After a time, places or nothing: a word there is never a unit
        return time.Success ? ForTime(expression, time, utcNow) : ForUnits(expression);
    }

    private static CalculatorSuggestionsModel? ForTime(string expression, Match time, DateTime utcNow)
    {
        Match destination = DestinationRegex().Match(expression, time.Length);

        if (destination.Success)
        {
            Group query = destination.Groups["query"];

            return Result(query, Places(query.Value, string.Empty, utcNow));
        }

        Match source = SourceRegex().Match(expression, time.Length);

        if (!source.Success)
        {
            return null;
        }

        Group typed = source.Groups["query"];
        List<CalculatorSuggestionModel> items = [];

        // "10:30 p" may be the start of pm, more likely than of Paris
        if (!time.Groups["ampm"].Success && time.Groups["hour"].Success && typed.Value.Length > 0)
        {
            int hour = int.Parse(time.Groups["hour"].Value, CultureInfo.InvariantCulture);

            items.AddRange(new[] { "am", "pm" }
                .Where(word => hour is >= 1 and <= 12 && StartsWith(word, typed.Value))
                .Select(word => new CalculatorSuggestionModel(word, word + " ")));
        }

        items.AddRange(Places(typed.Value, To, utcNow));

        return Result(typed, items);
    }

    private static CalculatorSuggestionsModel? ForUnits(string expression)
    {
        Match to = ToUnitRegex().Match(expression);

        if (to.Success)
        {
            UnitDefinition? from = FindUnit(to.Groups["from"].Value);
            Group query = to.Groups["query"];

            return Result(query, Units(query.Value, from, string.Empty));
        }

        Match unit = FromUnitRegex().Match(expression);

        if (!unit.Success)
        {
            return null;
        }

        Group typed = unit.Groups["query"];

        // Nothing typed yet: every unit only after a single number
        if (typed.Length == 0 && !SingleNumberRegex().IsMatch(expression))
        {
            return null;
        }

        return Result(typed, Units(typed.Value, null, To));
    }

    private static CalculatorSuggestionsModel? Result(Group typed, IEnumerable<CalculatorSuggestionModel> items)
    {
        List<CalculatorSuggestionModel> list = [.. items];

        return list.Count == 0 ? null : new CalculatorSuggestionsModel(typed.Index, typed.Length, list);
    }

    /// <summary>The places whose name, or another name, starts with what's typed; any case, without accents.</summary>
    private static IEnumerable<CalculatorSuggestionModel> Places(string typed, string after, DateTime utcNow)
    {
        string start = Simplified(typed);

        foreach (TimeZonePlace place in TimeZoneTable.Places)
        {
            string? other = Simplified(place.Name).StartsWith(start, StringComparison.Ordinal)
                ? null
                : place.OtherNames.FirstOrDefault(name => Simplified(name).StartsWith(start, StringComparison.Ordinal));

            if (other is null && !Simplified(place.Name).StartsWith(start, StringComparison.Ordinal))
            {
                continue;
            }

            string offset = OffsetOf(place.ZoneId, utcNow);
            string text = other is null ? $"{place.Name} ({offset})" : $"{place.Name} ({other}, {offset})";

            yield return new CalculatorSuggestionModel(text, place.Name + after);
        }

        if (TimeZoneTable.LocalName.StartsWith(start, StringComparison.Ordinal))
        {
            yield return new CalculatorSuggestionModel(
                $"{TimeZoneTable.LocalName} (this computer's time zone)",
                TimeZoneTable.LocalName + after);
        }

        if ("utc".StartsWith(start, StringComparison.Ordinal))
        {
            yield return new CalculatorSuggestionModel("UTC (or UTC-5, UTC+5:30)", "UTC" + after);
        }
    }

    /// <summary>
    ///     The units whose symbol starts with what's typed, then those with another name that does; of the kind of
    ///     the unit converted from, if there's one, but not that unit.
    /// </summary>
    private static IEnumerable<CalculatorSuggestionModel> Units(string typed, UnitDefinition? from, string after)
    {
        List<UnitDefinition> units =
        [
            .. UnitTable.Units.Where(unit => from is null || (unit.Group == from.Group && unit != from)),
        ];

        foreach (UnitDefinition unit in units.Where(unit => StartsWith(unit.Symbol, typed)))
        {
            string text = UnitText(unit, unit.Names.FirstOrDefault());

            yield return new CalculatorSuggestionModel(text, unit.Symbol + after);
        }

        foreach (UnitDefinition unit in units.Where(unit => !StartsWith(unit.Symbol, typed)))
        {
            if (unit.Names.FirstOrDefault(name => StartsWith(name, typed)) is string name)
            {
                yield return new CalculatorSuggestionModel(UnitText(unit, name), unit.Symbol + after);
            }
        }
    }

    private static string UnitText(UnitDefinition unit, string? name)
    {
        string kind = unit.Group.ToString().ToLowerInvariant();

        return name is null ? $"{unit.Symbol} ({kind})" : $"{unit.Symbol} ({name}, {kind})";
    }

    private static bool StartsWith(string name, string typed) =>
        name.StartsWith(typed, StringComparison.OrdinalIgnoreCase);

    private static UnitDefinition? FindUnit(string name)
    {
        try
        {
            return UnitTable.Find(name, 0);
        }
        catch (ExpressionException)
        {
            // Unknown, or could be more than one: no kind to keep to
            return null;
        }
    }

    private static string OffsetOf(string zoneId, DateTime utcNow) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(zoneId, out TimeZoneInfo? zone)
            ? TimeZoneConverter.FormatOffset(zone.GetUtcOffset(utcNow))
            : zoneId;

    /// <summary>As the calculator reads a place: any case, '_' for a space, without accents (São is sao).</summary>
    private static string Simplified(string name)
    {
        var simple = new StringBuilder();

        foreach (char letter in name.Replace('_', ' ').ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
            {
                simple.Append(letter);
            }
        }

        return simple.ToString();
    }

    // A time the calculator converts, followed by a space: 10:30, 10:30 pm, 3 pm, now
    [GeneratedRegex(
        @"^\s*(?:now|(?<hour>\d{1,2}):\d{2}(?:\s*(?<ampm>[ap]m))?|(?<hour>\d{1,2})\s*(?<ampm>[ap]m))(?=\s)",
        RegexOptions.IgnoreCase)]
    private static partial Regex TimeRegex();

    // After the time: a place, or none, then to, then the place being typed
    [GeneratedRegex(@"\G\s+(?:\S.*?\s+)?to\s+(?<query>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex DestinationRegex();

    [GeneratedRegex(@"\G\s+(?<query>.*)$")]
    private static partial Regex SourceRegex();

    // A number or a closing parenthesis, a unit, to, and the unit being typed
    [GeneratedRegex(@"(?:\d|\))\s*(?<from>[A-Za-z][\w/^]*)\s+to\s+(?<query>(?:[A-Za-z][\w/^]*)?)$")]
    private static partial Regex ToUnitRegex();

    // A number or a closing parenthesis, and the unit being typed after it
    [GeneratedRegex(@"(?:\d|\))\s*(?<query>(?:[A-Za-z][\w/^]*)?)$")]
    private static partial Regex FromUnitRegex();

    [GeneratedRegex(@"^\s*(?:\d+(?:\.\d*)?|\.\d+)\s+$")]
    private static partial Regex SingleNumberRegex();
}
