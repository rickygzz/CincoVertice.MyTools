using System.Drawing.Imaging;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

public static class PngEncoder
{
    public static byte[] Encode(Image image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Png);

        return stream.ToArray();
    }
}
