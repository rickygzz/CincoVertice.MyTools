namespace CincoVertice.MyTools.Ui.Shell.Models;

/// <summary>A published version of the app, and the page to download it from.</summary>
/// <param name="Version">Its version, as major.minor.patch.</param>
/// <param name="PageUrl">Its release page.</param>
public sealed record AppReleaseModel(Version Version, string PageUrl);
