using CincoVertice.Common.Utils.Config;
using CincoVertice.MyTools.Ui.Hotstrings.Models;

namespace CincoVertice.MyTools.Ui.Hotstrings.Services;

/// <summary>
///     Reads hotstrings.cfg with the config lexer: "btw = by the way" per line, or a text of several lines between [" and
///     "], # for comments. A name is letters and digits only: a hotstring is found from the keys pressed, and other
///     characters are on different keys on each keyboard layout. Every line that can't be used is a problem, and skipped.
/// </summary>
public static class HotstringReader
{
    public static HotstringsResultModel Read(string text)
    {
        List<HotstringModel> hotstrings = [];
        List<HotstringProblemModel> problems = [];
        Dictionary<string, int> lines = new(StringComparer.OrdinalIgnoreCase);
        ConfigLexer lexer = new(text);
        ConfigItems items = lexer.Items;

        for (int i = 0; i < items.KeysCount; i++)
        {
            string name = items.GetKeyName(i);
            int line = items.GetLine(i);
            int type = items.GetValueType(i);

            if (type == ConfigTokenType.ErrorQuotedStringNotClosed)
            {
                problems.Add(new(line, $"{name}: the [\" is never closed with \"], so the rest of the file is ignored."));

                continue;
            }

            if (!name.All(char.IsAsciiLetterOrDigit))
            {
                problems.Add(new(line, $"{name}: only letters and digits can be typed as a hotstring."));

                continue;
            }

            if (name.Length > HotstringTracker.MaxLength)
            {
                problems.Add(new(line, $"{name}: a hotstring has at most {HotstringTracker.MaxLength} letters."));

                continue;
            }

            string replacement = type == ConfigTokenType.StringQuoted
                ? items.GetValue(i).ReplaceLineEndings("\n")
                : items.GetValue(i).TrimEnd();

            if (type == ConfigTokenType.ErrorNoValueFound || replacement.Length == 0)
            {
                problems.Add(new(line, $"{name}: what does it become? {name} = text."));

                continue;
            }

            if (lines.TryGetValue(name, out int first))
            {
                problems.Add(new(line, $"{name}: already on line {first}; that one is used."));

                continue;
            }

            lines[name] = line;
            hotstrings.Add(new HotstringModel(name, replacement, line));
        }

        foreach (var (number, badLine) in lexer.BadLines)
        {
            problems.Add(new(number, $"'{badLine}' is not 'abbreviation = text'."));
        }

        return new HotstringsResultModel(hotstrings, [.. problems.OrderBy(problem => problem.Line)]);
    }
}
