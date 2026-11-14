namespace CincoVertice.MyTools.Ui.Shell.Interfaces;

/// <summary>Keeps whether the app looks for a newer version when it starts.</summary>
public interface IUpdateSettingsStore
{
    /// <summary>What was saved last; false when nothing was, or it can't be read.</summary>
    bool LoadCheckAtStart();

    /// <summary>Saves it. A failure only loses it: it never stops the app.</summary>
    void SaveCheckAtStart(bool checkAtStart);
}
