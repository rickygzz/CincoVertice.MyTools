using CincoVertice.Common.Ui.Win.Controls.Rtf;
using CincoVertice.MyTools.Ui.RegexTester.Models;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using CincoVertice.MyTools.Ui.RegexTester.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.RegexTester;

/// <summary>
///     Regex tester view: the pattern and its options, the text with its matches colored (<see cref="CodeTextControl"/>,
///     whose undo leaves the colors out), the list of matches and groups, and the text replaced.
///     <see cref="Ui.RegexTester.Presenters.RegexTesterPresenter"/> decides what it shows; the form tells it of a change
///     once the user pauses (<see cref="InputTimer"/>), so a long text isn't run at every key.
/// </summary>
public partial class RegexTesterForm : Form, IRegexTesterView
{
    private static readonly Color _matchColor = Color.FromArgb(255, 236, 140);
    private static readonly Color _otherMatchColor = Color.FromArgb(255, 205, 130);

    // A color per group number, again from the first past the last
    private static readonly Color[] _groupColors =
    [
        Color.FromArgb(170, 215, 255),
        Color.FromArgb(175, 235, 175),
        Color.FromArgb(255, 190, 220),
        Color.FromArgb(215, 200, 255),
        Color.FromArgb(160, 235, 230),
    ];

    private IReadOnlyList<RegexRowModel> _rows = [];

    // While the boxes are filled here, not by the user
    private bool _isShowing;

    public RegexTesterForm()
    {
        InitializeComponent();
        FillPieceMenu();
        FillExampleMenu();
        FillReference();

        // Nothing is made from the DPI here; this rescales the window once, when it's dropped on another monitor
        DpiFollower.Attach(this, _ => { });
    }

    public event EventHandler? InputChanged;

    public event EventHandler<RegexRowModel>? RowChosen;

    public event EventHandler<RegexPieceRequestModel>? PieceChosen;

    public event EventHandler<RegexExampleModel>? ExampleChosen;

    public RegexInputModel Input => new(
        PatternBox.Text,
        SampleBox.Text,
        ReplacementBox.Text,
        IgnoreCaseCheckBox.Checked,
        MultilineCheckBox.Checked,
        DotAllCheckBox.Checked);

    /// <summary>The text box, for the tests.</summary>
    internal CodeTextControl Sample => SampleBox;

    /// <summary>The matches list, for the tests.</summary>
    internal ListView Matches => MatchesList;

    /// <summary>The pattern box, for the tests.</summary>
    internal TextBox Pattern => PatternBox;

    /// <summary>The Add menu, for the tests.</summary>
    internal ContextMenuStrip Pieces => PieceMenu;

    /// <summary>The Examples menu, for the tests.</summary>
    internal ContextMenuStrip Examples => ExampleMenu;

    /// <summary>The quick reference and its button, for the tests.</summary>
    internal (CheckBox Button, ListView List) Reference => (ReferenceButton, ReferenceList);

    /// <summary>The color a part is shown in, for the tests.</summary>
    internal static Color ColorOf(RegexHighlightModel highlight) => highlight.Kind switch
    {
        RegexHighlightKind.Match => _matchColor,
        RegexHighlightKind.OtherMatch => _otherMatchColor,
        _ => _groupColors[(Math.Max(1, highlight.Group) - 1) % _groupColors.Length],
    };

    public void ShowInput(RegexInputModel input)
    {
        _isShowing = true;

        try
        {
            PatternBox.Text = input.Pattern;
            ReplacementBox.Text = input.Replacement;
            IgnoreCaseCheckBox.Checked = input.IgnoreCase;
            MultilineCheckBox.Checked = input.Multiline;
            DotAllCheckBox.Checked = input.DotAll;

            // A new text: undo starts here. The same text (an example on the user's) keeps its undo.
            if (SampleBox.Text != input.Text)
            {
                SampleBox.SetCode(input.Text);
            }
        }
        finally
        {
            _isShowing = false;
        }
    }

    public void ShowHighlights(IReadOnlyList<RegexHighlightModel> highlights) =>
        SampleBox.SetBackgrounds([.. highlights.Select(part => new CodeBackground(part.Start, part.Length, ColorOf(part)))]);

    public void ShowRows(IReadOnlyList<RegexRowModel> rows)
    {
        _rows = rows;
        MatchesList.BeginUpdate();

        try
        {
            MatchesList.Items.Clear();

            foreach (RegexRowModel row in rows)
            {
                var item = new ListViewItem([row.Name, row.Value, row.Place]);

                if (row.IsGroup)
                {
                    item.ForeColor = SystemColors.GrayText;
                }

                MatchesList.Items.Add(item);
            }
        }
        finally
        {
            MatchesList.EndUpdate();
        }
    }

    public void ShowStatus(string text, bool isProblem)
    {
        StatusLabel.Text = text;
        StatusLabel.ForeColor = isProblem ? Color.Firebrick : SystemColors.ControlText;
    }

