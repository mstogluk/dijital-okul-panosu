namespace OkulPanosu.Core.Data;

/// <summary>Tarihte Bugün modülünde gösterilen bir tarih ve o tarihe ait olaylar. Gün/Ay serbest metin
/// yerine sayı/liste seçimiyle girilir — "4 Temuz" gibi yazım hataları panoyla asla eşleşmiyordu, artık
/// yapısal alanlar sayesinde bu imkansız.</summary>
public sealed class TodayInHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>true ise Day/Month yok sayılır — bugüne özel eşleşen kayıt yoksa gösterilen "her zaman geçerli" kayıt.</summary>
    public bool IsGeneral { get; set; }

    public int Day { get; set; } = 1;

    /// <summary>1-12.</summary>
    public int Month { get; set; } = 1;

    public List<string> Events { get; set; } = new();
}
