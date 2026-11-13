namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>Where a link of the user guide goes: a page and a heading in it, or a web page.</summary>
/// <param name="Page">The page's file name; null for the page it's on.</param>
/// <param name="Anchor">The heading's link name; null for the top of the page.</param>
/// <param name="Url">A web page, or another address the shell opens; then the others are null.</param>
public sealed record HelpLinkModel(string? Page, string? Anchor, string? Url = null)
{
    /// <param name="target">The link as written: "02-hotkeys.md#default-hotkeys", "#usage", "https://...".</param>
    public static HelpLinkModel Parse(string target)
    {
        string link = target.Trim();

        if (link.Contains("://", StringComparison.Ordinal)
            || link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return new HelpLinkModel(null, null, link);
        }

        int hash = link.IndexOf('#', StringComparison.Ordinal);
        string path = Uri.UnescapeDataString(hash < 0 ? link : link[..hash]);
        string? anchor = hash < 0 || hash == link.Length - 1 ? null : Uri.UnescapeDataString(link[(hash + 1)..]);

        // The pages are all in one folder: "./02-hotkeys.md" is "02-hotkeys.md"
        string? page = path.Length == 0 ? null : path.Replace('\\', '/').Split('/')[^1];

        return new HelpLinkModel(page, anchor);
    }
}
