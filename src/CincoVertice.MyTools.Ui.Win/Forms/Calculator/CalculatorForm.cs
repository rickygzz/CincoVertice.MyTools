namespace CincoVertice.MyTools.Ui.Win.Forms.Calculator;

public partial class CalculatorForm : Form
{
    private bool _dragging = false;
    private Point _dragCursorPoint;
    private Point _dragFormPoint;

    private const int MOVE_STEP = 10; // Number of pixels to move the window with each arrow key press

    public CalculatorForm()
    {
        InitializeComponent();

        // Center on current screen
        CenterOnCurrentScreen();
    }

    private void CenterOnCurrentScreen()
    {
        // Get the screen where the mouse cursor is
        Screen currentScreen = Screen.FromPoint(Cursor.Position);

        // Calculate center position
        int x = currentScreen.WorkingArea.Left + (currentScreen.WorkingArea.Width - Width) / 2;
        int y = currentScreen.WorkingArea.Top + (currentScreen.WorkingArea.Height - Height) / 2;

        Location = new Point(x, y);
    }

    private void CalculatorForm_Shown(object sender, EventArgs e)
    {
        CalculatorTextBox.Focus();
        CalculatorTextBox.SelectAll();
    }

    private void CalculatorForm_KeyDown(object sender, KeyEventArgs e)
    {
        int moveStep = e.Shift ? MOVE_STEP * 12 : MOVE_STEP; // Move faster with Shift key

        // Move window with Ctrl + Arrow keys
        if (e.Control)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    Location = new Point(Location.X, Location.Y - moveStep);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;

                case Keys.Down:
                    Location = new Point(Location.X, Location.Y + moveStep);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;

                case Keys.Left:
                    Location = new Point(Location.X - moveStep, Location.Y);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;

                case Keys.Right:
                    Location = new Point(Location.X + moveStep, Location.Y);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
            }
        }

        // Close on Escape
        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
        else if (e.KeyCode == Keys.Enter)
        {
            EvaluateExpression();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void EvaluateExpression()
    {
        // TODO: Implement expression parser later
        // For now, just show a placeholder
        string expression = CalculatorTextBox.Text;

        if (string.IsNullOrWhiteSpace(expression))
        {
            return;
        }

        // Placeholder - will implement parser later
        ResultOutputControl.Text = $"Result: (parser to be implemented)";
        ResultOutputControl.Visible = true;
    }

    private void CalculatorForm_Deactivate(object sender, EventArgs e)
    {
        Close();
    }

    private void Calculator_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _dragCursorPoint = Cursor.Position;
            _dragFormPoint = Location;
        }
    }

    private void Calculator_MouseUp(object sender, MouseEventArgs e)
    {
        _dragging = false;
    }

    private void Calculator_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging)
        {
            Point diff = Point.Subtract(Cursor.Position, new Size(_dragCursorPoint));
            Location = Point.Add(_dragFormPoint, new Size(diff));
        }
    }
}
