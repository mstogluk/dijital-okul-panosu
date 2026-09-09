namespace OkulPanosu.Core.Data;

/// <summary>
/// Paylaşılan veri klasöründeki tek JSON dosyasına (board-data.json) yazılan kök nesne.
/// Yayın (Kiosk) ve Yönetim örnekleri aynı dosyayı okuyup yazar.
/// </summary>
public sealed class GlobalBoardData
{
    /// <summary>Kurum adı — pano üzerindeki başlık/ticker gibi yerlerde koda gömülü sabit yerine buradan okunur.</summary>
    public string InstitutionName { get; set; } = "";

    /// <summary>Hava durumu modülü gibi konum gerektiren özellikler için il/ilçe.</summary>
    public string InstitutionCity { get; set; } = "";

    public List<Template> Templates { get; set; } = new();

    public string? ActiveTemplateId { get; set; }

    public List<DutyRosterDay> DutyRoster { get; set; } = CreateDefaultRoster();

    public List<Announcement> Announcements { get; set; } = new();

    public List<FoodMenuDay> FoodMenu { get; set; } = CreateDefaultFoodMenu();

    public List<LessonPeriod> LessonSchedule { get; set; } = new();

    /// <summary>Sınıfların haftalık ders programı — "Ders & Teneffüs Saatleri" modülü, LessonSchedule'daki
    /// periyot zamanlarıyla eşleştirip şu an hangi sınıfın hangi dersi olduğunu gösterir.</summary>
    public List<SchoolClass> Classes { get; set; } = new();

    /// <summary>Okulun genel öğrenci listesi (Personel'in öğrenci karşılığı) — Doğum Günleri modülü
    /// kişileri buradan tarih filtreleyerek okur, bkz. Student.</summary>
    public List<Student> Students { get; set; } = new();

    public List<StudentOfTheMonth> StudentsOfTheMonth { get; set; } = new();

    public List<Personnel> Personnel { get; set; } = new();

    public List<TodayInHistoryEntry> TodayInHistory { get; set; } = new();

    public WeeklyQuestion WeeklyQuestion { get; set; } = new();

    public List<CleanestClassEntry> CleanestClasses { get; set; } = new();

    /// <summary>Yönetici şifresi SHA256 hash'i — makineden bağımsız, tüm PC'lerde aynı şifre geçerli olsun diye burada (LocalSettings'te değil).</summary>
    public string? AdminPasswordHash { get; set; }

    public string? AdminRecoveryHash { get; set; }

    private static List<DutyRosterDay> CreateDefaultRoster() =>
        Enum.GetValues<DayOfWeek>().Select(d => new DutyRosterDay { Day = d }).ToList();

    private static List<FoodMenuDay> CreateDefaultFoodMenu() =>
        Enum.GetValues<DayOfWeek>().Select(d => new FoodMenuDay { Day = d }).ToList();
}
