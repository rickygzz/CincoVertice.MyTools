namespace CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;

/// <summary>
///     The keys the config editor offers for a shortcut, by the names HotkeyParser reads (a test checks each one),
///     letters and digits first, then the named keys most used.
/// </summary>
public static class ShortcutKeyNames
{
    public static IReadOnlyList<string> Keys { get; } =
    [
        .. Enumerable.Range('A', 26).Select(letter => ((char)letter).ToString()),
        .. Enumerable.Range(0, 10).Select(digit => digit.ToString()),
        .. Enumerable.Range(1, 24).Select(number => $"F{number}"),
        "Space", "Enter", "Esc", "Tab", "Backspace", "Delete", "Insert", "Home", "End", "PageUp", "PageDown",
        "Up", "Down", "Left", "Right", "PrintScreen", "Pause", "Plus", "Minus",
        .. Enumerable.Range(0, 10).Select(digit => $"NumPad{digit}"),
    ];
}
