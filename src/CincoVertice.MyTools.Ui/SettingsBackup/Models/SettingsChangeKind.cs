namespace CincoVertice.MyTools.Ui.SettingsBackup.Models;

/// <summary>What a restore would do to a file of the settings.</summary>
public enum SettingsChangeKind
{
    /// <summary>The backup has it and the settings don't: it's written.</summary>
    Added,

    /// <summary>Both have it, with other contents: the backup's is written over it.</summary>
    Changed,

    /// <summary>Only the settings have it: kept, unless the user asks to delete such files.</summary>
    NotInBackup,

    /// <summary>Both have it, the same: left as it is.</summary>
    Same,
}
