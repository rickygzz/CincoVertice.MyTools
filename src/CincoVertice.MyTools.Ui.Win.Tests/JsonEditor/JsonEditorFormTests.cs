using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using CincoVertice.MyTools.Ui.Win.JsonEditor;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.JsonEditor;

public class JsonEditorFormTests
{
    private static readonly string[] _json =
    [
        "{",
        "  \"people\": [",
        "    { \"name\": \"Ann\", \"age\": 31 },",
        "    { \"name\": \"Bob\" }",
        "  ],",
        "  \"count\": 2",
        "}",
    ];

    private static JsonNodeModel Root(params string[] lines) => JsonDocumentReader.Read(lines).Root!;

    private static IReadOnlyList<string> Texts(TreeNodeCollection nodes) =>
        [.. nodes.Cast<TreeNode>().Select(node => node.Text)];

    [Fact]
    public void Tabs_EachKeepTheirText_AndTheirOwnUndo()
    {
        string folder = Path.Combine(Path.GetTempPath(), "JsonEditorTabs_" + Guid.NewGuid());
        Directory.CreateDirectory(folder);

        try
        {
            // Arrange: two files, the second opened in a tab of its own, then changed
            string first = Path.Combine(folder, "first.json");
            string second = Path.Combine(folder, "second.json");
            File.WriteAllText(first, "[1]");
            File.WriteAllText(second, "[2]");
            using var form = new JsonEditorForm(isDark: false);
            var presenter = new Ui.JsonEditor.Presenters.JsonEditorPresenter(
                form,
                new Ui.Diff.Services.DiskDiffFileReader(),
                new Ui.Diff.Services.DiskDiffFileWriter());
            presenter.Start(first);
            presenter.Open(second);
            form.TextControl.ReplaceAll("[2, 3]");
            JsonTextControl secondText = form.TextControl;

            // Act / Assert: the first tab shows its own text, unchanged
            Assert.Equal(["first.json", "second.json *"], form.TabTitles);
            form.ClickTab(0);
            Assert.Equal(["[1]"], form.TextControl.Lines);
            Assert.NotSame(secondText, form.TextControl);
            Assert.Equal("first.json - JSON editor", form.Text);

            // Back to the second: its undo takes back its change
            form.ClickTab(1);
            form.TextControl.HandleKey(Keys.Control | Keys.Z);
            Assert.Equal(["[2]"], form.TextControl.Lines);
            Assert.Equal(["first.json", "second.json"], form.TabTitles);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Tabs_RemoveTab_ShowsTheOneBefore_AndCtrlWAsksToCloseTheShownOne()
    {
        // Arrange: three tabs, the last shown
        using var form = new JsonEditorForm(isDark: false);
        form.AddTab("b");
        form.ShowText(["b"]);
        form.AddTab("c");
        List<int> closing = [];
        form.TabCloseRequested += (_, index) => closing.Add(index);

        // Act
        form.RemoveTab(2);

        // Assert
        Assert.Equal(["Untitled", "b"], form.TabTitles);
        Assert.Equal(["b"], form.TextControl.Lines);
        Assert.True(form.PressKey(Keys.Control | Keys.W));
        Assert.Equal([1], closing);
    }

    [Fact]
    public void Tree_ShowsTheDocument_OpeningEachValueWhenAsked()
    {
        // Arrange
        using var form = new JsonEditorForm(isDark: false);
        JsonNodeModel root = Root(_json);

        // Act
        form.ShowTreePanel(true);
        form.ShowTree(root);
        TreeNode people = form.Tree.Nodes[0].Nodes[0];
        IReadOnlyList<string> closed = Texts(people.Nodes);
        form.SelectInTree(root.Member("people")!.Items[1].Member("name"));

        // Assert: what's closed holds only a stand-in until it opens
        Assert.True(form.IsTreeShown);
        Assert.Equal(["people [2]", "count: 2"], Texts(form.Tree.Nodes[0].Nodes));
        Assert.Equal(["…"], closed);
        Assert.Equal(["[0] {2}", "[1] {1}"], Texts(people.Nodes));
        Assert.Equal("name: \"Bob\"", form.Tree.SelectedNode?.Text);
    }

    [Fact]
    public void Tree_ChosenByTheUser_GoesToTheValue_ButNotWhenItFollowsTheCaret()
    {
        StaThread.Run(() =>
        {
            // Arrange: shown, as a tree tells a selection only once its window is made
            using var form = new JsonEditorForm(isDark: false);
            JsonNodeModel root = Root(_json);
            List<JsonNodeModel> chosen = [];
            form.NodeChosen += (_, node) => chosen.Add(node);
            form.Location = new System.Drawing.Point(-3000, -3000);
            form.Show();
            form.ShowTreePanel(true);
            form.ShowTree(root);

            // Act
            form.SelectInTree(root.Member("count"));
            form.Tree.SelectedNode = form.Tree.Nodes[0].Nodes[0];

            // Assert
            Assert.Same(root.Member("people"), Assert.Single(chosen));
        });
    }

    [Fact]
    public void Tree_ShownAgain_KeepsWhatWasOpen()
    {
        // Arrange
        using var form = new JsonEditorForm(isDark: false);
        form.ShowTreePanel(true);
        JsonNodeModel root = Root(_json);
        form.ShowTree(root);

        // Following the caret into Ann opens people
        form.SelectInTree(root.Member("people")!.Items[0]);

        // Act: the document read again, after an edit
        form.ShowTree(Root(_json));

        // Assert
        Assert.True(form.Tree.Nodes[0].Nodes[0].IsExpanded);
        Assert.Equal(["[0] {2}", "[1] {1}"], Texts(form.Tree.Nodes[0].Nodes[0].Nodes));
    }

    [Fact]
    public void Tree_OfAHugeArray_ShowsTheFirstThousand_AndHowManyMore()
    {
        // Arrange
        using var form = new JsonEditorForm(isDark: false);
        form.ShowTreePanel(true);
        string items = string.Join(",", Enumerable.Range(0, 1500));

        // Act
        form.ShowTree(Root($"[{items}]"));

        // Assert
        Assert.Equal(1001, form.Tree.Nodes[0].Nodes.Count);
        Assert.Equal("… 500 more: go to them in the text", form.Tree.Nodes[0].Nodes[1000].Text);
    }

    [Fact]
    public void Crumbs_ShowTheSteps_AndAClickGoesToOne()
    {
        // Arrange
        using var form = new JsonEditorForm(isDark: false);
        JsonNodeModel root = Root(_json);
        JsonNodeModel? chosen = null;
        form.NodeChosen += (_, node) => chosen = node;

        // Act
        form.ShowCrumbs(JsonPathWriter.CrumbsOf(root.Member("people")!.Items[0].Member("age")!));
        IReadOnlyList<string> crumbs = form.CrumbTexts;
        form.ClickCrumb(1);

        // Assert
        Assert.Equal(["people", "[0]", "age"], crumbs);
        Assert.Same(root.Member("people")!.Items[0], chosen);
    }

    [Fact]
    public void SchemaStatus_ShowsOnlyWithASchema()
    {
        // Arrange
        using var form = new JsonEditorForm(isDark: false);

        // Act
        form.ShowSchemaStatus("Schema s.json: 2 problems");
        string? shown = form.SchemaText;
        form.ShowSchemaStatus(null);

        // Assert
        Assert.Equal("Schema s.json: 2 problems", shown);
        Assert.Null(form.SchemaText);
    }

    [Fact]
    public void ReplaceRange_IsOneEdit_ThatUndoTakesBack()
    {
        // Arrange
        using var form = new JsonEditorForm(isDark: false);
        form.ShowText(["{ \"b\": 1, \"a\": 2 }"]);

        // Act
        form.ReplaceRange(new DiffPosition(0, 0), new DiffPosition(0, 18), "{\n  \"a\": 2,\n  \"b\": 1\n}");
        IReadOnlyList<string> replaced = [.. form.Lines];
        form.TextControl.HandleKey(Keys.Control | Keys.Z);

        // Assert
        Assert.Equal(["{", "  \"a\": 2,", "  \"b\": 1", "}"], replaced);
        Assert.Equal(["{ \"b\": 1, \"a\": 2 }"], form.Lines);
    }

    [Fact]
    public void CtrlBracket_JumpsBetweenThePair()
    {
        // Arrange
        using var form = new JsonEditorForm(isDark: false);
        form.ShowText(_json);
        form.ShowBracketPair((new DiffPosition(1, 12), new DiffPosition(4, 2)));
        form.GoTo(new JsonMatchModel(new DiffPosition(1, 12), new DiffPosition(1, 12)));

        // Act
        form.TextControl.HandleKey(Keys.Control | Keys.OemCloseBrackets);
        DiffPosition there = form.TextControl.Caret;
        form.TextControl.HandleKey(Keys.Control | Keys.OemCloseBrackets);

        // Assert
        Assert.Equal(new DiffPosition(4, 2), there);
        Assert.Equal(new DiffPosition(1, 12), form.TextControl.Caret);
    }

    [Fact]
    public void CtrlSpace_OffersTheNamesStartingAsTyped_AndWritesTheOneChosen()
    {
        // Arrange: "ag typed in Bob's object
        using var form = new JsonEditorForm(isDark: false);
        form.KeysRequested += (_, request) => request.Names = ["age", "address", "name"];
        form.ShowText(["{ \"name\": \"Bob\", \"ag }"]);
        form.GoTo(new JsonMatchModel(new DiffPosition(0, 20), new DiffPosition(0, 20)));

        // Act
        IReadOnlyList<string> offered = form.TextControl.ShowKeys(show: false);
        form.TextControl.WriteName(new DiffPosition(0, 17), "age");

        // Assert
        Assert.Equal(["age"], offered);
        Assert.Equal(["{ \"name\": \"Bob\", \"age\":  }"], form.Lines);
    }
}
