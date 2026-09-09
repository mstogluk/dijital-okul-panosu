using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Services;
using OkulPanosu.App.Views.Modules;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

/// <summary>Modül üzerindeki ⚙️ butonuyla açılan modal — başlık, vurgu rengi ve (modül tipine göre)
/// tipe özel ayarları (ör. video ses düzeyi, slayt geçiş süresi) düzenler. Ayrıca modül tipi Yönetim
/// penceresindeki "İçerikler" alt menüsünden bir sayfayla eşleşiyorsa (ör. Duyurular, Nöbet Çizelgesi),
/// o sayfanın aynısını burada da gömerek gösterir — kullanıcı ayrı bir menüye gitmek zorunda kalmaz.
/// Gömülü sayfa kendi "Kaydet" butonunu GİZLER (bkz. IEmbeddableContentPage) — tek bir "Kaydet" burada
/// hem modül ayarlarını hem gömülü içeriği aynı anda kaydeder, iki ayrı buton kafa karıştırmasın diye.</summary>
public partial class ModuleSettingsDialog : Window
{
    /// <summary>Otomatik döngülü/slayt gösteren TÜM modül tipleri ve varsayılan geçiş süreleri (saniye).
    /// Yeni bir döngülü modül eklendiğinde buraya bir satır eklemek yeterli — ayar arayüzü otomatik çıkar.</summary>
    private static readonly Dictionary<string, int> SlideDurationDefaults = new()
    {
        ["announcements"] = 8,
        ["duty_teacher"] = 6,
        ["student_of_month"] = 8,
        ["school_gallery"] = 6,
        ["today_in_history"] = 8,
        ["cleanest_class"] = 6,
        ["birthdays"] = 6,
    };

    private sealed record FontChoice(string Key, string Label);

    /// <summary>Büyük Anons (Yazı) için, hemen her Windows kurulumunda hazır bulunan, kalın/dikkat
    /// çekici anonslara uygun bir yazı tipi listesi.</summary>
    private static readonly FontChoice[] BannerFontChoices =
    [
        new("Segoe UI", "Segoe UI"),
        new("Segoe UI Semibold", "Segoe UI Semibold"),
        new("Segoe UI Black", "Segoe UI Black"),
        new("Arial", "Arial"),
        new("Arial Black", "Arial Black"),
        new("Calibri", "Calibri"),
        new("Tahoma", "Tahoma"),
        new("Impact", "Impact"),
    ];

    private const int DefaultBannerFontSize = 72;
    private const string DefaultBannerFontFamily = "Segoe UI Black";

    private readonly BoardModule _module;
    private IEmbeddableContentPage? _embeddedPage;
    private bool _muted = true;
    private int _volume = 50;
    private int _transitionSeconds = 8;
    private string _birthdayFilter = "week";
    private string _announcementImageLayout = "top";
    private string _dutyLocationLayout = "right";
    private string _bannerText = "";
    private int _bannerFontSize = DefaultBannerFontSize;
    private string _bannerFontFamily = DefaultBannerFontFamily;
    private string _bannerImageFileName = "";

