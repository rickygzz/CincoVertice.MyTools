// PSEUDOCODE PLAN:
// 1) Add palette index constants and a flag to ensure colors are only added once.
// 2) Provide EnsureConsoleColorsInitialized() to register colors idempotently.
// 3) Add a UI-thread-safe ConsoleWrite() wrapper around ConsoleOutput.AddText().
// 4) Define an enum for message levels and a helper GetFormat() that returns RtfFormat per level.
// 5) Implement small, focused methods to write common patterns: WriteTitle, WriteInfo, WriteSuccess, WriteWarning, WriteError, WriteTip.
// 6) Add helpers for structure: NewLine, WriteKeyValue, WriteList, WriteWithTimestamp, WriteException, WriteBatch to minimize repaint churn.
// 7) Keep InitializeConsoleOutput minimal and delegate to the helpers for clarity and maintainability.

//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Windows.Forms;
//using CincoVertice.Common.Win.Controls.RTF.Models;

//namespace CincoVertice.MyTools.UI.CodeReview;

//partial class CodeReviewForm
//{
//    // Palette indices mapped to the order in AddColor()
//    private const int Palette_White = 0;
//    private const int Palette_PaleBlue = 1;
//    private const int Palette_PaleGreen = 2;
//    private const int Palette_Blue = 3;
//    private const int Palette_Green = 4;
//    private const int Palette_Orange = 5;
//    private const int Palette_Gold = 6;

//    private bool _consoleColorsInitialized;

//    private enum ConsoleLevel
//    {
//        Default,
//        Title,
//        Info,
//        Success,
//        Warning,
//        Error,
//        Tip,
//        Highlight
//    }

//    private void EnsureConsoleColorsInitialized()
//    {
//        if (_consoleColorsInitialized) return;

//        ConsoleOutput.AddColor(
//            Color.FromArgb(255, 255, 255), // White
//            Color.FromArgb(59, 142, 234),  // Pale blue
//            Color.FromArgb(35, 209, 139),  // Pale green
//            Color.FromArgb(47, 84, 150),   // Blue
//            Color.FromArgb(83, 129, 53),   // Green
//            Color.FromArgb(197, 90, 17),   // Orange
//            Color.FromArgb(191, 144, 0));  // Gold

//        _consoleColorsInitialized = true;
//    }

//    // Marshals to UI thread if required and writes text with format.
//    private void ConsoleWrite(string text, RtfFormat format, bool append = true)
//    {
//        if (ConsoleOutput.InvokeRequired)
//        {
//            ConsoleOutput.BeginInvoke(new Action(() => ConsoleOutput.AddText(text, format, append)));
//        }
//        else
//        {
//            ConsoleOutput.AddText(text, format, append);
//        }
//    }

//    // Returns a format based on the message level and options.
//    private static RtfFormat GetFormat(ConsoleLevel level, bool highlight = false, int size = 10, double? lineHeight = null, bool? bold = null, bool? italic = null)
//    {
//        var fmt = new RtfFormat();

//        // Defaults
//        fmt.Size = size;
//        if (lineHeight.HasValue) fmt.LineHeight = lineHeight.Value;
//        if (bold.HasValue) fmt.Bold = bold.Value ? 1 : 0;
//        if (italic.HasValue) fmt.Italic = italic.Value ? 1 : 0;

//        switch (level)
//        {
//            case ConsoleLevel.Title:
//                fmt.ForeColorIndex = Palette_PaleBlue;
//                fmt.Size = Math.Max(size, 12);
//                fmt.LineHeight = lineHeight ?? 1.5;
//                fmt.Bold = 1;
//                break;

//            case ConsoleLevel.Info:
//                fmt.ForeColorIndex = Palette_PaleBlue;
//                if (highlight) fmt.HighlightColorIndex = Palette_PaleGreen;
//                break;

//            case ConsoleLevel.Success:
//                fmt.ForeColorIndex = Palette_PaleGreen;
//                if (highlight) fmt.HighlightColorIndex = Palette_White;
//                break;

//            case ConsoleLevel.Warning:
//                fmt.ForeColorIndex = Palette_Orange;
//                fmt.Bold = bold ?? true ? 1 : 0;
//                break;

//            case ConsoleLevel.Error:
//                fmt.ForeColorIndex = Palette_Orange;
//                fmt.Bold = 1;
//                if (highlight) fmt.HighlightColorIndex = Palette_Gold;
//                break;

