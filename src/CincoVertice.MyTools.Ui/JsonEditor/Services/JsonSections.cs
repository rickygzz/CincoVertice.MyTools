using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>Which section a line is in, for the menu's Collapse section and Expand section.</summary>
public static class JsonSections
{
    /// <returns>The innermost section the line is in, its first and last lines included; null for none.</returns>
    public static JsonFoldModel? SectionAt(IEnumerable<JsonFoldModel> folds, int line) => folds
        .Where(fold => fold.StartLine <= line && line <= fold.EndLine)
        .MaxBy(fold => fold.StartLine);

    /// <returns>
    ///     The innermost section the line is in that's open, for Collapse section: on the first line of a collapsed
    ///     one, the one around it, so collapsing again goes outward. Null for none.
    /// </returns>
    public static JsonFoldModel? OpenSectionAt(
        IEnumerable<JsonFoldModel> folds,
        IReadOnlyCollection<int> collapsed,
        int line) =>
        SectionAt(folds.Where(fold => !collapsed.Contains(fold.StartLine)), line);
}
