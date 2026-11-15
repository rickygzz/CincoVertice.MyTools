using System.Globalization;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Presenters;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Actions;

/// <summary>
///     Opens a new <see cref="DiagramEditorForm"/> with an empty diagram; each one has its own diagram. Diagrams whose
///     changes weren't saved when the app or Windows ended are offered first, each in a window of its own.
/// </summary>
public sealed class OpenDiagramEditorAction : IAppAction
{
    private readonly IDiagramFileStore _fileStore;
    private readonly IDiagramEditorSettingsStore _settingsStore;
    private readonly IDiagramPdfExporter _pdfExporter;
    private readonly IDiagramPngExporter _pngExporter;
    private readonly IDiagramImageCatalog _imageCatalog;
    private readonly IDiagramRecoveryStore _recoveryStore;
    private readonly RobotoFontInstaller _fontInstaller;
    private readonly HelpWindowService _help;

    public OpenDiagramEditorAction(
        IDiagramFileStore fileStore,
        IDiagramEditorSettingsStore settingsStore,
        IDiagramPdfExporter pdfExporter,
        IDiagramPngExporter pngExporter,
        IDiagramImageCatalog imageCatalog,
        IDiagramRecoveryStore recoveryStore,
        RobotoFontInstaller fontInstaller,
        HelpWindowService help)
    {
        _fileStore = fileStore;
        _settingsStore = settingsStore;
        _pdfExporter = pdfExporter;
        _pngExporter = pngExporter;
        _imageCatalog = imageCatalog;
        _recoveryStore = recoveryStore;
        _fontInstaller = fontInstaller;
        _help = help;
    }

    /// <summary>Where the editor keeps the last used style and its layout: config\diagrameditor.json.</summary>
    public static string SettingsFilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "config", "diagrameditor.json");

    /// <summary>Where the images inserted are kept, to insert again: config\images.</summary>
    public static string ImagesFolder { get; } = Path.Combine(AppContext.BaseDirectory, "config", "images");

    /// <summary>Where each window keeps a copy of its unsaved changes: config\recovery.</summary>
    public static string RecoveryFolder { get; } = Path.Combine(AppContext.BaseDirectory, "config", "recovery");

    public string Name => "DiagramEditor";

    public string? DefaultShortcut => "Ctrl+Alt+D";

    public Task ExecuteAsync()
    {
        _fontInstaller.EnsureInstalled();

        IReadOnlyList<DiagramRecoveryModel> left = _recoveryStore.Left();

        if (left.Count > 0)
        {
            switch (AskToRecover(left))
            {
                case DialogResult.Yes:
                    foreach (DiagramRecoveryModel recovery in left)
                    {
                        Open(recovery);
                    }

                    return Task.CompletedTask;
                case DialogResult.No:
                    foreach (DiagramRecoveryModel recovery in left)
                    {
                        _recoveryStore.Delete(recovery.Id);
                    }

                    break;
            }
        }

        Open(null);

        return Task.CompletedTask;
    }

    /// <summary>The text of the question, with each diagram's name and when its copy was made.</summary>
    internal static string RecoverQuestion(IReadOnlyList<DiagramRecoveryModel> left, CultureInfo culture)
    {
        IEnumerable<string> names = left.Select(recovery =>
            $"• {(recovery.Path is null ? DiagramEditorPresenter.UntitledName : Path.GetFileName(recovery.Path))}, "
            + recovery.SavedAt.ToLocalTime().ToString("g", culture));

        return (left.Count == 1
                ? "My Tools or Windows closed before the changes to this diagram were saved:"
                : "My Tools or Windows closed before the changes to these diagrams were saved:")
            + "\n\n" + string.Join("\n", names) + "\n\n"
            + "Open them to get the changes back?\n\n"
            + "Yes: open each in a window, to save it.\nNo: throw the changes away.\nCancel: ask again next time.";
    }

    private static DialogResult AskToRecover(IReadOnlyList<DiagramRecoveryModel> left) => MessageBox.Show(
        RecoverQuestion(left, CultureInfo.CurrentCulture),
        "Diagram editor",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question);

    /// <summary>A new window, with an empty diagram or the one recovered.</summary>
    private void Open(DiagramRecoveryModel? recovery)
    {
        // Disposed itself when closed
        var editor = new DiagramEditorForm();
        _help.OpenOnF1(editor, () => HelpTopics.DiagramEditor);
        var presenter = new DiagramEditorPresenter(
            editor,
            _fileStore,
            _settingsStore,
            _pdfExporter,
            _pngExporter,
            _imageCatalog,
            _recoveryStore);

        presenter.Start();

        if (recovery is not null)
        {
            presenter.Recover(recovery);
        }

        editor.Show();
    }
}
