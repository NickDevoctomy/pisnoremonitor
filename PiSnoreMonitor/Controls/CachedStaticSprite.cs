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
}
