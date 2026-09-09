# Okul Panosu (Digital Signage Board) - C# / .NET Claude Code Prompt ve Teknik Döküman

Bu döküman, **Okul Panosu** uygulamasını **C# (.NET / WPF / Avalonia UI / Blazor)** teknolojileriyle Claude Code kullanarak birebir geliştirebilmeniz için hazırlanmış **Master Prompt** ve **Teknik Mimari Şartname** içerir.

---

## 1. Claude Code İçin Hazır C# Master Prompt

> **Kullanım Yönergesi:** Aşağıdaki metni kopyalayıp Claude Code ortamına yapıştırarak projenizi başlatabilirsiniz.

```markdown
Sen kıdemli bir C# / .NET mimarısın. Okullarda TV, akıllı tahta ve dijital panolarda (Digital Signage) kesintisiz çalışacak, modüler, izgara (grid matrix) tabanlı bir **Okul Dijital Pano Sistemi** geliştireceksin.

### 📐 MİMARİ VE DÜZEN TASARIMI (Grid Matrix Yöntemi)
- **Ekran Düzeni:** Ekran, dinamik bir Izgara Matrisi (Grid Matrix - örn: 6x4, 8x4 veya 12x6 hücreli matris) olarak yönetilir.
- **Neden Izgara Matrisi?** TV ve pano ekranlarında serbest sürükle-bırak yöntemi modüllerin üst üste binmesine, taşmasına veya hizasız görünmesine yol açar. Izgara matrisi yöntemi ise tüm çözünürlüklerde pixel-perfect, düzenli, çakışmasız ve profesyonel bir görünüm garantiler.
- **Modül Konumlandırması:** Her modül `X` (sütun), `Y` (satır), `W` (genişlik/sütun sayısı) ve `H` (yükseklik/satır sayısı) değerlerine sahiptir.
- **Çakışma Önleme:** Bir modül eklenirken veya yeri değiştirilirken diğer modüllerin hücreleriyle çakışması engellenir veya otomatik kaydırma yapılır.

---

### 🎨 TEMA VE GÖRSEL TASARIM
- **Açık Tema (Light Mode) ve Koyu Tema (Dark Mode):** Uygulama her iki temada da yüksek kontrast ve okunabilirlik sunmalıdır.
- **Renk Paleti (Theme Colors):** Mavi (Blue), Zümrüt (Emerald), Kehribar (Amber), Gül (Rose), İndigo (Indigo), Menekşe (Violet), Macenta/Mor (Purple), Gökyüzü (Sky), Turkuaz (Teal), Turuncu (Orange), Kayrak (Slate).
- **Görsel Stil:** Yuvarlatılmış köşeler (border-radius), yumuşak gölgeler, neon Vurgu efektleri, okullar için yüksek kontrastlı net tipografi.

---

### 📋 ŞABLON VE DOSYA YÖNETİMİ
1. **Çoklu Şablon Yönetimi:** 
   - Kullanıcı birden fazla pano şablonu (örn: "Pazartesi Düzeni", "Tören Günü Düzeni", "Sınav Haftası") oluşturabilir, düzenleyebilir ve değiştirebilir.
   - Aktif şablon tek tıkla panoda yayına alınabilir.
2. **Şablon Yeniden Adlandırma:** 
   - Kullanıcı var olan şablonların adlarını dilediği zaman liste üzerinden doğrudan değiştirebilir.
3. **Konum Seçmeli Dışa Aktarma (Export):**
   - Kullanıcı "Şablonu Dışa Aktar" butonuna bastığında işletim sisteminin Dosya Kaydetme Diyaloğu (`SaveFileDialog`) açılır.
   - Varsayılan dosya adı aktif şablonun ismini içerir (Örn: `{Sablon_Adi}_şablonu_yedek.json`).
   - Varsayılan hedef klasör önerisi: `okulpanosu\Şablonlar` veya kullanıcının seçeceği herhangi bir klasör.
4. **İçe Aktarma (Import):**
   - `.json` formatındaki yedek dosyaları yüklenerek şablonlar ve pano verileri sisteme geri aktarılabilir.

---

### 🧩 DAHİLİ MODÜLLER (Widgets)
1. **Nöbetçi Öğretmen / İdari Kadro Modülü:** Günün nöbetçi öğretmenleri ve idarecilerini gösterir, gün değişimini otomatik algılar. Hafta sonu tatil mesajı verir.
2. **Kayan Duyuru / Haber Bandı (Ticker):** Alt veya üst bantta dinamik kayan duyurular.
3. **Nöbetçi Öğrenci Modülü:** Katlara göre nöbetçi öğrencilerin listesi.
4. **Haftanın En Temiz Sınıfları (Hijyen Ödülü):** Birincilik, ikincilik, üçüncülük kupalarıyla ödüllendirilen sınıflar.
5. **Ders & Teneffüs Zaman Sayacı (Bell Schedule / Timer):** Derse/teneffüse kalan süreyi canlı geri sayan sayaç.
6. **Yemek Listesi Modülü:** Günün menüsü, kalori değerleri ve besin öğeleri.
7. **Saat, Tarih ve Hava Durumu:** Canlı dijital/analog saat ve canlı hava durumu bilgisi.
8. **Tarihte Bugün:** Tarihteki önemli olaylar ve bilgilendirme kartları.
9. **Ayın / Haftanın Öğrencisi:** Fotoğraflı ve başarı açıklamalı tebrik kartı.
10. **Vefat / Başsağlığı / Tebrik Duyuruları:** Özel şablonlu duyuru kartları.
11. **Günün Sözü / Ayet / Hadis:** Motive edici özlü sözler.
12. **Resim / Video Galeri Slaytı:** Okul etkinlik fotoğrafları ve videoları.

---

### 💻 C# TEKNİK VERİ MODELLERİ (Data Models)

```csharp
public class BoardModule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } // Örn: "duty_list", "announcement", "food_menu"
    public string Title { get; set; }
    public int X { get; set; } // Izgara Sütun İndeksi
    public int Y { get; set; } // Izgara Satır İndeksi
    public int W { get; set; } // Genişlik (Hücre Sayısı)
    public int H { get; set; } // Yükseklik (Hücre Sayısı)
    public string ThemeColor { get; set; } = "blue";
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class Template
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public List<BoardModule> Modules { get; set; } = new();
}

