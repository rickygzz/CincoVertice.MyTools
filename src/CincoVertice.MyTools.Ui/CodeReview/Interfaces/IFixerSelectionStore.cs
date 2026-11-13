namespace CincoVertice.MyTools.Ui.CodeReview.Interfaces;

/// <summary>
///     Where the fixers the user turned off are kept. The ones off, not the ones on, so a fixer added later starts on.
/// </summary>
public interface IFixerSelectionStore
{
    /// <returns>The names of the fixers turned off; none when nothing was saved, or it can't be read.</returns>
    IReadOnlyCollection<string> LoadOff();

    void SaveOff(IReadOnlyCollection<string> names);
}
