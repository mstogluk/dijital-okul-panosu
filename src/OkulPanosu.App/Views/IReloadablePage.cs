namespace OkulPanosu.App.Views;

/// <summary>Yönetim penceresi sayfaları örneklerini önbellekte tutar (sekmeler arası geçişte state
/// korunsun diye) — ama bu yüzden bir sayfa yeniden gösterildiğinde başka bir sayfada yapılan
/// değişiklikleri (ör. Personel'e eklenen yeni öğretmen) görmeyebilir. Bu arayüzü uygulayan sayfalar
/// her gösterildiğinde verisini diskten tazeler.</summary>
public interface IReloadablePage
{
    void Reload();
}
