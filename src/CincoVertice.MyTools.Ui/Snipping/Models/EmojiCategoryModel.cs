namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>A group of emoji to choose from, e.g. faces.</summary>
public sealed record EmojiCategoryModel(string Name, IReadOnlyList<string> Emojis);
