using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace OkulPanosu.App.Views;

/// <summary>İçerik yönetimi sayfalarında (Personel, Duyurular, Nöbet Çizelgesi vb.) tekrar eden
/// küçük form elemanlarını üreten paylaşılan yardımcılar — her sayfa aynı kodu kopyalamasın diye.</summary>
public static class EditorControls
{
    private static T Res<T>(string key) => (T)Application.Current.Resources[key];

    public static Style Card => Res<Style>("CardStyle");
    public static Style Heading => Res<Style>("HeadingStyle");
    public static Style Muted => Res<Style>("MutedTextStyle");
    public static Style TextBoxStyle => Res<Style>("WatermarkTextBoxStyle");
    public static Style PrimaryButton => Res<Style>("PrimaryButtonStyle");
    public static Style SecondaryButton => Res<Style>("SecondaryButtonStyle");

    // Not: "Segoe MDL2 Assets" ikon fontuyla denenen sembol glyph'ler bu makinede yanlış/anlamsız
    // şekiller olarak render oluyordu. Sembol/emoji glyph'lere güvenmek yerine düz metin
    // kullanıyoruz — "Sil" gibi metin butonları hiçbir zaman sorun çıkarmadı, en güvenilir seçenek bu.

    public static StackPanel LabeledTextBox(string label, string value, Action<string> onChanged, int marginTop = 6)
    {
        var stack = new StackPanel { Margin = new Thickness(0, marginTop, 0, 0) };
        stack.Children.Add(new TextBlock { Text = label, Style = Muted, Margin = new Thickness(0, 0, 0, 4) });
        var box = new TextBox { Style = TextBoxStyle, Text = value };
        box.LostFocus += (_, _) => onChanged(box.Text);
        stack.Children.Add(box);
        return stack;
    }

