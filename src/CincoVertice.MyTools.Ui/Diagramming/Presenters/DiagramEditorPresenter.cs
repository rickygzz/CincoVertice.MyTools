using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using CincoVertice.MyTools.Ui.ColorPicker.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Diagramming.Views;
using CincoVertice.MyTools.Ui.Snipping.Presenters;

namespace CincoVertice.MyTools.Ui.Diagramming.Presenters;

/// <summary>
///     One diagram editor window: turns what the user does on the view into changes to the diagram, with undo and
///     redo, and opens and saves it through <see cref="IDiagramFileStore"/>. A diagram is immutable: a drag only
///     changes a preview, which becomes the diagram when it ends.
/// </summary>
public sealed class DiagramEditorPresenter
{
    /// <summary>The line widths to choose from, in pixels.</summary>
    public static IReadOnlyList<int> LineWidths { get; } = [1, 2, 3, 4, 5, 6, 8, 10, 12, 16];

    /// <summary>The font sizes to choose from, in pixels.</summary>
    public static IReadOnlyList<int> FontSizes { get; } = [10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 64, 72];

    /// <summary>The name of a diagram that was never saved.</summary>
    public const string UntitledName = "Untitled";

    /// <summary>How many colors used lately the color menus show.</summary>
    public const int MaxRecentColors = 10;

    /// <summary>How many diagrams opened or saved lately the Recent menu lists.</summary>
    public const int MaxRecentFiles = 10;

    private const string RecentColorName = "Used lately";
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4;
    private const float WheelZoomFactor = 1.1f;
    private const float CommandZoomFactor = 1.25f;
    private const int WheelScrollPixels = 40;

    private const int MinShapeSize = 10;
    private const int MaxLineWidth = 100;
    private const int MinFontSize = 6;
    private const int MaxFontSize = 400;
    private const int PasteOffset = 20;

    // The images panel's group of the images used last, and how many it shows
    private const string RecentGroup = "Recent";
    private const int RecentImages = 12;

    // What SVG text pasted is called in the catalog
    private const string PastedImageName = "pasted.svg";

    // All in canvas pixels, so they feel the same at any zoom
    private const float SnapDistance = 6;
    private const float GrabDistance = 6;
    private const float DragThreshold = 3;

    // Outside the edges, so they don't cover the scale handles on the same edges of a selected shape
    private const float AnchorOffset = 14;
    private const float AnchorGrabDistance = 8;

    // The edge's width when picking its color turns it on
    private const int TurnedOnTextOutlineWidth = 2;

    private static readonly Size _defaultShapeSize = new(120, 60);

    private readonly IDiagramEditorView _view;
    private readonly IDiagramFileStore _fileStore;
    private readonly IDiagramPdfExporter _pdfExporter;
    private readonly IDiagramPngExporter _pngExporter;
    private readonly IDiagramImageCatalog _imageCatalog;
    private readonly IDiagramEditorSettingsStore _settingsStore;
    private readonly IDiagramRecoveryStore _recoveryStore;
    private readonly IScreenColorPicker _screenColorPicker;

    // Colors used lately that the menus don't offer otherwise, newest first, kept in the settings
    private readonly List<Color> _recentColors = [];

    // Diagrams opened or saved lately, newest first, kept in the settings for every window
    private List<string> _recentFiles = [];
    private readonly Stack<DiagramModel> _undo = new();
    private readonly Stack<DiagramModel> _redo = new();
    private readonly HashSet<int> _selectedShapes = [];
    private readonly HashSet<int> _selectedConnectors = [];

    private DiagramModel _diagram = DiagramModel.Empty;
    private DiagramModel _savedDiagram = DiagramModel.Empty;
    private string? _path;
    private string? _savedVersion;
    private DiagramViewportModel _viewport = new(1, Point.Empty);
    private DiagramTool _tool = DiagramTool.Select;

    // What new shapes and connectors get
    private DiagramStyleModel _style = new(
        Color.FromArgb(255, 255, 255),
        Color.FromArgb(64, 64, 64),
        2,
        false,
        Color.FromArgb(0, 0, 0),
        14);

    // Kept by Copy style, only in this window. A connector's style has no fill to give
    private DiagramStyleModel? _copiedStyle;
    private bool _copiedStyleHasFill;

    private DiagramRouting _routing = DiagramRouting.Elbow;
    private DiagramArrowHeads _heads = DiagramArrowHeads.End;
    // The side panel's tabs: one at most is shown
    private bool _isPropertiesPanelVisible = true;
    private bool _isImagesPanelVisible;
    private bool _isLayersPanelVisible;

    // What the layers panel shows, so it's told only of changes; null until it's told
    private IReadOnlyList<DiagramLayerModel>? _shownLayers;
    private HashSet<int>? _shownLayerSelection;

    // What the images panel shows, and what it shows only the images of
    private IReadOnlyList<DiagramCatalogImageModel> _shownCatalog = [];

    // Null until the panel has been told what to show
    private string? _shownCatalogEmptyText;
    private string _imageSearch = string.Empty;
    private DiagramWindowSizeModel? _windowSize;

    // The drag in progress and what it shows
    private Drag? _drag;
    private DiagramModel? _preview;
    private IReadOnlyList<DiagramGuideModel> _guides = [];
    private Rectangle? _band;
    private (Point From, Point To)? _draftConnector;
    private int? _connectTarget;
    private DiagramSide? _connectTargetSide;

    // The shape showing its circles, and the circle under the mouse
    private int? _hoverShape;
    private DiagramSide? _hoverSide;

    private DiagramPropertiesModel? _shownProperties;

    // What ends the text being typed
    private Action<DiagramRichTextModel>? _commitText;

    private DiagramCursor _cursor = DiagramCursor.Default;
    private Point _lastPointer;
    private bool _isPointerOnCanvas;
    private int _pasteCount;

    // The status shown under the canvas, so it's sent only when it changes
    private (string Pointer, string Selection)? _shownStatus;

    // What was looked for last, and which of what has it was found last
    private string _findText = string.Empty;

    // The theme chosen last, which a new diagram gets; and what the color menus show, so they're told only of changes
    private string? _themeForNew;
    private string? _shownPalette;
    private int _findIndex = -1;

    // This window's copy of its unsaved changes, and what the copy has, so it's written only when that changes
    private string _recoveryId;
    private (DiagramModel Diagram, bool HasChanges)? _autosaved;

    // Set once the user said what to do with the changes as the window closes: then the copy isn't needed
    private bool _isLeaveConfirmed;

    /// <param name="view">The window.</param>
    /// <param name="fileStore">Where diagrams are opened from and saved to.</param>
    /// <param name="settingsStore">
    ///     Where the last used style and the window's layout are kept: the editor starts with the ones saved last, and
    ///     saves them on every change.
    /// </param>
    /// <param name="pdfExporter">Writes the diagram as a PDF file.</param>
    /// <param name="pngExporter">Writes the diagram as a PNG picture.</param>
    /// <param name="imageCatalog">Keeps the images inserted, and those the user adds, to insert again.</param>
    /// <param name="screenColorPicker">Lets the user pick a color from the screen for a color menu.</param>
    /// <param name="recoveryStore">
    ///     Keeps a copy of the diagram while it has changes that weren't saved, to get them back after the app or
    ///     Windows ends.
    /// </param>
    public DiagramEditorPresenter(
        IDiagramEditorView view,
        IDiagramFileStore fileStore,
        IDiagramEditorSettingsStore settingsStore,
        IDiagramPdfExporter pdfExporter,
        IDiagramPngExporter pngExporter,
        IDiagramImageCatalog imageCatalog,
        IDiagramRecoveryStore recoveryStore,
        IScreenColorPicker screenColorPicker)
    {
        _recoveryStore = recoveryStore;
        _screenColorPicker = screenColorPicker;
        _recoveryId = recoveryStore.NewId();
        _imageCatalog = imageCatalog;
        _view = view;
        _fileStore = fileStore;
        _settingsStore = settingsStore;
        _pdfExporter = pdfExporter;
        _pngExporter = pngExporter;

        Apply(settingsStore.Load());

        // A new diagram has the theme chosen last
        _diagram = DiagramModel.Empty with { Theme = _themeForNew };
        _savedDiagram = _diagram;

        _view.PointerPressed += OnPointerPressed;
        _view.PointerMoved += OnPointerMoved;
        _view.PointerReleased += OnPointerReleased;
        _view.PointerDoubleClicked += OnPointerDoubleClicked;
        _view.PointerRightClicked += OnPointerRightClicked;
        _view.PointerLeft += OnPointerLeft;
        _view.WheelTurned += OnWheelTurned;
        _view.ToolSelected += OnToolSelected;
        _view.CommandRequested += OnCommandRequested;
        _view.NudgeRequested += OnNudgeRequested;
        _view.ColorSelected += OnColorSelected;
        _view.MoreColorsRequested += OnMoreColorsRequested;
        _view.ScreenColorRequested += OnScreenColorRequested;
        _view.ThemeSelected += OnThemeSelected;
        _view.RecentChosen += OnRecentChosen;
        _view.LineWidthSelected += OnLineWidthSelected;
        _view.DashedSelected += OnDashedSelected;
        _view.FontSizeSelected += OnFontSizeSelected;
        _view.FontFamilySelected += OnFontFamilySelected;
        _view.TextOutlineWidthSelected += OnTextOutlineWidthSelected;
        _view.TextAlignmentSelected += OnTextAlignmentSelected;
        _view.RoutingSelected += OnRoutingSelected;
        _view.ArrowHeadsSelected += OnArrowHeadsSelected;
        _view.LineDirectionSelected += OnLineDirectionSelected;
        _view.FilesDropped += OnFilesDropped;
        _view.BoundsEdited += OnBoundsEdited;
        _view.CornerRadiusEdited += OnCornerRadiusEdited;
        _view.BottomCornerRadiusEdited += OnBottomCornerRadiusEdited;
        _view.LockToggled += OnLockToggled;
        _view.TextEdited += OnTextEdited;
        _view.NameEdited += OnNameEdited;
        _view.TextCommitted += OnTextCommitted;
        _view.Closing += OnClosing;
        _view.PropertiesPanelToggled += OnPropertiesPanelToggled;
        _view.ImagesPanelToggled += OnImagesPanelToggled;
        _view.LayersPanelToggled += OnLayersPanelToggled;
        _view.LayersSelected += OnLayersSelected;
        _view.LayerRenamed += OnLayerRenamed;
        _view.LayersMoved += OnLayersMoved;
        _view.LayerHiddenToggled += OnLayerHiddenToggled;
        _view.LayerLockedToggled += OnLayerLockedToggled;
        _view.ImageSearchChanged += OnImageSearchChanged;
        _view.CatalogImageChosen += OnCatalogImageChosen;
        _view.CatalogImageDropped += OnCatalogImageDropped;
        _view.ImagesFolderRequested += OnImagesFolderRequested;
        _view.FindRequested += OnFindRequested;
        _view.AutosaveDue += OnAutosaveDue;
        _view.WindowActivated += OnWindowActivated;
        _view.WindowClosed += OnWindowClosed;
    }

    /// <summary>The diagram, without the change being dragged.</summary>
    public DiagramModel Diagram => _diagram;

    public IReadOnlySet<int> SelectedShapeIds => _selectedShapes;

    public IReadOnlySet<int> SelectedConnectorIds => _selectedConnectors;

    private DiagramModel Shown => _preview ?? _diagram;

    private bool HasChanges => !ReferenceEquals(_diagram, _savedDiagram);

    private string Name => _path is null ? UntitledName : Path.GetFileName(_path);

    /// <summary>Shows the choices and the empty diagram.</summary>
    public void Start()
    {
        ShowPalette();
        ShowRecent();
        _view.ShowSizes(LineWidths, FontSizes, SnipEditorPresenter.TextOutlineWidths);
        _view.ShowPropertiesPanel(_isPropertiesPanelVisible);
        _view.ShowImagesPanel(_isImagesPanelVisible);
        _view.ShowLayersPanel(_isLayersPanelVisible);
        ShowCatalog();

        if (_windowSize is not null)
        {
            _view.ShowWindowSize(_windowSize);
        }

        _view.ShowTool(_tool);
        ShowSelectedStyle();
        Refresh();
    }

    /// <summary>
    ///     Takes the saved settings that can still be used: a routing, a head or a window size the editor can't use
    ///     (e.g. from a file edited by hand) keeps its default.
    /// </summary>
    private void Apply(DiagramEditorSettingsModel? saved)
    {
        if (saved is null)
        {
            return;
        }

        _style = saved.Style;
        // From before the panels were tabs, several can be on: properties first, then images
        _isPropertiesPanelVisible = saved.IsPropertiesPanelVisible;
        _isImagesPanelVisible = saved.IsImagesPanelVisible && !_isPropertiesPanelVisible;
        _isLayersPanelVisible = saved.IsLayersPanelVisible && !_isPropertiesPanelVisible && !_isImagesPanelVisible;

        if (Enum.IsDefined(saved.Routing))
        {
            _routing = saved.Routing;
        }

        if (Enum.IsDefined(saved.Heads))
        {
            _heads = saved.Heads;
        }

        _themeForNew = saved.Theme;
        _recentFiles = [.. (saved.RecentFiles ?? []).Take(MaxRecentFiles)];
        _recentColors.AddRange((saved.RecentColors ?? []).Where(color => !color.IsEmpty).Take(MaxRecentColors));

        if (saved.Window is { Size: { Width: > 0, Height: > 0 } })
        {
            _windowSize = saved.Window;
        }
    }

    /// <param name="withRecentFiles">
    ///     True to write this window's recent diagrams; else the ones saved are kept, as another window may have
    ///     changed them since.
    /// </param>
    private void SaveSettings(bool withRecentFiles = false)
    {
        IReadOnlyList<string> recentFiles = withRecentFiles
            ? _recentFiles
            : _settingsStore.Load()?.RecentFiles ?? _recentFiles;

        _settingsStore.Save(new DiagramEditorSettingsModel(
            _style,
            _routing,
            _heads,
            _isPropertiesPanelVisible,
            _windowSize,
            _isImagesPanelVisible,
            _isLayersPanelVisible,
            _themeForNew,
            [.. _recentColors],
            [.. recentFiles]));
    }

    private void OnPropertiesPanelToggled(object? sender, bool isVisible)
    {
        _isPropertiesPanelVisible = isVisible;
        SaveSettings();
    }

    private void OnImagesPanelToggled(object? sender, bool isVisible)
    {
        _isImagesPanelVisible = isVisible;
        SaveSettings();
        ShowCatalog();
    }

    private void OnLayersPanelToggled(object? sender, bool isVisible)
    {
        _isLayersPanelVisible = isVisible;
        _shownLayers = null;
        _shownLayerSelection = null;
        SaveSettings();
        Refresh();
    }

    /// <summary>
    ///     The layers panel's rows and which are selected, when they changed: the diagram, not a drag's preview, so the
    ///     list doesn't change on every mouse move.
    /// </summary>
    private void ShowLayers()
    {
        if (!_isLayersPanelVisible)
        {
            return;
        }

        List<DiagramLayerModel> layers = LayersOf(_diagram);

        if (_shownLayers is null || !_shownLayers.SequenceEqual(layers))
        {
            _shownLayers = layers;
            _shownLayerSelection = null;
            _view.ShowLayers(layers);
        }

        HashSet<int> selected = [.. _selectedShapes, .. _selectedConnectors];

        if (_shownLayerSelection is null || !_shownLayerSelection.SetEquals(selected))
        {
            _shownLayerSelection = selected;
            _view.ShowLayerSelection(selected);
        }
    }

    /// <summary>Front first: connectors and lines, drawn over every shape, then the shapes, last drawn first.</summary>
    private static List<DiagramLayerModel> LayersOf(DiagramModel diagram) =>
    [
        .. diagram.Connectors.Reverse().Select(connector => new DiagramLayerModel(
            connector.Id,
            LayerText(connector, diagram),
            connector.Name ?? string.Empty,
            true,
            IsHidden: connector.IsHidden,
            IsLocked: connector.IsLocked)),
        .. diagram.Shapes.Reverse().Select(shape => new DiagramLayerModel(
            shape.Id,
            LayerText(shape),
            shape.Name ?? string.Empty,
            false,
            shape.GroupId,
            shape.IsHidden,
            shape.IsLocked)),
    ];

    /// <summary>Its name, or else its kind and the start of its text, e.g. "Rectangle: Start".</summary>
    private static string LayerText(DiagramShapeModel shape)
    {
        string kind = shape.Kind switch
        {
            DiagramShapeKind.RoundedRectangle => "Rounded rectangle",
            _ => shape.Kind.ToString(),
        };

        return shape.Name ?? WithExcerpt(kind, shape.Text);
    }

