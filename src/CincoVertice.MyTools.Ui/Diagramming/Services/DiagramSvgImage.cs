using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     SVG images placed in a diagram, such as icons. Their markup is made ready once, when it's inserted, so every
///     program that draws it shows the same: Windows' SVG drawing ignores style sheets, so their rules go into each
///     element's own style, and the image is scaled to whatever box it's drawn in.
/// </summary>
public static partial class DiagramSvgImage
{
    /// <summary>The size of an image that says nothing of its own size.</summary>
    public const float DefaultSize = 100;

    // A small icon is made larger and a large picture smaller, so either is easy to see and to place
    private const int SmallestSide = 64;
    private const int LargestSide = 320;

    private static readonly XNamespace _svg = "http://www.w3.org/2000/svg";

    /// <summary>The markup to keep in the diagram: style sheets applied, scripts left out, and scaled to fit.</summary>
    /// <exception cref="InvalidDataException">The text isn't an SVG image.</exception>
    public static string Prepare(string text)
    {
        XElement root = Parse(text);

        foreach (XElement unsafePart in root.Descendants()
                     .Where(element => element.Name.LocalName is "script" or "foreignObject")
                     .ToList())
        {
            unsafePart.Remove();
        }

        foreach (XAttribute handler in root.DescendantsAndSelf()
                     .Attributes()
                     .Where(attribute => attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            handler.Remove();
        }

        InlineStyleSheets(root);

        // Its own size goes into the view box, so it fills the box it's drawn in, keeping its proportions
        if (ViewBoxSize(root) is null
            && Length(root.Attribute("width")) is float width
            && Length(root.Attribute("height")) is float height)
        {
            root.SetAttributeValue("viewBox", $"0 0 {Number(width)} {Number(height)}");
        }

        root.SetAttributeValue("width", null);
        root.SetAttributeValue("height", null);

        return root.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>The image's own size, from its view box.</summary>
    public static SizeF SizeOf(string markup)
    {
        try
        {
            return ViewBoxSize(Parse(markup)) ?? new SizeF(DefaultSize, DefaultSize);
        }
        catch (InvalidDataException)
        {
            return new SizeF(DefaultSize, DefaultSize);
        }
    }

    /// <summary>
    ///     How large to insert an image: its own size, with the longer side made at least 64 and at most 320 pixels.
    /// </summary>
    public static Size InsertSize(string markup)
    {
        SizeF size = SizeOf(markup);
        float longer = Math.Max(size.Width, size.Height);
        float scale = Math.Clamp(longer, SmallestSide, LargestSide) / longer;

        return new Size(
            Math.Max((int)Math.Round(size.Width * scale), 1),
            Math.Max((int)Math.Round(size.Height * scale), 1));
    }

    /// <summary>The image as a link that holds it, for an SVG file's image element.</summary>
    public static string DataUri(string markup) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(markup));

    private static XElement Parse(string text)
    {
        // A file from a drawing program can have a DOCTYPE, which is only skipped: nothing is fetched from elsewhere
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };

        try
        {
            using var reader = XmlReader.Create(new StringReader(text), settings);
            XElement? root = XDocument.Load(reader).Root;

            if (root is not null && root.Name == _svg + "svg")
            {
                return root;
            }
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("This isn't an SVG image.", ex);
        }

        throw new InvalidDataException("This isn't an SVG image.");
    }

