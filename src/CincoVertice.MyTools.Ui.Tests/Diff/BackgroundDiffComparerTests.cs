using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diff;

public class BackgroundDiffComparerTests
{
    [Fact]
    public async Task AComparison_ReturnsBeforeDoingWork_AndCanBeCancelledDuringTheDebounce()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<DiffResultModel> pending = new BackgroundDiffComparer().CompareAsync(["a"], ["b"], new(), cancellation.Token);
        Assert.False(pending.IsCompleted);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task ARunningMyersComparison_CanBeCancelled()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        int[] left = [.. Enumerable.Range(0, 100_000)];
        int[] right = [.. left.Reverse()];
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task running = Task.Run(() =>
        {
            started.SetResult();
            MyersDiff.Compare(left, right, cancellation.Token);
        }, cancellation.Token);
        await started.Task;
        await Task.Delay(20, TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            running.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }
}
