using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Sınav dönemleri (ÖSYM, Açık Öğretim vb.) gibi tek seferlik büyük duyurular için — Duyurular
/// modülünün aksine dönen bir liste değil, TEK bir metni ekran boyunca kocaman gösterir. Yazı boyutu ve
/// yazı tipi modül ayarlarından ("fontSize"/"fontFamily") seçilir. Metin, seçilen punto ile modül alanına
/// SIĞMIYORSA Viewbox (DownOnly) orantılı olarak küçültür — asla taşmaz/kırpılmaz, ama kullanıcının
/// seçtiği punto alan yeterliyse birebir uygulanır.</summary>
public partial class BannerTextModuleView : UserControl
{
    public BannerTextModuleView(BoardModule module)
    {
        InitializeComponent();

        var text = module.Settings.TryGetValue("text", out var t) ? t : "";
        if (string.IsNullOrWhiteSpace(text))
        {
            TextHost.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Anons metni tanımlanmamış — modül ayarlarından ekleyin";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        var fontSize = module.Settings.TryGetValue("fontSize", out var fs) && double.TryParse(fs, out var size) && size > 0
            ? size
            : 72;
        var fontFamily = module.Settings.TryGetValue("fontFamily", out var ff) && !string.IsNullOrWhiteSpace(ff)
            ? ff
            : "Segoe UI Black";

        BannerText.Text = text;
        BannerText.FontSize = fontSize;
        try { BannerText.FontFamily = new FontFamily(fontFamily); }
        catch (FormatException) { }
    }
}
