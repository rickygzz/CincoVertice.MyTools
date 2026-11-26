using CincoVertice.MyTools.Ui.SettingsBackup.Models;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.SettingsBackup;

/// <summary>
///     The restore's review: every file the restore writes, and the ones only the settings have, which are kept unless
///     the box is ticked. A text file can be compared with its version in the backup before choosing.
/// </summary>
public partial class SettingsRestoreForm : Form
{
    private readonly Action<IWin32Window, SettingsFileModel> _compare;
    private readonly IReadOnlyList<SettingsFileModel> _shown;

    /// <param name="plan">What the restore would do.</param>
    /// <param name="compare">
    ///     Shows a file's two versions side by side, over this window; only given files that can be compared.
    /// </param>
    public SettingsRestoreForm(SettingsRestorePlanModel plan, Action<IWin32Window, SettingsFileModel> compare)
    {
        InitializeComponent();
        _compare = compare;
        NoteLabel.Text = "Before restoring, your current settings are saved to the backups folder. Close My Tools'"
            + " other windows first, and exit and reopen My Tools after, so nothing writes old settings over the"
            + " restored ones.";

        // The same files aren't shown: the restore leaves them alone
        _shown = [.. plan.ToWrite, .. plan.NotInBackup];
        int same = plan.Files.Count - _shown.Count;
        string alone = same switch
        {
            0 => string.Empty,
            1 => " 1 file is the same in both and is left alone.",
            _ => $" {same} files are the same in both and are left alone.",
        };
        SummaryLabel.Text = $"Restoring {Path.GetFileName(plan.BackupPath)} writes {Files(plan.ToWrite.Count)}."
            + alone
            + " Select a text file and choose Compare to see what changes.";
        DeleteCheck.Text = $"Also &delete the {Files(plan.NotInBackup.Count)} the backup doesn't have";
        DeleteCheck.Enabled = plan.NotInBackup.Count > 0;

        FilesList.BeginUpdate();

        foreach (SettingsFileModel file in _shown)
        {
            FilesList.Items.Add(new ListViewItem([file.Path, string.Empty]) { Tag = file });
        }

        FilesList.EndUpdate();
        ShowChanges();
        UpdateCompare();

        DpiFollower.Attach(this, dpi =>
        {
            FileColumn.Width = DpiFollower.Scaled(390, dpi);
            ChangeColumn.Width = DpiFollower.Scaled(200, dpi);
        });
    }

    /// <summary>Whether the user ticked to delete the files the backup doesn't have.</summary>
    public bool DeleteFilesNotInBackup => DeleteCheck.Checked;

    internal IReadOnlyList<string> Rows =>
        [.. FilesList.Items.Cast<ListViewItem>().Select(item => $"{item.Text}: {item.SubItems[1].Text}")];

    internal void Select(int row)
    {
        FilesList.SelectedIndices.Clear();
        FilesList.SelectedIndices.Add(row);
    }

    internal bool CanCompare => CompareFilesButton.Enabled;

    internal void Compare() => CompareFilesButton.PerformClick();

    internal void TickDelete() => DeleteCheck.Checked = true;

    private static string Files(int count) => count == 1 ? "1 file" : $"{count} files";

    private string ChangeOf(SettingsFileModel file) => file.Change switch
    {
        SettingsChangeKind.Added => "Added",
        SettingsChangeKind.Changed => "Replaced",
        _ => DeleteCheck.Checked ? "Deleted" : "Kept: not in the backup",
    };

    private void ShowChanges()
    {
        foreach (ListViewItem item in FilesList.Items)
        {
            item.SubItems[1].Text = ChangeOf((SettingsFileModel)item.Tag!);
        }
    }

    private SettingsFileModel? Selected =>
        FilesList.SelectedItems.Count == 1 ? (SettingsFileModel)FilesList.SelectedItems[0].Tag! : null;

    private void UpdateCompare() => CompareFilesButton.Enabled = Selected?.CanCompare == true;

    private void CompareSelected()
    {
        if (Selected is { CanCompare: true } file)
        {
            _compare(this, file);
        }
    }

    private void FilesList_SelectedIndexChanged(object sender, EventArgs e) => UpdateCompare();

    private void FilesList_DoubleClick(object sender, EventArgs e) => CompareSelected();

    private void CompareFilesButton_Click(object sender, EventArgs e) => CompareSelected();

    private void DeleteCheck_CheckedChanged(object sender, EventArgs e) => ShowChanges();
}
