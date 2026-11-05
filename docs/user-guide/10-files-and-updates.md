# Files and Updates

## Files the App Creates

Everything below is in the `config` folder next to the exe unless noted.

| File | When it is created | Purpose |
|---|---|---|
| `hotkeys.cfg` | On startup, if missing | Your hotkeys |
| `scripts\GoogleSearch.mts` | On startup, if the `scripts` folder is missing | Example script |
| `activity.cfg` | The first time you turn the mouse mover on, or open it in the config editor | Mouse mover settings |
| `global.vars` | The first time a script uses `global.set` | Values scripts keep between runs |
| `snipeditor.json` | After the first snip editor setting change | Snip editor tool choices |
| `diagrameditor.json` | After diagram editor settings are saved | Diagram editor choices and window size |
| `%LocalAppData%\Microsoft\Windows\Fonts\Roboto.ttf` and a Fonts registry entry | The first time the diagram editor opens, if Roboto is not installed | Lets other programs show saved diagrams in the right font |
| Startup entries in the Windows registry | When you turn **Start with Windows** on | Starts the app when you sign in |

The `scripts` folder is recreated with the example if it is missing when the app next lists scripts. Deleting it while the app runs and then using **Open scripts folder** creates an empty folder.

The app writes no log files and has no internet access of its own, although scripts can open web addresses (the example script `GoogleSearch` does). It keeps no diagnostic files for support.

If the folder holding the exe is protected, the `config` folder cannot be written. Hotkeys and activity settings then use their defaults and report `Could not use <path>, so the default ... used`, the JSON settings do not save, and `global.set` fails with a script error.

---

## Updating and Uninstalling

### Updating

There is no updater, and no setting migration. To update:

1. Choose **Exit** from the tray menu.
2. Replace the exe (and the `licenses` folder) with the new ones in the same folder.
3. Start the new exe.

The new version reads your existing `config` folder. Because the config files are read with the current rules, a line that the new version rejects appears as a problem. The JSON settings files are tolerant: a missing or unreadable one simply means defaults.

If you keep **Start with Windows** on and put the exe in the same place, nothing more is needed. If the exe is at a new path, click **Start with Windows** once (see [Start with Windows](01-getting-started.md#start-with-windows)).

The app does not show its version anywhere. Windows shows it in the exe's file properties.

### Uninstalling

There is no uninstaller. To remove My Tools:

1. Right-click the tray icon. If **Start with Windows** is checked, click it to turn it off. This removes the startup entries. **Exit** alone does not.
2. Choose **Exit**.
3. Delete the folder that holds the exe, including its `config` folder.

**Leftovers.** The app may leave these behind, and nothing removes them for you:

- The startup entries in the Windows registry, if you exited without turning **Start with Windows** off. The entry is the value `MyTools`, under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` and `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run`.
- The Roboto font file `%LocalAppData%\Microsoft\Windows\Fonts\Roboto.ttf` and the registry value `Roboto (TrueType)` under `HKCU\Software\Microsoft\Windows NT\CurrentVersion\Fonts`. They are created when the diagram editor first opens, if Roboto was not installed. Leaving them is harmless.

---

← Previous: [Code Review: Check and Fix a Folder](09-code-review.md) | [Contents](README.md) | Next: [Troubleshooting](11-troubleshooting.md) →
