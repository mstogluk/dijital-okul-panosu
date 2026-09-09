using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Sınav dönemleri gibi tek seferlik büyük duyurular için "Büyük Anons (Görsel)" — Okulumuzdan
/// Kareler'e benzer ama bir slayt havuzu DEĞİL: sadece TEK, sabit bir görsel gösterir (dönmez).
/// "Aynı anda birden fazla görsel yan yana/2x2 görünsün" ihtiyacı, bu modülü TEK bir örnekte çoklu-görsel
/// düzenine zorlamak yerine (önceki tasarım denemesi), bu modül tipinin İSTİSNAİ olarak bir şablona
/// BİRDEN FAZLA kez eklenebilmesiyle çözüldü (bkz. TemplateEditorView.MultiInstanceModuleTypes) — her
/// eklenen kopya kendi TEK görselini taşır, kullanıcı normal ızgara editörüyle (sürükle/boyutlandır)
/// istediği gibi yan yana/alt alta/2x2/3x2... dizer; tamamen ona kalır. Görsel, Okulumuzdan Kareler'in
/// Slayt klasöründen AYRI bir klasörde tutulur (AnnouncementImageFolderPath) — aksi hâlde buraya seçilen
/// görsel oradaki otomatik döngüye de karışırdı.</summary>
public partial class BannerImageModuleView : UserControl
{
    public BannerImageModuleView(BoardModule module)
    {
        InitializeComponent();

        var fileName = module.Settings.TryGetValue("imageFileName", out var f) ? f : "";
        var folder = AppServices.Data?.AnnouncementImageFolderPath;
        var path = !string.IsNullOrWhiteSpace(fileName) && folder is not null ? Path.Combine(folder, fileName) : null;

        if (path is null || !File.Exists(path))
        {
            EmptyText.Text = "Anons görseli seçilmemiş — modül ayarlarından ekleyin";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            PhotoImage.Source = bitmap;
            PhotoImage.Visibility = Visibility.Visible;
        }
        catch (IOException)
        {
            EmptyText.Text = "Görsel yüklenemedi";
            EmptyText.Visibility = Visibility.Visible;
        }
        catch (NotSupportedException)
        {
            EmptyText.Text = "Görsel yüklenemedi";
            EmptyText.Visibility = Visibility.Visible;
        }
    }
}