    /// <summary>Its name, or else "Line" or "Connector" and its label, or the shapes it joins.</summary>
    private static string LayerText(DiagramConnectorModel connector, DiagramModel diagram)
    {
        if (connector.Name is string name)
        {
            return name;
        }

        if (connector.IsLine || connector.Label.Length > 0)
        {
            return WithExcerpt(connector.IsLine ? "Line" : "Connector", connector.Label);
        }

        string Of(int? shapeId) => diagram.Shapes.FirstOrDefault(shape => shape.Id == shapeId) is { } shape
            ? Excerpt(shape.Name ?? shape.Text)
            : string.Empty;

        string from = Of(connector.FromShapeId);
        string to = Of(connector.ToShapeId);

        return from.Length > 0 || to.Length > 0 ? $"Connector: {from} → {to}" : "Connector";
    }

    private static string WithExcerpt(string kind, string text) =>
        Excerpt(text) is { Length: > 0 } excerpt ? $"{kind}: {excerpt}" : kind;

    /// <summary>The first line of a text, cut short when it's long.</summary>
    private static string Excerpt(string text)
    {
        const int longest = 30;
        string line = text.ReplaceLineEndings("\n").Split('\n')[0].Trim();

        return line.Length <= longest ? line : line[..(longest - 1)].TrimEnd() + "…";
    }

    /// <summary>Selects exactly the rows chosen, even one shape of a group, which a click on the canvas can't.</summary>
    private void OnLayersSelected(object? sender, IReadOnlyList<int> ids)
    {
        if (_drag is not null)
        {
            return;
        }

        // What's hidden can't be selected, even here; what's locked can, to change it
        ClearSelection();
        _selectedShapes.UnionWith(_diagram.Shapes
            .Where(shape => ids.Contains(shape.Id) && !shape.IsHidden)
            .Select(shape => shape.Id));
        _selectedConnectors.UnionWith(_diagram.Connectors
            .Where(connector => ids.Contains(connector.Id) && !connector.IsHidden)
            .Select(connector => connector.Id));

        // The panel shows it already
        _shownLayerSelection = [.. _selectedShapes, .. _selectedConnectors];
        SelectionChanged();
        Refresh();
    }

    /// <summary>The name of the one thing selected, typed in the properties panel.</summary>
    private void OnNameEdited(object? sender, string name)
    {
        if (_drag is null && _selectedShapes.Count + _selectedConnectors.Count == 1)
        {
            Rename(_selectedShapes.Concat(_selectedConnectors).Single(), name);
        }
    }

    private void OnLayerRenamed(object? sender, DiagramLayerRenameModel rename) => Rename(rename.Id, rename.Name);

    /// <summary>Shows or hides a shape or connector; one hidden is no longer selected. One step to undo.</summary>
    private void OnLayerHiddenToggled(object? sender, int id)
    {
        if (_drag is not null)
        {
            return;
        }

        Commit(_diagram with
        {
            Shapes =
            [
                .. _diagram.Shapes.Select(shape => shape.Id == id ? shape with { IsHidden = !shape.IsHidden } : shape),
            ],
            Connectors =
            [
                .. _diagram.Connectors.Select(connector =>
                    connector.Id == id ? connector with { IsHidden = !connector.IsHidden } : connector),
            ],
        });

        if (_selectedShapes.Remove(id) | _selectedConnectors.Remove(id))
        {
            SelectionChanged();
            Refresh();
        }
    }

    /// <summary>Locks or frees a shape or connector, which canvas clicks then go through. One undo step.</summary>
    private void OnLayerLockedToggled(object? sender, int id)
    {
        if (_drag is not null)
        {
            return;
        }

        Commit(_diagram with
        {
            Shapes =
            [
                .. _diagram.Shapes.Select(shape => shape.Id == id ? shape with { IsLocked = !shape.IsLocked } : shape),
            ],
            Connectors =
            [
                .. _diagram.Connectors.Select(connector =>
                    connector.Id == id ? connector with { IsLocked = !connector.IsLocked } : connector),
            ],
        });
    }

    /// <summary>Names a shape or a connector; a name of only spaces is none: its kind and text show again.</summary>
    private void Rename(int id, string typed)
    {
        string? name = string.IsNullOrWhiteSpace(typed) ? null : typed.Trim();

        Commit(
            _diagram with
            {
                Shapes =
                [
                    .. _diagram.Shapes.Select(shape => shape.Id == id ? shape with { Name = name } : shape),
                ],
                Connectors =
                [
                    .. _diagram.Connectors.Select(connector =>
                        connector.Id == id ? connector with { Name = name } : connector),
                ],
            },
            onlyIfChanged: true);
    }

    /// <summary>
    ///     Puts the rows dragged above the one they were dropped on, in the order they had, as one step to undo. Shapes
    ///     stay under the connectors and lines whatever the drop: a shape dropped among them goes in front of the other
    ///     shapes.
    /// </summary>
    private void OnLayersMoved(object? sender, DiagramLayerMoveModel move)
    {
        if (_drag is not null)
        {
            return;
        }

        List<int> shown = [.. LayersOf(_diagram).Select(layer => layer.Id)];
        HashSet<int> moving = [.. move.Ids.Where(shown.Contains)];

        // Dropped on one of the rows dragged: above the first row after it that stays
        int start = move.BeforeId is int id ? shown.IndexOf(id) : -1;
        int? before = start < 0
            ? null
            : shown.Skip(start).Where(other => !moving.Contains(other)).Cast<int?>().FirstOrDefault();
        List<int> order = [.. shown.Where(other => !moving.Contains(other))];
        int at = before is int target ? order.IndexOf(target) : order.Count;

        order.InsertRange(at, shown.Where(moving.Contains));

        Dictionary<int, DiagramShapeModel> shapes = _diagram.Shapes.ToDictionary(shape => shape.Id);
        Dictionary<int, DiagramConnectorModel> connectors = _diagram.Connectors.ToDictionary(c => c.Id);
        List<int> backFirst = [.. Enumerable.Reverse(order)];

        Commit(
            new DiagramModel(
                [.. backFirst.Where(shapes.ContainsKey).Select(shapeId => shapes[shapeId])],
                [.. backFirst.Where(connectors.ContainsKey).Select(connectorId => connectors[connectorId])]),
            onlyIfChanged: true);
    }

    private void OnImageSearchChanged(object? sender, string search)
    {
        _imageSearch = search.Trim();
        ShowCatalog();
    }

    /// <summary>Images copied into the catalog's folder show when the user comes back to the window.</summary>
    private void OnWindowActivated(object? sender, EventArgs e)
    {
        ShowCatalog();

        // Another window may have opened or saved one since
        _recentFiles = [.. (_settingsStore.Load()?.RecentFiles ?? _recentFiles).Take(MaxRecentFiles)];
        ShowRecent();
    }

