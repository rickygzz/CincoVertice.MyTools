using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.RegexTester.Models;

namespace CincoVertice.MyTools.Ui.RegexTester.Services;

/// <summary>
///     Runs a .NET regular expression on a text, as the regex tester shows it: its matches with their groups, and the
///     text replaced. A pattern that takes longer than <see cref="Timeout"/> is stopped, as (a+)+b can on a long text,
///     so typing a pattern never freezes the window.
/// </summary>
public static class RegexRunner
{
    /// <summary>The matches shown at most: more would take long to color, and be read by no one.</summary>
    public const int MaxMatches = 1000;

    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    public static RegexResultModel Run(RegexInputModel input)
    {
        if (input.Pattern.Length == 0)
        {
            return new RegexResultModel([], false, null, null);
        }

        Regex regex;

        try
        {
            regex = new Regex(input.Pattern, OptionsOf(input), Timeout);
        }
        catch (ArgumentException ex)
        {
            return new RegexResultModel([], false, ex.Message, null);
        }

        try
        {
            List<RegexMatchModel> matches = [];
            bool hasMore = false;

            for (Match match = regex.Match(input.Text); match.Success; match = match.NextMatch())
            {
                if (matches.Count == MaxMatches)
                {
                    hasMore = true;

                    break;
                }

                matches.Add(ToModel(regex, match, matches.Count + 1));
            }

            string? replaced = input.Replacement.Length > 0 ? regex.Replace(input.Text, input.Replacement) : null;

            return new RegexResultModel(matches, hasMore, null, replaced);
        }
        catch (RegexMatchTimeoutException)
        {
            return new RegexResultModel(
                [],
                false,
                $"The pattern took more than {Timeout.TotalSeconds:0} second on this text, so it was stopped: "
                    + "a part that repeats inside another that repeats, as (a+)+, can try without end.",
                null);
        }
    }

    private static RegexOptions OptionsOf(RegexInputModel input)
    {
        RegexOptions options = RegexOptions.None;

        if (input.IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        }

        if (input.Multiline)
        {
            options |= RegexOptions.Multiline;
        }

        if (input.DotAll)
        {
            options |= RegexOptions.Singleline;
        }

        return options;
    }

    private static RegexMatchModel ToModel(Regex regex, Match match, int number)
    {
        List<RegexGroupModel> groups = [];

        // Group 0 is the whole match
        for (int i = 1; i < match.Groups.Count; i++)
        {
            Group group = match.Groups[i];
            int groupNumber = regex.GroupNumberFromName(group.Name);
            string? name = int.TryParse(group.Name, out _) ? null : group.Name;

            groups.Add(new RegexGroupModel(groupNumber, name, group.Index, group.Length, group.Value, group.Success));
        }

        return new RegexMatchModel(number, match.Index, match.Length, match.Value, groups);
    }
}