//            case ConsoleLevel.Tip:
//                fmt.ForeColorIndex = Palette_Blue;
//                fmt.Italic = 1;
//                if (highlight) fmt.HighlightColorIndex = Palette_PaleGreen;
//                break;

//            case ConsoleLevel.Highlight:
//                fmt.ForeColorIndex = Palette_White;
//                fmt.HighlightColorIndex = Palette_Blue;
//                fmt.Bold = 1;
//                break;

//            default:
//                fmt.ForeColorIndex = Palette_White;
//                break;
//        }

//        return fmt;
//    }

//    // High-level helpers

//    private void WriteTitle(string title, string? version = null)
//    {
//        EnsureConsoleColorsInitialized();

//        ConsoleWrite(title + (version is null ? string.Empty : " "), GetFormat(ConsoleLevel.Title, size: 12, lineHeight: 1.5, bold: true));
//        if (!string.IsNullOrWhiteSpace(version))
//        {
//            ConsoleWrite(version!, new RtfFormat { Bold = 1 });
//        }
//        NewLine();
//    }

//    private void WriteInfo(string text, bool highlight = false)
//        => ConsoleWrite(text, GetFormat(ConsoleLevel.Info, highlight));

//    private void WriteSuccess(string text, bool highlight = false)
//        => ConsoleWrite(text, GetFormat(ConsoleLevel.Success, highlight));

//    private void WriteWarning(string text, bool highlight = false)
//        => ConsoleWrite(text, GetFormat(ConsoleLevel.Warning, highlight, bold: true));

//    private void WriteError(string text, bool highlight = false)
//        => ConsoleWrite(text, GetFormat(ConsoleLevel.Error, highlight, bold: true));

//    private void WriteTip(string text, bool highlight = false)
//        => ConsoleWrite(text, GetFormat(ConsoleLevel.Tip, highlight, italic: true));

//    private void WriteHighlight(string text)
//        => ConsoleWrite(text, GetFormat(ConsoleLevel.Highlight));

//    private void NewLine(int count = 1)
//        => ConsoleWrite(new string('\n', Math.Max(1, count)), new RtfFormat());

//    private void WriteKeyValue(string key, string value, bool boldKey = true)
//    {
//        ConsoleWrite(key + ": ", new RtfFormat { Bold = boldKey ? 1 : 0, ForeColorIndex = Palette_PaleBlue });
//        ConsoleWrite(value, new RtfFormat { ForeColorIndex = Palette_White });
//        NewLine();
//    }

//    private void WriteList(IEnumerable<string> items, string bullet = "•")
//    {
//        foreach (var item in items)
//        {
//            ConsoleWrite($"{bullet} ", new RtfFormat { ForeColorIndex = Palette_PaleBlue, Bold = 1 });
//            ConsoleWrite(item, new RtfFormat { ForeColorIndex = Palette_White });
//            NewLine();
//        }
//    }

//    private void WriteWithTimestamp(string text, ConsoleLevel level = ConsoleLevel.Info)
//    {
//        var stamp = DateTime.Now.ToString("HH:mm:ss");
//        ConsoleWrite($"[{stamp}] ", new RtfFormat { ForeColorIndex = Palette_Gold });
//        ConsoleWrite(text, GetFormat(level));
//        NewLine();
//    }

//    private void WriteException(Exception ex)
//    {
//        WriteError($"Exception: {ex.Message}", highlight: true);
//        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
//        {
//            ConsoleWrite(ex.StackTrace!, new RtfFormat { ForeColorIndex = Palette_White });
//            NewLine();
//        }
//    }

//    // Batch writing to minimize layout thrashing
//    private void WriteBatch(IEnumerable<(string Text, RtfFormat Format, bool Append)> entries)
//    {
//        if (entries is null) return;

//        ConsoleOutput.SuspendLayout();
//        try
//        {
//            foreach (var (text, fmt, append) in entries)
//            {
//                ConsoleWrite(text, fmt, append);
//            }
//        }
//        finally
//        {
//            ConsoleOutput.ResumeLayout();
//        }
//    }

//    // Example of a clean, minimal initializer using the helpers
//    private void InitializeConsoleOutput()
//    {
//        EnsureConsoleColorsInitialized();

//        WriteTitle("New console", "v1.0.0");

//        WriteInfo("Working on: middleware-services-nubank-quote/common", highlight: true);
//        NewLine();

//        WriteTip("Checkout to DEV and delete branches starting with DEV-");
//        NewLine();
//    }
//}
