namespace OkulPanosu.Core.Data;

/// <summary>Bir kat/alan için nöbetçi öğretmen ataması.</summary>
public sealed class DutyAssignment
{
    public string Floor { get; set; } = "";

    public string TeacherName { get; set; } = "";
}

/// <summary>Haftanın bir gününe atanmış nöbetçi müdür yardımcısı ve kat bazlı öğretmen listesi.</summary>
public sealed class DutyRosterDay
{
    /// <summary>Nöbet matrisindeki satırlar (kat/alan) — tüm günlerde aynı sırayla gösterilir.</summary>
    public static readonly string[] DefaultFloors =
        ["Bahçe / Dış Alan", "Zemin Kat", "1. Kat", "2. Kat", "3. Kat", "4. Kat"];

    public DayOfWeek Day { get; set; }

    public string Deputy { get; set; } = "";

    public List<DutyAssignment> Assignments { get; set; } = new();
}
