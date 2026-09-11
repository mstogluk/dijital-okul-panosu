using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

/// <summary>Gerçek bir nöbet çizelgesi gibi: satırlar kat/alan, sütunlar gün. Her hücrede birden
/// fazla öğretmen (çip + çarpı) olabilir, Personel listesinden seçilerek eklenir.</summary>
public partial class DutyRosterView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly DayOfWeek[] WorkDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private readonly List<DutyRosterDay> _roster = new();
    private List<Personnel> _personnel = new();

    public DutyRosterView()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        var data = repo.Load();

        _personnel = data.Personnel;
        _roster.Clear();
        _roster.AddRange(Enum.GetValues<DayOfWeek>().Select(d =>
            data.DutyRoster.FirstOrDefault(r => r.Day == d) ?? new DutyRosterDay { Day = d }));

        BuildMatrix();
    }

    /// <summary>Sayfa önbellekten yeniden gösterildiğinde çağrılır — Personel sayfasında az önce
    /// eklenen öğretmenler buradaki "+ Öğretmen" listesinde hemen görünsün diye.</summary>
    public void Reload() => Load();

    private DutyRosterDay DayRow(DayOfWeek day) => _roster.First(r => r.Day == day);

    /// <summary>false: mevcut/varsayılan görünüm (satır=nöbet yeri, sütun=gün). true: idareden gelen
    /// çizelgelerin çoğunun kullandığı görünüm (satır=gün, sütun=nöbet yeri) — kullanıcı elle giriş
    /// yaparken kaynak belgeyle aynı yönde olması çapraz okumayı/hata riskini azaltıyor. Sadece GÖRÜNÜMÜ
    /// değiştirir, veri (DutyRosterDay/Assignments) her iki yönde de aynı, ToggleOrientation_Click ile
    /// değiştirilip BuildMatrix() yeniden çağrılır.</summary>
    private bool _daysAsRows;

    private void ToggleOrientation_Click(object sender, RoutedEventArgs e)
    {
        _daysAsRows = !_daysAsRows;
        BuildMatrix();
    }

    private void BuildMatrix()
    {
        MatrixGrid.RowDefinitions.Clear();
        MatrixGrid.ColumnDefinitions.Clear();
        MatrixGrid.Children.Clear();

        if (_daysAsRows) BuildMatrixDaysAsRows(); else BuildMatrixFloorsAsRows();
    }

    private void BuildMatrixFloorsAsRows()
    {
        MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        foreach (var _ in WorkDays)
            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

        var rowCount = 2 + DutyRosterDay.DefaultFloors.Length;
        for (var i = 0; i < rowCount; i++)
            MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Gün başlıkları
        AddCell(0, 0, HeaderCell(""));
        for (var c = 0; c < WorkDays.Length; c++)
            AddCell(0, c + 1, HeaderCell(Turkish.DateTimeFormat.GetDayName(WorkDays[c]).ToUpper(Turkish)));

        // Müdür yardımcısı satırı
        AddCell(1, 0, HeaderCell("👑 NÖBETÇİ MD. YRD."));
        for (var c = 0; c < WorkDays.Length; c++)
        {
            var day = DayRow(WorkDays[c]);
            var combo = PersonnelCombo(day.Deputy, name => day.Deputy = name ?? "");
            AddCell(1, c + 1, Pad(combo));
        }

        // Kat satırları
        for (var f = 0; f < DutyRosterDay.DefaultFloors.Length; f++)
        {
            var floor = DutyRosterDay.DefaultFloors[f];
            AddCell(f + 2, 0, HeaderCell(floor.ToUpper(Turkish)));
            for (var c = 0; c < WorkDays.Length; c++)
            {
                var day = DayRow(WorkDays[c]);
                AddCell(f + 2, c + 1, Pad(BuildFloorCell(day, floor)));
            }
        }
    }

    private void BuildMatrixDaysAsRows()
    {
        MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        foreach (var _ in DutyRosterDay.DefaultFloors)
            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

        var rowCount = 1 + WorkDays.Length;
        for (var i = 0; i < rowCount; i++)
            MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Nöbet yeri başlıkları + Müdür Yrd
        AddCell(0, 0, HeaderCell(""));
        for (var f = 0; f < DutyRosterDay.DefaultFloors.Length; f++)
            AddCell(0, f + 1, HeaderCell(DutyRosterDay.DefaultFloors[f].ToUpper(Turkish)));
        AddCell(0, DutyRosterDay.DefaultFloors.Length + 1, HeaderCell("👑 NÖBETÇİ MD. YRD."));

        for (var d = 0; d < WorkDays.Length; d++)
        {
            var day = DayRow(WorkDays[d]);
            AddCell(d + 1, 0, HeaderCell(Turkish.DateTimeFormat.GetDayName(WorkDays[d]).ToUpper(Turkish)));
            for (var f = 0; f < DutyRosterDay.DefaultFloors.Length; f++)
            {
                var floor = DutyRosterDay.DefaultFloors[f];
                AddCell(d + 1, f + 1, Pad(BuildFloorCell(day, floor)));
            }
            var combo = PersonnelCombo(day.Deputy, name => day.Deputy = name ?? "");
            AddCell(d + 1, DutyRosterDay.DefaultFloors.Length + 1, Pad(combo));
        }
    }

    private UIElement BuildFloorCell(DutyRosterDay day, string floor)
    {
        var stack = new StackPanel();

        void Rebuild()
        {
            stack.Children.Clear();
            foreach (var assignment in day.Assignments.Where(a => a.Floor == floor).ToList())
            {
                var chip = new Border
                {
                    Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 4, 6, 4),
                    Margin = new Thickness(0, 0, 0, 4),
                };
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Children.Add(new TextBlock { Text = assignment.TeacherName, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
                var remove = new Button { Content = "x", Style = EditorControls.SecondaryButton, Width = 22, Height = 22, Padding = new Thickness(0), FontSize = 11, ToolTip = "Kaldır" };
                remove.Click += (_, _) => { day.Assignments.Remove(assignment); Rebuild(); };
                Grid.SetColumn(remove, 1);
                row.Children.Add(remove);
                chip.Child = row;
                stack.Children.Add(chip);
            }

            var available = _personnel.Where(p => p.Category == "teacher" && !IsDeputyTitle(p)).Select(p => p.Name)
                .Where(n => !day.Assignments.Any(a => a.Floor == floor && a.TeacherName == n))
                .OrderBy(n => n, StringComparer.Create(Turkish, false)).ToList();
            var addCombo = SearchableNameCombo(available, null, "+ Öğretmen ekle", name =>
            {
                if (name is null) return;
                day.Assignments.Add(new DutyAssignment { Floor = floor, TeacherName = name });
                Rebuild();
            });
            stack.Children.Add(addCombo);
        }

        Rebuild();
        return stack;
    }

    private ComboBox PersonnelCombo(string currentName, Action<string?> onChanged)
    {
        // Müdür Yardımcısı sütunu: Kategori DEĞİL Görev metni "müdür" içerenlerle sınırlı (bkz. IsDeputyTitle) -
        // bir öğretmen Kategorisi "Öğretmen" kalırken fiilen müdür yrd. görevi üstlenebiliyor (Görev alanına
        // "Müdür Yardımcısı" yazılması yeterli). Bu sayede normal branş öğretmenleri bu listede görünmüyor.
        var names = _personnel.Where(IsDeputyTitle).Select(p => p.Name).OrderBy(n => n, StringComparer.Create(Turkish, false)).ToList();
        if (!string.IsNullOrWhiteSpace(currentName) && !names.Contains(currentName)) names.Insert(0, currentName);

        return SearchableNameCombo(names, currentName, "— Seçiniz —", onChanged);
    }

    private static bool IsDeputyTitle(Personnel p) => EditorControls.NormalizeForMatch(p.Title).Contains("mudur");

    /// <summary>Yazarken filtrelenen (aranabilir) isim seçici — hem Müdür Yardımcısı hem "+ Öğretmen"
    /// listesi için ortak: kullanıcı öğretmen sayısı arttıkça (bu okulda 40+) sıradan bir ComboBox'ta
    /// isim bulmak zorlaşıyordu. <paramref name="initialValue"/> null/boşsa yer tutucu gösterilir; bir
    /// öğe seçildiğinde <paramref name="onSelected"/> çağrılır (yer tutucu seçilirse null geçilir) ve
    /// filtre sıfırlanıp liste tam hâline döner.</summary>
    private static ComboBox SearchableNameCombo(List<string> sortedNames, string? initialValue, string placeholder, Action<string?> onSelected)
    {
        List<string> FullList() => new List<string> { placeholder }.Concat(sortedNames).ToList();

        var combo = new ComboBox
        {
            IsEditable = true,
            IsTextSearchEnabled = false,
            StaysOpenOnEdit = true,
            ItemsSource = FullList(),
            SelectedItem = string.IsNullOrWhiteSpace(initialValue) ? placeholder : initialValue,
        };

        var suppress = false;

        combo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) =>
        {
            if (suppress) return;
            var norm = EditorControls.NormalizeForMatch(combo.Text);
            combo.ItemsSource = string.IsNullOrEmpty(norm)
                ? FullList()
                : sortedNames.Where(n => EditorControls.NormalizeForMatch(n).Contains(norm)).ToList();
            combo.IsDropDownOpen = true;
        }));

        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is not string selected || suppress) return;
            suppress = true;
            onSelected(selected == placeholder ? null : selected);
            combo.ItemsSource = FullList();
            combo.IsDropDownOpen = false;
            suppress = false;
        };

        return combo;
    }

    private static Border HeaderCell(string text) => new()
    {
        Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x5F)),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(10, 10, 10, 10),
        Margin = new Thickness(2),
        Child = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
        },
    };

    private static Border Pad(UIElement content) => new() { Padding = new Thickness(6), Child = content };

    private void AddCell(int row, int col, UIElement element)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, col);
        MatrixGrid.Children.Add(element);
    }

    private static readonly Dictionary<string, DayOfWeek> DayNameMap = new()
    {
        ["pazartesi"] = DayOfWeek.Monday, ["pzt"] = DayOfWeek.Monday, ["pt"] = DayOfWeek.Monday,
        ["sali"] = DayOfWeek.Tuesday, ["sal"] = DayOfWeek.Tuesday,
        ["carsamba"] = DayOfWeek.Wednesday, ["car"] = DayOfWeek.Wednesday, ["crs"] = DayOfWeek.Wednesday,
        ["persembe"] = DayOfWeek.Thursday, ["per"] = DayOfWeek.Thursday,
        ["cuma"] = DayOfWeek.Friday, ["cum"] = DayOfWeek.Friday,
    };

    /// <summary>İdareden gelen çizelgenin, panomuzunkinin AKSİNE (satır=kat, sütun=gün) genelde satır=gün,
    /// sütun=nöbet yeri biçiminde geldiğini gördük (bkz. gerçek okul PDF'i) — bu yüzden yapıştırma ayrı bir
    /// yönde ayrıştırılıyor: ilk satır başlık (nöbet yeri adları + "Nöbetçi Müdür Yrd."), sonraki her satırın
    /// ilk hücresi gün adı. Bir hücrede birden fazla isim (Alt+Enter ile) ayrı satırlarda olabilir. Kısaltılmış
    /// isimler ("E.BAKIR" gibi) Personel listesiyle (baş harf + soyad eşleşmesi, ClassSchedulesView'daki
    /// ResolveTeacher ile aynı sezgisel yöntem) eşleştirilmeye çalışılır; birden fazla ya da hiç aday
    /// eşleşmezse yazdığınız gibi bırakılır (yanlış eşleştirmektense) — arama kutulu seçicilerden elle
    /// düzeltilebilir.</summary>
    private void ApplyPaste_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PasteBox.Text)) return;

        var rows = EditorControls.ParseTable(PasteBox.Text);
        if (rows.Count < 2)
        {
            ShowStatus("Yapıştırılan veri tanınamadı — en az bir başlık satırı ve bir gün satırı olmalı.");
            return;
        }

        var header = rows[0];
        var floorColumns = new Dictionary<int, string>();
        int? deputyColumn = null;
        for (var c = 1; c < header.Count; c++)
        {
            var text = header[c];
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (IsDeputyHeader(text)) { deputyColumn = c; continue; }
            if (MatchFloorHeader(text) is { } floor) floorColumns[c] = floor;
        }

        if (floorColumns.Count == 0 && deputyColumn is null)
        {
            ShowStatus("Sütun başlıkları tanınamadı — başlık satırında \"Bahçe\", \"Zemin Kat\", \"1. Kat\" gibi " +
                       "nöbet yeri adları ya da \"Nöbetçi Müdür Yrd.\" olmalı.");
            return;
        }

        var resolvedCount = 0;
        var unresolvedCount = 0;
        var dayCount = 0;

        for (var r = 1; r < rows.Count; r++)
        {
            var cells = rows[r];
            if (cells.Count == 0 || MatchDayName(cells[0]) is not { } dow) continue;
            dayCount++;
            var day = DayRow(dow);

            if (deputyColumn is { } dc && dc < cells.Count)
            {
                var raw = cells[dc].Split('\n')[0].Trim();
                if (raw.Length > 0)
                {
                    var deputyCandidates = _personnel.Where(IsDeputyTitle).ToList();
                    var (name, resolved) = ResolvePersonName(raw, deputyCandidates);
                    day.Deputy = name;
                    if (resolved) resolvedCount++; else unresolvedCount++;
                }
            }

            foreach (var (col, floor) in floorColumns)
            {
                if (col >= cells.Count) continue;
                var names = cells[col].Replace("\r\n", "\n").Split('\n')
                    .SelectMany(l => l.Split([',', ';']))
                    .Select(n => n.Trim())
                    .Where(n => n.Length > 0)
                    .ToList();
                if (names.Count == 0) continue;

                day.Assignments.RemoveAll(a => a.Floor == floor);
                foreach (var raw in names)
                {
                    var (name, resolved) = ResolvePersonName(raw);
                    day.Assignments.Add(new DutyAssignment { Floor = floor, TeacherName = name });
                    if (resolved) resolvedCount++; else unresolvedCount++;
                }
            }
        }

        BuildMatrix();
        PasteBox.Text = "";

        var message = $"{dayCount} gün işlendi.";
        if (resolvedCount > 0) message += $" {resolvedCount} isim Personel listesiyle eşleştirildi.";
        if (unresolvedCount > 0) message += $" {unresolvedCount} isim eşleştirilemedi, yazdığınız gibi kaydedildi — arama kutusundan elle düzeltebilirsiniz.";
        message += " Kontrol edip \"Kaydet\"e basmayı unutmayın.";
        ShowStatus(message);
    }

    private void ShowStatus(string text)
    {
        StatusText.Text = text;
        StatusText.Visibility = Visibility.Visible;
    }

    private static bool IsDeputyHeader(string header) => EditorControls.NormalizeForMatch(header).Contains("mudur");

    private static string? MatchFloorHeader(string header)
    {
        var digitMatch = Regex.Match(header, @"\d+");
        if (digitMatch.Success)
        {
            var candidate = $"{digitMatch.Value}. Kat";
            if (DutyRosterDay.DefaultFloors.Contains(candidate)) return candidate;
        }
        var norm = EditorControls.NormalizeForMatch(header);
        if (norm.Contains("bahce") || norm.Contains("disalan")) return DutyRosterDay.DefaultFloors.FirstOrDefault(f => f.Contains("Bahçe"));
        if (norm.Contains("zemin")) return DutyRosterDay.DefaultFloors.FirstOrDefault(f => f == "Zemin Kat");
        return null;
    }

    private static DayOfWeek? MatchDayName(string text) =>
        DayNameMap.TryGetValue(EditorControls.NormalizeForMatch(text), out var d) ? d : null;

    private (string Name, bool Resolved) ResolvePersonName(string raw, List<Personnel>? candidates = null)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (raw, false);
        return ResolvePersonnelAbbreviation(raw, candidates ?? _personnel) is { } resolved ? (resolved, true) : (raw, false);
    }

    /// <summary>"E.BAKIR" gibi kısaltılmış bir adı Personel listesindeki TAM adla eşleştirmeye çalışır — baş
    /// harf + soyadın (sesli harfleri atılmış hâliyle de) örtüşmesi aranır. ClassSchedulesView.ResolveTeacher
    /// ile aynı sezgisel yöntem (kod tekrarını önlemek için ortak bir yere taşımaya değecek kadar büyümedi,
    /// ikisi de küçük ve bağımsız).</summary>
    private static string? ResolvePersonnelAbbreviation(string abbreviation, List<Personnel> personnel)
    {
        var normAbbrev = EditorControls.NormalizeForMatch(abbreviation);
        if (normAbbrev.Length < 2) return null;

        var initial = normAbbrev[0];
        var rest = normAbbrev[1..];
        if (rest.Length == 0) return null;
        var restStripped = EditorControls.StripVowels(rest);

        string? bestMatch = null;
        var matchCount = 0;

        foreach (var p in personnel)
        {
            var parts = p.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var firstNorm = EditorControls.NormalizeForMatch(parts[0]);
            var lastNorm = EditorControls.NormalizeForMatch(parts[^1]);
            if (firstNorm.Length == 0 || firstNorm[0] != initial) continue;

            var lastStripped = EditorControls.StripVowels(lastNorm);
            var isMatch = lastNorm == rest || lastStripped == restStripped
                          || (rest.Length >= 3 && lastNorm.StartsWith(rest, StringComparison.Ordinal));
            if (!isMatch) continue;

            matchCount++;
            bestMatch = p.Name;
        }

        return matchCount == 1 ? bestMatch : null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.DutyRoster = _roster);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
