namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>Reads a bounded batch of thumbnails at a time, disposing the entire batch even if encoding fails.</summary>
internal static class GifFrameSequence
{
    internal const int BatchSize = 8;

    internal static async Task WriteAsync<T>(
        IEnumerable<(TimeSpan Time, ushort Delay)> timeline,
        Func<TimeSpan[], Task<IReadOnlyList<T>>> readFrames,
        Func<T, ushort, Task> writeFrame) where T : IDisposable
    {
        using var times = timeline.GetEnumerator();

        while (true)
        {
            List<(TimeSpan Time, ushort Delay)> batch = new(BatchSize);

            while (batch.Count < BatchSize && times.MoveNext())
            {
                batch.Add(times.Current);
            }

            if (batch.Count == 0)
            {
                return;
            }

            IReadOnlyList<T> frames = await readFrames([.. batch.Select(frame => frame.Time)]);

            try
            {
                if (frames.Count != batch.Count)
                {
                    throw new InvalidOperationException("Windows returned an incomplete batch of video frames.");
                }

                for (int i = 0; i < frames.Count; i++)
                {
                    await writeFrame(frames[i], batch[i].Delay);
                }
            }
            finally
            {
                foreach (T frame in frames)
                {
                    frame.Dispose();
                }
            }
        }
    }
}
