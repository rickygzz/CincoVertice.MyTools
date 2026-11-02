using CincoVertice.Common.Utils.Config;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     The config lexer drops lines it can't read (e.g. "Ctrl+Alt+A = ShowCalculator") without a trace, so a reader
///     that reports problems asks here which lines those were.
/// </summary>
public static class ConfigLineChecker
{
    /// <returns>Every line that is not blank, not a comment and not one of the parsed keys, numbered from 1.</returns>
    public static IEnumerable<(int Number, string Line)> FindUnreadLines(string text, ConfigItems items)
    {
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < items.KeysCount; i++)
        {
            keys.Add(items.GetKeyName(i));
        }

        string[] lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int equals = line.IndexOf('=');

            if (equals < 0 || !keys.Contains(line[..equals].Trim()))
            {
                yield return (i + 1, line);
            }
        }
    }
}
