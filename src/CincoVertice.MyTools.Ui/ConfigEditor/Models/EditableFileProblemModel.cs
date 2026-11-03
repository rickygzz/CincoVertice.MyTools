namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>Something wrong in a file. Line and Column start at 1; without a Column, it's about the whole line.</summary>
public sealed record EditableFileProblemModel(int Line, string Message, int? Column = null);
