using CincoVertice.Common.Application.Calculator;
using CincoVertice.Common.Ui.Rtf.Constants;

namespace CincoVertice.MyTools.Ui.Win.Forms.Calculator;

public partial class CalculatorForm : Form
{
    private readonly ExpressionCalculator _calculator;

    private bool _dragging = false;
    private Point _dragCursorPoint;
    private Point _dragFormPoint;

    private const int MOVE_STEP = 10; // Number of pixels to move the window with each arrow key press

    public CalculatorForm(ExpressionCalculator calculator)
    {
        InitializeComponent();

        _calculator = calculator;
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

    // The form is a singleton reused on every hotkey press, so this runs on each show (Shown only fires once).
    private void CalculatorForm_VisibleChanged(object? sender, EventArgs e)
    {
        if (!Visible)
        {
            return;
        }

        // Center on current screen
        CenterOnCurrentScreen();

        // Not Focus(): the form is not active yet, and on activation WinForms restores ActiveControl,
        // which would otherwise be the control that had focus when the form was last hidden.
        ActiveControl = CalculatorTextBox;
        CalculatorTextBox.SelectAll();
    }

    // Safety net for Alt+F4 (still works with ControlBox = false) or any future Close() call.
    private void CalculatorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Hide instead of closing, so the singleton is not disposed. 
        // Windows shutdown, etc. still close normally.
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
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

        if (e.KeyCode == Keys.Escape)
        {
            Hide();
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
        string expression = CalculatorTextBox.Text;

        if (string.IsNullOrWhiteSpace(expression))
        {
            return;
        }

        try
        {
            decimal result = _calculator.Evaluate(expression);

            var output = ExpressionCalculator.Format(result, groupThousands: true);
            ResultOutputControl.AddText(output, RtfConstants.Normal, true);
        }
        catch (ExpressionException ex)
        {
            ResultOutputControl.Text = ex.Message;

            // Put the caret on the error, so it can be fixed right away
            CalculatorTextBox.Select(Math.Min(ex.Position, expression.Length), ex.Position < expression.Length ? 1 : 0);
        }

        ResultOutputControl.Visible = true;
    }

    private void CalculatorForm_Deactivate(object sender, EventArgs e)
    {
        Hide();
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