public class GlobalBoardData
{
    public List<Template> Templates { get; set; } = new();
    public string ActiveTemplateId { get; set; }
    // Nöbetçiler, Yemek Menüsü, Sınıflar vb. genel okul verileri
}
```

### 🛠️ BEKLENEN ÇIKTI VE UYGULAMA MİMARİSİ
- Temiz MVVM / Clean Architecture desenine uygun C# kodu.
- Akıcı UI geçişleri ve performanslı render altyapısı.
- Konum seçmeli JSON kaydetme/yükleme fonksiyonları.
- Lütfen projeyi adım adım kur, gerekli ViewModel ve View sınıflarını ve UI bileşenlerini oluştur.
```

---

## 2. Detaylı Uygulama Mimarisi ve Teknik Şartname

### 2.1. Neden Izgara Matrisi (Grid Matrix) Yöntemi Tercih Edilmelidir?

| Özellik | Serbest Sürükle-Bırak (Freeform) | Izgara Matrisi (Grid Matrix) |
| :--- | :--- | :--- |
| **Hizalama ve Düzen** | Modüller 1px kayabilir, orantısız görünebilir. | Tam hizalı, pixel-perfect bloklar oluşur. |
| **TV / Pano Uyumluluğu** | Farklı ekran boyutlarında (1080p, 4K) taşmalar yaşanır. | Matris oranları korunur, ekrana tam oturur. |
| **Çakışma Yönetimi** | Modüller üst üste biner, altındaki içerik kapanır. | Hücre bazlı çakışma kontrolü ile düzen bozulmaz. |
| **Kullanım Kolaylığı** | Okul yöneticisi için milimetrik ayarlama zordur. | Sütun ve satır seçerek saniyeler içinde düzen kurulur. |

### 2.2. Şablon ve Dışa Aktarma Akışı

1. **Şablon Yeniden Adlandırma**:
   - Yönetim panelindeki şablon listesinde her şablonun yanında bir **Düzenle (Kalem)** butonu yer alır.
   - Tıklandığında şablon adı anında düzenlenebilir metin kutusuna dönüşür.
   - `Enter` veya `Kaydet` ile yeni isim kaydedilir.

2. **Gelişmiş Dosya Saklama (SaveFileDialog)**:
   - Dışa aktarma butonuna basıldığında C# tarafında `SaveFileDialog` (WPF/WinForms) veya platform dialog servisi tetiklenir.
   - Dosya adı önerisi: `{Aktif_Sablon_Adi}_şablonu_yedek.json`.
   - Kullanıcı hedef klasörü (örneğin `C:\okulpanosu\Şablonlar`) özgürce seçer.

---

## 3. C# / .NET Uygulama Örnek Kod Yapısı (WPF / Avalonia)

```csharp
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32; // WPF için SaveFileDialog

namespace OkulPanosu.Services
{
    public class TemplateExportService
    {
        public async Task ExportTemplateAsync(Template activeTemplate, GlobalBoardData globalData)
        {
            // Dosya adı güvenli hale getiriliyor
            string safeName = string.Join("_", activeTemplate.Name.Split(Path.GetInvalidFileNameChars()));
            string defaultFileName = $"{safeName}_şablonu_yedek.json";

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Dosyası (*.json)|*.json",
                FileName = defaultFileName,
                Title = "Şablonu Kaydedeceğiniz Konumu Seçin",
                InitialDirectory = @"C:\okulpanosu\Şablonlar" // Varsayılan klasör önerisi
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var exportData = new
                {
                    ExportDate = DateTime.Now,
                    ActiveTemplate = activeTemplate,
                    GlobalData = globalData
                };

                string jsonString = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                await File.WriteAllTextAsync(saveFileDialog.FileName, jsonString);
            }
        }
    }
}
```

---
*Hazırlayan: Google AI Studio - Okul Panosu Mimari Dökümantasyonu*
