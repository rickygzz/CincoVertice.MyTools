using CincoVertice.Common.Application.Scripting.Models;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.Scripting;

/// <summary>
///     A small dialog that asks for one or more lines of text, for ask() and form() in scripts. Built in code: it has
///     no logic of its own, and its buttons close it through their DialogResult. Shown on top, because a hotkey
///     usually runs the script while another application is active.
/// </summary>
public static class InputDialog
{
    private const int TextWidth = 420;

    /// <returns>The text entered, or null when the user cancels.</returns>
    public static string? Show(string title, string prompt, string defaultText) =>
        Show(title, null, [new FormFieldModel(prompt, defaultText)])?[0];

    /// <param name="title">The window title.</param>
    /// <param name="heading">Bold text above the boxes, or null for none.</param>
    /// <param name="fields">One label and box per field: a text box, or a fixed list for a field with choices.</param>
    /// <returns>The text entered or the item picked, one per field, or null when the user cancels.</returns>
    public static IReadOnlyList<string>? Show(string title, string? heading, IReadOnlyList<FormFieldModel> fields)
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.Manual,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            TopMost = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12),
        };

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };

        // Controls don't dispose a font they are given
        using var headingFont = new Font(form.Font, FontStyle.Bold);

        if (!string.IsNullOrEmpty(heading))
        {
            var headingLabel = CreateLabel(heading, new Padding(0, 0, 0, 10));
            headingLabel.Font = headingFont;
            layout.Controls.Add(headingLabel);
        }

        List<Control> boxes = [];

        for (int i = 0; i < fields.Count; i++)
        {
            Control box = CreateBox(fields[i]);
            box.Width = TextWidth;
            box.Margin = new Padding(0, 0, 0, i < fields.Count - 1 ? 10 : 0);

            layout.Controls.Add(CreateLabel(fields[i].Prompt, new Padding(0, 0, 0, 4)));
            layout.Controls.Add(box);
            boxes.Add(box);
        }

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons);

        form.Controls.Add(layout);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.ActiveControl = boxes[0];
        form.ActivateWhenShown();

        // On the screen the user is looking at, where the mouse is; CenterScreen can pick another monitor. On Load,
        // AutoSize has given the form its final size
        form.Load += (_, _) => form.CenterOnCursorScreen();

        if (form.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        // A text box's text, or the item picked in a list
        return [.. boxes.Select(box => box.Text)];
    }

    /// <summary>
    ///     A text box with the default text, or a list that only allows its items, with the default item selected
    ///     (the first when the default is not one of them).
    /// </summary>
    private static Control CreateBox(FormFieldModel field)
    {
        if (field.Choices is null)
        {
            return new TextBox { Text = field.DefaultText };
        }

        var list = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        list.Items.AddRange([.. field.Choices]);
        list.SelectedIndex = Math.Max(0, IndexOf(field.Choices, field.DefaultText));

        return list;
    }

    private static int IndexOf(IReadOnlyList<string> items, string item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item)
            {
                return i;
            }
        }

        return -1;
    }

    private static Label CreateLabel(string text, Padding margin) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(TextWidth, 0),
        Margin = margin,
    };
}
