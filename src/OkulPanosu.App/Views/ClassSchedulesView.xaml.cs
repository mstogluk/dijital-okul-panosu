using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

/// <summary>Her sınıfın haftalık ders programını (gün × periyot → ders + opsiyonel öğretmen) düzenler.
/// Zaman aralıkları burada tekrar tutulmaz — "Ders & Zil Saatleri" (LessonSchedule) sayfasındaki periyot
/// tanımlarına referans verilir (Nöbet Çizelgesi'ndeki matris deseniyle aynı yaklaşım).</summary>
public partial class ClassSchedulesView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly DayOfWeek[] WorkDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private readonly List<SchoolClass> _classes = new();
    private List<LessonPeriod> _periods = new();
    private SchoolClass? _selected;

    public ClassSchedulesView()
    {
        InitializeComponent();
        BuildNewClassForm();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        var data = repo.Load();

        _classes.Clear();
        _classes.AddRange(data.Classes);
        _periods = data.LessonSchedule.OrderBy(p => p.Period).ToList();

        if (_selected is not null) _selected = _classes.FirstOrDefault(c => c.Id == _selected.Id);

        RebuildChips();
        RebuildMatrix();
    }

    public void Reload() => Load();

    /// <summary>Sınıf ekleme/silme, hücre düzenleme ve Excel yapıştırma gibi her mutasyondan sonra ANINDA
    /// paylaşılan veriye yazar — kullanıcı "Kaydet"e basmayı unutup sayfadan çıkarsa değişiklik kaybolmasın
    /// diye (bkz. Ders &amp; Zil Saatleri sayfasındaki aynı düzeltme).</summary>
    private void PersistSilently()
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.Classes = _classes);
    }

    /// <summary>Sınıf adı iki ayrı alandan ("Seviye" + "Şube/Alan") "Seviye-Şube" biçiminde otomatik
    /// birleştirilir — böylece "9-A" gibi basit şubeler de, okuldaki gerçek alan adları (ör. "9-KSH",
    /// "9-Büro") da AYNI sabit ayırıcıyla tutarlı bir biçimde yazılır, serbest metinle karışıklık olmaz.</summary>
    private void BuildNewClassForm()
    {
        var gradeBox = new TextBox { Style = EditorControls.TextBoxStyle, Width = 60, Margin = new Thickness(0, 0, 4, 0), ToolTip = "Seviye (ör. 9)" };
        var separator = LabeledInline(" ", new TextBlock
        {
            Text = "-",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0),
        });
        var branchBox = new TextBox { Style = EditorControls.TextBoxStyle, Width = 220, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Şube / Alan (ör. A, KSH, Büro)" };
        var addButton = new Button { Content = "+ Sınıf Ekle", Style = EditorControls.SecondaryButton };
        addButton.Click += (_, _) =>
        {
            var grade = gradeBox.Text.Trim();
            var branch = branchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(grade) || string.IsNullOrWhiteSpace(branch)) return;

            var schoolClass = new SchoolClass { Name = $"{grade}-{branch}" };
            _classes.Add(schoolClass);
            _selected = schoolClass;
            gradeBox.Text = "";
            branchBox.Text = "";
            PersistSilently();
            RebuildChips();
            RebuildMatrix();
        };

        NewClassForm.Children.Add(LabeledInline("Seviye", gradeBox));
        NewClassForm.Children.Add(separator);
        NewClassForm.Children.Add(LabeledInline("Şube / Alan", branchBox));
        NewClassForm.Children.Add(addButton);
    }

    private static StackPanel LabeledInline(string label, UIElement input)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Bottom };
        stack.Children.Add(new TextBlock { Text = label, Style = EditorControls.Muted, Margin = new Thickness(0, 0, 0, 4) });
        stack.Children.Add(input);
        return stack;
    }

    private void RebuildChips()
    {
        ClassChips.Items.Clear();
        foreach (var schoolClass in _classes.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            var isSelected = ReferenceEquals(schoolClass, _selected);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var selectButton = new Button
            {
                Content = schoolClass.Name,
                Style = isSelected ? EditorControls.PrimaryButton : EditorControls.SecondaryButton,
                Padding = new Thickness(12, 6, 12, 6),
            };
            selectButton.Click += (_, _) => { _selected = schoolClass; RebuildChips(); RebuildMatrix(); };
            row.Children.Add(selectButton);
            row.Children.Add(EditorControls.DeleteIconButton(() =>
            {
                _classes.Remove(schoolClass);
                if (ReferenceEquals(_selected, schoolClass)) _selected = null;
                PersistSilently();
                RebuildChips();
                RebuildMatrix();
            }));

            ClassChips.Items.Add(new Border { Margin = new Thickness(0, 0, 8, 8), Child = row });
        }
    }

    private void RebuildMatrix()
    {
        MatrixGrid.RowDefinitions.Clear();
        MatrixGrid.ColumnDefinitions.Clear();
        MatrixGrid.Children.Clear();

        if (_selected is not { } schoolClass)
        {
            EmptyText.Visibility = Visibility.Visible;
            SelectedClassHeader.Visibility = Visibility.Collapsed;
            PasteCard.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        SelectedClassHeader.Visibility = Visibility.Visible;
        SelectedClassHeader.Text = $"{schoolClass.Name} — Haftalık Program";

        if (_periods.Count == 0)
        {
            PasteCard.Visibility = Visibility.Collapsed;
            MatrixGrid.Children.Add(new TextBlock
            {
                Text = "Önce 'Ders & Zil Saatleri' sayfasından periyot tanımlayın.",
                Style = (Style)FindResource("MutedTextStyle"),
            });
            return;
        }

        PasteCard.Visibility = Visibility.Visible;

        MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        foreach (var _ in WorkDays)
            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });

        for (var i = 0; i < _periods.Count + 1; i++)
            MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddCell(0, 0, HeaderCell(""));
        for (var c = 0; c < WorkDays.Length; c++)
            AddCell(0, c + 1, HeaderCell(Turkish.DateTimeFormat.GetDayName(WorkDays[c]).ToUpper(Turkish)));

        for (var r = 0; r < _periods.Count; r++)
        {
            var period = _periods[r];
            AddCell(r + 1, 0, HeaderCell($"{period.Period}. {period.TimeLabel}"));

            for (var c = 0; c < WorkDays.Length; c++)
            {
                var day = WorkDays[c];
                var entry = schoolClass.Lessons.FirstOrDefault(l => l.Day == day && l.Period == period.Period);
                AddCell(r + 1, c + 1, Pad(BuildLessonCell(schoolClass, day, period.Period, entry)));
            }
        }
    }

    private UIElement BuildLessonCell(SchoolClass schoolClass, DayOfWeek day, int periodNumber, ClassLessonEntry? existing)
    {
        var stack = new StackPanel();

        var subjectBox = new TextBox
        {
            Style = EditorControls.TextBoxStyle,
            Text = existing?.Subject ?? "",
            ToolTip = "Ders Adı",
            Margin = new Thickness(0, 0, 0, 4),
        };
        var teacherBox = new TextBox
        {
            Style = EditorControls.TextBoxStyle,
            Text = existing?.Teacher ?? "",
            ToolTip = "Öğretmen (opsiyonel)",
            FontSize = 11,
        };

        void Commit()
        {
            var subject = subjectBox.Text.Trim();
            var teacher = teacherBox.Text.Trim();
            var entry = schoolClass.Lessons.FirstOrDefault(l => l.Day == day && l.Period == periodNumber);

            if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(teacher))
            {
                if (entry is not null)
                {
                    schoolClass.Lessons.Remove(entry);
                    PersistSilently();
                }
                return;
            }

            if (entry is null)
            {
                entry = new ClassLessonEntry { Day = day, Period = periodNumber };
                schoolClass.Lessons.Add(entry);
            }
            entry.Subject = subject;
            entry.Teacher = teacher;
            PersistSilently();
        }

        subjectBox.LostFocus += (_, _) => Commit();
        teacherBox.LostFocus += (_, _) => Commit();

        stack.Children.Add(subjectBox);
        stack.Children.Add(teacherBox);
        return stack;
    }

    /// <summary>Gün adlarının normalize edilmiş (küçük harf, Türkçe karakter sadeleştirilmiş, noktasız)
    /// hâlden DayOfWeek'e eşlemi — hem tam adları ("pazartesi") hem yaygın kısaltmaları ("pzt") kapsar.</summary>
    private static readonly Dictionary<string, DayOfWeek> DayNameMap = new()
    {
        ["pazartesi"] = DayOfWeek.Monday, ["pzt"] = DayOfWeek.Monday, ["pt"] = DayOfWeek.Monday,
        ["sali"] = DayOfWeek.Tuesday, ["sal"] = DayOfWeek.Tuesday,
        ["carsamba"] = DayOfWeek.Wednesday, ["car"] = DayOfWeek.Wednesday, ["crs"] = DayOfWeek.Wednesday,
        ["persembe"] = DayOfWeek.Thursday, ["per"] = DayOfWeek.Thursday,
        ["cuma"] = DayOfWeek.Friday, ["cum"] = DayOfWeek.Friday,
    };

    /// <summary>Excel'den kopyalanan bir hücre aralığını ayrıştırıp seçili sınıfın TÜM haftalık programını
    /// bununla değiştirir. İki şeyi ESNEK ele alır: (1) sütunların hangi güne ait olduğu — başlık satırında
    /// gün adları varsa ORADAN okunur (sütun sırası önemli değildir), yoksa varsayılan Pazartesi-Cuma sırasına
    /// düşülür; (2) bir hücrede ders adının ALTINA (Alt+Enter ile) öğretmenin kısaltılmış adı yazılmışsa,
    /// bu kısaltma Personel listesindeki tam adla eşleştirilmeye çalışılır (tek net eşleşme varsa kullanılır,
    /// belirsizse veya eşleşme yoksa kısaltma olduğu gibi yazılır). "Ders - Öğretmen" tek satırlık biçimi de
    /// desteklenir. Excel'in panoya kopyaladığı format, hücre içinde tab/satır sonu varsa o hücreyi tırnak
    /// içine alır — bu yüzden düz Split yerine tırnak-farkında bir ayrıştırıcı (ParseTsv) kullanılıyor.</summary>
    private void ApplyPaste_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not { } schoolClass || _periods.Count == 0) return;
        if (string.IsNullOrWhiteSpace(PasteBox.Text)) return;

        var rows = EditorControls.ParseTable(PasteBox.Text);
        if (rows.Count == 0) return;

        var personnel = AppServices.Data?.Load().Personnel ?? [];

        var columnDays = DetectColumnDays(rows[0]);
        int rowOffset;
        if (columnDays is not null)
        {
            rowOffset = 1;
        }
        else
        {
            columnDays = new Dictionary<int, DayOfWeek>();
            var maxCols = rows.Max(r => r.Count);
            var colOffset = maxCols == WorkDays.Length + 1 ? 1 : 0;
            for (var c = 0; c < WorkDays.Length; c++) columnDays[c + colOffset] = WorkDays[c];
            rowOffset = rows.Count == _periods.Count + 1 ? 1 : 0;
        }

        var newLessons = new List<ClassLessonEntry>();
        var resolvedCount = 0;
        var unresolved = new List<string>();

        for (var r = 0; r + rowOffset < rows.Count && r < _periods.Count; r++)
        {
            var cells = rows[r + rowOffset];
            for (var c = 0; c < cells.Count; c++)
            {
                if (!columnDays.TryGetValue(c, out var day)) continue;

                var raw = cells[c];
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var (subject, teacherRaw) = SplitCell(raw);
                if (string.IsNullOrWhiteSpace(subject)) continue;

                var teacher = teacherRaw;
                if (!string.IsNullOrWhiteSpace(teacherRaw))
                {
                    var resolved = ResolveTeacher(teacherRaw, personnel);
                    if (resolved is not null)
                    {
                        teacher = resolved;
                        resolvedCount++;
                    }
                    else
                    {
                        unresolved.Add(teacherRaw);
                    }
                }

                newLessons.Add(new ClassLessonEntry { Day = day, Period = _periods[r].Period, Subject = subject, Teacher = teacher });
            }
        }

        schoolClass.Lessons = newLessons;
        PasteBox.Text = "";
        PersistSilently();
        RebuildMatrix();

        var message = $"{schoolClass.Name} için {newLessons.Count} ders hücresi yapıştırıldı ve kaydedildi.";
        if (resolvedCount > 0) message += $" {resolvedCount} öğretmen kısaltması Personel listesiyle eşleştirildi.";
        var unresolvedCount = unresolved.Distinct().Count();
        if (unresolvedCount > 0) message += $" {unresolvedCount} öğretmen kısaltması eşleştirilemedi, yazdığınız gibi kaydedildi — dilerseniz hücreden elle düzeltebilirsiniz.";
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
    }

    /// <summary>Başlık satırında en az 3 sütunda tanınan bir gün adı varsa sütun→gün eşlemini döner,
    /// yoksa null (bu durumda çağıran taraf varsayılan Pazartesi-Cuma sırasına düşer).</summary>
    private static Dictionary<int, DayOfWeek>? DetectColumnDays(List<string> headerRow)
    {
        var map = new Dictionary<int, DayOfWeek>();
        for (var c = 0; c < headerRow.Count; c++)
        {
            var norm = EditorControls.NormalizeForMatch(headerRow[c]);
            if (DayNameMap.TryGetValue(norm, out var day) && !map.ContainsValue(day))
                map[c] = day;
        }
        return map.Count >= 3 ? map : null;
    }

    /// <summary>Bir hücreyi (Ders Adı, Öğretmen) çiftine ayırır. Hücre içinde satır sonu varsa (Excel'de
    /// Alt+Enter ile ders adının altına öğretmen kısaltması yazılmışsa) ilk satır ders, geri kalanı
    /// öğretmen kabul edilir; yoksa "Ders - Öğretmen" biçimi (son " - " ayracı) denenir; o da yoksa hücrenin
    /// tamamı sadece ders adıdır.</summary>
    private static (string Subject, string TeacherRaw) SplitCell(string raw)
    {
        var lines = raw.Replace("\r\n", "\n").Split('\n');
        if (lines.Length > 1)
        {
            var subject = lines[0].Trim();
            var teacher = string.Join(" ", lines.Skip(1).Select(l => l.Trim()).Where(l => l.Length > 0));
            return (subject, teacher);
        }

        var dashIndex = raw.LastIndexOf(" - ", StringComparison.Ordinal);
        if (dashIndex > 0)
            return (raw[..dashIndex].Trim(), raw[(dashIndex + 3)..].Trim());

        return (raw.Trim(), "");
    }

    /// <summary>"A. Yılmaz" veya "A.Ylmz" gibi kısaltılmış bir öğretmen adını Personel listesindeki TAM
    /// adla eşleştirmeye çalışır — baş harf + soyadın (sesli harfleri atılmış hâliyle de) örtüşmesi aranır.
    /// Birden fazla veya HİÇ aday eşleşmezse null döner (yanlış eşleştirmektense kısaltmayı olduğu gibi
    /// bırakmak daha güvenli) — kullanıcı sonucu paylaşmadan önce gözden geçirebilir.</summary>
    private static string? ResolveTeacher(string abbreviation, List<Personnel> personnel)
    {
        var normAbbrev = EditorControls.NormalizeForMatch(abbreviation);
        if (normAbbrev.Length < 2) return null;

        var initial = normAbbrev[0];
        var rest = normAbbrev[1..];
        var restStripped = EditorControls.StripVowels(rest);
        if (rest.Length == 0) return null;

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

    private static Border HeaderCell(string text) => new()
    {
        Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x5F)),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(8, 8, 8, 8),
        Margin = new Thickness(2),
        Child = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
        },
    };

    private static Border Pad(UIElement content) => new() { Padding = new Thickness(4), Child = content };

    private void AddCell(int row, int col, UIElement element)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, col);
        MatrixGrid.Children.Add(element);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.Classes = _classes);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
