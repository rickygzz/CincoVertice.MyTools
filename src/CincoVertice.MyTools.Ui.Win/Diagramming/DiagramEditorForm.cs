using System.ComponentModel;
using System.Diagnostics;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Diagramming.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;

namespace CincoVertice.MyTools.Ui.Win.Diagramming;

/// <summary>
///     Diagram editor view: the canvas, with the shapes on a bar at the left and the style and file commands on a bar
///     at the top. <see cref="Ui.Diagramming.Presenters.DiagramEditorPresenter"/> does the work; the form only shows
///     what it's given and passes on what the user does.
/// </summary>
public partial class DiagramEditorForm : Form, IDiagramEditorView
{
    private const string WindowTitle = "Diagram editor";

    // How many pixels the arrow keys move the selection; with Shift, one
    private const int NudgeStep = 10;

    // The pictures in the images panel, in logical pixels
    private const int ThumbnailSize = 48;

    // What an image dragged from the images panel carries: its path in the catalog
    private const string CatalogImageFormat = "CincoVertice.MyTools.DiagramCatalogImage";

    // Where the cloud makers offer their architecture icons, for the images panel's links
    private static readonly (string Name, string Page)[] OfficialIconPages =
    [
        ("Azure", "https://learn.microsoft.com/azure/architecture/icons/"),
        ("AWS", "https://aws.amazon.com/architecture/icons/"),
    ];

    // What rows dragged in the layers panel carry: their ids, with commas between
    private const string LayersDragFormat = "CincoVertice.MyTools.DiagramLayers";

    // The name browsers, chats and Office give a PNG picture on the clipboard
    internal const string PngClipboardFormat = "PNG";

    private readonly int _iconSize;
    private readonly List<Image> _images = [];

    // Sizes the images panel's items, whose pictures are drawn by the form: it holds no image
    private readonly ImageList _thumbnails = new() { ColorDepth = ColorDepth.Depth32Bit };

    // Each color menu's Theme submenu, filled with the themes when the palette is shown
    private readonly List<ToolStripMenuItem> _themeMenus = [];

    // The color buttons' images: the current colors, replaced when they change
    private readonly Dictionary<ToolStripDropDownButton, Image> _swatches = [];

    private DiagramSceneModel? _scene;
    private IReadOnlyList<int> _lineWidths = [];
    private IReadOnlyList<int> _fontSizes = [];
    private IReadOnlyList<int> _textOutlineWidths = [];

    // Set while the toolbar is filled from the presenter, so that isn't taken as the user's choice
    private bool _isShowingChoices;

    // True while the text box holds a text the presenter is waiting for
    private bool _isEditingText;

    // The text box's font, made here for the size asked for
    private Font? _textFont;

    // Held with a drag to move the view instead
    private bool _isSpaceDown;

    // Set while a tab is shown as the presenter says, so that isn't taken as the user's choice
    private bool _isShowingTabs;

    // Set while the layers panel is filled from the presenter, so that isn't taken as the user's choice
    private bool _isShowingLayers;

    // True while a row of the layers panel is being renamed, so Esc is the rename's
    private bool _isRenamingLayer;

    // The layers panel's eye and lock, from Windows' icon font: Segoe Fluent Icons on Windows 11, MDL2 before
    private const string FluentIconFont = "Segoe Fluent Icons";
    private const string Mdl2IconFont = "Segoe MDL2 Assets";
    private const char LayerEyeGlyph = (char)0xE890;
    private const char LayerHideGlyph = (char)0xED1A;
    private const char LayerLockGlyph = (char)0xE72E;
    private const char LayerUnlockGlyph = (char)0xE785;

    private Font? _layerIconFont;

    // The row a drag in the layers panel would put the rows above; the number of rows for the bottom, -1 for none
    private int _layerDropIndex = -1;

    public DiagramEditorForm()
    {
        InitializeComponent();

        // 16 pixels at 100% scaling, drawn at the screen's size rather than stretched, so they stay sharp
        _iconSize = (int)Math.Round(16 * DeviceDpi / 96.0);
        ToolsStrip.ImageScalingSize = new Size(_iconSize, _iconSize);
        StyleStrip.ImageScalingSize = new Size(_iconSize, _iconSize);

        foreach (ToolStripButton button in ToolButtons())
        {
            button.Image = Keep(DiagramToolIcons.Create(ToolOf(button), _iconSize));
        }

        foreach (ToolStripDropDownButton button in ColorButtons())
        {
            ((ToolStripDropDownMenu)button.DropDown).ShowImageMargin = false;
        }

        AddColorMenuItems();

        string[] routings = ["Elbow", "Straight"];
        string[] arrows = ["Arrow at end", "Arrow at start", "Arrows at both", "No arrows"];

        RoutingComboBox.Items.AddRange(routings);
        ArrowHeadsComboBox.Items.AddRange(arrows);
        RoutingPropertyBox.Items.AddRange(routings);
        ArrowsPropertyBox.Items.AddRange(arrows);
        DirectionPropertyBox.Items.AddRange(["Free", "Horizontal", "Vertical"]);
        TextAlignPropertyBox.Items.AddRange(["Left", "Center", "Right"]);
        FontFamilyBox.Items.AddRange([.. DiagramFonts.Names()]);

        // Windows takes drops only on a single-threaded apartment, as the app's UI thread is; elsewhere, such as a
        // test, it would show an error when the canvas is created
        Canvas.AllowDrop = Thread.CurrentThread.GetApartmentState() == ApartmentState.STA;
        LayersListView.AllowDrop = Canvas.AllowDrop;

        FormExtensions.CenterOnCursorScreen(this);
        _thumbnails.ImageSize = new Size(LogicalToDeviceUnits(ThumbnailSize), LogicalToDeviceUnits(ThumbnailSize));
        ImagesListView.LargeImageList = _thumbnails;
        components?.Add(_thumbnails);

        FormExtensions.ActivateWhenShown(this);
        FindBox.TextBox.PlaceholderText = "Find (Ctrl+F)";

        // Each name links to where its maker offers its icons; they're the maker's, so they aren't in the app
        OfficialIconsLinkLabel.Links.Clear();

        foreach ((string name, string page) in OfficialIconPages)
        {
            int start = OfficialIconsLinkLabel.Text.LastIndexOf(name, StringComparison.Ordinal);

            OfficialIconsLinkLabel.Links.Add(start, name.Length, page);
        }
    }

    /// <summary>Whether the focus is in a panel or the find box, whose keys are their own.</summary>
    private bool IsInAPanel => SidePanelTabs.ContainsFocus || FindBox.Focused;

    public event EventHandler<DiagramPointerModel>? PointerPressed;

    public event EventHandler<DiagramPointerModel>? PointerMoved;

    public event EventHandler<DiagramPointerModel>? PointerReleased;

    public event EventHandler<DiagramPointerModel>? PointerDoubleClicked;

    public event EventHandler<Point>? PointerRightClicked;

    public event EventHandler? PointerLeft;

    public event EventHandler<DiagramWheelModel>? WheelTurned;

    public event EventHandler<DiagramTool>? ToolSelected;

    public event EventHandler<DiagramCommand>? CommandRequested;

    public event EventHandler<Size>? NudgeRequested;

    public event EventHandler<DiagramColorChoiceModel>? ColorSelected;

    public event EventHandler<DiagramColorTarget>? MoreColorsRequested;

    public event EventHandler<DiagramColorTarget>? ScreenColorRequested;

    public event EventHandler<string>? ThemeSelected;

    public event EventHandler<DiagramRecentModel>? RecentChosen;

    public event EventHandler<int>? LineWidthSelected;

    public event EventHandler<bool>? DashedSelected;

    public event EventHandler<int>? FontSizeSelected;

    public event EventHandler<string>? FontFamilySelected;

    public event EventHandler<int>? TextOutlineWidthSelected;

    public event EventHandler<DiagramRouting>? RoutingSelected;

    public event EventHandler<DiagramArrowHeads>? ArrowHeadsSelected;

    public event EventHandler<DiagramLineDirection>? LineDirectionSelected;

    public event EventHandler<DiagramTextAlignment>? TextAlignmentSelected;

    public event EventHandler<DiagramFilesDropModel>? FilesDropped;

    public event EventHandler<Rectangle>? BoundsEdited;

    public event EventHandler<DiagramLockChoiceModel>? LockToggled;

    public event EventHandler<int>? CornerRadiusEdited;

    public event EventHandler<int>? BottomCornerRadiusEdited;

    public event EventHandler<string>? TextEdited;

    public event EventHandler<string>? NameEdited;

    public event EventHandler<DiagramRichTextModel>? TextCommitted;

    public event EventHandler<bool>? PropertiesPanelToggled;

    public event EventHandler<bool>? ImagesPanelToggled;

    public event EventHandler<string>? ImageSearchChanged;

    public event EventHandler<string>? CatalogImageChosen;

    public event EventHandler<DiagramCatalogDropModel>? CatalogImageDropped;

    public event EventHandler? ImagesFolderRequested;

    public event EventHandler<DiagramFindModel>? FindRequested;

    public event EventHandler<DiagramBringIntoViewModel>? BringIntoViewRequested;

    public event EventHandler<bool>? LayersPanelToggled;

    public event EventHandler<IReadOnlyList<int>>? LayersSelected;

    public event EventHandler<DiagramLayerRenameModel>? LayerRenamed;

