# Diagram Editor

Press **Ctrl+Alt+D** or choose **Diagram editor** from the tray menu. Each time, a **new** window opens with an empty diagram. Windows are independent.

A `hotkeys.cfg` made by a version without the diagram editor has no line for it, since the file is created only once. Add `DiagramEditor = Ctrl+Alt+D` to it to get the hotkey (see [How to change a hotkey](02-hotkeys.md#how-to-change-a-hotkey)).

The window title is `<name> - Diagram editor`, with `*` after the name when there are unsaved changes. A new diagram is called "Untitled". The default size is 1280 by 760 pixels, and the editor remembers its last size.

New text uses [Roboto](https://fonts.google.com/specimen/Roboto), which comes with the app under the [SIL Open Font License](https://openfontlicense.org). The first time you open it, the editor installs Roboto for your Windows user if it is missing, so that other programs such as a browser show saved diagrams in the same font. This needs no administrator rights. On a computer without a diagram's font, its SVG shows in another sans serif font.

## Tools

| Tool | Key | Tooltip |
|---|---|---|
| Select | V | "Select, move and scale (V). Space or the middle button drags the view" |
| Connector | C | "Connector (C): drag from one shape to another. Double-click it for a label" |
| Rectangle | R | "Rectangle, or process (R)" |
| Rounded rectangle | U | "Rounded rectangle, or start and end (U)" |
| Ellipse | O | "Ellipse (O)" |
| Diamond | D | "Diamond, or decision (D)" |
| Parallelogram | P | "Parallelogram, or input and output (P)" |
| Hexagon | H | "Hexagon, or preparation (H)" |
| Cylinder | Y | "Cylinder, or database (Y)" |
| Document | M | "Document (M)" |
| Text | T | "Text (T): click where it goes, Enter to end, Shift+Enter for a new line" |

## Working with shapes

- **Add** a shape: pick it on the left, or press its key, then drag on the canvas. A click instead adds it at the usual size, 120 by 60 pixels.
- **Select:** click a shape, Shift+click (or Ctrl+click) to add or remove one, or drag on empty canvas to draw a box around several. Ctrl+A selects everything.
- **Move** the selection by dragging it, or with the arrow keys.
- **Resize** a shape with its 8 handles; with several selected, the handles go around all of them. The minimum size is 10 pixels.
- **Connectors** join two shapes and follow them when the shapes move. Deleting a shape also deletes its connectors.
  - *Connect:* hover a shape, and circles appear around it; drag from one to another shape. Drop on one of that shape's circles to choose its side too. Or pick the Connector tool (C) and drag from one shape to the other, and the sides are chosen for you. **V** or **Esc** goes back to selecting.
  - *Attach an end somewhere else:* click the connector, then drag one of the dots at its ends to another shape, or to a circle on it to choose the side. Dropped on nothing, the end stays where it was. Dropped on the middle of its own shape, the side is chosen for you again.
  - *Change the path:* click an elbow connector, then drag any piece of its line sideways. The line stays level and upright, with rounded corners. **Reset line** on the toolbar lets it work out its own path again, and so does choosing **Elbow** or **Straight**.
- **Text:** double-click a shape to edit its text, double-click a connector to edit its label, or double-click empty canvas for free text. The Text tool also adds free text where you click, and the Properties panel has a Text box. Enter ends editing; Shift+Enter inserts a new line.
- **Group:** select two or more shapes and press Ctrl+G, or choose **Edit > Group**, or right-click one of them and choose **Group**. Groups are one level only; there is no nesting. Selecting one shape of a group selects the whole group. Ctrl+Shift+G, **Edit > Ungroup** or **Ungroup** in the right-click menu ungroups. In the right-click menu, Group is grayed out unless two or more shapes are selected that are not already one group, and Ungroup unless a selected shape is in a group.
- **Copy and paste** use the Windows clipboard as text: the selected shapes and the connectors between them. Pasting adds the shapes 20 pixels further each time. Pasting other text does nothing. If you paste into another program, you get that text, not a picture. Because it is the clipboard, you can copy from one editor window and paste into another. **Duplicate** (Ctrl+D) does the same without using the clipboard.
- **Snapping.** While you drag with the mouse, shapes snap in two ways.
  - *To other shapes:* the left edge, middle or right edge of what you move lines up with the left edge, middle or right edge of any other shape (top, middle and bottom likewise) when it is close. A thin guide line is drawn along each alignment.
  - *To a grid:* when no other shape is close in a direction, the position rounds to the nearest 10 pixels: a moved shape's left and top edges land on a multiple of 10 (when resizing, the edge you drag does). The faint grid lines on the canvas are every 50 pixels, not every 10.
  - It applies when moving selected shapes, resizing with a handle, drawing a new shape, and dragging a connector's bend segment. Dragging a connector from one shape to another does not snap to lines or the grid; the connector attaches to a shape's side instead.
  - Arrow-key nudging and the Properties panel never snap.
  - To drag without snapping, hold **Alt**. When resizing, hold **Alt** or **Shift** (Shift also keeps the proportions). There is no other setting to switch snapping off.

## Style and properties

The toolbar offers the **File** menu (see [Saving and opening](#saving-and-opening)), the **Edit** menu, Fill (with "No fill"), Line, line width, Dashed, Text color, text size, Edge color (with "No edge") and width, routing ("Elbow" or "Straight", tooltip "How connectors go between shapes"), arrows ("Arrow at end", "Arrow at start", "Arrows at both", "No arrows"), "Reset line" (tooltip "Let the selected connectors work out their own path again"), Front, Back and Properties.

The **Edit** menu has **Undo** (Ctrl+Z), **Redo** (Ctrl+Y), **Copy style** (Ctrl+Shift+C), **Paste style** (Ctrl+Shift+V), **Group** (Ctrl+G) and **Ungroup** (Ctrl+Shift+G). Undo and Redo are grayed out when there is nothing to undo or redo.

A toolbar change goes to everything selected, and to what you draw next.

**Text edge.** An edge is a line drawn around the letters of a text, to make it stand out on any background. It is off at first. Turn it off with **No edge** in the Edge color menu, or with the width "No edge"; its color then shows as none, and is kept for when you turn it on again. Picking an edge color while it is off turns it on at 2 px.

**Copy a style.** Select a shape or connector and choose **Edit > Copy style** (or press **Ctrl+Shift+C**). Then select one or more others and choose **Edit > Paste style** (**Ctrl+Shift+V**). Both are also on the menu you get by right-clicking a shape or connector: a right-click on something not selected selects just it first, and one on the selection keeps it all. Paste style is grayed out there until you copy a style. Pasted objects get the copied fill, border, border width, dashes, text color, font, text size and text edge, as one step to undo. What you draw next gets that style too. A style copied from a connector leaves a shape's fill as it is, since a connector has none. Routing and arrows are not copied. The copied style stays until you copy another, in that window only.

The Properties panel (show or hide with **F4**) has these fields: Text, X, Y, Width, Height, Corner radius, Fill, Border, Border width, Dashed, Text color, Font, Text size, Text edge, Edge width, Route and Arrows.

**Lock a shape's place or size.** Beside X, Y, Width and Height is a lock button, shown as an open padlock. Click it to lock that value for the selected shapes: the button stays pressed, shows a closed padlock, and the field is grayed out. Click it again to unlock it. Each one is separate, so you can, say, lock only the width.

- A locked value stays as it is when you drag the shape, move it with the arrow keys, scale it by its handles or type in the fields. The rest still changes. For example, with X locked a drag only moves the shape up and down, and with the width locked the side handles do nothing.
- With several shapes selected, a lock button is pressed only when all of them have that lock, and clicking it locks or unlocks it for all of them. Shapes that are moved or scaled together keep what each one has locked.
- Locks are saved with the diagram, and locking or unlocking is a step you can undo. Copies and pastes keep them, but a pasted copy still goes where it's pasted.

## Keys

| Key | Effect |
|---|---|
| Ctrl+N / Ctrl+O | New / open |
| Ctrl+S / Ctrl+Shift+S | Save / save as |
| Ctrl+E | Export as PDF |
| Ctrl+Z | Undo |
| Ctrl+Y or Ctrl+Shift+Z | Redo |
| Ctrl+X, Ctrl+C, Ctrl+V | Cut, copy, paste |
| Ctrl+D | Duplicate |
| Ctrl+Shift+C / Ctrl+Shift+V | Copy style / paste style |
| Ctrl+A | Select all |
| Ctrl+G / Ctrl+Shift+G | Group / ungroup |
| Ctrl+] / Ctrl+[ | Bring to front / send to back |
| Ctrl++ / Ctrl+- / Ctrl+0 | Zoom in / zoom out / reset zoom |
| Delete or Backspace | Delete the selection |
| Arrow keys | Move the selection 10 px (with Shift, 1 px) |
| F4 | Show or hide the Properties panel |
| Space (held) or middle mouse button | Drag the view |
| Wheel / Shift+wheel | Move the view up and down / sideways |
| Ctrl+wheel | Zoom (25% to 400%) |
| Esc | Cancels a text being typed; else returns focus from the Properties panel to the canvas (applying what you typed); else ends a drag in progress, or switches to Select, or clears the selection |

The zoom label shows the percentage; its tooltip reads "Ctrl+wheel to zoom. Click, or Ctrl+0, for 100%".

## Saving and opening

- The toolbar's **File** menu has **New** (Ctrl+N), **Open...** (Ctrl+O), **Save** (Ctrl+S), **Save as...** (Ctrl+Shift+S), **Export as PDF...** (Ctrl+E) and **Exit**. Exit closes this window, as its X button does, and asks about unsaved changes; the rest of My Tools keeps running.
- The dialogs are "Save diagram" and "Open diagram", with the filter "SVG image (*.svg)". The suggested name is `Diagram.svg`.
- A saved diagram is a normal SVG image that any browser can show. It carries the diagram's data inside, so only My Tools can edit it again. A failed save leaves the old file as it was.
- **Only diagrams made by this editor can be opened.** Other files give `This isn't an SVG file.` or `This SVG file wasn't made by the diagram editor, so it can't be edited here.`
- Messages from the editor, in boxes titled "Diagram editor":
  - "Save the changes to <name>?" (Yes, No or Cancel), shown when closing or leaving unsaved work.
  - "<name> was changed elsewhere since it was opened here. Replace it with this diagram?" (Yes or No).
  - `Could not open <file name>: <message>`, `Could not save <file name>: <message>` and `Could not export <file name>: <message>`.
- There is no export to PNG and no printing from the editor; print the PDF instead.

### Export as PDF

**Export as PDF...** (Ctrl+E) writes a copy of the diagram as a PDF file, to share or print. The dialog is "Export as PDF", with the filter "PDF document (*.pdf)"; the suggested name is the diagram's, such as `flow.pdf`, or `Diagram.pdf` for a diagram never saved.

- The page is as large as the SVG image would be, around everything with a margin, at the size the diagram has at 100% zoom.
- Shapes, lines and arrows are drawn as lines and curves, so they stay sharp when you zoom in or print.
- Text keeps the font it has in the editor, even on a computer without that font, because each letter is drawn as its outline. So the text can't be selected, copied or searched in the PDF.
- The export doesn't save the diagram: the title keeps its `*` until you save, and the editor can't open the PDF. A failed export leaves the old file as it was.

The editor remembers your last style, routing, arrow heads, whether the Properties panel is shown, and the window size in `config\diagrameditor.json`.

---

← Previous: [Snipping and the Snip Editor](04-snipping.md) | [Contents](README.md) | Next: [Mouse Mover (Activity)](06-mouse-mover.md) →
