using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Doğum günü aralığı ("birthdayFilter" ayarı — bugün/bu hafta/bu ay) modül ⚙️ ayarlarından
/// seçilebilir, varsayılan "bu hafta". Birden fazla kişi aralığa düşerse sırayla, tek tek büyük gösterilir
/// (bkz. Nöbetçi Öğretmen/Ayın Öğrencisi ile aynı döngü deseni).</summary>
public partial class BirthdaysModuleView : UserControl
{
    private const int DefaultSeconds = 6;

    private readonly DispatcherTimer _timer = new();
    private List<Student> _students = [];
    private int _index;

    public BirthdaysModuleView(BoardModule module)
    {
        InitializeComponent();

        var seconds = module.Settings.TryGetValue("transitionSeconds", out var raw) && int.TryParse(raw, out var s) && s > 0
            ? s
            : DefaultSeconds;
        _timer.Interval = TimeSpan.FromSeconds(seconds);

        var filter = module.Settings.TryGetValue("birthdayFilter", out var f) && f is "today" or "week" or "month" ? f : "week";
        Load(filter);

        _timer.Tick += (_, _) => Advance();
        _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private static bool FallsWithin(int month, int day, DateTime rangeStart, DateTime rangeEnd)
    {
        for (var year = rangeStart.Year; year <= rangeEnd.Year; year++)
        {
            if (month == 2 && day == 29 && !DateTime.IsLeapYear(year)) continue;
            var candidate = new DateTime(year, month, day);
            if (candidate >= rangeStart.Date && candidate <= rangeEnd.Date) return true;
        }
        return false;
    }

    private void Load(string filter)
    {
        var today = DateTime.Today;
        DateTime rangeStart, rangeEnd;
        switch (filter)
        {
            case "today":
                rangeStart = rangeEnd = today;
                break;
            case "month":
                rangeStart = new DateTime(today.Year, today.Month, 1);
                rangeEnd = rangeStart.AddMonths(1).AddDays(-1);
                break;
            default: // "week" — Pazartesi başlangıçlı
                var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                rangeStart = today.AddDays(-diff);
                rangeEnd = rangeStart.AddDays(6);
                break;
        }

        HeaderTextBlock.Text = filter switch
        {
            "today" => "🎂 Bugün Doğanlar",
            "month" => "🎂 Bu Ay Doğanlar",
            _ => "🎂 Bu Hafta Doğanlar",
        };

        _students = (AppServices.Data?.Load().Students ?? [])
            .Where(s => s.BirthDay is not null && s.BirthMonth is not null && FallsWithin(s.BirthMonth.Value, s.BirthDay.Value, rangeStart, rangeEnd))
            .OrderBy(s => s.BirthMonth * 100 + s.BirthDay)
            .ToList();

        if (_students.Count == 0)
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Bu aralıkta doğum günü tanımlanmamış";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        ContentPanel.Visibility = Visibility.Visible;
        EmptyText.Visibility = Visibility.Collapsed;
        _index = 0;
        ShowCurrent();
    }

    private void Advance()
    {
        if (_students.Count == 0) return;
        _index = (_index + 1) % _students.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var student = _students[_index];
        NameText.Text = student.Name;
        ClassText.Text = student.Class;
        DateText.Text = $"{student.BirthDay} {TurkishCalendar.MonthNames[student.BirthMonth!.Value - 1]}";
    }
}
