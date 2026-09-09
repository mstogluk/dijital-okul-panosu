namespace OkulPanosu.Core.Data;

/// <summary>Duyurular modülünde dönüşümlü gösterilen tek bir duyuru.</summary>
public sealed class Announcement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    /// <summary>Duyurunun yayına gireceği tarih — varsayılan bugün.</summary>
    public DateTime StartDate { get; set; } = DateTime.Today;

    /// <summary>Duyurunun yayından otomatik kalkacağı tarih — opsiyonel, boşsa süresiz.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Duyurunun İÇERİĞİ (başlık/metin/görsel) tüm şablonlar arasında PAYLAŞILIR — aynı duyuruyu
    /// her şablonda yeniden yazmaya gerek kalmasın diye. Ama HANGİ şablon(lar)da gösterileceği ayrı ayrı
    /// seçilebilir: bir şablonun kimliği bu listede varsa (ve tarih aralığı uygunsa) o şablonda gösterilir.
    /// Yönetim'deki "Yayında" kutucuğu, o an düzenlenen şablon için bu listeye ekleme/çıkarma yapar.</summary>
    public List<string> PublishedInTemplateIds { get; set; } = new();

    /// <summary>normal / high / critical.</summary>
    public string Importance { get; set; } = "normal";

    /// <summary>Medya\Duyuru ve Haber Resimleri altındaki dosyanın SADECE adı (tam yol değil) — opsiyonel.</summary>
    public string ImageFileName { get; set; } = "";
}
