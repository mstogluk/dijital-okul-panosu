namespace OkulPanosu.Core.Data;

/// <summary>Panoda gösterilecek bir sınıf (ör. "9-A") ve haftalık ders programı.</summary>
public sealed class SchoolClass
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    /// <summary>Bu sınıfın haftalık programındaki DOLU hücreler — boş kalan gün/periyot
    /// kombinasyonları için hiç kayıt tutulmaz (Nöbet Çizelgesi'ndeki DutyAssignment ile aynı desen).</summary>
    public List<ClassLessonEntry> Lessons { get; set; } = new();
}

/// <summary>Bir sınıfın belirli bir gün + ders saatindeki dersi.</summary>
public sealed class ClassLessonEntry
{
    public DayOfWeek Day { get; set; }

    /// <summary>Ders &amp; Zil Saatleri listesindeki LessonPeriod.Period ile eşleşir (1, 2, 3...) —
    /// zaman aralığı ORADAN okunur, burada tekrar saat tutulmaz (tek doğruluk kaynağı).</summary>
    public int Period { get; set; }

    public string Subject { get; set; } = "";

    /// <summary>Opsiyonel — boş bırakılırsa panoda o satırda öğretmen alanı boş kalır.</summary>
    public string Teacher { get; set; } = "";
}
