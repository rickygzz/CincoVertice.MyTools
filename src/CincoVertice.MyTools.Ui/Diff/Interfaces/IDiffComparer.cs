using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Diff.Interfaces;

public interface IDiffComparer
{
    Task<DiffResultModel> CompareAsync(
        IReadOnlyList<string> left, IReadOnlyList<string> right, DiffOptionsModel options, CancellationToken cancellation);
}
