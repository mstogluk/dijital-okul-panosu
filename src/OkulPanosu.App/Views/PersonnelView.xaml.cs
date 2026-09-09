using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class PersonnelView : UserControl, IReloadablePage
{
    private static readonly (string Key, string Label)[] Categories =
        [("teacher", "Öğretmen"), ("staff", "Diğer Personel (Memur / Hizmetli)")];

    private sealed record MonthOption(int Month, string Label);

    private static readonly MonthOption[] MonthOptions =
        Enumerable.Range(1, 12).Select(m => new MonthOption(m, TurkishCalendar.MonthNames[m - 1])).ToArray();

    private readonly List<Personnel> _personnel = new();

    private string _newName = "";
    private string _newCategory = "teacher";
    private string _newBranch = "";
    private string _newTitle = "";
    private string _newGender = "male";
    private string _newPhotoFileName = "";
    private int _newBirthDay = DateTime.Today.Day;
    private int _newBirthMonth = DateTime.Today.Month;

    public PersonnelView()
    {
        InitializeComponent();
        BuildNewPersonForm();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        _personnel.Clear();
        _personnel.AddRange(repo.Load().Personnel);
        RebuildList();
    }

    /// <summary>Sayfa önbellekten (Yönetim penceresinin sayfa cache'i) yeniden gösterildiğinde çağrılır
    /// — başka bir sayfada eklenen personel burada anında görünsün diye (ör. Nöbet Çizelgesi).</summary>
    public void Reload() => Load();

    private void BuildNewPersonForm()
    {
        NewPersonForm.Children.Add(EditorControls.LabeledTextBox("Personel Adı Soyadı", "", v => _newName = v, 0));

        var categoryStack = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        categoryStack.Children.Add(new TextBlock { Text = "Kategori", Style = EditorControls.Muted, Margin = new Thickness(0, 0, 0, 4) });
        categoryStack.Children.Add(EditorControls.LabeledComboBox(
            Categories.Select(c => new { c.Key, c.Label }), "Label", "Key", "teacher", v => _newCategory = v as string ?? "teacher"));
        NewPersonForm.Children.Add(categoryStack);

        NewPersonForm.Children.Add(EditorControls.LabeledTextBox("Branşı (öğretmenler için, ör. \"Fen Bilgisi\")", "", v => _newBranch = v));
        NewPersonForm.Children.Add(EditorControls.LabeledTextBox("Görevi / Ünvanı (ör. \"Öğretmen\", \"Müdür Yardımcısı\", \"Hizmetli\")", "", v => _newTitle = v));

        var genderStack = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        genderStack.Children.Add(new TextBlock { Text = "Cinsiyet (Varsayılan Silüet İçin)", Style = EditorControls.Muted, Margin = new Thickness(0, 0, 0, 4) });
        genderStack.Children.Add(EditorControls.LabeledComboBox(
            new[] { new { Key = "male", Label = "Erkek" }, new { Key = "female", Label = "Kadın" } },
            "Label", "Key", "male", v => _newGender = v as string ?? "male"));
        NewPersonForm.Children.Add(genderStack);

        var birthRow = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        birthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        birthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        birthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var dayBox = EditorControls.LabeledDayTextBox("Doğum Günü", _newBirthDay, v => _newBirthDay = v, 0);
        birthRow.Children.Add(dayBox);

        var monthBox = EditorControls.LabeledComboColumn("Doğum Ayı", MonthOptions, "Label", "Month", _newBirthMonth,
            v => _newBirthMonth = v as int? ?? 1, 0);
        Grid.SetColumn(monthBox, 2);
        birthRow.Children.Add(monthBox);

        NewPersonForm.Children.Add(birthRow);

        NewPersonForm.Children.Add(EditorControls.ImageFilePicker("Profil Fotoğrafı (opsiyonel)",
            "", v => _newPhotoFileName = v, () => AppServices.Data?.TeacherPhotosFolderPath ?? ""));
    }

    private void AddPerson_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_newName)) return;

        _personnel.Add(new Personnel
        {
            Name = _newName,
            Category = _newCategory,
            Branch = _newBranch,
            Title = _newTitle,
            Gender = _newGender,
            PhotoFileName = _newPhotoFileName,
            BirthDay = _newBirthDay,
            BirthMonth = _newBirthMonth,
        });

        _newName = "";
        _newCategory = "teacher";
        _newBranch = "";
        _newTitle = "";
        _newGender = "male";
        _newPhotoFileName = "";
        _newBirthDay = DateTime.Today.Day;
        _newBirthMonth = DateTime.Today.Month;

        NewPersonForm.Children.Clear();
        BuildNewPersonForm();
        RebuildList();
    }

    private void RebuildList()
    {
        ListHeader.Text = $"Tanımlı Personel Listesi ({_personnel.Count})";
        PersonnelList.Items.Clear();

        foreach (var person in _personnel.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            var row = new Grid { Margin = new Thickness(4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });

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
            var image = new Image { Stretch = Stretch.UniformToFill, Source = LoadPhoto(person) };
            photo.Child = image;
            row.Children.Add(photo);

            var nameBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = person.Name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            nameBox.LostFocus += (_, _) => person.Name = nameBox.Text;
            Grid.SetColumn(nameBox, 1);
            row.Children.Add(nameBox);

            var categoryCombo = EditorControls.LabeledComboBox(
                Categories.Select(c => new { c.Key, c.Label }), "Label", "Key", person.Category,
                v => person.Category = v as string ?? "teacher");
            categoryCombo.VerticalAlignment = VerticalAlignment.Center;
            categoryCombo.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(categoryCombo, 2);
            row.Children.Add(categoryCombo);

            var branchBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = person.Branch, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Branş" };
            branchBox.LostFocus += (_, _) => person.Branch = branchBox.Text;
            Grid.SetColumn(branchBox, 3);
            row.Children.Add(branchBox);

            var titleBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = person.Title, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Görev" };
            titleBox.LostFocus += (_, _) => person.Title = titleBox.Text;
            Grid.SetColumn(titleBox, 4);
            row.Children.Add(titleBox);

            var genderCombo = EditorControls.LabeledComboBox(
                new[] { new { Key = "male", Label = "Erkek" }, new { Key = "female", Label = "Kadın" } },
                "Label", "Key", person.Gender,
                v => { person.Gender = v as string ?? "male"; image.Source = LoadPhoto(person); });
            genderCombo.VerticalAlignment = VerticalAlignment.Center;
            genderCombo.Margin = new Thickness(0, 0, 8, 0);
            genderCombo.ToolTip = "Cinsiyet (fotoğrafsızsa silüeti belirler)";
            Grid.SetColumn(genderCombo, 5);
            row.Children.Add(genderCombo);

            var dayText = new TextBlock { Text = person.BirthDay?.ToString() ?? "—", VerticalAlignment = VerticalAlignment.Center, Style = EditorControls.Muted, ToolTip = "Doğum Günü" };
            Grid.SetColumn(dayText, 6);
            row.Children.Add(dayText);

            var monthText = new TextBlock { Text = person.BirthMonth is { } bm ? TurkishCalendar.MonthNames[bm - 1] : "—", VerticalAlignment = VerticalAlignment.Center, Style = EditorControls.Muted, ToolTip = "Doğum Ayı" };
            Grid.SetColumn(monthText, 7);
            row.Children.Add(monthText);

            var buttonsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var photoButton = new Button
            {
                Content = string.IsNullOrWhiteSpace(person.PhotoFileName) ? "📷 Foto Ekle" : "📷 Foto",
                Style = EditorControls.SecondaryButton,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Fotoğraf seç",
            };
            photoButton.Click += (_, _) => { PickPhoto(photoButton, person, image); photoButton.Content = string.IsNullOrWhiteSpace(person.PhotoFileName) ? "📷 Foto Ekle" : "📷 Foto"; };
            buttonsStack.Children.Add(photoButton);
            buttonsStack.Children.Add(EditorControls.DeleteIconButton(() => { _personnel.Remove(person); RebuildList(); }));
            Grid.SetColumn(buttonsStack, 8);
            row.Children.Add(buttonsStack);

            PersonnelList.Items.Add(EditorControls.CardWith(row));
        }
    }

    /// <summary>Windows'un kendi (küçük resim önizlemeli) "Dosya Aç" penceresini doğrudan Öğretmen
    /// Fotoğrafları klasöründe açar; başka bir yerden seçilirse dosya oraya kopyalanır. Sadece dosya adı
    /// saklanır (tam yol DEĞİL) — veri klasörü taşınsa/adı değişse bile fotoğraf her zaman bulunabilir.</summary>
    private void PickPhoto(Button anchor, Personnel person, Image image)
    {
        var folder = AppServices.Data?.TeacherPhotosFolderPath;
        if (folder is null) return;

        var picked = EditorControls.PickAndImportFile(Window.GetWindow(anchor), folder, EditorControls.ImageExtensions, "Görsel Dosyaları");
        if (picked is null) return;

        person.PhotoFileName = picked;
        image.Source = LoadPhoto(person);
    }

    private static BitmapSource LoadPhoto(Personnel person)
    {
        var folder = AppServices.Data?.TeacherPhotosFolderPath;
        if (!string.IsNullOrWhiteSpace(person.PhotoFileName) && folder is not null)
        {
            var fullPath = Path.Combine(folder, person.PhotoFileName);
            if (File.Exists(fullPath))
                return new BitmapImage(new Uri(fullPath, UriKind.Absolute));
        }

        return PersonPlaceholder.GetSilhouette(person.Gender);
    }

    /// <summary>Excel'den yapıştırılan personel listesini içe aktarır. Sütun sırası önemli değil — başlık
    /// satırındaki metinlerden (Ad Soyad ya da ayrı Adı/Soyadı, Branş, Cinsiyet, Görev/Ünvan, opsiyonel
    /// doğum tarihi) hangi sütunun ne olduğu otomatik bulunur (bkz. DetectColumns), Sınıf Ders Programı ve
    /// Öğrenciler'deki yapıştırma özellikleriyle aynı yaklaşım. Bir Görev sütunu VARSA her satır kendi
    /// göreviyle eklenir ve Kategori o göreve göre belirlenir (metninde "öğretmen" geçiyorsa Öğretmen,
    /// yoksa Diğer Personel — ör. "Memur", "Hizmetli"); Görev sütunu YOKSA eski davranış korunur (tüm
    /// satırlar "Öğretmen" olur) — geriye dönük uyumluluk için. Zaten listede AYNEN olan bir isim sessizce
    /// atlanır; BENZER görünen (ör. "Mehmet Sait Örnek" listede varken yeni satır "M. Sait Örnek" diyorsa —
    /// muhtemelen aynı kişi farklı yazılmış) bir isim bulunursa kullanıcıya sorulur, otomatik karar
    /// verilmez (bkz. FindSimilarExisting).</summary>
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
                  "\"Ad Soyad;Görev;Doğum Tarihi\")"
                : "";
            StatusText.Text = "Sütunlar tanınamadı — ilk satıra Ad Soyad (ya da Adı/Soyadı) başlığını " +
                               "içeren bir başlık satırı ekleyip tekrar deneyin." + hint;
            StatusText.Visibility = Visibility.Visible;
            return;
        }

        var existingNames = new HashSet<string>(_personnel.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var skippedSimilar = 0;
        var genderDefaulted = 0;
        var withDate = 0;

        for (var r = 1; r < rows.Count; r++)
        {
            var cells = rows[r];

            string Cell(int? col) => col is { } c && c < cells.Count ? cells[c].Trim() : "";

            var name = columns.Name is { } n
                ? Cell(n)
                : $"{Cell(columns.FirstName)} {Cell(columns.LastName)}".Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!existingNames.Add(name)) continue; // Zaten listede AYNEN var ya da bu yapıştırmada tekrar ediyor — atla.

            if (FindSimilarExisting(name, _personnel.Select(p => p.Name)) is { } similar)
            {
                var addAnyway = AppMessageBox.Confirm(Window.GetWindow(this),
                    $"'{name}' satırı, listede zaten kayıtlı olan '{similar}' ile benzer görünüyor — aynı " +
                    "kişi farklı yazılmış olabilir (ör. kısaltılmış ad). Yine de AYRI bir kayıt olarak eklensin mi?",
                    "Benzer İsim Bulundu");
                if (!addAnyway) { skippedSimilar++; continue; }
            }

            var branch = Cell(columns.Branch);
            var genderRaw = Cell(columns.Gender);
            var gender = genderRaw.Contains("kad", StringComparison.OrdinalIgnoreCase) ? "female" : "male";
            if (columns.Gender is null) genderDefaulted++;

            var title = columns.Title is { } t ? Cell(t) : "Öğretmen";
            if (string.IsNullOrWhiteSpace(title)) title = "Öğretmen";
            var category = EditorControls.NormalizeForMatch(title).Contains("ogretmen") ? "teacher" : "staff";

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

            _personnel.Add(new Personnel
            {
                Name = name,
                Category = category,
                Branch = branch,
                Title = title,
                Gender = gender,
                BirthDay = day,
                BirthMonth = month,
            });
            added++;
        }

        BulkImportBox.Text = "";
        RebuildList();

        var message = $"{added} personel eklendi.";
        if (added > 0) message += $" {withDate} tanesinin doğum tarihi de okundu.";
        if (skippedSimilar > 0) message += $" {skippedSimilar} satır, benzer bir isim bulunduğu ve \"aynı kişi\" denildiği için atlandı.";
        if (genderDefaulted > 0) message += $" Yapıştırılan listede Cinsiyet sütunu bulunmadığı için {genderDefaulted} kişi varsayılan olarak \"Erkek\" eklendi — listeden CİNSİYET sütununu kontrol edip gerekenleri düzeltin.";
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
    }

    /// <summary>Yeni bir isim, listede zaten kayıtlı bir isimle "muhtemelen aynı kişi" gibi görünüyorsa
    /// (soyadı aynı/çok benzer + ad ya birebir aynı ya da biri diğerinin kısaltması) o mevcut ismi döner,
    /// yoksa null. Ad Soyad'ın ORTA kelimeleri (varsa) karşılaştırmaya katılmaz — yalnızca ilk ve son
    /// kelimeye bakılır, böylece kısaltılmış/eksik orta ad farkları yanlış negatif üretmez. Sınıf Ders
    /// Programı'ndaki öğretmen kısaltması çözümlemesiyle (ResolveTeacher) aynı ailede bir sezgisel yöntem.</summary>
    private static string? FindSimilarExisting(string newName, IEnumerable<string> existingNames)
    {
        var newParts = newName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (newParts.Length == 0) return null;

        var newFirst = EditorControls.NormalizeForMatch(newParts[0]);
        var newLast = EditorControls.NormalizeForMatch(newParts[^1]);
        if (newFirst.Length == 0 || newLast.Length == 0) return null;

        foreach (var existing in existingNames)
        {
            if (string.Equals(existing, newName, StringComparison.OrdinalIgnoreCase)) continue;

            var exParts = existing.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (exParts.Length == 0) continue;

            var exFirst = EditorControls.NormalizeForMatch(exParts[0]);
            var exLast = EditorControls.NormalizeForMatch(exParts[^1]);
            if (exFirst.Length == 0 || exLast.Length == 0) continue;

            var lastMatches = newLast == exLast || EditorControls.StripVowels(newLast) == EditorControls.StripVowels(exLast);
            if (!lastMatches) continue;

            var firstMatches = newFirst == exFirst || (newFirst[0] == exFirst[0] && (newFirst.Length <= 2 || exFirst.Length <= 2));
            if (firstMatches) return existing;
        }

        return null;
    }

    private sealed record ColumnMap(int? Name, int? FirstName, int? LastName, int? Branch, int? Gender, int? Title, int? Day, int? Month, int? Date);

    private static ColumnMap? DetectColumns(List<string> header)
    {
        int? name = null, first = null, last = null, branch = null, gender = null, title = null, day = null, month = null, date = null;

        for (var c = 0; c < header.Count; c++)
        {
            var n = EditorControls.NormalizeForMatch(header[c]);
            if (name is null && n is "adsoyad" or "adisoyadi" or "ogretmenadisoyadi" or "isim") name = c;
            else if (first is null && n is "ad" or "adi") first = c;
            else if (last is null && n is "soyad" or "soyadi") last = c;
            else if (branch is null && n is "brans" or "bransi") branch = c;
            else if (gender is null && n is "cinsiyet") gender = c;
            else if (title is null && n is "gorev" or "gorevi" or "unvan" or "unvani" or "kategori") title = c;
            else if (day is null && n is "gun") day = c;
            else if (month is null && n is "ay") month = c;
            else if (date is null && n is "dogumtarihi" or "tarih" or "dogumgunu") date = c;
        }

        var hasName = name is not null || first is not null;
        return hasName ? new ColumnMap(name, first, last, branch, gender, title, day, month, date) : null;
    }

    /// <summary>"gg.aa.yyyy", "gg/aa/yyyy" ya da "gg-aa-yyyy" biçimlerinden gün/ay çıkarır (yıl yok sayılır
    /// — bkz. StudentsView.ParseDayMonth, aynı yaklaşım).</summary>
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
        repo.UpdateContent(data => data.Personnel = _personnel);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }
}
