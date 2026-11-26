using System.Globalization;
using System.Text;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;

namespace CincoVertice.MyTools.Ui.JsonEditor.JmesPath;

/// <summary>A JMESPath expression's tokens, the last of them <see cref="JmesTokenKind.End"/>.</summary>
public static class JmesLexer
{
    /// <exception cref="JmesPathException">Something that isn't JMESPath, or a quote not closed.</exception>
    public static List<JmesToken> Tokenize(string expression)
    {
        List<JmesToken> tokens = [];
        int i = 0;

        while (i < expression.Length)
        {
            char c = expression[i];
            int start = i;
            char next = i + 1 < expression.Length ? expression[i + 1] : '\0';

            if (char.IsWhiteSpace(c))
            {
                i++;

                continue;
            }

            if (char.IsAsciiLetter(c) || c == '_')
            {
                while (i < expression.Length && (char.IsAsciiLetterOrDigit(expression[i]) || expression[i] == '_'))
                {
                    i++;
                }

                tokens.Add(new JmesToken(JmesTokenKind.UnquotedIdentifier, start, expression[start..i]));

                continue;
            }

            if (char.IsAsciiDigit(c) || (c == '-' && char.IsAsciiDigit(next)))
            {
                i++;

                while (i < expression.Length && char.IsAsciiDigit(expression[i]))
                {
                    i++;
                }

                if (!int.TryParse(expression[start..i], CultureInfo.InvariantCulture, out int number))
                {
                    throw new JmesPathException($"'{expression[start..i]}' is too large", start);
                }

                tokens.Add(new JmesToken(JmesTokenKind.Number, start, Number: number));

                continue;
            }

            switch (c)
            {
                case '"':
                    string name = Quoted(expression, ref i, '"', start);
                    tokens.Add(new JmesToken(JmesTokenKind.QuotedIdentifier, start, Unescape(name, start)));
                    continue;
                case '\'':
                    string raw = Quoted(expression, ref i, '\'', start).Replace("\\'", "'", StringComparison.Ordinal);
                    tokens.Add(new JmesToken(JmesTokenKind.Literal, start, Value: JsonNodeModel.String(raw)));
                    continue;
                case '`':
                    string json = Quoted(expression, ref i, '`', start).Replace("\\`", "`", StringComparison.Ordinal);
                    tokens.Add(new JmesToken(JmesTokenKind.Literal, start, Value: Literal(json, start)));
                    continue;
            }

            (JmesTokenKind kind, int length) = c switch
            {
                '.' => (JmesTokenKind.Dot, 1),
                '*' => (JmesTokenKind.Star, 1),
                '[' when next == ']' => (JmesTokenKind.Flatten, 2),
                '[' when next == '?' => (JmesTokenKind.Filter, 2),
                '[' => (JmesTokenKind.LBracket, 1),
                ']' => (JmesTokenKind.RBracket, 1),
                '{' => (JmesTokenKind.LBrace, 1),
                '}' => (JmesTokenKind.RBrace, 1),
                '(' => (JmesTokenKind.LParen, 1),
                ')' => (JmesTokenKind.RParen, 1),
                ',' => (JmesTokenKind.Comma, 1),
                ':' => (JmesTokenKind.Colon, 1),
                '@' => (JmesTokenKind.Current, 1),
                '|' when next == '|' => (JmesTokenKind.Or, 2),
                '|' => (JmesTokenKind.Pipe, 1),
                '&' when next == '&' => (JmesTokenKind.And, 2),
                '&' => (JmesTokenKind.ExpRef, 1),
                '!' when next == '=' => (JmesTokenKind.Ne, 2),
                '!' => (JmesTokenKind.Not, 1),
                '=' when next == '=' => (JmesTokenKind.Eq, 2),
                '<' when next == '=' => (JmesTokenKind.Lte, 2),
                '<' => (JmesTokenKind.Lt, 1),
                '>' when next == '=' => (JmesTokenKind.Gte, 2),
                '>' => (JmesTokenKind.Gt, 1),
                '=' => throw new JmesPathException("Write == to compare", start),
                _ => throw new JmesPathException($"'{c}' isn't part of JMESPath", start),
            };

            tokens.Add(new JmesToken(kind, start));
            i += length;
        }

        tokens.Add(new JmesToken(JmesTokenKind.End, expression.Length));

        return tokens;
    }

    // What's between the quotes, escapes as written; past the closing quote
    private static string Quoted(string expression, ref int i, char quote, int start)
    {
        i++;
        int from = i;

        while (i < expression.Length && expression[i] != quote)
        {
            i += expression[i] == '\\' ? 2 : 1;
        }

        if (i >= expression.Length)
        {
            throw new JmesPathException($"The {quote} isn't closed", start);
        }

        return expression[from..i++];
    }

    private static string Unescape(string text, int position)
    {
        JsonDocumentModel document = JsonDocumentReader.Read([$"\"{text}\""]);

        return document.Error is null && document.Root is { Kind: JsonNodeKind.String } value
            ? value.Text
            : throw new JmesPathException("The quoted name isn't a JSON string", position);
    }

    private static JsonNodeModel Literal(string json, int position)
    {
        JsonDocumentModel document = JsonDocumentReader.Read(json.Trim().ReplaceLineEndings("\n").Split('\n'));

        return document.Error is null && document.Root is { } value
            ? Detached(value)
            : throw new JmesPathException($"`{json}` isn't JSON: {document.Error ?? "it's empty"}", position);
    }

    // A literal is no place in the document: a search must not show where it was read
    private static JsonNodeModel Detached(JsonNodeModel node)
    {
        node.Start = null;
        node.End = null;
        node.KeyStart = null;
        node.Members.ForEach(member => Detached(member.Value));
        node.Items.ForEach(item => Detached(item));

        return node;
    }

    /// <returns>A token as an error names it: 'name', '[', the end.</returns>
    internal static string Describe(JmesToken token) => token.Kind switch
    {
        JmesTokenKind.End => "the end",
        JmesTokenKind.UnquotedIdentifier or JmesTokenKind.QuotedIdentifier => $"'{token.Text}'",
        JmesTokenKind.Number => token.Number.ToString(CultureInfo.InvariantCulture),
        JmesTokenKind.Literal => "a literal",
        _ => $"'{Symbol(token.Kind)}'",
    };

    private static string Symbol(JmesTokenKind kind) => kind switch
    {
        JmesTokenKind.Dot => ".",
        JmesTokenKind.Star => "*",
        JmesTokenKind.Flatten => "[]",
        JmesTokenKind.Filter => "[?",
        JmesTokenKind.LBracket => "[",
        JmesTokenKind.RBracket => "]",
        JmesTokenKind.LBrace => "{",
        JmesTokenKind.RBrace => "}",
        JmesTokenKind.LParen => "(",
        JmesTokenKind.RParen => ")",
        JmesTokenKind.Comma => ",",
        JmesTokenKind.Colon => ":",
        JmesTokenKind.Pipe => "|",
        JmesTokenKind.Or => "||",
        JmesTokenKind.And => "&&",
        JmesTokenKind.Not => "!",
        JmesTokenKind.Eq => "==",
        JmesTokenKind.Ne => "!=",
        JmesTokenKind.Lt => "<",
        JmesTokenKind.Lte => "<=",
        JmesTokenKind.Gt => ">",
        JmesTokenKind.Gte => ">=",
        JmesTokenKind.Current => "@",
        _ => "&",
    };
}
