using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Şu an hangi periyotta olduğumuzu (LessonSchedule'daki saat aralıklarına göre) hesaplayıp,
/// ders saatindeysek TÜM sınıfların o periyottaki dersini (Sınıf | Ders | Öğretmen) bir tabloda gösterir.
/// Teneffüsteysek son biten periyodun tablosu ALTTA kalır, üzerine yarı saydam "TENEFFÜS" biner; teneffüs
/// bitince otomatik olarak bir sonraki periyodun tablosuna geçer. Periyodik olarak (20 sn) yeniden hesaplar.</summary>
public partial class ScheduleModuleView : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(20) };

    public ScheduleModuleView(BoardModule module)
    {
        InitializeComponent();
        Refresh();

        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private sealed record ScheduleRow(string ClassName, string Subject, string Teacher);

    private void Refresh()
    {
        var now = DateTime.Now;
        var today = now.DayOfWeek;

        if (today is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            ShowEmpty("Hafta sonu tatili");
            return;
        }

        // NormalizeTimeOfDay: eski/olası bozuk kayıtlarda Start 24 saati aşmış (TimeSpan.Days > 0) olabilir
        // — bu, DateTime.Now.TimeOfDay (her zaman <24 saat) ile karşılaştırmayı sessizce hep başarısız
        // kılardı. Burada normalize etmek sadece GÖRÜNTÜLEME içindir, kaydedilen veriyi değiştirmez; asıl
        // kalıcı düzeltme ScheduleView'daki yazma noktalarında yapılıyor.
        var periods = (AppServices.Data?.Load().LessonSchedule ?? [])
            .Select(p => { p.Start = LessonPeriod.NormalizeTimeOfDay(p.Start); return p; })
            .OrderBy(p => p.Start)
            .ToList();

        if (periods.Count == 0)
        {
            ShowEmpty("Ders/Teneffüs çizelgesi tanımlanmamış");
            return;
        }

        var nowTime = now.TimeOfDay;

        var current = periods.FirstOrDefault(p => nowTime >= p.Start && nowTime <= p.End);
        if (current is not null)
        {
            ShowPeriod(today, current.Period, isBreak: false, nextLabel: null);
            return;
        }

        if (nowTime < periods[0].Start)
        {
            ShowEmpty("Bugünkü dersler henüz başlamadı");
            return;
        }

        if (nowTime > periods[^1].End)
        {
            ShowEmpty("Bugünkü dersler tamamlandı");
            return;
        }

        // Aradayız (iki periyot arası) — son biten periyodun tablosu üzerine "TENEFFÜS" biner.
        var finished = periods.LastOrDefault(p => p.End <= nowTime);
        if (finished is null)
        {
            ShowEmpty("Ders/Teneffüs çizelgesi tanımlanmamış");
            return;
        }

        var nextIndex = periods.IndexOf(finished) + 1;
        var next = nextIndex < periods.Count ? periods[nextIndex] : null;
        var nextLabel = next is not null ? $"{next.Period}. ders {next.Start:hh\\:mm}'de başlıyor" : null;
        ShowPeriod(today, finished.Period, isBreak: true, nextLabel);
    }

    private void ShowPeriod(DayOfWeek day, int periodNumber, bool isBreak, string? nextLabel)
    {
        HeaderText.Text = $"🔔 {periodNumber}. Ders";

        var classes = AppServices.Data?.Load().Classes ?? [];
        var rows = classes
            .Select(c => (Class: c, Entry: c.Lessons.FirstOrDefault(l => l.Day == day && l.Period == periodNumber)))
            .Where(r => r.Entry is not null)
            .OrderBy(r => r.Class.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new ScheduleRow(r.Class.Name, r.Entry!.Subject, r.Entry.Teacher))
            .ToList();

        if (rows.Count == 0)
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Bu periyot için tanımlı sınıf programı yok";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        ContentPanel.Visibility = Visibility.Visible;
        RowsList.ItemsSource = rows;

        if (isBreak)
        {
            BreakSubText.Text = nextLabel ?? "";
            BreakOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            BreakOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowEmpty(string message)
    {
        HeaderText.Text = "🔔 Ders ve Teneffüs Saatleri";
        ContentPanel.Visibility = Visibility.Collapsed;
        BreakOverlay.Visibility = Visibility.Collapsed;
        EmptyText.Text = message;
        EmptyText.Visibility = Visibility.Visible;
    }
}
