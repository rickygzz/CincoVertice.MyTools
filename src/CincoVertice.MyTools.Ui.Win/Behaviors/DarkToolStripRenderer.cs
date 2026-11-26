namespace CincoVertice.MyTools.Ui.Win.Behaviors;

/// <summary>
///     Draws a toolbar or a menu dark (<see cref="DarkColorTable"/>), with its arrows in the text's color: the
///     professional renderer draws a drop-down button's arrow black, which can't be seen on dark.
/// </summary>
public sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer()
        : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item?.Enabled == false ? Color.Gray : DarkColorTable.Text;
        base.OnRenderArrow(e);
    }
}
