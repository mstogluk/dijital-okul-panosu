using System.Windows;
using System.Windows.Media;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Services;

public sealed record BoardColorModeOption(string Key, string DisplayName);

/// <summary>
/// Şablon bazlı pano (Kiosk/TV + Yönetim'deki canlı önizleme) taban rengi. Vurgu rengi (ThemeManager)
/// app-genelinde ve makine bazlıdır; bu ise şablonun KENDİ ayarıdır ve sadece panoya yerleştirilen
/// modüllerin göründüğü kapsamda (bkz. BoardGridControl.Resources) uygulanır — Yönetim arayüzünün geri
/// kalanı HER ZAMAN koyu temada sabit kalır.
///
/// Önceden 3 sabit hazır palet (Koyu/Gök Mavisi/Buz Mavisi) vardı — kullanıcı elle ayarlanan tonların
/// birbirini tutmadığını (kart/liste zeminleri "sırıtıyor" — ana zeminle aynı tonda değil) fark edip
/// bunun yerine kendi rengini seçebileceği bir seçenek istedi. Şimdi kullanıcı SADECE tek bir "taban"
/// rengi seçiyor (bkz. TemplateEditorView'daki renk seçici), kart/liste/kenarlık/metin tonları bu TEK
/// renkten OTOMATİK türetiliyor (aynı renk ailesinde küçük açık/koyu adımlar) — böylece hiçbir öğe farklı
/// bir renk ailesinden gelmiyor, hepsi birbiriyle tutarlı kalıyor.
/// </summary>
public static class BoardColorModeManager
{
    public const string DefaultModeKey = "dark";
    public const string CustomModeKey = "custom";
    public const string DefaultCustomColorHex = "#1B4B85";

    private const string MarkerKey = "__BoardColorModeCustom";

    public static readonly IReadOnlyList<BoardColorModeOption> Modes =
    [
        new(DefaultModeKey, "Koyu (Varsayılan)"),
        new(CustomModeKey, "Özel Renk"),
    ];

    /// <summary>Verilen scope'un (tipik olarak BoardGridControl) kendi Resources.MergedDictionaries'ine,
    /// şablonun ColorMode/CustomBaseColor'üne göre türetilmiş paleti ekler/değiştirir — sadece bu scope ve
    /// altındaki DynamicResource referanslarını etkiler, uygulamanın geri kalanını ETKİLEMEZ. "dark" için
    /// hiçbir sözlük eklenmez (Application seviyesindeki Themes/Base.xaml varsayılanına düşer).</summary>
    public static void Apply(FrameworkElement scope, Template? template)
    {
        var dictionaries = scope.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(d => d.Contains(MarkerKey));
        if (existing is not null) dictionaries.Remove(existing);

        if (template?.ColorMode != CustomModeKey) return;

        var baseColor = TryParseHex(template.CustomBaseColor, out var color) ? color : TryParseHexOrDefault();
        dictionaries.Add(BuildPalette(baseColor));
    }

    public static bool TryParseHex(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        try
        {
            var converted = ColorConverter.ConvertFromString(hex);
            if (converted is not Color c) return false;
            color = c;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Color TryParseHexOrDefault() =>
        TryParseHex(DefaultCustomColorHex, out var c) ? c : Color.FromRgb(0x1B, 0x4B, 0x85);

    /// <summary>Tek bir taban rengi (kullanıcının seçtiği) etrafında tutarlı bir palet üretir. Zemin
    /// niteliğindeki TÜM anahtarlar (kart, liste/boş alan, kenarlık öncesi giriş kutusu, kanvas) kasıtlı
    /// olarak BİREBİR AYNI renge eşitlenir — önceki sürümde bunlar birbirinden küçük oranlarda farklı
    /// harmanlanıyordu, geniş boş alanlarda (ör. Ders &amp; Teneffüs'ün doldurulmamış kısmı) bu küçük fark
    /// bile "sırıtan" bir yama gibi göze çarpıyordu. Kartları kanvastan ayırmak için artık SADECE
    /// kenarlık (BorderBrush1) ve mevcut gölge efekti kullanılıyor. Metin rengi tabanın parlaklığına göre
    /// (koyu tabanda açık, açık tabanda koyu) maksimum kontrastlı "zıt" uçlardan seçiliyor.</summary>
    private static ResourceDictionary BuildPalette(Color baseColor)
    {
        var isDark = Luma(baseColor) < 0.5;
        var tint = isDark ? Colors.White : Color.FromRgb(0x06, 0x0A, 0x10);

        var textPrimary = isDark ? Color.FromRgb(0xF6, 0xF9, 0xFC) : Color.FromRgb(0x07, 0x0D, 0x16);
        var textMuted = Blend(textPrimary, baseColor, 0.30);

        var dict = new ResourceDictionary { { MarkerKey, true } };

        var groundBrush = Freeze(new SolidColorBrush(baseColor));
        dict["BgPrimaryBrush"] = groundBrush;
        dict["BgSecondaryBrush"] = groundBrush;
        dict["BgInputBrush"] = groundBrush;
        dict["BgElevatedBrush"] = Freeze(new SolidColorBrush(Blend(baseColor, tint, 0.18)));
        dict["BorderBrush1"] = Freeze(new SolidColorBrush(Blend(baseColor, tint, 0.38)));
        dict["TextPrimaryBrush"] = Freeze(new SolidColorBrush(textPrimary));
        dict["TextMutedBrush"] = Freeze(new SolidColorBrush(textMuted));

        dict["SuccessBrush"] = Freeze(new SolidColorBrush(isDark ? Color.FromRgb(0x10, 0xB9, 0x81) : Color.FromRgb(0x05, 0x96, 0x69)));
        dict["WarningBrush"] = Freeze(new SolidColorBrush(isDark ? Color.FromRgb(0xFB, 0xBF, 0x24) : Color.FromRgb(0xC2, 0x76, 0x0A)));
        dict["DangerBrush"] = Freeze(new SolidColorBrush(isDark ? Color.FromRgb(0xFB, 0x71, 0x85) : Color.FromRgb(0xE1, 0x1D, 0x48)));

        dict["BoardCanvasBackgroundBrush"] = groundBrush;

        // Ders & Teneffüs'ün "TENEFFÜS" gölgelemesi — sabit bir renge (ör. lacivert) değil, kullanıcının
        // seçtiği rengin KENDİSİNİN koyulaştırılmış hâline dayanır, böylece o da aynı renk ailesinden kalır.
        var overlayTint = Blend(baseColor, Colors.Black, 0.72);
        dict["BreakOverlayBrush"] = Freeze(new SolidColorBrush(Color.FromArgb(0x9A, overlayTint.R, overlayTint.G, overlayTint.B)));

        return dict;
    }

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }

    private static Color Blend(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }

    private static double Luma(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
}