    private void OnImagesFolderRequested(object? sender, EventArgs e)
    {
        try
        {
            _imageCatalog.CreateFolder();
            _view.OpenFolder(_imageCatalog.Folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowError($"Could not open {_imageCatalog.Folder}: {ex.Message}");
        }
    }

    private void OnCatalogImageChosen(object? sender, string path) =>
        InsertFromCatalog(path, _viewport.ToDiagram(_lastPointer));

    private void OnCatalogImageDropped(object? sender, DiagramCatalogDropModel drop) =>
        InsertFromCatalog(drop.Path, _viewport.ToDiagram(drop.Location));

    private void InsertFromCatalog(string path, Point at)
    {
        if (_drag is not null || _shownCatalog.FirstOrDefault(image => image.Path == path) is not { } image)
        {
            return;
        }

        InsertImageShapes([image.Markup], at);
        _imageCatalog.MarkUsed(path);
        ShowCatalog();
    }

    /// <summary>
    ///     What the images panel shows: the ones used last, then all in their groups; or, while searching, those
    ///     whose name or group has the text. Shown again only when it changed, so the panel keeps its place.
    /// </summary>
    private void ShowCatalog()
    {
        if (!_isImagesPanelVisible)
        {
            return;
        }

        IReadOnlyList<DiagramCatalogImageModel> all = _imageCatalog.Images();
        List<DiagramCatalogImageModel> shown;

        if (_imageSearch.Length == 0)
        {
            Dictionary<string, DiagramCatalogImageModel> byPath = all.ToDictionary(
                image => image.Path,
                StringComparer.OrdinalIgnoreCase);

            shown =
            [
                .. _imageCatalog.Recent()
                    .Where(byPath.ContainsKey)
                    .Take(RecentImages)
                    .Select(path => byPath[path] with { Group = RecentGroup }),
                .. all,
            ];
        }
        else
        {
            shown =
            [
                .. all.Where(image => image.Name.Contains(_imageSearch, StringComparison.OrdinalIgnoreCase)
                    || image.Group.Contains(_imageSearch, StringComparison.OrdinalIgnoreCase)),
            ];
        }

        string emptyText = _imageSearch.Length > 0
            ? $"No image has \"{_imageSearch}\"."
            : "Images you insert show here, to insert again. You can also copy SVG files into the folder.";

        if (_shownCatalogEmptyText != emptyText || !shown.SequenceEqual(_shownCatalog))
        {
            _shownCatalog = shown;
            _shownCatalogEmptyText = emptyText;
            _view.ShowCatalog(shown, emptyText);
        }
    }

    private void OnWindowClosed(object? sender, DiagramWindowSizeModel size)
    {
        _windowSize = size;
        SaveSettings();

        // Closed without asking, as when Windows shuts down: the copy stays, up to date, to get the changes back
        if (_isLeaveConfirmed || !HasChanges)
        {
            _recoveryStore.Delete(_recoveryId);
        }
        else
        {
            Autosave();
        }
    }

    /// <summary>
    ///     Opens a copy of a diagram whose changes weren't saved when the app or Windows ended, still with changes to
    ///     save, to the file it had. Call it after <see cref="Start"/>, in place of the empty diagram.
    /// </summary>
    public void Recover(DiagramRecoveryModel recovery)
    {
        _recoveryStore.Adopt(recovery.Id);
        _recoveryStore.Delete(_recoveryId);
        _recoveryId = recovery.Id;
        Replace(recovery.Diagram, recovery.Path);

        // Its changes aren't saved; and a file changed since the copy was made is asked about before it's replaced
        _savedDiagram = DiagramModel.Empty;
        _savedVersion = recovery.Version;
        _autosaved = (_diagram, true);

        if (recovery.Path is string path)
        {
            RememberFile(path);
        }

        ShowRecent();
        Refresh();
    }

    /// <summary>Puts a diagram first among those opened lately, as the other windows saved them.</summary>
    private void RememberFile(string path)
    {
        List<string> files = [.. _settingsStore.Load()?.RecentFiles ?? _recentFiles];

        files.RemoveAll(other => string.Equals(other, path, StringComparison.OrdinalIgnoreCase));
        files.Insert(0, path);
        _recentFiles = [.. files.Take(MaxRecentFiles)];
        SaveSettings(withRecentFiles: true);
        ShowRecent();
    }

    /// <summary>
    ///     Unsaved changes no window has, left when the app or Windows ended, then the diagrams opened or saved lately.
    /// </summary>
    private void ShowRecent()
    {
        IEnumerable<DiagramRecentModel> unsaved = _recoveryStore.Left().Select(recovery => new DiagramRecentModel(
            string.Create(
                CultureInfo.CurrentCulture,
                $"Unsaved: {(recovery.Path is null ? UntitledName : Path.GetFileName(recovery.Path))}, "
                + $"{recovery.SavedAt.ToLocalTime():g}"),
            null,
            recovery.Id));
        IEnumerable<DiagramRecentModel> files = _recentFiles.Select(path => new DiagramRecentModel(
            $"{Path.GetFileName(path)}  —  {Path.GetDirectoryName(path)}",
            path,
            null));

        _view.ShowRecent([.. unsaved, .. files]);
    }

    /// <summary>
    ///     Opens it here, after asking about this one's changes: a diagram from its file, or unsaved changes. One that
    ///     can't be opened any more leaves the list.
    /// </summary>
    private void OnRecentChosen(object? sender, DiagramRecentModel item)
    {
        if (_drag is not null)
        {
            return;
        }

        if (item.RecoveryId is string id)
        {
            // Another window may have taken them since the menu was shown
            if (_recoveryStore.Left().FirstOrDefault(recovery => recovery.Id == id) is { } recovery && ConfirmLeave())
            {
                Recover(recovery);
            }

            ShowRecent();

            return;
        }

        if (item.Path is string path && ConfirmLeave() && !OpenFile(path) && _fileStore.VersionOf(path) is null)
        {
            _recentFiles = [.. (_settingsStore.Load()?.RecentFiles ?? _recentFiles).Where(other => other != path)];
            SaveSettings(withRecentFiles: true);
            ShowRecent();
        }
    }

    private void OnAutosaveDue(object? sender, EventArgs e)
    {
        Autosave();
    }

    /// <summary>Keeps a copy of the diagram while it has unsaved changes, and none once it hasn't.</summary>
    private void Autosave()
    {
        (DiagramModel Diagram, bool HasChanges) state = (_diagram, HasChanges);

        if (_autosaved is { } saved
            && ReferenceEquals(saved.Diagram, state.Diagram)
            && saved.HasChanges == state.HasChanges)
        {
            return;
        }

        if (!state.HasChanges)
        {
            _recoveryStore.Delete(_recoveryId);
            _autosaved = state;

            return;
        }

        try
        {
            _recoveryStore.Save(
                new DiagramRecoveryModel(_recoveryId, _path, _savedVersion, _diagram, DateTime.UtcNow));
            _autosaved = state;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Tried again the next time; the diagram itself is still in the window
        }
    }

    private static Rectangle FromCorners(Point a, Point b) => Rectangle.FromLTRB(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Max(a.X, b.X),
        Math.Max(a.Y, b.Y));

    private static Rectangle UnionOf(IEnumerable<Rectangle> boxes) => boxes.Aggregate(Rectangle.Union);

    private static Rectangle Moved(Rectangle box, Size delta) => new(box.Location + delta, box.Size);

    private static Point CenterOf(Rectangle box) => new(box.Left + box.Width / 2, box.Top + box.Height / 2);

    /// <summary>The box after one of its handles is dragged to the point, never smaller than a shape can be.</summary>
    private static Rectangle ScaleBox(Rectangle box, DiagramHandle handle, Point point, bool keepProportions)
    {
        int left = box.Left;
        int top = box.Top;
        int right = box.Right;
        int bottom = box.Bottom;

        if (handle is DiagramHandle.TopLeft or DiagramHandle.Left or DiagramHandle.BottomLeft)
        {
            left = Math.Min(point.X, right - MinShapeSize);
        }

        if (handle is DiagramHandle.TopRight or DiagramHandle.Right or DiagramHandle.BottomRight)
        {
            right = Math.Max(point.X, left + MinShapeSize);
        }

        if (handle is DiagramHandle.TopLeft or DiagramHandle.Top or DiagramHandle.TopRight)
        {
            top = Math.Min(point.Y, bottom - MinShapeSize);
        }

        if (handle is DiagramHandle.BottomLeft or DiagramHandle.Bottom or DiagramHandle.BottomRight)
        {
            bottom = Math.Max(point.Y, top + MinShapeSize);
        }

        bool isCorner = handle is DiagramHandle.TopLeft or DiagramHandle.TopRight
            or DiagramHandle.BottomRight or DiagramHandle.BottomLeft;

        if (!keepProportions || !isCorner)
        {
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        double scale = Math.Max((right - left) / (double)box.Width, (bottom - top) / (double)box.Height);
        int width = (int)Math.Round(box.Width * scale);
        int height = (int)Math.Round(box.Height * scale);

        // The opposite corner stays where it is
        return new Rectangle(
            left == box.Left ? box.Left : box.Right - width,
            top == box.Top ? box.Top : box.Bottom - height,
            width,
            height);
    }

    /// <summary>Where a shape goes when the box around it changes from one to the other.</summary>
    private static Rectangle ScaleInside(Rectangle shape, Rectangle from, Rectangle to)
    {
        double scaleX = to.Width / (double)from.Width;
        double scaleY = to.Height / (double)from.Height;

        return new Rectangle(
            to.Left + (int)Math.Round((shape.Left - from.Left) * scaleX),
            to.Top + (int)Math.Round((shape.Top - from.Top) * scaleY),
            Math.Max(1, (int)Math.Round(shape.Width * scaleX)),
            Math.Max(1, (int)Math.Round(shape.Height * scaleY)));
    }

    private static DiagramCursor CursorOf(DiagramHandle handle) => handle switch
    {
        DiagramHandle.Top or DiagramHandle.Bottom => DiagramCursor.SizeVertical,
        DiagramHandle.Left or DiagramHandle.Right => DiagramCursor.SizeHorizontal,
        DiagramHandle.TopLeft or DiagramHandle.BottomRight => DiagramCursor.SizeTopLeftBottomRight,
        _ => DiagramCursor.SizeTopRightBottomLeft,
    };

    private static bool IsVerticalGuide(DiagramGuideModel guide) => guide.From.X == guide.To.X;

    private void OnPointerPressed(object? sender, DiagramPointerModel pointer)
    {
        _lastPointer = pointer.Location;

        if (_drag is not null)
        {
            return;
        }

        if (pointer.Pan)
        {
            _drag = new PanDrag(pointer.Location);
            ShowCursor(DiagramCursor.Pan);

            return;
        }

        Point point = _viewport.ToDiagram(pointer.Location);
        (DiagramShapeModel? anchorShape, DiagramSide? anchorSide) = AnchorUnder(point);
        (DiagramConnectorModel? endConnector, DiagramConnectorEnd? connectorEnd) = ConnectorEndUnder(point);

        ClearHover();

        // Before the circles: the end of a selected connector is on a shape, right next to them
        if (endConnector is not null && connectorEnd is DiagramConnectorEnd end)
        {
            // A line's end goes anywhere; a connector's goes to another shape
            _drag = endConnector.IsLine
                ? new LineEndDrag(point, endConnector.Id, end)
                : new ReconnectDrag(point, endConnector.Id, end);
            Refresh();

            return;
        }

        // With Shift or Ctrl, a press on a selected connector takes it off the selection instead
        if (!pointer.Shift && !pointer.Control && SegmentDragAt(point) is { } segmentDrag)
        {
            _drag = segmentDrag;
            Refresh();

            return;
        }

        if (anchorShape is not null && anchorSide is DiagramSide side)
        {
            _drag = new ConnectDrag(anchorShape.Id, side);
            _draftConnector = (Point.Round(DiagramRouter.AnchorPoint(anchorShape, side)), point);
            Refresh();

            return;
        }

        switch (_tool)
        {
            case DiagramTool.Select:
                PressWithSelect(pointer, point);
                break;
            case DiagramTool.Connector:
                PressWithConnector(point);
                break;
            case DiagramTool.Line:
                _drag = new LineDrag(pointer.Alt ? point : Snap(point));
                break;
            case DiagramTool.Text:
                SelectTool(DiagramTool.Select);
                BeginNewText(point);
                break;
            default:
                _drag = new CreateDrag(
                    Enum.Parse<DiagramShapeKind>(_tool.ToString()),
                    pointer.Alt ? point : Snap(point));
                break;
        }

        Refresh();
    }

    private void PressWithSelect(DiagramPointerModel pointer, Point point)
    {
        bool isAdding = pointer.Shift || pointer.Control;

        // Even with Shift held, which keeps a shape's proportions while it scales
        if (_selectedShapes.Count > 0
            && DiagramHitTester.HandleAt(SelectedBox(), point, GrabDistance / _viewport.Zoom) is DiagramHandle handle)
        {
            _drag = new ScaleDrag(point, handle, SelectedBounds(), SelectedBox(), SelectedLines());

            return;
        }

        // Lines are drawn over the shapes, so the one under the mouse is on top
        if (DiagramHitTester.LineAt(_diagram, point, GrabDistance / _viewport.Zoom) is { } line)
        {
            PressOnLine(line, point, isAdding);

            return;
        }

        if (DiagramHitTester.ShapeAt(_diagram, point) is { } shape)
        {
            PressOnShape(shape, point, isAdding);

            return;
        }

        if (DiagramHitTester.ConnectorAt(_diagram, point, GrabDistance / _viewport.Zoom) is { } connector)
        {
            if (!isAdding)
            {
                ClearSelection();
            }

            if (!_selectedConnectors.Remove(connector.Id))
            {
                _selectedConnectors.Add(connector.Id);
            }

            SelectionChanged();

            return;
        }

        if (!isAdding)
        {
            ClearSelection();
            SelectionChanged();
        }

        _drag = new BandDrag(point);
    }

    private void PressOnShape(DiagramShapeModel shape, Point point, bool isAdding)
    {
        IEnumerable<int> withGroup = WithGroup(shape);
        bool isSelected = _selectedShapes.Contains(shape.Id);

        if (isAdding && isSelected)
        {
            _selectedShapes.ExceptWith(withGroup);
            SelectionChanged();

            return;
        }

        if (!isSelected)
        {
            if (!isAdding)
            {
                ClearSelection();
            }

            _selectedShapes.UnionWith(withGroup);
            SelectionChanged();
        }

        _drag = new MoveDrag(point, MovingBounds(), MoveBox(), MovingLines());
    }

    /// <summary>Selects a line as a shape is, and drags it with the rest of the selection.</summary>
    private void PressOnLine(DiagramConnectorModel line, Point point, bool isAdding)
    {
        bool isSelected = _selectedConnectors.Contains(line.Id);

        if (isAdding && isSelected)
        {
            _selectedConnectors.Remove(line.Id);
            SelectionChanged();

            return;
        }

        if (!isSelected)
        {
            if (!isAdding)
            {
                ClearSelection();
            }

            _selectedConnectors.Add(line.Id);
            SelectionChanged();
        }

        _drag = new MoveDrag(point, MovingBounds(), MoveBox(), MovingLines());
    }

    private void PressWithConnector(Point point)
    {
        if (DiagramHitTester.ShapeUnder(_diagram, point) is { } shape)
        {
            _drag = new ConnectDrag(shape.Id, null);
            _draftConnector = (CenterOf(shape.Bounds), point);
            Refresh();
        }
    }

    private void OnPointerMoved(object? sender, DiagramPointerModel pointer)
    {
        _lastPointer = pointer.Location;
        _isPointerOnCanvas = true;
        Point point = _viewport.ToDiagram(pointer.Location);

        switch (_drag)
        {
            case null:
                ShowCursor(HoverCursor(point));
                UpdateHover(point);
                ShowStatus(Shown);
                return;
            case PanDrag pan:
                _viewport = _viewport with
                {
                    Pan = new Point(
                        _viewport.Pan.X + pointer.Location.X - pan.Last.X,
                        _viewport.Pan.Y + pointer.Location.Y - pan.Last.Y),
                };
                _drag = new PanDrag(pointer.Location);
                break;
            case MoveDrag move:
                DragMove(move, point, pointer.Alt);
                break;
            case ScaleDrag scale:
                DragScale(scale, point, pointer);
                break;
            case CreateDrag create:
                DragCreate(create, point, pointer.Alt);
                break;
            case ConnectDrag connect:
                DragConnect(connect, point);
                break;
            case ReconnectDrag reconnect:
                DragReconnect(reconnect, point);
                break;
            case LineDrag line:
                DragLine(line, point, pointer);
                break;
            case LineEndDrag lineEnd:
                DragLineEnd(lineEnd, point, pointer);
                break;
            case SegmentDrag segment:
                DragSegment(segment, point, pointer.Alt);
                break;
            case BandDrag band:
                _band = FromCorners(band.Start, point);
                break;
        }

        Refresh();
    }

    private void DragConnect(ConnectDrag connect, Point point)
    {
        DiagramShapeModel from = _diagram.Shapes.First(shape => shape.Id == connect.FromShapeId);
        DiagramShapeModel? target = ShapeNear(point);

        if (target?.Id == from.Id)
        {
            target = null;
        }

        _connectTarget = target?.Id;
        _connectTargetSide = target is null ? null : DropSide(target, point);

        Point start = connect.FromSide is DiagramSide fromSide
            ? Point.Round(DiagramRouter.AnchorPoint(from, fromSide))
            : CenterOf(from.Bounds);
        Point end = target is not null && _connectTargetSide is DiagramSide toSide
            ? Point.Round(DiagramRouter.AnchorPoint(target, toSide))
            : point;

        // From a circle to empty space: the new shape it would make, already connected, shows where it goes
        _preview = target is null && connect.FromSide is DiagramSide side
            ? WithConnectedShape(from, side, point)
            : null;
        _draftConnector = _preview is null ? (start, end) : null;
    }

    /// <summary>
    ///     The diagram with a copy of the shape, empty, at the point, and a connector to it from the side dragged
    ///     from, entering it on the side that faces the shape; null when the point is still by the shape. Close to
    ///     the shape's middle line, the copy lines up with it; else it goes on the grid.
    /// </summary>
    private DiagramModel? WithConnectedShape(DiagramShapeModel original, DiagramSide fromSide, Point point)
    {
        const int alignDistance = 20;

        // Closer, it's a drag given up, or one that missed a shape, not a place for a new one
        const int nearDistance = 40;
        Rectangle source = original.Bounds;

        if (Rectangle.Inflate(source, nearDistance, nearDistance).Contains(point))
        {
            return null;
        }

        // A container would hold nothing; a plain box of the usual size is what comes next
        DiagramShapeModel copy = original.Kind == DiagramShapeKind.Container
            ? new DiagramShapeModel(
                0,
                DiagramShapeKind.Rectangle,
                new Rectangle(Point.Empty, _defaultShapeSize),
                string.Empty,
                _style)
            : original with
            {
                Text = string.Empty,
                TextSpans = null,
                GroupId = null,
                InnerGroupIds = null,
                Name = null,
            };
        Size size = copy.Bounds.Size;
        Point middle = CenterOf(source);
        int dx = point.X - middle.X;
        int dy = point.Y - middle.Y;
        DiagramSide toSide = Math.Abs(dx) >= Math.Abs(dy)
            ? dx > 0 ? DiagramSide.Left : DiagramSide.Right
            : dy > 0 ? DiagramSide.Top : DiagramSide.Bottom;

        static int OnGrid(int value) =>
            (int)Math.Round(value / (double)DiagramSnapper.GridSize) * DiagramSnapper.GridSize;

        int x;
        int y;

        if (toSide is DiagramSide.Left or DiagramSide.Right)
        {
            x = OnGrid(toSide == DiagramSide.Left ? point.X : point.X - size.Width);
            y = Math.Abs(dy) <= alignDistance ? middle.Y - (size.Height / 2) : OnGrid(point.Y - (size.Height / 2));
        }
        else
        {
            x = Math.Abs(dx) <= alignDistance ? middle.X - (size.Width / 2) : OnGrid(point.X - (size.Width / 2));
            y = OnGrid(toSide == DiagramSide.Top ? point.Y : point.Y - size.Height);
        }

        int id = _diagram.NextId;
        DiagramShapeModel shape = copy with { Id = id, Bounds = new Rectangle(new Point(x, y), size) };
        var connector = new DiagramConnectorModel(
            id + 1,
            original.Id,
            shape.Id,
            _routing,
            _heads,
            string.Empty,
            _style,
            fromSide,
            toSide);

        return _diagram with
        {
            Shapes = [.. _diagram.Shapes, shape],
            Connectors = [.. _diagram.Connectors, connector],
        };
    }

    /// <summary>
    ///     Shows the connector attached to the shape under the mouse, or else a line from its other end to the mouse.
    ///     It can't attach to the shape its other end is on.
    /// </summary>
    private void DragReconnect(ReconnectDrag reconnect, Point point)
    {
        if (IsStillAClick(reconnect.Start, point))
        {
            return;
        }

        DiagramConnectorModel connector = _diagram.Connectors.First(c => c.Id == reconnect.ConnectorId);
        bool isStart = reconnect.End == DiagramConnectorEnd.Start;
        int? otherShapeId = isStart ? connector.ToShapeId : connector.FromShapeId;
        DiagramShapeModel? target = ShapeNear(point);

        if (target?.Id == otherShapeId)
        {
            target = null;
        }

        _connectTarget = target?.Id;
        _connectTargetSide = target is null ? null : DropSide(target, point);

        if (target is null)
        {
            DiagramConnectorEnd otherEnd = isStart ? DiagramConnectorEnd.End : DiagramConnectorEnd.Start;
            PointF other = DiagramHitTester.EndPoint(DiagramRouter.PathOf(connector, _diagram)!, otherEnd);

            _draftConnector = (Point.Round(other), point);
            _preview = _diagram with { Connectors = [.. _diagram.Connectors.Where(c => c.Id != connector.Id)] };

            return;
        }

        DiagramConnectorModel moved = isStart
            ? connector with { FromShapeId = target.Id, FromSide = _connectTargetSide }
            : connector with { ToShapeId = target.Id, ToSide = _connectTargetSide };

        _draftConnector = null;
        _preview = _diagram with
        {
            Connectors = [.. _diagram.Connectors.Select(c => c.Id == connector.Id ? moved : c)],
        };
    }

    /// <summary>Shows the new line, with the style and heads it gets, from where the drag started to here.</summary>
    private void DragLine(LineDrag line, Point point, DiagramPointerModel pointer)
    {
        Point end = LineEnd(line.Start, point, pointer);

        DiagramConnectorModel drawn = DiagramConnectorModel.Line(_diagram.NextId, line.Start, end, _heads, _style);

        _preview = end == line.Start ? null : _diagram with { Connectors = [.. _diagram.Connectors, drawn] };
    }

    /// <summary>Shows the line with its end here; not on its other end, where it would have no direction.</summary>
    private void DragLineEnd(LineEndDrag drag, Point point, DiagramPointerModel pointer)
    {
        if (IsStillAClick(drag.Start, point))
        {
            return;
        }

        DiagramConnectorModel line = _diagram.Connectors.First(c => c.Id == drag.ConnectorId);
        bool isStart = drag.End == DiagramConnectorEnd.Start;
        Point other = (isStart ? line.ToPoint : line.FromPoint)!.Value;
        Point end = Kept(line.Direction, other, LineEnd(other, point, pointer));

        if (end == other)
        {
            return;
        }

        DiagramConnectorModel moved = isStart ? line with { FromPoint = end } : line with { ToPoint = end };

        _preview = _diagram with { Connectors = [.. _diagram.Connectors.Select(c => c.Id == line.Id ? moved : c)] };
    }

    /// <summary>
    ///     Where a line from the point to the mouse ends: on the edges and middles of the shapes nearby, or with Shift
    ///     along the nearest of level, upright and the diagonals. Alt turns snapping off.
    /// </summary>
    private Point LineEnd(Point from, Point point, DiagramPointerModel pointer)
    {
        if (pointer.Shift)
        {
            return AlongNearestAngle(from, point);
        }

        return pointer.Alt ? point : Snap(point);
    }

    /// <summary>The end of a line from the point moved, if need be, so the line points the way it must.</summary>
    private static Point Kept(DiagramLineDirection direction, Point from, Point end) => direction switch
    {
        DiagramLineDirection.Horizontal => end with { Y = from.Y },
        DiagramLineDirection.Vertical => end with { X = from.X },
        _ => end,
    };

    /// <summary>The point moved onto the nearest of the eight directions from the start, as far from it.</summary>
    private static Point AlongNearestAngle(Point from, Point point)
    {
        int dx = point.X - from.X;
        int dy = point.Y - from.Y;
        double angle = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) * (Math.PI / 4);
        double length = Math.Sqrt((double)dx * dx + (double)dy * dy);

        return new Point(
            from.X + (int)Math.Round(Math.Cos(angle) * length),
            from.Y + (int)Math.Round(Math.Sin(angle) * length));
    }

    /// <summary>A drag of the piece of line at the point, of a selected elbow connector.</summary>
    /// <returns>The drag; null when there's no such line at the point.</returns>
    private SegmentDrag? SegmentDragAt(Point point)
    {
        if (_tool != DiagramTool.Select)
        {
            return null;
        }

        IEnumerable<DiagramConnectorModel> elbows = _diagram.Connectors
            .Where(c => _selectedConnectors.Contains(c.Id) && c.Routing == DiagramRouting.Elbow);

        foreach (DiagramConnectorModel connector in elbows)
        {
            if (DiagramRouter.PathOf(connector, _diagram) is { } path
                && DiagramHitTester.SegmentAt(path.Points, point, GrabDistance / _viewport.Zoom) is int segment
                && DiagramRouter.RouteFor(connector, _diagram) is { } route
                && DiagramRouter.IsLevelOrUpright(route[segment], route[segment + 1]))
            {
                return new SegmentDrag(point, connector.Id, route, segment);
            }
        }

        return null;
    }

    private void DragSegment(SegmentDrag drag, Point point, bool isSnapOff)
    {
        if (IsStillAClick(drag.Start, point))
        {
            return;
        }

        bool isLevel = drag.Route[drag.Segment].Y == drag.Route[drag.Segment + 1].Y;
        DiagramSnapModel snap = isSnapOff
            ? new DiagramSnapModel(0, 0, [])
            : DiagramSnapper.SnapPoint(point, [.. _diagram.Shapes.Select(shape => shape.Bounds)], SnapTolerance());
        Point moved = point;

        moved.Offset(snap.DeltaX, snap.DeltaY);
        _guides = [.. snap.Guides.Where(guide => IsVerticalGuide(guide) != isLevel)];

        List<PointF> route = DiagramRouter.WithSegmentMoved(drag.Route, drag.Segment, isLevel ? moved.Y : moved.X);
        List<Point> corners = [.. route.Skip(1).SkipLast(1).Select(Point.Round)];

        _preview = WithBends(_diagram, drag.ConnectorId, corners.Count > 0 ? corners : null);
    }

    private static DiagramModel WithBends(DiagramModel diagram, int connectorId, IReadOnlyList<Point>? bends) => diagram with
    {
        Connectors = [.. diagram.Connectors.Select(c => c.Id == connectorId ? c with { Bends = bends } : c)],
    };

    /// <returns>The selected connector with an end at the point, and that end; both null when there's none.</returns>
    private (DiagramConnectorModel? Connector, DiagramConnectorEnd? End) ConnectorEndUnder(Point point)
    {
        if (_tool != DiagramTool.Select)
        {
            return (null, null);
        }

        foreach (DiagramConnectorModel connector in _diagram.Connectors.Where(c => _selectedConnectors.Contains(c.Id)))
        {
            if (DiagramRouter.PathOf(connector, _diagram) is { } path
                && DiagramHitTester.ConnectorEndAt(path, point, AnchorGrabDistance / _viewport.Zoom) is DiagramConnectorEnd end)
            {
                return (connector, end);
            }
        }

        return (null, null);
    }

    /// <summary>The side of the target a connector dropped here snaps to: near its circle or its edge's middle.</summary>
    private DiagramSide? DropSide(DiagramShapeModel target, Point point)
    {
        float distance = AnchorGrabDistance / _viewport.Zoom;

        return DiagramHitTester.AnchorAt(target, point, AnchorOffset / _viewport.Zoom, distance)
            ?? DiagramHitTester.AnchorAt(target, point, 0, distance);
    }

    /// <summary>The shape at the point, or the one whose circles reach it, just outside its edges.</summary>
    private DiagramShapeModel? ShapeNear(Point point)
    {
        if (DiagramHitTester.ShapeUnder(_diagram, point) is { } shape)
        {
            return shape;
        }

        int reach = (int)Math.Ceiling((AnchorOffset + AnchorGrabDistance) / _viewport.Zoom);

        return _diagram.Shapes.LastOrDefault(other =>
        {
            Rectangle area = other.Bounds;

            area.Inflate(reach, reach);

            return area.Contains(point);
        });
    }

    /// <returns>The shape showing circles at the point, and the circle there; the circle is null when none is.</returns>
    private (DiagramShapeModel? Shape, DiagramSide? Side) AnchorUnder(Point point)
    {
        if (_tool is not (DiagramTool.Select or DiagramTool.Connector) || ShapeNear(point) is not { } shape)
        {
            return (null, null);
        }

        return (shape, DiagramHitTester.AnchorAt(
            shape,
            point,
            AnchorOffset / _viewport.Zoom,
            AnchorGrabDistance / _viewport.Zoom));
    }

    private void UpdateHover(Point point)
    {
        (DiagramShapeModel? shape, DiagramSide? side) = AnchorUnder(point);

        if (shape?.Id != _hoverShape || side != _hoverSide)
        {
            _hoverShape = shape?.Id;
            _hoverSide = side;
            Refresh();
        }
    }

    private void ClearHover()
    {
        _hoverShape = null;
        _hoverSide = null;
    }

    private void OnPointerLeft(object? sender, EventArgs e)
    {
        _isPointerOnCanvas = false;

        if (_drag is null && _hoverShape is not null)
        {
            ClearHover();
            Refresh();
        }
        else
        {
            ShowStatus(Shown);
        }
    }

    private void DragMove(MoveDrag move, Point point, bool isSnapOff)
    {
        var delta = new Size(point.X - move.Start.X, point.Y - move.Start.Y);

        if (IsStillAClick(move.Start, point))
        {
            return;
        }

        var moved = Moved(move.Box, delta);
        DiagramSnapModel snap = isSnapOff
            ? new DiagramSnapModel(0, 0, [])
            : DiagramSnapper.SnapBox(moved, BoundsOfOthers(move.Originals), SnapTolerance());

        delta += new Size(snap.DeltaX, snap.DeltaY);
        _guides = snap.Guides;
        _preview = WithBounds(
            _diagram,
            move.Originals,
            bounds => Moved(bounds, delta),
            bend => bend + delta,
            move.Lines);
    }

    /// <summary>A click that moves a little isn't a drag, or snapping would shift what was only clicked.</summary>
    private bool IsStillAClick(Point start, Point point)
    {
        int distance = Math.Abs(point.X - start.X) + Math.Abs(point.Y - start.Y);

        return _preview is null && distance * _viewport.Zoom < DragThreshold;
    }

    private void DragScale(ScaleDrag scale, Point point, DiagramPointerModel pointer)
    {
        if (IsStillAClick(scale.Start, point))
        {
            return;
        }

        Point target = point;

        _guides = [];

        // With its proportions kept, a box can't also follow what's around it
        if (!pointer.Alt && !pointer.Shift)
        {
            DiagramSnapModel snap = DiagramSnapper.SnapPoint(point, BoundsOfOthers(scale.Originals), SnapTolerance());
            bool movesX = scale.Handle is not (DiagramHandle.Top or DiagramHandle.Bottom);
            bool movesY = scale.Handle is not (DiagramHandle.Left or DiagramHandle.Right);

            target.Offset(snap.DeltaX, snap.DeltaY);
            _guides = [.. snap.Guides.Where(guide => IsVerticalGuide(guide) ? movesX : movesY)];
        }

        Rectangle box = ScaleBox(scale.Box, scale.Handle, target, pointer.Shift);

        _preview = WithBounds(
            _diagram,
            scale.Originals,
            bounds => ScaleInside(bounds, scale.Box, box),
            bend => ScaleInside(new Rectangle(bend, Size.Empty), scale.Box, box).Location,
            scale.Lines);
    }

    private void DragCreate(CreateDrag create, Point point, bool isSnapOff)
    {
        DiagramSnapModel snap = isSnapOff
            ? new DiagramSnapModel(0, 0, [])
            : DiagramSnapper.SnapPoint(point, [.. _diagram.Shapes.Select(shape => shape.Bounds)], SnapTolerance());
        Point end = point;

        end.Offset(snap.DeltaX, snap.DeltaY);
        _guides = snap.Guides;

        var shape = new DiagramShapeModel(
            _diagram.NextId,
            create.Kind,
            FromCorners(create.Start, end),
            string.Empty,
            _style);

        _preview = _diagram with { Shapes = [.. _diagram.Shapes, shape] };
    }

    private void OnPointerReleased(object? sender, DiagramPointerModel pointer)
    {
        // The drag ends where the button was released, even if no move was reported there
        if (_drag is not null)
        {
            OnPointerMoved(sender, pointer);
        }

        Drag? drag = _drag;
        DiagramModel? preview = _preview;
        Point point = _viewport.ToDiagram(pointer.Location);
        Rectangle? band = _band;
        int? target = _connectTarget;
        DiagramSide? targetSide = _connectTargetSide;

        EndDrag();

        switch (drag)
        {
            // Dragged and brought back to where it was is nothing to undo
            case MoveDrag or ScaleDrag or LineEndDrag when preview is not null && IsMoved(preview):
                Commit(preview);
                break;
            case CreateDrag create:
                ReleaseCreate(create, preview);
                break;
            case LineDrag when preview is not null:
                ReleaseLine(preview);
                break;
            case ConnectDrag connect when target is int toShapeId:
                ReleaseConnect(connect, toShapeId, targetSide);
                break;

            // From a circle to empty space: the new shape and its connector, one step to undo
            case ConnectDrag when preview is not null:
                Commit(preview);
                ClearSelection();
                _selectedShapes.Add(preview.Shapes[^1].Id);
                SelectionChanged();
                break;

            // Dropped on no shape, it stays as it was. Dropped where it already was is nothing to undo
            case ReconnectDrag when target is not null
                && preview is not null
                && !preview.Connectors.SequenceEqual(_diagram.Connectors):
                Commit(preview);
                break;
            case SegmentDrag segment when preview is not null && RouteChanged(segment, preview):
                Commit(preview);
                break;
            case BandDrag when band is Rectangle box:
                _selectedShapes.UnionWith(_diagram.Shapes
                    .Where(shape => DiagramHitTester.IsClickable(shape) && box.Contains(shape.Bounds))
                    .SelectMany(WithGroup));
                _selectedConnectors.UnionWith(_diagram.Connectors
                    .Where(c => c is { FromPoint: Point from, ToPoint: Point to }
                        && DiagramHitTester.IsClickable(c)
                        && box.Contains(from)
                        && box.Contains(to))
                    .Select(c => c.Id));
                SelectionChanged();
                break;
        }

        ShowCursor(HoverCursor(point));
        UpdateHover(point);
        Refresh();
    }

    /// <summary>
    ///     Whether a shape or a line is somewhere else. Bends go along with the shapes, in new lists that never compare
    ///     equal, so they don't count.
    /// </summary>
    private bool IsMoved(DiagramModel preview) =>
        !preview.Shapes.SequenceEqual(_diagram.Shapes)
        || !preview.Connectors.Where(c => c.IsLine).SequenceEqual(_diagram.Connectors.Where(c => c.IsLine));

    /// <summary>Dragged and brought back to where it was is nothing to undo.</summary>
    private static bool RouteChanged(SegmentDrag drag, DiagramModel preview)
    {
        DiagramConnectorModel connector = preview.Connectors.First(c => c.Id == drag.ConnectorId);

        return !DiagramRouter.RouteFor(connector, preview)!.SequenceEqual(drag.Route);
    }

    private void ReleaseCreate(CreateDrag create, DiagramModel? preview)
    {
        Rectangle bounds = preview?.Shapes[^1].Bounds ?? Rectangle.Empty;

        // A click, or a drag too small to see, makes the usual size around that point
        if (bounds.Width < MinShapeSize || bounds.Height < MinShapeSize)
        {
            bounds = new Rectangle(
                create.Start.X - _defaultShapeSize.Width / 2,
                create.Start.Y - _defaultShapeSize.Height / 2,
                _defaultShapeSize.Width,
                _defaultShapeSize.Height);
        }

        var shape = new DiagramShapeModel(_diagram.NextId, create.Kind, bounds, string.Empty, _style);

        // A container goes under the first shape it's drawn around, so it doesn't hide what's in it
        int under = create.Kind == DiagramShapeKind.Container
            ? _diagram.Shapes.ToList().FindIndex(other => bounds.Contains(other.Bounds))
            : -1;

        Commit(_diagram with
        {
            Shapes = under < 0
                ? [.. _diagram.Shapes, shape]
                : [.. _diagram.Shapes.Take(under), shape, .. _diagram.Shapes.Skip(under)],
        });
        ClearSelection();
        _selectedShapes.Add(shape.Id);
        SelectTool(DiagramTool.Select);
        SelectionChanged();
    }

    /// <summary>Adds the line drawn, and selects it to change it at once, as a new shape is.</summary>
    private void ReleaseLine(DiagramModel preview)
    {
        DiagramConnectorModel line = preview.Connectors[^1];

        Commit(preview);
        ClearSelection();
        _selectedConnectors.Add(line.Id);
        SelectTool(DiagramTool.Select);
        SelectionChanged();
    }

    private void ReleaseConnect(ConnectDrag connect, int toShapeId, DiagramSide? toSide)
    {
        var connector = new DiagramConnectorModel(
            _diagram.NextId,
            connect.FromShapeId,
            toShapeId,
            _routing,
            _heads,
            string.Empty,
            _style,
            connect.FromSide,
            toSide);

        Commit(_diagram with { Connectors = [.. _diagram.Connectors, connector] });
    }

    /// <summary>
    ///     On a shape or a connector, shows the menu; a right-click on something not selected selects only it (with
    ///     its group) first, and one on what's selected keeps the whole selection. Nothing on an empty spot.
    /// </summary>
    private void OnPointerRightClicked(object? sender, Point location)
    {
        if (_drag is not null)
        {
            return;
        }

        Point point = _viewport.ToDiagram(location);

        if (DiagramHitTester.ShapeAt(_diagram, point) is { } shape)
        {
            if (!_selectedShapes.Contains(shape.Id))
            {
                ClearSelection();
                _selectedShapes.UnionWith(WithGroup(shape));
            }
        }
        else if (DiagramHitTester.ConnectorAt(_diagram, point, GrabDistance / _viewport.Zoom) is { } connector)
        {
            if (!_selectedConnectors.Contains(connector.Id))
            {
                ClearSelection();
                _selectedConnectors.Add(connector.Id);
            }
        }
        else
        {
            return;
        }

        SelectionChanged();
        Refresh();
        _view.ShowContextMenu(location, new DiagramContextMenuModel(
                _copiedStyle is not null,
                CanGroup(),
                CanUngroup(),
                SelectedUnits().Count >= 2,
                SelectedUnits().Count >= 3));
    }

    private void OnPointerDoubleClicked(object? sender, DiagramPointerModel pointer)
    {
        if (_tool != DiagramTool.Select)
        {
            return;
        }

        // The press before this one may have started a drag
        EndDrag();

        Point point = _viewport.ToDiagram(pointer.Location);

        if (DiagramHitTester.ShapeAt(_diagram, point) is { } shape)
        {
            BeginShapeText(shape);
        }
        else if (DiagramHitTester.ConnectorAt(_diagram, point, GrabDistance / _viewport.Zoom) is { } connector)
        {
            BeginLabel(connector);
        }
        else
        {
            BeginNewText(point);
        }

        Refresh();
    }

    private void ResetRoute()
    {
        if (_diagram.Connectors.Any(c => _selectedConnectors.Contains(c.Id) && c.Bends is not null))
        {
            ChangeSelectedConnectors(connector => connector with { Bends = null });
        }
    }

    /// <summary>
    ///     The selected images go back to the size they were inserted at, from where their top left is; what's locked
    ///     of their place and size stays, but not their proportions, which are the image's own again.
    /// </summary>
    private void ResetSize()
    {
        Commit(
            _diagram with
            {
                Shapes =
                [
                    .. _diagram.Shapes.Select(shape => shape is { Kind: DiagramShapeKind.Image, Image: string image }
                        && _selectedShapes.Contains(shape.Id)
                            ? shape with
                            {
                                Bounds = Locked(
                                    shape.Bounds,
                                    new Rectangle(shape.Bounds.Location, DiagramSvgImage.InsertSize(image)),
                                    shape.Locks & DiagramLock.Bounds),
                            }
                            : shape),
                ],
            },
            onlyIfChanged: true);
    }

    private void BeginShapeText(DiagramShapeModel shape)
    {
        _commitText = text => Commit(_diagram with
        {
            Shapes =
            [
                .. _diagram.Shapes.Select(s => s.Id == shape.Id
                    ? s with { Text = text.Text, TextSpans = DiagramRichText.OrNull(text.Spans) }
                    : s),
            ],
        });

        Rectangle area = shape.Kind switch
        {
            DiagramShapeKind.Image => TextAreaUnder(shape),
            DiagramShapeKind.Container => Rectangle.Round(DiagramFigures.ContainerTitle(shape)),
            _ => shape.Bounds,
        };
        Point topLeft = _viewport.ToCanvas(area.Location);
        Point bottomRight = _viewport.ToCanvas(new Point(area.Right, area.Bottom));

        _view.BeginTextEdit(
            Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y),
            new DiagramRichTextModel(shape.Text, shape.TextSpans ?? []),
            CanvasFontSize(shape.Style.FontSize),
            shape.Style.FontFamily,
            shape.Style.TextColor);
    }

