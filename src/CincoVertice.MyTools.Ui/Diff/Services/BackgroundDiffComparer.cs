using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Diff.Services;

/// <summary>Coalesces rapid edits, then compares away from the UI thread.</summary>
public sealed class BackgroundDiffComparer : IDiffComparer
{
    public async Task<DiffResultModel> CompareAsync(
        IReadOnlyList<string> left, IReadOnlyList<string> right, DiffOptionsModel options, CancellationToken cancellation)
    {
        await Task.Delay(100, cancellation);
        return await Task.Run(() => TextDiffer.Compare(left, right, options, cancellation), cancellation);
    }
}
