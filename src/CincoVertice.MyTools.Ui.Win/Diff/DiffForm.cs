using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Presenters;
using CincoVertice.MyTools.Ui.Diff.Views;

namespace CincoVertice.MyTools.Ui.Win.Diff;

/// <summary>
///     Compare files view: the toolbar, what each side shows, the rows side by side (<see cref="DiffTextControl"/>) and
///     a status bar. Files can be dropped on either side. <see cref="DiffPresenter"/> decides everything shown.
/// </summary>
public partial class DiffForm : Form, IDiffView
{
    public DiffForm()
    {
        InitializeComponent();

        // The side titles line up with the sides, which stop before the change strip and the scroll bar
        HeaderTable.Padding = new Padding(0, 0, DiffText.RightMargin, 0);
    }

    public event EventHandler<DiffFilesModel>? FilesChosen;

    public event EventHandler<DiffPasteModel>? TextPasted;

    public event EventHandler? SwapRequested;

    public event EventHandler? ReloadRequested;

    public event EventHandler? WindowActivated;

    public event EventHandler<DiffOptionsModel>? OptionsChanged;

    public event EventHandler<int>? NextChangeRequested;

    public event EventHandler<int>? PreviousChangeRequested;

    public void ShowTitle(string title)
    {
        Text = title;
    }

    public void ShowSides(string left, string right)
    {
        LeftTitleLabel.Text = left;
        RightTitleLabel.Text = right;
    }

    public void ShowDiff(DiffResultModel result, bool keepPlace)
    {
        DiffText.SetRows(result.Rows, result.Hunks, keepPlace);
    }

    public void ShowChange(DiffHunkModel? change)
    {
        DiffText.ShowHunk(change);
    }

    public void ShowStatus(string summary, string position)
    {
        SummaryLabel.Text = summary;
        PositionLabel.Text = position;
    }

    public void ShowCanNavigate(bool canNavigate)
    {
        PreviousButton.Enabled = canNavigate;
        NextButton.Enabled = canNavigate;
    }

    public void ShowProblem(string message)
    {
        MessageBox.Show(this, message, DiffPresenter.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F7:
                NextChangeRequested?.Invoke(this, DiffText.CurrentRow);
                return true;
            case Keys.Shift | Keys.F7:
                PreviousChangeRequested?.Invoke(this, DiffText.CurrentRow);
                return true;
            case Keys.F5:
                ReloadRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.O:
                ChooseFiles(DiffSide.Left);
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private void ChooseFiles(DiffSide side)
    {
        OpenDialog.Title = side == DiffSide.Left
            ? "Left file (pick two to compare them)"
            : "Right file (pick two to compare them)";

        if (OpenDialog.ShowDialog(this) == DialogResult.OK)
        {
            FilesChosen?.Invoke(this, new DiffFilesModel(OpenDialog.FileNames, side));
        }
    }

    private void Paste(DiffSide side)
    {
        string text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
        TextPasted?.Invoke(this, new DiffPasteModel(side, text));
    }

    private void LeftFileButton_Click(object? sender, EventArgs e) => ChooseFiles(DiffSide.Left);

    private void RightFileButton_Click(object? sender, EventArgs e) => ChooseFiles(DiffSide.Right);

    private void PasteLeftButton_Click(object? sender, EventArgs e) => Paste(DiffSide.Left);

    private void PasteRightButton_Click(object? sender, EventArgs e) => Paste(DiffSide.Right);

    private void SwapButton_Click(object? sender, EventArgs e) => SwapRequested?.Invoke(this, EventArgs.Empty);

    private void ReloadButton_Click(object? sender, EventArgs e) => ReloadRequested?.Invoke(this, EventArgs.Empty);

    private void PreviousButton_Click(object? sender, EventArgs e) =>
        PreviousChangeRequested?.Invoke(this, DiffText.CurrentRow);

    private void NextButton_Click(object? sender, EventArgs e) =>
        NextChangeRequested?.Invoke(this, DiffText.CurrentRow);

    private void IgnoreButton_CheckedChanged(object? sender, EventArgs e) =>
        OptionsChanged?.Invoke(
            this,
            new DiffOptionsModel(IgnoreWhitespaceButton.Checked, IgnoreCaseButton.Checked));

    private void DiffForm_Activated(object? sender, EventArgs e) => WindowActivated?.Invoke(this, EventArgs.Empty);

    private void DiffText_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void DiffText_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }

        DiffSide side = DiffText.SideAt(DiffText.PointToClient(new Point(e.X, e.Y)));
        FilesChosen?.Invoke(this, new DiffFilesModel(paths, side));
    }
}
