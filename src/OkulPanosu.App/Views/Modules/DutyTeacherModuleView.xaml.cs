using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Günün nöbet listesini gösterir ve birkaç saniyede bir sıradaki kişiyi otomatik seçip
/// (listede görünür olacak şekilde kaydırıp) sağ panelde fotoğrafı/branşıyla öne çıkarır.</summary>
public partial class DutyTeacherModuleView : UserControl
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private const int DefaultSeconds = 6;

    private readonly DispatcherTimer _timer = new();
    private List<Row> _rows = [];
    private List<Personnel> _personnel = [];
    private int _index;

    public DutyTeacherModuleView(BoardModule module)
    {
        InitializeComponent();

        var seconds = module.Settings.TryGetValue("transitionSeconds", out var raw) && int.TryParse(raw, out var s) && s > 0
            ? s
            : DefaultSeconds;
        _timer.Interval = TimeSpan.FromSeconds(seconds);

        var layout = module.Settings.TryGetValue("dutyLocationLayout", out var layoutRaw) ? layoutRaw : "right";
        RosterList.ItemTemplate = (DataTemplate)FindResource(
            layout == "below" ? "RosterItemBelowTemplate" : "RosterItemRightTemplate");

        Load();

        _timer.Tick += (_, _) => Advance();
        _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Load()
    {
        var today = DateTime.Now.DayOfWeek;
        DayBadge.Text = Turkish.DateTimeFormat.GetDayName(today).ToUpper(Turkish);

        if (today is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            ShowEmpty("Hafta sonu tatili");
            return;
        }

        var data = AppServices.Data?.Load();
        _personnel = data?.Personnel ?? [];
        var day = data?.DutyRoster.FirstOrDefault(r => r.Day == today);

        _rows = [];
        if (day is not null && !string.IsNullOrWhiteSpace(day.Deputy))
            _rows.Add(new Row("Nöbetçi Müdür Yrd.", day.Deputy));
        if (day is not null)
            _rows.AddRange(day.Assignments.Select(a => new Row(a.Floor, a.TeacherName)));

        if (_rows.Count == 0)
        {
            ShowEmpty("Bugün için nöbetçi öğretmen tanımlanmamış");
            return;
        }

        ListPanel.Visibility = Visibility.Visible;
        PhotoPanel.Visibility = Visibility.Visible;
        EmptyText.Visibility = Visibility.Collapsed;

        RosterList.ItemsSource = _rows;
        _index = 0;
        ShowCurrent();
    }

    private void Advance()
    {
        if (_rows.Count == 0) return;
        _index = (_index + 1) % _rows.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var row = _rows[_index];
        RosterList.SelectedIndex = _index;
        RosterList.ScrollIntoView(row);

        RoleText.Text = row.Label.ToUpper(Turkish);
        CurrentNameText.Text = row.Name;

        var person = _personnel.FirstOrDefault(p => string.Equals(p.Name, row.Name, StringComparison.OrdinalIgnoreCase));
        CurrentBranchText.Text = person?.Branch ?? "";

        var photoPath = person is not null && !string.IsNullOrWhiteSpace(person.PhotoFileName) && AppServices.Data is { } repo
            ? Path.Combine(repo.TeacherPhotosFolderPath, person.PhotoFileName)
            : null;
        PhotoImage.Source = photoPath is not null && File.Exists(photoPath)
            ? new BitmapImage(new Uri(photoPath, UriKind.Absolute))
            : PersonPlaceholder.GetSilhouette(person?.Gender ?? "male");
    }

    private void ShowEmpty(string message)
    {
        ListPanel.Visibility = Visibility.Collapsed;
        PhotoPanel.Visibility = Visibility.Collapsed;
        EmptyText.Text = message;
        EmptyText.Visibility = Visibility.Visible;
    }

    private sealed record Row(string Label, string Name);
}
