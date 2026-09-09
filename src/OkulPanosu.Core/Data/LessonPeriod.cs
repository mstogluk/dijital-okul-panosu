using System.Text.Json.Serialization;

namespace OkulPanosu.Core.Data;

/// <summary>Ders/teneffüs zil çizelgesindeki tek bir satır. Zaman artık serbest metin değil — bu periyodun
/// başlangıcı (Start), ders süresi (DurationMinutes) ve bu periyottan SONRAKİ teneffüsün süresi
/// (BreakAfterMinutes) olarak tutulur. Bir sonraki periyodun Start'ı bunlardan HESAPLANIR (bkz.
/// ScheduleView.RecalculateStartTimes) — yalnızca ilk periyodun Start'ı elle/otomatik oluşturmayla girilir,
/// geri kalanı zincirleme türetilir.</summary>
public sealed class LessonPeriod
{
    public int Period { get; set; }

    public string Subject { get; set; } = "";

    public TimeSpan Start { get; set; }

    public int DurationMinutes { get; set; } = 40;

    /// <summary>Bu ders bitince başlayacak teneffüsün süresi — bir sonraki periyodun Start'ı
    /// Start + DurationMinutes + BreakAfterMinutes olarak hesaplanır.</summary>
    public int BreakAfterMinutes { get; set; } = 10;

    [JsonIgnore]
    public TimeSpan End => Start.Add(TimeSpan.FromMinutes(DurationMinutes));

    [JsonIgnore]
    public string TimeLabel => $"{Start:hh\\:mm} - {End:hh\\:mm}";

    /// <summary>Bir TimeSpan'i 0-24 saatlik "günün saati" aralığına sarar (mod 24 saat). Periyot
    /// başlangıçları zincirleme toplanarak hesaplandığı için (bkz. ScheduleView.RecalculateStartTimes),
    /// normalize edilmezse gece yarısını geçen bir değer TimeSpan.Days &gt; 0 olarak birikip kalıyordu —
    /// "hh\:mm" biçimi bunu GÖRSEL olarak gizliyordu (sadece saat/dakika kısmını gösterip Days'i atlıyordu)
    /// ama karşılaştırmalarda (ör. "şu an bu periyotta mıyız") DateTime.Now.TimeOfDay her zaman &lt;24 saat
    /// olduğu için hiçbir zaman eşleşmiyordu — pano modülü sessizce boş kalıyordu. Kesin çözüm: her
    /// hesaplanan Start bu metottan geçirilir.</summary>
    public static TimeSpan NormalizeTimeOfDay(TimeSpan value)
    {
        var ticks = value.Ticks % TimeSpan.TicksPerDay;
        if (ticks < 0) ticks += TimeSpan.TicksPerDay;
        return TimeSpan.FromTicks(ticks);
    }
}
