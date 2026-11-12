using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WIC;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Draws emoji in their colors, as Windows shows them: with Direct2D, since GDI+ and GDI draw them in black and
///     white. Each emoji is drawn once per size, then kept. Use it on the UI thread only.
/// </summary>
public static class ColorEmojiRenderer
{
    public const string FontFamilyName = "Segoe UI Emoji";

    // An emoji's picture is about as tall as its font size, plus room the line keeps above and below it
    private const float FontSizePerPixel = 0.74f;

    private const int MaxSize = 1024;

    // Enough for every emoji of the menus and those drawn, at the sizes used; beyond, they're drawn again
    private const int MaxKept = 400;

    private static readonly Dictionary<(string Emoji, int Size), Bitmap> _kept = [];

    private static ID2D1Factory? _direct2D;
    private static IDWriteFactory? _directWrite;
    private static IWICImagingFactory? _imaging;

    // When Direct2D can't be used, emoji are drawn as GDI+ draws them, in black and white, rather than not at all
    private static bool _isDirect2DUnavailable;

    /// <summary>
    ///     Draws the emoji to fill the square, at the transparency of the alpha. Drawn at the size it takes on the
    ///     screen, so it stays sharp when the graphics are scaled, e.g. a snip zoomed in.
    /// </summary>
    /// <param name="graphics">Where to draw it.</param>
    /// <param name="emoji">The emoji.</param>
    /// <param name="square">Where, in the graphics' units.</param>
    /// <param name="alpha">From 0, invisible, to 255, solid.</param>
    public static void Draw(Graphics graphics, string emoji, RectangleF square, int alpha = 255)
    {
        if (alpha <= 0 || square.Width <= 0 || emoji.Length == 0)
        {
            return;
        }

        float scale;

        using (System.Drawing.Drawing2D.Matrix transform = graphics.Transform)
        {
            scale = Math.Abs(transform.Elements[0]);
        }

        int pixels = Math.Clamp((int)Math.Ceiling(square.Width * Math.Max(scale, 0.01f)), 1, MaxSize);
        Bitmap image = Render(emoji, pixels);

        if (alpha >= 255)
        {
            graphics.DrawImage(image, square);

            return;
        }

        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = alpha / 255f });

        graphics.DrawImage(
            image,
            [square.Location, new PointF(square.Right, square.Top), new PointF(square.Left, square.Bottom)],
            new RectangleF(0, 0, image.Width, image.Height),
            GraphicsUnit.Pixel,
            attributes);
    }

    /// <returns>
    ///     The emoji on a transparent square of the size, which is kept for the next time: the caller doesn't dispose
    ///     it.
    /// </returns>
    public static Bitmap Render(string emoji, int size)
    {
        size = Math.Clamp(size, 1, MaxSize);

        if (_kept.TryGetValue((emoji, size), out Bitmap? kept))
        {
            return kept;
        }

        if (_kept.Count >= MaxKept)
        {
            foreach (Bitmap old in _kept.Values)
            {
                old.Dispose();
            }

            _kept.Clear();
        }

        Bitmap image = _isDirect2DUnavailable ? RenderInBlackAndWhite(emoji, size) : RenderInColor(emoji, size);
        _kept[(emoji, size)] = image;

        return image;
    }

    private static Bitmap RenderInColor(string emoji, int size)
    {
        try
        {
            _direct2D ??= D2D1.D2D1CreateFactory<ID2D1Factory>();
            _directWrite ??= DWrite.DWriteCreateFactory<IDWriteFactory>();
            _imaging ??= new IWICImagingFactory();

            using IWICBitmap target = _imaging.CreateBitmap(
                (uint)size,
                (uint)size,
                Vortice.WIC.PixelFormat.Format32bppPBGRA,
                BitmapCreateCacheOption.CacheOnLoad);
            var properties = new RenderTargetProperties(new Vortice.DCommon.PixelFormat(
                Vortice.DXGI.Format.B8G8R8A8_UNorm,
                Vortice.DCommon.AlphaMode.Premultiplied));

            using (ID2D1RenderTarget canvas = _direct2D.CreateWicBitmapRenderTarget(target, properties))
            using (IDWriteTextFormat format = _directWrite.CreateTextFormat(
                FontFamilyName,
                FontWeight.Normal,
                Vortice.DirectWrite.FontStyle.Normal,
                FontStretch.Normal,
                size * FontSizePerPixel))
            using (ID2D1SolidColorBrush ink = canvas.CreateSolidColorBrush(new Color4(0, 0, 0, 1)))
            {
                format.TextAlignment = TextAlignment.Center;
                format.ParagraphAlignment = ParagraphAlignment.Center;
                format.WordWrapping = WordWrapping.NoWrap;

                canvas.BeginDraw();
                canvas.Clear(new Color4(0, 0, 0, 0));
                canvas.DrawText(emoji, format, new Rect(0, 0, size, size), ink, DrawTextOptions.EnableColorFont);
                canvas.EndDraw();
            }

            return ToBitmap(target, size);
        }
        catch (Exception ex) when (ex is SharpGenException or COMException or DllNotFoundException)
        {
            _isDirect2DUnavailable = true;

            return RenderInBlackAndWhite(emoji, size);
        }
    }

    // Premultiplied both: Direct2D's pixels go into the bitmap as they are
    private static Bitmap ToBitmap(IWICBitmap source, int size)
    {
        int stride = size * 4;
        byte[] pixels = new byte[stride * size];
        source.CopyPixels((uint)stride, pixels);

        var image = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        BitmapData data = image.LockBits(
            new Rectangle(0, 0, size, size),
            ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

        try
        {
            for (int y = 0; y < size; y++)
            {
                Marshal.Copy(pixels, y * stride, data.Scan0 + (y * data.Stride), stride);
            }
        }
        finally
        {
            image.UnlockBits(data);
        }

        return image;
    }

    private static Bitmap RenderInBlackAndWhite(string emoji, int size)
    {
        var image = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(image);
        using var font = new Font(FontFamilyName, size * FontSizePerPixel, GraphicsUnit.Pixel);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        graphics.DrawString(emoji, font, Brushes.Black, new RectangleF(0, 0, size, size), format);

        return image;
    }
}