    public static StackPanel LabeledMultilineTextBox(string label, string value, Action<string> onChanged, double height = 70)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        stack.Children.Add(new TextBlock { Text = label, Style = Muted, Margin = new Thickness(0, 0, 0, 4) });
        var box = new TextBox { Style = TextBoxStyle, Text = value, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = height };
        box.LostFocus += (_, _) => onChanged(box.Text);
        stack.Children.Add(box);
        return stack;
    }

    /// <summary>1-31 arası, rakam-dışı girişi engelleyen, LostFocus'ta aralığa kenetlenen gün kutusu —
    /// Tarihte Bugün ve Doğum Günleri modüllerinde serbest metin yerine kullanılır (bkz. TurkishCalendar).</summary>
    public static StackPanel LabeledDayTextBox(string label, int value, Action<int> onChanged, int marginTop = 6)
    {
        var stack = new StackPanel { Margin = new Thickness(0, marginTop, 0, 0) };
        stack.Children.Add(new TextBlock { Text = label, Style = Muted, Margin = new Thickness(0, 0, 0, 4) });
        var box = new TextBox { Style = TextBoxStyle, Text = value.ToString() };
        box.PreviewTextInput += (_, e) => e.Handled = e.Text.Length == 0 || !char.IsDigit(e.Text[0]);
        box.LostFocus += (_, _) =>
        {
            if (!int.TryParse(box.Text, out var day) || day < 1) day = 1;
            day = Math.Min(day, 31);
            box.Text = day.ToString();
            onChanged(day);
        };
        stack.Children.Add(box);
        return stack;
    }

    /// <summary>LabeledTextBox ile aynı dikey (etiket üstte) yerleşimde, ComboBox tabanlı bir seçim
    /// alanı — LabeledComboBox'ın (aşağıda) üstüne bir etiket ekler.</summary>
    public static StackPanel LabeledComboColumn<T>(string label, IEnumerable<T> items, string displayPath, string valuePath, object? selectedValue, Action<object?> onChanged, int marginTop = 6)
    {
        var stack = new StackPanel { Margin = new Thickness(0, marginTop, 0, 0) };
        stack.Children.Add(new TextBlock { Text = label, Style = Muted, Margin = new Thickness(0, 0, 0, 4) });
        stack.Children.Add(LabeledComboBox(items, displayPath, valuePath, selectedValue, onChanged));
        return stack;
    }

    /// <summary>"gg.aa.yyyy" biçiminde, yazarken noktaları otomatik ekleyen tarih kutusu — WPF'in
    /// DatePicker/Calendar kontrolleri bu projede hiç temalanmadı ve önceki "Segoe MDL2"/beyaz-zemin
    /// hatalarıyla aynı aileden riskler taşıyor; bunun yerine zaten kanıtlanmış WatermarkTextBoxStyle'a
    /// dayalı, sade bir metin kutusu kullanıyoruz. allowEmpty=true ise boş bırakılırsa null döner
    /// (opsiyonel bitiş tarihi için).</summary>
    public static TextBox DateTextBox(DateTime? value, Action<DateTime?> onChanged, bool allowEmpty = false)
    {
        var box = new TextBox
        {
            Style = TextBoxStyle,
            Text = value?.ToString("dd.MM.yyyy") ?? "",
            ToolTip = "gg.aa.yyyy",
        };

        box.PreviewTextInput += (_, e) => e.Handled = e.Text.Length == 0 || !char.IsDigit(e.Text[0]);

        var updating = false;
        box.TextChanged += (_, _) =>
        {
            if (updating) return;
            updating = true;

            var digits = new string(box.Text.Where(char.IsDigit).ToArray());
            if (digits.Length > 8) digits = digits[..8];
            var formatted = digits.Length switch
            {
                <= 2 => digits,
                <= 4 => $"{digits[..2]}.{digits[2..]}",
                _ => $"{digits[..2]}.{digits[2..4]}.{digits[4..]}",
            };
            box.Text = formatted;
            box.CaretIndex = box.Text.Length;

            updating = false;
        };

        box.LostFocus += (_, _) =>
        {
            if (DateTime.TryParseExact(box.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                onChanged(parsed);
                box.Text = parsed.ToString("dd.MM.yyyy");
            }
            else if (allowEmpty && string.IsNullOrWhiteSpace(box.Text))
            {
                onChanged(null);
            }
            else
            {
                box.Text = value?.ToString("dd.MM.yyyy") ?? "";
            }
        };

        return box;
    }

    public static ComboBox LabeledComboBox<T>(IEnumerable<T> items, string displayPath, string valuePath, object? selectedValue, Action<object?> onChanged)
    {
        var combo = new ComboBox { ItemsSource = items, SelectedValuePath = valuePath };
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(displayPath));
        template.VisualTree = factory;
        combo.ItemTemplate = template;
        combo.SelectedValue = selectedValue;
        combo.SelectionChanged += (_, _) => onChanged(combo.SelectedValue);
        return combo;
    }

    public static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp"];
    public static readonly string[] VideoExtensions = [".mp4", ".wmv", ".avi", ".mov"];

    /// <summary>Windows'un kendi "Dosya Aç" penceresini (büyük simge/küçük resim önizlemeli, okunaklı)
    /// doğrudan ilgili paylaşılan alt klasörde (Medya\...) açar. Seçilen dosya zaten o klasördeyse
    /// olduğu gibi kullanılır; başka bir yerdense klasöre KOPYALANIR — böylece medya her zaman paylaşılan
    /// veri klasörünün altında kalır. Geriye sadece DOSYA ADI döner (tam yol DEĞİL) — kaydedilen değerde
    /// hiçbir zaman "D:\YAYIN\..." gibi kök klasör yolu bulunmaz; okunurken güncel klasör yoluyla
    /// (Path.Combine ile) birleştirilir, bu yüzden veri klasörü taşınsa/adı değişse bile medya bulunur.</summary>
    public static string? PickAndImportFile(Window? owner, string folderPath, string[] extensions, string filterDescription)
    {
        Directory.CreateDirectory(folderPath);
        var patterns = string.Join(";", extensions.Select(ext => "*" + ext));
        var dialog = new OpenFileDialog
        {
            Filter = $"{filterDescription} ({patterns})|{patterns}",
            InitialDirectory = folderPath,
        };
        if (dialog.ShowDialog(owner) != true) return null;

        var fileName = Path.GetFileName(dialog.FileName);
        var destination = Path.Combine(folderPath, fileName);
        if (!string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
        {
            // Seçilen dosya, pencere içinden (Gezgin benzeri sağ-tık Sil) ya da başka bir uygulamayla
            // seçimle kopyalama arasındaki anda silinmiş/taşınmış olabilir — bu durumda sessizce
            // vazgeçiyoruz, uygulamayı çökertmiyoruz.
            try { File.Copy(dialog.FileName, destination, overwrite: true); }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        return fileName;
    }

    /// <summary>PickAndImportFile'ın aynısı, ama kopyalama adımı arka plan thread'inde (Task.Run) çalışır —
    /// video gibi büyük dosyalarda (özellikle ağ paylaşımlı bir veri klasörüne kopyalanırken) senkron
    /// File.Copy arayüz thread'ini uzun süre bloke ediyordu, pencere Windows tarafından "Yanıt Vermiyor"
    /// olarak işaretleniyordu — kullanıcı donduğunu sanıyordu. <paramref name="setBusy"/> (varsa) kopyalama
    /// başlarken true, bitince false ile çağrılır — çağıran taraf bunu "İçe aktarılıyor..." göstergesi için
    /// kullanır. Dosya seçim penceresi (ShowDialog) senkron kalır — o zaten anlık.</summary>
    public static async Task<string?> PickAndImportFileAsync(Window? owner, string folderPath, string[] extensions, string filterDescription, Action<bool>? setBusy = null)
    {
        Directory.CreateDirectory(folderPath);
        var patterns = string.Join(";", extensions.Select(ext => "*" + ext));
        var dialog = new OpenFileDialog
        {
            Filter = $"{filterDescription} ({patterns})|{patterns}",
            InitialDirectory = folderPath,
        };
        if (dialog.ShowDialog(owner) != true) return null;

        var fileName = Path.GetFileName(dialog.FileName);
        var destination = Path.Combine(folderPath, fileName);
        if (!string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
        {
            setBusy?.Invoke(true);
            try
            {
                await Task.Run(() => File.Copy(dialog.FileName, destination, overwrite: true));
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            finally { setBusy?.Invoke(false); }
        }

        return fileName;
    }

    /// <summary>PickAndImportFile'ın çoklu seçim hâli — Okulumuzdan Kareler gibi tek bir kayda değil,
    /// paylaşılan bir medya havuzuna birden fazla dosya birden eklerken kullanılır. Geriye eklenen
    /// dosyaların (tam yol DEĞİL, sadece) adları döner.</summary>
    public static string[] PickAndImportFiles(Window? owner, string folderPath, string[] extensions, string filterDescription)
    {
        Directory.CreateDirectory(folderPath);
        var patterns = string.Join(";", extensions.Select(ext => "*" + ext));
        var dialog = new OpenFileDialog
        {
            Filter = $"{filterDescription} ({patterns})|{patterns}",
            InitialDirectory = folderPath,
            Multiselect = true,
        };
        if (dialog.ShowDialog(owner) != true) return [];

        var imported = new List<string>();
        foreach (var sourcePath in dialog.FileNames)
        {
            var fileName = Path.GetFileName(sourcePath);
            var destination = Path.Combine(folderPath, fileName);
            if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            {
                // Çoklu seçimde bir dosya, kopyalama sırası gelmeden silinmiş/taşınmış olabilir —
                // o dosyayı atla, kalanları kopyalamaya devam et, uygulamayı çökertme.
                try { File.Copy(sourcePath, destination, overwrite: true); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
            }
            imported.Add(fileName);
        }
        return imported.ToArray();
    }

    public static StackPanel ImageFilePicker(string label, string currentFileName, Action<string> onChanged, Func<string> folderPath) =>
        MediaPickerWithPreview(label, currentFileName, onChanged, folderPath, ImageExtensions, "Görsel Dosyaları", isImage: true);

    public static StackPanel VideoFilePicker(string label, string currentFileName, Action<string> onChanged, Func<string> folderPath) =>
        MediaPickerWithPreview(label, currentFileName, onChanged, folderPath, VideoExtensions, "Video Dosyaları", isImage: false);

    /// <summary>Küçük önizleme (fotoğraflarda gerçek görsel, videolarda 🎬 simgesi) + okunaklı dosya adı +
    /// "Seç..." butonu (native OpenFileDialog açar, bkz. PickAndImportFile). Önceki ComboBox tabanlı liste
    /// (soluk yazı, önizleme yok) kullanıcı geri bildirimiyle bu native pencereye çevrildi.</summary>
    public static StackPanel MediaPickerWithPreview(string label, string currentFileName, Action<string> onChanged, Func<string> folderPath, string[] extensions, string filterDescription, bool isImage)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        if (!string.IsNullOrEmpty(label))
            stack.Children.Add(new TextBlock { Text = label, Style = Muted, Margin = new Thickness(0, 0, 0, 4) });

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var preview = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true,
            Margin = new Thickness(0, 0, 10, 0),
            Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
        };

        var nameText = new TextBlock { Foreground = Brushes.White, FontSize = 13, TextWrapping = TextWrapping.Wrap };
        var pickButton = new Button
        {
            Content = isImage ? "🖼 Fotoğraf Seç..." : "🎬 Video Seç...",
            Style = SecondaryButton,
            Margin = new Thickness(0, 6, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
        };

        void RefreshPreview(string fileName)
        {
            nameText.Text = string.IsNullOrWhiteSpace(fileName) ? "Seçili dosya yok" : fileName;

            if (isImage && !string.IsNullOrWhiteSpace(fileName))
            {
                var fullPath = Path.Combine(folderPath(), fileName);
                if (File.Exists(fullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    preview.Child = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };
                    return;
                }
            }

            preview.Child = new TextBlock
            {
                Text = isImage ? "🖼" : "🎬",
                FontSize = 26,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        RefreshPreview(currentFileName);

        var idleContent = pickButton.Content;
        pickButton.Click += async (_, _) =>
        {
            var picked = await PickAndImportFileAsync(Window.GetWindow(pickButton), folderPath(), extensions, filterDescription,
                busy =>
                {
                    pickButton.IsEnabled = !busy;
                    pickButton.Content = busy ? "⏳ İçe aktarılıyor, lütfen bekleyin..." : idleContent;
                });
            if (picked is null) return;
            onChanged(picked);
            RefreshPreview(picked);
        };

        var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        infoStack.Children.Add(nameText);
        infoStack.Children.Add(pickButton);

        Grid.SetColumn(preview, 0);
        row.Children.Add(preview);
        Grid.SetColumn(infoStack, 1);
        row.Children.Add(infoStack);

        stack.Children.Add(row);
        return stack;
    }

    /// <summary>Tüm İçerikler sayfalarındaki "🗑 Sil" butonlarının TEK ortak kaynağı — bu yüzden burada
    /// eklenen bir onay adımı, elle tek tek her sayfayı gezmeden UYGULAMA GENELİNDE devreye girer.
    /// Kullanıcı yanlışlıkla çöp kutusuna tıklayıp geri alınamaz bir silme yapabildiğini bildirdi;
    /// artık her tıklamada önce "emin misiniz?" onayı isteniyor, sadece "Evet" denirse onClick çalışır.</summary>
    public static Button DeleteIconButton(Action onClick, string itemLabel = "bu öğeyi")
    {
        var button = new Button
        {
            Content = "🗑 Sil",
            Style = SecondaryButton,
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(6, 0, 0, 0),
        };
        button.Click += (_, _) =>
        {
            if (AppMessageBox.Confirm(Window.GetWindow(button), $"{itemLabel} silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.", "Silme Onayı"))
                onClick();
        };
        return button;
    }

    public static Border CardWith(UIElement content, Thickness? margin = null) => new()
    {
        Style = Card,
        Margin = margin ?? new Thickness(0, 0, 0, 10),
        Child = content,
    };

    public static TextBlock SectionTitle(string text) => new()
    {
        Text = text,
        Style = Heading,
        Margin = new Thickness(0, 0, 0, 6),
    };

    /// <summary>Bir isim/kısaltma karşılaştırmasında sesli harflerin varlığı/yokluğu (ör. "Yılmz" vs
    /// "Yılmaz") gürültü sayılıp atılır — kısaltılmış yazımları tam adlarla eşleştirmeye çalışan
    /// sayfalarda (Sınıf Ders Programı, Personel, Öğrenciler) NormalizeForMatch'in üstüne uygulanır.</summary>
    public static string StripVowels(string s) =>
        new(s.Where(c => "aeiou".IndexOf(c) < 0).ToArray());

    /// <summary>Türkçe karakterleri sadeleştirip (ı/İ→i, ş→s, ç→c, ğ→g, ö→o, ü→u) harf-dışı her şeyi
    /// atar — Excel/e-Okul yapıştırmalarında başlık metinlerini ("Ad Soyad", "Doğum Tarihi" gibi) sütun
    /// sırasından bağımsız tanımak için tüm sayfalarda (Sınıf Ders Programı, Öğrenciler, Personel)
    /// kullanılan ortak karşılaştırma anahtarı.</summary>
    public static string NormalizeForMatch(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var raw in s.ToLowerInvariant())
        {
            var c = raw switch
            {
                'ı' or 'i' or 'İ' => 'i',
                'ş' => 's',
                'ç' => 'c',
                'ğ' => 'g',
                'ö' => 'o',
                'ü' => 'u',
                _ => raw,
            };
            if (char.IsLetter(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Excel'in pano formatını (sekmeyle ayrılmış sütun, satır başıyla ayrılmış satır) ayrıştırır —
    /// standart CSV/TSV tırnaklama kuralına uyar: bir hücre tab/satır sonu/tırnak içeriyorsa Excel o hücreyi
    /// çift tırnak içine alır (içindeki tırnaklar ikizlenerek kaçırılır). Düz Split('\t')/Split('\n') bu
    /// durumda satır/sütun sayımını bozar — ör. bir ders adının altına Alt+Enter ile yazılan öğretmen
    /// kısaltması tam olarak bu senaryoyu tetikler. Boş satırlar (tamamen boş hücrelerden oluşan) elenir.</summary>
    public static List<List<string>> ParseTsv(string text)
    {
        var normalized = text.Replace("\r\n", "\n");
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        var i = 0;
        while (i < normalized.Length)
        {
            var ch = normalized[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < normalized.Length && normalized[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                        continue;
                    }
                    inQuotes = false;
                    i++;
                    continue;
                }
                field.Append(ch);
                i++;
                continue;
            }

            switch (ch)
            {
                case '"':
                    inQuotes = true;
                    i++;
                    break;
                case '\t':
                    currentRow.Add(field.ToString());
                    field.Clear();
                    i++;
                    break;
                case '\n':
                    currentRow.Add(field.ToString());
                    field.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    i++;
                    break;
                default:
                    field.Append(ch);
                    i++;
                    break;
            }
        }

        currentRow.Add(field.ToString());
        rows.Add(currentRow);

        return rows.Where(r => r.Any(cell => !string.IsNullOrWhiteSpace(cell))).ToList();
    }

    /// <summary>ParseTsv'nin sütun ayracını otomatik seçen sürümü. Excel'den kopyalanan veri gerçek TAB
    /// karakterleriyle ayrılır — bu durumda tırnak-farkında ParseTsv'ye devredilir. Elle örnek/test verisi
    /// yazan bir kullanıcı genelde Tab tuşunu değil (çoğu kutuda odağı değiştirir) noktalı virgülü kullanır
    /// — metinde HİÇ tab yoksa satırlar ';' ile ayrıştırılır (basit Split, tırnaklama gerekmiyor çünkü elle
    /// yazılan veride Excel'in tırnaklama senaryosu zaten oluşmaz). Yapıştırma kutuları toplu içe aktarımın
    /// tek giriş noktası olduğu için (Personel, Öğrenciler, Sınıf Ders Programı) bu seçim burada, TEK
    /// yerde yapılır.</summary>
    public static List<List<string>> ParseTable(string text)
    {
        if (text.Contains('\t')) return ParseTsv(text);

        return text.Replace("\r\n", "\n").Split('\n')
            .Select(line => line.Split(';').Select(c => c.Trim()).ToList())
            .Where(r => r.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .ToList();
    }
}
