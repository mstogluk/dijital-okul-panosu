namespace OkulPanosu.Core.Data;

/// <summary>Okul personeli/öğretmen kaydı — nöbet çizelgesi gibi yerlerde isim serbest metin yerine
/// buradaki listeden seçilir, fotoğrafı da (varsa) buradan gelir.</summary>
public sealed class Personnel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    /// <summary>teacher / staff — nöbet çizelgesi ve ders programı seçimlerinde sadece "teacher"
    /// olanlar listelenir; "staff" (memur/hizmetli) sadece pano üzerinde (ör. doğum günü) görünür.</summary>
    public string Category { get; set; } = "teacher";

    /// <summary>Branş — ör. "Fen Bilgisi". Öğretmen olmayan personelde genelde boş kalır.</summary>
    public string Branch { get; set; } = "";

    /// <summary>Görev/Ünvan — ör. "Öğretmen", "Müdür Yardımcısı", "Hizmetli", "Memur".</summary>
    public string Title { get; set; } = "";

    /// <summary>Medya\Öğretmen Fotoğrafları altındaki dosyanın SADECE adı (tam yol DEĞİL) — veri
    /// klasörü taşınsa/adı değişse bile bulunabilsin diye her zaman güncel klasör yoluyla birleştirilir.
    /// Boşsa cinsiyete göre varsayılan silüet gösterilir.</summary>
    public string PhotoFileName { get; set; } = "";

    /// <summary>male / female — varsayılan silüet seçimi için.</summary>
    public string Gender { get; set; } = "male";

    /// <summary>1-31, bilinmiyorsa null — bkz. Student.BirthDay (aynı opsiyonel yaklaşım).</summary>
    public int? BirthDay { get; set; }

    /// <summary>1-12, bilinmiyorsa null.</summary>
    public int? BirthMonth { get; set; }
}
