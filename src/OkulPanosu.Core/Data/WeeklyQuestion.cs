namespace OkulPanosu.Core.Data;

/// <summary>Haftanın Beyin Egzersizi modülünün tek, paylaşılan içeriği (bilmece + kelime öğren).</summary>
public sealed class WeeklyQuestion
{
    public string Question { get; set; } = "";

    public string Answer { get; set; } = "";

    public string Clue { get; set; } = "";

    public string Word { get; set; } = "";

    public string Meaning { get; set; } = "";

    public string Example { get; set; } = "";
}
