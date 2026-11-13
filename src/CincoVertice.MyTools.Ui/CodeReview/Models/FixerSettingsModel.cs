using CincoVertice.Common.Application.CodeStandard.Models;

namespace CincoVertice.MyTools.Ui.CodeReview.Models;

/// <summary>What Fix folder does, as the user chose it in the tray menu.</summary>
/// <param name="Off">The fixers turned off, by name; the ones off, so a fixer added later starts on.</param>
/// <param name="LineEnding">The line ending every file gets; <see cref="LineEndingEnum.None"/> keeps them.</param>
/// <param name="Encoding">How every file is written.</param>
public sealed record FixerSettingsModel(
    IReadOnlyCollection<string> Off,
    LineEndingEnum LineEnding = LineEndingEnum.None,
    FileEncodingEnum Encoding = FileEncodingEnum.Keep)
{
    /// <summary>Every fixer on; line endings and encodings as they are.</summary>
    public static FixerSettingsModel Default { get; } = new([]);
}
