using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Bugünün tarihiyle eşleşen TÜM kayıtları (birden fazla eklenmiş olabilir) sırayla gösterir —
/// önceden sadece İLK eşleşen kayıt gösteriliyordu, diğerleri sessizce kayboluyordu.</summary>
public partial class TodayInHistoryModuleView : UserControl
{
    private const int DefaultSeconds = 8;

    private readonly DispatcherTimer _timer = new();
    private List<TodayInHistoryEntry> _entries = [];
    private int _index;

    public TodayInHistoryModuleView(BoardModule module)
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
        var today = DateTime.Now;
        var all = AppServices.Data?.Load().TodayInHistory ?? [];

        _entries = all.Where(e => !e.IsGeneral && e.Day == today.Day && e.Month == today.Month).ToList();
        if (_entries.Count == 0)
            _entries = all.Where(e => e.IsGeneral).ToList();

        DateHeader.Text = $"📜 Tarihte Bugün — {today.Day} {TurkishCalendar.MonthNames[today.Month - 1]}";

        if (_entries.Count == 0 || _entries.All(e => e.Events.Count == 0))
        {
            EventsList.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Bugün için tanımlı bir kayıt yok";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        _index = 0;
        ShowCurrent();
    }

    private void Advance()
    {
        if (_entries.Count == 0) return;
        _index = (_index + 1) % _entries.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        EventsList.ItemsSource = _entries[_index].Events;
        EventsList.Visibility = Visibility.Visible;
    }
}
