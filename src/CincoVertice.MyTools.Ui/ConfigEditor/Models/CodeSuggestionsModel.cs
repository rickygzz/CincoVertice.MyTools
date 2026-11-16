namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>What to offer at the caret, and the part of the text a choice replaces: what was typed so far.</summary>
public sealed record CodeSuggestionsModel(int Start, int Length, IReadOnlyList<CodeSuggestionModel> Items);
