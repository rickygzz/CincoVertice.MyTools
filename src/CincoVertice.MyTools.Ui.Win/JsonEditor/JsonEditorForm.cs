using System.ComponentModel;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.JsonEditor;

/// <summary>
///     JSON editor view: the text (<see cref="JsonTextControl"/>), a search box for text or JMESPath with its
///     suggestions, the result of an expression under the text, and the path at the caret in the status bar.
///     <see cref="Ui.JsonEditor.Presenters.JsonEditorPresenter"/> decides everything.
/// </summary>
public partial class JsonEditorForm : Form, IJsonEditorView
{
    private const int MaxSuggestionsShown = 10;

    private IReadOnlyList<JsonSuggestionModel> _suggestions = [];

    // While the search text or mode is set here, not by the user
    private bool _isSettingSearch;

    // An array or object shows at most this many of its values in the tree, then how many more
    private const int MaxTreeChildren = 1000;

    private bool _isDark;
    private Color _textColor = SystemColors.ControlText;
    private Color _detailColor = SystemColors.GrayText;

    // While the tree's selection is set here, following the caret, not by the user
    private bool _isSelectingInTree;
    private readonly List<ToolStripItem> _crumbItems = [];
    private IReadOnlyList<JsonCrumbModel> _crumbs = [];

    // A text per tab, each with its own undo, sections and scroll, and the tab's button; only the shown one is visible
    private readonly List<JsonTextControl> _editors = [];
    private readonly List<ToolStripButton> _tabButtons = [];
    private int _shown;

    public JsonEditorForm()
        : this(DarkMode.IsOn())
    {
    }

    /// <param name="isDark">Dark, whatever Windows' mode is; for the tests.</param>
    internal JsonEditorForm(bool isDark)
    {
        InitializeComponent();
        _editors.Add(FirstEditor);
        _tabButtons.Add(TabButtonOf("Untitled"));
        _tabButtons[0].Checked = true;
        _isDark = isDark;
        ApplyColors();
        _isSettingSearch = true;
        ModeComboBox.SelectedIndex = 0;
        _isSettingSearch = false;
        DpiFollower.Attach(
            this,
            dpi => SuggestionsList.ItemHeight = SuggestionsList.Font.Height + DpiFollower.Scaled(4, dpi));
    }

    public event EventHandler? ThemeToggled;

    public event EventHandler? TreeToggled;

    public event EventHandler<JsonNodeModel>? NodeChosen;

    public event EventHandler<JsonHoverRequestModel>? HoverRequested;

    public event EventHandler<JsonKeyRequestModel>? KeysRequested;

    public event EventHandler<bool>? SortKeysRequested;

    public event EventHandler<JsonCompareSource>? CompareRequested;

    public event EventHandler? GoToPathRequested;

    public event EventHandler<bool>? CopyResultRequested;

    public event EventHandler? RepairRequested;

    public event EventHandler<JsonConversionKind>? ConvertRequested;

    public event EventHandler<bool>? SchemaRequested;

    public event EventHandler<int>? ProblemStepRequested;

    public event EventHandler? TextEdited;

    public event EventHandler<DiffPosition>? CaretMoved;

    public event EventHandler? NewRequested;

    public event EventHandler? OpenRequested;

    public event EventHandler? SaveRequested;

    public event EventHandler? SaveAsRequested;

    public event EventHandler? FormatRequested;

    public event EventHandler? MinifyRequested;

    public event EventHandler<string>? FileDropped;

    public event EventHandler? SearchChanged;

    public event EventHandler<int>? SearchStepRequested;

    public event EventHandler<JsonSuggestionModel>? SuggestionChosen;

    public event EventHandler<JsonPathRequestModel>? PathRequested;

    public event EventHandler<CancelEventArgs>? CloseRequested;

    public event EventHandler<int>? TabChosen;

    public event EventHandler<int>? TabCloseRequested;

    public IReadOnlyList<string> Lines => Editor.Lines;

    /// <summary>The text of the tab shown.</summary>
    private JsonTextControl Editor => _editors[_shown];

    /// <summary>The tabs' titles, for tests.</summary>
    internal IReadOnlyList<string> TabTitles => [.. _tabButtons.Select(button => button.Text ?? string.Empty)];

