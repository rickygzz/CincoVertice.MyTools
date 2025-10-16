using CincoVertice.Common.Win.Controls.RTF;
using CincoVertice.Common.Win.Controls.RTF.Models;

namespace CincoVertice.MyTools.UI.Win.CodeReview.Adapters;

public sealed class ConsoleOutputAdapter : IConsoleOutputAdapter
{
    private readonly ConsoleOutputControl _console;

    public ConsoleOutputAdapter(ConsoleOutputControl console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public void AddColors(params Color[] colors) => _console.AddColor(colors);

    public void Write(string text, RtfFormat format)
    {
        if (_console.InvokeRequired)
        {
            _console.BeginInvoke(new Action(() => _console.AddText(text, format)));
        }
        else
        {
            _console.AddText(text, format);
        }
    }

    public void WriteLine(string text, RtfFormat format)
    {
        if (_console.InvokeRequired)
        {
            _console.BeginInvoke(
                new Action(
                    () =>
                    {
                        _console.AddText(text, format);
                        _console.AddNewLine();
                    }));
        }
        else
        {
            _console.AddText(text, format);
        }
    }

    public void WriteTitle(string title)
    {
        Write(title, new RtfFormat { Size = 12, LineHeight = 1.5, Bold = 1 });
        NewLine();
    }

    public void NewLine(int count = 1)
    {
        Write(new string('\n', Math.Max(1, count)), new RtfFormat());
    }

    public void UpdateText()
    {
        if (_console.InvokeRequired)
        {
            _console.BeginInvoke(new Action(() => _console.UpdateText()));
        }
        else
        {
            _console.UpdateText();
        }
    }
}
