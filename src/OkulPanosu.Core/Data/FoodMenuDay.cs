namespace OkulPanosu.Core.Data;

/// <summary>Haftanın bir gününe ait yemek listesi.</summary>
public sealed class FoodMenuDay
{
    public DayOfWeek Day { get; set; }

    public List<string> Items { get; set; } = new();

    public string Calories { get; set; } = "";
}
