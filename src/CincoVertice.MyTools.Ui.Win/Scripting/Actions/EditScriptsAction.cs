using CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Actions;

/// <summary>
///     Opens the first script in the config editor, whose list has the others and can add new ones.
/// </summary>
public sealed class EditScriptsAction : IAppAction
{
    private readonly ConfigEditorWindowService _editor;

    public EditScriptsAction(ConfigEditorWindowService editor)
    {
        _editor = editor;
    }

    public string Name => "EditScripts";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _editor.Open(ScriptCatalog.GetScriptFiles(ScriptCatalog.FolderPath).FirstOrDefault());

        return Task.CompletedTask;
    }
}
