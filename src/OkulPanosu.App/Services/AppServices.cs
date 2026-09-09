using System.IO;
using System.Windows;
using OkulPanosu.App.Views;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Services;

/// <summary>Uygulama genelinde paylaşılan tekil servisler (basit bir servis konteyneri, Taşıt Tanıma'daki desenin aynısı).</summary>
public static class AppServices
{
    public static LocalSettingsRepository LocalSettingsRepo { get; } = new();

    public static LocalSettings LocalSettings { get; private set; } = null!;

    /// <summary>Veri klasörü henüz seçilmediyse (ilk çalıştırma sihirbazı tamamlanana kadar) null olabilir.</summary>
    public static BoardDataRepository? Data { get; private set; }

    /// <summary>Sadece makineye özel ayarları (yerel dosya, ağ gerektirmez) yükler — hızlı ve güvenli,
    /// veri klasörünün paylaşıldığı PC kapalı olsa bile asla hata vermez. Veri klasörüne asıl erişim
    /// (ağ üzerinden olabilir) ayrı olarak <see cref="EnsureDataReadyWithRetry"/> ile yapılır — böylece
    /// "Yayın PC'si kapalı" durumu, ayarların kendisini okuyamamaktan AYRI, net bir hata olarak ele
    /// alınabilir (bkz. kullanıcı geri bildirimi: "sadece bir hata oluştu gibi" belirsiz bir mesaj
    /// görüyordu — kök neden, bu ikisinin tek bir adımda birleşmiş olmasıydı).</summary>
    public static void LoadLocalSettings() => LocalSettings = LocalSettingsRepo.Load();

    /// <summary>Ağ paylaşımlı bir veri klasörüne ulaşılamadığında (ör. onu paylaşan Yayın PC'si kapalı)
    /// hem Directory.CreateDirectory hem File işlemleri IOException/UnauthorizedAccessException fırlatır
    /// — bu, "veri klasörü henüz hiç seçilmemiş" gibi normal/beklenen bir durum değil, kullanıcının aksiyon
    /// alması gereken geçici bir ağ sorunudur.</summary>
    public static bool IsFolderUnreachableError(Exception ex) => ex is IOException or UnauthorizedAccessException;

    public static string DescribeFolderUnreachable(string path) =>
        $"Paylaşılan veri klasörüne ulaşılamıyor:\n{path}\n\n" +
        "Bu genellikle veri klasörünü paylaşan PC (Yayın PC'si) kapalı ya da ağa bağlı değilse olur. " +
        "O PC'yi açıp ağa bağlandığından emin olduktan sonra tekrar deneyin.";

    /// <summary>Veri klasörünü (gerekirse ağ üzerinden) hazırlamayı DENER — Şablonlar/Medya alt klasörleri,
    /// ilk kurulum varsayılanları. Klasöre ulaşılamazsa (bkz. IsFolderUnreachableError) kullanıcıyı
    /// ASLA bloke etmez: sadece net bir uyarı gösterir, <see cref="Data"/> null kalır (henüz hiç kurulum
    /// yapılmamış bir uygulamadaki gibi) ve çağıran taraf normal şekilde devam edip pencereyi açar.
    /// Kullanıcı kendi geri bildirimiyle bunu istedi — önceki tasarım (tekrar dene / hemen değiştir
    /// seçenekleri, ikisi de reddedilirse uygulama kapanır) "o PC'nin adresini bilmiyorsam ya da PC'den
    /// tamamen vazgeçildiyse hiçbir seçeneği kabul edemeyip uygulamaya HİÇ giremem, veri klasörümü bile
    /// değiştiremem" riski taşıyordu. Artık uygulama HER ZAMAN açılır; veri klasörünü değiştirmek isteyen
    /// kullanıcı zaten var olan Ayarlar sayfasındaki "Değiştir" akışını (kendi hata mesajı zaten var)
    /// kendi zamanında kullanır.</summary>
    public static void EnsureDataReady(Window? owner)
    {
        // Bu metot sadece LocalSettings.DataFolderPath boş OLMADIĞI zaten doğrulandıktan sonra çağrılır
        // (bkz. App.xaml.cs) — null-forgiving burada güvenli.
        var path = LocalSettings.DataFolderPath!;

        try
        {
            Data = new BoardDataRepository(path);
            Data.EnsureFolderStructure();
            Data.EnsureDefaultTemplate();
            Data.EnsureDefaultLessonSchedule();
        }
        catch (Exception ex) when (IsFolderUnreachableError(ex))
        {
            Data = null;
            AppMessageBox.Show(owner,
                DescribeFolderUnreachable(path) + "\n\nUygulama yine de açılacak, ama veri klasörüne " +
                "ulaşılamadığı için içerikler şimdilik boş görünecek. Klasöre yeniden ulaşabildiğinizde " +
                "uygulamayı kapatıp tekrar açın — ya da Ayarlar sayfasından başka bir veri klasörü seçin.",
                "Veri Klasörüne Ulaşılamıyor");
        }
    }

    /// <summary>İlk çalıştırma sihirbazında veya Ayarlar sayfasında veri klasörü (yeniden) seçildiğinde
    /// çağrılır. Klasöre ulaşılamazsa (bkz. IsFolderUnreachableError) istisna ÇAĞIRANA bırakılır — sihirbaz
    /// ve Ayarlar sayfası bunu kendi arayüzlerinde (satır içi hata metni / mesaj kutusu) farklı şekillerde
    /// gösterir, burada genel bir davranış dayatılmaz.</summary>
    public static void SetDataFolder(string path)
    {
        LocalSettings.DataFolderPath = path;
        SaveLocalSettings();
        Data = new BoardDataRepository(path);
        Data.EnsureFolderStructure();
        Data.EnsureDefaultTemplate();
    }

    public static void SaveLocalSettings() => LocalSettingsRepo.Save(LocalSettings);

    /// <summary>Ayarlar'da yayın için seçilen ekranın çözünürlüğü — Şablon Editörü, çalışma alanını
    /// (kaç ızgara sütunu/satırı olursa olsun) tam olarak bu boyutta çizip Excel gibi kaydırma sunar,
    /// böylece yerleştirilen modüllerin gerçek yayında ne kadar yer kaplayacağı editörde de bellidir.</summary>
    public static System.Drawing.Size GetKioskScreenSize()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        var target = screens.FirstOrDefault(s => s.DeviceName == LocalSettings.KioskMonitorDeviceName)
                     ?? System.Windows.Forms.Screen.PrimaryScreen
                     ?? screens[0];
        return target.Bounds.Size;
    }
}
