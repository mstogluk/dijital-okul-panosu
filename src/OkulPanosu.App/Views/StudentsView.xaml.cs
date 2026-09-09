using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

/// <summary>Okulun genel öğrenci listesi — Personel'in öğrenci karşılığı. Doğum Günleri modülü panoda
/// göstereceği kişileri BURADAN okur (bkz. Student, BirthdaysModuleView), bu sayfa artık sadece
/// "doğum günleri" değil, öğrencilerle ilgili tüm ortak bilgiyi (ad, sınıf, cinsiyet, fotoğraf, doğum
/// tarihi) tek yerden yönetir — böylece toplu aktarım tek bir noktaya yapılır ve personel listesiyle
/// karışmaz.</summary>
public partial class StudentsView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private sealed record MonthOption(int Month, string Label);

    private static readonly MonthOption[] MonthOptions =
        Enumerable.Range(1, 12).Select(m => new MonthOption(m, TurkishCalendar.MonthNames[m - 1])).ToArray();

    private readonly List<Student> _students = new();
    private string _newName = "";
    private string _newClass = "";
    private int _newDay = DateTime.Today.Day;
    private int _newMonth = DateTime.Today.Month;
    private string _newGender = "male";

    public StudentsView()
    {
        InitializeComponent();
        BuildForm();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        _students.Clear();
        _students.AddRange(repo.Load().Students);
        RebuildList();
    }

    public void Reload() => Load();

    private void BuildForm()
    {
        var nameField = EditorControls.LabeledTextBox("Öğrenci Adı Soyadı", _newName, v => _newName = v, 0);
        nameField.Width = 240;
        nameField.Margin = new Thickness(0, 0, 10, 0);
        NewStudentForm.Children.Add(nameField);

        var classField = EditorControls.LabeledTextBox("Sınıfı", _newClass, v => _newClass = v, 0);
        classField.Width = 120;
        classField.Margin = new Thickness(0, 0, 10, 0);
        NewStudentForm.Children.Add(classField);

        var genderField = EditorControls.LabeledComboColumn("Cinsiyet", new[]
        {
            new { Key = "male", Label = "Erkek" },
            new { Key = "female", Label = "Kadın" },
        }, "Label", "Key", _newGender, v => _newGender = v as string ?? "male", 0);
        genderField.Width = 110;
        genderField.Margin = new Thickness(0, 0, 10, 0);
        NewStudentForm.Children.Add(genderField);

        var dayField = EditorControls.LabeledDayTextBox("Gün", _newDay, v => _newDay = v, 0);
        dayField.Width = 70;
        dayField.Margin = new Thickness(0, 0, 10, 0);
        NewStudentForm.Children.Add(dayField);

        var monthField = EditorControls.LabeledComboColumn("Ay", MonthOptions, "Label", "Month", _newMonth,
            v => _newMonth = v as int? ?? 1, 0);
        monthField.Width = 140;
        monthField.Margin = new Thickness(0, 0, 10, 0);
        NewStudentForm.Children.Add(monthField);

        var addButton = new Button
        {
            Content = "+ Öğrenci Ekle",
            Style = EditorControls.PrimaryButton,
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(14, 8, 14, 8),
        };
        addButton.Click += AddStudent_Click;
        NewStudentForm.Children.Add(addButton);
    }

    private void AddStudent_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_newName)) return;

        _students.Add(new Student { Name = _newName, Class = _newClass, BirthDay = _newDay, BirthMonth = _newMonth, Gender = _newGender });
        _newName = "";
        _newClass = "";
        _newDay = DateTime.Today.Day;
        _newMonth = DateTime.Today.Month;
        _newGender = "male";
        NewStudentForm.Children.Clear();
        BuildForm();
        RebuildList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RebuildList();

    private void RebuildList()
    {
        var query = SearchBox.Text.Trim();
        var visible = (string.IsNullOrEmpty(query)
            ? _students
            : _students.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ListHeader.Text = visible.Count == _students.Count
            ? $"Öğrenci Listesi ({_students.Count})"
            : $"Öğrenci Listesi ({visible.Count} / {_students.Count})";

        StudentsList.Items.Clear();
        foreach (var student in visible)
        {
            var row = new Grid { Margin = new Thickness(4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

            var photo = new Border
            {
                Width = 48,
                Height = 48,
                CornerRadius = new CornerRadius(24),
                ClipToBounds = true,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
            };
            var image = new Image { Stretch = Stretch.UniformToFill, Source = LoadPhoto(student) };
            photo.Child = image;
            row.Children.Add(photo);

            var nameText = new TextBlock { Text = student.Name, VerticalAlignment = VerticalAlignment.Center, FontWeight = System.Windows.FontWeights.SemiBold };
            Grid.SetColumn(nameText, 1);
            row.Children.Add(nameText);

            var classText = new TextBlock { Text = student.Class, VerticalAlignment = VerticalAlignment.Center, Style = EditorControls.Muted };
            Grid.SetColumn(classText, 2);
            row.Children.Add(classText);

            var dayText = new TextBlock { Text = student.BirthDay?.ToString() ?? "—", VerticalAlignment = VerticalAlignment.Center, Style = EditorControls.Muted };
            Grid.SetColumn(dayText, 3);
            row.Children.Add(dayText);

            var monthText = new TextBlock { Text = student.BirthMonth is { } m ? TurkishCalendar.MonthNames[m - 1] : "—", VerticalAlignment = VerticalAlignment.Center, Style = EditorControls.Muted };
            Grid.SetColumn(monthText, 4);
            row.Children.Add(monthText);

            var genderCombo = EditorControls.LabeledComboBox(
                new[] { new { Key = "male", Label = "Erkek" }, new { Key = "female", Label = "Kadın" } },
                "Label", "Key", student.Gender,
                v => { student.Gender = v as string ?? "male"; image.Source = LoadPhoto(student); });
            genderCombo.VerticalAlignment = VerticalAlignment.Center;
            genderCombo.Margin = new Thickness(0, 0, 8, 0);
            genderCombo.ToolTip = "Cinsiyet (fotoğrafsızsa silüeti belirler)";
            Grid.SetColumn(genderCombo, 5);
            row.Children.Add(genderCombo);

            var buttonsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var photoButton = new Button
            {
                Content = string.IsNullOrWhiteSpace(student.PhotoFileName) ? "📷 Foto Ekle" : "📷 Foto",
                Style = EditorControls.SecondaryButton,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Fotoğraf seç",
            };
            photoButton.Click += (_, _) => { PickPhoto(photoButton, student, image); photoButton.Content = string.IsNullOrWhiteSpace(student.PhotoFileName) ? "📷 Foto Ekle" : "📷 Foto"; };
            buttonsStack.Children.Add(photoButton);
            buttonsStack.Children.Add(EditorControls.DeleteIconButton(() => { _students.Remove(student); RebuildList(); }));
            Grid.SetColumn(buttonsStack, 6);
            row.Children.Add(buttonsStack);

            StudentsList.Items.Add(EditorControls.CardWith(row, new Thickness(0, 0, 0, 6)));
        }
    }

    private void PickPhoto(Button anchor, Student student, Image image)
    {
        var folder = AppServices.Data?.StudentPhotosFolderPath;
        if (folder is null) return;

        var picked = EditorControls.PickAndImportFile(Window.GetWindow(anchor), folder, EditorControls.ImageExtensions, "Görsel Dosyaları");
        if (picked is null) return;

        student.PhotoFileName = picked;
        image.Source = LoadPhoto(student);
    }

    private static BitmapSource LoadPhoto(Student student)
    {
        var folder = AppServices.Data?.StudentPhotosFolderPath;
        if (!string.IsNullOrWhiteSpace(student.PhotoFileName) && folder is not null)
        {
            var fullPath = Path.Combine(folder, student.PhotoFileName);
            if (File.Exists(fullPath))
                return new BitmapImage(new Uri(fullPath, UriKind.Absolute));
        }

        return PersonPlaceholder.GetSilhouette(student.Gender);
    }

    /// <summary>e-Okul/Excel'den yapıştırılan öğrenci listesini içe aktarır. Sütun sırası önemli değil —
    /// başlık satırındaki metinlerden (Ad Soyad ya da ayrı Adı/Soyadı, Sınıf, opsiyonel Cinsiyet, opsiyonel
    /// Gün/Ay ya da tek bir Doğum Tarihi sütunu) hangi sütunun ne olduğu otomatik bulunur (bkz.
    /// DetectColumns), Sınıf Ders Programı'ndaki yapıştırma özelliğiyle aynı yaklaşım.
    /// "ReplaceAllCheck" işaretliyse (yeni öğretim yılı senaryosu — kullanıcı e-Okul'dan yeni roster
    /// aldığında eski öğrencilerin TAMAMEN silinmesini istiyor, personel bu listede zaten yok, ona hiç
    /// dokunulmuyor) önce onay istenir, onaylanırsa mevcut liste TAMAMEN boşaltılıp yerine SADECE
    /// yapıştırılan liste yazılır; işaretli değilse normal ekleme yapılır (zaten listede olan bir isim
    /// atlanır).</summary>
    private void BulkImport_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BulkImportBox.Text)) return;

        var rows = EditorControls.ParseTable(BulkImportBox.Text);
        if (rows.Count < 2) return;

        var columns = DetectColumns(rows[0]);
        if (columns is null)
        {
            var hint = rows[0].Count == 1
                ? " (sütunlar ayrılamadı — Excel'den kopyalarken hücreler otomatik Tab ile ayrılır, bu " +
                  "doğru çalışır; elle yazıyorsanız sütunlar arasına noktalı virgül (;) koyun, ör. " +
                  "\"Ad Soyad;Sınıf;Doğum Tarihi\")"
                : "";
            StatusText.Text = "Sütunlar tanınamadı — ilk satıra Ad Soyad (ya da Adı/Soyadı) başlığını " +
                               "içeren bir başlık satırı ekleyip tekrar deneyin." + hint;
            StatusText.Visibility = Visibility.Visible;
            return;
        }

        if (ReplaceAllCheck.IsChecked == true)
        {
            var confirmed = AppMessageBox.Confirm(Window.GetWindow(this),
                $"Mevcut {_students.Count} öğrenci silinip yerine yapıştırdığınız liste eklenecek. " +
                "Bu işlem geri alınamaz (personel listesine dokunulmaz). Emin misiniz?", "Listeyi Tamamen Değiştir");
            if (!confirmed) return;
            _students.Clear();
        }

        var existingNames = new HashSet<string>(_students.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var withDate = 0;
        var genderDefaulted = 0;

        for (var r = 1; r < rows.Count; r++)
        {
            var cells = rows[r];

            string Cell(int? col) => col is { } c && c < cells.Count ? cells[c].Trim() : "";

            var name = columns.Name is { } n
                ? Cell(n)
                : $"{Cell(columns.FirstName)} {Cell(columns.LastName)}".Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!existingNames.Add(name)) continue;

            var studentClass = Cell(columns.Class);
            var genderRaw = Cell(columns.Gender);
            var gender = genderRaw.Contains("kad", StringComparison.OrdinalIgnoreCase) ? "female" : "male";
            if (columns.Gender is null) genderDefaulted++;

            int? day = null, month = null;
            if (columns.Date is { } d)
            {
                if (ParseDayMonth(Cell(d)) is { } dm) (day, month) = dm;
            }
            else if (columns.Day is { } dc && columns.Month is { } mc)
            {
                if (int.TryParse(Cell(dc), out var parsedDay) && parsedDay is >= 1 and <= 31)
                {
                    var monthRaw = Cell(mc);
                    if (int.TryParse(monthRaw, out var parsedMonth) && parsedMonth is >= 1 and <= 12)
                    {
                        day = parsedDay;
                        month = parsedMonth;
                    }
                    else
                    {
                        var monthIndex = Array.FindIndex(TurkishCalendar.MonthNames,
                            mn => string.Equals(mn, monthRaw, StringComparison.OrdinalIgnoreCase));
                        if (monthIndex >= 0)
                        {
                            day = parsedDay;
                            month = monthIndex + 1;
                        }
                    }
                }
            }

            if (day is not null) withDate++;
            _students.Add(new Student { Name = name, Class = studentClass, BirthDay = day, BirthMonth = month, Gender = gender });
            added++;
        }

        BulkImportBox.Text = "";
        ReplaceAllCheck.IsChecked = false;
        RebuildList();

        var message = $"{added} öğrenci eklendi.";
        if (added > 0) message += $" {withDate} tanesinin doğum tarihi de okundu.";
        if (genderDefaulted > 0) message += $" Yapıştırılan listede Cinsiyet sütunu bulunmadığı için {genderDefaulted} kişi varsayılan olarak \"Erkek\" eklendi — listeden CİNSİYET sütununu kontrol edip gerekenleri düzeltin.";
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
    }

    private sealed record ColumnMap(int? Name, int? FirstName, int? LastName, int? Class, int? Gender, int? Day, int? Month, int? Date);

    private static ColumnMap? DetectColumns(List<string> header)
    {
        int? name = null, first = null, last = null, cls = null, gender = null, day = null, month = null, date = null;

        for (var c = 0; c < header.Count; c++)
        {
            var n = EditorControls.NormalizeForMatch(header[c]);
            if (name is null && n is "adsoyad" or "adisoyadi" or "ogrenciadisoyadi" or "isim" or "ogrenci" or "ogrenciadi") name = c;
            else if (first is null && n is "ad" or "adi") first = c;
            else if (last is null && n is "soyad" or "soyadi") last = c;
            else if (cls is null && n is "sinif" or "sube" or "sinifsube") cls = c;
            else if (gender is null && n is "cinsiyet") gender = c;
            else if (day is null && n is "gun") day = c;
            else if (month is null && n is "ay") month = c;
            else if (date is null && n is "dogumtarihi" or "tarih" or "dogumgunu") date = c;
        }

        var hasName = name is not null || first is not null;
        return hasName ? new ColumnMap(name, first, last, cls, gender, day, month, date) : null;
    }

    /// <summary>"gg.aa.yyyy", "gg/aa/yyyy" ya da "gg-aa-yyyy" biçimlerinden gün/ay çıkarır (yıl yok sayılır
    /// — sadece doğum GÜNÜ gösterildiği için önemsiz).</summary>
    private static (int Day, int Month)? ParseDayMonth(string raw)
    {
        var parts = raw.Split(['.', '/', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        if (!int.TryParse(parts[0], out var day) || day is < 1 or > 31) return null;
        if (!int.TryParse(parts[1], out var month) || month is < 1 or > 12) return null;
        return (day, month);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.Students = _students);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
