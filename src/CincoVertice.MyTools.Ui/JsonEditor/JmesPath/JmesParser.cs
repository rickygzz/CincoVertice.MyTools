namespace CincoVertice.MyTools.Ui.JsonEditor.JmesPath;

/// <summary>
///     A JMESPath expression's syntax tree, read as the specification's reference implementation does (a Pratt parser
///     with its binding powers), so projections, pipes and comparisons group as in every other JMESPath tool.
/// </summary>
public sealed class JmesParser
{
    // Below this, a token ends a projection: what follows it isn't applied to each element
    private const int ProjectionStop = 10;

    private readonly List<JmesToken> _tokens;
    private int _next;

    private JmesParser(List<JmesToken> tokens)
    {
        _tokens = tokens;
    }

    private JmesToken Current => _tokens[_next];

    /// <exception cref="JmesPathException">It isn't JMESPath; the message says what's wrong, and where.</exception>
    public static JmesNode Parse(string expression)
    {
        var parser = new JmesParser(JmesLexer.Tokenize(expression));

        if (parser.Current.Kind == JmesTokenKind.End)
        {
            throw new JmesPathException("Write an expression, such as people[0].name", 0);
        }

        JmesNode tree = parser.Expression(0);

        if (parser.Current.Kind != JmesTokenKind.End)
        {
            throw parser.Unexpected();
        }

        return tree;
    }

    private static int BindingPower(JmesTokenKind kind) => kind switch
    {
        JmesTokenKind.Pipe => 1,
        JmesTokenKind.Or => 2,
        JmesTokenKind.And => 3,
        JmesTokenKind.Eq or JmesTokenKind.Ne or JmesTokenKind.Lt or JmesTokenKind.Lte or JmesTokenKind.Gt
            or JmesTokenKind.Gte => 5,
        JmesTokenKind.Flatten => 9,
        JmesTokenKind.Star => 20,
        JmesTokenKind.Filter => 21,
        JmesTokenKind.Dot => 40,
        JmesTokenKind.Not => 45,
        JmesTokenKind.LBrace => 50,
        JmesTokenKind.LBracket => 55,
        JmesTokenKind.LParen => 60,
        _ => 0,
    };

    private JmesNode Expression(int bindingPower)
    {
        JmesNode left = Nud(Advance());

        while (bindingPower < BindingPower(Current.Kind))
        {
            left = Led(Advance(), left);
        }

        return left;
    }

    private JmesNode Nud(JmesToken token)
    {
        int at = token.Position;

        switch (token.Kind)
        {
            case JmesTokenKind.Literal:
                return new JmesNode(JmesNodeKind.Literal, at, [], Value: token.Value);
            case JmesTokenKind.UnquotedIdentifier:
                return new JmesNode(JmesNodeKind.Field, at, [], token.Text);
            case JmesTokenKind.QuotedIdentifier:
                if (Current.Kind == JmesTokenKind.LParen)
                {
                    throw new JmesPathException("A function's name isn't quoted", at);
                }

                return new JmesNode(JmesNodeKind.Field, at, [], token.Text);
            case JmesTokenKind.Star:
                return JmesNode.Of(
                    JmesNodeKind.ValueProjection,
                    at,
                    Identity(at),
                    ProjectionRhs(BindingPower(JmesTokenKind.Star)));
            case JmesTokenKind.Filter:
                return FilterOn(Identity(at), at);
            case JmesTokenKind.LBrace:
                return MultiSelectHash(at);
            case JmesTokenKind.LParen:
                JmesNode inner = Expression(0);
                Expect(JmesTokenKind.RParen);
                return inner;
            case JmesTokenKind.Flatten:
                return JmesNode.Of(
                    JmesNodeKind.Projection,
                    at,
                    JmesNode.Of(JmesNodeKind.Flatten, at, Identity(at)),
                    ProjectionRhs(BindingPower(JmesTokenKind.Flatten)));
            case JmesTokenKind.Not:
                return JmesNode.Of(JmesNodeKind.Not, at, Expression(BindingPower(JmesTokenKind.Not)));
            case JmesTokenKind.LBracket:
                if (Current.Kind is JmesTokenKind.Number or JmesTokenKind.Colon)
                {
                    return ProjectIfSlice(Identity(at), IndexOrSlice(), at);
                }

                if (Current.Kind == JmesTokenKind.Star && Peek(1).Kind == JmesTokenKind.RBracket)
                {
                    _next += 2;

                    return JmesNode.Of(
                        JmesNodeKind.Projection,
                        at,
                        Identity(at),
                        ProjectionRhs(BindingPower(JmesTokenKind.Star)));
                }

                return MultiSelectList(at);
            case JmesTokenKind.Current:
                return new JmesNode(JmesNodeKind.Current, at, []);
            case JmesTokenKind.ExpRef:
                return JmesNode.Of(JmesNodeKind.ExpRef, at, Expression(BindingPower(JmesTokenKind.ExpRef)));
            default:
                _next--;
                throw Unexpected();
        }
    }