    public event EventHandler<DiagramLayerMoveModel>? LayersMoved;

    public event EventHandler<int>? LayerHiddenToggled;

    public event EventHandler<int>? LayerLockedToggled;

    public event EventHandler? AutosaveDue;

    public event EventHandler? WindowActivated;

    public event EventHandler<DiagramWindowSizeModel>? WindowClosed;

    event EventHandler<CancelEventArgs>? IDiagramEditorView.Closing
    {
        add => _closing += value;
        remove => _closing -= value;
    }

    private event EventHandler<CancelEventArgs>? _closing;

    public void ShowScene(DiagramSceneModel scene)
    {
        _scene = scene;
        Canvas.Invalidate();
    }

    public void ShowTool(DiagramTool tool)
    {
        foreach (ToolStripButton button in ToolButtons())
        {
            button.Checked = ToolOf(button) == tool;
        }
    }

    public void ShowPalette(DiagramPaletteModel palette)
    {
        foreach ((_, ThemeColorPaletteControl control, _) in ColorMenus())
        {
            control.Palette = palette;
        }

        foreach (ToolStripMenuItem themeMenu in _themeMenus)
        {
            List<ToolStripItem> old = [.. themeMenu.DropDownItems.Cast<ToolStripItem>()];

            themeMenu.DropDownItems.Clear();

            foreach (ToolStripItem item in old)
            {
                item.Dispose();
            }

            foreach (string theme in palette.Themes)
            {
                var item = new ToolStripMenuItem(theme) { Checked = theme == palette.Theme };

                item.Click += (_, _) =>
                {
                    Canvas.Focus();
                    ThemeSelected?.Invoke(this, theme);
                };
                themeMenu.DropDownItems.Add(item);
            }

            themeMenu.Text = $"Theme: {palette.Theme}";
        }
    }

    /// <summary>
    ///     The Recent submenu, numbered for the keyboard; made here, as its items change with the diagrams opened.
    /// </summary>
    public void ShowRecent(IReadOnlyList<DiagramRecentModel> items)
    {
        List<ToolStripItem> old = [.. RecentMenuItem.DropDownItems.Cast<ToolStripItem>()];

        RecentMenuItem.DropDownItems.Clear();

        foreach (ToolStripItem item in old)
        {
            item.Dispose();
        }

        for (int i = 0; i < items.Count; i++)
        {
            DiagramRecentModel recent = items[i];
            string number = i < 9 ? $"&{i + 1} " : string.Empty;
            var item = new ToolStripMenuItem(number + recent.Text.Replace("&", "&&")) { ToolTipText = recent.Path };

            item.Click += (_, _) =>
            {
                CommitText();
                Canvas.Focus();
                RecentChosen?.Invoke(this, recent);
            };
            RecentMenuItem.DropDownItems.Add(item);
        }

        if (items.Count == 0)
        {
            RecentMenuItem.DropDownItems.Add(new ToolStripMenuItem("No diagrams yet") { Enabled = false });
        }
    }