    public void ShowReplaced(string? text)
    {
        ResultBox.Text = text?.ReplaceLineEndings(Environment.NewLine) ?? string.Empty;
        ResultLabel.Enabled = text is not null;
    }

    public void ShowPattern(RegexEditModel pattern, bool eachLine)
    {
        // A change of the user's: it runs once they pause, as typing does
        PatternBox.Text = pattern.Text;
        PatternBox.Select(pattern.SelectionStart, pattern.SelectionLength);
        PatternBox.Focus();

        if (eachLine)
        {
            MultilineCheckBox.Checked = true;
        }
    }

    public void SelectText(int start, int length)
    {
        SampleBox.Select(start, length);
        SampleBox.ScrollToCaret();
    }

    // An item per part, in a submenu per group: what it writes beside its title, what it matches as its tooltip.
    // The items are made here, from the list, so their Click is wired here.
    private void FillPieceMenu()
    {
        var submenus = new Dictionary<string, ToolStripMenuItem>();

        foreach (RegexPieceModel piece in RegexPieces.All)
        {
            var item = new ToolStripMenuItem(piece.Title)
            {
                ShortcutKeyDisplayString = piece.Example,
                ToolTipText = piece.Description,
                Tag = piece,
            };
            item.Click += PieceItem_Click;

            if (piece.Group.Length == 0)
            {
                PieceMenu.Items.Add(item);
                continue;
            }

            if (!submenus.TryGetValue(piece.Group, out ToolStripMenuItem? submenu))
            {
                if (submenus.Count == 0)
                {
                    PieceMenu.Items.Add(new ToolStripSeparator());
                }

                submenu = new ToolStripMenuItem(piece.Group);
                submenu.DropDown.ShowItemToolTips = true;
                submenus.Add(piece.Group, submenu);
                PieceMenu.Items.Add(submenu);
            }

            submenu.DropDownItems.Add(item);
        }
    }

    // A submenu per group, an item per example with what it matches as its tooltip. Made here, from the list, so
    // their Click is wired here.
    private void FillExampleMenu()
    {
        foreach (IGrouping<string, RegexExampleModel> group in RegexExamples.All.GroupBy(example => example.Group))
        {
            var submenu = new ToolStripMenuItem(group.Key);
            submenu.DropDown.ShowItemToolTips = true;

            foreach (RegexExampleModel example in group)
            {
                var item = new ToolStripMenuItem(example.Title) { ToolTipText = example.Description, Tag = example };
                item.Click += ExampleItem_Click;
                submenu.DropDownItems.Add(item);
            }

            ExampleMenu.Items.Add(submenu);
        }
    }

    // A row per part, under its heading
    private void FillReference()
    {
        var groups = new Dictionary<string, ListViewGroup>();
        ReferenceList.BeginUpdate();

        foreach (RegexReferenceModel row in RegexReference.All)
        {
            if (!groups.TryGetValue(row.Group, out ListViewGroup? group))
            {
                group = new ListViewGroup(row.Group);
                groups.Add(row.Group, group);
                ReferenceList.Groups.Add(group);
            }

            // The tooltip shows a description the column cuts
            ReferenceList.Items.Add(
                new ListViewItem([row.Pattern, row.Description], group) { ToolTipText = row.Description });
        }

        ReferenceList.EndUpdate();
    }

    private void ReferenceButton_CheckedChanged(object? sender, EventArgs e) =>
        ReferenceList.Visible = ReferenceButton.Checked;

    private void ExamplesButton_Click(object? sender, EventArgs e) =>
        ExampleMenu.Show(ExamplesButton, new Point(0, ExamplesButton.Height));

    private void ExampleItem_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripItem { Tag: RegexExampleModel example })
        {
            InputTimer.Stop();
            ExampleChosen?.Invoke(this, example);
        }
    }

    private void AddButton_Click(object? sender, EventArgs e) =>
        PieceMenu.Show(AddButton, new Point(0, AddButton.Height));

    // The sample's text can be added only while some of it is selected
    private void PieceMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (ToolStripItem item in PieceMenu.Items)
        {
            if (item.Tag is RegexPieceModel { IsSampleText: true })
            {
                item.Enabled = SampleBox.SelectionLength > 0;
            }
        }
    }

    private void PieceItem_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripItem { Tag: RegexPieceModel piece })
        {
            PieceChosen?.Invoke(
                this,
                new RegexPieceRequestModel(
                    piece,
                    new RegexEditModel(PatternBox.Text, PatternBox.SelectionStart, PatternBox.SelectionLength),
                    SampleBox.SelectedText));
        }
    }

    private void Input_Changed(object? sender, EventArgs e)
    {
        if (_isShowing)
        {
            return;
        }

        InputTimer.Stop();
        InputTimer.Start();
    }

    private void InputTimer_Tick(object? sender, EventArgs e)
    {
        InputTimer.Stop();
        InputChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MatchesList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (MatchesList.SelectedIndices.Count == 1 && MatchesList.SelectedIndices[0] < _rows.Count)
        {
            RowChosen?.Invoke(this, _rows[MatchesList.SelectedIndices[0]]);
        }
    }
}