    private static SizeF? ViewBoxSize(XElement root)
    {
        string[] parts = ((string?)root.Attribute("viewBox") ?? string.Empty)
            .Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 4
            && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float width)
            && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float height)
            && width > 0
            && height > 0)
        {
            return new SizeF(width, height);
        }

        return null;
    }

    /// <returns>A width or height in pixels; null for none, a percentage or nothing above 0.</returns>
    private static float? Length(XAttribute? attribute)
    {
        string text = ((string?)attribute ?? string.Empty).Trim();

        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^2];
        }

        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value > 0
            ? value
            : null;
    }

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>
    ///     Puts each rule of the style elements into the style of the elements it's for, before what they have of their
    ///     own, then leaves out the style elements. Only simple selectors are known: an element's name, classes and id,
    ///     such as <c>.cls-1</c>, <c>path.a</c> or <c>#logo</c>; rules with others are left out.
    /// </summary>
    private static void InlineStyleSheets(XElement root)
    {
        List<XElement> sheets = [.. root.Descendants().Where(element => element.Name.LocalName == "style")];

        if (sheets.Count == 0)
        {
            return;
        }

        List<(Selector Selector, string Declarations)> rules = [.. sheets.SelectMany(sheet => RulesOf(sheet.Value))];

        foreach (XElement sheet in sheets)
        {
            sheet.Remove();
        }

        foreach (XElement element in root.DescendantsAndSelf())
        {
            string fromSheets = string.Join(
                ";",
                rules.Where(rule => rule.Selector.Matches(element)).Select(rule => rule.Declarations));

            if (fromSheets.Length > 0)
            {
                string? own = (string?)element.Attribute("style");

                element.SetAttributeValue("style", own is null ? fromSheets : $"{fromSheets};{own}");
            }
        }
    }

    private static IEnumerable<(Selector Selector, string Declarations)> RulesOf(string sheet)
    {
        string css = CommentRegex().Replace(sheet, string.Empty);
        int position = 0;

        while (position < css.Length)
        {
            int open = css.IndexOf('{', position);

            if (open < 0)
            {
                yield break;
            }

            string selectors = css[position..open].Trim();
            int close = CloseOf(css, open);
            string body = css[(open + 1)..(close < 0 ? css.Length : close)].Trim().TrimEnd(';');

            position = close < 0 ? css.Length : close + 1;

            // An at-rule, such as @media or @font-face, isn't for the elements
            if (selectors.StartsWith('@') || body.Length == 0)
            {
                continue;
            }

            foreach (string text in selectors.Split(','))
            {
                if (Selector.TryParse(text.Trim()) is Selector selector)
                {
                    yield return (selector, body);
                }
            }
        }
    }

    /// <returns>Where the block opened at the position ends, counting the blocks inside it; -1 if it doesn't.</returns>
    private static int CloseOf(string css, int open)
    {
        int depth = 0;

        for (int i = open; i < css.Length; i++)
        {
            if (css[i] == '{')
            {
                depth++;
            }
            else if (css[i] == '}' && --depth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"^([A-Za-z][\w-]*)?((?:[.#][\w-]+)*)$")]
    private static partial Regex SelectorRegex();

    [GeneratedRegex(@"[.#][\w-]+")]
    private static partial Regex SelectorPartRegex();

    /// <summary>An element name, classes and an id, each of which an element must have.</summary>
    private sealed record Selector(string? Name, IReadOnlyList<string> Classes, string? Id)
    {
        /// <returns>Null for a selector that isn't simple, such as <c>g path</c> or <c>a:hover</c>.</returns>
        public static Selector? TryParse(string text)
        {
            Match match = SelectorRegex().Match(text);

            if (text.Length == 0 || !match.Success)
            {
                return null;
            }

            List<string> classes = [];
            string? id = null;

            foreach (Match part in SelectorPartRegex().Matches(match.Groups[2].Value))
            {
                if (part.Value[0] == '.')
                {
                    classes.Add(part.Value[1..]);
                }
                else
                {
                    id = part.Value[1..];
                }
            }

            return new Selector(match.Groups[1].Success ? match.Groups[1].Value : null, classes, id);
        }

        public bool Matches(XElement element)
        {
            if (Name is not null && element.Name.LocalName != Name)
            {
                return false;
            }

            if (Id is not null && (string?)element.Attribute("id") != Id)
            {
                return false;
            }

            string[] classes = ((string?)element.Attribute("class") ?? string.Empty)
                .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            return Classes.All(classes.Contains);
        }
    }
}
