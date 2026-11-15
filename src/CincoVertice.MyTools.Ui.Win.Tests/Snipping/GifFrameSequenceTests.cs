using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

public class GifFrameSequenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FramesStayBounded_AndEveryReturnedStreamIsDisposed_EvenOnEncodingFailure(bool fails)
    {
        List<Frame> created = [];
        List<TimeSpan> written = [];
        List<int> sizes = [];
        int count = GifFrameSequence.BatchSize * 3 + 1;
        var timeline = Enumerable.Range(0, count).Select(index => (TimeSpan.FromMilliseconds(index * 100), (ushort)10));

        Task writing = GifFrameSequence.WriteAsync<Frame>(timeline, times =>
        {
            Assert.All(created, frame => Assert.True(frame.IsDisposed));
            sizes.Add(times.Length);
            Frame[] batch = times.Select(time => new Frame(time)).ToArray();
            created.AddRange(batch);
            return Task.FromResult<IReadOnlyList<Frame>>(batch);
        }, async (frame, delay) =>
        {
            Assert.False(frame.IsDisposed);
            Assert.Equal(10, delay);
            await Task.Yield();
            if (fails && written.Count == GifFrameSequence.BatchSize + 2)
            {
                throw new IOException("Encoding failed");
            }

            written.Add(frame.Time);
        });

        if (fails)
        {
            await Assert.ThrowsAsync<IOException>(() => writing);
        }
        else
        {
            await writing;
            Assert.Equal(timeline.Select(frame => frame.Item1), written);
            Assert.Equal(1, sizes[^1]);
        }

        Assert.All(sizes, size => Assert.InRange(size, 1, GifFrameSequence.BatchSize));
        Assert.All(created, frame => Assert.True(frame.IsDisposed));
    }

    [Fact]
    public async Task AnIncompleteBatch_IsDisposed_AndReportedInsteadOfDroppingVideoFrames()
    {
        var frame = new Frame(TimeSpan.Zero);
        var timeline = new[] { (TimeSpan.Zero, (ushort)10), (TimeSpan.FromMilliseconds(100), (ushort)10) };
        bool wasWritten = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => GifFrameSequence.WriteAsync<Frame>(timeline,
            _ => Task.FromResult<IReadOnlyList<Frame>>([frame]),
            (_, _) =>
            {
                wasWritten = true;
                return Task.CompletedTask;
            }));

        Assert.False(wasWritten);
        Assert.True(frame.IsDisposed);
    }

    private sealed class Frame(TimeSpan time) : IDisposable
    {
        public TimeSpan Time { get; } = time;
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
