using CincoVertice.Common.Application.Calculator;
using CincoVertice.MyTools.Ui.Calculator.Presenters;
using CincoVertice.MyTools.Ui.Win.Calculator;
using System.Windows.Forms;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Calculator;

public class CalculatorFormTests
{
    [Fact]
    public void Suggestions_ShowUnderTheExpression_DownAndEnterWriteOne_AndEscHidesThemNotTheCalculator()
    {
        // Arrange
        using var form = new KeyedCalculatorForm();
        _ = new CalculatorPresenter(form, new ExpressionCalculator());
        form.Show();
        var input = (TextBox)form.Controls.Find("CalculatorTextBox", true).Single();
        var list = (ListBox)form.Controls.Find("SuggestionsListBox", true).Single();
        int height = form.ClientSize.Height;

        // Act / Assert: the window grows to fit them, nothing chosen until Down
        input.AppendText("10 ");
        Assert.True(list.Visible);
        Assert.Equal(-1, list.SelectedIndex);
        Assert.True(form.ClientSize.Height > height);
        Assert.True(list.Bottom <= form.ClientSize.Height);

        Assert.True(form.Press(Keys.Down));
        Assert.Equal(0, list.SelectedIndex);

        Assert.True(form.Press(Keys.Enter));
        Assert.Equal("10 nm to ", input.Text);
        Assert.Equal(input.TextLength, input.SelectionStart);
        Assert.True(list.Visible);

        Assert.True(form.Press(Keys.Escape));
        Assert.False(list.Visible);
        Assert.True(form.Visible);
        Assert.Equal(height, form.ClientSize.Height);
    }

    [Fact]
    public void Suggestions_TabWritesTheFirst_WhenNoneIsChosen()
    {
        // Arrange
        using var form = new KeyedCalculatorForm();
        _ = new CalculatorPresenter(form, new ExpressionCalculator());
        form.Show();
        var input = (TextBox)form.Controls.Find("CalculatorTextBox", true).Single();
        input.AppendText("10:30 mex");

        // Act
        bool handled = form.Press(Keys.Tab);

        // Assert
        Assert.True(handled);
        Assert.Equal("10:30 Mexico City to ", input.Text);
    }

    /// <summary>Presses keys as the window gets them first, before the text box.</summary>
    private sealed class KeyedCalculatorForm : CalculatorForm
    {
        public bool Press(Keys keys)
        {
            Message message = default;

            return ProcessCmdKey(ref message, keys);
        }
    }
}
