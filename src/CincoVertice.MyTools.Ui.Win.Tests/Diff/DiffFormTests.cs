using CincoVertice.MyTools.Ui.Diff.Presenters;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Win.Diff;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diff;

public sealed class DiffFormTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "MyToolsDiffTests", Guid.NewGuid().ToString("N"));

    public DiffFormTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task TwoFiles_ShowSideBySide_WithTheirChanges()
    {
        // Arrange
        string left = Write("old.cs", "int a = 1;\r\nint b = 2;\r\n");
        string right = Write("new.cs", "int a = 1;\nint b = 3;\nint c = 4;\n");
        using var form = new DiffForm();
        var presenter = new DiffPresenter(form, new DiskDiffFileReader(), new DiskDiffFileWriter());

        // Act
        presenter.Open(left, right);

        // Keep the actual UI message loop moving while the worker returns its result to this thread.
        var wait = System.Diagnostics.Stopwatch.StartNew();
        while (!presenter.Comparison.IsCompleted && wait.Elapsed < TimeSpan.FromSeconds(10))
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            System.Windows.Forms.Application.DoEvents();
            Thread.Sleep(1);
        }
        Assert.True(presenter.Comparison.IsCompleted);
        await presenter.Comparison;

        // Assert
        Assert.Equal("old.cs vs new.cs - Compare files", form.Text);
        Assert.Equal(4, presenter.Result!.Rows.Count);
        Assert.Single(presenter.Result.Hunks);
    }

    private string Write(string name, string text)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllText(path, text);

        return path;
    }
}
