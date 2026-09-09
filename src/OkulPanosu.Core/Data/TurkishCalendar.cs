namespace OkulPanosu.Core.Data;

/// <summary>Tarihte Bugün ve Doğum Günleri modüllerinde serbest metin yerine ay listelerinden seçim
/// yapmak için paylaşılan ay adları — kullanıcı "Temuz"/"subat" gibi yazım hatası yapamasın, ve
/// panodaki eşleştirme metin karşılaştırması yerine güvenilir Gün/Ay sayılarına dayansın diye.</summary>
public static class TurkishCalendar
{
    public static readonly string[] MonthNames =
    [
        "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık",
    ];
}
