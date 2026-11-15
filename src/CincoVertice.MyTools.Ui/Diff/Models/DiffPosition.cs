namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>A place in a text: before character <see cref="Column"/> of line <see cref="Line"/>, from 0.</summary>
public readonly record struct DiffPosition(int Line, int Column) : IComparable<DiffPosition>
{
    public int CompareTo(DiffPosition other) =>
        Line != other.Line ? Line.CompareTo(other.Line) : Column.CompareTo(other.Column);

    public static bool operator <(DiffPosition a, DiffPosition b) => a.CompareTo(b) < 0;

    public static bool operator >(DiffPosition a, DiffPosition b) => a.CompareTo(b) > 0;

    public static bool operator <=(DiffPosition a, DiffPosition b) => a.CompareTo(b) <= 0;

    public static bool operator >=(DiffPosition a, DiffPosition b) => a.CompareTo(b) >= 0;
}