    /// <summary>Where an image's text is typed: under it, as wide as its text can be, two lines high or more.</summary>
    private static Rectangle TextAreaUnder(DiagramShapeModel shape)
    {
        string typed = shape.Text.Length > 0 ? shape.Text : " ";
        RectangleF text = DiagramTextLayout.OfShape(shape with { Text = typed }).Bounds;
        int width = Math.Max(shape.Bounds.Width, DiagramTextLayout.ImageTextWidth);
        int height = Math.Max((int)Math.Ceiling(text.Height), shape.Style.FontSize * 2);

        return new Rectangle(shape.Bounds.Left + (shape.Bounds.Width - width) / 2, shape.Bounds.Bottom, width, height);
    }

    private void BeginLabel(DiagramConnectorModel connector)
    {
        _commitText = text => Commit(_diagram with
        {
            Connectors =
            [
                .. _diagram.Connectors.Select(c => c.Id == connector.Id
                    ? c with { Label = text.Text, LabelSpans = DiagramRichText.OrNull(text.Spans) }
                    : c),
            ],
        });

        PointF center = DiagramRouter.PathOf(connector, _diagram)!.LabelCenter;
        int fontSize = CanvasFontSize(connector.Style.FontSize);
        Point canvas = _viewport.ToCanvas(Point.Round(center));
        int width = fontSize * 14;
        int height = fontSize * 2;

        _view.BeginTextEdit(
            new Rectangle(canvas.X - width / 2, canvas.Y - height / 2, width, height),
            new DiagramRichTextModel(connector.Label, connector.LabelSpans ?? []),
            fontSize,
            connector.Style.FontFamily,
            connector.Style.TextColor);
    }

