using System.Text;

namespace CincoVertice.MyTools.Ui.Help.Services;

/// <summary>A heading's link name, made as GitHub makes it, so the guide's links work there and here alike.</summary>
public static class MarkdownAnchor
{
    /// <returns>
    ///     The heading's text in lowercase, spaces as hyphens, without punctuation: "Start with Windows" is
    ///     "start-with-windows", "Files (and updates)" is "files-and-updates".
    /// </returns>
    public static string Of(string headingText)
    {
        var anchor = new StringBuilder(headingText.Length);

        foreach (char character in headingText.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_')
            {
                anchor.Append(character);
            }
            else if (character == ' ')
            {
                anchor.Append('-');
            }
        }

        return anchor.ToString();
    }
}
