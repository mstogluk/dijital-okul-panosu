using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Duyuruları belirli aralıklarla dönüşümlü gösterir (Kiosk'ta izleyici müdahale edemeyeceği için
/// otomatik döngü). Görsel yerleşimi ("imageLayout" ayarı — "top": üstte görsel/altta yazı, "side": solda
/// görsel/sağda yazı) modül ⚙️ ayarlarından seçilebildiği için içerik her Render()'da KOD ile kuruluyor
/// (sabit XAML yerine) — iki farklı yerleşimi tek bir statik XAML ile ifade etmek pratik değildi.</summary>
public partial class AnnouncementsModuleView : UserControl
{
    private static readonly Dictionary<string, Color> ImportanceColors = new()
    {
        ["critical"] = Color.FromRgb(0xF4, 0x3F, 0x5E),
        ["high"] = Color.FromRgb(0xD9, 0x77, 0x06),
        ["normal"] = Color.FromRgb(0x47, 0x55, 0x69),
    };

    private static readonly Dictionary<string, string> ImportanceLabels = new()
    {
        ["critical"] = "ACİL",
        ["high"] = "ÖNEMLİ",
        ["normal"] = "DUYURU",
    };

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private const int DefaultSeconds = 8;

    private readonly DispatcherTimer _timer = new();
    private readonly string _imageLayout;
    private List<Announcement> _announcements = [];
    private int _index;

    public AnnouncementsModuleView(BoardModule module, Template template)
    {
        InitializeComponent();

        var seconds = module.Settings.TryGetValue("transitionSeconds", out var raw) && int.TryParse(raw, out var s) && s > 0
            ? s
            : DefaultSeconds;
        _timer.Interval = TimeSpan.FromSeconds(seconds);

        _imageLayout = module.Settings.TryGetValue("imageLayout", out var layout) && layout == "side" ? "side" : "top";

        var today = DateTime.Today;
        _announcements = (AppServices.Data?.Load().Announcements ?? [])
            .Where(a => a.PublishedInTemplateIds.Contains(template.Id)
                        && a.StartDate.Date <= today && (a.EndDate is null || a.EndDate.Value.Date >= today))
            .ToList();
        Render();

        _timer.Tick += (_, _) => Advance();
        _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Advance()
    {
        if (_announcements.Count == 0) return;
        _index = (_index + 1) % _announcements.Count;
        Render();
    }

    private void Render()
    {
        ContentHost.Children.Clear();
        ContentHost.RowDefinitions.Clear();
        ContentHost.ColumnDefinitions.Clear();

        if (_announcements.Count == 0)
        {
            ContentHost.Visibility = Visibility.Collapsed;
            PageText.Text = "";
            EmptyText.Text = "Duyuru tanımlanmamış";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        var a = _announcements[_index];
        PageText.Text = $"{_index + 1} / {_announcements.Count}";

        var textGroup = BuildTextGroup(a);

        var imagePath = !string.IsNullOrWhiteSpace(a.ImageFileName) && AppServices.Data is { } repo
            ? Path.Combine(repo.ImagesFolderPath, a.ImageFileName)
            : null;
        var hasImage = imagePath is not null && File.Exists(imagePath);

        if (!hasImage)
        {
            ContentHost.Children.Add(textGroup);
        }
        else
        {
            var imageBorder = new Border
            {
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Background = (Brush)FindResource("BgElevatedBrush"),
                Child = new Image
                {
                    Source = new BitmapImage(new Uri(imagePath!, UriKind.Absolute)),
                    Stretch = Stretch.Uniform,
                },
            };

            if (_imageLayout == "side")
            {
                ContentHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
                ContentHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
                ContentHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetColumn(imageBorder, 0);
                ContentHost.Children.Add(imageBorder);

                textGroup.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(textGroup, 2);
                ContentHost.Children.Add(textGroup);
            }
            else
            {
                ContentHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                ContentHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                imageBorder.Margin = new Thickness(0, 0, 0, 8);
                Grid.SetRow(imageBorder, 0);
                ContentHost.Children.Add(imageBorder);

                Grid.SetRow(textGroup, 1);
                ContentHost.Children.Add(textGroup);
            }
        }

        ContentHost.Visibility = Visibility.Visible;
        EmptyText.Visibility = Visibility.Collapsed;
    }

    private StackPanel BuildTextGroup(Announcement a)
    {
        var group = new StackPanel();

        var badgeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };

        var importance = string.IsNullOrWhiteSpace(a.Importance) ? "normal" : a.Importance;
        if (ImportanceColors.TryGetValue(importance, out var color) && importance != "normal")
        {
            badgeRow.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(color),
                Child = new TextBlock { Text = ImportanceLabels[importance], FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Brushes.White },
            });
        }

        var dateText = a.EndDate is null
            ? a.StartDate.ToString("dd MMMM yyyy", Turkish)
            : $"{a.StartDate:dd MMMM yyyy} – {a.EndDate:dd MMMM yyyy}";
        badgeRow.Children.Add(new TextBlock { Text = dateText, Style = (Style)FindResource("MutedTextStyle"), VerticalAlignment = VerticalAlignment.Center });
        group.Children.Add(badgeRow);

        group.Children.Add(new TextBlock { Text = a.Title, FontSize = 16, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
        group.Children.Add(new TextBlock
        {
            Text = a.Content,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Style = (Style)FindResource("MutedTextStyle"),
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
        });

        return group;
    }
}
