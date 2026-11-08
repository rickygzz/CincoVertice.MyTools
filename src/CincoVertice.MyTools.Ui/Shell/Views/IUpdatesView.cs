using CincoVertice.MyTools.Ui.Shell.Models;

namespace CincoVertice.MyTools.Ui.Shell.Views;

/// <summary>The tray menu's About and update items, and what they show.</summary>
public interface IUpdatesView
{
    /// <summary>Raised when the user asks which version this is.</summary>
    event EventHandler? AboutRequested;

    /// <summary>Raised when the user asks to look for a newer version now.</summary>
    event EventHandler? CheckForUpdatesRequested;

    /// <summary>Raised with what the user asked for with Check for updates at start.</summary>
    event EventHandler<bool>? CheckAtStartRequested;

    void ShowCheckAtStart(bool isEnabled);

    /// <summary>Which version this is, and where the releases are.</summary>
    void ShowAbout(Version version, string releasesPageUrl);

    /// <summary>Says a newer version is out, and asks whether to open its page.</summary>
    /// <returns>True to open it.</returns>
    bool AskToOpenRelease(AppReleaseModel release, Version current);

    /// <summary>Says this is the latest version.</summary>
    void ShowUpToDate(Version current);

    /// <summary>Says why it couldn't find out.</summary>
    void ShowUpdateCheckFailed(string reason);

    /// <summary>A notification that a newer version is out, which opens its page when clicked.</summary>
    void NotifyUpdateAvailable(AppReleaseModel release);

    /// <summary>Opens a web page in the browser.</summary>
    void OpenPage(string url);
}
