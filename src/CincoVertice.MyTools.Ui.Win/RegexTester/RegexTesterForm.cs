using CincoVertice.Common.Ui.Win.Controls.Rtf;
using CincoVertice.MyTools.Ui.RegexTester.Models;
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

        // Nothing is made from the DPI here; this rescales the window once, when it's dropped on another monitor
        DpiFollower.Attach(this, _ => { });
    }

    public event EventHandler? InputChanged;

    public event EventHandler<RegexRowModel>? RowChosen;

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

            // A new text: undo starts here
            SampleBox.SetCode(input.Text);
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

    public void SelectText(int start, int length)
    {
        SampleBox.Select(start, length);
        SampleBox.ScrollToCaret();
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
