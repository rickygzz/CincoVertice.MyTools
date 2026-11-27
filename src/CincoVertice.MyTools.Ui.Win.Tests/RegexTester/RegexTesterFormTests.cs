using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.RegexTester.Models;
using CincoVertice.MyTools.Ui.RegexTester.Presenters;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using CincoVertice.MyTools.Ui.Win.RegexTester;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.RegexTester;

/// <summary>The form with its presenter, as the action opens it, shown off the screen.</summary>
public sealed class RegexTesterFormTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "RegexTesterFormTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private RegexTesterForm Open(RegexInputModel saved)
    {
        var store = new JsonFileRegexTesterSettingsStore(Path.Combine(_folder, "regextester.json"));
        store.Save(saved);

        var form = new RegexTesterForm
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-5000, 100),
            ShowInTaskbar = false,
        };

        new RegexTesterPresenter(form, store).Start();
        form.Show();
        Application.DoEvents();

        return form;
    }

    private static Color BackColorAt(RegexTesterForm form, int index)
    {
        form.Sample.Select(index, 1);

        return form.Sample.SelectionBackColor;
    }

    [Fact]
    public void Open_ShowsWhatWasSaved_ColorsTheMatchesAndGroups_AndListsThem()
    {
        // Act: "a1" and "b2", the digit a group of each
        using RegexTesterForm form = Open(new RegexInputModel(@"\w(\d)", "a1 b2", "<$1>"));

        // Assert
        Assert.Equal(@"\w(\d)", form.Input.Pattern);
        Assert.Equal("a1 b2", form.Sample.Text);
        Assert.Equal(
            [
                RegexTesterForm.ColorOf(new RegexHighlightModel(0, 1, RegexHighlightKind.Match)).ToArgb(),
                RegexTesterForm.ColorOf(new RegexHighlightModel(0, 1, RegexHighlightKind.Group, 1)).ToArgb(),
                form.Sample.BackColor.ToArgb(),
                RegexTesterForm.ColorOf(new RegexHighlightModel(0, 1, RegexHighlightKind.OtherMatch)).ToArgb(),
            ],
            Enumerable.Range(0, 4).Select(i => BackColorAt(form, i).ToArgb()));
        Assert.Equal(
            ["Match 1", "  Group 1", "Match 2", "  Group 1"],
            form.Matches.Items.Cast<ListViewItem>().Select(item => item.Text));
    }

    [Fact]
    public void ChoosingARow_SelectsItsPartOfTheText()
    {
        // Arrange
        using RegexTesterForm form = Open(new RegexInputModel(@"\d+", "ab 123 cd"));
        form.Sample.Select(0, 0);

        // Act
        form.Matches.Items[0].Selected = true;
        Application.DoEvents();

        // Assert
        Assert.Equal((3, 3), (form.Sample.SelectionStart, form.Sample.SelectionLength));
    }

    [Fact]
    public void AddMenu_WritesThePartAroundWhatsSelectedInThePattern_AndRunsIt()
    {
        // Arrange: "b" selected in the pattern
        using RegexTesterForm form = Open(new RegexInputModel("ab", "abc ab"));
        form.Pattern.Select(1, 1);
        ToolStripItem endsWith = form.Pieces.Items.Cast<ToolStripItem>().Single(item => item.Text == "Ends with…");

        // Act
        endsWith.PerformClick();
        Thread.Sleep(400);
        Application.DoEvents();

        // Assert: only the "ab" at the end matches
        Assert.Equal("ab$", form.Input.Pattern);
        Assert.Equal(["Match 1"], form.Matches.Items.Cast<ListViewItem>().Select(item => item.Text));
        Assert.Equal("at 4, 2 long", form.Matches.Items[0].SubItems[2].Text);
    }

    [Fact]
    public void AddMenu_HasEveryPart_InItsSubmenu()
    {
        // Arrange
        using RegexTesterForm form = Open(new RegexInputModel("", ""));

        // Act
        IEnumerable<string> titles = form.Pieces.Items.OfType<ToolStripMenuItem>()
            .SelectMany(item => item.HasDropDownItems
                ? item.DropDownItems.OfType<ToolStripMenuItem>().Select(inner => inner.Text ?? "")
                : [item.Text ?? ""]);

        // Assert
        Assert.Equal(RegexPieces.All.Select(piece => piece.Title), titles);
    }

    [Fact]
    public void ExamplesMenu_OnAnEmptyText_ShowsTheExampleWithItsSample()
    {
        // Arrange
        using RegexTesterForm form = Open(new RegexInputModel("", ""));
        ToolStripMenuItem email = form.Examples.Items.OfType<ToolStripMenuItem>()
            .SelectMany(group => group.DropDownItems.OfType<ToolStripMenuItem>())
            .Single(item => item.Text == "Email address");

        // Act
        email.PerformClick();
        Application.DoEvents();

        // Assert
        RegexExampleModel example = RegexExamples.All.Single(e => e.Title == "Email address");
        Assert.Equal((example.Pattern, example.Sample), (form.Input.Pattern, form.Sample.Text));
        Assert.Equal(["Match 1", "Match 2"], form.Matches.Items.Cast<ListViewItem>().Select(item => item.Text));
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 7)]
    public void GroupColors_ComeAgainAfterTheLast(int group, int sameAs)
    {
        Assert.Equal(
            RegexTesterForm.ColorOf(new RegexHighlightModel(0, 1, RegexHighlightKind.Group, group)),
            RegexTesterForm.ColorOf(new RegexHighlightModel(0, 1, RegexHighlightKind.Group, sameAs)));
    }
}
