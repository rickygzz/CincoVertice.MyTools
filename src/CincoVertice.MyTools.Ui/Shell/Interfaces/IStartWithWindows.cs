namespace CincoVertice.MyTools.Ui.Shell.Interfaces;

/// <summary>Whether the app starts when the user signs in to the computer.</summary>
public interface IStartWithWindows
{
    /// <summary>True when this copy of the app will start at the next sign-in.</summary>
    bool IsEnabled { get; }

    /// <exception cref="IOException">It can't be changed.</exception>
    /// <exception cref="UnauthorizedAccessException">It can't be changed.</exception>
    /// <exception cref="System.Security.SecurityException">It can't be changed.</exception>
    void SetEnabled(bool enabled);
}
