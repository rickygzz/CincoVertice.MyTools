using CincoVertice.MyTools.Ui.Diff.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diff;

public class MyersDiffTests
{
    [Fact]
    public void TheSame_HasNoChanges()
    {
        // Act
        (bool[] removed, bool[] added) = MyersDiff.Compare([1, 2, 3], [1, 2, 3], TestContext.Current.CancellationToken);

        // Assert
        Assert.DoesNotContain(true, removed);
        Assert.DoesNotContain(true, added);
    }

    [Fact]
    public void AnItemInserted_IsAdded()
    {
        // Act
        (bool[] removed, bool[] added) = MyersDiff.Compare([1, 2, 3], [1, 2, 9, 3], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([false, false, false], removed);
        Assert.Equal([false, false, true, false], added);
    }

    [Fact]
    public void EmptySides_AreAllAddedOrRemoved()
    {
        // Act
        (bool[] removed, bool[] added) = MyersDiff.Compare([], [1, 2], TestContext.Current.CancellationToken);
        (bool[] removed2, bool[] added2) = MyersDiff.Compare([1, 2], [], TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(removed);
        Assert.Equal([true, true], added);
        Assert.Equal([true, true], removed2);
        Assert.Empty(added2);
    }

    [Fact]
    public void NothingInCommon_IsAllChanged()
    {
        // Act
        (bool[] removed, bool[] added) = MyersDiff.Compare([1, 2, 3], [4, 5], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([true, true, true], removed);
        Assert.Equal([true, true], added);
    }

    [Fact]
    public void RandomSequences_KeepAsManyAsTheLongestCommonOnes_InOrder()
    {
        var random = new Random(1234);

        for (int run = 0; run < 2000; run++)
        {
            // Arrange: few different items, so there's much in common and many ways to match it
            int[] a = Sequence(random);
            int[] b = random.Next(3) == 0 ? Edited(a, random) : Sequence(random);

            // Act
            (bool[] removed, bool[] added) = MyersDiff.Compare(a, b, TestContext.Current.CancellationToken);

            // Assert
            int[] keptA = [.. a.Where((_, i) => !removed[i])];
            int[] keptB = [.. b.Where((_, i) => !added[i])];
            Assert.Equal(keptA, keptB);
            Assert.Equal(LongestCommon(a, b), keptA.Length);
        }
    }

    [Fact]
    public void LongFilesAlike_AreQuick()
    {
        // Arrange: 200,000 lines, a few changed
        int[] a = [.. Enumerable.Range(0, 200_000)];
        int[] b = [.. a];
        b[10] = -1;
        b[100_000] = -2;
        b[199_000] = -3;

        // Act
        (bool[] removed, bool[] added) = MyersDiff.Compare(a, b, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, removed.Count(r => r));
        Assert.Equal(3, added.Count(r => r));
    }

    private static int[] Sequence(Random random) =>
        [.. Enumerable.Range(0, random.Next(0, 25)).Select(_ => random.Next(0, 5))];

    private static int[] Edited(int[] items, Random random)
    {
        var edited = items.ToList();

        for (int i = random.Next(1, 4); i > 0; i--)
        {
            if (edited.Count > 0 && random.Next(2) == 0)
            {
                edited.RemoveAt(random.Next(edited.Count));
            }
            else
            {
                edited.Insert(random.Next(edited.Count + 1), random.Next(0, 5));
            }
        }

        return [.. edited];
    }

    private static int LongestCommon(int[] a, int[] b)
    {
        var lengths = new int[a.Length + 1, b.Length + 1];

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                lengths[i, j] = a[i - 1] == b[j - 1]
                    ? lengths[i - 1, j - 1] + 1
                    : Math.Max(lengths[i - 1, j], lengths[i, j - 1]);
            }
        }

        return lengths[a.Length, b.Length];
    }
}
