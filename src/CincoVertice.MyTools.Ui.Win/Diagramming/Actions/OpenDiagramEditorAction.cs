using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Actions;

/// <summary>
///     Opens a new <see cref="DiagramEditorForm"/> with an empty diagram; each one has its own diagram.
/// </summary>
public sealed class OpenDiagramEditorAction : IAppAction
{
    private readonly IDiagramFileStore _fileStore;

    public OpenDiagramEditorAction(IDiagramFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    public string Name => "DiagramEditor";

    public string? DefaultShortcut => "Ctrl+Alt+D";

    public Task ExecuteAsync()
    {
        // Disposed itself when closed
        var editor = new DiagramEditorForm();
        var presenter = new DiagramEditorPresenter(editor, _fileStore);

        presenter.Start();
        editor.Show();

        return Task.CompletedTask;
    }
}
