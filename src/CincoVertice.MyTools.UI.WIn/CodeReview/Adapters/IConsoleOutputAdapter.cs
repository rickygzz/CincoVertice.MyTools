using CincoVertice.Common.Win.Controls.RTF.Models;

namespace CincoVertice.MyTools.UI.Win.CodeReview.Adapters;

public interface IConsoleOutputAdapter
{
    void AddColors(params Color[] colors);
    void Write(string text, RtfFormat format);

    void WriteLine(string text, RtfFormat format);

    void NewLine(int count = 1);

    void UpdateText();
}
