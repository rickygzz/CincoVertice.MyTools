using CincoVertice.MyTools.Ui.CodeReview.Models;

namespace CincoVertice.MyTools.Ui.CodeReview.Interfaces;

/// <summary>Where the choices for Fix folder are kept: the fixers off, the line ending and the encoding.</summary>
public interface IFixerSelectionStore
{
    /// <returns>The choices saved last; the defaults when nothing was saved, or it can't be read.</returns>
    FixerSettingsModel Load();

    void Save(FixerSettingsModel settings);
}
