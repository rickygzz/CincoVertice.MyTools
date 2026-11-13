namespace CincoVertice.MyTools.Ui.Win.Help.Services;

/// <summary>A toolbar and its menus in dark colors, for Windows' dark mode: WinForms draws them light.</summary>
public sealed class HelpDarkColorTable : ProfessionalColorTable
{
    public static Color Background { get; } = Color.FromArgb(43, 43, 43);

    public static Color Text { get; } = Color.FromArgb(222, 222, 222);

    private static readonly Color _hover = Color.FromArgb(62, 62, 64);
    private static readonly Color _pressed = Color.FromArgb(0, 84, 153);
    private static readonly Color _line = Color.FromArgb(70, 70, 70);

    public override Color ToolStripGradientBegin => Background;

    public override Color ToolStripGradientMiddle => Background;

    public override Color ToolStripGradientEnd => Background;

    public override Color ToolStripBorder => Background;

    public override Color ToolStripDropDownBackground => Background;

    public override Color ImageMarginGradientBegin => Background;

    public override Color ImageMarginGradientMiddle => Background;

    public override Color ImageMarginGradientEnd => Background;

    public override Color MenuBorder => _line;

    public override Color MenuItemBorder => _hover;

    public override Color MenuItemSelected => _hover;

    public override Color MenuItemSelectedGradientBegin => _hover;

    public override Color MenuItemSelectedGradientEnd => _hover;

    public override Color MenuItemPressedGradientBegin => _pressed;

    public override Color MenuItemPressedGradientEnd => _pressed;

    public override Color ButtonSelectedHighlight => _hover;

    public override Color ButtonSelectedBorder => _hover;

    public override Color ButtonSelectedGradientBegin => _hover;

    public override Color ButtonSelectedGradientMiddle => _hover;

    public override Color ButtonSelectedGradientEnd => _hover;

    public override Color ButtonPressedGradientBegin => _pressed;

    public override Color ButtonPressedGradientMiddle => _pressed;

    public override Color ButtonPressedGradientEnd => _pressed;

    public override Color SeparatorDark => _line;

    public override Color SeparatorLight => Background;
}
