using System.Runtime.Versioning;
using CincoVertice.Common.Ui.Rtf.Models;
using CincoVertice.Common.Ui.Rtf.Themes;
using CincoVertice.Common.Ui.Win.Controls.Rtf;
using CincoVertice.MyTools.Ui.CodeReview.Views;

namespace CincoVertice.MyTools.Ui.Win.CodeReview.Views;

[SupportedOSPlatform("windows10.0")]
public class ConsoleOutputView : IConsoleOutputView
{
    public readonly ConsoleOutputControl ConsoleOutput;

    public ConsoleOutputView(ConsoleOutputControl control)
    {
        ConsoleOutput = control;

        InitializeConsoleOutput();
    }

    public void InitializeConsoleOutput()
    {
        RtfColor backcolor = ModernConsole.Colors["Background"];

        ConsoleOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        ConsoleOutput.BackColor = Color.FromArgb(backcolor.Red, backcolor.Green, backcolor.Blue);
        ConsoleOutput.DetectUrls = false;
        ConsoleOutput.Font = new Font("D2CodingLigature Nerd Font Mono", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
        ConsoleOutput.Location = new Point(4, 104);
        ConsoleOutput.Name = "ConsoleOutput";
        ConsoleOutput.ReadOnly = true;
        ConsoleOutput.Size = new Size(913, 511);
        ConsoleOutput.TabIndex = 0;
        ConsoleOutput.Text = "";

        //ConsoleOutput.AddColor(
        //    Color.FromArgb(255, 255, 255), // White
        //    Color.FromArgb(59, 142, 234), // Pale blue
        //    Color.FromArgb(35, 209, 139), // Pale green
        //    Color.FromArgb(47, 84, 150),
        //    Color.FromArgb(83, 129, 53),
        //    Color.FromArgb(197, 90, 17),
        //    Color.FromArgb(191, 144, 0)); -

        foreach (var color in ModernConsole.Colors.Values)
        {
            ConsoleOutput.AddColor(Color.FromArgb(color.Red, color.Green, color.Blue));
        }
    }

    public void OnLoadConsoleOuput(object? sender, EventArgs e)
    {
        DisplayMessage("abc", new RtfFormat()
        {
            Bold = 1,
            ForeColorIndex = ModernConsole.ColorIndex("Accent1Lighter"),
            HighlightColorIndex = ModernConsole.ColorIndex("Accent1Base"),
        });

        DisplayMessage("abc", new RtfFormat()
        {
            Bold = 1,
            ForeColorIndex = ModernConsole.ColorIndex("Accent1Lighter"),
            HighlightColorIndex = ModernConsole.ColorIndex("Accent1Lightest"),
        });

        DisplayMessage("abc", new RtfFormat()
        {
            Bold = 1,
            ForeColorIndex = ModernConsole.ColorIndex("Accent1Lighter"),
            HighlightColorIndex = ModernConsole.ColorIndex("Accent1Lighter"),
        });

        DisplayMessage("abc", new RtfFormat()
        {
            Bold = 1,
            ForeColorIndex = ModernConsole.ColorIndex("Accent1Lighter"),
            HighlightColorIndex = ModernConsole.ColorIndex("Accent1Light"),
        });

        DisplayMessage("abc", new RtfFormat()
        {
            Bold = 1,
            ForeColorIndex = ModernConsole.ColorIndex("Accent1Lighter"),
            HighlightColorIndex = ModernConsole.ColorIndex("Accent1Dark"),
        });

        DisplayMessage("abc", new RtfFormat()
        {
            Bold = 1,
            ForeColorIndex = ModernConsole.ColorIndex("Accent1Lighter"),
            HighlightColorIndex = ModernConsole.ColorIndex("Accent1Darker"),
        });

    }

    public void DisplayMessage(string message, RtfFormat? format = null)
    {
#pragma warning disable S125 // Sections of code should not be commented out
        // if (ConsoleOutput.InvokeRequired)
        // {
        //    ConsoleOutput.BeginInvoke(
        //        new Action(
        //            () =>
        //            {
        //                ConsoleOutput.AddText(message);
        //                ConsoleOutput.AddNewLine();
        //                ConsoleOutput.UpdateText();
        //            }));
        // }
        // else
        // {
        ConsoleOutput.AddText(message, format);

        ConsoleOutput.AddNewLine();
        ConsoleOutput.UpdateText();
        //}
#pragma warning restore S125 // Sections of code should not be commented out
    }
}