    /// <summary>A click on a tab, for tests.</summary>
    internal void ClickTab(int index) => _tabButtons[index].PerformClick();

    /// <summary>A shortcut pressed in the window, for tests: whether the window took it.</summary>
    internal bool PressKey(Keys keys)
    {
        var message = default(Message);

        return ProcessCmdKey(ref message, keys);
    }

    public IReadOnlyList<string> LinesOf(int tab) => _editors[tab].Lines;

    public void AddTab(string title)
    {
        var editor = new JsonTextControl { Dock = DockStyle.Fill, Visible = false };
        editor.TextEdited += Editor_TextEdited;
        editor.CaretMoved += Editor_CaretMoved;
        editor.PathRequested += Editor_PathRequested;
        editor.HoverRequested += Editor_HoverRequested;
        editor.KeysRequested += Editor_KeysRequested;
        editor.SortKeysHereRequested += Editor_SortKeysHereRequested;
        editor.GoToPathRequested += Editor_GoToPathRequested;
        editor.ShowTheme(_isDark);

        // In front, so it's laid out after the tab bar, in what's left
        TreeSplit.Panel2.Controls.Add(editor);
        editor.BringToFront();
        _editors.Add(editor);
        _tabButtons.Add(TabButtonOf(title));
        SelectTab(_editors.Count - 1);
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= _editors.Count)
        {
            return;
        }

        bool hadFocus = Editor.ContainsFocus;
        _shown = index;

        for (int i = 0; i < _editors.Count; i++)
        {
            _tabButtons[i].Checked = i == index;
        }

        // The new one shown before the old one hides, so the area never shows empty
        Editor.Visible = true;

        foreach (JsonTextControl other in _editors.Where(editor => editor != Editor))
        {
            other.Visible = false;
        }

