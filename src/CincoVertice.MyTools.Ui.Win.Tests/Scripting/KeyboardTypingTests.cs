using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Scripting;

/// <summary>
///     What keyboard.type and keyboard.press send, as a real window gets it: through Windows, to the text box that has
///     the keyboard focus.
/// </summary>
[Collection(KeyboardFocusCollection.Name)]
public class KeyboardTypingTests
{
    private const uint MAPVK_VK_TO_VSC = 0;
    private const byte KeyDown = 0x80;

    [Fact]
    public void SendUnicodeText_TypesEveryCharacter_WhateverTheLayout()
    {
        // Arrange: @ is AltGr+2 on a Spanish layout and Shift+2 on an English one; ñ and € have no key on some
        using Form form = TypingInTheForeground(out TextBox box);

        // Act
        InputSimulator.SendUnicodeText("a@ñ€\U0001F600(x)+{y}^%~ end");
        Pump();

        // Assert
        Assert.Equal("a@ñ€\U0001F600(x)+{y}^%~ end", box.Text);
    }

    [Fact]
    public void SendUnicodeText_NewLinesAndTabs_PressEnterAndTab()
    {
        // Arrange
        using Form form = TypingInTheForeground(out TextBox box);

        // Act
        InputSimulator.SendUnicodeText("a\tb\nc\r\nd");
        Pump();

        // Assert: Enter in a text box of several lines starts one; Tab, where it's accepted, types one
        Assert.Equal("a\tb\r\nc\r\nd", box.Text);
    }

    [Fact]
    public void SendShortcut_WithAltGr_TypesWhatAltGrTypesOnTheLayout()
    {
        // Arrange: what AltGr and the key give on this computer's layout, e.g. @ for 2 on a Spanish one
        using Form form = TypingInTheForeground(out TextBox box);
        (KeyCode key, string expected) = FirstAltGrCharacter();

        // Act
        InputSimulator.SendShortcut(0, key, altGr: true);
        Pump();

        // Assert
        Assert.Equal(expected, box.Text);
    }

    /// <summary>
    ///     A key that types a character with AltGr on the current layout, and the character; skips the test when there
    ///     is none, as on an English (US) layout. Not one another program has as a hotkey with Ctrl+Alt, which AltGr
    ///     is to Windows, such as My Tools' MoveToZone = Ctrl+Alt+1: that program would get it, not the text box.
    /// </summary>
    private static (KeyCode Key, string Text) FirstAltGrCharacter()
    {
        bool isAnyTaken = false;

        var state = new byte[256];
        state[(int)KeyCode.VK_CONTROL] = KeyDown;
        state[(int)KeyCode.VK_LCONTROL] = KeyDown;
        state[(int)KeyCode.VK_MENU] = KeyDown;
        state[(int)KeyCode.VK_RMENU] = KeyDown;

        foreach (KeyCode key in Enumerable.Range((int)KeyCode.D0, 10).Concat(Enumerable.Range((int)KeyCode.A, 26))
            .Select(code => (KeyCode)code))
        {
            var text = new StringBuilder(8);

            // Flag 4: leaves the layout as it was, so a dead key doesn't change what's typed next
            int length = ToUnicode((uint)key, MapVirtualKey((uint)key, MAPVK_VK_TO_VSC), state, text, text.Capacity, 4);

            if (length != 1 || char.IsControl(text[0]))
            {
                continue;
            }

            if (IsTakenAsHotkey(key))
            {
                isAnyTaken = true;

                continue;
            }

            return (key, text.ToString(0, 1));
        }

        Assert.Skip(isAnyTaken
            ? "Every key that types something with AltGr is another program's Ctrl+Alt hotkey, e.g. My Tools'."
            : "This keyboard layout types nothing with AltGr on the letters and digits.");

        // Skip throws; the compiler doesn't know it
        return default;
    }

    // Whether Ctrl+Alt and the key is a hotkey already: Windows lets one program have it
    private static bool IsTakenAsHotkey(KeyCode key)
    {
        const int Id = 0xBFFF;

        if (User32.RegisterHotKey(0, Id, FSModifiers.MOD_CONTROL | FSModifiers.MOD_ALT, key) == 0)
        {
            return true;
        }

        User32.UnregisterHotKey(0, Id);

        return false;
    }

    /// <summary>A text box of several lines in the window in front, with the keyboard focus.</summary>
    private static Form TypingInTheForeground(out TextBox box)
    {
        box = new TextBox { Multiline = true, AcceptsTab = true, AcceptsReturn = true, Dock = DockStyle.Fill };

        var form = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-5000, 100),
            ClientSize = new Size(300, 200),
        };

        form.Controls.Add(box);
        form.Show();
        WindowHelper.ForceForeground(form.Handle);
        form.Activate();
        box.Focus();
        Pump();

        // Without the focus the keys go elsewhere, e.g. with the computer locked; a held key would add to them
        Assert.SkipUnless(box.Focused && Form.ActiveForm == form, "This window can't get the keyboard focus.");
        Assert.SkipWhen(Control.ModifierKeys != Keys.None, "Shift, Ctrl or Alt is held on the keyboard.");

        return form;
    }

    private static void Pump()
    {
        for (int i = 0; i < 30; i++)
        {
            Application.DoEvents();
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicode(
        uint virtualKey,
        uint scanCode,
        byte[] keyState,
        StringBuilder text,
        int capacity,
        uint flags);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint code, uint mapType);
}
