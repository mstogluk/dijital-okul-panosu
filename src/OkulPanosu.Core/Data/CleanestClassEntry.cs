namespace OkulPanosu.Core.Data;

/// <summary>Haftanın En Temiz Sınıfları modülünde bir derece kartı. Sıra numarası artık elle GİRİLMİYOR —
/// hangi sırada eklenmiş olursa olsun, gösterilirken Score'a göre büyükten küçüğe sıralanıp sıra numarası
/// oradan türetiliyor (bkz. CleanestClassView/CleanestClassModuleView).</summary>
public sealed class CleanestClassEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string ClassName { get; set; } = "";

    public int Score { get; set; }

    public string Award { get; set; } = "";
}
