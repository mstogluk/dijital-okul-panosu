using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

/// <summary>Duyuru İÇERİĞİ (başlık/metin/görsel/tarih/önem) tüm şablonlar arasında PAYLAŞILIR — aynı
/// listeyi düzenler, kimden açılırsa açılsın. Ama "Yayında" kutucuğu artık ŞABLONA ÖZGÜ (bkz.
/// Announcement.PublishedInTemplateIds): kullanıcı "duyuruyu bu şablonda yayınla" demek istedi, her
/// şablonda içeriği yeniden yazmak istemedi. Bağımsız İçerikler sayfası olarak açıldığında üstte bir
/// şablon seçici belirir (Videolar'daki AYNI desen); Modül Ayarları içinden gömülü açıldığında hangi
/// şablonun düzenlendiği zaten belli olduğundan seçici gizlenir.</summary>
public partial class AnnouncementsView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private readonly string? _lockedTemplateId;
    private readonly List<Announcement> _announcements = new();
    private string? _templateId;

    public AnnouncementsView(string? lockedTemplateId = null)
    {
        InitializeComponent();
        _lockedTemplateId = lockedTemplateId;
        TemplatePickerHost.Visibility = lockedTemplateId is null ? Visibility.Visible : Visibility.Collapsed;
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        var data = repo.Load();

        if (data.Templates.Count == 0)
        {
            _announcements.Clear();
            AnnouncementsList.Items.Clear();
            EmptyText.Text = "Önce Şablonlar sayfasından bir şablon oluşturun.";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        _templateId ??= _lockedTemplateId ?? data.ActiveTemplateId ?? data.Templates[0].Id;
        if (data.Templates.All(t => t.Id != _templateId)) _templateId = data.Templates[0].Id;

        if (_lockedTemplateId is null)
        {
            TemplatePickerCombo.Content = EditorControls.LabeledComboBox(
                data.Templates, "Name", "Id", _templateId,
                v =>
                {
                    _templateId = v as string;
                    Load();
                });
        }

        EmptyText.Visibility = Visibility.Collapsed;
        _announcements.Clear();
        _announcements.AddRange(data.Announcements);
        Rebuild();
    }

    public void Reload() => Load();

    private void PersistSilently()
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.Announcements = _announcements);
    }

    /// <summary>Sütun sırası: DURUM (yayın durumu) | BAŞLIK | BAŞLANGIÇ | BİTİŞ | ÖNEM | Fotoğraf |
    /// Foto Ekle | Sil — kullanıcının açıkça istediği sıra. Önceden "Önem" ayrı bir alt satırdaydı,
    /// artık ana satırda; DURUM ise sadece çıplak bir kutucuktu (başlığıyla birlikte kartlar arasında
    /// kaybolup "bu ne?" sorusuna yol açıyordu) — artık her kartta "Yayında"/"Yayında Değil" yazısıyla
    /// birlikte, tek başına da anlaşılır.</summary>
    private void Rebuild()
    {
        AnnouncementsList.Items.Clear();
        foreach (var a in _announcements)
        {
            var card = new StackPanel();

            var row = new Grid { Margin = new Thickness(4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // --- DURUM (0): kutucuk + "Yayında"/"Yayında Değil" yazısı, bu ŞABLONA özgü ---
            // ("tümünden kaldır" bağlantısı denendi ama kaldırıldı — kullanıcı: yanlışlıkla tıklanma
            // riski, gerçek fayda düşük (bir duyuru genelde en fazla 2-3 şablonda oluyor, elle tek tek
            // kapatmak yeterli).
            var statusStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            var statusCheck = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
            var statusLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontSize = 12, FontWeight = FontWeights.SemiBold };

            void UpdateStatusLabel(bool published)
            {
                statusLabel.Text = published ? "Yayında" : "Yayında Değil";
                statusLabel.Foreground = published ? (Brush)Application.Current.Resources["SuccessBrush"] : (Brush)Application.Current.Resources["TextMutedBrush"];
            }

            var isPublished = _templateId is not null && a.PublishedInTemplateIds.Contains(_templateId);
            statusCheck.IsChecked = isPublished;
            UpdateStatusLabel(isPublished);

            statusCheck.Checked += (_, _) =>
            {
                if (_templateId is not null && !a.PublishedInTemplateIds.Contains(_templateId))
                    a.PublishedInTemplateIds.Add(_templateId);
                UpdateStatusLabel(true);
                PersistSilently();
            };
            statusCheck.Unchecked += (_, _) =>
            {
                if (_templateId is not null) a.PublishedInTemplateIds.Remove(_templateId);
                UpdateStatusLabel(false);
                PersistSilently();
            };

            statusStack.Children.Add(statusCheck);
            statusStack.Children.Add(statusLabel);
            Grid.SetColumn(statusStack, 0);
            row.Children.Add(statusStack);

            // --- BAŞLIK (1) ---
            var titleBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = a.Title, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Başlık" };
            titleBox.LostFocus += (_, _) => { a.Title = titleBox.Text; PersistSilently(); };
            Grid.SetColumn(titleBox, 1);
            row.Children.Add(titleBox);

            // --- BAŞLANGIÇ (2) ---
            var startBox = EditorControls.DateTextBox(a.StartDate, v => { a.StartDate = v ?? DateTime.Today; PersistSilently(); });
            startBox.Margin = new Thickness(0, 0, 8, 0);
            startBox.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(startBox, 2);
            row.Children.Add(startBox);

            // --- BİTİŞ (3) ---
            var endBox = EditorControls.DateTextBox(a.EndDate, v => { a.EndDate = v; PersistSilently(); }, allowEmpty: true);
            endBox.Margin = new Thickness(0, 0, 8, 0);
            endBox.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(endBox, 3);
            row.Children.Add(endBox);

            // --- ÖNEM (4) — kullanıcı isteğiyle Bitiş'in hemen sağına taşındı ---
            var currentImportance = string.IsNullOrWhiteSpace(a.Importance) ? "normal" : a.Importance;
            var importanceCombo = EditorControls.LabeledComboBox(
                new[]
                {
                    new { Key = "normal", Label = "Normal" },
                    new { Key = "high", Label = "Önemli" },
                    new { Key = "critical", Label = "Acil" },
                },
                "Label", "Key", currentImportance, v => { a.Importance = v as string ?? "normal"; PersistSilently(); });
            importanceCombo.VerticalAlignment = VerticalAlignment.Center;
            importanceCombo.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(importanceCombo, 4);
            row.Children.Add(importanceCombo);

            // --- Fotoğraf (5) ---
            var photo = new Border
            {
                Width = 48,
                Height = 48,
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
            };
            var image = new Image { Stretch = Stretch.UniformToFill, Source = LoadPhoto(a) };
            photo.Child = image;
            Grid.SetColumn(photo, 5);
            row.Children.Add(photo);

            // --- Foto Ekle (6) ---
            var photoButton = new Button
            {
                Content = string.IsNullOrWhiteSpace(a.ImageFileName) ? "📷 Foto Ekle" : "📷 Foto",
                Style = EditorControls.SecondaryButton,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Görsel seç",
            };
            photoButton.Click += (_, _) => PickPhoto(photoButton, a, image, photoButton);
            Grid.SetColumn(photoButton, 6);
            row.Children.Add(photoButton);

            // --- Sil (7) ---
            var deleteButton = EditorControls.DeleteIconButton(() => DeleteAnnouncement(a), "bu duyuruyu");
            deleteButton.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(deleteButton, 7);
            row.Children.Add(deleteButton);

            card.Children.Add(row);

            var detailsRow = new Grid { Margin = new Thickness(4, 8, 4, 4) };
            detailsRow.Children.Add(EditorControls.LabeledMultilineTextBox("İçerik", a.Content, v => { a.Content = v; PersistSilently(); }, 60));
            card.Children.Add(detailsRow);

            AnnouncementsList.Items.Add(EditorControls.CardWith(card));
        }
    }

    private void PickPhoto(Button anchor, Announcement a, Image image, Button photoButton)
    {
        var folder = AppServices.Data?.ImagesFolderPath;
        if (folder is null) return;

        var picked = EditorControls.PickAndImportFile(Window.GetWindow(anchor), folder, EditorControls.ImageExtensions, "Görsel Dosyaları");
        if (picked is null) return;

        a.ImageFileName = picked;
        image.Source = LoadPhoto(a);
        photoButton.Content = "📷 Foto";
        PersistSilently();
    }

    /// <summary>Bir duyuruyu silmek görseli KLASÖRDEN silmez — aynı görsel başka bir duyuruda da
    /// kullanılıyor olabilir. Kullanılmayan görselleri temizlemek için "📂 Klasörü Aç" butonuyla klasörü
    /// açıp istediğinizi elle silebilirsiniz. Duyuru İÇERİĞİ paylaşılan olduğundan, silme İŞLEMİ de
    /// duyuruyu TÜM şablonlardan kaldırır (sadece bu şablondan kaldırmak için Yayında kutusunu kapatmak
    /// yeterli). Yönetici şifresi kuruluysa ilk silmede sorulur, 20 dakikalık oturum boyunca tekrar
    /// sorulmaz.</summary>
    private void DeleteAnnouncement(Announcement a)
    {
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;

        _announcements.Remove(a);
        PersistSilently();
        Rebuild();
    }

    private void OpenImagesFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = AppServices.Data?.ImagesFolderPath;
        if (folder is null) return;
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    private static BitmapSource? LoadPhoto(Announcement a)
    {
        var folder = AppServices.Data?.ImagesFolderPath;
        if (!string.IsNullOrWhiteSpace(a.ImageFileName) && folder is not null)
        {
            var fullPath = Path.Combine(folder, a.ImageFileName);
            if (File.Exists(fullPath))
                return new BitmapImage(new Uri(fullPath, UriKind.Absolute));
        }

        return null;
    }

    private void AddAnnouncement_Click(object sender, RoutedEventArgs e)
    {
        var announcement = new Announcement();
        if (_templateId is not null) announcement.PublishedInTemplateIds.Add(_templateId);
        _announcements.Add(announcement);
        PersistSilently();
        Rebuild();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        PersistSilently();
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
