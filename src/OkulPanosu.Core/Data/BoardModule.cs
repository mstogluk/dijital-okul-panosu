namespace OkulPanosu.Core.Data;

/// <summary>Panodaki tek bir modülün ızgara üzerindeki konumu ve ayarları.</summary>
public sealed class BoardModule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Modül tipi anahtarı — örn. "clock_date", "ticker", "duty_teacher".</summary>
    public string Type { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>Izgara sütun indeksi (0 tabanlı).</summary>
    public int X { get; set; }

    /// <summary>Izgara satır indeksi (0 tabanlı).</summary>
    public int Y { get; set; }

    /// <summary>Genişlik (hücre sayısı).</summary>
    public int W { get; set; } = 1;

    /// <summary>Yükseklik (hücre sayısı).</summary>
    public int H { get; set; } = 1;

    public string ThemeColor { get; set; } = "Blue";

    /// <summary>Modüle özel serbest ayarlar (örn. ticker mesajları "|" ile ayrılmış tek satır).</summary>
    public Dictionary<string, string> Settings { get; set; } = new();
}