    /// <summary>A text on its own is only added once it has something in it.</summary>
    private void BeginNewText(Point point)
    {
        _commitText = typed =>
        {
            string text = typed.Text;

            if (text.Length == 0)
            {
                return;
            }

            IReadOnlyList<string> lines = text.Split('\n');
            int width = (int)lines.Max(line => DiagramTextLayout.WidthOf(line, _style.FontSize)) + 16;
            int height = (int)DiagramTextLayout.LayoutAt(text, point, _style.FontSize).Bounds.Height + 8;
            var shape = new DiagramShapeModel(
                _diagram.NextId,
                DiagramShapeKind.Text,
                new Rectangle(point.X, point.Y, width, height),
                text,
                _style,
                TextSpans: DiagramRichText.OrNull(typed.Spans));

            Commit(_diagram with { Shapes = [.. _diagram.Shapes, shape] });
            ClearSelection();
            _selectedShapes.Add(shape.Id);
            SelectionChanged();
        };

        int fontSize = CanvasFontSize(_style.FontSize);
        Point canvas = _viewport.ToCanvas(point);

        _view.BeginTextEdit(
            new Rectangle(canvas.X, canvas.Y, fontSize * 14, fontSize * 2),
            DiagramRichTextModel.Plain(string.Empty),
            fontSize,
            _style.FontFamily,
            _style.TextColor);
    }

    private void OnTextCommitted(object? sender, DiagramRichTextModel typed)
    {
        Action<DiagramRichTextModel>? commit = _commitText;

        _commitText = null;
        commit?.Invoke(DiagramRichText.Normalized(typed.Text, typed.Spans));
        Refresh();
    }

    private void OnWheelTurned(object? sender, DiagramWheelModel wheel)
    {
        if (wheel.Control)
        {
            ZoomAround(wheel.Location, MathF.Pow(WheelZoomFactor, wheel.Steps));
        }
        else
        {
            int pixels = (int)Math.Round(wheel.Steps * WheelScrollPixels);
            Size scroll = wheel.Shift ? new Size(pixels, 0) : new Size(0, pixels);

            _viewport = _viewport with { Pan = _viewport.Pan + scroll };
            Refresh();
        }
    }

    private void ZoomAround(Point canvas, float factor)
    {
        // The diagram's point under the mouse stays under it
        Point point = _viewport.ToDiagram(canvas);
        float zoom = Math.Clamp(_viewport.Zoom * factor, MinZoom, MaxZoom);

        _viewport = new DiagramViewportModel(
            zoom,
            new Point(canvas.X - (int)Math.Round(point.X * zoom), canvas.Y - (int)Math.Round(point.Y * zoom)));
        Refresh();
    }

    private void OnToolSelected(object? sender, DiagramTool tool)
    {
        SelectTool(tool);
        ShowCursor(tool == DiagramTool.Select ? DiagramCursor.Default : DiagramCursor.Cross);
    }

    private void SelectTool(DiagramTool tool)
    {
        _tool = tool;
        _view.ShowTool(tool);
    }

    private void OnCommandRequested(object? sender, DiagramCommand command)
    {
        // A drag only ends with its release, or with Esc
        if (_drag is not null && command != DiagramCommand.Cancel)
        {
            return;
        }

        switch (command)
        {
            case DiagramCommand.New:
                New();
                break;
            case DiagramCommand.Open:
                Open();
                break;
            case DiagramCommand.Save:
                Save();
                break;
            case DiagramCommand.SaveAs:
                SaveAs();
                break;
            case DiagramCommand.ExportPdf:
                ExportPdf();
                break;
            case DiagramCommand.ExportPng:
                ExportPng();
                break;
            case DiagramCommand.Undo:
                Undo();
                break;
            case DiagramCommand.Redo:
                Redo();
                break;
            case DiagramCommand.Cut:
                Copy();
                Delete();
                break;
            case DiagramCommand.Copy:
                Copy();
                break;
            case DiagramCommand.CopyPicture:
                CopyPicture();
                break;
            case DiagramCommand.Paste:
                Paste();
                break;
            case DiagramCommand.InsertImage:
                InsertImages(_view.AskImagePaths(), _viewport.ToDiagram(_lastPointer));
                break;
            case DiagramCommand.Duplicate:
                Insert(SelectedFragment(), PasteOffset);
                break;
            case DiagramCommand.CopyStyle:
                CopyStyle();
                break;
            case DiagramCommand.PasteStyle:
                PasteStyle();
                break;
            case DiagramCommand.Delete:
                Delete();
                break;
            case DiagramCommand.SelectAll:
                SelectAll();
                break;
            case DiagramCommand.Group:
                Group();
                break;
            case DiagramCommand.Ungroup:
                Ungroup();
                break;
            case DiagramCommand.BringToFront:
                Reorder(selectedFirst: false);
                break;
            case DiagramCommand.SendToBack:
                Reorder(selectedFirst: true);
                break;
            case DiagramCommand.AlignLeft or DiagramCommand.AlignCenter or DiagramCommand.AlignRight
                or DiagramCommand.AlignTop or DiagramCommand.AlignMiddle or DiagramCommand.AlignBottom
                or DiagramCommand.DistributeHorizontally or DiagramCommand.DistributeVertically:
                Arrange(command);
                break;
            case DiagramCommand.ResetRoute:
                ResetRoute();
                break;
            case DiagramCommand.ResetSize:
                ResetSize();
                break;
            case DiagramCommand.Bold:
                ToggleFormat(DiagramTextFormat.Bold);
                break;
            case DiagramCommand.Italic:
                ToggleFormat(DiagramTextFormat.Italic);
                break;
            case DiagramCommand.ZoomIn:
                ZoomAround(_lastPointer, CommandZoomFactor);
                break;
            case DiagramCommand.ZoomOut:
                ZoomAround(_lastPointer, 1 / CommandZoomFactor);
                break;
            case DiagramCommand.ZoomReset:
                _viewport = new DiagramViewportModel(1, _viewport.Pan);
                Refresh();
                break;
            case DiagramCommand.Cancel:
                Cancel();
                break;
        }
    }

    private void Cancel()
    {
        if (_drag is not null)
        {
            EndDrag();
        }
        else if (_tool != DiagramTool.Select)
        {
            SelectTool(DiagramTool.Select);
        }
        else
        {
            ClearSelection();
            SelectionChanged();
        }

        ShowCursor(DiagramCursor.Default);
        Refresh();
    }

    private void New()
    {
        if (ConfirmLeave())
        {
            Replace(DiagramModel.Empty with { Theme = _themeForNew }, null);
        }
    }

    private void Open()
    {
        if (ConfirmLeave() && _view.AskOpenPath() is string path)
        {
            OpenFile(path);
        }
    }

