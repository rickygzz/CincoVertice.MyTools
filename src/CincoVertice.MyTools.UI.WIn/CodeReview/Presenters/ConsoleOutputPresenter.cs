using CincoVertice.Common.Win.Controls.RTF.Models;
using CincoVertice.MyTools.UI.Win.CodeReview.Adapters;

namespace CincoVertice.MyTools.UI.Win.CodeReview.Presenters;

public sealed class ConsoleOutputPresenter
{
    private readonly ConsoleOutputAdapter _output;
    private bool _colorsReady;

    // Palette indices (order must match AddColors)
    // private const int White = 0, PaleBlue = 1, PaleGreen = 2, Blue = 3, Orange = 5, Gold = 6;

    public ConsoleOutputPresenter(ConsoleOutputAdapter output) => _output = output;

    public void Initialize()
    {
        EnsureColors();
        _output.WriteTitle("New console");
        _output.WriteLine("Checkout to DEV and delete branches starting with DEV-", new RtfFormat() { HighlightColorIndex = 3 });
        _output.NewLine();
        _output.UpdateText();
    }

    private void EnsureColors()
    {
        if (_colorsReady)
        {
            return;
        }

        _output.AddColors(
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(59, 142, 234),
            Color.FromArgb(35, 209, 139),
            Color.FromArgb(47, 84, 150),
            Color.FromArgb(83, 129, 53),
            Color.FromArgb(197, 90, 17),
            Color.FromArgb(191, 144, 0));

        _colorsReady = true;
    }

    //private static RtfFormat Fmt(int? fore = null, int? back = null, int? bold = null, int? italic = null, float? size = null, double? line = null)
    //    => new RtfFormat
    //    {
    //        ForeColorIndex = fore ?? -1,
    //        HighlightColorIndex = back ?? -1,
    //        Bold = bold ?? -1,
    //        // Italic = italic ?? -1,
    //        Size = size ?? -1,
    //        LineHeight = line ?? -1
    //    };

    //public void WriteTitle(string title, string? version = null)
    //{
    //    EnsureColors();
    //    _output.Write(title + (version is null ? string.Empty : " "), Fmt(fore: PaleBlue, bold: 1, size: 12, line: 1.5));
    //    if (!string.IsNullOrWhiteSpace(version))
    //        _output.Write(version!, Fmt(bold: 1));
    //    _output.NewLine();
    //}

    //public void WriteInfo(string text, bool highlight = false)
    //    => _output.Write(text, Fmt(fore: PaleBlue, back: highlight ? PaleGreen : null));

    //public void WriteSuccess(string text, bool highlight = false)
    //    => _output.Write(text, Fmt(fore: PaleGreen, back: highlight ? White : null));

    //public void WriteWarning(string text, bool highlight = false)
    //    => _output.Write(text, Fmt(fore: Orange, bold: 1));

    //public void WriteError(string text, bool highlight = false)
    //    => _output.Write(text, Fmt(fore: Orange, bold: 1, back: highlight ? Gold : null));

    //public void WriteTip(string text, bool highlight = false)
    //    => _output.Write(text, Fmt(fore: Blue, italic: 1, back: highlight ? PaleGreen : null));
}
