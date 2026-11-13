using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Presenters;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Actions;

/// <summary>
///     Opens a new <see cref="DiagramEditorForm"/> with an empty diagram; each one has its own diagram.
/// </summary>
public sealed class OpenDiagramEditorAction : IAppAction
{
    private readonly IDiagramFileStore _fileStore;
    private readonly IDiagramEditorSettingsStore _settingsStore;
    private readonly IDiagramPdfExporter _pdfExporter;
    private readonly RobotoFontInstaller _fontInstaller;
    private readonly HelpWindowService _help;

    public OpenDiagramEditorAction(
        IDiagramFileStore fileStore,
        IDiagramEditorSettingsStore settingsStore,
        IDiagramPdfExporter pdfExporter,
        RobotoFontInstaller fontInstaller,
        HelpWindowService help)
    {
        _fileStore = fileStore;
        _settingsStore = settingsStore;
        _pdfExporter = pdfExporter;
        _fontInstaller = fontInstaller;
        _help = help;
    }

    /// <summary>Where the editor keeps the last used style and its layout: config\diagrameditor.json.</summary>
    public static string SettingsFilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "config", "diagrameditor.json");

    public string Name => "DiagramEditor";

    public string? DefaultShortcut => "Ctrl+Alt+D";

    public Task ExecuteAsync()
    {
        _fontInstaller.EnsureInstalled();

        // Disposed itself when closed
        var editor = new DiagramEditorForm();
        _help.OpenOnF1(editor, () => HelpTopics.DiagramEditor);
        var presenter = new DiagramEditorPresenter(editor, _fileStore, _settingsStore, _pdfExporter);

        presenter.Start();
        editor.Show();

        return Task.CompletedTask;
    }
}
