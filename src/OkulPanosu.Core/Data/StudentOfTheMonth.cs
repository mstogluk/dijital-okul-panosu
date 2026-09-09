namespace OkulPanosu.Core.Data;

/// <summary>Ayın Öğrencisi listesindeki tek bir kayıt — yılın her ayı için ayrı bir öğrenci eklenebilir,
/// panoda sadece <see cref="IsActive"/> işaretli olanlar sırayla gösterilir.</summary>
public sealed class StudentOfTheMonth
{
    public string Name { get; set; } = "";

    public string Class { get; set; } = "";

    public string Reason { get; set; } = "";

    /// <summary>Medya\Öğrenci Fotoğrafları altındaki dosyanın SADECE adı (tam yol değil) — opsiyonel.</summary>
    public string ImageFileName { get; set; } = "";

    public string Quote { get; set; } = "";

    /// <summary>İşaretliyken panoda gösterilir (videolardaki "Yayında" checkbox'ıyla aynı desen).</summary>
    public bool IsActive { get; set; } = true;
}
