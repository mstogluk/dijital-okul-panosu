using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Bugüne planlanmış (ScheduleDay == "Her Gün" veya bugünün adı) ve etkin videoları sırayla
/// oynatır — biri bitince otomatik bir sonrakine geçer, hepsi bitince baştan başlar. Ses/sessiz ve
/// ses düzeyi, modülün ⚙️ ayarlarında (ModuleSettingsDialog) belirlenir.</summary>
public partial class VideoModuleView : UserControl
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private List<LocalVideo> _queue = [];
    private int _index;

    public VideoModuleView(BoardModule module, Template template)
    {
        InitializeComponent();

        var isMuted = !module.Settings.TryGetValue("muted", out var mutedRaw) || mutedRaw != "false";
        var volume = module.Settings.TryGetValue("volume", out var volumeRaw) && int.TryParse(volumeRaw, out var v) ? v : 50;
        Player.IsMuted = isMuted;
        Player.Volume = Math.Clamp(volume / 100.0, 0, 1);

        Load(template);
        Unloaded += (_, _) => Player.Close();
    }

    private void Load(Template template)
    {
        if (AppServices.Data is not { } repo) return;

        var todayName = Turkish.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek);
        _queue = template.Videos
            .Where(v => v.IsActive && (v.ScheduleDay == "Her Gün" || v.ScheduleDay == todayName)
                        && !string.IsNullOrWhiteSpace(v.FileName)
                        && System.IO.File.Exists(System.IO.Path.Combine(repo.VideosFolderPath, v.FileName)))
            .ToList();

        if (_queue.Count == 0)
        {
            EmptyText.Text = "Bugün için planlanmış video yok";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        _index = 0;
        PlayCurrent();
    }

    private void PlayCurrent()
    {
        if (AppServices.Data is not { } repo) return;
        Player.Source = new Uri(System.IO.Path.Combine(repo.VideosFolderPath, _queue[_index].FileName), UriKind.Absolute);
        Player.Play();
    }

    private void Player_MediaEnded(object sender, RoutedEventArgs e)
    {
        if (_queue.Count == 0) return;
        _index = (_index + 1) % _queue.Count;
        PlayCurrent();
    }
}