    public ModuleSettingsDialog(BoardModule module, string templateId)
    {
        InitializeComponent();
        _module = module;

        HeaderText.Text = $"{ModuleFactory.DisplayNameFor(module.Type)} — Ayarlar";
        TitleBox.Text = module.Title;
        ThemeCombo.ItemsSource = ThemeManager.Themes;
        ThemeCombo.SelectedValue = module.ThemeColor;

        if (module.Type == "video") BuildVideoSettings();
        if (module.Type == "banner_text") BuildBannerTextSettings();
        if (module.Type == "banner_image") BuildBannerImageSettings();

        UIElement? slideRow = SlideDurationDefaults.TryGetValue(module.Type, out var defaultSeconds) ? BuildSlideDurationRow(defaultSeconds) : null;
        UIElement? secondaryRow = module.Type switch
        {
            "birthdays" => BuildBirthdayFilterRow(),
            "announcements" => BuildAnnouncementLayoutRow(),
            "duty_teacher" => BuildDutyLocationLayoutRow(),
            _ => null,
        };

        if (slideRow is not null && secondaryRow is not null)
            TypeSpecificPanel.Children.Add(TwoColumnRow(slideRow, secondaryRow));
        else
        {
            if (slideRow is not null) TypeSpecificPanel.Children.Add(slideRow);
            if (secondaryRow is not null) TypeSpecificPanel.Children.Add(secondaryRow);
        }

        var editor = ModuleFactory.CreateContentEditor(module.Type, templateId);
        if (editor is not null)
        {
            ContentEditorHeaderText.Text = $"{ModuleFactory.DisplayNameFor(module.Type)} İçeriği";
            ContentEditorHeader.Visibility = Visibility.Visible;
            ContentEditorHost.Content = editor;

            if (editor is IEmbeddableContentPage embeddable)
            {
                embeddable.SetEmbedded();
                _embeddedPage = embeddable;
            }
        }
    }

    private void BuildVideoSettings()
    {
        _muted = !_module.Settings.TryGetValue("muted", out var mutedRaw) || mutedRaw != "false";
        _volume = _module.Settings.TryGetValue("volume", out var volumeRaw) && int.TryParse(volumeRaw, out var v) ? v : 50;

        var mutedCheck = new CheckBox { Content = "Sessiz oynat", IsChecked = _muted, Margin = new Thickness(0, 0, 0, 6) };
        mutedCheck.Checked += (_, _) => _muted = true;
        mutedCheck.Unchecked += (_, _) => _muted = false;
        TypeSpecificPanel.Children.Add(mutedCheck);

        var row = InlineSettingRow("Ses Düzeyi", out var slider, out var valueLabel, 0, 100, _volume);
        slider.ValueChanged += (_, e) =>
        {
            _volume = (int)e.NewValue;
            valueLabel.Text = _volume.ToString();
        };
        TypeSpecificPanel.Children.Add(row);
    }

    /// <summary>Büyük Anons (Yazı) — sınav dönemleri gibi tek seferlik duyurular için tek bir metni
    /// ekran boyunca kocaman gösteren modül. Metin + yazı boyutu + yazı tipi burada düzenlenir (ayrı bir
    /// İçerikler sayfası yok — tek bir modüle özel, listelenecek başka bir yerde kullanılmayan içerik).</summary>
    private void BuildBannerTextSettings()
    {
        _bannerText = _module.Settings.TryGetValue("text", out var t) ? t : "";
        _bannerFontSize = _module.Settings.TryGetValue("fontSize", out var fs) && int.TryParse(fs, out var size) && size > 0
            ? size
            : DefaultBannerFontSize;
        _bannerFontFamily = _module.Settings.TryGetValue("fontFamily", out var ff) && !string.IsNullOrWhiteSpace(ff)
            ? ff
            : DefaultBannerFontFamily;

        TypeSpecificPanel.Children.Add(EditorControls.LabeledMultilineTextBox("Anons Metni", _bannerText, v => _bannerText = v, 90));

        var sizeRow = InlineSettingRow("Yazı Boyutu (pt)", out var sizeSlider, out var sizeLabel, 24, 200, _bannerFontSize);
        sizeSlider.TickFrequency = 4;
        sizeSlider.IsSnapToTickEnabled = true;
        sizeSlider.ValueChanged += (_, e) =>
        {
            _bannerFontSize = (int)e.NewValue;
            sizeLabel.Text = _bannerFontSize.ToString();
        };

        var fontRow = new Grid();
        fontRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        fontRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        fontRow.Children.Add(new TextBlock { Text = "Yazı Tipi", Style = EditorControls.Muted, VerticalAlignment = VerticalAlignment.Center });
        var fontCombo = EditorControls.LabeledComboBox(BannerFontChoices, "Label", "Key", _bannerFontFamily,
            v => _bannerFontFamily = v as string ?? DefaultBannerFontFamily);
        Grid.SetColumn(fontCombo, 1);
        fontRow.Children.Add(fontCombo);

        TypeSpecificPanel.Children.Add(TwoColumnRow(sizeRow, fontRow));
    }

