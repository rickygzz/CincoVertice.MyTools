using CincoVertice.MyTools.Ui.Shell.Models;
using CincoVertice.MyTools.Ui.Shell.Services;

namespace CincoVertice.MyTools.Ui.Shell.Interfaces;

/// <summary>Where the app's releases are published.</summary>
public interface IReleaseSource
{
    /// <summary>The page that lists every release.</summary>
    string ReleasesPageUrl { get; }

    /// <summary>The latest release, not counting drafts and pre-releases; null when there's none yet.</summary>
    /// <exception cref="UpdateCheckException">It couldn't be asked, or its answer can't be read.</exception>
    Task<AppReleaseModel?> GetLatestAsync(CancellationToken cancellationToken = default);
}
