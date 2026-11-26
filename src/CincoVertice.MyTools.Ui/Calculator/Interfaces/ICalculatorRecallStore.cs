namespace CincoVertice.MyTools.Ui.Calculator.Interfaces;

/// <summary>The lines entered in the calculator, which Up and Down go back through, kept from one run to the next.</summary>
public interface ICalculatorRecallStore
{
    /// <returns>The lines kept, the newest last; none when there are none or they can't be read.</returns>
    IReadOnlyList<string> Load();

    /// <summary>Keeps these lines, the newest last, instead of those kept before. Never throws.</summary>
    void Save(IReadOnlyList<string> lines);
}
