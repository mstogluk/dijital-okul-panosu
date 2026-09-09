using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OkulPanosu.App.Services;

namespace OkulPanosu.App.Views;

/// <summary>Okulumuzdan Kareler — diğer modüllerden farklı olarak paylaşılan JSON'da hiç kaydı yok,
/// doğrudan Medya\Slayt klasöründeki dosyalarla çalışır (bkz. SchoolGalleryModuleView). Kullanıcı
/// (müdür/idareci/öğretmen gibi paylaşılan klasörün nerede olduğunu bilmeyen biri) kendi PC'sinden
/// "Fotoğraf Ekle" ile istediği kadar dosya seçer, uygulama bunları otomatik olarak o klasöre kopyalar
/// — Personel fotoğrafı/Ayın Öğrencisi ile AYNI "seç, biz halledelim" deseni (bkz. EditorControls.
/// PickAndImportFiles).</summary>
public partial class SchoolGalleryView : UserControl, IReloadablePage
{
    public SchoolGalleryView()
    {
        InitializeComponent();
        Load();
    }

    public void Reload() => Load();

    private void Load()
    {
        PhotosPanel.Children.Clear();

        var folder = AppServices.Data?.SlideshowFolderPath;
        var files = folder is not null && Directory.Exists(folder)
            ? Directory.GetFiles(folder)
                .Where(f => EditorControls.ImageExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : [];

        EmptyText.Visibility = files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var path in files)
        {
            var tile = BuildTile(path);
            if (tile is not null) PhotosPanel.Children.Add(tile);
        }
    }

    /// <summary>Dosya, listeledikten SONRA (ör. Gezgin'den ya da dosya seçme penceresinden) silinmiş
    /// olabilir — bu artık uygulamayı çökertmesin diye korumalı; yüklenemeyen dosya sessizce atlanır.</summary>
    private UIElement? BuildTile(string path)
    {
        BitmapImage bitmap;
        try
        {
            if (!File.Exists(path)) return null;

            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 220;
            bitmap.EndInit();
        }
        catch (IOException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }

        var thumb = new Border
        {
            Width = 130,
            Height = 130,
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
            Child = new Image { Source = bitmap, Stretch = Stretch.UniformToFill },
        };

        var overlay = new Grid { Width = 130, Height = 130, Margin = new Thickness(0, 0, 10, 10) };
        overlay.Children.Add(thumb);

        var delete = EditorControls.DeleteIconButton(() =>
        {
            if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            Load();
        });
        var deleteHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4),
            Background = (Brush)Application.Current.Resources["BgPrimaryBrush"],
            CornerRadius = new CornerRadius(4),
            Child = delete,
        };
        overlay.Children.Add(deleteHost);

        return overlay;
    }

    private void AddPhotos_Click(object sender, RoutedEventArgs e)
    {
        var folder = AppServices.Data?.SlideshowFolderPath;
        if (folder is null) return;

        var imported = EditorControls.PickAndImportFiles(Window.GetWindow(this), folder, EditorControls.ImageExtensions, "Görsel Dosyaları");
        if (imported.Length == 0) return;

        Load();
    }
}
