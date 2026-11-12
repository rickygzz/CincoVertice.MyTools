# Troubleshooting

| Problem | What to try |
|---|---|
| **No tray icon after starting** | The icon is created visible. Look in the hidden icons area of the taskbar. If another copy is already running, the new one exits silently. |
| **Starting the app again does nothing** | Only one copy runs. The second start exits silently. Any other program with the same process name also blocks the start. |
| **A hotkey does nothing** | Look for the balloon "My Tools: some hotkeys are off", or click **Hotkey problems (N)...** in the tray menu. Check that the line in `hotkeys.cfg` is not a comment, and that you saved the file. If the file changes did not apply, choose **Reload hotkeys**. |
| **`... is not available. <Windows error text>`** | Another program already owns that shortcut. Change it in `hotkeys.cfg` or in the other program, then choose **Reload hotkeys**. Hotkeys are not retried automatically. |
| **A hotkey with a character such as @ or € is refused** | The message reads `'<c>' is typed with AltGr, and a hotkey on its key would stop you from typing it.` A character you type with AltGr cannot be a hotkey, because the hotkey would stop you from typing it. Pick another key. |
| **A script types the wrong characters, or none** | `keyboard.type` sends each character as itself, so the keyboard layout doesn't matter. If a program still gets nothing, it reads only real keys, as some games and remote desktop or virtual machine windows do: use `clipboard.paste(text)` instead. If `keyboard.press` types the wrong character, that key types something else on the window's layout; use `keyboard.type` to write characters. |
| **Only some hotkeys work** | A bad line is skipped; the rest still work. Open **Edit hotkeys...** and read the problem list at the bottom. |
| **Everything after one line in hotkeys.cfg is ignored** | A value starting with `["` was never closed with `"]`. Fix or remove that line. |
| **Settings do not save, or "Could not use <path>..." appears** | The `config` folder is not writable. Move the app to a folder you can write to. |
| **The mouse mover seems not to react to my moves** | It notices only moves that change both the horizontal and vertical position between checks. See [Mouse Mover](06-mouse-mover.md#mouse-mover-activity). |
| **I changed activity.cfg but nothing changed** | Edits apply the next time you turn the mover on. Turn it off and on with Ctrl+Alt+M. |
| **My startup actions did not run** | They run only when Windows starts My Tools at sign-in, with **Start with Windows** on; starting it yourself does not run them. A line that could not be used was shown in a notification titled "My Tools: some startup actions are off", and is squiggled in `startup.cfg` in the config editor. |
| **Did I leave the mouse mover on?** | The tray icon is red while it is on. |
| **My clipboard content disappeared after a snip** | `SnipAndEdit` and `SnipRegion` replace the clipboard with the snip. The snip editor does so again after every change. |
| **Copy text or Ctrl+Alt+T says Windows can't read text in my languages** | Add a language with its **Optical character recognition** feature in **Settings > Time & language > Language & region**. See [Copying the text in a snip](04-snipping.md#copying-the-text-in-a-snip). |
| **The copied text has mistakes** | Small text is hard to read. Zoom in on it before you snip it. |
| **The snip editor ignores my keys** | While you are typing a text, keys belong to the text. Press Esc or Enter to end it. |
| **A script does nothing or shows an error** | The error box is titled `My Tools: <Action>` and gives file, line and column. Scripts are only checked when they run, so open the script in the config editor to see its first syntax error. |
| **A script is stuck waiting** | There is no cancel command. Press Cancel in its dialog, or choose **Exit** from the tray menu. |
| **A diagram will not open** | Only SVG files saved by the diagram editor can be opened. |
| **The Code Review window is empty and titled "CodeReviewForm"** | That is **File > Code review** in the main window, which starts nothing. Use **Check folder for standard...** or **Fix folder for standard...** from the tray menu. |
| **A folder fix changed files I did not want changed** | Fix has no undo. Restore your files from your commit or backup. |
| **The app shows "Application error"** | An unexpected error occurred. The box shows the error text, or "Unknown error." Note the text if you need to ask for help. |

The app writes no log files, so when asking for help include a screenshot of the message you see.

---

← Previous: [Files and Updates](10-files-and-updates.md) | [Contents](README.md) | Next: [Quick Reference: Default Shortcuts](12-quick-reference.md) →