    /// <returns>False when it couldn't be opened, which the user is told.</returns>
    private bool OpenFile(string path)
    {
        try
        {
            Replace(_fileStore.Load(path), path);
            _savedVersion = _fileStore.VersionOf(path);
            RememberFile(path);

            return true;
        }
        catch (InvalidDataException ex)
        {
            // Not a diagram, but maybe an SVG image made elsewhere
            if (OpenAsImage(path))
            {
                return true;
            }

            _view.ShowError($"Could not open {Path.GetFileName(path)}: {ex.Message}");

            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowError($"Could not open {Path.GetFileName(path)}: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     An SVG image made elsewhere opens as a new diagram that holds it, at its own size: it's never saved over,
    ///     since the diagram isn't the file.
    /// </summary>
    /// <returns>False when the file isn't an SVG image either.</returns>
    private bool OpenAsImage(string path)
    {
        string text;
        string image;

        try
        {
            text = _fileStore.ReadImage(path);
            image = DiagramSvgImage.Prepare(text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return false;
        }

        var shape = new DiagramShapeModel(
            1,
            DiagramShapeKind.Image,
            new Rectangle(Point.Empty, DiagramSvgImage.OpenSize(image)),
            string.Empty,
            _style,
            Locks: DiagramLock.AspectRatio,
            Image: image);

        Replace(new DiagramModel([shape], []), null);
        _savedVersion = null;
        _imageCatalog.Add(text, Path.GetFileName(path));
        ShowCatalog();

        return true;
    }

    /// <summary>Starts over with another diagram, with its top left corner in view.</summary>
    private void Replace(DiagramModel diagram, string? path)
    {
        _diagram = diagram;
        _savedDiagram = diagram;
        _path = path;
        _undo.Clear();
        _redo.Clear();
        ClearSelection();

        const int margin = 40;
        Rectangle area = diagram.Shapes.Count == 0
            ? Rectangle.Empty
            : UnionOf(diagram.Shapes.Select(shape => shape.Bounds));

        _viewport = new DiagramViewportModel(1, new Point(margin - area.Left, margin - area.Top));
        SelectionChanged();
        Refresh();
    }

    private bool Save() => _path is null ? SaveAs() : SaveTo(_path);

    private bool SaveAs()
    {
        string suggested = _path is null ? "Diagram.svg" : Path.GetFileName(_path);

        return _view.AskSavePath(suggested) is string path && SaveTo(path);
    }

    private bool SaveTo(string path)
    {
        // Another window or program may have saved it. Saving as another file has its own dialog's question
        if (string.Equals(path, _path, StringComparison.OrdinalIgnoreCase)
            && _fileStore.VersionOf(path) != _savedVersion
            && !_view.AskOverwrite(Name))
        {
            return false;
        }

        try
        {
            _fileStore.Save(path, _diagram);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowError($"Could not save {Path.GetFileName(path)}: {ex.Message}");

            return false;
        }

        _path = path;
        _savedVersion = _fileStore.VersionOf(path);
        _savedDiagram = _diagram;
        RememberFile(path);
        Refresh();

        return true;
    }

    /// <summary>Writes a copy to share or print; the diagram stays saved, or not, where it was.</summary>
    private void ExportPdf()
    {
        string suggested = Path.ChangeExtension(_path is null ? "Diagram" : Path.GetFileName(_path), ".pdf");

        if (_view.AskExportPdfPath(suggested) is not string path)
        {
            return;
        }

        try
        {
            _pdfExporter.Export(path, _diagram);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowError($"Could not export {Path.GetFileName(path)}: {ex.Message}");
        }
    }

    private void ExportPng()
    {
        string suggested = Path.ChangeExtension(_path is null ? "Diagram" : Path.GetFileName(_path), ".png");

        if (_view.AskExportPng(suggested) is not { } export)
        {
            return;
        }

        try
        {
            _pngExporter.Export(export.Path, _diagram, Math.Clamp(export.Scale, 1, 2));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowError($"Could not export {Path.GetFileName(export.Path)}: {ex.Message}");
        }
    }

    /// <returns>True when the diagram can be left: nothing is lost, or the user said so.</returns>
    private bool ConfirmLeave()
    {
        if (!HasChanges)
        {
            return true;
        }

        return _view.AskSaveChanges(Name) switch
        {
            DiagramSaveChoice.Save => Save(),
            DiagramSaveChoice.Discard => true,
            _ => false,
        };
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = !ConfirmLeave();
        _isLeaveConfirmed = !e.Cancel;
    }

    private void Undo()
    {
        if (_undo.TryPop(out DiagramModel? diagram))
        {
            _redo.Push(_diagram);
            _diagram = diagram;
            KeepSelectionThatExists();
        }
    }

    private void Redo()
    {
        if (_redo.TryPop(out DiagramModel? diagram))
        {
            _undo.Push(_diagram);
            _diagram = diagram;
            KeepSelectionThatExists();
        }
    }

    private void KeepSelectionThatExists()
    {
        _selectedShapes.IntersectWith(_diagram.Shapes.Select(shape => shape.Id));
        _selectedConnectors.IntersectWith(_diagram.Connectors.Select(connector => connector.Id));
        SelectionChanged();
        Refresh();
    }

    private void Copy()
    {
        if (_selectedShapes.Count > 0 || SelectedLines().Count > 0)
        {
            _view.SetClipboardText(DiagramJson.Write(SelectedFragment()));
            _pasteCount = 0;
        }
    }

    /// <summary>A picture of what's selected, or of the whole diagram when nothing is, for other programs.</summary>
    private void CopyPicture()
    {
        DiagramModel picture = _selectedShapes.Count > 0 || SelectedLines().Count > 0 ? SelectedFragment() : _diagram;

        if (picture.Shapes.Count > 0 || picture.Connectors.Count > 0)
        {
            _view.SetClipboardPicture(picture);
        }
    }

    private void Paste()
    {
        if (_view.GetClipboardText() is not string text)
        {
            return;
        }

        try
        {
            DiagramModel fragment = DiagramJson.Read(text);

            // So several pastes don't land on top of each other
            _pasteCount++;
            Insert(fragment, PasteOffset * _pasteCount);
        }
        catch (InvalidDataException)
        {
            PasteImage(text);
        }
    }

    /// <summary>SVG markup, copied from a web page or an editor, goes in as an image where the mouse was.</summary>
    private void PasteImage(string text)
    {
        if (!text.TrimStart().StartsWith('<'))
        {
            return;
        }

        try
        {
            InsertImageShapes([DiagramSvgImage.Prepare(text)], _viewport.ToDiagram(_lastPointer));
        }
        catch (InvalidDataException)
        {
            // Other markup, such as HTML
            return;
        }

        _imageCatalog.Add(text, PastedImageName);
        ShowCatalog();
    }

    private void OnFilesDropped(object? sender, DiagramFilesDropModel drop)
    {
        if (_drag is null)
        {
            InsertImages(drop.Paths, _viewport.ToDiagram(drop.Location));
        }
    }

    /// <summary>
    ///     Adds the SVG images in a row from the point, selected; tells which files couldn't be, and why, without
    ///     leaving out the others.
    /// </summary>
    private void InsertImages(IReadOnlyList<string> paths, Point at)
    {
        List<string> images = [];
        List<(string Text, string Name)> read = [];
        List<string> problems = [];

        foreach (string path in paths)
        {
            try
            {
                string text = _fileStore.ReadImage(path);

                images.Add(DiagramSvgImage.Prepare(text));
                read.Add((text, Path.GetFileName(path)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                problems.Add($"{Path.GetFileName(path)}: {ex.Message}");
            }
        }

        InsertImageShapes(images, at);

        // In the catalog, to insert again from the images panel
        foreach ((string text, string name) in read)
        {
            _imageCatalog.Add(text, name);
        }

        ShowCatalog();

        if (problems.Count > 0)
        {
            _view.ShowError($"Could not insert {string.Join(Environment.NewLine, problems)}");
        }
    }

    private void InsertImageShapes(IReadOnlyList<string> images, Point at)
    {
        // On the grid, as a shape drawn there would be
        int x = (int)Math.Round(at.X / (double)DiagramSnapper.GridSize) * DiagramSnapper.GridSize;
        int y = (int)Math.Round(at.Y / (double)DiagramSnapper.GridSize) * DiagramSnapper.GridSize;
        List<DiagramShapeModel> shapes = [];

        foreach (string image in images)
        {
            Size size = DiagramSvgImage.InsertSize(image);

            shapes.Add(new DiagramShapeModel(
                shapes.Count + 1,
                DiagramShapeKind.Image,
                new Rectangle(new Point(x, y), size),
                string.Empty,
                _style,
                Locks: DiagramLock.AspectRatio,
                Image: image));
            x += size.Width + PasteOffset;
        }

        if (shapes.Count > 0)
        {
            Insert(new DiagramModel(shapes, []), 0);
            SelectTool(DiagramTool.Select);
        }
    }

    /// <summary>The selected shapes, the connectors between them, and the selected lines.</summary>
    private DiagramModel SelectedFragment()
    {
        HashSet<int> shapes = [.. MovingBounds().Keys];
        HashSet<int> lines = MovingLines();

        bool IsCopied(int? shapeId) => shapeId is int id && shapes.Contains(id);

        return new DiagramModel(
            [.. _diagram.Shapes.Where(shape => shapes.Contains(shape.Id))],
            [
                .. _diagram.Connectors.Where(connector => connector.IsLine
                    ? lines.Contains(connector.Id)
                    : IsCopied(connector.FromShapeId) && IsCopied(connector.ToShapeId)),
            ]);
    }

    private bool IsSelectedShape(int? shapeId) => shapeId is int id && _selectedShapes.Contains(id);

    /// <summary>Adds copies of the parts, with new ids and groups, and selects them.</summary>
    private void Insert(DiagramModel fragment, int offset)
    {
        if (fragment.Shapes.Count == 0 && !fragment.Connectors.Any(connector => connector.IsLine))
        {
            return;
        }

        int nextId = _diagram.NextId;
        int nextGroupId = _diagram.NextGroupId;
        Dictionary<int, int> shapeIds = [];
        Dictionary<int, int> groupIds = [];
        List<DiagramShapeModel> shapes = [];

        int NewGroupId(int groupId)
        {
            if (!groupIds.TryGetValue(groupId, out int newGroupId))
            {
                newGroupId = nextGroupId++;
                groupIds[groupId] = newGroupId;
            }

            return newGroupId;
        }

        foreach (DiagramShapeModel shape in fragment.Shapes)
        {
            shapeIds[shape.Id] = nextId++;

            shapes.Add(shape with
            {
                Id = shapeIds[shape.Id],
                Bounds = Moved(shape.Bounds, new Size(offset, offset)),
                GroupId = shape.GroupId is int group ? NewGroupId(group) : null,
                InnerGroupIds = shape.InnerGroupIds is { } inner ? [.. inner.Select(NewGroupId)] : null,
            });
        }

        List<DiagramConnectorModel> connectors = [];

        var shift = new Size(offset, offset);

        foreach (DiagramConnectorModel connector in fragment.Connectors)
        {
            if (connector is { FromPoint: Point from, ToPoint: Point to })
            {
                connectors.Add(connector with { Id = nextId++, FromPoint = from + shift, ToPoint = to + shift });
            }
            else if (connector is { FromShapeId: int fromId, ToShapeId: int toId }
                && shapeIds.TryGetValue(fromId, out int newFromId)
                && shapeIds.TryGetValue(toId, out int newToId))
            {
                connectors.Add(connector with
                {
                    Id = nextId++,
                    FromShapeId = newFromId,
                    ToShapeId = newToId,
                    Bends = connector.Bends?.Select(bend => bend + shift).ToList(),
                });
            }
        }

        Commit(new DiagramModel([.. _diagram.Shapes, .. shapes], [.. _diagram.Connectors, .. connectors]));
        ClearSelection();
        _selectedShapes.UnionWith(shapes.Select(shape => shape.Id));
        _selectedConnectors.UnionWith(connectors.Where(connector => connector.IsLine).Select(line => line.Id));
        SelectionChanged();
        Refresh();
    }

    /// <summary>Deletes what's selected, and the connectors that lose one of their shapes.</summary>
    private void Delete()
    {
        if (_selectedShapes.Count == 0 && _selectedConnectors.Count == 0)
        {
            return;
        }

        Commit(new DiagramModel(
            [.. _diagram.Shapes.Where(shape => !_selectedShapes.Contains(shape.Id))],
            [
                .. _diagram.Connectors.Where(connector => !_selectedConnectors.Contains(connector.Id)
                    && !IsSelectedShape(connector.FromShapeId)
                    && !IsSelectedShape(connector.ToShapeId)),
            ]));
        ClearSelection();
        SelectionChanged();
        Refresh();
    }

    private void SelectAll()
    {
        // Not what's hidden or locked, which clicks go through too
        _selectedShapes.UnionWith(_diagram.Shapes.Where(DiagramHitTester.IsClickable).Select(shape => shape.Id));
        _selectedConnectors.UnionWith(
            _diagram.Connectors.Where(DiagramHitTester.IsClickable).Select(connector => connector.Id));
        SelectionChanged();
        Refresh();
    }

    /// <summary>Two or more shapes, not all in one group already, which grouping again would leave as they are.</summary>
    private bool CanGroup()
    {
        if (_selectedShapes.Count < 2)
        {
            return false;
        }

        List<int?> groups = [.. _diagram.Shapes
            .Where(shape => _selectedShapes.Contains(shape.Id))
            .Select(shape => shape.GroupId)
            .Distinct()];

        return groups.Count > 1 || groups[0] is null;
    }

    private bool CanUngroup() =>
        _diagram.Shapes.Any(shape => _selectedShapes.Contains(shape.Id) && shape.GroupId is not null);

    private void Group()
    {
        if (!CanGroup())
        {
            return;
        }

        int groupId = _diagram.NextGroupId;

        // A group selected goes inside the new one whole, so ungrouping gives it back
        Commit(_diagram with
        {
            Shapes =
            [
                .. _diagram.Shapes.Select(shape => _selectedShapes.Contains(shape.Id)
                    ? shape with
                    {
                        GroupId = groupId,
                        InnerGroupIds = shape.GroupId is int outer ? [.. shape.InnerGroupIds ?? [], outer] : null,
                    }
                    : shape),
            ],
        });
    }

    /// <summary>Takes the selected shapes out of their outermost group, back into the group just inside it.</summary>
    private void Ungroup()
    {
        if (CanUngroup())
        {
            Commit(_diagram with
            {
                Shapes =
                [
                    .. _diagram.Shapes.Select(shape => _selectedShapes.Contains(shape.Id) ? Ungrouped(shape) : shape),
                ],
            });
        }
    }

    private static DiagramShapeModel Ungrouped(DiagramShapeModel shape)
    {
        if (shape.InnerGroupIds is not { Count: > 0 } inner)
        {
            return shape with { GroupId = null, InnerGroupIds = null };
        }

        return shape with
        {
            GroupId = inner[^1],
            InnerGroupIds = inner.Count > 1 ? [.. inner.Take(inner.Count - 1)] : null,
        };
    }

    /// <summary>Moves the selected shapes to the start or the end of the list, keeping their order.</summary>
    private void Reorder(bool selectedFirst)
    {
        if (_selectedShapes.Count == 0)
        {
            return;
        }

        IEnumerable<DiagramShapeModel> selected = _diagram.Shapes.Where(shape => _selectedShapes.Contains(shape.Id));
        IEnumerable<DiagramShapeModel> others = _diagram.Shapes.Where(shape => !_selectedShapes.Contains(shape.Id));

        Commit(_diagram with { Shapes = selectedFirst ? [.. selected, .. others] : [.. others, .. selected] });
    }

    private void OnNudgeRequested(object? sender, Size delta)
    {
        if (_drag is null && (_selectedShapes.Count > 0 || SelectedLines().Count > 0))
        {
            // Shapes locked where they are don't move, and that's nothing to undo
            Commit(
                WithBounds(
                    _diagram,
                    MovingBounds(),
                    bounds => Moved(bounds, delta),
                    bend => bend + delta,
                    MovingLines()),
                onlyIfChanged: true);
        }
    }

    /// <summary>The diagram's theme; the default one for a diagram that names none.</summary>
    private DiagramThemeModel Theme => DiagramThemes.Of(_diagram.Theme);

    /// <summary>What the color menus offer, when the theme or the colors used lately changed.</summary>
    private void ShowPalette()
    {
        DiagramThemeModel theme = Theme;
        string shown = theme.Name + ":" + string.Join(",", _recentColors.Select(color => color.ToArgb()));

        if (shown == _shownPalette)
        {
            return;
        }

        _shownPalette = shown;
        _view.ShowPalette(new DiagramPaletteModel(
            theme.Name,
            [.. DiagramThemes.All.Select(other => other.Name)],
            DiagramThemes.Grid(theme),
            DiagramThemes.StandardColors,
            [.. _recentColors.Select(color => new DiagramSwatchModel(color, RecentColorName))]));
    }

    /// <summary>
    ///     Gives the diagram another theme, and what's drawn next its colors. When the diagram has colors of the old
    ///     theme, asks whether they take the new theme's: either way, one step to undo.
    /// </summary>
    private void OnThemeSelected(object? sender, string name)
    {
        DiagramThemeModel from = Theme;
        DiagramThemeModel to = DiagramThemes.Of(name);

        if (_drag is not null || to.Name == from.Name)
        {
            return;
        }

        DiagramModel themed = _diagram with { Theme = to.Name };
        DiagramModel recolored = DiagramThemes.Recolored(themed, from, to);
        bool hasThemeColors = !recolored.Shapes.SequenceEqual(themed.Shapes)
            || !recolored.Connectors.SequenceEqual(themed.Connectors);

        if (hasThemeColors && _view.AskApplyTheme(to.Name))
        {
            themed = recolored;
        }

        _style = DiagramThemes.DefaultsOf(to, _style);
        _themeForNew = to.Name;
        SaveSettings();
        Commit(themed);
        ShowSelectedStyle();
    }

    /// <summary>Picks a color from the screen, as the color picker does, and uses it as if chosen from a menu.</summary>
    private async void OnScreenColorRequested(object? sender, DiagramColorTarget target)
    {
        if (_drag is null && await _screenColorPicker.PickAsync() is { } picked)
        {
            OnColorSelected(sender, new DiagramColorChoiceModel(target, picked.Color));
        }
    }

    /// <summary>
    ///     A color the menus don't offer, e.g. from More colors, typed or picked from the screen, goes first among the
    ///     colors used lately.
    /// </summary>
    private void RememberColor(Color color)
    {
        if (color.IsEmpty || color.A == 0 || DiagramThemes.Offers(Theme, color))
        {
            return;
        }

        _recentColors.RemoveAll(other => other.ToArgb() == color.ToArgb());
        _recentColors.Insert(0, color);

        if (_recentColors.Count > MaxRecentColors)
        {
            _recentColors.RemoveRange(MaxRecentColors, _recentColors.Count - MaxRecentColors);
        }

        SaveSettings();
        ShowPalette();
    }

    private void OnColorSelected(object? sender, DiagramColorChoiceModel choice)
    {
        // The no fill choice is Color.Empty, which stays empty, and a title fill of none, transparent, stays so
        bool isNone = choice.Target == DiagramColorTarget.TitleFill && choice.Color.A == 0;
        Color color = choice.Color.IsEmpty || isNone
            ? choice.Color
            : Color.FromArgb(255, choice.Color);

        ChangeStyle(style => choice.Target switch
        {
            DiagramColorTarget.Fill => style with { Fill = color },
            DiagramColorTarget.Line => style with { Line = color },
            DiagramColorTarget.TitleFill => style with { TitleFill = color },
            DiagramColorTarget.TextOutline => WithTextOutlineColor(style, color),
            _ => style with { TextColor = color },
        });
        RememberColor(color);
    }

    /// <summary>
    ///     No color turns the edge off, keeping its color for later; a color turns it on when it's off, or the color
    ///     would seem to do nothing.
    /// </summary>
    private static DiagramStyleModel WithTextOutlineColor(DiagramStyleModel style, Color color)
    {
        if (color.IsEmpty)
        {
            return style with { TextOutlineWidth = 0 };
        }

        return style with
        {
            TextOutlineColor = color,
            TextOutlineWidth = style.TextOutlineWidth == 0 ? TurnedOnTextOutlineWidth : style.TextOutlineWidth,
        };
    }

    private void OnMoreColorsRequested(object? sender, DiagramColorTarget target)
    {
        DiagramStyleModel style = SelectedStyle();
        Color current = target switch
        {
            DiagramColorTarget.Fill => style.Fill,
            DiagramColorTarget.Line => style.Line,
            DiagramColorTarget.TitleFill => style.TitleFill,
            DiagramColorTarget.TextOutline => style.TextOutlineColor,
            _ => style.TextColor,
        };

        if (_view.AskColor(current) is Color color)
        {
            OnColorSelected(sender, new DiagramColorChoiceModel(target, color));
        }
    }

    private void OnLineWidthSelected(object? sender, int width)
    {
        ChangeStyle(style => style with { LineWidth = Math.Clamp(width, 0, MaxLineWidth) });
    }

    /// <summary>Moves and scales the selected shapes as a whole, to where the box around them was typed.</summary>
    private void OnBoundsEdited(object? sender, Rectangle box)
    {
        if (_drag is not null || _selectedShapes.Count == 0)
        {
            return;
        }

        Rectangle from = SelectedBox();
        var to = new Rectangle(box.Location, new Size(Math.Max(box.Width, MinShapeSize), Math.Max(box.Height, MinShapeSize)));

        if (to != from)
        {
            Commit(
                WithBounds(
                    _diagram,
                    SelectedBounds(),
                    bounds => ScaleInside(bounds, from, to),
                    bend => ScaleInside(new Rectangle(bend, Size.Empty), from, to).Location),
                onlyIfChanged: true);
        }
    }

    /// <summary>A change to the diagram like any other, so it can be undone, and it's saved with the shapes.</summary>
    private void OnLockToggled(object? sender, DiagramLockChoiceModel choice)
    {
        if (_drag is not null || _selectedShapes.Count == 0)
        {
            return;
        }

        DiagramLock locks = choice.Lock & DiagramLock.All;

        Commit(
            _diagram with
            {
                Shapes =
                [
                    .. _diagram.Shapes.Select(shape => _selectedShapes.Contains(shape.Id)
                        ? shape with { Locks = choice.IsLocked ? shape.Locks | locks : shape.Locks & ~locks }
                        : shape),
                ],
            },
            onlyIfChanged: true);
    }

    /// <summary>
    ///     The top corners only: bottom corners that followed them keep how round they are, so the two never change
    ///     together.
    /// </summary>
    private void OnCornerRadiusEdited(object? sender, int radius)
    {
        int corner = Math.Max(radius, 0);

        bool IsChanged(DiagramShapeModel shape) => _selectedShapes.Contains(shape.Id)
            && DiagramFigures.HasCornerRadius(shape.Kind)
            && (shape.CornerRadius ?? DiagramFigures.DefaultCornerRadius(shape.Kind)) != corner;

        if (_drag is null && _diagram.Shapes.Any(IsChanged))
        {
            Commit(_diagram with
            {
                Shapes =
                [
                    .. _diagram.Shapes.Select(shape => IsChanged(shape)
                        ? shape with
                        {
                            CornerRadius = corner,
                            BottomCornerRadius = DiagramFigures.BottomCornerRadius(shape),
                        }
                        : shape),
                ],
            });
        }
    }

    /// <summary>The bottom corners only.</summary>
    private void OnBottomCornerRadiusEdited(object? sender, int radius)
    {
        int corner = Math.Max(radius, 0);

        bool IsChanged(DiagramShapeModel shape) => _selectedShapes.Contains(shape.Id)
            && DiagramFigures.HasCornerRadius(shape.Kind)
            && shape.BottomCornerRadius != corner;

        if (_drag is null && _diagram.Shapes.Any(IsChanged))
        {
            Commit(_diagram with
            {
                Shapes =
                [
                    .. _diagram.Shapes.Select(shape =>
                        IsChanged(shape) ? shape with { BottomCornerRadius = corner } : shape),
                ],
            });
        }
    }

    /// <summary>
    ///     The whole text of each selected shape and label in the format; or out of it when all of them already have it
    ///     everywhere. One step to undo.
    /// </summary>
    private void ToggleFormat(DiagramTextFormat format)
    {
        List<DiagramShapeModel> shapes =
            [.. _diagram.Shapes.Where(shape => _selectedShapes.Contains(shape.Id) && shape.Text.Length > 0)];
        List<DiagramConnectorModel> connectors =
        [
            .. _diagram.Connectors.Where(connector =>
                _selectedConnectors.Contains(connector.Id) && connector.Label.Length > 0),
        ];

        if (shapes.Count + connectors.Count == 0)
        {
            return;
        }

        bool hasIt = shapes.All(shape => DiagramRichText.HasFormat(shape.Text, shape.TextSpans, format))
            && connectors.All(connector => DiagramRichText.HasFormat(connector.Label, connector.LabelSpans, format));

        IReadOnlyList<DiagramTextSpanModel>? Changed(string text, IReadOnlyList<DiagramTextSpanModel>? spans) =>
            DiagramRichText.OrNull(DiagramRichText.WithFormat(text, spans, format, !hasIt));

        Commit(_diagram with
        {
            Shapes =
            [
                .. _diagram.Shapes.Select(shape => shapes.Contains(shape)
                    ? shape with { TextSpans = Changed(shape.Text, shape.TextSpans) }
                    : shape),
            ],
            Connectors =
            [
                .. _diagram.Connectors.Select(connector => connectors.Contains(connector)
                    ? connector with { LabelSpans = Changed(connector.Label, connector.LabelSpans) }
                    : connector),
            ],
        });
    }

    /// <summary>The text of the one thing selected: a shape's text or a connector's label.</summary>
    private void OnTextEdited(object? sender, string text)
    {
        if (_drag is not null || _selectedShapes.Count + _selectedConnectors.Count != 1)
        {
            return;
        }

        string edited = text.ReplaceLineEndings("\n").TrimEnd();

        Commit(new DiagramModel(
            [
                .. _diagram.Shapes.Select(shape => _selectedShapes.Contains(shape.Id) && shape.Text != edited
                    ? shape with
                    {
                        Text = edited,
                        TextSpans = DiagramRichText.OrNull(
                            DiagramRichText.AfterEdit(shape.Text, shape.TextSpans, edited)),
                    }
                    : shape),
            ],
            [
                .. _diagram.Connectors.Select(connector => _selectedConnectors.Contains(connector.Id) && connector.Label != edited
                    ? connector with
                    {
                        Label = edited,
                        LabelSpans = DiagramRichText.OrNull(
                            DiagramRichText.AfterEdit(connector.Label, connector.LabelSpans, edited)),
                    }
                    : connector),
            ]),
            onlyIfChanged: true);
    }

    private void OnDashedSelected(object? sender, bool isDashed)
    {
        ChangeStyle(style => style with { Dashed = isDashed });
    }

    private void OnFontSizeSelected(object? sender, int size)
    {
        ChangeStyle(style => style with { FontSize = Math.Clamp(size, MinFontSize, MaxFontSize) });
    }

    private void OnTextOutlineWidthSelected(object? sender, int width)
    {
        ChangeStyle(style => style with { TextOutlineWidth = Math.Clamp(width, 0, DiagramStyleModel.MaxTextOutlineWidth) });
    }

    private void OnTextAlignmentSelected(object? sender, DiagramTextAlignment alignment)
    {
        if (Enum.IsDefined(alignment))
        {
            ChangeStyle(style => style with { TextAlignment = alignment });
        }
    }

    private void OnFontFamilySelected(object? sender, string fontFamily)
    {
        if (!string.IsNullOrWhiteSpace(fontFamily))
        {
            ChangeStyle(style => style with { FontFamily = fontFamily });
        }
    }

    private void OnRoutingSelected(object? sender, DiagramRouting routing)
    {
        _routing = routing;

        // Choosing how it should go takes the place of the bends
        // A line on its own stays straight
        ChangeSelectedConnectors(connector =>
            connector.IsLine ? connector : connector with { Routing = routing, Bends = null });
        SaveSettings();
    }

    private void OnArrowHeadsSelected(object? sender, DiagramArrowHeads heads)
    {
        _heads = heads;
        ChangeSelectedConnectors(connector => connector with { Heads = heads });
        SaveSettings();
    }

    /// <summary>A line made level or upright is turned that way around its start, keeping its length.</summary>
    private void OnLineDirectionSelected(object? sender, DiagramLineDirection direction)
    {
        if (_drag is not null || !Enum.IsDefined(direction))
        {
            return;
        }

        Commit(
            _diagram with
            {
                Connectors =
                [
                    .. _diagram.Connectors.Select(connector =>
                        connector.IsLine && _selectedConnectors.Contains(connector.Id)
                            ? (connector with { Direction = direction }).Aligned()
                            : connector),
                ],
            },
            onlyIfChanged: true);
    }

    private void ChangeSelectedConnectors(Func<DiagramConnectorModel, DiagramConnectorModel> change)
    {
        if (_selectedConnectors.Count > 0)
        {
            Commit(_diagram with
            {
                Connectors =
                [
                    .. _diagram.Connectors.Select(connector =>
                        _selectedConnectors.Contains(connector.Id) ? change(connector) : connector),
                ],
            });
        }

        ShowSelectedStyle();
    }

    /// <summary>Changes what comes next, and what's selected.</summary>
    private void ChangeStyle(Func<DiagramStyleModel, DiagramStyleModel> change)
    {
        _style = change(_style);
        SaveSettings();

        if (_selectedShapes.Count > 0 || _selectedConnectors.Count > 0)
        {
            Commit(new DiagramModel(
                [
                    .. _diagram.Shapes.Select(shape =>
                        _selectedShapes.Contains(shape.Id) ? shape with { Style = change(shape.Style) } : shape),
                ],
                [
                    .. _diagram.Connectors.Select(connector => _selectedConnectors.Contains(connector.Id)
                        ? connector with { Style = change(connector.Style) }
                        : connector),
                ]));
        }

        ShowSelectedStyle();
    }

    private void CopyStyle()
    {
        if (_selectedShapes.Count > 0 || _selectedConnectors.Count > 0)
        {
            _copiedStyle = SelectedStyle();
            _copiedStyleHasFill = _selectedShapes.Count > 0;
        }
    }

    /// <summary>
    ///     The whole copied style, except that a style copied from a connector, which has no fill, leaves a shape's
    ///     fill as it is.
    /// </summary>
    private void PasteStyle()
    {
        if (_copiedStyle is not DiagramStyleModel copied)
        {
            return;
        }

        // A connector's style has no fills to give, so the shape keeps its own
        ChangeStyle(style => _copiedStyleHasFill
            ? copied
            : copied with { Fill = style.Fill, TitleFill = style.TitleFill });
    }

    private void SelectionChanged() => ShowSelectedStyle();

    private DiagramStyleModel SelectedStyle()
    {
        return _diagram.Shapes.FirstOrDefault(shape => _selectedShapes.Contains(shape.Id))?.Style
            ?? _diagram.Connectors.FirstOrDefault(connector => _selectedConnectors.Contains(connector.Id))?.Style
            ?? _style;
    }

    private void ShowSelectedStyle()
    {
        DiagramConnectorModel? connector = _diagram.Connectors
            .FirstOrDefault(c => _selectedConnectors.Contains(c.Id));

        _view.ShowStyle(SelectedStyle(), connector?.Routing ?? _routing, connector?.Heads ?? _heads);
    }

    /// <summary>The new diagram becomes current; undo goes back to the one before it, redo is lost.</summary>
    /// <param name="diagram">The new diagram.</param>
    /// <param name="onlyIfChanged">True to leave nothing to undo when its parts are all the same as now.</param>
    private void Commit(DiagramModel diagram, bool onlyIfChanged = false)
    {
        if (onlyIfChanged
            && diagram.Shapes.SequenceEqual(_diagram.Shapes)
            && diagram.Connectors.SequenceEqual(_diagram.Connectors))
        {
            return;
        }

        _undo.Push(_diagram);
        _redo.Clear();

        // A change made from the shapes and connectors keeps the theme; nothing takes a theme away
        _diagram = diagram.Theme is null ? diagram with { Theme = _diagram.Theme } : diagram;
        Refresh();
    }

    private void EndDrag()
    {
        _drag = null;
        _preview = null;
        _guides = [];
        _band = null;
        _draftConnector = null;
        _connectTarget = null;
        _connectTargetSide = null;
    }

    private void ClearSelection()
    {
        _selectedShapes.Clear();
        _selectedConnectors.Clear();
    }

    /// <summary>The shape's id, and those of the rest of its group.</summary>
    /// <summary>
    ///     What align and distribute move: each group of the selection as one, and each shape in none; with the box
    ///     around it.
    /// </summary>
    private List<(IReadOnlyDictionary<int, Rectangle> Shapes, Rectangle Box)> SelectedUnits() =>
    [
        .. _diagram.Shapes
            .Where(shape => _selectedShapes.Contains(shape.Id))
            .GroupBy(shape => (shape.GroupId, Id: shape.GroupId is null ? shape.Id : 0))
            .Select(unit => (
                (IReadOnlyDictionary<int, Rectangle>)unit.ToDictionary(shape => shape.Id, shape => shape.Bounds),
                UnionOf(unit.Select(shape => shape.Bounds)))),
    ];

    /// <summary>
    ///     Aligns the selected shapes and groups with the box around them all, or spreads them evenly between the two
    ///     outermost, which stay. Each moves as a whole, without what it has locked; one step to undo.
    /// </summary>
    private void Arrange(DiagramCommand command)
    {
        bool isDistribute = command is DiagramCommand.DistributeHorizontally or DiagramCommand.DistributeVertically;
        List<(IReadOnlyDictionary<int, Rectangle> Shapes, Rectangle Box)> units = SelectedUnits();

        if (units.Count < (isDistribute ? 3 : 2))
        {
            return;
        }

        Rectangle all = UnionOf(units.Select(unit => unit.Box));
        List<(IReadOnlyDictionary<int, Rectangle> Shapes, Size Delta)> moves = isDistribute
            ? Distributed(units, command == DiagramCommand.DistributeHorizontally)
            : [.. units.Select(unit => (unit.Shapes, AlignDelta(command, unit.Box, all)))];
        DiagramModel arranged = _diagram;

        foreach ((IReadOnlyDictionary<int, Rectangle> shapes, Size delta) in moves)
        {
            // A container takes along what's in it
            (Dictionary<int, Rectangle> inside, HashSet<int> lines) = InsideOf([.. shapes.Keys]);
            Dictionary<int, Rectangle> moving = new(shapes);

            foreach ((int id, Rectangle bounds) in inside)
            {
                moving.TryAdd(id, bounds);
            }

            arranged = WithBounds(arranged, moving, bounds => Moved(bounds, delta), bend => bend + delta, lines);
        }

        Commit(arranged, onlyIfChanged: true);
    }

    private static Size AlignDelta(DiagramCommand command, Rectangle box, Rectangle all) => command switch
    {
        DiagramCommand.AlignLeft => new Size(all.Left - box.Left, 0),
        DiagramCommand.AlignCenter => new Size(CenterOf(all).X - CenterOf(box).X, 0),
        DiagramCommand.AlignRight => new Size(all.Right - box.Right, 0),
        DiagramCommand.AlignTop => new Size(0, all.Top - box.Top),
        DiagramCommand.AlignMiddle => new Size(0, CenterOf(all).Y - CenterOf(box).Y),
        _ => new Size(0, all.Bottom - box.Bottom),
    };

    /// <summary>In order of their middles, the space between each two the same; the first and last stay.</summary>
    private static List<(IReadOnlyDictionary<int, Rectangle> Shapes, Size Delta)> Distributed(
        List<(IReadOnlyDictionary<int, Rectangle> Shapes, Rectangle Box)> units,
        bool isHorizontal)
    {
        static int Start(Rectangle box, bool isHorizontal) => isHorizontal ? box.Left : box.Top;
        static int Length(Rectangle box, bool isHorizontal) => isHorizontal ? box.Width : box.Height;

        var ordered = units
            .OrderBy(unit => (Start(unit.Box, isHorizontal) * 2) + Length(unit.Box, isHorizontal))
            .ToList();
        Rectangle first = ordered[0].Box;
        Rectangle last = ordered[^1].Box;
        int span = Start(last, isHorizontal) + Length(last, isHorizontal) - Start(first, isHorizontal);
        double gap = (span - ordered.Sum(unit => Length(unit.Box, isHorizontal))) / (double)(ordered.Count - 1);
        double next = Start(first, isHorizontal);
        List<(IReadOnlyDictionary<int, Rectangle> Shapes, Size Delta)> moves = [];

        foreach ((IReadOnlyDictionary<int, Rectangle> shapes, Rectangle box) in ordered)
        {
            int move = (int)Math.Round(next) - Start(box, isHorizontal);

            moves.Add((shapes, isHorizontal ? new Size(move, 0) : new Size(0, move)));
            next += Length(box, isHorizontal) + gap;
        }

        return moves;
    }

    private IEnumerable<int> WithGroup(DiagramShapeModel shape) => shape.GroupId is null
        ? [shape.Id]
        : _diagram.Shapes.Where(other => other.GroupId == shape.GroupId).Select(other => other.Id);

    private Dictionary<int, Rectangle> SelectedBounds() => _diagram.Shapes
        .Where(shape => _selectedShapes.Contains(shape.Id))
        .ToDictionary(shape => shape.Id, shape => shape.Bounds);

    private Rectangle SelectedBox() => UnionOf(SelectedBounds().Values);

    /// <summary>The ids of the selected lines on their own.</summary>
    private HashSet<int> SelectedLines() =>
        [.. _diagram.Connectors.Where(c => c.IsLine && _selectedConnectors.Contains(c.Id)).Select(c => c.Id)];

    /// <summary>Around everything a move takes along: what's selected, and what's in the containers.</summary>
    private Rectangle MoveBox()
    {
        HashSet<int> lines = MovingLines();

        return UnionOf(MovingBounds().Values.Concat(_diagram.Connectors
            .Where(c => lines.Contains(c.Id))
            .Select(c => FromCorners(c.FromPoint!.Value, c.ToPoint!.Value))));
    }

    /// <summary>The selected shapes, and those wholly inside the selected containers, which go with them.</summary>
    private Dictionary<int, Rectangle> MovingBounds()
    {
        Dictionary<int, Rectangle> moving = SelectedBounds();

        foreach ((int id, Rectangle bounds) in InsideOf(moving.Keys).Shapes)
        {
            moving[id] = bounds;
        }

        return moving;
    }

    /// <summary>The selected lines, and the lines wholly inside the selected containers.</summary>
    private HashSet<int> MovingLines() => [.. SelectedLines(), .. InsideOf(_selectedShapes).Lines];

    /// <summary>
    ///     What's wholly inside the containers among the shapes, but not those shapes: other shapes, nested
    ///     containers and what's in them too, and lines on their own.
    /// </summary>
    private (Dictionary<int, Rectangle> Shapes, HashSet<int> Lines) InsideOf(IReadOnlyCollection<int> shapeIds)
    {
        List<Rectangle> boxes =
        [
            .. _diagram.Shapes
                .Where(shape => shape.Kind == DiagramShapeKind.Container && shapeIds.Contains(shape.Id))
                .Select(shape => shape.Bounds),
        ];

        if (boxes.Count == 0)
        {
            return ([], []);
        }

        return (
            _diagram.Shapes
                .Where(shape => !shapeIds.Contains(shape.Id) && boxes.Any(box => box.Contains(shape.Bounds)))
                .ToDictionary(shape => shape.Id, shape => shape.Bounds),
            [
                .. _diagram.Connectors
                    .Where(c => c is { FromPoint: Point from, ToPoint: Point to }
                        && boxes.Any(box => box.Contains(from) && box.Contains(to)))
                    .Select(c => c.Id),
            ]);
    }

    private List<Rectangle> BoundsOfOthers(IReadOnlyDictionary<int, Rectangle> moving) =>
        [.. _diagram.Shapes.Where(shape => !moving.ContainsKey(shape.Id)).Select(shape => shape.Bounds)];

    /// <summary>
    ///     Changes where some shapes are, except what they have locked, and moves the lines given with them. The bends
    ///     of a connector go along when both of its shapes do, without locks, so a part of a diagram keeps its look
    ///     when it's moved or scaled as a whole.
    /// </summary>
    /// <param name="lines">The ids of the lines on their own whose ends go as the bends do.</param>
    private static DiagramModel WithBounds(
        DiagramModel diagram,
        IReadOnlyDictionary<int, Rectangle> originals,
        Func<Rectangle, Rectangle> change,
        Func<Point, Point> changeBend,
        IReadOnlySet<int>? lines = null)
    {
        HashSet<int> free =
        [
            .. diagram.Shapes
                .Where(shape => originals.ContainsKey(shape.Id)
                    && (shape.Locks & DiagramLock.Bounds) == DiagramLock.None)
                .Select(shape => shape.Id),
        ];

        return new DiagramModel(
            [
                .. diagram.Shapes.Select(shape => originals.TryGetValue(shape.Id, out Rectangle bounds)
                    ? shape with { Bounds = Locked(bounds, change(bounds), shape.Locks) }
                    : shape),
            ],
            [
                .. diagram.Connectors.Select(connector => connector switch
                {
                    { FromPoint: Point from, ToPoint: Point to } when lines?.Contains(connector.Id) == true =>
                        MovedLine(connector, changeBend(from), changeBend(to)),
                    { Bends: { } bends, FromShapeId: int fromId, ToShapeId: int toId }
                        when free.Contains(fromId) && free.Contains(toId) =>
                        connector with { Bends = [.. bends.Select(changeBend)] },
                    _ => connector,
                }),
            ]);
    }

    /// <summary>A line with its ends here; as it was if they'd meet, since then it would have no direction.</summary>
    private static DiagramConnectorModel MovedLine(DiagramConnectorModel line, Point from, Point to) =>
        from == to ? line : (line with { FromPoint = from, ToPoint = to }).Aligned();

    /// <summary>
    ///     The changed bounds with the locked parts as they were. A size kept as it was, or made to keep the
    ///     proportions, keeps the edge that didn't move where it was: scaling a shape of locked width by its left edge
    ///     leaves it, rather than sliding it.
    /// </summary>
    private static Rectangle Locked(Rectangle original, Rectangle changed, DiagramLock locks)
    {
        if (locks == DiagramLock.None)
        {
            return changed;
        }

        bool isWidthLocked = locks.HasFlag(DiagramLock.Width);
        bool isHeightLocked = locks.HasFlag(DiagramLock.Height);
        int width = isWidthLocked ? original.Width : changed.Width;
        int height = isHeightLocked ? original.Height : changed.Height;

        if (locks.HasFlag(DiagramLock.AspectRatio))
        {
            (width, height) = Proportional(original.Size, width, height, isWidthLocked || isHeightLocked);
        }

        // Only the left or top edge moved, so the other stays put
        bool keepsRight = changed.Right == original.Right && changed.Left != original.Left;
        bool keepsBottom = changed.Bottom == original.Bottom && changed.Top != original.Top;
        int x = locks.HasFlag(DiagramLock.X) ? original.X : keepsRight ? original.Right - width : changed.X;
        int y = locks.HasFlag(DiagramLock.Y) ? original.Y : keepsBottom ? original.Bottom - height : changed.Y;

        return new Rectangle(x, y, width, height);
    }

    /// <summary>
    ///     A size with the original's proportions: scaled as the side that changed was, or the one that changed most;
    ///     the original's if one side can't change.
    /// </summary>
    private static (int Width, int Height) Proportional(Size original, int width, int height, bool isFixed)
    {
        if (isFixed || original.Width <= 0 || original.Height <= 0)
        {
            return (original.Width, original.Height);
        }

        double scaleX = width / (double)original.Width;
        double scaleY = height / (double)original.Height;
        double scale = width == original.Width ? scaleY
            : height == original.Height ? scaleX
            : Math.Max(scaleX, scaleY);

        return (
            Math.Max(1, (int)Math.Round(original.Width * scale)),
            Math.Max(1, (int)Math.Round(original.Height * scale)));
    }

    private Point Snap(Point point)
    {
        DiagramSnapModel snap = DiagramSnapper.SnapPoint(
            point,
            [.. _diagram.Shapes.Select(shape => shape.Bounds)],
            SnapTolerance());

        point.Offset(snap.DeltaX, snap.DeltaY);

        return point;
    }

    private int SnapTolerance() => Math.Max(1, (int)Math.Round(SnapDistance / _viewport.Zoom));

    private int CanvasFontSize(int fontSize) => Math.Max(1, (int)Math.Round(fontSize * _viewport.Zoom));

    private DiagramCursor HoverCursor(Point point)
    {
        if (_tool != DiagramTool.Select
            || AnchorUnder(point).Side is not null
            || ConnectorEndUnder(point).End is not null)
        {
            return DiagramCursor.Cross;
        }

        if (SegmentDragAt(point) is { } segment)
        {
            return segment.Route[segment.Segment].Y == segment.Route[segment.Segment + 1].Y
                ? DiagramCursor.SizeVertical
                : DiagramCursor.SizeHorizontal;
        }

        if (_selectedShapes.Count > 0
            && DiagramHitTester.HandleAt(SelectedBox(), point, GrabDistance / _viewport.Zoom) is DiagramHandle handle)
        {
            return CursorOf(handle);
        }

        bool canMove = DiagramHitTester.LineAt(_diagram, point, GrabDistance / _viewport.Zoom) is not null
            || DiagramHitTester.ShapeAt(_diagram, point) is not null;

        return canMove ? DiagramCursor.Move : DiagramCursor.Default;
    }

    private void ShowCursor(DiagramCursor cursor)
    {
        if (cursor != _cursor)
        {
            _cursor = cursor;
            _view.ShowCursor(cursor);
        }
    }

    private void Refresh()
    {
        DiagramModel shown = Shown;

        // Only what exists: a change may have just removed a shape that's still selected
        List<Rectangle> selected =
        [
            .. shown.Shapes.Where(shape => _selectedShapes.Contains(shape.Id)).Select(shape => shape.Bounds),
        ];
        Rectangle? handleBounds = selected.Count > 0 ? UnionOf(selected) : null;

        _view.ShowScene(new DiagramSceneModel(
            shown,
            _selectedShapes.ToHashSet(),
            _selectedConnectors.ToHashSet(),
            _viewport,
            handleBounds,
            _guides,
            _band,
            _draftConnector,
            _connectTarget,
            AnchorsOf(shown),
            ConnectorEndsOf(shown)));
        ShowProperties(shown);
        ShowStatus(shown);
        ShowLayers();
        ShowPalette();
        _view.ShowUndoRedo(_undo.Count > 0, _redo.Count > 0);
        _view.ShowDocument(Name, HasChanges);
        _view.ShowZoom((int)Math.Round(_viewport.Zoom * 100));
    }

    /// <summary>Where the mouse and the selection are; only when it changed, as this runs on every mouse move.</summary>
    private void ShowStatus(DiagramModel diagram)
    {
        Point pointer = _viewport.ToDiagram(_lastPointer);
        (string Pointer, string Selection) status = (
            _isPointerOnCanvas ? string.Create(CultureInfo.InvariantCulture, $"{pointer.X}, {pointer.Y}") : string.Empty,
            SelectionText(diagram));

        if (status != _shownStatus)
        {
            _shownStatus = status;
            _view.ShowStatus(status.Pointer, status.Selection);
        }
    }

    /// <summary>E.g. "2 shapes, 1 line: 120 × 60 at 40, 80"; the box is around the shapes and the lines.</summary>
    private string SelectionText(DiagramModel diagram)
    {
        List<Rectangle> shapes =
            [.. diagram.Shapes.Where(shape => _selectedShapes.Contains(shape.Id)).Select(shape => shape.Bounds)];
        List<DiagramConnectorModel> connectors =
            [.. diagram.Connectors.Where(connector => _selectedConnectors.Contains(connector.Id))];
        int lines = connectors.Count(connector => connector.IsLine);
        List<string> counts = [];

        static string Counted(int count, string name) => count == 1 ? $"1 {name}" : $"{count} {name}s";

        if (shapes.Count > 0)
        {
            counts.Add(Counted(shapes.Count, "shape"));
        }

        if (lines > 0)
        {
            counts.Add(Counted(lines, "line"));
        }

        if (connectors.Count > lines)
        {
            counts.Add(Counted(connectors.Count - lines, "connector"));
        }

        List<Rectangle> boxes =
        [
            .. shapes,
            .. connectors
                .Where(connector => connector is { FromPoint: not null, ToPoint: not null })
                .Select(connector => BoxOf(connector.FromPoint!.Value, connector.ToPoint!.Value)),
        ];

        if (boxes.Count == 0)
        {
            return string.Join(", ", counts);
        }

        Rectangle box = UnionOf(boxes);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{string.Join(", ", counts)}: {box.Width} × {box.Height} at {box.X}, {box.Y}");
    }

    private static Rectangle BoxOf(Point from, Point to) => Rectangle.FromLTRB(
        Math.Min(from.X, to.X),
        Math.Min(from.Y, to.Y),
        Math.Max(from.X, to.X),
        Math.Max(from.Y, to.Y));

    /// <summary>
    ///     Selects the shape or connector with the text, as asked, in reading order, top to bottom, then left to right,
    ///     and brings it into view when it's out of it.
    /// </summary>
    private void OnFindRequested(object? sender, DiagramFindModel find)
    {
        if (_drag is not null)
        {
            return;
        }

        string text = find.Text.Trim();
        List<(DiagramShapeModel? Shape, DiagramConnectorModel? Connector, Rectangle Box)> found = Found(text);
        bool isSameText = string.Equals(text, _findText, StringComparison.OrdinalIgnoreCase) && _findIndex >= 0;

        _findText = text;

        if (text.Length == 0 || found.Count == 0)
        {
            _findIndex = -1;
            _view.ShowFindResult(text.Length == 0 ? string.Empty : "Not found");

            return;
        }

        _findIndex = (find.Step, isSameText) switch
        {
            (DiagramFindStep.Next, true) => (_findIndex + 1) % found.Count,
            (DiagramFindStep.Previous, true) => (_findIndex + found.Count - 1) % found.Count,
            (DiagramFindStep.Previous, false) => found.Count - 1,
            _ => 0,
        };

        (DiagramShapeModel? shape, DiagramConnectorModel? connector, Rectangle box) = found[_findIndex];

        ClearSelection();

        if (shape is not null)
        {
            _selectedShapes.UnionWith(WithGroup(shape));
        }
        else
        {
            _selectedConnectors.Add(connector!.Id);
        }

        BringIntoView(box, find.CanvasSize);
        SelectionChanged();
        Refresh();
        _view.ShowFindResult(string.Create(CultureInfo.InvariantCulture, $"{_findIndex + 1} of {found.Count}"));
    }

    /// <summary>The shapes whose text, and the connectors whose label, has the text, in any case.</summary>
    private List<(DiagramShapeModel? Shape, DiagramConnectorModel? Connector, Rectangle Box)> Found(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        IEnumerable<(DiagramShapeModel?, DiagramConnectorModel?, Rectangle)> shapes = _diagram.Shapes
            .Where(shape => shape.Text.Contains(text, StringComparison.OrdinalIgnoreCase)
                || shape.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
            .Select(shape => ((DiagramShapeModel?)shape, (DiagramConnectorModel?)null, shape.Bounds));
        IEnumerable<(DiagramShapeModel?, DiagramConnectorModel?, Rectangle)> connectors = _diagram.Connectors
            .Where(connector => connector.Label.Contains(text, StringComparison.OrdinalIgnoreCase)
                || connector.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
            .Select(connector => (Path: DiagramRouter.PathOf(connector, _diagram), Connector: connector))
            .Where(item => item.Path is { Points.Count: > 0 })
            .Select(item =>
                ((DiagramShapeModel?)null, (DiagramConnectorModel?)item.Connector, BoxOf(item.Path!.Points)));

        return [.. shapes.Concat(connectors).OrderBy(item => item.Item3.Y).ThenBy(item => item.Item3.X)];
    }

    private static Rectangle BoxOf(IReadOnlyList<PointF> points) => Rectangle.FromLTRB(
        (int)Math.Floor(points.Min(point => point.X)),
        (int)Math.Floor(points.Min(point => point.Y)),
        (int)Math.Ceiling(points.Max(point => point.X)),
        (int)Math.Ceiling(points.Max(point => point.Y)));

    /// <summary>Moves the view so the box is in the middle of the canvas, unless it's all in view already.</summary>
    private void BringIntoView(Rectangle box, Size canvasSize)
    {
        Point topLeft = _viewport.ToCanvas(box.Location);
        Point bottomRight = _viewport.ToCanvas(new Point(box.Right, box.Bottom));
        var canvas = new Rectangle(Point.Empty, canvasSize);

        if (canvasSize.IsEmpty || (canvas.Contains(topLeft) && canvas.Contains(bottomRight)))
        {
            return;
        }

        var middle = new PointF(box.X + (box.Width / 2f), box.Y + (box.Height / 2f));

        _viewport = _viewport with
        {
            Pan = new Point(
                (canvasSize.Width / 2) - (int)Math.Round(middle.X * _viewport.Zoom),
                (canvasSize.Height / 2) - (int)Math.Round(middle.Y * _viewport.Zoom)),
        };
    }

    /// <summary>Only when they changed, since this runs on every mouse move and the fields shouldn't flicker.</summary>
    private void ShowProperties(DiagramModel diagram)
    {
        DiagramPropertiesModel properties = PropertiesOf(diagram);

        if (properties != _shownProperties)
        {
            _shownProperties = properties;
            _view.ShowProperties(properties);
        }
    }

    /// <summary>
    ///     What applies to everything selected; a value that differs among them is taken from the first one.
    /// </summary>
    private DiagramPropertiesModel PropertiesOf(DiagramModel diagram)
    {
        List<DiagramShapeModel> shapes = [.. diagram.Shapes.Where(shape => _selectedShapes.Contains(shape.Id))];
        List<DiagramConnectorModel> connectors =
        [
            .. diagram.Connectors.Where(connector => _selectedConnectors.Contains(connector.Id)),
        ];

        if (shapes.Count == 0 && connectors.Count == 0)
        {
            return new DiagramPropertiesModel("Nothing selected");
        }

        DiagramStyleModel style = shapes.FirstOrDefault()?.Style ?? connectors[0].Style;

        // A text and an image have no outline, so no fill or border either
        List<DiagramShapeModel> outlined =
            [.. shapes.Where(shape => shape.Kind is not (DiagramShapeKind.Text or DiagramShapeKind.Image))];
        bool hasLine = outlined.Count > 0 || connectors.Count > 0;
        bool hasCorners = connectors.Count == 0 && shapes.All(shape => DiagramFigures.HasCornerRadius(shape.Kind));

        int? corner = hasCorners ? DiagramFigures.TopCornerRadius(shapes[0]) : null;
        int? bottomCorner = hasCorners ? DiagramFigures.BottomCornerRadius(shapes[0]) : null;

        string? text = null;
        string? name = null;

        if (shapes.Count + connectors.Count == 1)
        {
            text = shapes.Count == 1 ? shapes[0].Text : connectors[0].Label;
            name = (shapes.Count == 1 ? shapes[0].Name : connectors[0].Name) ?? string.Empty;
        }

        return new DiagramPropertiesModel(
            TitleOf(shapes, connectors),
            shapes.Count > 0 ? UnionOf(shapes.Select(shape => shape.Bounds)) : null,
            corner,
            outlined.Count > 0 ? outlined[0].Style.Fill : null,
            hasLine ? style.Line : null,
            hasLine ? style.LineWidth : null,
            hasLine ? style.Dashed : null,
            style.TextColor,
            style.FontSize,
            text,

            // A line on its own is always straight
            connectors.FirstOrDefault(connector => !connector.IsLine)?.Routing,
            connectors.Count > 0 ? connectors[0].Heads : null,
            style.FontFamily,
            style.TextOutlineWidth,
            style.TextOutlineColor,
            shapes.Count > 0 ? shapes.Aggregate(DiagramLock.All, (locks, shape) => locks & shape.Locks) : null,
            connectors.FirstOrDefault(connector => connector.IsLine)?.Direction,
            shapes.Any(shape => shape.Kind == DiagramShapeKind.Image),
            style.TextAlignment,
            shapes.FirstOrDefault(shape => shape.Kind == DiagramShapeKind.Container)?.Style.TitleFill,
            bottomCorner,
            name);
    }

    private static string TitleOf(List<DiagramShapeModel> shapes, List<DiagramConnectorModel> connectors)
    {
        int count = shapes.Count + connectors.Count;

        if (count > 1)
        {
            return connectors.Count == 0 ? $"{count} shapes" : $"{count} objects";
        }

        if (connectors.Count == 1)
        {
            return connectors[0].IsLine ? "Line" : "Connector";
        }

        return shapes[0].Kind switch
        {
            DiagramShapeKind.RoundedRectangle => "Rounded rectangle",
            DiagramShapeKind kind => kind.ToString(),
        };
    }

    /// <summary>The circles of the shape a connector is being dragged to, or else of the one under the mouse.</summary>
    private DiagramAnchorsModel? AnchorsOf(DiagramModel diagram)
    {
        int? shapeId = _connectTarget ?? _hoverShape;
        DiagramSide? highlighted = _connectTarget is null ? _hoverSide : _connectTargetSide;

        if (diagram.Shapes.FirstOrDefault(shape => shape.Id == shapeId) is not { } shape)
        {
            return null;
        }

        float offset = AnchorOffset / _viewport.Zoom;

        return new DiagramAnchorsModel(
            [.. Enum.GetValues<DiagramSide>().Select(side => DiagramHitTester.AnchorCenter(shape, side, offset))],
            highlighted is DiagramSide side ? DiagramHitTester.AnchorCenter(shape, side, offset) : null);
    }

    private List<PointF> ConnectorEndsOf(DiagramModel diagram)
    {
        List<PointF> ends = [];

        foreach (DiagramConnectorModel connector in diagram.Connectors.Where(c => _selectedConnectors.Contains(c.Id)))
        {
            if (DiagramRouter.PathOf(connector, diagram) is { } path)
            {
                ends.Add(DiagramHitTester.EndPoint(path, DiagramConnectorEnd.Start));
                ends.Add(DiagramHitTester.EndPoint(path, DiagramConnectorEnd.End));
            }
        }

        return ends;
    }

    private abstract record Drag;

    private sealed record PanDrag(Point Last) : Drag;

    /// <param name="Lines">The ids of the selected lines on their own, which go along.</param>
    private sealed record MoveDrag(
        Point Start,
        IReadOnlyDictionary<int, Rectangle> Originals,
        Rectangle Box,
        IReadOnlySet<int> Lines)
        : Drag;

    /// <param name="Lines">The ids of the selected lines on their own, which go along.</param>
    private sealed record ScaleDrag(
        Point Start,
        DiagramHandle Handle,
        IReadOnlyDictionary<int, Rectangle> Originals,
        Rectangle Box,
        IReadOnlySet<int> Lines)
        : Drag;

    /// <param name="Start">Where the line starts.</param>
    private sealed record LineDrag(Point Start) : Drag;

    /// <param name="Start">Where the drag started.</param>
    /// <param name="ConnectorId">The line being changed.</param>
    /// <param name="End">The end being dragged.</param>
    private sealed record LineEndDrag(Point Start, int ConnectorId, DiagramConnectorEnd End) : Drag;

    private sealed record CreateDrag(DiagramShapeKind Kind, Point Start) : Drag;

    private sealed record ConnectDrag(int FromShapeId, DiagramSide? FromSide) : Drag;

    private sealed record ReconnectDrag(Point Start, int ConnectorId, DiagramConnectorEnd End) : Drag;

    /// <param name="Start">Where the drag started.</param>
    /// <param name="ConnectorId">The connector being changed.</param>
    /// <param name="Route">Its whole line before the drag.</param>
    /// <param name="Segment">The index of the point that starts the piece being dragged.</param>
    private sealed record SegmentDrag(Point Start, int ConnectorId, IReadOnlyList<PointF> Route, int Segment) : Drag;

    private sealed record BandDrag(Point Start) : Drag;
}
