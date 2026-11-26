using System.Globalization;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.JmesPath;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     Where a search's matches are: text, ignoring case; or the values of the document a JMESPath expression gives,
///     each of them where it is, those a projection gathers too.
/// </summary>
public static class JsonSearch
{
    // Enough to find through a large file without holding up typing
    private const int MaxMatches = 10_000;

    public static IReadOnlyList<JsonMatchModel> FindText(IReadOnlyList<string> lines, string text)
    {
        List<JsonMatchModel> matches = [];

        if (text.Length == 0)
        {
            return matches;
        }

        CompareInfo compare = CultureInfo.InvariantCulture.CompareInfo;

        for (int line = 0; line < lines.Count && matches.Count < MaxMatches; line++)
        {
            int from = 0;

            while (from <= lines[line].Length - text.Length)
            {
                int at = compare.IndexOf(lines[line], text, from, CompareOptions.IgnoreCase);

                if (at < 0)
                {
                    break;
                }

                matches.Add(new JsonMatchModel(new DiffPosition(line, at), new DiffPosition(line, at + text.Length)));
                from = at + Math.Max(1, text.Length);
            }
        }

        return matches;
    }

    /// <returns>Where the values a result holds are in the text, in their order; none for values made.</returns>
    public static IReadOnlyList<JsonMatchModel> PlacesOf(JsonNodeModel result)
    {
        List<JsonMatchModel> matches = [];
        Collect(result, matches);

        return [.. matches.Distinct().OrderBy(match => match.Start)];
    }

    /// <summary>Runs a JMESPath expression on the document.</summary>
    /// <returns>Its result, or why it can't run and where in the expression.</returns>
    public static (JsonNodeModel? Result, string? Error, int ErrorAt) Run(string expression, JsonNodeModel? root)
    {
        if (root is null)
        {
            return (null, "There's no JSON to search", 0);
        }

        try
        {
            return (JmesEvaluator.Search(expression, root), null, 0);
        }
        catch (JmesPathException ex)
        {
            return (null, ex.Message, ex.Position);
        }
    }

    // A value of the document is a match; an array made, as a projection's, holds the matches
    private static void Collect(JsonNodeModel node, List<JsonMatchModel> matches)
    {
        if (matches.Count >= MaxMatches)
        {
            return;
        }

        if (node.Start is DiffPosition start && node.End is DiffPosition end)
        {
            matches.Add(new JsonMatchModel(start, end));
        }
        else if (node.Kind == JsonNodeKind.Array)
        {
            node.Items.ForEach(item => Collect(item, matches));
        }
        else if (node.Kind == JsonNodeKind.Object)
        {
            node.Members.ForEach(member => Collect(member.Value, matches));
        }
    }
}