    /// <summary>Büyük Anons (Görsel) — Okulumuzdan Kareler'e benzer ama SABİT bir görsel gösterir,
    /// döngü/slayt yok. "Aynı anda birden fazla görsel görünsün" ihtiyacı, bu modülü çoklu-görsel/sabit
    /// düzen kalıplarına zorlamak yerine (önceki deneme), bu modül tipinin bir şablona BİRDEN FAZLA kez
    /// eklenebilmesiyle çözüldü (bkz. TemplateEditorView.MultiInstanceModuleTypes) — kullanıcı istediği
    /// kadar kopya ekleyip her birine ayrı görsel atar, normal ızgara editörüyle serbestçe dizer. Görsel
    /// Okulumuzdan Kareler'in Slayt klasöründen AYRI bir klasörde tutulur (bkz.
    /// AnnouncementImageFolderPath) ki oradaki otomatik döngüye karışmasın.</summary>
    private void BuildBannerImageSettings()
    {
        _bannerImageFileName = _module.Settings.TryGetValue("imageFileName", out var f) ? f : "";

        TypeSpecificPanel.Children.Add(EditorControls.ImageFilePicker("Anons Görseli", _bannerImageFileName,
            v => _bannerImageFileName = v, () => AppServices.Data?.AnnouncementImageFolderPath ?? ""));
    }

    /// <summary>Otomatik döngülü/slayt modüllerinde (bkz. SlideDurationDefaults), bir sonraki içeriğe
    /// kaç saniyede bir geçileceğini belirler ("transitionSeconds" ayarı).</summary>
    private UIElement BuildSlideDurationRow(int defaultSeconds)
    {
        _transitionSeconds = _module.Settings.TryGetValue("transitionSeconds", out var raw) && int.TryParse(raw, out var s) && s > 0
            ? s
            : defaultSeconds;

        var row = InlineSettingRow("Slayt Geçiş Süresi (sn)", out var slider, out var valueLabel, 3, 60, _transitionSeconds);
        slider.TickFrequency = 1;
        slider.IsSnapToTickEnabled = true;
        slider.ValueChanged += (_, e) =>
        {
            _transitionSeconds = (int)e.NewValue;
            valueLabel.Text = _transitionSeconds.ToString();
        };
        return row;
    }

    /// <summary>Doğum Günleri modülünün hangi aralıktaki (bugün/bu hafta/bu ay) kişileri göstereceğini
    /// belirler ("birthdayFilter" ayarı) — dinamik olsun istendiği için sabit "bu ay" yerine seçilebilir.</summary>
    private UIElement BuildBirthdayFilterRow()
    {
        _birthdayFilter = _module.Settings.TryGetValue("birthdayFilter", out var raw) && raw is "today" or "week" or "month" ? raw : "week";

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.Children.Add(new TextBlock { Text = "Doğum Günü Aralığı", Style = EditorControls.Muted, VerticalAlignment = VerticalAlignment.Center });

        var combo = EditorControls.LabeledComboBox(
            new[]
            {
                new { Key = "today", Label = "Bugün" },
                new { Key = "week", Label = "Bu Hafta" },
                new { Key = "month", Label = "Bu Ay" },
            },
            "Label", "Key", _birthdayFilter, v => _birthdayFilter = v as string ?? "week");
        Grid.SetColumn(combo, 1);
        row.Children.Add(combo);

        return row;
    }

