using System.Windows.Controls;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Modül tipi anahtarından ilgili görünüm sınıfına eşleme — Taşıt Tanıma'daki sayfa fabrikası deseninin aynısı.</summary>
public static class ModuleFactory
{
    public static readonly IReadOnlyList<(string Type, string DisplayName)> KnownTypes =
    [
        ("clock_date", "Saat & Tarih"),
        ("duty_teacher", "Nöbetçi Öğretmen"),
        ("announcements", "Duyurular"),
        ("food_menu", "Yemek Menüsü"),
        ("schedule", "Ders & Teneffüs Saatleri"),
        ("birthdays", "Doğum Günleri"),
        ("student_of_month", "Ayın Öğrencisi"),
        ("video", "Yerel Video"),
        ("today_in_history", "Tarihte Bugün"),
        ("weekly_question", "Haftanın Beyin Egzersizi"),
        ("cleanest_class", "Haftanın Temiz Sınıfları"),
        ("school_gallery", "Okulumuzdan Kareler"),
        ("banner_text", "Büyük Anons (Yazı)"),
        ("banner_image", "Büyük Anons (Görsel)"),
    ];

    /// <summary>Yeni eklenen bir modülün ızgarada başlayacağı varsayılan boyut — modül tipine göre.
    /// Her yeni modül tipi eklendiğinde buraya da bir satır eklenir.</summary>
    private static readonly Dictionary<string, (int W, int H)> DefaultSizes = new()
    {
        ["clock_date"] = (6, 4),
        ["duty_teacher"] = (6, 8),
        ["announcements"] = (8, 8),
        ["food_menu"] = (6, 4),
        ["schedule"] = (8, 4),
        ["birthdays"] = (6, 4),
        ["student_of_month"] = (6, 8),
        ["video"] = (10, 8),
        ["today_in_history"] = (6, 6),
        ["weekly_question"] = (6, 8),
        ["cleanest_class"] = (6, 4),
        ["school_gallery"] = (10, 8),
        ["banner_text"] = (36, 14),
        ["banner_image"] = (24, 20),
    };

    /// <summary>Çoğu modül tipi paylaşılan/global içerik okuduğu için sadece BoardModule yeterli, ama
    /// "video" (bkz. Template.Videos — TAMAMEN şablona özgü liste) ve "announcements" (bkz.
    /// Announcement.PublishedInTemplateIds — İÇERİK paylaşılır, sadece "bu şablonda yayında mı" şablona
    /// özgüdür) kapsayan Template'e de ihtiyaç duyar.</summary>
    public static UserControl CreateView(BoardModule module, Template template) => module.Type switch
    {
        "clock_date" => new ClockDateModuleView(module),
        "duty_teacher" => new DutyTeacherModuleView(module),
        "announcements" => new AnnouncementsModuleView(module, template),
        "food_menu" => new FoodMenuModuleView(module),
        "schedule" => new ScheduleModuleView(module),
        "birthdays" => new BirthdaysModuleView(module),
        "student_of_month" => new StudentOfMonthModuleView(module),
        "video" => new VideoModuleView(module, template),
        "today_in_history" => new TodayInHistoryModuleView(module),
        "weekly_question" => new WeeklyQuestionModuleView(module),
        "cleanest_class" => new CleanestClassModuleView(module),
        "school_gallery" => new SchoolGalleryModuleView(module),
        "banner_text" => new BannerTextModuleView(module),
        "banner_image" => new BannerImageModuleView(module),
        _ => new UnknownModuleView(module),
    };

    public static string DisplayNameFor(string type) =>
        KnownTypes.FirstOrDefault(t => t.Type == type).DisplayName ?? type;

    public static (int W, int H) DefaultSizeFor(string type) =>
        DefaultSizes.TryGetValue(type, out var size) ? size : (3, 2);

    /// <summary>Modül tipi, Yönetim penceresinin "İçerikler" alt menüsündeki bir sayfayla eşleşiyorsa
    /// o sayfanın YENİ bir örneğini döner — Modül Ayarları penceresi (⚙️) bunu gömerek, kullanıcının
    /// içerik sayfasına ayrıca gitmesine gerek kalmadan aynı düzenleme arayüzünü orada da gösterir.
    /// Kendine ait bir içerik sayfası olmayan tipler (ör. Saat/Tarih) için null döner. "birthdays" da
    /// KASITLI OLARAK null döner — içeriği artık kendine özgü değil, Öğrenciler (StudentsView, bkz.
    /// GlobalBoardData.Students) sayfasındaki PAYLAŞILAN veriden tarih filtreleyerek okunuyor; Personel'in
    /// hiç modülü olmayışıyla AYNI mantık (⚙️'den "bu modülün içeriğini düzenle" demek yanıltıcı olurdu,
    /// çünkü orada düzenlenen aslında TÜM şablonlardaki Doğum Günleri modüllerini etkileyen ortak roster).
    /// "video" için
    /// <paramref name="templateId"/> zorunlu — hangi şablonun video listesini düzenleyeceğini bilmesi
    /// gerekiyor (bkz. Template.Videos); embedded (⚙️ içinden) çağrılarda gerçek şablon kimliği,
    /// bağımsız İçerikler sayfasından çağrılırken null geçilir (o zaman VideosView kendi şablon
    /// seçicisini gösterir).</summary>
    public static UserControl? CreateContentEditor(string type, string? templateId = null) => type switch
    {
        "duty_teacher" => new DutyRosterView(),
        "announcements" => new AnnouncementsView(templateId),
        "food_menu" => new FoodMenuView(),
        "schedule" => new ScheduleView(),
        "student_of_month" => new StudentOfMonthView(),
        "video" => new VideosView(templateId),
        "today_in_history" => new TodayInHistoryView(),
        "weekly_question" => new WeeklyQuestionView(),
        "cleanest_class" => new CleanestClassView(),
        "school_gallery" => new SchoolGalleryView(),
        _ => null,
    };
}
