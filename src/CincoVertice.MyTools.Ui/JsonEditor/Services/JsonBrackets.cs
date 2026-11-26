using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     The bracket at the caret and the one it pairs with, from the document's pieces, so brackets in strings and
///     comments don't count. A { pairs only with a }, and [ with ], so a broken document still pairs the rest.
/// </summary>
public static class JsonBrackets
{
    /// <returns>
    ///     The bracket just after the caret, or else just before it, and its pair, the opening one first; null when
    ///     the caret isn't at a bracket, or it has no pair.
    /// </returns>
    public static (DiffPosition Open, DiffPosition Close)? PairAt(
        IReadOnlyList<IReadOnlyList<JsonTokenModel>> tokens,
        IReadOnlyList<string> lines,
        DiffPosition caret)
    {
        List<(DiffPosition At, char Bracket)> brackets = [];
        int at = -1;

        for (int line = 0; line < tokens.Count && line < lines.Count; line++)
        {
            foreach (JsonTokenModel token in tokens[line])
            {
                if (token.Kind != JsonTokenKind.Punctuation || token.Column >= lines[line].Length)
                {
                    continue;
                }

                char c = lines[line][token.Column];

                if (c is '{' or '}' or '[' or ']')
                {
                    var position = new DiffPosition(line, token.Column);

                    // After the caret is preferred to before it, as in most editors
                    if (position == caret || (at < 0 && position == caret with { Column = caret.Column - 1 }))
                    {
                        at = brackets.Count;
                    }

                    brackets.Add((position, c));
                }
            }
        }

        if (at < 0)
        {
            return null;
        }

        (DiffPosition start, char bracket) = brackets[at];
        bool opens = bracket is '{' or '[';
        char partner = bracket switch { '{' => '}', '}' => '{', '[' => ']', _ => '[' };
        int depth = 0;

        for (int i = at + (opens ? 1 : -1); i >= 0 && i < brackets.Count; i += opens ? 1 : -1)
        {
            if (brackets[i].Bracket == bracket)
            {
                depth++;
            }
            else if (brackets[i].Bracket == partner && depth-- == 0)
            {
                return opens ? (start, brackets[i].At) : (brackets[i].At, start);
            }
        }

        return null;
    }
}
