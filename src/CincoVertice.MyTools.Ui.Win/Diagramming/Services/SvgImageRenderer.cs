using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using SharpGen.Runtime;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using Vortice.WIC;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     Draws the diagram's SVG images with Windows' own SVG drawing (Direct2D), at the size they take on the screen,
///     so they stay sharp at any zoom. Each is drawn once per size, then kept. Use it on the UI thread only.
/// </summary>
public static class SvgImageRenderer
{
    private const int MaxSide = 2048;

    // Enough for the images of a large diagram at one zoom; beyond, they're drawn again
    private const int MaxKept = 300;

    private static readonly System.Drawing.Color _placeholderFill = System.Drawing.Color.FromArgb(240, 240, 240);
    private static readonly System.Drawing.Color _placeholderLine = System.Drawing.Color.FromArgb(170, 170, 170);

    // Null for an image that can't be drawn, so it isn't tried again on every paint
    private static readonly Dictionary<(string Markup, int Width, int Height), Bitmap?> _kept = [];

    private static ID2D1Factory1? _direct2D;
    private static IWICImagingFactory? _imaging;
    private static bool _isDirect2DUnavailable;

    /// <summary>Draws the image in the bounds, fitted as its markup says; a crossed box if it can't be drawn.</summary>
    /// <param name="graphics">Where to draw it, with any transform, such as the canvas' zoom.</param>
    /// <param name="markup">The image, as <c>DiagramSvgImage.Prepare</c> gives it.</param>
    /// <param name="bounds">Where, in the graphics' units.</param>
    public static void Draw(Graphics graphics, string markup, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        float scale;

        using (Matrix transform = graphics.Transform)
        {
            scale = Math.Max(Math.Abs(transform.Elements[0]), 0.01f);
        }

        (int width, int height) = PixelSize(bounds.Width * scale, bounds.Height * scale);

        if (Render(markup, width, height) is Bitmap image)
        {
            graphics.DrawImage(image, bounds);
        }
        else
        {
            DrawPlaceholder(graphics, bounds);
        }
    }

    /// <returns>
    ///     The image on a transparent bitmap of the size, which is kept for the next time: the caller doesn't dispose
    ///     it. Null when it can't be drawn.
    /// </returns>
    public static Bitmap? Render(string markup, int width, int height)
    {
        (width, height) = PixelSize(width, height);

        if (_kept.TryGetValue((markup, width, height), out Bitmap? kept))
        {
            return kept;
        }

        if (_kept.Count >= MaxKept)
        {
            foreach (Bitmap? old in _kept.Values)
            {
                old?.Dispose();
            }

            _kept.Clear();
        }

        Bitmap? image = RenderPixels(markup, width, height) is byte[] pixels ? ToBitmap(pixels, width, height) : null;
        _kept[(markup, width, height)] = image;

        return image;
    }

    /// <returns>
    ///     The image's pixels, blue, green, red and alpha, with the colors multiplied by the alpha, row by row from the
    ///     top; null when it can't be drawn.
    /// </returns>
    public static byte[]? RenderPixels(string markup, int width, int height)
    {
        if (_isDirect2DUnavailable)
        {
            return null;
        }

        try
        {
            _direct2D ??= D2D1.D2D1CreateFactory<ID2D1Factory1>();
            _imaging ??= new IWICImagingFactory();
        }
        catch (Exception ex) when (ex is SharpGenException or COMException or DllNotFoundException)
        {
            _isDirect2DUnavailable = true;

            return null;
        }

        try
        {
            using IWICBitmap target = _imaging.CreateBitmap(
                (uint)width,
                (uint)height,
                Vortice.WIC.PixelFormat.Format32bppPBGRA,
                BitmapCreateCacheOption.CacheOnLoad);
            var properties = new RenderTargetProperties(new Vortice.DCommon.PixelFormat(
                Vortice.DXGI.Format.B8G8R8A8_UNorm,
                Vortice.DCommon.AlphaMode.Premultiplied));

            using (ID2D1RenderTarget canvas = _direct2D.CreateWicBitmapRenderTarget(target, properties))
            using (ID2D1DeviceContext5 context = canvas.QueryInterface<ID2D1DeviceContext5>())
            using (IWICStream stream = _imaging.CreateStream())
            {
                stream.Initialize(Encoding.UTF8.GetBytes(markup));

                using ID2D1SvgDocument document =
                    context.CreateSvgDocument(stream, new Vortice.Mathematics.Size(width, height));

                context.BeginDraw();
                context.Clear(new Color4(0, 0, 0, 0));
                context.DrawSvgDocument(document);
                context.EndDraw();
            }

            int stride = width * 4;
            byte[] pixels = new byte[stride * height];

            target.CopyPixels((uint)stride, pixels);

            return pixels;
        }
        catch (Exception ex) when (ex is SharpGenException or COMException or InvalidCastException)
        {
            // Markup Windows can't read, or a Windows too old to draw SVG
            return null;
        }
    }

    private static (int Width, int Height) PixelSize(float width, float height)
    {
        float scale = Math.Min(1, MaxSide / Math.Max(width, height));

        return (
            Math.Clamp((int)Math.Ceiling(width * scale), 1, MaxSide),
            Math.Clamp((int)Math.Ceiling(height * scale), 1, MaxSide));
    }

    // Premultiplied both: Direct2D's pixels go into the bitmap as they are
    private static Bitmap ToBitmap(byte[] pixels, int width, int height)
    {
        int stride = width * 4;
        var image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        BitmapData data = image.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

        try
        {
            for (int y = 0; y < height; y++)
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

    /// <summary>A box with a cross, so an image that can't be drawn can still be seen, moved and deleted.</summary>
    private static void DrawPlaceholder(Graphics graphics, Rectangle bounds)
    {
        using var fill = new SolidBrush(_placeholderFill);
        using var line = new Pen(_placeholderLine, 1);

        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(line, bounds);
        graphics.DrawLine(line, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
        graphics.DrawLine(line, bounds.Right, bounds.Top, bounds.Left, bounds.Bottom);
    }
}
