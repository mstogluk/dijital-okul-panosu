using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>"Okulumuzdan Kareler" — ayrı bir yönetim ekranı yok, Medya\Slayt klasöründe ne varsa
/// (kullanıcı dosyaları doğrudan o klasöre kopyalar) otomatik olarak sırayla gösterilir. Geçiş süresi
/// modülün ⚙️ ayarlarından ("transitionSeconds") gelir, girilmemişse 6 saniye.</summary>
public partial class SchoolGalleryModuleView : UserControl
{
    private const int DefaultSeconds = 6;

    private readonly DispatcherTimer _timer = new();
    private List<string> _files = [];
    private int _index;

    public SchoolGalleryModuleView(BoardModule module)
    {
        InitializeComponent();

        var seconds = module.Settings.TryGetValue("transitionSeconds", out var raw) && int.TryParse(raw, out var s) && s > 0
            ? s
            : DefaultSeconds;
        _timer.Interval = TimeSpan.FromSeconds(seconds);

        Load();

        _timer.Tick += (_, _) => Advance();
        _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Load()
    {
        var folder = AppServices.Data?.SlideshowFolderPath;
        try
        {
            _files = folder is not null && Directory.Exists(folder)
                ? Directory.GetFiles(folder)
                    .Where(f => EditorControls.ImageExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : [];
        }
        catch (IOException)
        {
            _files = [];
        }

        if (_files.Count == 0)
        {
            PhotoImage.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Slayt klasöründe resim yok — Medya\\Slayt klasörüne resim ekleyin";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        PhotoImage.Visibility = Visibility.Visible;
        _index = 0;
        ShowCurrent();
    }

    private void Advance()
    {
        if (_files.Count == 0) return;
        _index = (_index + 1) % _files.Count;
        ShowCurrent();
    }

    /// <summary>Klasördeki bir dosya, listeledikten SONRA (ör. yönetimden silinerek) kaybolmuş olabilir —
    /// bu artık uygulamayı çökertmesin diye her deneme korumalı: yüklenemeyen dosya listeden çıkarılır,
    /// bir SONRAKİ dosyaya geçilir (elle Advance beklemeden), hepsi bozuksa boş duruma düşülür.</summary>
    private void ShowCurrent()
    {
        while (_files.Count > 0)
        {
            var path = _files[_index];
            if (TryLoadBitmap(path, out var bitmap))
            {
                PhotoImage.Source = bitmap;
                return;
            }

            _files.RemoveAt(_index);
            if (_files.Count == 0) break;
            if (_index >= _files.Count) _index = 0;
        }

        PhotoImage.Visibility = Visibility.Collapsed;
        EmptyText.Text = "Slayt klasöründe resim yok — Medya\\Slayt klasörüne resim ekleyin";
        EmptyText.Visibility = Visibility.Visible;
    }

    private static bool TryLoadBitmap(string path, out BitmapImage bitmap)
    {
        try
        {
            if (!File.Exists(path)) { bitmap = null!; return false; }

            var loaded = new BitmapImage();
            loaded.BeginInit();
            loaded.UriSource = new Uri(path, UriKind.Absolute);
            loaded.CacheOption = BitmapCacheOption.OnLoad;
            loaded.EndInit();
            bitmap = loaded;
            return true;
        }
        catch (IOException)
        {
            bitmap = null!;
            return false;
        }
        catch (NotSupportedException)
        {
            bitmap = null!;
            return false;
        }
    }
}
