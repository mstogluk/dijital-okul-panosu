namespace OkulPanosu.Core.Data;

/// <summary>Bir pano düzeni — belirli bir ızgara boyutu ve üzerine yerleştirilmiş modüller.</summary>
public sealed class Template
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public bool IsActive { get; set; }

    public int GridColumns { get; set; } = 50;

    public int GridRows { get; set; } = 50;

    public List<BoardModule> Modules { get; set; } = new();

    /// <summary>Yerel Video modülünün oynattığı liste artık ŞABLONA ÖZGÜ — önceden tüm şablonlar aynı
    /// paylaşılan havuzu kullanıyordu, kullanıcı her şablonun kendi videolarını oynatabilmesini istedi
    /// (ör. sınav anons şablonunda farklı, ana şablonda farklı videolar).</summary>
    public List<LocalVideo> Videos { get; set; } = new();

    /// <summary>Kayan duyuru artık bir modül değil, şablonun kendi özelliği — panonun en altında sabit bir şerit olarak,
    /// ızgaradaki modüllerden bağımsız her zaman render edilir.</summary>
    public string TickerText { get; set; } = "";

    /// <summary>Tam bir tur için saniye — küçük değer daha hızlı kayar.</summary>
    public int TickerSpeed { get; set; } = 25;

    public string TickerBgColor { get; set; } = "#1e3a8a";

    public string TickerTextColor { get; set; } = "#ffffff";

    /// <summary>Panonun taban görünüm modu — "dark" (Koyu, varsayılan) veya "custom" (kullanıcının
    /// CustomBaseColor'da seçtiği tek renk üzerinden otomatik türetilen palet). Sadece bu şablonun
    /// yayındaki/önizlemedeki görünümünü etkiler; Yönetim arayüzünün kendi teması bundan bağımsız
    /// olarak her zaman koyu kalır.</summary>
    public string ColorMode { get; set; } = "dark";

    /// <summary>ColorMode "custom" iken kullanılan, kullanıcının renk seçiciyle belirlediği taban renk
    /// (ör. "#1B4B85") — kart/liste/kenarlık/metin tonları bundan OTOMATİK türetilir (bkz.
    /// BoardColorModeManager), böylece hepsi aynı renk ailesinden gelir.</summary>
    public string CustomBaseColor { get; set; } = "#1B4B85";
}
