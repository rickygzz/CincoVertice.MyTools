namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>
///     What hotkeys.cfg asks for: the valid bindings, and a problem per line that was skipped.
/// </summary>
public sealed class HotkeyConfigResultModel
{
    public List<HotkeyBindingModel> Bindings { get; } = [];

    public List<string> Problems { get; } = [];
}
