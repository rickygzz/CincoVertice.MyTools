namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>The pattern as written, with its selection (or its caret, when <paramref name="SelectionLength"/> is 0).</summary>
public sealed record RegexEditModel(string Text, int SelectionStart, int SelectionLength);
