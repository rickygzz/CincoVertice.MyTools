namespace CincoVertice.MyTools.Ui.JsonEditor.JmesPath;

/// <summary>The kinds of a JMESPath expression's parts, as the specification's grammar names them.</summary>
public enum JmesNodeKind
{
    Identity,
    Current,
    Field,
    Literal,
    Subexpression,
    Index,
    Slice,
    IndexExpression,
    Projection,
    ValueProjection,
    FilterProjection,
    Flatten,
    Pipe,
    Or,
    And,
    Not,
    Comparator,
    MultiSelectList,
    MultiSelectHash,
    FunctionCall,
    ExpRef,
}
