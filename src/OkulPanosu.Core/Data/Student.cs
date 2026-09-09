namespace OkulPanosu.Core.Data;

/// <summary>Okulun genel öğrenci listesi — Personel'in öğrenci karşılığı. Doğum Günleri modülü panoda
/// göstereceği kişileri BURADAN (BirthDay/BirthMonth alanlarına göre tarih aralığı filtreleyerek) okur,
/// kendi ayrı bir listesi yoktur; böylece toplu aktarım (Excel/e-Okul) tek bir yere yapılır ve personelle
/// karışmaz. Doğum tarihi bilinmiyorsa BirthDay/BirthMonth null kalır — o öğrenci hiçbir zaman Doğum
/// Günleri modülünde görünmez ama listede durur.</summary>
public sealed class Student
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    public string Class { get; set; } = "";

    /// <summary>1-31, bilinmiyorsa null.</summary>
    public int? BirthDay { get; set; }

    /// <summary>1-12, bilinmiyorsa null.</summary>
    public int? BirthMonth { get; set; }

    /// <summary>Fotoğrafsız kayıtlarda otomatik silüet için ("male"/"female") — bkz. Personnel.Gender.</summary>
    public string Gender { get; set; } = "male";

    public string PhotoFileName { get; set; } = "";
}
