using System.Text.Json;

namespace OkulPanosu.Core.Data;

/// <summary>
/// Paylaşılan veri klasöründeki board-data.json dosyasını okur/yazar. Her çağrı dosyanın
/// tamamını okuyup (gerekirse) değiştirip geri yazar — bu ölçekte (birkaç şablon, birkaç KB)
/// performans sorun değil; asıl önemli olan ağ paylaşımı üzerinden okuyan Kiosk örneğinin hiçbir
/// zaman yarım yazılmış bir dosya görmemesi (bkz. Save: geçici dosyaya yaz + atomik taşı).
/// </summary>
public sealed class BoardDataRepository(string dataFolderPath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string DataFolderPath { get; } = dataFolderPath;

    public string BoardDataFilePath => Path.Combine(DataFolderPath, "board-data.json");

    public string TemplatesFolderPath => Path.Combine(DataFolderPath, "Şablonlar");

    public string MediaFolderPath => Path.Combine(DataFolderPath, "Medya");

    public string ImagesFolderPath => Path.Combine(MediaFolderPath, "Duyuru ve Haber Resimleri");

    public string VideosFolderPath => Path.Combine(MediaFolderPath, "Videolar");

    public string TeacherPhotosFolderPath => Path.Combine(MediaFolderPath, "Öğretmen Fotoğrafları");

    public string StudentPhotosFolderPath => Path.Combine(MediaFolderPath, "Öğrenci Fotoğrafları");

    /// <summary>"Okulumuzdan Kareler" galeri modülünün otomatik olarak sırayla gösterdiği resimlerin
    /// bulunduğu klasör — bu klasörde ne varsa modül eklendiğinde otomatik olarak sırayla gösterilir,
    /// ayrı bir yönetim ekranı yok (kullanıcı dosyaları doğrudan bu klasöre kopyalar).</summary>
    public string SlideshowFolderPath => Path.Combine(MediaFolderPath, "Slayt");

    /// <summary>"Büyük Anons (Görsel)" modülünün gösterdiği TEK, sabit görsel için ayrı bir klasör —
    /// bilerek Slayt klasöründen FARKLI: aksi hâlde buraya konan görsel, Okulumuzdan Kareler'in otomatik
    /// döngüsüne de karışırdı.</summary>
    public string AnnouncementImageFolderPath => Path.Combine(MediaFolderPath, "Anons Görselleri");

    /// <summary>Veri klasörü ilk seçildiğinde (sihirbaz/Ayarlar) çağrılır — Şablonlar ve Medya alt klasörlerini önceden oluşturur.</summary>
    public void EnsureFolderStructure()
    {
        Directory.CreateDirectory(DataFolderPath);
        Directory.CreateDirectory(TemplatesFolderPath);
        MigrateLegacyImagesFolder();
        Directory.CreateDirectory(ImagesFolderPath);
        Directory.CreateDirectory(VideosFolderPath);
        Directory.CreateDirectory(TeacherPhotosFolderPath);
        Directory.CreateDirectory(StudentPhotosFolderPath);
        Directory.CreateDirectory(SlideshowFolderPath);
        Directory.CreateDirectory(AnnouncementImageFolderPath);
    }

    /// <summary>Duyuru/Haber görselleri klasörü eskiden "Resimler" adındaydı, daha açıklayıcı olsun diye
    /// "Duyuru ve Haber Resimleri" olarak değiştirildi. Eski klasörde dosya varsa (ve yeni klasör henüz
    /// yoksa) içindeki dosyalar kaybolmasın diye otomatik olarak yeni ada taşınır.</summary>
    private void MigrateLegacyImagesFolder()
    {
        var legacyPath = Path.Combine(MediaFolderPath, "Resimler");
        if (Directory.Exists(legacyPath) && !Directory.Exists(ImagesFolderPath))
            Directory.Move(legacyPath, ImagesFolderPath);
    }

    /// <summary>Veri klasöründe henüz hiç şablon yoksa (ilk kurulum), elimizdeki TÜM modülleri kullanan,
    /// 1920×1080 (50×50 ızgara) için elle düzenlenmiş hazır bir düzen oluşturur ve yayına alır — kullanıcı
    /// boş bir ekranla başlamak zorunda kalmaz. Bu, kullanıcının kendi panosunda fiilen kullandığı ve
    /// beğendiği düzenin BİREBİR aynısı (bkz. DEVAM_NOTU.md on dokuzuncu tur) — istenirse değiştirilip
    /// tekrar kaydedilebilir, sadece İLK kurulumda (hiç şablon yokken) devreye girer.</summary>
    public void EnsureDefaultTemplate()
    {
        var data = Load();
        if (data.Templates.Count > 0) return;

        var template = new Template
        {
            Name = "Ana Şablon",
            Description = "",
            IsActive = true,
            GridColumns = 50,
            GridRows = 50,
            TickerText = "Okul Dijital Panosu Yayınıdır.",
            TickerSpeed = 30,
            TickerBgColor = "#1e3a8a",
            TickerTextColor = "#ffffff",
            ColorMode = "custom",
            CustomBaseColor = "#7CABE4",
            Modules =
            [
                new BoardModule { Type = "clock_date", Title = "Saat & Tarih", X = 0, Y = 0, W = 9, H = 12, ThemeColor = "Blue" },
                new BoardModule { Type = "duty_teacher", Title = "Nöbetçi Öğretmen", X = 13, Y = 21, W = 13, H = 29, ThemeColor = "Indigo", Settings = new() { ["transitionSeconds"] = "6", ["dutyLocationLayout"] = "right" } },
                new BoardModule { Type = "video", Title = "Yerel Video", X = 34, Y = 0, W = 16, H = 21, ThemeColor = "Emerald" },
                new BoardModule { Type = "today_in_history", Title = "Tarihte Bugün", X = 0, Y = 12, W = 9, H = 9, ThemeColor = "Sky", Settings = new() { ["transitionSeconds"] = "8" } },
                new BoardModule { Type = "student_of_month", Title = "Ayın Öğrencisi", X = 26, Y = 0, W = 8, H = 21, ThemeColor = "Violet" },
                new BoardModule { Type = "announcements", Title = "Duyurular", X = 9, Y = 0, W = 17, H = 21, ThemeColor = "Amber", Settings = new() { ["transitionSeconds"] = "8", ["imageLayout"] = "side" } },
                new BoardModule { Type = "weekly_question", Title = "Haftanın Beyin Egzersizi", X = 26, Y = 33, W = 12, H = 17, ThemeColor = "Rose" },
                new BoardModule { Type = "school_gallery", Title = "Okulumuzdan Kareler", X = 0, Y = 32, W = 13, H = 18, ThemeColor = "Blue", Settings = new() { ["transitionSeconds"] = "6" } },
                new BoardModule { Type = "food_menu", Title = "Yemek Menüsü", X = 26, Y = 21, W = 12, H = 12, ThemeColor = "Blue" },
                new BoardModule { Type = "birthdays", Title = "Doğum Günleri", X = 6, Y = 21, W = 7, H = 11, ThemeColor = "Blue" },
                new BoardModule { Type = "cleanest_class", Title = "Haftanın Temiz Sınıfları", X = 0, Y = 21, W = 6, H = 11, ThemeColor = "Blue", Settings = new() { ["transitionSeconds"] = "6" } },
                new BoardModule { Type = "schedule", Title = "Ders & Teneffüs Saatleri", X = 38, Y = 21, W = 12, H = 29, ThemeColor = "Blue" },
            ],
        };

        data.Templates.Add(template);
        data.ActiveTemplateId = template.Id;
        Save(data);
    }

    /// <summary>Veri klasöründe henüz hiç ders periyodu yoksa (ilk kurulum, ya da kullanıcı "Ders & Zil
    /// Saatleri" sayfasını hiç kaydetmediyse) 08:30'dan başlayan, 40 dk ders + 10 dk teneffüs ile 10
    /// periyotluk bir varsayılan oluşturur ve kaydeder. Bu olmadan "Ders & Zil Saatleri" sayfası ekranda
    /// dolu GÖRÜNÜYOR olsa bile (o sayfa "Kaydet" denmeden kendi içinde geçici bir varsayılan üretiyordu)
    /// aslında paylaşılan veri boş kalıyordu — "Sınıf Ders Programı" ve pano modülü gerçek kaydı okuyunca
    /// "önce periyot tanımlayın" diyordu. Artık periyotlar KAYDEDİLMİŞ olarak garanti hazır geliyor.</summary>
    public void EnsureDefaultLessonSchedule()
    {
        var data = Load();
        if (data.LessonSchedule.Count > 0) return;

        var start = new TimeSpan(8, 30, 0);
        var periods = new List<LessonPeriod>();
        for (var i = 0; i < 10; i++)
        {
            var period = new LessonPeriod { Period = i + 1, Subject = $"{i + 1}. Ders", Start = start, DurationMinutes = 40, BreakAfterMinutes = 10 };
            periods.Add(period);
            start = period.End.Add(TimeSpan.FromMinutes(period.BreakAfterMinutes));
        }

        data.LessonSchedule = periods;
        Save(data);
    }

    public GlobalBoardData Load()
    {
        if (!File.Exists(BoardDataFilePath)) return new GlobalBoardData();

        var json = File.ReadAllText(BoardDataFilePath);
        return JsonSerializer.Deserialize<GlobalBoardData>(json) ?? new GlobalBoardData();
    }

    public void Save(GlobalBoardData data)
    {
        Directory.CreateDirectory(DataFolderPath);

        var tempPath = BoardDataFilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(tempPath, BoardDataFilePath, overwrite: true);
    }

    public DateTime? GetLastWriteTimeUtc() =>
        File.Exists(BoardDataFilePath) ? File.GetLastWriteTimeUtc(BoardDataFilePath) : null;

    public Template? GetActiveTemplate()
    {
        var data = Load();
        return data.Templates.FirstOrDefault(t => t.Id == data.ActiveTemplateId);
    }

    public Template AddTemplate(string name)
    {
        var data = Load();
        var template = new Template { Name = name };
        data.Templates.Add(template);
        if (data.ActiveTemplateId is null) data.ActiveTemplateId = template.Id;
        Save(data);
        return template;
    }

    public void RenameTemplate(string templateId, string newName)
    {
        var data = Load();
        var template = data.Templates.FirstOrDefault(t => t.Id == templateId);
        if (template is null) return;
        template.Name = newName;
        Save(data);
    }

    public Template? DuplicateTemplate(string templateId)
    {
        var data = Load();
        var source = data.Templates.FirstOrDefault(t => t.Id == templateId);
        if (source is null) return null;

        var copy = new Template
        {
            Name = source.Name + " (kopya)",
            Description = source.Description,
            GridColumns = source.GridColumns,
            GridRows = source.GridRows,
            Modules = source.Modules.Select(m => new BoardModule
            {
                Type = m.Type,
                Title = m.Title,
                X = m.X,
                Y = m.Y,
                W = m.W,
                H = m.H,
                ThemeColor = m.ThemeColor,
                Settings = new Dictionary<string, string>(m.Settings),
            }).ToList(),
        };
        data.Templates.Add(copy);
        Save(data);
        return copy;
    }

    public void DeleteTemplate(string templateId)
    {
        var data = Load();
        data.Templates.RemoveAll(t => t.Id == templateId);
        if (data.ActiveTemplateId == templateId)
            data.ActiveTemplateId = data.Templates.FirstOrDefault()?.Id;
        Save(data);
    }

    public void SetActiveTemplate(string templateId)
    {
        var data = Load();
        if (data.Templates.All(t => t.Id != templateId)) return;
        data.ActiveTemplateId = templateId;
        Save(data);
    }

    public void UpdateTemplateModules(string templateId, int gridColumns, int gridRows, List<BoardModule> modules, string tickerText, int tickerSpeed, string colorMode, string customBaseColor)
    {
        var data = Load();
        var template = data.Templates.FirstOrDefault(t => t.Id == templateId);
        if (template is null) return;
        template.GridColumns = gridColumns;
        template.GridRows = gridRows;
        template.Modules = modules;
        template.TickerText = tickerText;
        template.TickerSpeed = tickerSpeed;
        template.ColorMode = colorMode;
        template.CustomBaseColor = customBaseColor;
        Save(data);
    }

    /// <summary>Yerel Video listesi artık ŞABLONA ÖZGÜ (bkz. Template.Videos) — her şablon kendi video
    /// havuzunu okur/kaydeder, önceki paylaşılan tek havuzun yerini aldı.</summary>
    public void UpdateTemplateVideos(string templateId, List<LocalVideo> videos)
    {
        var data = Load();
        var template = data.Templates.FirstOrDefault(t => t.Id == templateId);
        if (template is null) return;
        template.Videos = videos;
        Save(data);
    }

    public void SaveDutyRoster(List<DutyRosterDay> roster)
    {
        var data = Load();
        data.DutyRoster = roster;
        Save(data);
    }

    /// <summary>Duyurular/Yemek Menüsü/Ders Saatleri/Doğum Günleri/Ayın Öğrencisi gibi paylaşılan
    /// içerik türlerini düzenlerken kullanılan genel amaçlı oku-değiştir-kaydet yardımcı metodu.</summary>
    public void UpdateContent(Action<GlobalBoardData> mutate)
    {
        var data = Load();
        mutate(data);
        Save(data);
    }
}
