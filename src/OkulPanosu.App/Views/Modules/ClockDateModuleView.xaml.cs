using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

public partial class ClockDateModuleView : UserControl
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    // API anahtarı gerektirmeyen basit metin tabanlı hava durumu servisi.
    private static readonly HttpClient WeatherClient = new() { Timeout = TimeSpan.FromSeconds(8) };

    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _weatherTimer = new() { Interval = TimeSpan.FromMinutes(30) };

    public ClockDateModuleView(BoardModule module)
    {
        InitializeComponent();

        Tick();
        _clockTimer.Tick += (_, _) => Tick();
        _clockTimer.Start();

        _ = LoadWeatherAsync();
        _weatherTimer.Tick += async (_, _) => await LoadWeatherAsync();
        _weatherTimer.Start();

        Unloaded += (_, _) =>
        {
            _clockTimer.Stop();
            _weatherTimer.Stop();
        };
    }

    private void Tick()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("HH:mm:ss", Turkish);
        DateText.Text = now.ToString("dd MMMM yyyy, dddd", Turkish);
    }

    private async Task LoadWeatherAsync()
    {
        var city = AppServices.Data?.Load().InstitutionCity;
        if (string.IsNullOrWhiteSpace(city)) return;

        try
        {
            var text = await WeatherClient.GetStringAsync($"https://wttr.in/{Uri.EscapeDataString(city)}?format=%C+%t&lang=tr");
            text = text.Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Contains("Unknown location")) return;

            WeatherIcon.Text = IconFor(text);
            WeatherText.Text = $"{city}: {text}";
            WeatherPanel.Visibility = Visibility.Visible;
        }
        catch
        {
            // İnternet yoksa veya servis yanıt vermezse hava durumu satırı sessizce gizli kalır.
        }
    }

    /// <summary>wttr.in'in Türkçe metin açıklamasındaki anahtar kelimelere göre kaba bir emoji eşlemesi
    /// — ikon fontu yerine düz emoji kullanıyoruz (bkz. EditorControls'teki not: ikon fontları bu
    /// ortamda güvenilir render olmuyordu).</summary>
    private static string IconFor(string conditionText)
    {
        var t = conditionText.ToLowerInvariant();
        if (t.Contains("fırtına") || t.Contains("gökgürültü")) return "⛈️";
        if (t.Contains("kar") || t.Contains("dolu")) return "❄️";
        if (t.Contains("sis") || t.Contains("pus")) return "🌫️";
        if (t.Contains("yağmur") || t.Contains("sağanak") || t.Contains("çise")) return "🌧️";
        if (t.Contains("az bulutlu") || t.Contains("parçalı")) return "⛅";
        if (t.Contains("bulut") || t.Contains("kapalı")) return "☁️";
        if (t.Contains("açık") || t.Contains("güneş")) return "☀️";
        return "🌡️";
    }
}
