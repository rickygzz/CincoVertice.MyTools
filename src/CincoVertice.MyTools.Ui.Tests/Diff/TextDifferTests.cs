using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diff;

public class TextDifferTests
{
    private static readonly DiffOptionsModel _exact = new();

    [Fact]
    public void SplitLines_TakesEveryLineEnding_AndNoEmptyLastLine()
    {
        // Act
        string[] lines = TextDiffer.SplitLines("a\r\nb\nc\rd\n");

        // Assert
        Assert.Equal(["a", "b", "c", "d"], lines);
        Assert.Empty(TextDiffer.SplitLines(string.Empty));
        Assert.Equal(["", ""], TextDiffer.SplitLines("\n\n"));
    }

    [Fact]
    public void Compare_GivesRowsSideBySide_WithLineNumbers()
    {
        // Act
        DiffResultModel result = TextDiffer.Compare("one\ntwo\nthree\nfour", "one\n2\nthree\nfour\nfive", _exact, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            [DiffRowKind.Same, DiffRowKind.Changed, DiffRowKind.Same, DiffRowKind.Same, DiffRowKind.Added],
            result.Rows.Select(row => row.Kind));
        Assert.Equal(2, result.Rows[1].Left!.Number);
        Assert.Equal("2", result.Rows[1].Right!.Text);
        Assert.Null(result.Rows[4].Left);
        Assert.Equal(5, result.Rows[4].Right!.Number);
        Assert.Equal([new DiffHunkModel(1, 1), new DiffHunkModel(4, 1)], result.Hunks);
        Assert.Equal((1, 0, 1), (result.Added, result.Removed, result.Changed));
    }

    [Fact]
    public void Compare_PairsRemovedAndAddedLines_AndKeepsTheRestApart()
    {
        // Act
        DiffResultModel result = TextDiffer.Compare("x\na\nb\nc\ny", "x\nA\ny", _exact, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            [DiffRowKind.Same, DiffRowKind.Changed, DiffRowKind.Removed, DiffRowKind.Removed, DiffRowKind.Same],
            result.Rows.Select(row => row.Kind));
        Assert.Equal([new DiffHunkModel(1, 3)], result.Hunks);
        Assert.Equal((0, 2, 1), (result.Added, result.Removed, result.Changed));
    }

    [Fact]
    public void Compare_LineEndings_NeverDiffer()
    {
        // Act
        DiffResultModel result = TextDiffer.Compare("a\r\nb\r\n", "a\nb", _exact, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.HasChanges);
    }

    [Fact]
    public void Compare_CanIgnoreWhitespace_AndCase()
    {
        // Act
        DiffResultModel whitespace =
            TextDiffer.Compare("int x = 1;", "int  x=1;\t", new DiffOptionsModel(IgnoreWhitespace: true), TestContext.Current.CancellationToken);
        DiffResultModel exact = TextDiffer.Compare("int x = 1;", "int  x=1;\t", _exact, TestContext.Current.CancellationToken);
        DiffResultModel noCase = TextDiffer.Compare("Hello", "HELLO", new DiffOptionsModel(IgnoreCase: true), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(whitespace.HasChanges);
        Assert.True(exact.HasChanges);
        Assert.False(noCase.HasChanges);
    }

    [Fact]
    public void ChangedWords_MarksOnlyTheWordsThatDiffer()
    {
        // Act
        (IReadOnlyList<DiffRange> left, IReadOnlyList<DiffRange> right) =
            TextDiffer.ChangedWords("var total = price * 2;", "var total = cost * 3;", _exact, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([new DiffRange(12, 5), new DiffRange(20, 1)], left);
        Assert.Equal([new DiffRange(12, 4), new DiffRange(19, 1)], right);
    }

    [Theory]
    [InlineData("abc", "abcd", "", "3+1")]
    [InlineData("colour", "color", "4+1", "")]
    [InlineData("Hello world", "Hallo world", "1+1", "1+1")]
    [InlineData("x = total;", "x = totals;", "", "9+1")]
    public void ChangedWords_MostlyTheSame_MarksOnlyTheCharactersThatDiffer(
        string left,
        string right,
        string leftMarked,
        string rightMarked)
    {
        // Act
        (IReadOnlyList<DiffRange> leftRanges, IReadOnlyList<DiffRange> rightRanges) =
            TextDiffer.ChangedWords(left, right, _exact, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(leftMarked, string.Join(" ", leftRanges.Select(range => $"{range.Start}+{range.Length}")));
        Assert.Equal(rightMarked, string.Join(" ", rightRanges.Select(range => $"{range.Start}+{range.Length}")));
    }

    [Fact]
    public void ChangedWords_Replaced_AreMarkedWhole()
    {
        // Act: "name" and "id" share nothing, "price" and "cost" only a c
        (IReadOnlyList<DiffRange> left, IReadOnlyList<DiffRange> right) =
            TextDiffer.ChangedWords("get(name, price)", "get(id, cost)", _exact, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([new DiffRange(4, 4), new DiffRange(10, 5)], left);
        Assert.Equal([new DiffRange(4, 2), new DiffRange(8, 4)], right);
    }

    [Fact]
    public void ChangedWords_NextToEachOther_AreOneRange()
    {
        // Act
        (IReadOnlyList<DiffRange> left, IReadOnlyList<DiffRange> right) =
            TextDiffer.ChangedWords("a b c d", "a x y d", _exact, TestContext.Current.CancellationToken);

        // Assert: "b c" and "x y", spaces between included
        Assert.Equal([new DiffRange(2, 3)], left);
        Assert.Equal([new DiffRange(2, 3)], right);
    }

    [Fact]
    public void ChangedWords_IgnoringWhitespace_NeverMarksSpaces()
    {
        // Act
        (IReadOnlyList<DiffRange> left, IReadOnlyList<DiffRange> right) =
            TextDiffer.ChangedWords("f(a,  b)", "f(a, c)", new DiffOptionsModel(IgnoreWhitespace: true), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([new DiffRange(6, 1)], left);
        Assert.Equal([new DiffRange(5, 1)], right);
    }

    [Fact]
    public void Compare_NothingInCommon_IsAllChanged()
    {
        // Act
        DiffResultModel result = TextDiffer.Compare("a\nb", "c\nd\ne", _exact, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            [DiffRowKind.Changed, DiffRowKind.Changed, DiffRowKind.Added],
            result.Rows.Select(row => row.Kind));
        Assert.Single(result.Hunks);
    }

    [Fact]
    public void Compare_AnEmptySide_IsAllAdded()
    {
        // Act
        DiffResultModel result = TextDiffer.Compare(string.Empty, "a\nb", _exact, TestContext.Current.CancellationToken);

        // Assert
        Assert.All(result.Rows, row => Assert.Equal(DiffRowKind.Added, row.Kind));
        Assert.Equal(2, result.Added);
    }
}
