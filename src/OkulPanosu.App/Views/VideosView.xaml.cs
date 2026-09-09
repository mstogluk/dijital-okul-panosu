using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

/// <summary>Video listesi artık ŞABLONA ÖZGÜ (bkz. Template.Videos) — her şablonun kendi video havuzu
/// olabilir. Bağımsız İçerikler sayfası olarak açıldığında (<see cref="_lockedTemplateId"/> null) üstte
/// bir şablon seçici belirir; Modül Ayarları (⚙️) içinden gömülü açıldığında zaten hangi şablonun
/// düzenlendiği belli olduğundan seçici gizlenir.</summary>
public partial class VideosView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private static readonly string[] ScheduleOptions =
        ["Her Gün", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar"];

    private readonly string? _lockedTemplateId;
    private readonly List<LocalVideo> _videos = new();
    private string? _templateId;

    public VideosView(string? lockedTemplateId = null)
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
            _videos.Clear();
            VideosList.Items.Clear();
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

        var template = data.Templates.First(t => t.Id == _templateId);
        EmptyText.Visibility = Visibility.Collapsed;
        _videos.Clear();
        _videos.AddRange(template.Videos);
        Rebuild();
    }

    public void Reload() => Load();

    private void PersistSilently()
    {
        if (AppServices.Data is not { } repo || _templateId is null) return;
        repo.UpdateTemplateVideos(_templateId, _videos);
    }

    private void AddVideo_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        Directory.CreateDirectory(repo.VideosFolderPath);

        _videos.Add(new LocalVideo
        {
            Title = "",
            FileName = "",
            IsActive = true,
            ScheduleDay = "Her Gün",
        });
        PersistSilently();
        Rebuild();

        // Liste uzunsa yeni eklenen (boş) kart ekranın dışında kalabiliyordu — kullanıcı "+ Video Ekle"ye
        // basıp hiçbir tepki almadığını sanıyordu, oysa kart en alta ekleniyordu ama görünmüyordu. Layout
        // tamamlanana kadar (Loaded önceliği) beklenip yeni kart otomatik görünür alana kaydırılıyor.
        if (VideosList.Items.Count > 0 && VideosList.Items[^1] is FrameworkElement lastCard)
            Dispatcher.BeginInvoke(new Action(() => lastCard.BringIntoView()), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void Rebuild()
    {
        VideosList.Items.Clear();
        foreach (var video in _videos)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var checkbox = new CheckBox { IsChecked = video.IsActive, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), ToolTip = "Yayında" };
            checkbox.Checked += (_, _) => { video.IsActive = true; PersistSilently(); };
            checkbox.Unchecked += (_, _) => { video.IsActive = false; PersistSilently(); };
            row.Children.Add(checkbox);

            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = video.Title, Margin = new Thickness(0, 0, 0, 4) };
            titleBox.LostFocus += (_, _) => { video.Title = titleBox.Text; PersistSilently(); };
            infoStack.Children.Add(titleBox);
            infoStack.Children.Add(EditorControls.VideoFilePicker("", video.FileName,
                f =>
                {
                    video.FileName = f;
                    if (string.IsNullOrWhiteSpace(video.Title)) video.Title = Path.GetFileNameWithoutExtension(f);
                    PersistSilently();
                },
                () => AppServices.Data?.VideosFolderPath ?? ""));
            Grid.SetColumn(infoStack, 1);
            row.Children.Add(infoStack);

            var dayCombo = new ComboBox { ItemsSource = ScheduleOptions, SelectedItem = video.ScheduleDay, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
            dayCombo.SelectionChanged += (_, _) => { video.ScheduleDay = dayCombo.SelectedItem as string ?? "Her Gün"; PersistSilently(); };
            Grid.SetColumn(dayCombo, 2);
            row.Children.Add(dayCombo);

            var delete = EditorControls.DeleteIconButton(() => { _videos.Remove(video); PersistSilently(); Rebuild(); }, "bu videoyu");
            Grid.SetColumn(delete, 3);
            row.Children.Add(delete);

            VideosList.Items.Add(EditorControls.CardWith(row));
        }
    }

    private bool _folderPanelExpanded;

    private void ToggleFolderPanel_Click(object sender, RoutedEventArgs e)
    {
        _folderPanelExpanded = !_folderPanelExpanded;
        FolderPanel.Visibility = _folderPanelExpanded ? Visibility.Visible : Visibility.Collapsed;
        if (_folderPanelExpanded) RebuildFolderPanel();
    }

    /// <summary>Videolar klasöründeki TÜM dosyaları listeler (herhangi bir video kartında referans
    /// verilmemiş olanlar dahil) — kullanıcı uzak PC'ye Gezgin ile bağlanmadan artık kullanılmayan
    /// dosyaları buradan tamamen silebilsin diye. "Kullanılıyor" durumu TÜM şablonlar taranarak belirlenir
    /// (sadece şu an açık olan şablon değil) — bir dosya başka bir şablonun video listesinde kullanılıyor
    /// olabilir, silmeden önce kullanıcı bunu görsün.</summary>
    private void RebuildFolderPanel()
    {
        FolderFilesList.Children.Clear();
        if (AppServices.Data is not { } repo) return;

        var folder = repo.VideosFolderPath;
        Directory.CreateDirectory(folder);
        var files = Directory.GetFiles(folder)
            .Select(Path.GetFileName)
            .Where(f => !string.IsNullOrEmpty(f))
            .Cast<string>()
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var usedNames = new HashSet<string>(
            repo.Load().Templates.SelectMany(t => t.Videos).Select(v => v.FileName).Where(f => !string.IsNullOrWhiteSpace(f)),
            StringComparer.OrdinalIgnoreCase);

        FolderPanelHeader.Text = $"Videolar Klasöründeki Dosyalar ({files.Count})";

        if (files.Count == 0)
        {
            FolderFilesList.Children.Add(new TextBlock { Text = "Klasörde hiç dosya yok.", Style = EditorControls.Muted });
            return;
        }

        foreach (var file in files)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var isUsed = usedNames.Contains(file);

            var nameText = new TextBlock { Text = file, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White };
            row.Children.Add(nameText);

            var statusText = new TextBlock
            {
                Text = isUsed ? "Kullanılıyor" : "Kullanılmıyor",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0),
                Foreground = isUsed ? (Brush)Application.Current.Resources["SuccessBrush"] : (Brush)Application.Current.Resources["TextMutedBrush"],
            };
            Grid.SetColumn(statusText, 1);
            row.Children.Add(statusText);

            var deleteLabel = isUsed ? "bu dosyayı (en az bir video kartında kullanılıyor!)" : "bu dosyayı";
            var delete = EditorControls.DeleteIconButton(() =>
            {
                try { File.Delete(Path.Combine(folder, file)); }
                catch (IOException) { AppMessageBox.Show(Window.GetWindow(this), "Dosya silinemedi — başka bir programda açık olabilir.", "Silinemedi"); return; }
                catch (UnauthorizedAccessException) { AppMessageBox.Show(Window.GetWindow(this), "Dosya silinemedi — erişim izni yok.", "Silinemedi"); return; }
                RebuildFolderPanel();
            }, deleteLabel);
            Grid.SetColumn(delete, 2);
            row.Children.Add(delete);

            FolderFilesList.Children.Add(row);
        }
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