    /// <summary>Duyurular modülünde görsel/yazı yerleşimi ("imageLayout" ayarı) — üstte görsel/altta yazı
    /// ya da solda görsel/sağda yazı arasında seçilebilir.</summary>
    private UIElement BuildAnnouncementLayoutRow()
    {
        _announcementImageLayout = _module.Settings.TryGetValue("imageLayout", out var raw) && raw == "side" ? "side" : "top";

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.Children.Add(new TextBlock { Text = "Görsel Yerleşimi", Style = EditorControls.Muted, VerticalAlignment = VerticalAlignment.Center });

        var combo = EditorControls.LabeledComboBox(
            new[]
            {
                new { Key = "top", Label = "Üstte Görsel" },
                new { Key = "side", Label = "Solda Görsel" },
            },
            "Label", "Key", _announcementImageLayout, v => _announcementImageLayout = v as string ?? "top");
        Grid.SetColumn(combo, 1);
        row.Children.Add(combo);

        return row;
    }

    /// <summary>Nöbetçi Öğretmen listesinde nöbet yerinin (kat/bölge) isimle birlikte nerede gösterileceği
    /// ("dutyLocationLayout" ayarı) — sağında sütun olarak ya da altında ikinci satır olarak seçilebilir.</summary>
    private UIElement BuildDutyLocationLayoutRow()
    {
        _dutyLocationLayout = _module.Settings.TryGetValue("dutyLocationLayout", out var raw) && raw == "below" ? "below" : "right";

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.Children.Add(new TextBlock { Text = "Nöbet Yeri Konumu", Style = EditorControls.Muted, VerticalAlignment = VerticalAlignment.Center });

        var combo = EditorControls.LabeledComboBox(
            new[]
            {
                new { Key = "right", Label = "Sağda Sütun" },
                new { Key = "below", Label = "İsmin Altında" },
            },
            "Label", "Key", _dutyLocationLayout, v => _dutyLocationLayout = v as string ?? "right");
        Grid.SetColumn(combo, 1);
        row.Children.Add(combo);

        return row;
    }

    /// <summary>Etiket + kaydırıcı + değer'i TEK satıra sığdıran ortak yardımcı — önceden her ayar
    /// (etiket üstte, kontrol altta) iki satır kaplıyordu, artık hepsi tek satırda.</summary>
    private static Grid InlineSettingRow(string label, out Slider slider, out TextBlock valueLabel, double min, double max, double value)
    {
        var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

        row.Children.Add(new TextBlock { Text = label, Style = EditorControls.Muted, VerticalAlignment = VerticalAlignment.Center });

        slider = new Slider { Minimum = min, Maximum = max, Value = value, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
        Grid.SetColumn(slider, 1);
        row.Children.Add(slider);

        valueLabel = new TextBlock { Text = ((int)value).ToString(), Style = EditorControls.Muted, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(valueLabel, 2);
        row.Children.Add(valueLabel);

        return row;
    }

    /// <summary>İki ayarı yan yana (yarı-yarıya) yerleştirir — dikey yer israfını azaltmak için.</summary>
    private static Grid TwoColumnRow(UIElement left, UIElement right)
    {
        var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        row.Children.Add(left);
        Grid.SetColumn(right, 2);
        row.Children.Add(right);

        return row;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _module.Title = TitleBox.Text;
        _module.ThemeColor = ThemeCombo.SelectedValue as string ?? "Blue";

        if (_module.Type == "video")
        {
            _module.Settings["muted"] = _muted ? "true" : "false";
            _module.Settings["volume"] = _volume.ToString();
        }

        if (SlideDurationDefaults.ContainsKey(_module.Type))
            _module.Settings["transitionSeconds"] = _transitionSeconds.ToString();

        if (_module.Type == "birthdays")
            _module.Settings["birthdayFilter"] = _birthdayFilter;

        if (_module.Type == "announcements")
            _module.Settings["imageLayout"] = _announcementImageLayout;

        if (_module.Type == "duty_teacher")
            _module.Settings["dutyLocationLayout"] = _dutyLocationLayout;

        if (_module.Type == "banner_text")
        {
            _module.Settings["text"] = _bannerText;
            _module.Settings["fontSize"] = _bannerFontSize.ToString();
            _module.Settings["fontFamily"] = _bannerFontFamily;
        }

        if (_module.Type == "banner_image")
            _module.Settings["imageFileName"] = _bannerImageFileName;

        _embeddedPage?.SaveContent();

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