    private JmesNode Led(JmesToken token, JmesNode left)
    {
        int at = token.Position;

        switch (token.Kind)
        {
            case JmesTokenKind.Dot when Current.Kind == JmesTokenKind.Star:
                _next++;

                return JmesNode.Of(
                    JmesNodeKind.ValueProjection,
                    at,
                    left,
                    ProjectionRhs(BindingPower(JmesTokenKind.Dot)));
            case JmesTokenKind.Dot:
                return JmesNode.Of(JmesNodeKind.Subexpression, at, left, DotRhs(BindingPower(JmesTokenKind.Dot)));
            case JmesTokenKind.Pipe:
                return JmesNode.Of(JmesNodeKind.Pipe, at, left, Expression(BindingPower(JmesTokenKind.Pipe)));
            case JmesTokenKind.Or:
                return JmesNode.Of(JmesNodeKind.Or, at, left, Expression(BindingPower(JmesTokenKind.Or)));
            case JmesTokenKind.And:
                return JmesNode.Of(JmesNodeKind.And, at, left, Expression(BindingPower(JmesTokenKind.And)));
            case JmesTokenKind.LParen:
                return FunctionCall(left, at);
            case JmesTokenKind.Filter:
                return FilterOn(left, at);
            case JmesTokenKind.Eq or JmesTokenKind.Ne or JmesTokenKind.Lt or JmesTokenKind.Lte or JmesTokenKind.Gt
                or JmesTokenKind.Gte:
                return new JmesNode(
                    JmesNodeKind.Comparator,
                    at,
                    [left, Expression(BindingPower(token.Kind))],
                    token.Kind.ToString());
            case JmesTokenKind.Flatten:
                return JmesNode.Of(
                    JmesNodeKind.Projection,
                    at,
                    JmesNode.Of(JmesNodeKind.Flatten, at, left),
                    ProjectionRhs(BindingPower(JmesTokenKind.Flatten)));
            case JmesTokenKind.LBracket when Current.Kind is JmesTokenKind.Number or JmesTokenKind.Colon:
                return ProjectIfSlice(left, IndexOrSlice(), at);
            case JmesTokenKind.LBracket:
                Expect(JmesTokenKind.Star);
                Expect(JmesTokenKind.RBracket);

                return JmesNode.Of(JmesNodeKind.Projection, at, left, ProjectionRhs(BindingPower(JmesTokenKind.Star)));
            default:
                _next--;
                throw Unexpected();
        }
    }

    // [? condition ] after what it filters, then what applies to each element kept
    private JmesNode FilterOn(JmesNode left, int at)
    {
        JmesNode condition = Expression(0);
        Expect(JmesTokenKind.RBracket);
        JmesNode right = Current.Kind == JmesTokenKind.Flatten
            ? Identity(at)
            : ProjectionRhs(BindingPower(JmesTokenKind.Filter));

        return JmesNode.Of(JmesNodeKind.FilterProjection, at, left, right, condition);
    }

    private JmesNode FunctionCall(JmesNode name, int at)
    {
        if (name.Kind != JmesNodeKind.Field)
        {
            throw new JmesPathException("Only a function's name goes before (", at);
        }

        List<JmesNode> arguments = [];

        while (Current.Kind != JmesTokenKind.RParen)
        {
            arguments.Add(Expression(0));

            if (Current.Kind == JmesTokenKind.Comma)
            {
                _next++;
            }
            else if (Current.Kind != JmesTokenKind.RParen)
            {
                throw Unexpected();
            }
        }

        _next++;

        return new JmesNode(JmesNodeKind.FunctionCall, name.Position, arguments, name.Name);
    }

