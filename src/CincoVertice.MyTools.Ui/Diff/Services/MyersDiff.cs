namespace CincoVertice.MyTools.Ui.Diff.Services;

/// <summary>
///     The fewest items to remove from one sequence and add from another to turn it into the other (Eugene Myers,
///     "An O(ND) Difference Algorithm and Its Variations", 1986), in its linear-space form: each step finds the middle
///     of the path from both ends, then solves the two halves. Items are numbers, so equal lines compare fast.
///     <para>
///         Items with no match on the other side are set aside first: they can't be common, so the result is the same,
///         and two very different files take no longer than two alike.
///     </para>
/// </summary>
public sealed class MyersDiff
{
    private readonly int[] _a;
    private readonly int[] _b;
    private readonly int[] _down;
    private readonly int[] _up;
    private readonly int _offset;

    private MyersDiff(int[] a, int[] b)
    {
        _a = a;
        _b = b;
        _offset = a.Length + b.Length + 1;
        _down = new int[2 * _offset + 2];
        _up = new int[2 * _offset + 2];
    }

    /// <returns>
    ///     Which items of <paramref name="a"/> are removed, and which of <paramref name="b"/> are added; the others are
    ///     common to both, in the same order.
    /// </returns>
    public static (bool[] Removed, bool[] Added) Compare(IReadOnlyList<int> a, IReadOnlyList<int> b)
    {
        var removed = new bool[a.Count];
        var added = new bool[b.Count];
        var inA = new HashSet<int>(a);
        var inB = new HashSet<int>(b);

        // Only the items found on both sides take part; the others are changes whatever else is
        List<int> keptA = Keep(a, inB, removed);
        List<int> keptB = Keep(b, inA, added);
        int[] shortA = [.. keptA.Select(i => a[i])];
        int[] shortB = [.. keptB.Select(i => b[i])];
        var shortRemoved = new bool[shortA.Length];
        var shortAdded = new bool[shortB.Length];

        new MyersDiff(shortA, shortB).Solve(0, shortA.Length, 0, shortB.Length, shortRemoved, shortAdded);

        for (int i = 0; i < keptA.Count; i++)
        {
            removed[keptA[i]] = shortRemoved[i];
        }

        for (int i = 0; i < keptB.Count; i++)
        {
            added[keptB[i]] = shortAdded[i];
        }

        return (removed, added);
    }

    private static List<int> Keep(IReadOnlyList<int> items, HashSet<int> other, bool[] changed)
    {
        var kept = new List<int>(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            if (other.Contains(items[i]))
            {
                kept.Add(i);
            }
            else
            {
                changed[i] = true;
            }
        }

        return kept;
    }

    // A loop where the halves are small, so a long, alike file doesn't recurse deeply on its first half only
    private void Solve(int lowerA, int upperA, int lowerB, int upperB, bool[] removed, bool[] added)
    {
        while (true)
        {
            // The same at the start and the end is common
            while (lowerA < upperA && lowerB < upperB && _a[lowerA] == _b[lowerB])
            {
                lowerA++;
                lowerB++;
            }

            while (lowerA < upperA && lowerB < upperB && _a[upperA - 1] == _b[upperB - 1])
            {
                upperA--;
                upperB--;
            }

            if (lowerA == upperA)
            {
                Array.Fill(added, true, lowerB, upperB - lowerB);

                return;
            }

            if (lowerB == upperB)
            {
                Array.Fill(removed, true, lowerA, upperA - lowerA);

                return;
            }

            (int x, int y) = MiddleSnake(lowerA, upperA, lowerB, upperB);
            Solve(lowerA, x, lowerB, y, removed, added);
            lowerA = x;
            lowerB = y;
        }
    }

    /// <returns>A point the shortest path goes through, between the two ends.</returns>
    private (int X, int Y) MiddleSnake(int lowerA, int upperA, int lowerB, int upperB)
    {
        int downK = lowerA - lowerB;
        int upK = upperA - upperB;
        int delta = upperA - lowerA - (upperB - lowerB);
        bool isOdd = (delta & 1) != 0;
        int downOffset = _offset - downK;
        int upOffset = _offset - upK;
        int maxD = (upperA - lowerA + upperB - lowerB) / 2 + 1;

        _down[downOffset + downK + 1] = lowerA;
        _up[upOffset + upK - 1] = upperA;

        for (int d = 0; d <= maxD; d++)
        {
            // Forward, from the top left
            for (int k = downK - d; k <= downK + d; k += 2)
            {
                int x;

                if (k == downK - d)
                {
                    x = _down[downOffset + k + 1];
                }
                else
                {
                    x = _down[downOffset + k - 1] + 1;

                    if (k < downK + d && _down[downOffset + k + 1] >= x)
                    {
                        x = _down[downOffset + k + 1];
                    }
                }

                int y = x - k;

                while (x < upperA && y < upperB && _a[x] == _b[y])
                {
                    x++;
                    y++;
                }

                _down[downOffset + k] = x;

                if (isOdd && upK - d < k && k < upK + d && _up[upOffset + k] <= _down[downOffset + k])
                {
                    return (_down[downOffset + k], _down[downOffset + k] - k);
                }
            }

            // Backward, from the bottom right
            for (int k = upK - d; k <= upK + d; k += 2)
            {
                int x;

                if (k == upK + d)
                {
                    x = _up[upOffset + k - 1];
                }
                else
                {
                    x = _up[upOffset + k + 1] - 1;

                    if (k > upK - d && _up[upOffset + k - 1] < x)
                    {
                        x = _up[upOffset + k - 1];
                    }
                }

                int y = x - k;

                while (x > lowerA && y > lowerB && _a[x - 1] == _b[y - 1])
                {
                    x--;
                    y--;
                }

                _up[upOffset + k] = x;

                if (!isOdd && downK - d <= k && k <= downK + d && _up[upOffset + k] <= _down[downOffset + k])
                {
                    return (_down[downOffset + k], _down[downOffset + k] - k);
                }
            }
        }

        throw new InvalidOperationException("The paths from both ends never met.");
    }
}