    public bool AskApplyTheme(string theme)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"Give the colors already in the diagram the {theme} theme's colors?\n\n"
            + "Each color picked from the old theme takes the new theme's color at the same place; other colors stay. "
            + $"Either way, what you draw next gets the {theme} theme's colors, and Undo takes it all back.",
            WindowTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        return answer == DialogResult.Yes;
    }

    /// <summary>Each color menu, with its palette and what its colors are for.</summary>
    private (ToolStripDropDownButton Menu, ThemeColorPaletteControl Palette, DiagramColorTarget Target)[] ColorMenus() =>
    [
        (FillDropDownButton, FillPalette, DiagramColorTarget.Fill),
        (TitleFillDropDownButton, TitleFillPalette, DiagramColorTarget.TitleFill),
        (LineDropDownButton, LinePalette, DiagramColorTarget.Line),
        (TextColorDropDownButton, TextPalette, DiagramColorTarget.Text),
        (TextOutlineDropDownButton, TextOutlinePalette, DiagramColorTarget.TextOutline),
    ];

    /// <summary>
    ///     Adds to every color menu, after More colors: Pick from the screen, a box to type a color's hex code, and the
    ///     Theme submenu. Made here, with their handlers, since all five menus get the same, each for its own colors.
    /// </summary>
    private void AddColorMenuItems()
    {
        foreach ((ToolStripDropDownButton menu, _, DiagramColorTarget target) in ColorMenus())
        {
            var pick = new ToolStripMenuItem("Pick from the screen...")
            {
                ToolTipText = "Freeze the screen and click a pixel, as the color picker (Ctrl+Alt+P) does",
            };
            var hex = new ToolStripTextBox
            {
                AutoSize = false,
                Width = LogicalToDeviceUnits(150),
                ToolTipText = "Type a color as #RRGGBB or #RGB, then Enter",
            };
            var themes = new ToolStripMenuItem("Theme")
            {
                ToolTipText = "The diagram's color theme: the colors offered first, and what new shapes get",
            };

            hex.TextBox.PlaceholderText = "#RRGGBB, then Enter";
            pick.Click += (_, _) => ScreenColorRequested?.Invoke(this, target);
            hex.KeyDown += (_, e) => HexBox_KeyDown(menu, hex, target, e);
            _themeMenus.Add(themes);
            menu.DropDownItems.AddRange([pick, hex, new ToolStripSeparator(), themes]);
        }
    }

    /// <summary>Enter uses the color typed, and closes the menu; something that isn't a color only beeps.</summary>
    private void HexBox_KeyDown(
        ToolStripDropDownButton menu,
        ToolStripTextBox box,
        DiagramColorTarget target,
        KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;

        if (!ColorText.TryParseHex(box.Text, out Color color))
        {
            System.Media.SystemSounds.Beep.Play();

            return;
        }

        box.Text = string.Empty;
        menu.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(target, color));
    }

    public void ShowSizes(IReadOnlyList<int> lineWidths, IReadOnlyList<int> fontSizes, IReadOnlyList<int> textOutlineWidths)
    {
        _lineWidths = lineWidths;
        _fontSizes = fontSizes;
        _textOutlineWidths = textOutlineWidths;
        _isShowingChoices = true;
        LineWidthComboBox.Items.AddRange([.. lineWidths.Select(width => $"{width} px")]);
        FontSizeComboBox.Items.AddRange([.. fontSizes.Select(size => $"{size} px")]);
        TextOutlineWidthComboBox.Items.AddRange(
            [.. textOutlineWidths.Select(width => width == 0 ? "No edge" : $"{width} px")]);
        _isShowingChoices = false;
    }

    public void ShowStyle(DiagramStyleModel style, DiagramRouting routing, DiagramArrowHeads heads)
    {
        _isShowingChoices = true;

        FillPalette.SelectedColor = style.Fill;
        TitleFillPalette.SelectedColor = style.TitleFill.A == 0 ? Color.Empty : style.TitleFill;
        LinePalette.SelectedColor = style.Line;
        TextPalette.SelectedColor = style.TextColor;
        // With no edge, its swatch shows no color, as no fill does; the color is kept for when it's turned on
        Color textOutline = style.TextOutlineWidth == 0 ? Color.Empty : style.TextOutlineColor;
        TextOutlinePalette.SelectedColor = textOutline;
        ShowSwatch(FillDropDownButton, style.Fill);
        ShowSwatch(LineDropDownButton, style.Line);
        ShowSwatch(TextColorDropDownButton, style.TextColor);
        ShowSwatch(TextOutlineDropDownButton, textOutline);

        LineWidthComboBox.SelectedIndex = _lineWidths.ToList().IndexOf(style.LineWidth);
        FontSizeComboBox.SelectedIndex = _fontSizes.ToList().IndexOf(style.FontSize);
        TextOutlineWidthComboBox.SelectedIndex = _textOutlineWidths.ToList().IndexOf(style.TextOutlineWidth);
        DashedButton.Checked = style.Dashed;
        RoutingComboBox.SelectedIndex = routing == DiagramRouting.Elbow ? 0 : 1;
        ArrowHeadsComboBox.SelectedIndex = (int)heads;

        _isShowingChoices = false;
    }

    public void ShowPropertiesPanel(bool isVisible) => ShowTab(PropertiesTab, isVisible);

    public void ShowImagesPanel(bool isVisible) => ShowTab(ImagesTab, isVisible);

    public void ShowLayersPanel(bool isVisible) => ShowTab(LayersTab, isVisible);

    /// <summary>
    ///     As the presenter says: a tab shown is selected, with the panel; one hidden hides the panel only when it's the
    ///     tab shown. Nothing is raised, since it isn't the user's choice.
    /// </summary>
    private void ShowTab(TabPage tab, bool isVisible)
    {
        _isShowingTabs = true;

        try
        {
            if (isVisible)
            {
                SidePanelTabs.SelectedTab = tab;
                SidePanelTabs.Visible = true;
            }
            else if (SidePanelTabs.SelectedTab == tab)
            {
                SidePanelTabs.Visible = false;
            }
        }
        finally
        {
            _isShowingTabs = false;
        }

        ShowTabButtons();
    }

    /// <summary>The tab shown, or null when the panel is hidden.</summary>
    private TabPage? ShownTab => SidePanelTabs.Visible ? SidePanelTabs.SelectedTab : null;

    /// <summary>The Panels button is pressed while the side panel shows.</summary>
    private void ShowTabButtons()
    {
        PanelsButton.Checked = SidePanelTabs.Visible;
    }

    /// <summary>
    ///     The Panels button or F4: shows the side panel, at the tab it had, or hides it. Says which tab starts or stops
    ///     showing.
    /// </summary>
    private void TogglePanels()
    {
        SidePanelTabs.Visible = !SidePanelTabs.Visible;

        if (SidePanelTabs.SelectedTab is TabPage tab)
        {
            RaiseTabShown(tab, SidePanelTabs.Visible);
        }

        ShowTabButtons();
    }

    private void RaiseTabShown(TabPage tab, bool isVisible)
    {
        EventHandler<bool>? toggled = tab == PropertiesTab ? PropertiesPanelToggled
            : tab == ImagesTab ? ImagesPanelToggled
            : LayersPanelToggled;

        toggled?.Invoke(this, isVisible);
    }

    // A tab header clicked: the tab left stops showing
    private void SidePanelTabs_Deselected(object? sender, TabControlEventArgs e)
    {
        if (!_isShowingTabs && SidePanelTabs.Visible && e.TabPage is TabPage tab)
        {
            RaiseTabShown(tab, false);
        }
    }

    // And the one clicked starts
    private void SidePanelTabs_Selected(object? sender, TabControlEventArgs e)
    {
        if (!_isShowingTabs && SidePanelTabs.Visible && e.TabPage is TabPage tab)
        {
            RaiseTabShown(tab, true);
            ShowTabButtons();
        }
    }

    public void ShowCatalog(IReadOnlyList<DiagramCatalogImageModel> images, string emptyText)
    {
        Dictionary<string, ListViewGroup> groups = [];

        ImagesListView.BeginUpdate();
        ImagesListView.Items.Clear();
        ImagesListView.Groups.Clear();

        foreach (DiagramCatalogImageModel image in images)
        {
            if (!groups.TryGetValue(image.Group, out ListViewGroup? group))
            {
                group = new ListViewGroup(image.Group, image.Group);
                groups[image.Group] = group;
                ImagesListView.Groups.Add(group);
            }

            // The whole name, which can be cut under the picture, and where the file is
            ImagesListView.Items.Add(new ListViewItem(image.Name, group)
            {
                Tag = image,
                ToolTipText = $"{image.Name}\n{image.Path}",
            });
        }

        ImagesListView.EndUpdate();
        ImagesEmptyLabel.Text = emptyText;
        ImagesEmptyLabel.Visible = images.Count == 0;
        ImagesListView.Visible = images.Count > 0;
    }

    public void OpenFolder(string folder)
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
        catch (Win32Exception ex)
        {
            ShowError($"Could not open {folder}: {ex.Message}");
        }
    }

    public void ShowWindowSize(DiagramWindowSizeModel size)
    {
        // It may have been saved on a larger screen than this one
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

        Size = new Size(Math.Min(size.Size.Width, area.Width), Math.Min(size.Size.Height, area.Height));
        FormExtensions.CenterOnCursorScreen(this);
        WindowState = size.IsMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
    }

    public void ShowProperties(DiagramPropertiesModel properties)
    {
        _isShowingChoices = true;
        PropertiesTable.SuspendLayout();

        PropertiesTitleLabel.Text = properties.Title;

        bool hasBounds = properties.Bounds is not null;
        Rectangle bounds = properties.Bounds ?? Rectangle.Empty;

        ShowNumber(XLabel, XBox, hasBounds ? bounds.X : null);
        ShowNumber(YLabel, YBox, hasBounds ? bounds.Y : null);
        ShowNumber(WidthLabel, WidthBox, hasBounds ? bounds.Width : null);
        ShowNumber(HeightLabel, HeightBox, hasBounds ? bounds.Height : null);
        ShowLock(XLockCheckBox, XBox, properties.Locks, DiagramLock.X);
        ShowLock(YLockCheckBox, YBox, properties.Locks, DiagramLock.Y);
        ShowLock(WidthLockCheckBox, WidthBox, properties.Locks, DiagramLock.Width);
        ShowLock(HeightLockCheckBox, HeightBox, properties.Locks, DiagramLock.Height);
        ProportionsCheckBox.Visible = properties.Locks is not null;
        ProportionsCheckBox.Checked = properties.Locks is DiagramLock locks && locks.HasFlag(DiagramLock.AspectRatio);
        ResetSizeButton.Visible = properties.HasImages;
        ShowNumber(CornerRadiusLabel, CornerRadiusBox, properties.CornerRadius);
        ShowNumber(BottomCornerRadiusLabel, BottomCornerRadiusBox, properties.BottomCornerRadius);
        ShowNumber(LineWidthLabel, LineWidthBox, properties.LineWidth);
        ShowNumber(FontSizeLabel, FontSizeBox, properties.FontSize);
        TextAlignLabel.Visible = properties.TextAlignment is not null;
        TextAlignPropertyBox.Visible = properties.TextAlignment is not null;
        TextAlignPropertyBox.SelectedIndex = (int)(properties.TextAlignment ?? DiagramTextAlignment.Center);
        ShowNumber(TextOutlineWidthLabel, TextOutlineWidthBox, properties.TextOutlineWidth);

        ShowColor(FillLabel, FillColorButton, properties.Fill);
        ShowColor(
            TitleFillLabel,
            TitleFillColorButton,
            properties.TitleFill is { IsEmpty: false, A: 0 } ? Color.Empty : properties.TitleFill);
        ShowTitleFillAsFill(properties);
        ShowColor(LineLabel, LineColorButton, properties.Line);
        ShowColor(TextColorLabel, TextColorButton, properties.TextColor);
        ShowColor(
            TextOutlineLabel,
            TextOutlineColorButton,
            properties.TextOutlineWidth == 0 ? Color.Empty : properties.TextOutlineColor);

        // A connector has a line where a shape has a border
        LineLabel.Text = hasBounds ? "Border" : "Line";
        LineWidthLabel.Text = hasBounds ? "Border width" : "Line width";

        DashedCheckBox.Visible = properties.Dashed is not null;
        DashedCheckBox.Checked = properties.Dashed ?? false;

        NameLabel.Visible = properties.Name is not null;
        NamePropertyBox.Visible = properties.Name is not null;

        if (!NamePropertyBox.Focused)
        {
            NamePropertyBox.Text = properties.Name ?? string.Empty;
        }

        TextLabel.Visible = properties.Text is not null;
        TextPropertyBox.Visible = properties.Text is not null;
        TextLabel.Text = hasBounds ? "Text" : "Label";

        if (!TextPropertyBox.Focused)
        {
            TextPropertyBox.Text = properties.Text?.ReplaceLineEndings(Environment.NewLine) ?? string.Empty;
        }

        FontFamilyLabel.Visible = properties.FontFamily is not null;
        FontFamilyBox.Visible = properties.FontFamily is not null;

        if (properties.FontFamily is string fontFamily)
        {
            // A diagram from another computer can have a font this one doesn't
            int index = FontFamilyBox.FindStringExact(fontFamily);

            FontFamilyBox.SelectedIndex = index >= 0 ? index : FontFamilyBox.Items.Add(fontFamily);
        }

        RoutingLabel.Visible = properties.Routing is not null;
        RoutingPropertyBox.Visible = properties.Routing is not null;
        RoutingPropertyBox.SelectedIndex = properties.Routing == DiagramRouting.Straight ? 1 : 0;

        ArrowsLabel.Visible = properties.Heads is not null;
        ArrowsPropertyBox.Visible = properties.Heads is not null;
        ArrowsPropertyBox.SelectedIndex = (int)(properties.Heads ?? DiagramArrowHeads.End);

        DirectionLabel.Visible = properties.LineDirection is not null;
        DirectionPropertyBox.Visible = properties.LineDirection is not null;
        DirectionPropertyBox.SelectedIndex = (int)(properties.LineDirection ?? DiagramLineDirection.Free);

        PropertiesTable.ResumeLayout();
        _isShowingChoices = false;
    }

    public void ShowContextMenu(Point location, DiagramContextMenuModel menu)
    {
        PasteStyleContextMenuItem.Enabled = menu.CanPasteStyle;
        GroupContextMenuItem.Enabled = menu.CanGroup;
        UngroupContextMenuItem.Enabled = menu.CanUngroup;
        AlignContextMenuItem.Enabled = menu.CanAlign;
        DistributeHorizontallyContextMenuItem.Enabled = menu.CanDistribute;
        DistributeVerticallyContextMenuItem.Enabled = menu.CanDistribute;
        SelectionMenu.Show(Canvas, location);
    }

    public void ShowUndoRedo(bool canUndo, bool canRedo)
    {
        UndoMenuItem.Enabled = canUndo;
        RedoMenuItem.Enabled = canRedo;
    }

    public void ShowDocument(string name, bool hasChanges)
    {
        Text = $"{name}{(hasChanges ? "*" : string.Empty)} - {WindowTitle}";
    }

    public void ShowZoom(int percent)
    {
        ZoomLabel.Text = $"{percent}%";
    }

    public void ShowCursor(DiagramCursor cursor)
    {
        Canvas.Cursor = cursor switch
        {
            DiagramCursor.Cross => Cursors.Cross,
            DiagramCursor.Move => Cursors.SizeAll,
            DiagramCursor.Pan => Cursors.Hand,
            DiagramCursor.SizeVertical => Cursors.SizeNS,
            DiagramCursor.SizeHorizontal => Cursors.SizeWE,
            DiagramCursor.SizeTopLeftBottomRight => Cursors.SizeNWSE,
            DiagramCursor.SizeTopRightBottomLeft => Cursors.SizeNESW,
            _ => Cursors.Default,
        };
    }

    public void ShowStatus(string pointer, string selection)
    {
        PointerStatusLabel.Text = pointer;
        SelectionStatusLabel.Text = selection;
    }

    public void ShowFindResult(string result)
    {
        FindResultLabel.Text = result;
    }

    public void BeginTextEdit(
        Rectangle bounds,
        DiagramRichTextModel text,
        int fontSize,
        string fontFamily,
        Color color)
    {
        Font font = SwapFont(TextInputBox, _textFont, DiagramFonts.Create(fontFamily, fontSize));

        _textFont = font;
        TextInputBox.ForeColor = color;

        // A light text on a light box couldn't be read while typing
        TextInputBox.BackColor = color.GetBrightness() > 0.7f ? Color.FromArgb(64, 64, 64) : Color.White;
        TextInputBox.Text = text.Text.ReplaceLineEndings("\n");
        ShowFormats(text, font);

        // A small shape still gets a box large enough to type in
        TextInputBox.Bounds = new Rectangle(
            bounds.Location,
            new Size(Math.Max(bounds.Width, fontSize * 6), Math.Max(bounds.Height, fontSize * 2)));
        TextInputBox.Visible = true;
        TextInputBox.SelectAll();
        TextInputBox.SelectionAlignment = HorizontalAlignment.Center;
        TextInputBox.Focus();
        _isEditingText = true;
    }

    /// <summary>
    ///     Gives a control a new font and frees the one given before. A control ignores a font equal to its own and keeps
    ///     its own, so then the new one is freed instead: freeing the kept one would leave the control a freed font.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="current">The font given to it before, which this frees; null for none.</param>
    /// <param name="font">The new font, which this owns from now.</param>
    /// <returns>The font the control has now, to free when another replaces it.</returns>
    internal static Font SwapFont(Control control, Font? current, Font font)
    {
        if (current is not null && current.Equals(font))
        {
            font.Dispose();

            return current;
        }

        control.Font = font;
        current?.Dispose();

        return font;
    }

    /// <summary>The text box's parts in bold or italic, as the text has them, in its font.</summary>
    private void ShowFormats(DiagramRichTextModel text, Font font)
    {
        TextInputBox.SelectAll();
        TextInputBox.SelectionFont = font;

        foreach (DiagramTextSpanModel span in DiagramRichText.Valid(TextInputBox.Text, text.Spans))
        {
            using Font spanFont = DiagramTextRuns.FontOf(font, span.Format);

            TextInputBox.Select(span.Start, span.Length);
            TextInputBox.SelectionFont = spanFont;
        }
    }

    /// <summary>What's typed, with the parts of it in bold or italic, read a character at a time.</summary>
    internal DiagramRichTextModel TypedText()
    {
        string text = TextInputBox.Text;
        var formats = new DiagramTextFormat[text.Length];
        (int start, int length) = (TextInputBox.SelectionStart, TextInputBox.SelectionLength);

        for (int i = 0; i < text.Length; i++)
        {
            TextInputBox.Select(i, 1);
            formats[i] = FormatOf(TextInputBox.SelectionFont);
        }

        TextInputBox.Select(start, length);

        return new DiagramRichTextModel(text, DiagramRichText.SpansOf(formats));
    }

    private static DiagramTextFormat FormatOf(Font? font) =>
        (font?.Bold == true ? DiagramTextFormat.Bold : DiagramTextFormat.None)
        | (font?.Italic == true ? DiagramTextFormat.Italic : DiagramTextFormat.None);

    /// <summary>
    ///     Makes what's selected in the text box bold or italic, or no longer, as Ctrl+B and Ctrl+I do in a word
    ///     processor: on, unless all of it has it already. With nothing selected, it's for what's typed next.
    /// </summary>
    internal void ToggleSelectedFormat(DiagramTextFormat format)
    {
        (int start, int length) = (TextInputBox.SelectionStart, TextInputBox.SelectionLength);
        bool isOn = (FormatOf(TextInputBox.SelectionFont) & format) == 0 || TextInputBox.SelectionFont is null;

        if (length == 0)
        {
            DiagramTextFormat typed = FormatOf(TextInputBox.SelectionFont);
            using Font font = DiagramTextRuns.FontOf(_textFont!, isOn ? typed | format : typed & ~format);

            TextInputBox.SelectionFont = font;

            return;
        }

        // A character at a time, so each keeps what else it is: making a part bold leaves its italic as it was
        for (int i = start; i < start + length; i++)
        {
            TextInputBox.Select(i, 1);

            DiagramTextFormat old = FormatOf(TextInputBox.SelectionFont);
            using Font font = DiagramTextRuns.FontOf(_textFont!, isOn ? old | format : old & ~format);

            TextInputBox.SelectionFont = font;
        }

        TextInputBox.Select(start, length);
    }

    public Color? AskColor(Color current)
    {
        using var dialog = new ColorDialog { AnyColor = true, Color = current, FullOpen = true };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Color : null;
    }

    public string? AskSavePath(string suggestedFileName)
    {
        using var dialog = new SaveFileDialog
        {
            DefaultExt = "svg",
            FileName = suggestedFileName,
            Filter = "SVG image (*.svg)|*.svg",
            Title = "Save diagram",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    /// <summary>The size is the type chosen in the dialog: the same picture, at twice the pixels or not.</summary>
    public DiagramPngExportModel? AskExportPng(string suggestedFileName)
    {
        using var dialog = new SaveFileDialog
        {
            DefaultExt = "png",
            FileName = suggestedFileName,
            Filter = "PNG picture, twice as large, sharper (*.png)|*.png|PNG picture, actual size (*.png)|*.png",
            Title = "Export as PNG",
        };

        return dialog.ShowDialog(this) == DialogResult.OK
            ? new DiagramPngExportModel(dialog.FileName, dialog.FilterIndex == 2 ? 1 : 2)
            : null;
    }

    public string? AskExportPdfPath(string suggestedFileName)
    {
        using var dialog = new SaveFileDialog
        {
            DefaultExt = "pdf",
            FileName = suggestedFileName,
            Filter = "PDF document (*.pdf)|*.pdf",
            Title = "Export as PDF",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? AskOpenPath()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "SVG image (*.svg)|*.svg",
            Title = "Open diagram",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public IReadOnlyList<string> AskImagePaths()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "SVG image (*.svg)|*.svg",
            Multiselect = true,
            Title = "Insert image",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileNames : [];
    }

    public bool AskOverwrite(string name)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"{name} was changed elsewhere since it was opened here. Replace it with this diagram?",
            WindowTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        return answer == DialogResult.Yes;
    }

    public DiagramSaveChoice AskSaveChanges(string name)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"Save the changes to {name}?",
            WindowTitle,
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => DiagramSaveChoice.Save,
            DialogResult.No => DiagramSaveChoice.Discard,
            _ => DiagramSaveChoice.Cancel,
        };
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, WindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public void SetClipboardText(string text)
    {
        Clipboard.SetText(text);
    }

    public string? GetClipboardText() => Clipboard.ContainsText() ? Clipboard.GetText() : null;

    /// <summary>
    ///     As a bitmap, which every program takes, and as PNG, which browsers and chats prefer; sharp at the screen's
    ///     scaling.
    /// </summary>
    public void SetClipboardPicture(DiagramModel diagram)
    {
        int scale = Math.Max(1, (int)Math.Ceiling(DeviceDpi / 96.0));

        using Bitmap picture = PngDiagramExporter.Draw(diagram, scale);
        using var png = new MemoryStream();

        // Copied now, so the picture and the stream can be disposed
        Clipboard.SetDataObject(PictureData(picture, png), true);
    }

    /// <summary>What the clipboard gets for a picture: the bitmap, and the PNG file written into the stream.</summary>
    internal static DataObject PictureData(Bitmap picture, MemoryStream png)
    {
        picture.Save(png, System.Drawing.Imaging.ImageFormat.Png);

        var data = new DataObject();

        data.SetImage(picture);
        data.SetData(PngClipboardFormat, false, png);

        return data;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // An open list closes itself on Esc, and a rename in the layers panel ends with it
        if (keyData != Keys.Escape
            || _isRenamingLayer
            || LineWidthComboBox.DroppedDown
            || FontSizeComboBox.DroppedDown
            || TextOutlineWidthComboBox.DroppedDown
            || RoutingComboBox.DroppedDown
            || ArrowHeadsComboBox.DroppedDown
            || RoutingPropertyBox.DroppedDown
            || ArrowsPropertyBox.DroppedDown
            || DirectionPropertyBox.DroppedDown
            || TextAlignPropertyBox.DroppedDown
            || FontFamilyBox.DroppedDown)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Esc never reaches the text box's KeyDown: it is a dialog key. While typing, it only drops the text
        if (_isEditingText)
        {
            CancelText();
        }
        else if (IsInAPanel)
        {
            // Back to the canvas, which also takes what was typed in the field
            Canvas.Focus();
        }
        else
        {
            CommandRequested?.Invoke(this, DiagramCommand.Cancel);
        }

        return true;
    }

    private static DiagramTool ToolOf(ToolStripButton button) => Enum.Parse<DiagramTool>((string)button.Tag!);

    private static DiagramCommand CommandOf(object? button) =>
        Enum.Parse<DiagramCommand>((string)((ToolStripItem)button!).Tag!);

    private static Bitmap CreateSwatch(Color color, int size)
    {
        var swatch = new Bitmap(size, size);
        int side = size - 2;

        using var graphics = Graphics.FromImage(swatch);

        if (color.IsEmpty)
        {
            // No fill: a red line through an empty square
            graphics.FillRectangle(Brushes.White, 1, 1, side, side);
            graphics.DrawLine(Pens.Red, 1, side, side, 1);
        }
        else
        {
            using var brush = new SolidBrush(color);

            graphics.FillRectangle(brush, 1, 1, side, side);
        }

        graphics.DrawRectangle(Pens.Gray, 1, 1, side - 1, side - 1);

        return swatch;
    }

    private ToolStripButton[] ToolButtons() =>
    [
        SelectButton,
        ConnectorButton,
        LineButton,
        RectangleButton,
        RoundedRectangleButton,
        EllipseButton,
        DiamondButton,
        ParallelogramButton,
        HexagonButton,
        CylinderButton,
        DocumentButton,
        ContainerButton,
        TextButton,
    ];

    private ToolStripDropDownButton[] ColorButtons() =>
        [FillDropDownButton, LineDropDownButton, TextColorDropDownButton, TextOutlineDropDownButton];

    private Image Keep(Image image)
    {
        _images.Add(image);

        return image;
    }

    private void ShowSwatch(ToolStripDropDownButton button, Color color)
    {
        _swatches.TryGetValue(button, out Image? old);

        Image swatch = CreateSwatch(color, _iconSize);

        _swatches[button] = swatch;
        button.Image = swatch;
        old?.Dispose();
    }

    private DiagramPointerModel PointerOf(MouseEventArgs e) => new(
        e.Location,
        (ModifierKeys & Keys.Shift) != 0,
        (ModifierKeys & Keys.Control) != 0,
        (ModifierKeys & Keys.Alt) != 0,
        e.Button == MouseButtons.Middle || _isSpaceDown);

    /// <summary>A number, kept in the box's range, which would otherwise throw; null hides the field.</summary>
    private static void ShowNumber(Label label, NumericUpDown box, int? value)
    {
        label.Visible = value is not null;
        box.Visible = value is not null;

        if (value is int number)
        {
            box.Value = Math.Clamp(number, box.Minimum, box.Maximum);
        }
    }

    /// <summary>
    ///     A lock beside its field, which can't be typed in while it's locked; null, with no shape selected, hides it.
    /// </summary>
    private static void ShowLock(CheckBox lockBox, NumericUpDown box, DiagramLock? locks, DiagramLock which)
    {
        bool isLocked = locks is DiagramLock shown && shown.HasFlag(which);

        lockBox.Visible = locks is not null;
        lockBox.Checked = isLocked;
        lockBox.Text = LockGlyph(isLocked);
        box.Enabled = !isLocked;
    }

    private static string LockGlyph(bool isLocked) => isLocked ? "\U0001F512" : "\U0001F513";

    /// <summary>A color on the button; null hides the field, and an empty color reads as none.</summary>
    /// <summary>
    ///     A container without a title fill shows its fill in the band, so its button shows that color too, saying so,
    ///     rather than "None", which wouldn't be what the band looks like.
    /// </summary>
    private void ShowTitleFillAsFill(DiagramPropertiesModel properties)
    {
        bool isSameAsFill = properties is { TitleFill.IsEmpty: true, Fill: { IsEmpty: false } };

        TitleFillColorButton.ForeColor = SystemColors.ControlText;

        if (isSameAsFill)
        {
            Color fill = properties.Fill!.Value;

            TitleFillColorButton.BackColor = fill;
            TitleFillColorButton.ForeColor = fill.GetBrightness() < 0.5f ? Color.White : Color.Black;
            TitleFillColorButton.Text = "Same as fill";
        }
    }

    private static void ShowColor(Label label, Button button, Color? color)
    {
        label.Visible = color is not null;
        button.Visible = color is not null;

        if (color is Color shown)
        {
            button.BackColor = shown.IsEmpty ? SystemColors.Control : shown;
            button.Text = shown.IsEmpty ? "None" : string.Empty;
        }
    }

    /// <summary>
    ///     The same choices as the top bar's color, below the panel's button. The menu belongs to the top bar's button,
    ///     and a menu with an owner opens under it, so it's lent without its owner until it closes.
    /// </summary>
    private static void ShowColorMenu(ToolStripDropDownButton colors, Control button)
    {
        ToolStripDropDown menu = colors.DropDown;

        if (menu.Visible)
        {
            menu.Close();
        }

        void GiveBack(object? sender, ToolStripDropDownClosedEventArgs e)
        {
            menu.Closed -= GiveBack;
            menu.OwnerItem = colors;
        }

        menu.OwnerItem = null;
        menu.Closed += GiveBack;
        menu.Show(button, new Point(0, button.Height));
    }

    private void PanelsButton_Click(object? sender, EventArgs e) => TogglePanels();

    public void ShowLayers(IReadOnlyList<DiagramLayerModel> layers)
    {
        _isShowingLayers = true;
        LayersListView.BeginUpdate();

        try
        {
            LayersListView.Items.Clear();

            foreach (DiagramLayerModel layer in layers)
            {
                LayersListView.Items.Add(new ListViewItem(layer.Text)
                {
                    Tag = layer,
                    ToolTipText = layer.IsConnector
                        ? "Connectors and lines are always in front of the shapes"
                        : layer.GroupId is null
                            ? null
                            : "In a group: a click on the canvas selects the whole group, a click here only this",
                });
            }

            SizeLayerTiles();
        }
        finally
        {
            LayersListView.EndUpdate();
            _isShowingLayers = false;
        }
    }

    public void ShowLayerSelection(IReadOnlySet<int> ids)
    {
        _isShowingLayers = true;

        try
        {
            ListViewItem? first = null;

            foreach (ListViewItem item in LayersListView.Items)
            {
                item.Selected = ids.Contains(((DiagramLayerModel)item.Tag!).Id);
                first ??= item.Selected ? item : null;
            }

            first?.EnsureVisible();
        }
        finally
        {
            _isShowingLayers = false;
        }
    }

    /// <summary>The ids of the rows, as the layers panel shows them, front first.</summary>
    private static IEnumerable<int> LayerIds(System.Collections.IEnumerable items) =>
        items.Cast<ListViewItem>().Select(item => ((DiagramLayerModel)item.Tag!).Id);

    /// <summary>One row per tile, as wide as the list: a tile view is the one that shows where a drop goes.</summary>
    private void SizeLayerTiles()
    {
        LayersListView.TileSize = new Size(
            Math.Max(LayersListView.ClientSize.Width - LogicalToDeviceUnits(4), LogicalToDeviceUnits(40)),
            LayersListView.Font.Height + LogicalToDeviceUnits(8));
    }

    private void LayersListView_Resize(object? sender, EventArgs e) => SizeLayerTiles();

    // The selection's color shows whether the list has the focus
    private void LayersListView_FocusChanged(object? sender, EventArgs e) => LayersListView.Invalidate();

    private void LayersListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingLayers)
        {
            LayersSelected?.Invoke(this, [.. LayerIds(LayersListView.SelectedItems)]);
        }
    }

    private void LayersListView_DoubleClick(object? sender, EventArgs e) => LayersListView.FocusedItem?.BeginEdit();

    private void LayersListView_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyData)
        {
            case Keys.F2:
                LayersListView.FocusedItem?.BeginEdit();
                break;
            case Keys.Delete:
                CommandRequested?.Invoke(this, DiagramCommand.Delete);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void LayersListView_BeforeLabelEdit(object? sender, LabelEditEventArgs e) => _isRenamingLayer = true;

    /// <summary>
    ///     The presenter shows the name, after the list is done with its edit: changing its rows inside this event would
    ///     upset it. A text left as it was isn't a name.
    /// </summary>
    private void LayersListView_AfterLabelEdit(object? sender, LabelEditEventArgs e)
    {
        _isRenamingLayer = false;
        e.CancelEdit = true;

        ListViewItem item = LayersListView.Items[e.Item];

        if (e.Label is string name && name != item.Text)
        {
            int id = ((DiagramLayerModel)item.Tag!).Id;

            BeginInvoke(() => LayerRenamed?.Invoke(this, new DiagramLayerRenameModel(id, name)));
        }
    }

    private void LayersListView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        string ids = string.Join(',', LayerIds(LayersListView.SelectedItems));

        LayersListView.DoDragDrop(new DataObject(LayersDragFormat, ids), DragDropEffects.Move);
    }

    private void LayersListView_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(LayersDragFormat) != true)
        {
            e.Effect = DragDropEffects.None;

            return;
        }

        e.Effect = DragDropEffects.Move;
        ShowLayerDrop(LayerDropIndex(LayersListView.PointToClient(new Point(e.X, e.Y))));
    }

    /// <summary>The row the dragged rows would go above: the one under the point, or the next below its middle.</summary>
    private int LayerDropIndex(Point point)
    {
        int count = LayersListView.Items.Count;

        if (count == 0)
        {
            return 0;
        }

        Rectangle last = LayersListView.GetItemRect(count - 1);

        if (point.Y >= last.Bottom)
        {
            return count;
        }

        for (int i = 0; i < count; i++)
        {
            Rectangle row = LayersListView.GetItemRect(i);

            if (point.Y < row.Bottom)
            {
                return point.Y < row.Top + row.Height / 2 ? i : i + 1;
            }
        }

        return count;
    }

    /// <summary>Draws the line where the rows would go, between the rows it's at.</summary>
    private void ShowLayerDrop(int index)
    {
        if (index == _layerDropIndex)
        {
            return;
        }

        int old = _layerDropIndex;

        _layerDropIndex = index;

        foreach (int row in new[] { old - 1, old, index - 1, index })
        {
            if (row >= 0 && row < LayersListView.Items.Count)
            {
                LayersListView.Invalidate(LayersListView.GetItemRect(row));
            }
        }
    }

    private void LayersListView_DragLeave(object? sender, EventArgs e) => ShowLayerDrop(-1);

    /// <summary>The rows dropped go above the row the line was over, or at the bottom.</summary>
    private void LayersListView_DragDrop(object? sender, DragEventArgs e)
    {
        int index = _layerDropIndex;

        ShowLayerDrop(-1);

        if (index < 0 || e.Data?.TryGetData(LayersDragFormat, out string? text) != true || text is null)
        {
            return;
        }

        int[] ids = [.. text.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)];
        int? beforeId = index < LayersListView.Items.Count
            ? ((DiagramLayerModel)LayersListView.Items[index].Tag!).Id
            : null;

        LayersMoved?.Invoke(this, new DiagramLayerMoveModel(ids, beforeId));
    }

    /// <summary>
    ///     Each row: its text, highlighted when selected (grayed while the list hasn't the focus), and the line where
    ///     dragged rows would go, above it or, for the last, under it.
    /// </summary>
    private void LayersListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        Rectangle row = e.Bounds;
        bool isFocused = LayersListView.Focused;
        Color back = !e.Item.Selected ? LayersListView.BackColor
            : isFocused ? SystemColors.Highlight
            : SystemColors.ControlLight;
        var layer = (DiagramLayerModel)e.Item.Tag!;
        Color fore = e.Item.Selected && isFocused ? SystemColors.HighlightText
            : layer.IsHidden ? SystemColors.GrayText
            : LayersListView.ForeColor;
        Color faint = e.Item.Selected && isFocused ? SystemColors.HighlightText : SystemColors.GrayText;
        (Rectangle lockIcon, Rectangle eyeIcon) = LayerIconsOf(LayerRowOf(e.Item));
        int padding = LogicalToDeviceUnits(4);

        using (var brush = new SolidBrush(back))
        {
            e.Graphics.FillRectangle(brush, row);
        }

        TextRenderer.DrawText(
            e.Graphics,
            e.Item.Text,
            LayersListView.Font,
            Rectangle.FromLTRB(row.Left + padding, row.Top, lockIcon.Left, row.Bottom),
            fore,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix);

        // Faint while they're as at first: shown, not locked
        char lockGlyph = layer.IsLocked ? LayerLockGlyph : LayerUnlockGlyph;
        char eyeGlyph = layer.IsHidden ? LayerHideGlyph : LayerEyeGlyph;

        DrawLayerIcon(e.Graphics, lockGlyph, lockIcon, layer.IsLocked ? fore : faint);
        DrawLayerIcon(e.Graphics, eyeGlyph, eyeIcon, layer.IsHidden ? fore : faint);

        int lineY = _layerDropIndex == e.ItemIndex ? row.Top
            : _layerDropIndex == e.ItemIndex + 1 && e.ItemIndex == LayersListView.Items.Count - 1 ? row.Bottom - 2
            : -1;

        if (lineY >= 0)
        {
            using var line = new Pen(SystemColors.Highlight, LogicalToDeviceUnits(2));

            e.Graphics.DrawLine(line, row.Left, lineY + 1, row.Right, lineY + 1);
        }
    }

    /// <summary>A row's whole tile: in a tile view, an item's own bounds are only around its text.</summary>
    private Rectangle LayerRowOf(ListViewItem item) => new(item.Bounds.Location, LayersListView.TileSize);

    /// <summary>Where a row's lock and eye are: two squares at its right end, the eye last.</summary>
    private static (Rectangle Lock, Rectangle Eye) LayerIconsOf(Rectangle row)
    {
        int side = row.Height;

        return (
            new Rectangle(row.Right - 2 * side, row.Top, side, side),
            new Rectangle(row.Right - side, row.Top, side, side));
    }

    private void DrawLayerIcon(Graphics graphics, char glyph, Rectangle square, Color color)
    {
        _layerIconFont ??= new Font(
            DiagramFonts.IsInstalled(FluentIconFont) ? FluentIconFont : Mdl2IconFont,
            LayersListView.Font.Size,
            FontStyle.Regular,
            GraphicsUnit.Point);

        TextRenderer.DrawText(
            graphics,
            glyph.ToString(),
            _layerIconFont,
            square,
            color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    /// <summary>
    ///     A click on a row's eye shows or hides it; on its lock, locks or frees it; elsewhere on the row past its text,
    ///     where the list itself sees no row, selects it. On the button's release, which the list raises wherever it is.
    /// </summary>
    private void LayersListView_MouseUp(object? sender, MouseEventArgs e)
    {
        // By the whole tile: GetItemAt finds an item only on its text
        ListViewItem? item = LayersListView.Items.Cast<ListViewItem>()
            .FirstOrDefault(each => LayerRowOf(each).Contains(e.Location));

        if (e.Button != MouseButtons.Left || item is null)
        {
            return;
        }

        int id = ((DiagramLayerModel)item.Tag!).Id;
        (Rectangle lockIcon, Rectangle eyeIcon) = LayerIconsOf(LayerRowOf(item));

        if (eyeIcon.Contains(e.Location))
        {
            LayerHiddenToggled?.Invoke(this, id);
        }
        else if (lockIcon.Contains(e.Location))
        {
            LayerLockedToggled?.Invoke(this, id);
        }
        else if (!item.Selected)
        {
            if ((ModifierKeys & (Keys.Control | Keys.Shift)) == 0)
            {
                LayersListView.SelectedItems.Clear();
            }

            item.Selected = true;
            item.Focused = true;
        }
    }

    private void ImageSearchBox_TextChanged(object? sender, EventArgs e) =>
        ImageSearchChanged?.Invoke(this, ImageSearchBox.Text);

    /// <summary>Opens the page in the browser: nothing is sent, the user downloads the icons from there.</summary>
    private void OfficialIconsLinkLabel_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        if (e.Link?.LinkData is string page)
        {
            OpenFolder(page);
        }
    }

    private void ImagesFolderButton_Click(object? sender, EventArgs e) =>
        ImagesFolderRequested?.Invoke(this, EventArgs.Empty);

    private void DiagramEditorForm_Activated(object? sender, EventArgs e) =>
        WindowActivated?.Invoke(this, EventArgs.Empty);

    private void ImagesListView_ItemActivate(object? sender, EventArgs e) => ChooseFocusedImage();

    /// <summary>A double-click or Enter on an image: inserted where the mouse was last on the canvas.</summary>
    internal void ChooseFocusedImage()
    {
        if (ImagesListView.FocusedItem?.Tag is DiagramCatalogImageModel image)
        {
            CatalogImageChosen?.Invoke(this, image.Path);
        }
    }

    // Onto the canvas, where it's dropped
    private void ImagesListView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is ListViewItem { Tag: DiagramCatalogImageModel image })
        {
            ImagesListView.DoDragDrop(new DataObject(CatalogImageFormat, image.Path), DragDropEffects.Copy);
        }
    }

    /// <summary>The image as it is, so it keeps its proportions, with its name under it, two lines at most.</summary>
    private void ImagesListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        if (e.Item.Tag is not DiagramCatalogImageModel image)
        {
            return;
        }

        Rectangle bounds = e.Bounds;
        bool isSelected = e.Item.Selected;

        using (var back = new SolidBrush(isSelected ? SystemColors.Highlight : ImagesListView.BackColor))
        {
            e.Graphics.FillRectangle(back, bounds);
        }

        int size = _thumbnails.ImageSize.Width;
        int margin = LogicalToDeviceUnits(4);
        var picture = new Rectangle(bounds.Left + ((bounds.Width - size) / 2), bounds.Top + margin, size, size);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        SvgImageRenderer.Draw(e.Graphics, image.Markup, picture);

        var text = Rectangle.FromLTRB(bounds.Left + 2, picture.Bottom + margin, bounds.Right - 2, bounds.Bottom);

        TextRenderer.DrawText(
            e.Graphics,
            e.Item.Text,
            ImagesListView.Font,
            text,
            isSelected ? SystemColors.HighlightText : ImagesListView.ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis
            | TextFormatFlags.NoPrefix);

        if (e.Item.Focused && ImagesListView.Focused)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, bounds);
        }
    }

    private void BoundsBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            BoundsEdited?.Invoke(
                this,
                new Rectangle((int)XBox.Value, (int)YBox.Value, (int)WidthBox.Value, (int)HeightBox.Value));
        }
    }

    private void LockCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        var lockBox = (CheckBox)sender!;

        lockBox.Text = LockGlyph(lockBox.Checked);

        if (!_isShowingChoices)
        {
            LockToggled?.Invoke(
                this,
                new DiagramLockChoiceModel(Enum.Parse<DiagramLock>((string)lockBox.Tag!), lockBox.Checked));
        }
    }

    private void ProportionsCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            LockToggled?.Invoke(
                this,
                new DiagramLockChoiceModel(DiagramLock.AspectRatio, ProportionsCheckBox.Checked));
        }
    }

    private void ResetSizeButton_Click(object? sender, EventArgs e)
    {
        Canvas.Focus();
        CommandRequested?.Invoke(this, DiagramCommand.ResetSize);
    }

    private void BottomCornerRadiusBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            BottomCornerRadiusEdited?.Invoke(this, (int)BottomCornerRadiusBox.Value);
        }
    }

    private void CornerRadiusBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            CornerRadiusEdited?.Invoke(this, (int)CornerRadiusBox.Value);
        }
    }

    private void LineWidthBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            LineWidthSelected?.Invoke(this, (int)LineWidthBox.Value);
        }
    }

    private void TextAlignPropertyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && TextAlignPropertyBox.SelectedIndex >= 0)
        {
            TextAlignmentSelected?.Invoke(this, (DiagramTextAlignment)TextAlignPropertyBox.SelectedIndex);
        }
    }

    private void FontSizeBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            FontSizeSelected?.Invoke(this, (int)FontSizeBox.Value);
        }
    }

    private void TextOutlineWidthBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            TextOutlineWidthSelected?.Invoke(this, (int)TextOutlineWidthBox.Value);
        }
    }

    private void FontFamilyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && FontFamilyBox.SelectedItem is string fontFamily)
        {
            FontFamilySelected?.Invoke(this, fontFamily);
        }
    }

    private void DashedCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            DashedSelected?.Invoke(this, DashedCheckBox.Checked);
        }
    }

    private void RoutingPropertyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && RoutingPropertyBox.SelectedIndex >= 0)
        {
            RoutingSelected?.Invoke(
                this,
                RoutingPropertyBox.SelectedIndex == 0 ? DiagramRouting.Elbow : DiagramRouting.Straight);
        }
    }

    private void ArrowsPropertyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && ArrowsPropertyBox.SelectedIndex >= 0)
        {
            ArrowHeadsSelected?.Invoke(this, (DiagramArrowHeads)ArrowsPropertyBox.SelectedIndex);
        }
    }

    private void DirectionPropertyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && DirectionPropertyBox.SelectedIndex >= 0)
        {
            LineDirectionSelected?.Invoke(this, (DiagramLineDirection)DirectionPropertyBox.SelectedIndex);
        }
    }

    private void TitleFillColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(TitleFillDropDownButton, TitleFillColorButton);
    }

    private void TitleFillPalette_ColorPicked(object? sender, Color color)
    {
        TitleFillDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.TitleFill, color));
    }

    private void NoTitleFillMenuItem_Click(object? sender, EventArgs e)
    {
        ColorSelected?.Invoke(
            this,
            new DiagramColorChoiceModel(DiagramColorTarget.TitleFill, DiagramStyleModel.NoTitleFill));
    }

    private void SameAsFillMenuItem_Click(object? sender, EventArgs e)
    {
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.TitleFill, Color.Empty));
    }

    private void MoreTitleFillColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.TitleFill);
    }

    private void FillColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(FillDropDownButton, FillColorButton);
    }

    private void LineColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(LineDropDownButton, LineColorButton);
    }

    private void TextColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(TextColorDropDownButton, TextColorButton);
    }

    private void TextOutlineColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(TextOutlineDropDownButton, TextOutlineColorButton);
    }

    private void TextPropertyBox_KeyDown(object? sender, KeyEventArgs e)
    {
        // Enter ends the text; Shift+Enter starts a new line
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            TextEdited?.Invoke(this, TextPropertyBox.Text);
        }
    }

    private void NamePropertyBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            // No beep
            e.SuppressKeyPress = true;
            NameEdited?.Invoke(this, NamePropertyBox.Text);
        }
    }

    private void NamePropertyBox_Leave(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            NameEdited?.Invoke(this, NamePropertyBox.Text);
        }
    }

    private void TextPropertyBox_Leave(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            TextEdited?.Invoke(this, TextPropertyBox.Text);
        }
    }

    private void CommitText()
    {
        if (!_isEditingText)
        {
            return;
        }

        // Cleared first: hiding the focused box raises Leave, which commits again
        _isEditingText = false;
        DiagramRichTextModel text = TypedText();
        TextInputBox.Visible = false;
        Canvas.Focus();

        TextCommitted?.Invoke(this, text);
    }

    private void CancelText()
    {
        _isEditingText = false;
        TextInputBox.Visible = false;
        Canvas.Focus();
    }

    private void ToolButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        ToolSelected?.Invoke(this, ToolOf((ToolStripButton)sender!));
        Canvas.Focus();
    }

    private void CommandButton_Click(object? sender, EventArgs e)
    {
        CommitText();

        // First: a toolbar button doesn't take the focus, and leaving a panel field is what applies what was typed
        Canvas.Focus();
        CommandRequested?.Invoke(this, CommandOf(sender));
    }

    // Closing asks about unsaved changes, as the window's X does
    private void ExitMenuItem_Click(object? sender, EventArgs e) => Close();

    private void ZoomLabel_Click(object? sender, EventArgs e)
    {
        CommitText();
        Canvas.Focus();
        CommandRequested?.Invoke(this, DiagramCommand.ZoomReset);
    }

    private void AutosaveTimer_Tick(object? sender, EventArgs e)
    {
        AutosaveDue?.Invoke(this, EventArgs.Empty);
    }

    private void FindBox_TextChanged(object? sender, EventArgs e)
    {
        Find(DiagramFindStep.First);
    }

    private void FindBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        Find(e.Shift ? DiagramFindStep.Previous : DiagramFindStep.Next);

        // No beep
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void TopLeftMenuItem_Click(object? sender, EventArgs e) => BringIntoView(DiagramPlacement.TopLeft);

    private void CenterMenuItem_Click(object? sender, EventArgs e) => BringIntoView(DiagramPlacement.Center);

    /// <summary>Takes the text being typed first, as a toolbar command does, then asks to move the view.</summary>
    private void BringIntoView(DiagramPlacement placement)
    {
        CommitText();
        Canvas.Focus();
        BringIntoViewRequested?.Invoke(this, new DiagramBringIntoViewModel(placement, Canvas.ClientSize));
    }

    private void Find(DiagramFindStep step)
    {
        FindRequested?.Invoke(this, new DiagramFindModel(FindBox.Text, step, Canvas.ClientSize));
    }

    private void FillPalette_ColorPicked(object? sender, Color color)
    {
        // Not HideDropDown: the panel's color buttons show this same menu themselves
        FillDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Fill, color));
    }

    private void LinePalette_ColorPicked(object? sender, Color color)
    {
        LineDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Line, color));
    }

    private void TextPalette_ColorPicked(object? sender, Color color)
    {
        TextColorDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Text, color));
    }

    private void TextOutlinePalette_ColorPicked(object? sender, Color color)
    {
        TextOutlineDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.TextOutline, color));
    }

    private void NoFillMenuItem_Click(object? sender, EventArgs e)
    {
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Fill, Color.Empty));
    }

    private void NoTextOutlineMenuItem_Click(object? sender, EventArgs e)
    {
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.TextOutline, Color.Empty));
    }

    private void MoreFillColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Fill);
    }

    private void MoreLineColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Line);
    }

    private void MoreTextColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Text);
    }

    private void MoreTextOutlineColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.TextOutline);
    }

    private void TextOutlineWidthComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && TextOutlineWidthComboBox.SelectedIndex >= 0)
        {
            TextOutlineWidthSelected?.Invoke(this, _textOutlineWidths[TextOutlineWidthComboBox.SelectedIndex]);
            Canvas.Focus();
        }
    }

    private void LineWidthComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && LineWidthComboBox.SelectedIndex >= 0)
        {
            LineWidthSelected?.Invoke(this, _lineWidths[LineWidthComboBox.SelectedIndex]);
            Canvas.Focus();
        }
    }

    private void FontSizeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && FontSizeComboBox.SelectedIndex >= 0)
        {
            FontSizeSelected?.Invoke(this, _fontSizes[FontSizeComboBox.SelectedIndex]);
            Canvas.Focus();
        }
    }

    private void RoutingComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && RoutingComboBox.SelectedIndex >= 0)
        {
            RoutingSelected?.Invoke(
                this,
                RoutingComboBox.SelectedIndex == 0 ? DiagramRouting.Elbow : DiagramRouting.Straight);
            Canvas.Focus();
        }
    }

    private void ArrowHeadsComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && ArrowHeadsComboBox.SelectedIndex >= 0)
        {
            ArrowHeadsSelected?.Invoke(this, (DiagramArrowHeads)ArrowHeadsComboBox.SelectedIndex);
            Canvas.Focus();
        }
    }

    private void DashedButton_Click(object? sender, EventArgs e)
    {
        DashedSelected?.Invoke(this, !DashedButton.Checked);
        Canvas.Focus();
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        if (_scene is not null)
        {
            DiagramRenderer.Draw(e.Graphics, _scene, Canvas.ClientSize);
        }
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        // Clicking the canvas doesn't take the focus from the text box by itself
        CommitText();
        Canvas.Focus();

        if (e.Button is MouseButtons.Left or MouseButtons.Middle)
        {
            PointerPressed?.Invoke(this, PointerOf(e));
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        PointerMoved?.Invoke(this, PointerOf(e));
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button is MouseButtons.Left or MouseButtons.Middle)
        {
            PointerReleased?.Invoke(this, PointerOf(e));
        }
        else if (e.Button == MouseButtons.Right)
        {
            PointerRightClicked?.Invoke(this, e.Location);
        }
    }

    private void Canvas_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerDoubleClicked?.Invoke(this, PointerOf(e));
        }
    }

    /// <summary>Files can be dropped; what they are is up to the presenter.</summary>
    private void Canvas_DragEnter(object? sender, DragEventArgs e)
    {
        bool canDrop = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            || e.Data?.GetDataPresent(CatalogImageFormat) == true;

        e.Effect = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void Canvas_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(CatalogImageFormat) is string catalogPath)
        {
            Point at = Canvas.PointToClient(new Point(e.X, e.Y));

            BeginInvoke(() => CatalogImageDropped?.Invoke(this, new DiagramCatalogDropModel(catalogPath, at)));
            Canvas.Focus();
        }
        else if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
        {
            // From another program, which keeps waiting while a message box is shown
            Point location = Canvas.PointToClient(new Point(e.X, e.Y));

            BeginInvoke(() => FilesDropped?.Invoke(this, new DiagramFilesDropModel(paths, location)));
            Activate();
        }
    }

    private void Canvas_MouseLeave(object? sender, EventArgs e)
    {
        PointerLeft?.Invoke(this, EventArgs.Empty);
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        WheelTurned?.Invoke(
            this,
            new DiagramWheelModel(
                e.Location,
                e.Delta / (float)SystemInformation.MouseWheelScrollDelta,
                (ModifierKeys & Keys.Control) != 0,
                (ModifierKeys & Keys.Shift) != 0));
    }

    private void TextInputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyData)
        {
            // Enter ends the text
            case Keys.Enter:
                CommitText();
                break;

            // A new line; the box itself would put a line break a diagram's text doesn't know
            case Keys.Shift | Keys.Enter:
                TextInputBox.SelectedText = "\n";
                break;
            case Keys.Control | Keys.B:
                ToggleSelectedFormat(DiagramTextFormat.Bold);
                break;
            case Keys.Control | Keys.I:
                ToggleSelectedFormat(DiagramTextFormat.Italic);
                break;

            // Only the text, in the box's font: a copied text's fonts, sizes and colors aren't the diagram's
            case Keys.Control | Keys.V or Keys.Shift | Keys.Insert:
                if (Clipboard.ContainsText())
                {
                    TextInputBox.SelectedText = Clipboard.GetText().ReplaceLineEndings("\n");
                }

                break;

            // The box's own formats a diagram's text doesn't have: underline, alignment and line spacing
            case Keys.Control | Keys.U or Keys.Control | Keys.L or Keys.Control | Keys.E or Keys.Control | Keys.R
                or Keys.Control | Keys.J or Keys.Control | Keys.D1 or Keys.Control | Keys.D2
                or Keys.Control | Keys.D5 or Keys.Control | Keys.Shift | Keys.A
                or Keys.Control | Keys.Shift | Keys.L or Keys.Control | Keys.Oemplus
                or Keys.Control | Keys.Shift | Keys.Oemplus:
                break;
            default:
                return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void TextInputBox_Leave(object? sender, EventArgs e)
    {
        CommitText();
    }

    private void DiagramEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        CommitText();

        // Applies what was typed in a panel field, so it counts as a change to ask about
        Canvas.Focus();

        // Only the user is asked: a question could stop Windows shutting down, and the changes are kept anyway
        if (e.CloseReason == CloseReason.UserClosing)
        {
            var closing = new CancelEventArgs();

            _closing?.Invoke(this, closing);
            e.Cancel = closing.Cancel;
        }

        if (!e.Cancel)
        {
            // Its normal size, also while it's maximized or minimized
            Size size = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;

            WindowClosed?.Invoke(this, new DiagramWindowSizeModel(size, WindowState == FormWindowState.Maximized));
        }
    }

    private void DiagramEditorForm_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            _isSpaceDown = false;
        }
    }

    private void DiagramEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // Not with Alt, which closes the window
        if (e.KeyData == Keys.F4)
        {
            TogglePanels();
            e.Handled = true;

            return;
        }

        // Anywhere in the window, even in the find box
        if (e.KeyData is Keys.F3 or (Keys.Shift | Keys.F3))
        {
            Find(e.Shift ? DiagramFindStep.Previous : DiagramFindStep.Next);
            e.Handled = true;

            return;
        }

        if (e.KeyData == (Keys.Control | Keys.F) && !_isEditingText)
        {
            FindBox.Focus();
            FindBox.SelectAll();
            e.Handled = true;
            e.SuppressKeyPress = true;

            return;
        }

        // While typing a text or in a panel, keys are theirs
        if (_isEditingText || IsInAPanel)
        {
            return;
        }

        if (e.KeyCode == Keys.Space)
        {
            _isSpaceDown = true;
        }

        Action? action = KeyAction(e);

        if (action is null)
        {
            return;
        }

        action();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private Action? KeyAction(KeyEventArgs e)
    {
        int step = e.Shift ? 1 : NudgeStep;

        if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down && !e.Control && !e.Alt)
        {
            Size delta = e.KeyCode switch
            {
                Keys.Left => new Size(-step, 0),
                Keys.Right => new Size(step, 0),
                Keys.Up => new Size(0, -step),
                _ => new Size(0, step),
            };

            return () => NudgeRequested?.Invoke(this, delta);
        }

        DiagramCommand? command = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.Control, Keys.N) => DiagramCommand.New,
            (Keys.Control, Keys.O) => DiagramCommand.Open,
            (Keys.Control, Keys.S) => DiagramCommand.Save,
            (Keys.Control | Keys.Shift, Keys.S) => DiagramCommand.SaveAs,
            (Keys.Control, Keys.E) => DiagramCommand.ExportPdf,
            (Keys.Control | Keys.Shift, Keys.E) => DiagramCommand.ExportPng,
            (Keys.Control, Keys.Z) => DiagramCommand.Undo,
            (Keys.Control, Keys.Y) or (Keys.Control | Keys.Shift, Keys.Z) => DiagramCommand.Redo,
            (Keys.Control, Keys.X) => DiagramCommand.Cut,
            (Keys.Control, Keys.C) => DiagramCommand.Copy,
            (Keys.Control, Keys.V) => DiagramCommand.Paste,
            (Keys.Control, Keys.D) => DiagramCommand.Duplicate,
            (Keys.Control, Keys.I) => DiagramCommand.InsertImage,
            (Keys.Control | Keys.Shift, Keys.C) => DiagramCommand.CopyStyle,
            (Keys.Control | Keys.Shift, Keys.P) => DiagramCommand.CopyPicture,
            (Keys.Control, Keys.B) => DiagramCommand.Bold,
            (Keys.Control | Keys.Shift, Keys.V) => DiagramCommand.PasteStyle,
            (Keys.Control, Keys.A) => DiagramCommand.SelectAll,
            (Keys.Control, Keys.G) => DiagramCommand.Group,
            (Keys.Control | Keys.Shift, Keys.G) => DiagramCommand.Ungroup,
            (Keys.Control, Keys.OemCloseBrackets) => DiagramCommand.BringToFront,
            (Keys.Control, Keys.OemOpenBrackets) => DiagramCommand.SendToBack,
            (Keys.Control, Keys.Oemplus or Keys.Add) => DiagramCommand.ZoomIn,
            (Keys.Control, Keys.OemMinus or Keys.Subtract) => DiagramCommand.ZoomOut,
            (Keys.Control, Keys.D0 or Keys.NumPad0) => DiagramCommand.ZoomReset,
            (Keys.None, Keys.Delete or Keys.Back) => DiagramCommand.Delete,
            _ => null,
        };

        // Home: the diagram, or what's selected, at the top left; with Shift, in the middle
        if (e.KeyCode == Keys.Home && (e.Modifiers is Keys.None or Keys.Shift))
        {
            DiagramPlacement placement = e.Shift ? DiagramPlacement.Center : DiagramPlacement.TopLeft;

            return () => BringIntoView(placement);
        }

        if (command is DiagramCommand requested)
        {
            return () => CommandRequested?.Invoke(this, requested);
        }

        DiagramTool? tool = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.None, Keys.V) => DiagramTool.Select,
            (Keys.None, Keys.C) => DiagramTool.Connector,
            (Keys.None, Keys.L) => DiagramTool.Line,
            (Keys.None, Keys.R) => DiagramTool.Rectangle,
            (Keys.None, Keys.U) => DiagramTool.RoundedRectangle,
            (Keys.None, Keys.O) => DiagramTool.Ellipse,
            (Keys.None, Keys.D) => DiagramTool.Diamond,
            (Keys.None, Keys.P) => DiagramTool.Parallelogram,
            (Keys.None, Keys.H) => DiagramTool.Hexagon,
            (Keys.None, Keys.Y) => DiagramTool.Cylinder,
            (Keys.None, Keys.M) => DiagramTool.Document,
            (Keys.None, Keys.B) => DiagramTool.Container,
            (Keys.None, Keys.T) => DiagramTool.Text,
            _ => null,
        };

        return tool is DiagramTool selected ? () => ToolSelected?.Invoke(this, selected) : null;
    }
}
