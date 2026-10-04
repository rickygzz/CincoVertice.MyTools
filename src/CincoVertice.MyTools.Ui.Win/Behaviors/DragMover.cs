namespace CincoVertice.MyTools.Ui.Win.Behaviors;

/// <summary>
///     Moves a form by dragging it with the left mouse button. Child controls take the mouse events, so the ones
///     that should also drag the form are passed as handles.
/// </summary>
public sealed class DragMover
{
    private readonly Form _form;

    private bool _dragging = false;
    private Point _dragCursorPoint;
    private Point _dragFormPoint;

    private DragMover(Form form)
    {
        _form = form;
    }

    /// <summary>
    ///     Lets the form be dragged by itself and by the given controls. The handlers keep the mover alive for as
    ///     long as the controls.
    /// </summary>
    public static void Attach(Form form, params Control[] handles)
    {
        var mover = new DragMover(form);

        foreach (var control in handles.Prepend(form))
        {
            control.MouseDown += mover.OnMouseDown;
            control.MouseMove += mover.OnMouseMove;
            control.MouseUp += mover.OnMouseUp;
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _dragCursorPoint = Cursor.Position;
            _dragFormPoint = _form.Location;
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragging)
        {
            Point diff = Point.Subtract(Cursor.Position, new Size(_dragCursorPoint));
            _form.Location = Point.Add(_dragFormPoint, new Size(diff));
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        _dragging = false;
    }
}
