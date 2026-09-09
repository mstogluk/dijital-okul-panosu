using System.IO;
using System.Windows.Controls;

namespace OkulPanosu.App.Views;

/// <summary>Uygulama içi kullanım kılavuzu — çıktı klasörüne kopyalanan (bkz. csproj) statik HTML
/// dosyasını gömülü bir tarayıcı kontrolüyle gösterir. Aynı içerik ayrıca web sayfası olarak da
/// yayınlanmıştır (bkz. DEVAM_NOTU.md) — ikisi de aynı kaynak dosyadan türetilir.</summary>
public partial class HelpView : UserControl
{
    public HelpView()
    {
        InitializeComponent();

        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Help", "kilavuz.html");
        if (File.Exists(path))
            Browser.Navigate(new Uri(path, UriKind.Absolute));
    }
}
