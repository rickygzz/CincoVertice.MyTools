using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Services;

/// <summary>
///     The emoji the snip editor offers, by group: those most used to mark up a screenshot. Symbols that are text by
///     default carry U+FE0F, so they're drawn as emoji, in color.
/// </summary>
public static class EmojiCatalog
{
    /// <summary>How many emoji dropped last the emoji menu keeps at its top.</summary>
    public const int MaxRecent = 10;

    /// <summary>The emoji the emoji tool starts with.</summary>
    public const string DefaultEmoji = "😀";

    public static IReadOnlyList<EmojiCategoryModel> Categories { get; } =
    [
        new("Faces",
        [
            "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "🙂", "😉", "😊", "😇", "🥰", "😍", "🤩", "😘",
            "😋", "😛", "😜", "🤪", "🤔", "🤨", "😐", "😑", "😶", "🙄", "😏", "😬", "😌", "😴", "😷", "🤒",
            "🤯", "🥳", "😎", "🤓", "😕", "😟", "🙁", "😮", "😲", "😳", "🥺", "😢", "😭", "😱", "😖", "😩",
            "😤", "😡", "🤬", "😈", "💀", "🤡", "👻", "🤖", "💩", "🙈",
        ]),
        new("Gestures",
        [
            "👍", "👎", "👌", "✌️", "🤞", "🤟", "🤘", "🤙", "👈", "👉", "👆", "👇", "☝️", "✋", "👋", "🤚",
            "🖐️", "👏", "🙌", "👐", "🤝", "🙏", "✍️", "💪", "👀", "👁️", "🧠", "🫶", "🤷", "🤦", "🙋", "🙅",
        ]),
        new("Marks and arrows",
        [
            "✅", "✔️", "☑️", "❌", "✖️", "❎", "⚠️", "🚫", "⛔", "❗", "❓", "❕", "❔", "‼️", "⁉️", "💯",
            "🔴", "🟠", "🟡", "🟢", "🔵", "🟣", "⚫", "⚪", "⭐", "🌟", "✨", "🔥", "💥", "💡", "📌", "📍",
            "➡️", "⬅️", "⬆️", "⬇️", "↗️", "↘️", "↙️", "↖️", "↔️", "↕️", "🔄", "🔁", "🔍", "🔎", "🎯", "🚩",
        ]),
        new("Work",
        [
            "💻", "🖥️", "⌨️", "🖱️", "📱", "🖨️", "💾", "📁", "📂", "📄", "📃", "📝", "📋", "📎", "✂️", "🗑️",
            "📊", "📈", "📉", "📅", "⏰", "⏳", "⌛", "🔒", "🔓", "🔑", "⚙️", "🔧", "🔨", "🛠️", "🐛", "🚀",
            "📧", "📨", "💬", "💭", "🗨️", "📢", "🔔", "🔕", "☎️", "📞", "🏷️", "💰", "💵", "🛒", "📦", "🎉",
        ]),
        new("Hearts and symbols",
        [
            "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "🤎", "💔", "❣️", "💕", "💖", "💘", "💝", "♥️",
            "➕", "➖", "➗", "✳️", "♻️", "🔆", "🆗", "🆕", "🆒", "🆘", "🔟", "#️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣",
        ]),
        new("Nature and food",
        [
            "☀️", "🌤️", "⛅", "🌧️", "⛈️", "❄️", "🌈", "🌙", "🌍", "🌱", "🌳", "🌸", "🌻", "🍀", "🐶", "🐱",
            "🦊", "🐻", "🐼", "🐵", "🦄", "🐝", "🦋", "🍎", "🍕", "🍔", "🍩", "🍪", "🎂", "☕", "🍺", "🍷",
        ]),
    ];
}
