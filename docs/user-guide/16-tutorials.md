# Try MyTools: Eight Short Tutorials

Pick something useful and try it. These walkthroughs start with the default shortcuts; you can also run tools from
the tray icon's **Actions** menu. If a shortcut is unavailable, check **Hotkey problems** in the tray menu.

First time here? [Start MyTools](01-getting-started.md), then try the first tutorial. You can see the finished examples
in the [online screenshot tour](https://github.com/rickygzz/CincoVertice.MyTools#see-it-in-action).

## 1. Explain a screenshot

**Goal:** send someone a picture that makes the next step clear. **Allow:** about two minutes.

1. Open a page or document you want to explain. For practice, use a simple checklist with no personal information.
2. Press **Ctrl+Alt+S**. Drag around the useful part of the screen and release.
3. In the snip editor, choose **Arrow** or press **A**. Drag toward the thing you want the other person to notice.
4. Choose **Text** or press **T**, click beside the arrow, and type a short instruction, such as "Start here".
5. Want numbered steps? Choose the **Step** tool and click where the first number goes, then the second.
6. If a detail should not be shared, choose **Blur** or press **B** and drag over the entire detail. Check the edges.
7. Press **Ctrl+C** to copy the result. Paste it into your message, or press **Ctrl+S** to save an image.

**You should see:** an image with your arrow, labels, and other annotations. Undo a misplaced mark with **Ctrl+Z**.

**Try next:** take another snip while the editor is open. Use **Alt+Left** and **Alt+Right** to move between snips.
Use Crop to keep only the useful part. [All snip editor tools](04-snipping.md).

## 2. Copy text from a picture

**Goal:** turn words on screen into text you can paste. **Allow:** about one minute.

1. Show a screenshot or document whose text you cannot select. For a first try, choose large, clear text.
2. Press **Ctrl+Alt+T**.
3. Drag closely around the words you want.
4. Open Notepad or another text editor and press **Ctrl+V**.
5. Read through the recognized text. Correct any mistakes before reusing it.

**You should see:** editable text instead of a pasted picture. Recognition works best when the letters are sharp and
large enough to read. If the result is empty or inaccurate, try a tighter area or enlarge the source first.

**Try next:** in the snip editor, use **Copy text** or **Ctrl+Shift+C** to recognize the snip you are already viewing.
[Text recognition and snipping](04-snipping.md).

## 3. Record a short how-to

**Goal:** show a few steps that are easier to demonstrate than explain. **Allow:** about two minutes.

1. Open a harmless example, such as a blank Notepad document.
2. Press **Ctrl+Alt+R**, then drag around the area you want to record.
3. Wait for the countdown. A red frame and small recording bar show where and when recording is happening.
4. Demonstrate the steps. For example, type a heading, add a few checklist items, then select a word.
5. Click **Pause** if you need a break; click **Resume** to continue.
6. Press **Ctrl+Alt+R** again, or click **Stop**.
7. In **Save recording**, choose **MP4 video** for a video file or **GIF animation** for a looping animation, then save.

**You should see:** a saved recording of the selected area. Click the saved notification to find it in its folder.
Recordings do not include audio. Cancelling the save dialog deletes the recording.

**Try next:** open `recording.cfg` in the config editor to change frame rate, GIF size, or click and keyboard-shortcut
overlays. Start with short recordings when trying GIF export. [Recording settings](04-snipping.md#recordingcfg).

## 4. Find what changed between two texts

**Goal:** understand a revision without reading both versions from scratch. **Allow:** about three minutes.

You can use your own notes or download `weekend-before.txt` and `weekend-after.txt` from the
[online tutorial samples](https://github.com/rickygzz/CincoVertice.MyTools/tree/HEAD/docs/tutorials).
Open a sample on GitHub and use **Download raw file** to save it.

1. Right-click the MyTools tray icon and choose **Compare files...**.
2. Choose **Left file...** and select the earlier version. Choose **Right file...** for the revised version.
3. With the sample files, look for changed meeting and train times, a different lunch plan, a rain jacket added to the
   packing list, and a different day to share the plan.
4. Press **F7** for the next change and **Shift+F7** to go back.
5. Click either side and make a small edit. After a brief pause, the comparison updates.
6. Press **Ctrl+Z** to undo. If you want to keep your edit, press **Ctrl+S** to save the side with the caret.

**You should see:** added lines in green, removed lines in red, and stronger highlights on changed words.

**Try next:** compare copied text with **Paste left** and **Paste right**. Turn on **Ignore case** or **Ignore
whitespace** when those differences are distracting. [Comparison and safe saving](15-compare-files.md).

## 5. Draw a plan you can share

**Goal:** make a small workflow diagram. **Allow:** about five minutes.

1. Press **Ctrl+Alt+D** to open a new diagram.
2. Choose the **Rounded rectangle** tool on the left. Drag to draw a box.
3. Double-click the box, type `Pick a destination`, and press **Enter** to finish.
4. Add two more boxes: `Plan the journey` and `Book the tickets`.
5. Choose **Connector** or press **C**. Drag from the first box to the second, then from the second to the third.
6. Press **V** for Select. Move the middle box and watch the connectors follow it.
7. Press **Ctrl+S** and save the diagram as an SVG. You can reopen that SVG to keep editing.
8. To share a picture, use **Ctrl+Shift+E** to export PNG, or **Ctrl+E** for PDF.

**You should see:** a connected plan that stays connected as you arrange it.

**Try next:** download `weekend-plan.svg` from the
[online sample folder](https://github.com/rickygzz/CincoVertice.MyTools/tree/HEAD/docs/tutorials/samples)
and open it with **Ctrl+O** in the diagram editor. Explore colors, groups, layers, and SVG image libraries.
[Full diagram guide](05-diagram-editor.md).

## 6. Set up a workspace that fits your task

**Goal:** keep your main document beside reference material. **Allow:** about three minutes.

1. Right-click the tray icon and choose **Edit window zones...**.
2. Under **Layout**, choose **2 columns** to start with equal halves.
3. Drag the shared border to give your main document more room. Choose a **Gap**, such as 8 px.
4. Click **Save** or press **Ctrl+S**, then close the editor.
5. Start dragging your document window by its title bar. While dragging, press **Shift** to show the zones.
6. Drop the window on the wider zone. Drag your reference window into the other zone the same way.

**You should see:** two windows arranged in the sizes you chose. Shift toggles the zones while you drag; you do not
have to keep it held. **Esc** cancels the drag.

**Try next:** split one zone into two, or give a second monitor its own layout using **Edit layout of**.
[Window-zone guide](13-window-zones.md).

## 7. Calculate and convert without breaking your flow

**Goal:** answer everyday number questions from one small window. **Allow:** about two minutes.

1. Press **Ctrl+Alt+C**.
2. Type `(45 + 18.50) * 1.15` and press **Enter**. The result is `73.025`.
3. Replace it with `10 km to mi` and press **Enter**. The result is about `6.21 mi`.
4. Try `100 F to C`. The result is about `37.78 C`.
5. Press **Ctrl+H** to see your successful calculations together.
6. Press **Up** to bring back an earlier expression and change it. Press **Esc** to hide the calculator when finished.

**You should see:** each expression and conversion result, with a history you can refer to during the session.

**Try next:** enter `10:30 Mexico to India` to convert a time between places, or `today + 30 d` to calculate a date.
The suggestions help you choose units and places. [Everything the calculator accepts](03-calculator.md).

## 8. Make a shortcut for a repeated task

**Goal:** paste copied text without bringing its original fonts and colors. **Allow:** about three minutes.

New installations include a script called `PasteAsText`. If your older installation does not have it, follow the
[plain-text paste example](08c-script-examples.md#paste-without-formatting) to create it first.

1. Right-click the tray icon and choose **Edit hotkeys...**.
2. Find the commented example for `PasteAsText`, or add this line. Keep only one active line with this shortcut:

   ```
   PasteAsText = Ctrl+Alt+V
   ```

3. Press **Ctrl+S**. Hotkeys reload after you save. If **Hotkey problems** appears, choose another unused shortcut.
4. Copy some formatted text from a document or web page.
5. Click into the document where you want the plain text, then press **Ctrl+Alt+V**.

**You should see:** the text pasted without its original formatting.

**Try next:** use **Scripts > Edit scripts...** and **New script...** to create a script from the included examples.
The [sample search script](https://github.com/rickygzz/CincoVertice.MyTools/tree/HEAD/docs/tutorials/samples)
asks what to search for and opens the result in your browser. Read a script before running it: scripts can access the
clipboard, type, and launch programs. [Getting started with scripts](08-scripts.md).

## Three more tools to try

| Task | Try this | More help |
|---|---|---|
| Match a color you can see | **Ctrl+Alt+P**, pick the color, then copy its HEX or RGB value | [Color picker](04-snipping.md#picking-a-color-from-the-screen) |
| Find the pointer while presenting | Tap **Left Ctrl twice**, then move the pointer; a click or key dismisses the spotlight | [Mouse spotlight](14-mouse-spotlight.md) |
| Keep the screen on temporarily | **Ctrl+Alt+K** turns Keep Awake on; press it again when finished | [Keep Awake](06-mouse-mover.md#keeping-the-computer-awake) |

[Quick shortcut reference](12-quick-reference.md) · [All tools and settings](README.md)
