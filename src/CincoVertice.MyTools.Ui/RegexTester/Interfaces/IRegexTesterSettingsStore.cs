using CincoVertice.MyTools.Ui.RegexTester.Models;

namespace CincoVertice.MyTools.Ui.RegexTester.Interfaces;

/// <summary>Keeps what the regex tester had last, so the next window starts with it, even after a restart.</summary>
public interface IRegexTesterSettingsStore
{
    /// <summary>What was saved last; null when there's nothing, or it can't be read.</summary>
    RegexInputModel? Load();

    /// <summary>Saves it. A failure only loses it: it never stops the window.</summary>
    void Save(RegexInputModel input);
}
