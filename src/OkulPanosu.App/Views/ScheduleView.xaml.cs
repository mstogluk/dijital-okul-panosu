using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class ScheduleView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private const int MinPeriods = 10;

    private readonly List<LessonPeriod> _periods = new();

    public ScheduleView()
    {
        InitializeComponent();
        BuildGeneratorForm();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        var data = repo.Load();

        _periods.Clear();
        _periods.AddRange(data.LessonSchedule.Count > 0
            ? data.LessonSchedule.OrderBy(p => p.Period)
            : Generate(new TimeSpan(8, 30, 0), 40, 10, MinPeriods));

        Rebuild();
    }

    public void Reload() => Load();

    private static List<LessonPeriod> Generate(TimeSpan firstStart, int durationMinutes, int breakMinutes, int count)
    {
        var periods = new List<LessonPeriod>();
        var start = firstStart;
        for (var i = 0; i < count; i++)
        {
            var period = new LessonPeriod
            {
                Period = i + 1,
                Subject = $"{i + 1}. Ders",
                Start = start,
                DurationMinutes = durationMinutes,
                BreakAfterMinutes = breakMinutes,
            };
            periods.Add(period);
            start = LessonPeriod.NormalizeTimeOfDay(period.End.Add(TimeSpan.FromMinutes(breakMinutes)));
        }
        return periods;
    }

    /// <summary>Bir periyodun süresi/teneffüsü değiştiğinde, ONDAN SONRAKİ tüm periyotların başlangıç
    /// saatini zincirleme yeniden hesaplar — ilk periyodun Start'ı sabit kalır (tek anchor), gerisi
    /// her periyodun kendi Duration/BreakAfter değerlerinden türetilir. Her toplama sonrası
    /// NormalizeTimeOfDay ile 24 saate sarılır — aksi hâlde gece yarısını geçen bir zincir TimeSpan.Days'i
    /// büyütüp pano tarafındaki "şu an bu periyotta mıyız" karşılaştırmasını sessizce bozuyordu.</summary>
    private void RecalculateStartTimes()
    {
        for (var i = 1; i < _periods.Count; i++)
        {
            var previous = _periods[i - 1];
            _periods[i].Start = LessonPeriod.NormalizeTimeOfDay(previous.End.Add(TimeSpan.FromMinutes(previous.BreakAfterMinutes)));
        }
    }

    /// <summary>Enter/odak kaybıyla yapılan her düzenleme (saat, ders adı, ekleme, silme, otomatik oluşturma)
    /// ANINDA paylaşılan veriye yazılır — kullanıcı sayfadan "Değişiklikleri Kaydet"e basmayı unutup
    /// çıkarsa (Sınıf Ders Programı gibi başka sayfalar periyotları okuyamıyordu) değişiklik kaybolmasın diye.
    /// Alttaki "Değişiklikleri Kaydet" butonu (minimum periyot doğrulamasıyla) hâlâ duruyor ama artık
    /// zorunlu değil, sadece ek bir güvence.</summary>
    private void PersistSilently()
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.LessonSchedule = _periods);
    }

    private void BuildGeneratorForm()
    {
        var startBox = new TextBox { Style = EditorControls.TextBoxStyle, Width = 90, Margin = new Thickness(0, 0, 8, 0), Text = "08:30", ToolTip = "İlk Ders Başlangıç Saati (sa:dk)" };
        var durationBox = new TextBox { Style = EditorControls.TextBoxStyle, Width = 70, Margin = new Thickness(0, 0, 8, 0), Text = "40", ToolTip = "Ders Süresi (dakika)" };
        var breakBox = new TextBox { Style = EditorControls.TextBoxStyle, Width = 70, Margin = new Thickness(0, 0, 8, 0), Text = "10", ToolTip = "Teneffüs Süresi (dakika)" };
        var countBox = new TextBox { Style = EditorControls.TextBoxStyle, Width = 60, Margin = new Thickness(0, 0, 8, 0), Text = MinPeriods.ToString(), ToolTip = "Periyot Sayısı" };

        GeneratorForm.Children.Add(LabeledInline("Başlangıç", startBox));
        GeneratorForm.Children.Add(LabeledInline("Ders (dk)", durationBox));
        GeneratorForm.Children.Add(LabeledInline("Teneffüs (dk)", breakBox));
        GeneratorForm.Children.Add(LabeledInline("Periyot Sayısı", countBox));

        var generateButton = new Button { Content = "Oluştur", Style = EditorControls.PrimaryButton, VerticalAlignment = VerticalAlignment.Bottom, Height = 34 };
        generateButton.Click += (_, _) =>
        {
            if (!TimeSpan.TryParse(startBox.Text, out var start)) start = new TimeSpan(8, 30, 0);
            if (!int.TryParse(durationBox.Text, out var duration) || duration <= 0) duration = 40;
            if (!int.TryParse(breakBox.Text, out var breakMinutes) || breakMinutes < 0) breakMinutes = 10;
            if (!int.TryParse(countBox.Text, out var count) || count < MinPeriods) count = MinPeriods;

            _periods.Clear();
            _periods.AddRange(Generate(start, duration, breakMinutes, count));
            PersistSilently();
            Rebuild();
        };
        GeneratorForm.Children.Add(generateButton);
    }

    private static StackPanel LabeledInline(string label, UIElement input)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        stack.Children.Add(new TextBlock { Text = label, Style = EditorControls.Muted, Margin = new Thickness(0, 0, 0, 4) });
        stack.Children.Add(input);
        return stack;
    }

    private void Rebuild()
    {
        PeriodsList.Items.Clear();
        foreach (var period in _periods)
        {
            var panel = new StackPanel();

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Children.Add(new TextBlock
            {
                Text = $"{period.Period}. PERİYOD",
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["SuccessBrush"],
                VerticalAlignment = VerticalAlignment.Center,
            });
            var delete = EditorControls.DeleteIconButton(() => { _periods.Remove(period); RecalculateStartTimes(); PersistSilently(); Rebuild(); });
            Grid.SetColumn(delete, 1);
            header.Children.Add(delete);
            panel.Children.Add(header);

            var row1 = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });

            var subjectStack = new StackPanel();
            subjectStack.Children.Add(new TextBlock { Text = "Ders Adı", Style = EditorControls.Muted, Margin = new Thickness(0, 0, 0, 4) });
            var subjectBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = period.Subject };
            subjectBox.LostFocus += (_, _) => { period.Subject = subjectBox.Text; PersistSilently(); };
            subjectStack.Children.Add(subjectBox);
            row1.Children.Add(subjectStack);

            var startStack = new StackPanel();
            startStack.Children.Add(new TextBlock { Text = "Başlangıç", Style = EditorControls.Muted, Margin = new Thickness(0, 0, 0, 4) });
            var startBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = period.Start.ToString(@"hh\:mm"), FontFamily = new System.Windows.Media.FontFamily("Consolas") };
            startStack.Children.Add(startBox);
            Grid.SetColumn(startStack, 2);
            row1.Children.Add(startStack);

            var endStack = new StackPanel();
            endStack.Children.Add(new TextBlock { Text = "Bitiş", Style = EditorControls.Muted, Margin = new Thickness(0, 0, 0, 4) });
            var endBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = period.End.ToString(@"hh\:mm"), FontFamily = new System.Windows.Media.FontFamily("Consolas") };
            endStack.Children.Add(endBox);
            Grid.SetColumn(endStack, 4);
            row1.Children.Add(endStack);

            void CommitStart()
            {
                if (!TimeSpan.TryParse(startBox.Text, out var newStart))
                {
                    startBox.Text = period.Start.ToString(@"hh\:mm");
                    return;
                }

                var index = _periods.IndexOf(period);
                if (index > 0)
                {
                    var previous = _periods[index - 1];
                    var gapMinutes = (int)Math.Round((newStart - previous.End).TotalMinutes);
                    previous.BreakAfterMinutes = Math.Max(0, gapMinutes);
                }

                period.Start = LessonPeriod.NormalizeTimeOfDay(newStart);
                RecalculateStartTimes();
                PersistSilently();
                Rebuild();
            }

            void CommitEnd()
            {
                if (!TimeSpan.TryParse(endBox.Text, out var newEnd))
                {
                    endBox.Text = period.End.ToString(@"hh\:mm");
                    return;
                }

                var durationMinutes = (int)Math.Round((newEnd - period.Start).TotalMinutes);
                if (durationMinutes <= 0)
                {
                    endBox.Text = period.End.ToString(@"hh\:mm");
                    return;
                }

                period.DurationMinutes = durationMinutes;
                RecalculateStartTimes();
                PersistSilently();
                Rebuild();
            }

            startBox.LostFocus += (_, _) => CommitStart();
            startBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) CommitStart(); };
            endBox.LostFocus += (_, _) => CommitEnd();
            endBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) CommitEnd(); };

            panel.Children.Add(row1);

            PeriodsList.Items.Add(EditorControls.CardWith(panel, new Thickness(0, 0, 10, 10)));
        }
    }

    private void AddPeriod_Click(object sender, RoutedEventArgs e)
    {
        var last = _periods.OrderBy(p => p.Period).LastOrDefault();
        var start = last is not null ? LessonPeriod.NormalizeTimeOfDay(last.End.Add(TimeSpan.FromMinutes(last.BreakAfterMinutes))) : new TimeSpan(8, 30, 0);

        _periods.Add(new LessonPeriod
        {
            Period = _periods.Count + 1,
            Subject = $"{_periods.Count + 1}. Ders",
            Start = start,
            DurationMinutes = last?.DurationMinutes ?? 40,
            BreakAfterMinutes = last?.BreakAfterMinutes ?? 10,
        });
        PersistSilently();
        Rebuild();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;

        if (_periods.Count < MinPeriods)
        {
            StatusText.Foreground = (System.Windows.Media.Brush)Application.Current.Resources["DangerBrush"];
            StatusText.Text = $"En az {MinPeriods} periyot olmalı.";
            StatusText.Visibility = Visibility.Visible;
            return;
        }

        repo.UpdateContent(data => data.LessonSchedule = _periods);
        StatusText.Foreground = (System.Windows.Media.Brush)Application.Current.Resources["SuccessBrush"];
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
