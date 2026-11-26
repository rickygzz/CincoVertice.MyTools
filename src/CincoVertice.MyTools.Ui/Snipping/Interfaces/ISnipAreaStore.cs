using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Interfaces;

/// <summary>Keeps the area snipped last, in physical screen pixels, for the next snip to start with.</summary>
public interface ISnipAreaStore
{
    /// <summary>The area saved last; null when there's none, or it can't be read.</summary>
    Rectangle? Load();

    /// <summary>Saves the area. A failure only loses it: it never stops the snip.</summary>
    void Save(Rectangle area);
}
