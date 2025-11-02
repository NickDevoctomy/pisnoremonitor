using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PiSnoreMonitor.Controls;

public class CachedStaticSprite(string imagePath)
{
    private Bitmap? _bitmap;

    public Bitmap GetBitmap()
    {
        if(_bitmap != null)
        {
            return _bitmap;
        }

        using var stream = File.OpenRead(imagePath);
        _bitmap = new Bitmap(stream);
        return _bitmap;
    }

    public void DrawImageUnscaled(DrawingContext ctx, double x, double y)
    {
        var rect = new Rect(x, y, GetBitmap().PixelSize.Width, GetBitmap().PixelSize.Height);
        ctx.DrawImage(GetBitmap(), rect);
    }
}
