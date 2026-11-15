using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

public class GifTimingTests
{
    [Fact]
    public void EverySupportedFrameRate_KeepsAMinuteOfPlayback_WithoutAccumulatingRoundingErrors()
    {
        for (int fps = 2; fps <= 20; fps++)
        {
            var frames = RecordingFileStore.GifFrames(TimeSpan.FromMinutes(1), fps).ToList();

            Assert.Equal(60 * fps, frames.Count);
            Assert.Equal(6000, frames.Sum(frame => frame.Delay));
            Assert.Equal(TimeSpan.Zero, frames[0].Time);
            Assert.All(frames, frame => Assert.InRange(frame.Delay, (ushort)1, (ushort)50));

            int elapsed = 0;
            foreach (var frame in frames)
            {
                Assert.InRange(Math.Abs(elapsed / 100d - frame.Time.TotalSeconds), 0, 0.005001);
                elapsed += frame.Delay;
            }
        }
    }

    [Theory]
    [InlineData(0, 15)]
    [InlineData(1, 15)]
    [InlineData(1001, 15)]
    [InlineData(2030, 15)]
    [InlineData(2030, 19)]
    public void APartialLastFrame_PreservesDuration_ToWithinOneHundredth(int milliseconds, int fps)
    {
        TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);
        var frames = RecordingFileStore.GifFrames(duration, fps).ToList();

        Assert.NotEmpty(frames);
        Assert.InRange(Math.Abs(frames.Sum(frame => frame.Delay) / 100d - duration.TotalSeconds), 0, 0.010001);
        Assert.All(frames, frame => Assert.True(frame.Time < duration || frame.Time == TimeSpan.Zero));
    }

    [Fact]
    public void LongRecordings_GenerateFrameTimesAsNeeded()
    {
        var first = RecordingFileStore.GifFrames(TimeSpan.FromHours(4), 20).Take(3).ToList();

        Assert.Equal(new[] { TimeSpan.Zero, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100) },
            first.Select(frame => frame.Time));
    }
}
