namespace CincoVertice.MyTools.Ui.TextTransform.Models;

/// <summary>What a text transform does to the text selected.</summary>
public enum TextTransformKind
{
    Upper,
    Lower,
    Title,
    TrimLines,
    SortLines,
    RemoveDuplicateLines,
    Base64Encode,
    Base64Decode,
    UrlEncode,
    UrlDecode,
    JsonPrettyPrint,
    JsonMinify,
}
