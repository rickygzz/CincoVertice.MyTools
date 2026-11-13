using CincoVertice.Common.WinApi.Libs;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     What a recording shows of a key pressed: a shortcut, as "Ctrl+Shift+P". Only shortcuts with Ctrl, Alt or Win,
///     and keys that do something on their own, such as Enter, Esc, Tab and F1; never what's typed, so a password or a
///     message never ends up in a video.
/// </summary>
public static class KeyComboText
{
    private static readonly HashSet<Keys> _modifiers =
    [
        Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey, Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
        Keys.Menu, Keys.LMenu, Keys.RMenu, Keys.LWin, Keys.RWin,
    ];

    // Keys shown even without Ctrl, Alt or Win: none of them types text
    private static readonly HashSet<Keys> _onTheirOwn = [Keys.Escape, Keys.Enter, Keys.Tab];

    /// <param name="key">The key pressed.</param>
    /// <param name="modifiers">
    ///     The modifier keys down with it: Control, Alt, Shift; Win is <see cref="Keys.LWin"/>.
    /// </param>
    /// <returns>The text to show, as "Ctrl+Shift+P"; null for a key not to show.</returns>
    public static string? From(Keys key, Keys modifiers)
    {
        if (_modifiers.Contains(key))
        {
            return null;
        }

        bool isWin = modifiers.HasFlag(Keys.LWin);
        bool isShortcut = modifiers.HasFlag(Keys.Control) || modifiers.HasFlag(Keys.Alt) || isWin;
        bool isFunctionKey = key is >= Keys.F1 and <= Keys.F24;

        if (!isShortcut && !isFunctionKey && !_onTheirOwn.Contains(key))
        {
            return null;
        }

        List<string> parts = [];

        if (modifiers.HasFlag(Keys.Control))
        {
            parts.Add("Ctrl");
        }

        if (isWin)
        {
            parts.Add("Win");
        }

        if (modifiers.HasFlag(Keys.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(Keys.Shift))
        {
            parts.Add("Shift");
        }

        parts.Add(NameOf(key));

        return string.Join("+", parts);
    }

    /// <returns>The name of the key as on the keyboard, e.g. "P", "5", "Enter", or "," for a punctuation key.</returns>
    public static string NameOf(Keys key) => key switch
    {
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => "Num " + (key - Keys.NumPad0),
        Keys.Enter => "Enter",
        Keys.Escape => "Esc",
        Keys.Back => "Backspace",
        Keys.Delete => "Del",
        Keys.Insert => "Ins",
        Keys.PageUp => "PageUp",
        Keys.PageDown => "PageDown",
        Keys.Space => "Space",
        Keys.Snapshot => "PrtScn",
        _ when key >= Keys.OemSemicolon && key <= Keys.Oem102 => CharacterOf(key) ?? key.ToString(),
        _ => key.ToString(),
    };

    // A punctuation key's character on the current layout, e.g. "," or "+"
    private static string? CharacterOf(Keys key)
    {
        uint character = User32.MapVirtualKey((uint)key, User32.MAPVK_VK_TO_CHAR) & 0xFFFF;

        return character == 0 ? null : char.ToUpperInvariant((char)character).ToString();
    }
}