    // After [: a number then ], or a slice start:stop:step
    private JmesNode IndexOrSlice()
    {
        int at = Current.Position;

        if (Current.Kind == JmesTokenKind.Colon || Peek(1).Kind == JmesTokenKind.Colon)
        {
            int?[] parts = new int?[3];
            int part = 0;

            while (Current.Kind != JmesTokenKind.RBracket && part < 3)
            {
                if (Current.Kind == JmesTokenKind.Colon)
                {
                    part++;
                    _next++;
                }
                else if (Current.Kind == JmesTokenKind.Number)
                {
                    parts[part] = Advance().Number;
                }
                else
                {
                    throw Unexpected();
                }
            }

            Expect(JmesTokenKind.RBracket);

            if (parts[2] == 0)
            {
                throw new JmesPathException("A slice's step can't be 0", at);
            }

            return new JmesNode(JmesNodeKind.Slice, at, [], Slice: parts);
        }

        int index = Expect(JmesTokenKind.Number).Number;
        Expect(JmesTokenKind.RBracket);

        return new JmesNode(JmesNodeKind.Index, at, [], Slice: [index]);
    }

    private JmesNode ProjectIfSlice(JmesNode left, JmesNode right, int at)
    {
        JmesNode index = JmesNode.Of(JmesNodeKind.IndexExpression, at, left, right);

        return right.Kind == JmesNodeKind.Slice
            ? JmesNode.Of(JmesNodeKind.Projection, at, index, ProjectionRhs(BindingPower(JmesTokenKind.Star)))
            : index;
    }

    // What a projection applies to each element: nothing more when a weaker token follows
    private JmesNode ProjectionRhs(int bindingPower)
    {
        if (BindingPower(Current.Kind) < ProjectionStop)
        {
            return Identity(Current.Position);
        }

        if (Current.Kind is JmesTokenKind.LBracket or JmesTokenKind.Filter)
        {
            return Expression(bindingPower);
        }

        if (Current.Kind == JmesTokenKind.Dot)
        {
            _next++;

            return DotRhs(bindingPower);
        }

        throw Unexpected();
    }

    private JmesNode DotRhs(int bindingPower)
    {
        switch (Current.Kind)
        {
            case JmesTokenKind.UnquotedIdentifier or JmesTokenKind.QuotedIdentifier or JmesTokenKind.Star:
                return Expression(bindingPower);
            case JmesTokenKind.LBracket:
                return MultiSelectList(Advance().Position);
            case JmesTokenKind.LBrace:
                return MultiSelectHash(Advance().Position);
            default:
                throw new JmesPathException(
                    $"Expected a name after '.', not {JmesLexer.Describe(Current)}",
                    Current.Position);
        }
    }

    // After [: expressions separated by commas, then ]
    private JmesNode MultiSelectList(int at)
    {
        List<JmesNode> expressions = [Expression(0)];

        while (Current.Kind == JmesTokenKind.Comma)
        {
            _next++;
            expressions.Add(Expression(0));
        }

        Expect(JmesTokenKind.RBracket);

        return new JmesNode(JmesNodeKind.MultiSelectList, at, expressions);
    }

    // After {: name: expression pairs separated by commas, then }
    private JmesNode MultiSelectHash(int at)
    {
        List<string> keys = [];
        List<JmesNode> values = [];

        do
        {
            if (keys.Count > 0)
            {
                _next++;
            }

            JmesToken key = Current.Kind is JmesTokenKind.UnquotedIdentifier or JmesTokenKind.QuotedIdentifier
                ? Advance()
                : throw new JmesPathException(
                    $"Expected a name in {{ }}, not {JmesLexer.Describe(Current)}",
                    Current.Position);
            Expect(JmesTokenKind.Colon);
            keys.Add(key.Text);
            values.Add(Expression(0));
        }
        while (Current.Kind == JmesTokenKind.Comma);

        Expect(JmesTokenKind.RBrace);

        return new JmesNode(JmesNodeKind.MultiSelectHash, at, values) { Keys = keys };
    }

    private static JmesNode Identity(int at) => new(JmesNodeKind.Identity, at, []);

    private JmesToken Advance() => _tokens[Math.Min(_next++, _tokens.Count - 1)];

    private JmesToken Peek(int ahead) => _tokens[Math.Min(_next + ahead, _tokens.Count - 1)];

    private JmesToken Expect(JmesTokenKind kind)
    {
        if (Current.Kind == JmesTokenKind.End && kind != JmesTokenKind.End)
        {
            throw Unexpected();
        }

        if (Current.Kind != kind)
        {
            throw new JmesPathException(
                $"Expected {JmesLexer.Describe(new JmesToken(kind, 0))}, not {JmesLexer.Describe(Current)}",
                Current.Position);
        }

        return Advance();
    }

    private JmesPathException Unexpected()
    {
        string token = JmesLexer.Describe(Current);

        return new JmesPathException(
            Current.Kind == JmesTokenKind.End
                ? "The expression ends too soon"
                : $"{char.ToUpperInvariant(token[0])}{token[1..]} doesn't go there",
            Current.Position);
    }
}
