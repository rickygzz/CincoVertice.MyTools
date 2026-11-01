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
        Show(title, null, [prompt], [defaultText])?[0];

    /// <param name="title">The window title.</param>
    /// <param name="heading">Bold text above the boxes, or null for none.</param>
    /// <param name="prompts">One label and box per prompt.</param>
    /// <param name="defaultTexts">The starting text of each box, one per prompt.</param>
    /// <returns>The texts entered, one per prompt, or null when the user cancels.</returns>
    public static IReadOnlyList<string>? Show(
        string title, string? heading, IReadOnlyList<string> prompts, IReadOnlyList<string> defaultTexts)
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
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

        List<TextBox> textBoxes = [];

        for (int i = 0; i < prompts.Count; i++)
        {
            var textBox = new TextBox
            {
                Text = defaultTexts[i],
                Width = TextWidth,
                Margin = new Padding(0, 0, 0, i < prompts.Count - 1 ? 10 : 0),
            };

            layout.Controls.Add(CreateLabel(prompts[i], new Padding(0, 0, 0, 4)));
            layout.Controls.Add(textBox);
            textBoxes.Add(textBox);
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
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons);

        form.Controls.Add(layout);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.ActiveControl = textBoxes[0];
        form.ActivateWhenShown();

        if (form.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        return [.. textBoxes.Select(textBox => textBox.Text)];
    }

    private static Label CreateLabel(string text, Padding margin) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(TextWidth, 0),
        Margin = margin,
    };
}
