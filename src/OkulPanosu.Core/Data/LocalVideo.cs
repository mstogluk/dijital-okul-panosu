namespace OkulPanosu.Core.Data;

/// <summary>Yerel Videolar modülünde oynatılan tek bir video kaydı.</summary>
public sealed class LocalVideo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = "";

    /// <summary>Medya\Videolar altındaki dosyanın SADECE adı (tam yol değil) — veri klasörü taşınsa/adı
    /// değişse bile bulunabilsin diye her zaman güncel klasör yoluyla birleştirilir.</summary>
    public string FileName { get; set; } = "";

    public bool IsActive { get; set; } = true;

    /// <summary>"Her Gün" veya bir DayOfWeek adı (Türkçe) — hangi gün oynatılacağı.</summary>
    public string ScheduleDay { get; set; } = "Her Gün";
}
