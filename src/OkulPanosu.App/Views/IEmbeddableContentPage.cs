namespace OkulPanosu.App.Views;

/// <summary>İçerikler menüsündeki bir sayfa (Duyurular, Nöbet Çizelgesi, vb.) hem bağımsız (kendi
/// "Kaydet" butonuyla) hem de ModuleSettingsDialog içine GÖMÜLÜ olarak çalışabilir. Gömülüyken iki ayrı
/// "Kaydet" butonu kafa karıştırdığı için (dialogun kendi Kaydet'i + sayfanın kendi Kaydet'i) sayfa kendi
/// butonunu gizler, dialogun tek "Kaydet"i <see cref="SaveContent"/> üzerinden ikisini birden tetikler.</summary>
public interface IEmbeddableContentPage
{
    /// <summary>ModuleSettingsDialog içine gömüldüğünde çağrılır — sayfanın kendi Kaydet butonu gizlenir.</summary>
    void SetEmbedded();

    /// <summary>İçeriği paylaşılan veriye kaydeder — sayfanın kendi Kaydet butonuyla AYNI mantık.</summary>
    void SaveContent();
}
