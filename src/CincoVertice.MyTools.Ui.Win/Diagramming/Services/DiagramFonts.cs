using System.Drawing.Text;
using System.Runtime.InteropServices;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     The fonts the diagram editor can use. Roboto is inside the app, so it draws the same whether or not it's
///     installed on this computer: GDI+ reads the installed fonts once, and wouldn't see one installed while it runs.
/// </summary>
public static class DiagramFonts
{
    private const string BundledResourceName = "Roboto.ttf";

    // Kept for as long as the app runs, since the fonts in it are used until then
    private static readonly Lazy<PrivateFontCollection> _bundled = new(LoadBundled);

    /// <summary>The font file inside the app, to install it.</summary>
    public static Stream OpenBundledFont() =>
        typeof(DiagramFonts).Assembly.GetManifestResourceStream(BundledResourceName)
        ?? throw new InvalidOperationException($"{BundledResourceName} isn't in the app.");

    /// <returns>True when Windows has a font of this name.</returns>
    public static bool IsInstalled(string fontFamily)
    {
        using var installed = new InstalledFontCollection();

        return installed.Families.Any(family => family.Name.Equals(fontFamily, StringComparison.OrdinalIgnoreCase));
    }

    /// <returns>The fonts to choose from, in order: the installed ones, and Roboto even if it isn't one of them.</returns>
    public static IReadOnlyList<string> Names()
    {
        using var installed = new InstalledFontCollection();

        return
        [
            .. installed.Families
                .Select(family => family.Name)
                .Append(DiagramStyleModel.DefaultFontFamily)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase),
        ];
    }

    /// <summary>A font sized in pixels; the caller disposes it. A font that isn't on this computer gets a default one.</summary>
    public static Font Create(string fontFamily, float size)
    {
        FontFamily? bundled = _bundled.Value.Families.FirstOrDefault(family =>
            family.Name.Equals(fontFamily, StringComparison.OrdinalIgnoreCase));

        return bundled is null
            ? new Font(fontFamily, size, FontStyle.Regular, GraphicsUnit.Pixel)
            : new Font(bundled, size, FontStyle.Regular, GraphicsUnit.Pixel);
    }

    private static PrivateFontCollection LoadBundled()
    {
        using Stream stream = OpenBundledFont();
        using var buffer = new MemoryStream();

        stream.CopyTo(buffer);

        byte[] font = buffer.ToArray();

        // GDI+ goes on reading this memory, so it's never freed: it holds one font for the whole run
        nint memory = Marshal.AllocCoTaskMem(font.Length);

        Marshal.Copy(font, 0, memory, font.Length);

        var collection = new PrivateFontCollection();

        collection.AddMemoryFont(memory, font.Length);

        return collection;
    }
}
