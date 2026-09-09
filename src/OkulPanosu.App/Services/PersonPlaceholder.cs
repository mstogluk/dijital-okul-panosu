using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OkulPanosu.App.Services;

/// <summary>Öğretmen/öğrenci fotoğrafı tanımlanmamış veya dosyası bulunamadığında kullanılan,
/// tamamen kod içinde (harici dosya gerekmeden) çizilen yüzsüz cinsiyet silüeti.</summary>
public static class PersonPlaceholder
{
    private static readonly Dictionary<string, BitmapSource> Cache = new();

    public static BitmapSource GetSilhouette(string? gender)
    {
        var key = string.Equals(gender, "female", StringComparison.OrdinalIgnoreCase) ? "female" : "male";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var bg = key == "female" ? Color.FromRgb(0xBE, 0x18, 0x5D) : Color.FromRgb(0x1D, 0x4E, 0xD8);
        var fg = Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF);

        const int size = 200;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawEllipse(new SolidColorBrush(bg), null, new Point(size / 2.0, size / 2.0), size / 2.0, size / 2.0);

            // Baş
            dc.DrawEllipse(new SolidColorBrush(fg), null, new Point(size / 2.0, size * 0.38), size * 0.16, size * 0.16);

            // Omuzlar / gövde (basit yarım elips, alt kısımda taşan alan daire tarafından kırpılır)
            var shoulders = new StreamGeometry();
            using (var g = shoulders.Open())
            {
                g.BeginFigure(new Point(size * 0.22, size * 1.05), true, true);
                g.QuadraticBezierTo(new Point(size * 0.5, size * 0.58), new Point(size * 0.78, size * 1.05), true, false);
            }
            dc.PushClip(new EllipseGeometry(new Point(size / 2.0, size / 2.0), size / 2.0, size / 2.0));
            dc.DrawGeometry(new SolidColorBrush(fg), null, shoulders);
            dc.Pop();
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        Cache[key] = bitmap;
        return bitmap;
    }
}