        if (hadFocus || !SearchBox.Focused)
        {
            Editor.Focus();
        }
    }

    public void RemoveTab(int index)
    {
        if (index < 0 || index >= _editors.Count || _editors.Count == 1)
        {
            return;
        }

        JsonTextControl editor = _editors[index];
        ToolStripButton button = _tabButtons[index];
        _editors.RemoveAt(index);
        _tabButtons.RemoveAt(index);
        TabStrip.Items.Remove(button);
        button.Dispose();
        TreeSplit.Panel2.Controls.Remove(editor);
        editor.Dispose();

        if (_shown >= index && _shown > 0)
        {
            _shown--;
        }

        SelectTab(_shown);
    }

    public void ShowTabTitle(int index, string title)
    {
        if (index >= 0 && index < _tabButtons.Count && _tabButtons[index].Text != title)
        {
            _tabButtons[index].Text = title;
        }
    }

    // A tab's button, after the others; a click shows it, a middle click closes it
    private ToolStripButton TabButtonOf(string title)
    {
        var button = new ToolStripButton(title)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText = "Middle-click or Ctrl+W to close; Ctrl+Tab for the next tab",
        };
        button.Click += TabButton_Click;
        button.MouseUp += TabButton_MouseUp;
        TabStrip.Items.Add(button);

        return button;
    }

    public bool IsDark => _isDark;

    /// <summary>
    ///     Shows two texts side by side over this window: Compare files', given by whoever makes the window.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<string, string, string, string, IWin32Window>? CompareTexts { get; set; }

    internal TreeView Tree => JsonTree;

    internal bool IsTreeShown => !TreeSplit.Panel1Collapsed;

    internal IReadOnlyList<string> CrumbTexts =>
        [.. _crumbItems.Where(item => item.Tag is not null).Select(item => item.Text!)];

    internal string? SchemaText => SchemaLabel.Available ? SchemaLabel.Text : null;

    /// <summary>A click on a step of the breadcrumb, for tests.</summary>
    internal void ClickCrumb(int index) =>
        _crumbItems.Where(item => item.Tag is not null).ElementAt(index).PerformClick();

    public string SearchText => SearchBox.Text;

    public JsonSearchMode SearchMode => ModeComboBox.SelectedIndex == 1 ? JsonSearchMode.JmesPath : JsonSearchMode.Text;

    internal JsonTextControl TextControl => Editor;

    internal IReadOnlyList<JsonSuggestionModel> SuggestionsShown => SuggestionsList.Visible ? _suggestions : [];

    public void ShowText(IReadOnlyList<string> lines) => Editor.SetText(lines);

    public void ReplaceText(string text) => Editor.ReplaceAll(text);

    public void ShowDocument(JsonDocumentModel document) => Editor.ShowDocument(document);

    public void ShowTitle(string title) => Text = title;

    public void ShowTheme(bool isDark)
    {
        if (isDark == _isDark)
        {
            return;
        }

        _isDark = isDark;
        ApplyColors();

        if (IsHandleCreated)
        {
            ApplyWindowTheme();
        }

        Invalidate(invalidateChildren: true);
    }

    public void ShowStatus(string position, string path, string validity)
    {
        PositionLabel.Text = position;
        PathLabel.ToolTipText = path.Length == 0 ? null : $"{path}: right-click a value to copy its path";
        ValidityLabel.Text = validity;
        ValidityLabel.ForeColor = validity is "Valid JSON" or "Empty" ? _textColor : ProblemColor;
    }

    public void ShowMatches(IReadOnlyList<JsonMatchModel> matches, int current) => Editor.ShowMatches(matches, current);

    public void GoTo(JsonMatchModel match) => Editor.Select(match);

    public void ShowSearchMessage(string message, bool isProblem)
    {
        SearchMessageLabel.Text = message;
        SearchMessageLabel.ForeColor = isProblem ? ProblemColor : _textColor;
        Tips.SetToolTip(SearchMessageLabel, message);
    }

    public void ShowResult(string? json)
    {
        Split.Panel2Collapsed = json is null;
        ResultBox.Text = json?.ReplaceLineEndings("\r\n") ?? string.Empty;
    }

    public void ShowSuggestions(IReadOnlyList<JsonSuggestionModel> suggestions)
    {
        _suggestions = suggestions;

        if (suggestions.Count == 0 || !SearchBox.Focused)
        {
            HideSuggestions();

            return;
        }

        SuggestionsList.BeginUpdate();
        SuggestionsList.Items.Clear();
        SuggestionsList.Items.AddRange([.. suggestions.Select(suggestion => (object)suggestion.Text)]);
        SuggestionsList.SelectedIndex = 0;
        SuggestionsList.EndUpdate();

        Point below = PointToClient(SearchBox.PointToScreen(new Point(0, SearchBox.Height)));
        int rows = Math.Min(suggestions.Count, MaxSuggestionsShown);
        SuggestionsList.Bounds = new Rectangle(
            below.X,
            below.Y,
            Math.Max(SearchBox.Width, DpiFollower.Scaled(360, DeviceDpi)),
            rows * SuggestionsList.ItemHeight + 4);
        SuggestionsList.Visible = true;
        SuggestionsList.BringToFront();
    }

    public void SetSearchText(string text)
    {
        SearchBox.Text = text;
        SearchBox.SelectionStart = text.Length;
        SearchBox.Focus();
    }

    public string? AskOpenPath()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? AskSavePath(string fileName)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save JSON",
            FileName = fileName,
            DefaultExt = ".json",
            AddExtension = true,
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public bool? AskSaveChanges()
    {
        DialogResult answer = MessageBox.Show(
            this,
            "Save the changes first?",
            "JSON editor",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => true,
            DialogResult.No => false,
            _ => null,
        };
    }

    public void ShowProblem(string message) =>
        MessageBox.Show(this, message, "JSON editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public void ShowMessage(string message) =>
        MessageBox.Show(this, message, "JSON editor", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public void ReplaceRange(DiffPosition start, DiffPosition end, string text) =>
        Editor.ReplaceRange(start, end, text);

    public void ShowTreePanel(bool isShown)
    {
        TreeSplit.Panel1Collapsed = !isShown;
        TreeButton.Checked = isShown;

        if (!isShown)
        {
            JsonTree.Nodes.Clear();
        }
    }

    // Built again for each document, keeping open what was open; each value's children are made when it opens
    public void ShowTree(JsonNodeModel? root)
    {
        HashSet<string> open = [.. OpenPaths(JsonTree.Nodes)];
        JsonTree.BeginUpdate();
        JsonTree.Nodes.Clear();

        if (root is not null)
        {
            TreeNode top = TreeNodeOf(root);
            JsonTree.Nodes.Add(top);
            Fill(top);
            top.Expand();
            Reopen(top.Nodes, open);
        }

        JsonTree.EndUpdate();
    }

    public void SelectInTree(JsonNodeModel? node)
    {
        if (node is null || !IsTreeShown || JsonTree.Nodes.Count == 0)
        {
            return;
        }

        TreeNode? found = null;
        TreeNodeCollection level = JsonTree.Nodes;

        foreach (JsonNodeModel step in Ui.JsonEditor.Services.JsonTree.LineageOf(node))
        {
            TreeNode? match = level.Cast<TreeNode>().FirstOrDefault(item => ReferenceEquals(item.Tag, step));

            if (match is null)
            {
                break;
            }

            found = match;

            if (!ReferenceEquals(step, node))
            {
                Fill(match);
                match.Expand();
                level = match.Nodes;
            }
        }

        if (found is not null && found != JsonTree.SelectedNode)
        {
            _isSelectingInTree = true;
            JsonTree.SelectedNode = found;
            _isSelectingInTree = false;
        }
    }

    public void ShowCrumbs(IReadOnlyList<JsonCrumbModel> crumbs)
    {
        if (crumbs.Count == _crumbs.Count
            && crumbs.Zip(_crumbs).All(pair => ReferenceEquals(pair.First.Node, pair.Second.Node)))
        {
            return;
        }

        _crumbs = crumbs;
        Status.SuspendLayout();

        foreach (ToolStripItem old in _crumbItems)
        {
            Status.Items.Remove(old);
            old.Dispose();
        }

        _crumbItems.Clear();
        int at = Status.Items.IndexOf(PathLabel);

        for (int i = 0; i < crumbs.Count; i++)
        {
            if (i > 0)
            {
                _crumbItems.Add(new ToolStripStatusLabel("›") { Margin = Padding.Empty });
            }

            var crumb = new ToolStripStatusLabel(crumbs[i].Text)
            {
                IsLink = true,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Tag = crumbs[i].Node,
                Margin = Padding.Empty,
                ToolTipText = $"Go to {Ui.JsonEditor.Services.JsonPathWriter.PathOf(crumbs[i].Node)}",
            };
            crumb.Click += Crumb_Click;
            _crumbItems.Add(crumb);
        }

        foreach (ToolStripItem item in _crumbItems)
        {
            Status.Items.Insert(at++, item);
        }

        ColorCrumbs();
        Status.ResumeLayout();
    }

    public void ShowBracketPair((DiffPosition Open, DiffPosition Close)? pair) => Editor.ShowBracketPair(pair);

    public void ShowSchemaProblems(IReadOnlyList<JsonSchemaProblemModel> problems) =>
        Editor.ShowProblemMarks([.. problems.Select(problem => (problem.Start, problem.End))]);

    public void ShowSchemaStatus(string? status)
    {
        SchemaLabel.Visible = status is not null;
        SchemaLabel.Text = status ?? string.Empty;
    }

    public void ShowComparison(string leftName, string leftText, string rightName, string rightText) =>
        CompareTexts?.Invoke(leftName, leftText, rightName, rightText, this);

    public string? AskPath(string current) =>
        Scripting.InputDialog.Show(
            "Go to path",
            "The JMESPath of the value to go to, such as people[3].name:",
            current);

    public string? AskSchemaPath() =>
        AskFile("Choose a JSON Schema", "JSON Schema (*.json)|*.json|All files (*.*)|*.*");

    public string? AskCsvPath() => AskFile("Open CSV as JSON", "CSV files (*.csv)|*.csv|All files (*.*)|*.*");

    public string? AskExportPath(string fileName)
    {
        bool isCsv = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
        using var dialog = new SaveFileDialog
        {
            Title = isCsv ? "Save as CSV" : "Save as YAML",
            FileName = fileName,
            AddExtension = true,
            DefaultExt = isCsv ? ".csv" : ".yaml",
            Filter = isCsv ? "CSV files (*.csv)|*.csv" : "YAML files (*.yaml;*.yml)|*.yaml;*.yml",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? ReadClipboard() => Clipboard.ContainsText() ? Clipboard.GetText() : null;

    public void WriteClipboard(string text)
    {
        if (text.Length > 0)
        {
            Clipboard.SetText(text);
        }
    }

    private string? AskFile(string title, string filter)
    {
        using var dialog = new OpenFileDialog { Title = title, Filter = filter };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    private static TreeNode TreeNodeOf(JsonNodeModel node)
    {
        var item = new TreeNode(Ui.JsonEditor.Services.JsonTree.LabelOf(node)) { Tag = node };

        // A stand-in child gives it its + until it's opened
        if (Ui.JsonEditor.Services.JsonTree.ChildrenOf(node).Count > 0)
        {
            item.Nodes.Add(new TreeNode("…"));
        }

        return item;
    }

    private static void Fill(TreeNode item)
    {
        if (item.Tag is not JsonNodeModel node || item.Nodes.Count != 1 || item.Nodes[0].Tag is not null)
        {
            return;
        }

        IReadOnlyList<JsonNodeModel> children = Ui.JsonEditor.Services.JsonTree.ChildrenOf(node);
        item.Nodes.Clear();
        item.Nodes.AddRange([.. children.Take(MaxTreeChildren).Select(TreeNodeOf)]);

        if (children.Count > MaxTreeChildren)
        {
            item.Nodes.Add(new TreeNode($"… {children.Count - MaxTreeChildren:N0} more: go to them in the text"));
        }
    }

    private static IEnumerable<string> OpenPaths(TreeNodeCollection nodes)
    {
        foreach (TreeNode item in nodes)
        {
            if (item.IsExpanded && item.Tag is JsonNodeModel node)
            {
                yield return Ui.JsonEditor.Services.JsonPathWriter.PathOf(node);

                foreach (string path in OpenPaths(item.Nodes))
                {
                    yield return path;
                }
            }
        }
    }

    private static void Reopen(TreeNodeCollection nodes, HashSet<string> open)
    {
        foreach (TreeNode item in nodes)
        {
            if (item.Tag is JsonNodeModel node && open.Contains(Ui.JsonEditor.Services.JsonPathWriter.PathOf(node)))
            {
                Fill(item);
                item.Expand();
                Reopen(item.Nodes, open);
            }
        }
    }

    private void ColorCrumbs()
    {
        Color link = _isDark ? Color.FromArgb(120, 180, 255) : Color.FromArgb(0, 90, 180);

        foreach (ToolStripStatusLabel crumb in _crumbItems.OfType<ToolStripStatusLabel>())
        {
            crumb.LinkColor = link;
            crumb.ActiveLinkColor = link;
            crumb.VisitedLinkColor = link;
        }

        SchemaLabel.LinkColor = _isDark ? Color.FromArgb(230, 180, 60) : Color.FromArgb(170, 100, 0);
        SchemaLabel.ActiveLinkColor = SchemaLabel.LinkColor;
        SchemaLabel.VisitedLinkColor = SchemaLabel.LinkColor;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.N:
                NewRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.W or Keys.Control | Keys.F4:
                TabCloseRequested?.Invoke(this, _shown);
                return true;
            case Keys.Control | Keys.Tab or Keys.Control | Keys.PageDown:
                ChooseTab((_shown + 1) % _editors.Count);
                return true;
            case Keys.Control | Keys.Shift | Keys.Tab or Keys.Control | Keys.PageUp:
                ChooseTab((_shown + _editors.Count - 1) % _editors.Count);
                return true;
            case Keys.Control | Keys.O:
                OpenRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.S:
                SaveRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.Shift | Keys.S:
                SaveAsRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.F:
                FocusSearch(JsonSearchMode.Text);
                return true;
            case Keys.Control | Keys.J:
                FocusSearch(JsonSearchMode.JmesPath);
                return true;
            case Keys.F3:
                SearchStepRequested?.Invoke(this, 1);
                return true;
            case Keys.Shift | Keys.F3:
                SearchStepRequested?.Invoke(this, -1);
                return true;
            case Keys.Shift | Keys.Alt | Keys.F:
                FormatRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.T:
                TreeToggled?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.G:
                GoToPathRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.F8:
                ProblemStepRequested?.Invoke(this, 1);
                return true;
            case Keys.Shift | Keys.F8:
                ProblemStepRequested?.Invoke(this, -1);
                return true;

            // Sections, from anywhere in the window: the search box too
            case Keys.Control | Keys.OemMinus or Keys.Control | Keys.Subtract:
            case Keys.Control | Keys.Oemplus or Keys.Control | Keys.Add:
            case Keys.Control | Keys.Shift | Keys.OemMinus or Keys.Control | Keys.Shift | Keys.Subtract:
            case Keys.Control | Keys.Shift | Keys.Oemplus or Keys.Control | Keys.Shift | Keys.Add:
                return Editor.HandleKey(keyData);
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    /// <summary>Windows draws the title bar and the scroll bars; dark, it's told so once they're there.</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_isDark)
        {
            ApplyWindowTheme();
        }
    }

    private Color ProblemColor => _isDark ? Color.FromArgb(244, 112, 112) : Color.Firebrick;

    private void ApplyWindowTheme()
    {
        DarkMode.UseDarkTitleBar(Handle, _isDark);
        DarkMode.UseDarkScrollBars(ResultBox.Handle, _isDark);
        DarkMode.UseDarkScrollBars(SuggestionsList.Handle, _isDark);
        DarkMode.UseDarkScrollBars(JsonTree.Handle, _isDark);
    }

    // The toolbar, search, result and status bar; the text draws its own
    private void ApplyColors()
    {
        Color back = _isDark ? DarkColorTable.Background : SystemColors.Control;
        Color box = _isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Window;
        _textColor = _isDark ? DarkColorTable.Text : SystemColors.ControlText;
        _detailColor = _isDark ? Color.FromArgb(150, 150, 150) : SystemColors.GrayText;

        BackColor = back;
        ForeColor = _textColor;

        foreach (ToolStrip strip in new ToolStrip[] { Tools, TabStrip, Status })
        {
            if (_isDark)
            {
                strip.Renderer = new DarkToolStripRenderer();
            }
            else
            {
                strip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            }

            strip.BackColor = back;
            strip.ForeColor = _textColor;
        }

        ThemeButton.Text = _isDark ? "&Light" : "&Dark";
        ThemeButton.ToolTipText = _isDark ? "Show the editor in light colors" : "Show the editor in dark colors";
        SearchLayout.BackColor = back;
        Split.BackColor = back;
        ResultLabel.ForeColor = _textColor;

        foreach (Control input in new Control[] { ModeComboBox, SearchBox, ResultBox, SuggestionsList })
        {
            input.BackColor = box;
            input.ForeColor = _textColor;
        }

        ModeComboBox.FlatStyle = _isDark ? FlatStyle.Flat : FlatStyle.Standard;

        foreach (Button button in new[] { PreviousButton, NextButton })
        {
            button.FlatStyle = _isDark ? FlatStyle.Flat : FlatStyle.Standard;
            button.UseVisualStyleBackColor = !_isDark;
            button.BackColor = _isDark ? Color.FromArgb(62, 62, 64) : SystemColors.Control;
            button.ForeColor = _textColor;
        }

        SearchMessageLabel.ForeColor = _textColor;
        ValidityLabel.ForeColor = ValidityLabel.Text is "Valid JSON" or "Empty" or "" ? _textColor : ProblemColor;
        JsonTree.BackColor = box;
        JsonTree.ForeColor = _textColor;
        JsonTree.LineColor = _detailColor;

        // Drop-down menus don't take their toolbar's renderer
        foreach (ToolStripDropDown menu in new ToolStripDropDown[]
            { CompareButton.DropDown, ConvertButton.DropDown, SchemaButton.DropDown, ResultMenu })
        {
            if (_isDark)
            {
                menu.Renderer = new DarkToolStripRenderer();
            }
            else
            {
                menu.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            }

            menu.ForeColor = _textColor;
        }

        ColorCrumbs();
        foreach (JsonTextControl editor in _editors)
        {
            editor.ShowTheme(_isDark);
        }
    }

    private void FocusSearch(JsonSearchMode mode)
    {
        int index = mode == JsonSearchMode.JmesPath ? 1 : 0;

        if (ModeComboBox.SelectedIndex != index)
        {
            ModeComboBox.SelectedIndex = index;
        }

        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void HideSuggestions() => SuggestionsList.Visible = false;

    private void Choose(int index)
    {
        if (index >= 0 && index < _suggestions.Count)
        {
            JsonSuggestionModel suggestion = _suggestions[index];
            HideSuggestions();
            SuggestionChosen?.Invoke(this, suggestion);
        }
    }

    // The user's choice of a tab: shown here, then the presenter shows its document
    private void ChooseTab(int index)
    {
        if (index != _shown)
        {
            SelectTab(index);
            TabChosen?.Invoke(this, index);
        }
    }

    private void TabButton_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripButton button)
        {
            ChooseTab(_tabButtons.IndexOf(button));
        }
    }

    private void TabButton_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle && sender is ToolStripButton button)
        {
            TabCloseRequested?.Invoke(this, _tabButtons.IndexOf(button));
        }
    }

    private void CloseTabButton_Click(object? sender, EventArgs e) => TabCloseRequested?.Invoke(this, _shown);

    private void NewButton_Click(object? sender, EventArgs e) => NewRequested?.Invoke(this, EventArgs.Empty);

    private void OpenButton_Click(object? sender, EventArgs e) => OpenRequested?.Invoke(this, EventArgs.Empty);

    private void SaveButton_Click(object? sender, EventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);

    private void SaveAsButton_Click(object? sender, EventArgs e) => SaveAsRequested?.Invoke(this, EventArgs.Empty);

    private void FormatButton_Click(object? sender, EventArgs e) => FormatRequested?.Invoke(this, EventArgs.Empty);

    private void MinifyButton_Click(object? sender, EventArgs e) => MinifyRequested?.Invoke(this, EventArgs.Empty);

    private void CollapseAllButton_Click(object? sender, EventArgs e) => Editor.CollapseAll();

    private void ExpandAllButton_Click(object? sender, EventArgs e) => Editor.ExpandAll();

    private void ThemeButton_Click(object? sender, EventArgs e) => ThemeToggled?.Invoke(this, EventArgs.Empty);

    private void TreeButton_Click(object? sender, EventArgs e) => TreeToggled?.Invoke(this, EventArgs.Empty);

    private void SortKeysButton_Click(object? sender, EventArgs e) => SortKeysRequested?.Invoke(this, false);

    private void RepairButton_Click(object? sender, EventArgs e) => RepairRequested?.Invoke(this, EventArgs.Empty);

    private void CompareClipboardItem_Click(object? sender, EventArgs e) =>
        CompareRequested?.Invoke(this, JsonCompareSource.Clipboard);

    private void CompareFileItem_Click(object? sender, EventArgs e) =>
        CompareRequested?.Invoke(this, JsonCompareSource.File);

    private void CopyYamlItem_Click(object? sender, EventArgs e) =>
        ConvertRequested?.Invoke(this, JsonConversionKind.CopyYaml);

    private void CopyCsvItem_Click(object? sender, EventArgs e) =>
        ConvertRequested?.Invoke(this, JsonConversionKind.CopyCsv);

    private void SaveYamlItem_Click(object? sender, EventArgs e) =>
        ConvertRequested?.Invoke(this, JsonConversionKind.SaveYaml);

    private void SaveCsvItem_Click(object? sender, EventArgs e) =>
        ConvertRequested?.Invoke(this, JsonConversionKind.SaveCsv);

    private void OpenCsvItem_Click(object? sender, EventArgs e) =>
        ConvertRequested?.Invoke(this, JsonConversionKind.OpenCsv);

    private void PasteCsvItem_Click(object? sender, EventArgs e) =>
        ConvertRequested?.Invoke(this, JsonConversionKind.PasteCsv);

    private void ChooseSchemaItem_Click(object? sender, EventArgs e) => SchemaRequested?.Invoke(this, true);

    private void NoSchemaItem_Click(object? sender, EventArgs e) => SchemaRequested?.Invoke(this, false);

    private void SchemaLabel_Click(object? sender, EventArgs e) => ProblemStepRequested?.Invoke(this, 1);

    private void CopyResultJsonItem_Click(object? sender, EventArgs e) => CopyResultRequested?.Invoke(this, false);

    private void CopyResultLinesItem_Click(object? sender, EventArgs e) => CopyResultRequested?.Invoke(this, true);

    private void Crumb_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripItem { Tag: JsonNodeModel node })
        {
            NodeChosen?.Invoke(this, node);
            Editor.Focus();
        }
    }

    private void JsonTree_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node is not null)
        {
            Fill(e.Node);
        }
    }

    // Only the user's choice goes to the value; one made here follows the caret
    private void JsonTree_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (!_isSelectingInTree && e.Node?.Tag is JsonNodeModel node)
        {
            NodeChosen?.Invoke(this, node);
        }
    }

    private void Editor_HoverRequested(object? sender, JsonHoverRequestModel request) =>
        HoverRequested?.Invoke(this, request);

    private void Editor_KeysRequested(object? sender, JsonKeyRequestModel request) =>
        KeysRequested?.Invoke(this, request);

    private void Editor_SortKeysHereRequested(object? sender, EventArgs e) => SortKeysRequested?.Invoke(this, true);

    private void Editor_GoToPathRequested(object? sender, EventArgs e) =>
        GoToPathRequested?.Invoke(this, EventArgs.Empty);

    private void PreviousButton_Click(object? sender, EventArgs e) => SearchStepRequested?.Invoke(this, -1);

    private void NextButton_Click(object? sender, EventArgs e) => SearchStepRequested?.Invoke(this, 1);

    private void ModeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isSettingSearch)
        {
            SearchChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e) => SearchChanged?.Invoke(this, EventArgs.Empty);

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        bool listed = SuggestionsList.Visible && SuggestionsList.Items.Count > 0;

        switch (e.KeyCode)
        {
            case Keys.Down when listed:
                SuggestionsList.SelectedIndex =
                    Math.Min(SuggestionsList.SelectedIndex + 1, SuggestionsList.Items.Count - 1);
                break;
            case Keys.Up when listed:
                SuggestionsList.SelectedIndex = Math.Max(SuggestionsList.SelectedIndex - 1, 0);
                break;
            case Keys.Tab when listed && !e.Shift:
                Choose(SuggestionsList.SelectedIndex);
                break;
            case Keys.Enter when listed:
                Choose(SuggestionsList.SelectedIndex);
                break;
            case Keys.Enter:
                HideSuggestions();
                SearchStepRequested?.Invoke(this, e.Shift ? -1 : 1);
                break;
            case Keys.Escape when listed:
                HideSuggestions();
                break;
            case Keys.Escape:
                Editor.Focus();
                break;
            default:
                return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void SearchBox_Leave(object? sender, EventArgs e)
    {
        // After the focus has gone where it goes: to the list for a click on it, which then chooses
        BeginInvoke(() =>
        {
            if (!SuggestionsList.Focused)
            {
                HideSuggestions();
            }
        });
    }

    private void SuggestionsList_MouseClick(object? sender, MouseEventArgs e) =>
        Choose(SuggestionsList.IndexFromPoint(e.Location));

    private void SuggestionsList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _suggestions.Count)
        {
            return;
        }

        e.DrawBackground();
        JsonSuggestionModel suggestion = _suggestions[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        Color text = selected ? SystemColors.HighlightText : _textColor;
        Color detail = selected ? SystemColors.HighlightText : _detailColor;
        int padding = DpiFollower.Scaled(4, DeviceDpi);
        Rectangle bounds = Rectangle.Inflate(e.Bounds, -padding, 0);
        const TextFormatFlags Flags =
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
        Font font = SuggestionsList.Font;

        TextRenderer.DrawText(e.Graphics, suggestion.Text, font, bounds, text, Flags);
        TextRenderer.DrawText(e.Graphics, suggestion.Detail, font, bounds, detail, Flags | TextFormatFlags.Right);
    }

    // Only the shown text is the presenter's; a hidden one says nothing
    private void Editor_TextEdited(object? sender, EventArgs e)
    {
        if (sender == Editor)
        {
            TextEdited?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Editor_CaretMoved(object? sender, DiffPosition caret)
    {
        if (sender == Editor)
        {
            CaretMoved?.Invoke(this, caret);
        }
    }

    private void Editor_PathRequested(object? sender, JsonPathRequestModel request) =>
        PathRequested?.Invoke(this, request);

    private void JsonEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            var closing = new CancelEventArgs();
            CloseRequested?.Invoke(this, closing);
            e.Cancel = closing.Cancel;
        }
    }

    private void JsonEditorForm_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void JsonEditorForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
        {
            FileDropped?.Invoke(this, paths[0]);
        }
    }
}
