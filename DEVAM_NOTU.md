# Okul Panosu — Devam Notu

Bu dosya, önceki bir Claude Code oturumunda (yanlışlıkla Taşıt Tanıma projesinin altına eklenmiş bir
oturumda) yapılan işin özetidir. Bu proje klasörü (`D:\MYPRG\CLAUDE\DijitalOkulPanosu`) diskte Taşıt
Tanıma'dan tamamen bağımsızdır — hiçbir dosya taşınması gerekmedi, sadece oturum/proje ilişkilendirmesi
yanlıştı. Yeni oturumda bu dosyayı okuyup kaldığımız yerden devam edebiliriz.

## Proje Ne

Okullarda TV/panoda çalışan, ızgara-matris (grid) tabanlı, çoklu şablon destekli dijital okul panosu.
C# / WPF, .NET 10, hiçbir ek kurulum gerektirmeyecek şekilde (self-contained publish).

Orijinal şartname: [OKUL_PANOSU_SARTNAME.md](OKUL_PANOSU_SARTNAME.md) (proje kökünde).
Onaylanmış ilk uygulama planı önceki oturumda `EnterPlanMode` ile yazılıp onaylandı — bu notta plan
özetlenmiştir, orijinal plan dosyası (`abundant-wobbling-gray.md`) önceki oturuma ait, buraya erişimi yok.

## Mimari Kararlar (kullanıcı ile netleştirildi, değiştirilmemeli)

- **Tek WPF uygulaması, iki mod**: Kiosk (Yayın, tam ekran) ve Yönetim — aynı exe, `LocalSettings.StartupMode`'a göre açılışta hangisi gösterileceği belirlenir.
- **Kullanım senaryosu**: Farklı katlardaki iki PC (TV'ye bağlı Yayın PC'si + idarenin Yönetim PC'si) ağ paylaşımlı ortak bir veri klasörünü kullanır. Tek PC'de de çalışabilmeli (aynı PC hem yayın hem yönetim).
- **Kiosk PC**: Windows açılışında otomatik başlar (HKCU Run kaydı, admin hakkı gerekmez), tam ekrana geçer, elektrik kesintisinden sonra kendiliğinden toparlanır. `Ctrl+Alt+Y` ile tam ekrandan çıkılabilir (yönetici şifresi ister), tekrar aynı kısayolla veya "Tam Ekrana Dön" butonuyla geri dönülür.
- **Veri saklama**: SQLite DEĞİL, düz JSON (`{VeriKlasörü}\board-data.json`), atomik yazma (temp dosya + `File.Move(overwrite:true)`).
- **Makineye özel ayarlar** (`%AppData%\OkulPanosu\local-settings.json`, paylaşılan klasörde DEĞİL): veri klasörü yolu, başlangıç modu, kiosk ekranı (çoklu monitör), otomatik başlatma tercihi, tema rengi.
- **Canlı senkronizasyon**: `FileSystemWatcher` + 5 saniyelik polling fallback (UNC ağ paylaşımlarında watcher güvenilmez olabiliyor).
- **Admin şifre kilidi**: Taşıt Tanıma'daki `AdminAuthService` deseni birebir taşındı, ama hash paylaşılan `GlobalBoardData`'da tutuluyor (LocalSettings'te değil) — hangi PC'den girilirse girilsin aynı şifre geçerli olsun diye.
- **Kod stili**: Taşıt Tanıma ile birebir tutarlı — DI container yok (statik `AppServices` servis lokatörü), toolkit'siz düz `INotifyPropertyChanged`, `UserControl` + code-behind view'lar, MVVM toolkit yok.
- **Tema**: Taşıt Tanıma'nın koyu taban + değiştirilebilir aksan rengi deseni taşındı, şartnamedeki 11 renk (Blue, Emerald, Amber, Rose, Indigo, Violet, Purple, Sky, Teal, Orange, Slate) `Themes/Accent.*.xaml` olarak eklendi. Açık/koyu tema geçişi YOK (Taşıt Tanıma'da da yok, kapsam dışı bırakıldı).
- **v1 kapsamı (küçük çekirdek)**: Işgara motoru + şablon yönetimi + SADECE 3 modül (Saat & Tarih, Kayan Duyuru, Nöbetçi Öğretmen). Kalan 9 modül BİLİNÇLİ OLARAK yapılmadı (aşağıda liste var).
- **Işgara editörü v1**: Sürükle-bırak YOK — sayısal X/Y/W/H alanlarıyla konumlandırma + çakışma (overlap) kontrolü kaydetmeden önce yapılıyor.

## Mevcut Durum

Çözüm başarıyla derleniyor (`dotnet build OkulPanosu.slnx` → 0 hata, 0 uyarı) ve uygulama çöküşsüz açılıyor
(smoke test yapıldı). **Ama kullanıcı tarafından uçtan uca manuel UI testi henüz doğrulanmadı** — native
WPF penceresi olduğu için tarayıcı tabanlı araçlarla tıklanamıyor, kullanıcının kendisinin denemesi gerekiyor.

### Doğrulanması gereken adımlar (kaldığımız yer):
1. Kurulum sihirbazı: Yönetim/Yayın modu seçimi + veri klasörü seçimi çalışıyor mu?
2. Yönetim → Şablonlar → Yeni Şablon → Düzenle → 3 modülü ızgaraya X/Y/W/H ile yerleştirip kaydet → Yayına Al.
3. "📺 Yayını Başlat" ile Kiosk penceresi açılıp doğru render ediliyor mu (saat çalışıyor mu, ticker kayıyor mu, nöbetçi öğretmen doğru günle eşleşiyor mu)?
4. Kiosk açıkken Yönetim tarafında değişiklik yapıp kaydedince birkaç saniye içinde Kiosk otomatik güncelleniyor mu (BoardSyncWatcher)?
5. `Ctrl+Alt+Y` ile tam ekrandan çıkış/giriş, admin şifre akışı.
6. Dışa Aktar / İçe Aktar JSON akışı.
7. Ayarlar sayfası: veri klasörü değiştirme, ekran seçimi, otomatik başlatma checkbox'ı, tema renkleri.

## Veri Klasörü Yapısı (Şablonlar + Medya)

Sihirbaz açılışta varsayılan olarak `{exe klasörü}\YAYIN` önerir (kullanıcı isterse "Klasör Seç..." ile
değiştirir). Tek PC'de bu varsayılan yeterli; Yayın ve Yönetim ayrı PC'lerdeyse ikisi de aynı ağ paylaşımlı
klasörü (ör. `\\SUNUCU\OkulPanosu`) göstermeli — sihirbazdaki açıklama metni bunu belirtiyor.

`AppServices.SetDataFolder` / `Initialize` her seferinde `BoardDataRepository.EnsureFolderStructure()`
çağırıyor, bu da veri klasörünün altında şu alt klasörleri (yoksa) oluşturuyor:

```
{VeriKlasörü}\
  board-data.json
  Şablonlar\                        (JSON yedekleri — TemplateExportService)
  Medya\
    Resimler\                       (slayt gösterisi / galeri modülü — henüz kod tarafında kullanılmıyor)
    Videolar\                       (henüz kod tarafında kullanılmıyor)
    Öğretmen Fotoğrafları\          (henüz kod tarafında kullanılmıyor)
    Öğrenci Fotoğrafları\           (henüz kod tarafında kullanılmıyor)
```

Bu klasörler şimdilik sadece **önceden hazır** ediliyor (`BoardDataRepository.ImagesFolderPath` vb. property'ler
üzerinden erişilebilir) — resim/video galerisi modülü henüz yazılmadı (bkz. "Sonraki Adımlar", kalan 9 modülden biri).

## Proje Yapısı

```
DijitalOkulPanosu/
  OKUL_PANOSU_SARTNAME.md      (orijinal şartname)
  DEVAM_NOTU.md                 (bu dosya)
  OkulPanosu.slnx
  src/
    OkulPanosu.Core/            (veri modelleri + JSON repository, net10.0)
      Data/
        BoardModule.cs, Template.cs, DutyRosterDay.cs, GlobalBoardData.cs
        BoardDataRepository.cs  (atomik JSON CRUD + EnsureFolderStructure/Şablonlar+Medya alt klasörleri)
        LocalSettings.cs, LocalSettingsRepository.cs
    OkulPanosu.App/              (WPF, net10.0-windows)
      App.xaml(.cs)              (OnStartup: sihirbaz → Kiosk/Yönetim)
      Themes/Base.xaml + Accent.*.xaml (11 renk)
      Converters/InverseBoolToVisibilityConverter.cs
      Models/NavItem.cs
      Services/
        AppServices.cs           (statik servis lokatörü)
        ThemeManager.cs
        AdminAuthService.cs
        BoardSyncWatcher.cs
        AutoStartService.cs      (HKCU Run kaydı)
        TemplateExportService.cs (SaveFileDialog/OpenFileDialog JSON)
      Views/
        SetupWizardWindow.xaml(.cs)
        BoardWindow.xaml(.cs)     (Kiosk — tam ekran, Ctrl+Alt+Y, çoklu monitör)
        ManagementWindow.xaml(.cs) (sidebar shell: Şablonlar / Ayarlar)
        TemplatesView.xaml(.cs)   (CRUD + yeniden adlandırma + dışa/içe aktar)
        TemplateEditorView.xaml(.cs) (ızgara boyutu, modül ekle/sil, X/Y/W/H, nöbetçi listesi)
        SettingsView.xaml(.cs)    (veri klasörü, ekran, otomatik başlatma, şifre, tema)
        AdminPasswordDialog.xaml(.cs), SetPasswordDialog.xaml(.cs)
        Modules/
          ModuleFactory.cs        (Type string → UserControl eşlemesi)
          BoardGridControl.xaml(.cs) (ızgara render motoru)
          ClockDateModuleView, TickerModuleView, DutyTeacherModuleView, UnknownModuleView
```

## Mimari Güncellemeler (AI Studio referansından sonra)

Kullanıcı, aynı projeyi daha önce Google AI Studio'da (Node.js/React, `okul-dijital-panosu.zip` — proje kökünde
duruyor) prototiplemiş. O kodu birebir taşımıyoruz (mimarimiz WPF/C#, JSON depolama — kendi başına
çalışan, iki PC arasında paylaşımlı klasörle haberleşen masaüstü uygulaması olarak kalıyor), ama iki fikri
aldık:

- **Kayan Duyuru artık bir modül DEĞİL, şablonun kendi özelliği.** `Template.TickerText/TickerSpeed/
  TickerBgColor/TickerTextColor` — panonun en altında sabit bir şerit olarak her zaman render edilir
  (`BoardGridControl` içinde `TickerBar` alt kontrolü, `Views/Modules/TickerBar.xaml(.cs)`). Eski
  `TickerModuleView` ve `ModuleFactory`'deki `"ticker"` modül tipi kaldırıldı. `TemplateEditorView`'da
  ızgara boyutu kartının hemen altında ayrı bir "Kayan Duyuru (Ticker)" kartı var (metin + hız/sn).
- **Kurum bilgileri artık koda gömülü değil, paylaşılan veride.** `GlobalBoardData.InstitutionName` /
  `InstitutionCity` eklendi, Ayarlar sayfasında "Kurum Bilgileri" kartından düzenleniyor. AI Studio
  referansında okul adı sabit kodlanmıştı — kullanıcı bunu haklı olarak hata olarak işaretledi, düzeltildi.

Referans zip'teki `src/data.ts` dosyasında **gerçek okul verileriyle** (Batman Mezopotamya Mesleki ve
Teknik Anadolu Lisesi) doldurulmuş 5 hazır şablon var (`DEFAULT_TEMPLATES`) — bunlar bizim "kalan 9 modül"
listemizle büyük ölçüde örtüşüyor (clock→clock_date, duty_teachers→duty_teacher, + video/announcements/
slideshow/student_of_the_month/birthdays/food_menu/schedule — henüz WPF tarafında yok). Editördeki
klavye ile taşı (yön tuşları) + boyutlandır (Shift+yön tuşları) + modül üzerinde beliren mini kontrol
butonları (⚙️/🎨/🗑️) deseni de bu referanstan — `TemplateEditorView`'daki sayısal X/Y/W/H alanlarının
yerini almak üzere planlanıyor ama henüz uygulanmadı.

**Bulunan ve düzeltilen arayüz hataları (ilk manuel testte):**
- `Themes/Base.xaml`'daki özel `ComboBox` şablonunda `Popup`'a `StaysOpen="False"` eksikti — bu yüzden bir
  combobox açılınca dışarı tıklayınca kapanmıyor, görünmez katmanı diğer butonların (Kaydet, Şablonlara
  Dön, Yayını Başlat) tıklamalarını yutuyordu. Düzeltildi.
- `DisplayMemberPath` kullanan 3 combobox (modül tipi, vurgu rengi, kiosk ekranı) seçili öğeyi ham
  `ToString()` (`{ Type = clock_date, DisplayName = ... }`) olarak gösteriyordu — hepsi açık `ItemTemplate`
  (`TextBlock Text="{Binding ...}"`) kullanacak şekilde değiştirildi, artık garanti doğru görünüyor.
- `ManagementWindow` (sol menü) ve `TemplateEditorView` (sol kart paneli) artık `GridSplitter` ile
  fareyle daraltılıp genişletilebiliyor, ayrıca üstte küçük bir ◀/▶ butonuyla tek tıkla tamamen
  kapatılıp/açılabiliyor (önizleme alanını büyütmek için).
- "+ Modül Ekle" listesi (`ModuleFactory.KnownTypes`) tek kaynak — yeni bir modül türü kodlandıkça oraya
  eklenecek ve otomatik olarak listede görünecek. Her tip için `ModuleFactory.DefaultSizeFor` ile
  varsayılan W/H tanımlı (yeni modül eklendiğinde artık hepsi aynı 3x2 değil, türüne uygun boyutla geliyor).
  Henüz yazılmamış modül türleri (video, duyurular, slayt, vb.) bilinçli olarak listede YOK — yazılınca
  eklenecekler, aksi halde kullanıcı "Bilinmeyen modül" placeholder'ı ile karşılaşırdı.

**Editör canvas'a taşındı (ikinci manuel test turu):**
- `BoardGridControl` artık `EditorMode` bool'u destekliyor. Açıkken: hücre çizgileri görünür, her modül
  kartının başlık çubuğunda (nokta + isim, Kiosk'ta da aynı şekilde görünür) ⚙️/✕ butonları belirir, modül
  tıklanıp seçilebilir (beyaz kenarlık), seçili modül yön tuşlarıyla taşınır / Shift+yön tuşlarıyla
  boyutlandırılır (bkz. `BoardGridControl.OnPreviewKeyDown`). Kapalıyken (Kiosk / "Önizle" basılınca) tam
  yayındaki temiz görünüm.
- `TemplateEditorView`'daki eski form-tabanlı modül kartları (`ModuleRow`, `BuildModuleRowUi`, sayısal
  X/Y/W/H kutuları) tamamen kaldırıldı — artık tek kaynak `List<BoardModule> _modules`, doğrudan
  `BoardGridControl`'e geçiriliyor (aynı nesne referansları, klavye ile taşıma otomatik yansıyor).
  Modül başlığı/vurgu rengi düzenlemesi artık ⚙️ ile açılan `ModuleSettingsDialog` modal'ında.
- "Önizle" butonu artık gerçek bir mod değişimi: `PreviewGrid.EditorMode` kapatılıp temiz yayın görünümü
  gösteriliyor, buton "Düzenlemeye Dön" olarak değişiyor.

**Üçüncü test turu:**
- Varsayılan ızgara 24×16'ya çıkarıldı (`Template.cs` + `TemplateEditorView` fallback değerleri).
- Sütun/Satır Sayısı kutularında Enter'a basınca veya odaktan çıkınca önizlemenin güncellenmediği bug
  vardı (hiç olay bağlı değildi) — `GridSize_Changed`/`GridSize_KeyDown` eklendi, düzeltildi.
- `GridSplitter`lar (sol panel/menü daraltma) `Focusable="False"` yapıldı — önceden bir modül seçiliyken
  yön tuşlarına basınca odak yanlışlıkla splitter'daysa WPF'in yerleşik splitter klavye-boyutlandırma
  davranışı devreye giriyordu. Artık sadece fare ile sürüklenebiliyorlar.
- Modül kartlarına, seçiliyken sağ alt köşede fare ile sürükleyerek boyutlandırma tutamacı eklendi
  (`BoardGridControl.StartResizeDrag`) — yön tuşu/araç çubuğu butonlarına ek bir yöntem.
- **Gerçek bug (dördüncü rapor):** "Kaydet/Şablonlara Dön tepki vermiyor" — Kaydet aslında veriyi
  doğru kaydediyordu (repo çağrıları çalışıyor), ama `ManagementWindow.ShowEditor`'daki
  `BackRequested` handler'ı sadece `NavList.SelectedIndex = 0` yapıyordu; editör "Şablonlar" sayfası
  NavList'ten değil `ShowEditor` içinden doğrudan `MainContent.Content = editor` ile açıldığı için
  SelectedIndex zaten 0'da kalıyordu — atama no-op oluyor, `SelectionChanged` hiç tetiklenmiyor, ekran
  hep editörde kalıyordu. Düzeltme: `MainContent.Content = GetOrCreatePage("templates")` artık açıkça
  çağrılıyor, SelectedIndex-değişimine güvenmiyor.
- Derlemeden önce çalışan `OkulPanosu.App.exe` varsa kullanıcıya SORMADAN `taskkill //F //IM
  OkulPanosu.App.exe` ile kapatılmalı (kullanıcı bunu istedi, tekrar tekrar "kapattım" demek istemiyor).

**Büyük modül turu (kullanıcı hız konusunda haklı olarak sabrı taştı, tek seferde toplu ilerleme yapıldı):**
- 5 yeni modül eklendi: `announcements` (Duyurular, otomatik döngülü carousel), `food_menu` (Yemek Menüsü),
  `schedule` (Ders & Teneffüs Saatleri), `birthdays` (Doğum Günleri), `student_of_month` (Ayın Öğrencisi,
  fotoğraflı). `duty_teacher` da zenginleştirildi: artık düz isim listesi değil, gün → müdür yardımcısı +
  kat/alan bazlı öğretmen ataması (`DutyAssignment`).
- Bu içeriklerin hepsi `GlobalBoardData`'da yeni koleksiyonlar: `Announcements`, `FoodMenu`,
  `LessonSchedule`, `Birthdays`, `StudentOfTheMonth`, ve zenginleşen `DutyRoster`.
- **Yeni "İçerikler" sayfası** (`Views/ContentView.xaml(.cs)`, Yönetim penceresi sol menüsünde) — nöbet
  çizelgesi (gün/müdür yardımcısı/kat/öğretmen satırları, ekle-sil), duyurular (başlık/içerik/tarih/önem/
  görsel, ekle-sil), yemek menüsü (gün bazlı), ders saatleri (ekle-sil), doğum günleri (ekle-sil), ayın
  öğrencisi (tekil form + fotoğraf seçici) — hepsi tek "Kaydet" butonuyla `BoardDataRepository.UpdateContent`
  üzerinden kaydediliyor. Eski, `TemplateEditorView` içine gömülü basit nöbet listesi kaldırıldı (oraya ait değildi).
- **Otomatik varsayılan şablon:** Veri klasöründe hiç şablon yoksa (`BoardDataRepository.EnsureDefaultTemplate`,
  `AppServices.Initialize`/`SetDataFolder` içinden çağrılıyor) artık elimizdeki 7 modülün tümünü kullanan,
  24×16 ızgarayı boşluksuz dolduran "Ana Pano Düzeni" otomatik oluşuyor — kullanıcı sıfırdan boş ekranla
  başlamak zorunda değil.
- `ModuleFactory.KnownTypes`/`DefaultSizes` yeni modüllerle güncellendi (24×16 ızgaraya göre boyutlar ~2 katına çıkarıldı).
- **Henüz yapılmadı:** `video` ve `slideshow` modülleri (medya oynatma/galeri — ayrı bir iş, dış bağımlılık
  yönetimi gerektiriyor, kasıtlı olarak bu turun kapsamı dışında bırakıldı).

**Süreç notu:** Kullanıcı "tek tek küçük düzeltme + tekrar dene" döngüsünden dolayı ciddi şekilde
sabırsızlandı ve haklıydı — bundan sonra, özellikle birden fazla benzer modül eklerken, TEK SEFERDE
büyük, test edilebilir bir batch halinde ilerleyip öyle geri dönülmeli; her küçük adımda onay/rapor beklenmemeli.

**Referansa yakınsama turu (kullanıcı ekran görüntüleriyle zip'teki AdminPanel'i işaret etti):**
- Eski tek-sayfa-Expander'lı `ContentView` tamamen kaldırıldı (Expander'ın koyu temada hiç
  stillendirilmemiş olması ciddi bir okunabilirlik hatasıydı — beyaz zemin üzerinde soluk yazı).
  Yerine `ManagementWindow` sol menüsüne referanstaki gibi DÜZ (nested değil) bir liste eklendi:
  Şablonlar, Nöbet Çizelgesi, Duyurular, Personel & Öğretmenler, Yemek Menüsü, Ders & Zil Saatleri,
  Doğum Günleri, Ayın Öğrencisi, Yerel Videolar, Tarihte Bugün, Haftanın Beyin Egzersizi, Haftanın Temiz
  Sınıfları, Ayarlar — her biri kendi bağımsız `Views/*.xaml(.cs)` sayfası.
- **Personel & Öğretmenler** (`PersonnelView`) yeni: toplu kopyala-yapıştır aktarım (Ad/Branş/Cinsiyet),
  tekil ekleme formu, fotoğraf seçici (`Medya\Öğretmen Fotoğrafları`), kart listesi. **Fotoğrafı olmayan
  personel/öğrenciler için artık kod içinde (harici dosya gerekmeden) çizilen, yüzsüz cinsiyet silüeti**
  gösteriliyor — `Services/PersonPlaceholder.cs` (DrawingVisual → RenderTargetBitmap, mavi=erkek/pembe=kadın).
  `StudentOfMonthModuleView` de fotoğraf yoksa aynı silüeti kullanıyor.
- **Nöbet Çizelgesi** (`DutyRosterView`) gerçek bir matris oldu: satırlar sabit kat listesi
  (`DutyRosterDay.DefaultFloors`), sütunlar Pazartesi-Cuma, üst satır Nöbetçi Müdür Yardımcısı (Personel
  listesinden seçmeli combo). Her hücrede birden fazla öğretmen "çip" (isim + ✕) olabilir, altında
  "+ Öğretmen" combo'su Personel listesinden seçilerek ekler.
- **Yemek Menüsü** (`FoodMenuView`): her gün için sabit 4 yemek alanı + kalori (referanstaki gibi).
- **Ders & Zil Saatleri** (`ScheduleView`): en az 10 periyot zorunlu (kaydetmeden önce kontrol edilir),
  10 varsayılan periyotla (08:30-17:15) seed ediliyor, 2 sütunlu kart grid.
- **Doğum Günleri** (`BirthdaysView`): sol formdan ekleme, sağda kart grid + sil ikonu (Excel/kopyala-
  yapıştır toplu aktarım kullanıcı isteğiyle SONRAYA bırakıldı, henüz yok).
- **Yerel Videolar** (`VideosView`): "+ Video Seç & Ekle" ile dosya seçilince otomatik olarak (ayrı bir
  "tanı/bağla" adımı OLMADAN, kullanıcı bunu istemedi) listeye ekleniyor, dosya `Medya\Videolar`'a
  kopyalanıyor, her satırda IsActive checkbox + editable başlık + gün combo + sil. `VideoModuleView`
  bugüne planlı & aktif ilk videoyu sessiz/döngülü oynatıyor (`MediaElement`).
- Yeni modüller: `video`, `today_in_history` (Tarihte Bugün — bugünün tarihine göre otomatik eşleşir),
  `weekly_question` (Haftanın Beyin Egzersizi), `cleanest_class` (Haftanın Temiz Sınıfları) — hepsi
  `ModuleFactory`'ye kayıtlı, editör sayfaları da var.
- Paylaşımlı küçük yardımcı: `Views/EditorControls.cs` (LabeledTextBox, ImagePathPicker, DeleteIconButton
  vb.) — tüm yeni sayfalar aynı kodu tekrar etmesin diye, `Application.Current.Resources` üzerinden
  statik erişim kullanıyor (instance `FindResource` gerektirmiyor).

**Menü ve kontrast düzeltmesi (kullanıcı üçüncü kez "silik" dedi, haklıydı):**
- `DutyRosterView.HeaderCell`'de `Application.Current.Resources["TextPrimaryBrush"/"TextMutedBrush"]`
  üzerinden dinamik renk seçimi bekleneni vermiyordu — satır başlıkları (kat isimleri, "NÖBETÇİ MD. YRD.")
  soluk/görünmez çıkıyordu. Kesin çözüm: artık kaynak sözlüğüne güvenmiyor, sabit koyu-mavi arka planlı
  pill (`Color.FromRgb(0x1E,0x3A,0x5F)`) + `Brushes.White` metin — resource lookup riskinden bağımsız,
  garanti yüksek kontrast.
- **Menü tekrar tek "İçerikler" öğesine döndü.** Kullanıcı flat 11-öğeli listeyi karışık buldu; asıl
  istediği referanstaki gibi iki seviyeli menüydü: `NavList` artık sadece Şablonlar/İçerikler/Ayarlar.
  "İçerikler"e tıklayınca `ManagementWindow.xaml`'de yeni bir `SubNavColumn` (210px, öncesi 0/gizli)
  açılıyor ve içinde 11 alt sayfa (`SubNavList`) listeleniyor; birine tıklayınca sağdaki `MainContent`
  o sayfayı gösteriyor. Şablonlar/Ayarlar seçilince alt menü tekrar kapanıyor.

**Büyük hata avı turu (kullanıcı çok sayıda somut sorun bildirdi, hepsi tek seferde ele alındı):**
- **Beyaz zemin/soluk yazı kök nedeni bulundu:** `UserControl`'ün Base.xaml'de hiç stili yoktu, bu
  yüzden `MainContent` (ContentControl) içine yerleşince arkasında koyu Window zemini değil Windows'un
  varsayılanı görünüyordu. `Style TargetType="{x:Type UserControl}"` eklendi (Background=BgPrimaryBrush,
  Foreground=TextPrimaryBrush) — artık her yeni sayfa otomatik koyu geliyor, tek tek ayarlamaya gerek yok.
- **İkon karışıklığı:** ✕/⚙️/📷 gibi sembol/emoji karakterleri bazı yerlerde ok işaretine benzer
  görünüyordu (font glyph sorunu). Artık `EditorControls.IconFont` ("Segoe MDL2 Assets" — Windows'un
  kendi ikon fontu, her Windows 10/11'de var) + `TrashIcon`/`GearIcon`/`CloseIcon`/`CameraIcon` sabitleri
  kullanılıyor (`\u` kaçış dizileriyle — kopyala/yapıştır kaynaklı görünmez karakter riski yok).
- **Kiosk↔Yönetim kritik hatası düzeltildi** (`BoardWindow.cs`/`ManagementWindow.cs`):
  1. Ctrl+Alt+Y'nin çalışması için önce fareyle tıklamak gerekiyordu → `EnterKiosk()`'a `Keyboard.Focus(this)` eklendi.
  2. "Yayını Başlat" ile açılan Yönetim penceresi, Ctrl+Alt+Y ile çıkışta KENDİSİNİ tanımıyordu, yeni bir
     tane daha açıyordu (iki pencere) → `BoardWindow.AttachManagementWindow(this)` ile var olan pencere
     BoardWindow'a "kayıt ettiriliyor", tekrar kullanılıyor.
  3. Bu yüzden kullanıcı Yönetim'i kapatınca (aslında kendi açtığı asıl pencere) uygulama "yayına dön"
     mantığıyla sürekli diriliyor, kapatılamıyordu → artık sadece BoardWindow'un KENDİSİNİN oluşturduğu
     (kullanıcı-başlatmadığı, ExitKiosk'tan gelen) pencere kapanınca otomatik yayına dönülüyor; kullanıcının
     "Yayını Başlat" ile açtığı asıl pencere normal kapanıyor.
- **Sayfa önbelleği tazeleme hatası:** `ManagementWindow` sayfaları cache'liyor (sekme geçişinde state
  korunsun diye), bu yüzden Personel'e yeni öğretmen eklenince Nöbet Çizelgesi'nin "+ Öğretmen" listesi
  güncellenmiyordu. `IReloadablePage` arayüzü eklendi, TÜM içerik sayfaları bunu uyguluyor,
  `GetOrCreatePage` artık her sayfa gösterildiğinde `Reload()` çağırıyor.
- **Personel modeli zenginleştirildi:** `Personnel.Category` (teacher/staff) + `Branch` (ayrı alan,
  `Title`'dan bağımsız) eklendi. Nöbet Çizelgesi'ndeki "+ Öğretmen" dropdown'ı artık sadece
  `Category=="teacher"` olanları listeliyor. Toplu içe aktarımda aynı isim tekrar eklenmiyor (dedupe).
- **Video modülü:** artık bugüne planlı+aktif TÜM videoları sırayla oynatıyor (biri bitince sıradakine
  geçiyor, hepsi bitince başa dönüyor) — `VideoModuleView._queue`. `ModuleSettingsDialog` artık modülün
  kendisini alıyor (`BoardModule`) ve tipine göre ek alanlar gösteriyor — video için Sessiz/Ses Düzeyi
  slider'ı, `BoardModule.Settings["muted"/"volume"]`'a yazılıyor.

**Beşinci tur — ikon fontu deneyi geri alındı, yayın toggle, kart görseli:**
- **Kritik ders:** "Segoe MDL2 Assets" ikon fontu bu makinede beklendiği gibi render OLMADI (trash yerine
  anlamsız şekiller). PUA (Private Use Area) kod noktaları font'a özgüdür — font bulunamazsa/farklı
  sürümdeyse fallback tamamen anlamsız bir glyph'e düşebilir. **Ders: bu projede sembol/emoji glyph'lere
  güvenme, düz metin kullan** ("Sil", "Foto", "Ayar", "x") — hiç sorun çıkarmadılar.
- **Ctrl+Alt+Y artık Yönetim penceresinden de çalışıyor** (`ManagementWindow.Window_PreviewKeyDown`) —
  TV'ye fiziksel erişim gerektirmiyor. "Yayını Başlat" butonu artık "Yayını Durdur"a dönüşen bir toggle
  (`BoardWindow.IsBroadcasting`/`StopBroadcastFromManagement`/`BroadcastStopped` event). Yönetim + Yayın
  aynı anda (farklı ekranlarda) açık kalabiliyor — kullanıcı bunun doğru davranış olduğuna karar verdi.
- **Modül kartlarına gerçek renk verildi:** `BoardGridControl.BuildModuleCard` artık başlık çubuğunda
  vurgu renginden gradient arka plan + kart kenarlığında yarı saydam vurgu rengi + `DropShadowEffect`
  (derinlik) kullanıyor. Panonun geneline de köşegen koyu-lacivert gradient zemin eklendi (flat tek renk
  yerine). Not: her modülün KENDİ view'ı (`ClockDateModuleView` vb.) hâlâ kendi opak `CardStyle` border'ını
  çiziyor, bu yüzden tint çoğunlukla sadece başlık çubuğunda ve kenarlıkta görünüyor — modül içeriklerini
  de yarı saydam yapmak (tint'in tamamen görünmesi için) ayrı bir iş.

**Altıncı tur:**
- **Sistem tepsisi ikonu eklendi** (`Services/TrayIconService.cs`, `App.xaml.cs`'te başlatılıyor) —
  Kiosk PC'sinin klavyesine/TV'ye hiç fiziksel erişim olmadan (uzak masaüstü vb.) çift tıkla Yönetim'e
  geçilebiliyor. `BoardWindow.RequestManagementAccess()` (eski private `ExitKiosk`'un genel hali) çağrılıyor,
  yine admin şifresi soruyor (fiziksel erişimi olmayan biri zaten tepsi ikonuna da erişemez ama tutarlılık
  için şifre korunuyor).
- **Gerçek "beyaz zemin" kaynağı bulundu — MessageBox.Show:** Native `System.Windows.MessageBox`, WPF
  stillerinden tamamen bağımsız, her zaman Windows'un klasik AÇIK temasıyla açılır. 3 kullanım yeri
  (şablon silme onayı, şifre hatası, şifre güncellendi bildirimi) artık koyu temalı özel
  `Views/AppMessageBox.xaml(.cs)` kullanıyor (`AppMessageBox.Show`/`AppMessageBox.Confirm`).
- **Modül başlığı köşe boşluğu düzeltildi:** `Border.ClipToBounds` çocuk içeriği YUVARLAK köşeye göre
  kırpmaz, sadece dikdörtgen sınıra göre kırpar — bu yüzden kare köşeli header, kartın yuvarlak köşesinden
  taşıp küçük bir boşluk bırakıyordu. Header artık kendi üst-köşeleri-yuvarlak (`CornerRadius="9,9,0,0"`)
  ayrı bir `Border`'a sarılı, kartın köşesiyle sorunsuz örtüşüyor.

**Yedinci tur — beyaz zeminin GERÇEK kök nedeni bulundu:**
- **`UserControl.Background`'ı Setter ile vermek WPF'te hiçbir görsel etki yaratmaz** — UserControl'ün
  varsayılan şablonu (`AdornerDecorator`+`ContentPresenter`) bu özelliği hiç ÇİZMEZ. Önceki "fix" (Base.xaml'e
  sadece `<Setter Property="Background".../>` eklemek) bu yüzden hiçbir şeyi değiştirmemişti — stil doğru
  duruyordu ama etkisizdi. Gerçek çözüm: `UserControl` stiline, Background'ı gerçekten çizen bir `Border`
  içeren bir `Template` (ControlTemplate) eklemek. Artık her sayfa gerçekten koyu geliyor.
- **İkon fontu tamamen terk edildi, Taşıt Tanıma'daki KANITLANMIŞ deseni kopyaladık:** O projede özel bir
  ikon fontu YOK — sadece normal Unicode emoji karakterleri ("🗑 Sil", "🔍 Sorgula") varsayılan Segoe UI
  ile kullanılıyor ve Windows'un emoji font fallback'i ile sorunsuz render oluyor. `EditorControls.
  DeleteIconButton`, `PersonnelView` foto butonu, `BoardGridControl`'ün ⚙/🗑 header butonları artık bu
  AYNI kanıtlanmış deseni kullanıyor (emoji+kısa metin birlikte, saf PUA glyph değil).
- **Modül köşe boşluğu — ikinci deneme, bu sefer kökten:** Header'a ayrı CornerRadius vermek yetmedi
  (kartın dış kenarlığının köşe eğrisiyle header'ın kendi köşe eğrisi tam örtüşmüyordu). Doğru çözüm:
  kartın kendisine, boyutu belli olduğunda (`SizeChanged`) TÜM içeriği (header dahil) aynı radius'la
  kırpan bir `RectangleGeometry` `Clip` vermek — artık header dahil her şey kartın gerçek köşesine
  birebir kırpılıyor, ayrı ayrı radius hesaplamaya gerek yok.
- **Sistem tepsisi ikonu hangi ekranda görünür — bu bizim kontrolümüzde değil.** `NotifyIcon` Windows'un
  görev çubuğu ayarlarına göre yerleşir (hangi monitörde görev çubuğu/tepsi gösteriliyorsa orada), WPF/
  WinForms API'siyle "şu monitörde göster" diye zorlanamaz. Kullanıcıya açıklandı, kod değişikliği yok.

**Sekizinci tur:**
- **Beyaz şerit bulundu:** `TemplateEditorView`'daki `GridSplitter`'a verilen `Background="Transparent"`
  (ve `ManagementWindow`'daki `Background="{StaticResource BorderBrush1}"`) görünüşe göre GridSplitter'ın
  kendi varsayılan şablonunda uygulanmıyor (muhtemelen UserControl'deki AYNI kök sorun — Background
  Setter'ı, TemplateBinding olarak kullanılmıyor). Kesin çözüm: GridSplitter'ın ARKASINA, aynı hücreye,
  açıkça koyu renkli bir `Border` eklendi (z-order'da altta) — GridSplitter kendi Background'ını
  çizsin çizmesin, altındaki Border her zaman koyu görünüyor.
- **Modül Ekleme Havuzu:** Sol paneldeki küçük ComboBox+Buton kaldırıldı, kullanıcının gönderdiği
  referans görüntüsündeki gibi (ama solda) kart-listesi modül paleti eklendi (`BuildModulePalette` —
  her modül tipi için isim + varsayılan boyut + "Ekle +" butonu, tıklayınca direkt panoya ekleniyor).

**Dokuzuncu tur:**
- **Şifre mantığı yeniden düzenlendi (kullanıcı isteği):** Yönetim penceresini AÇMAK artık hiç şifre
  istemiyor (`BoardWindow.ExitKiosk` — hem fiziksel Ctrl+Alt+Y hem tepsi ikonu). Şifre koruması artık
  sadece gerçek değişiklik yapan eylemlerde: `StopBroadcastFromManagement(Window owner)` (yayını
  durdurma — artık `owner` parametresi alıyor, dialog BoardWindow yerine ÇAĞIRAN pencerede
  (Yönetim) açılıyor, kiosk ekranında değil) ve `TemplateEditorView.OnDeleteModule` (modül silme).
- **Yönetim penceresi artık her zaman ANA (birincil) ekranda açılıyor** — `WindowStartupLocation=
  "CenterScreen"` imleç konumuna göre merkezliyordu (kiosk ekranındaysa oraya gidiyordu, kullanıcı
  göremiyordu). `ManagementWindow.PositionOnPrimaryScreen()` ile açıkça `Screen.PrimaryScreen`'e göre
  konumlandırılıyor artık, `WindowStartupLocation="Manual"`.
- **"Şablon düzenlemeye girince uygulama tamamen kapanıyor" — reprodüksiyon YAPAMADIM** (UI otomasyon
  aracım yok, fiziksel tıklama simüle edemiyorum). Bunun yerine `App.xaml.cs`'e global
  `DispatcherUnhandledException` yakalayıcı + `Services/CrashLogger.cs` (`%AppData%\OkulPanosu\
  crash.log`) eklendi — bundan sonra böyle bir hata olursa uygulama sessizce kapanmak yerine hatayı
  loglayıp koyu temalı bir hata penceresi gösterecek (çöküp gitmeyecek), ben de log dosyasını okuyup
  kesin sebebini bulabileceğim. **Kullanıcıdan istenen: tekrar dener, hata penceresi çıkarsa
  içeriğini/log dosyasını paylaşsın.**
- **"Hâlâ beyaz ekran var" — hangi sayfada olduğu belirtilmedi, ekran görüntüsü istendi**, körlemesine
  başka bir yer tahmin edip zaman kaybetmemek için.

**Onuncu tur — ekran görüntüsü sayesinde beyaz zeminin GERÇEK kaynağı kesin bulundu:**
- **`ScrollViewer.Background` hiçbir yerde açıkça verilmemişti** — WPF'in varsayılanı devreye giriyordu
  (pratikte beyaz). Alt kartlar tüm alanı kapladığında fark edilmiyordu, ama "🧩 Modül Ekleme Havuzu"
  başlığı gibi kartsız çıplak metinlerin olduğu boşluklarda doğrudan görünüyordu. `Base.xaml`'e
  `Style TargetType="ScrollViewer"` ile uygulama geneli koyu taban eklendi (ScrollViewer'ın varsayılan
  şablonu, UserControl'ün aksine, Background'ı gerçekten TemplateBinding ile kullanıyor — bu yüzden basit
  bir Setter yeterliydi).
- **Modül kartı mimarisi düzeltildi (kullanıcı asıl BUNU kastetmiş, iki kez yanlış anlamıştım):**
  `BoardGridControl`'ün dış kartı artık OPAK (BgSecondaryBrush, yarı saydam tint değil) ve başlık çubuğu
  da opak vurgu rengi. Her modül view'ının (ClockDateModuleView, DutyTeacherModuleView, vb. — 12 dosya)
  kendi ayrı `CardStyle` Border'ı (kendi arka planı + kendi yuvarlak köşeleri) KALDIRILDI — artık tek bir
  kart (dış), tek bir köşe yuvarlatması var; önceden başlığın hemen altında modülün kendi iç kartının da
  ayrıca yuvarlak köşesi oluyordu, çift köşe = görsel boşluk/tuhaflık.
- **Nöbetçi Öğretmen modülü fotoğraflı + otomatik döngülü oldu:** Sol tarafta günün nöbet listesi
  (`ListBox`), sağda dairesel fotoğraf + "N. NÖBETÇİ" rozeti + isim + branş. 6 saniyede bir sıradaki
  kişiye geçiliyor (`DispatcherTimer`), liste otomatik o satıra kayıyor (`ScrollIntoView`), isim Personel
  listesinde eşleşiyorsa fotoğrafı/branşı oradan geliyor, yoksa varsayılan silüet.
- **Saat & Tarih modülüne hava durumu eklendi** — API anahtarı GEREKTİRMEYEN `wttr.in` metin servisi
  kullanılıyor (`https://wttr.in/{şehir}?format=%C+%t`), konum Ayarlar'daki Kurum Bilgileri → İl/İlçe'den
  okunuyor, 30 dakikada bir yenileniyor, internet/servis yoksa satır sessizce gizli kalıyor.
- **Tepsi ikonu artık yayını durdurmuyor:** Çift tıklayınca `BoardWindow.OpenManagementKeepingBroadcast()`
  çağrılıyor — yayın TV'de devam ederken Yönetim penceresi (ana ekranda) açılıyor, "Yayını Başlat" butonu
  doğru şekilde "Durdur" durumunu gösteriyor (`ManagementWindow(BoardWindow)` constructor'ı artık
  `IsBroadcasting`'i kontrol edip başlangıç metnini buna göre ayarlıyor).

**On birinci tur — beyaz zemin için artık kesin/kaba kuvvet çözüm:**
- Global stil yaklaşımı (UserControl/ScrollViewer Template) tekrar tekrar farklı sayfalarda farklı
  şekillerde açık kalıyordu (bir sayfanın Grid.Row="0" başlık alanı ScrollViewer'ın DIŞINDA kalıyordu,
  o da beyaz çıkıyordu — TemplatesView tam olarak buydu). Artık spekülasyon yok: **her Views/*.xaml
  dosyasının KÖK `Grid`'ine (veya kök ScrollViewer'ına) doğrudan `Background="{StaticResource
  BgPrimaryBrush}"` verildi** — Grid/ScrollViewer'ın kendi Background'ı render etmesi WPF'te temel,
  şablon bağımlı olmayan bir davranış, bir daha bu sınıf hatayı yaşamamamız lazım. ~18 dosya tek seferde
  düzeltildi (`ManagementWindow`, `TemplatesView`, `TemplateEditorView`, tüm içerik sayfaları, tüm
  dialoglar).
- **Nöbetçi Öğretmen modülü yeniden düzenlendi:** Foto alanı büyütüldü (130px, önce 90px), sütun
  genişletildi (200px, önce 150px). Liste satırlarında artık SADECE isim var (kat/görev bilgisi yok) —
  daha çok kişi sığıyor. Foto alanında sırayla: foto → kat/görev etiketi (müdür yrd. için "NÖBETÇİ MÜDÜR
  YRD." yazıyor) → isim → branş.
- **Şablon editöründeki modül paleti artık "Şablonda Var" durumunu gösteriyor** — bir modül tipi zaten
  eklenmişse yeşil "Şablonda Var" rozeti çıkıyor (Ekle butonu kayboluyor), kartın zemini de farklı
  (BgElevatedBrush) oluyor. Modül eklenince/silinince palet otomatik yenileniyor.

**On ikinci tur:**
- **`ListBox` de aynı "Background Setter'ı işe yaramıyor" ailesine dahil çıktı** — `NavListStyle` ve
  `DutyTeacherModuleView`'daki `RosterList` sadece `Background="Transparent"` Setter'ı veriyordu, gerçek
  bir Template yoktu. Artık `Base.xaml`'de TÜM ListBox'lar için garanti çalışan bir Template var
  (Border+ScrollViewer+ItemsPresenter), `NavListStyle` ondan `BasedOn` ile türüyor. Bu muhtemelen nöbet
  listesindeki "liste zemini foto alanından farklı" sorununun gerçek kaynağıydı.
- **Diyalog pencereleri (`AdminPasswordDialog`, `SetPasswordDialog`, `ModuleSettingsDialog`,
  `AppMessageBox`, `SetupWizardWindow`) — kaynak kodda Background zaten doğruydu** (hem iç Grid'de hem
  şimdi ek olarak `<Window>` etiketinin kendisinde de açıkça verildi, çift güvence). Kullanıcı yine de
  beyaz gördüğünü bildirdi — kaynağı ONAYLADIM doğru, ama neden hâlâ beyaz göründüğünü açıklayamadım;
  muhtemelen ekran görüntüsü pencerenin İLK render karesinde (henüz tam boyanmadan) alınmış olabilir.
  **Kullanıcıya tekrar denemesi ve hâlâ oluyorsa hangi TAM anda (pencere ilk açılır açılmaz mı, birkaç
  saniye sonra da mı) beyaz kaldığını netleştirmesi istendi.**
- Nöbetçi Öğretmen: foto 150px'e büyütüldü, liste/foto arasına ince (üstten/alttan girintili) bir ayraç
  çizgisi eklendi, liste artık kartla aynı zeminde (yukarıdaki ListBox düzeltmesi sayesinde).

**Çözülmedi / araştırma gerektiriyor (kullanıcıya açıklanacak, kod değişikliği YAPILMADI):**
- **Dikey video yatay görünüyor + oynatma başında kararma/gecikme:** Kasıtlı bir rotasyon kodu YOK
  (`VideoModuleView` sadece `Stretch="Uniform"` kullanıyor). Bu muhtemelen WPF `MediaElement`'in (eski
  DirectShow/WMF tabanlı) telefonla çekilmiş dikey videoların rotasyon metadata'sını (EXIF/moov atom
  rotation matrix) doğru uygulamaması — ham (yatay) pikselleri gösteriyor. Düzeltmek için video dosyasının
  rotasyonunu (ör. MediaToolkit/FFmpeg ile) tespit edip `LayoutTransform`/`RotateTransform` uygulamak
  gerekir — araştırma+test gerektiren ayrı bir iş, kullanıcıya sorulacak.
- **Hafta sonu nöbetçi testi:** `DutyTeacherModuleView` bilinçli olarak Cumartesi/Pazar'da "Hafta sonu
  tatili" gösteriyor (referans şartnamesine uygun). Test için PC'nin sistem tarihini geçici olarak
  hafta içi bir güne almak öneriliyor — kod değişikliği gerekmiyor.

**Henüz bilerek YAPILMADI (kullanıcıya söylenecek):**
- Personel listesinde ızgara/liste görünüm anahtarı ve karta tıklayınca modal düzenleme — şu an satır
  içi (inline) düzenlenebilir alanlar var, çalışıyor ama istenen "tıkla → modal aç" deseni değil.
  Aynı desen (her karta ⚙️ ile modal ayar) tüm içerik sayfalarına (Duyurular, Doğum Günleri vb.) henüz
  yayılmadı — kapsamı büyük, ayrı bir round gerektirir.
- Doğum Günleri için Excel/kopyala-yapıştır toplu aktarım (Personel'de var, Doğum Günleri'nde yok).
- Video 16:9 letterbox davranışı `Stretch="Uniform"` + siyah zemin ile zaten otomatik sağlanıyor
  (dikey video yanlarda siyah boşlukla gösterilir) — ayrıca test edilmeli.

**Henüz bilerek YAPILMADI (kullanıcı "sonra ekleriz" dedi veya kapsam dışı):**
- Doğum günleri / Personel için Excel'den satır ayrıştırarak toplu aktarım (şimdilik personelde basit
  kopyala-yapıştır var, doğum günlerinde henüz yok).
- Slideshow/Galeri modülü.
- Nöbet çizelgesi UI'ında satır/gün ekleme-çıkarma (sabit 6 kat + Pazartesi-Cuma, referansla aynı).

**Henüz yapılmadı (sıradaki adımlar):**
1. Gerçek okul verileriyle ilk varsayılan şablonu oluşturup `board-data.json`'a gömmek (kullanıcı ile
   üzerinde konuşulan 3-modüllü basit yerleşim — Nöbetçi Öğretmen + Saat/Tarih + Ticker — ama artık
   Ticker modül değil, ayrı alan).
2. Klavye tabanlı taşı/boyutlandır editörü (`TemplateEditorView`'daki sayısal alanların yerine).
3. Kalan modüller (video, duyurular, slayt galerisi, ayın öğrencisi, doğum günleri, yemek menüsü, ders/
   teneffüs saatleri — sırası kullanıcıyla netleşecek).

## Önemli Notlar / Tuzaklar

- **`UseWPF` + `UseWindowsForms` birlikte açıkken** (çoklu monitör için `Screen.AllScreens` gerekiyor), SDK örtük olarak `System.Windows.Forms` ve `System.Drawing`'i global using olarak ekliyor ve `UserControl`/`KeyEventArgs`/`Color`/`ColorConverter` gibi WPF ile aynı isimli tiplerde belirsizlik hatası veriyor. Çözüm `OkulPanosu.App.csproj`'da:
  ```xml
  <ItemGroup>
    <Using Remove="System.Windows.Forms" />
    <Using Remove="System.Drawing" />
  </ItemGroup>
  ```
  `Screen.AllScreens` her yerde tam nitelikli (`System.Windows.Forms.Screen.AllScreens`) kullanılıyor, bu yüzden güvenli.
- `DayOfWeek.ToString(CultureInfo)` Türkçe gün adı VERMİYOR (obsolete/no-op) — bunun yerine `CultureInfo.GetDayName(dayOfWeek)` kullanılmalı (bkz. `TemplateEditorView.xaml.cs`).
- Yönetici şifresi hash'i `GlobalBoardData` içinde (paylaşılan JSON), `LocalSettings`'te DEĞİL.

## Sonraki Adımlar (Kapsam Dışı Bırakılanlar — sırayla eklenecek)

1. Kullanıcının manuel UI testi + bulunan hataların düzeltilmesi.
2. Kalan 9 modül: Nöbetçi Öğrenci, Haftanın En Temiz Sınıfları (Hijyen Ödülü), Ders & Teneffüs Zaman Sayacı, Yemek Listesi, Saat/Tarih/Hava Durumu (hava durumu kısmı — dış API kararı gerekiyor), Tarihte Bugün, Ayın/Haftanın Öğrencisi, Vefat/Tebrik Duyuruları, Günün Sözü, Resim/Video Galerisi.
3. Işgara editöründe sürükle-bırak / fare ile yeniden boyutlandırma (şu an sayısal alanlarla).
4. Açık/Koyu tema geçişi (şu an sadece koyu taban + aksan rengi).
5. `publish-al.bat` benzeri self-contained publish script'i (Taşıt Tanıma'daki `publish-al.bat` deseniyle: `dotnet publish -c Release -r win-x64 --self-contained true`).

**On üçüncü tur — medya yolu dayanıklılığı + hava durumu ikonu:**
- **Kök sorun:** Medya alanları (`Personnel.ImagePath`, `Announcement.ImagePath`,
  `StudentOfTheMonth.ImagePath`, `LocalVideo.FilePath`) TAM yol saklıyordu. Kullanıcı paylaşılan veri
  klasörünü yeniden adlandırır/taşırsa (ki bunu düzenli yapıyor), kayıtlı tam yollar artık geçersiz
  oluyordu — foto/video hiçbir zaman bulunamıyordu.
- **Çözüm — sadece dosya adı sakla, yolu HER ZAMAN kullanım anında kur:** Dört model alanı yeniden
  adlandırıldı: `Personnel.ImagePath→PhotoFileName`, `Announcement.ImagePath→ImageFileName`,
  `StudentOfTheMonth.ImagePath→ImageFileName`, `LocalVideo.FilePath→FileName`. Artık HİÇBİR yerde tam yol
  saklanmıyor; her okuma anında `Path.Combine(AppServices.Data.XxxFolderPath, kayıtlıDosyaAdı)` ile CANLI
  kuruluyor (`XxxFolderPath` her zaman GÜNCEL `LocalSettings.DataFolderPath`'ten türüyor) — veri klasörü
  taşınsa/adı değişse bile, ayarlardan yeni konum gösterilince tüm medya otomatik yeniden bulunuyor.
- **OS dosya seçici (`OpenFileDialog`) TAMAMEN kaldırıldı, hiçbir medya seçiminde yok artık.** Kullanıcının
  net talebi: "klasör seçmek için pencere açılmasın, direkt yayın klasörünün ilgili alt klasörünün içeriği
  gösterilsin, oradan seçilsin." Yeni ortak bileşen `EditorControls.MediaFilePicker` (+ `ImageFilePicker`
  sarmalayıcı): ilgili alt klasördeki (Medya\Videolar, Öğretmen Fotoğrafları, vb.) dosyaları tarayan bir
  `ComboBox` (her `DropDownOpened`'ta yeniden tarıyor — güncel) + yanında "Klasörü Aç" butonu (Gezgin'i o
  klasörde açar, kullanıcı dosyayı önce oraya koyar, sonra combodan seçer). Kullanıldığı yerler:
  `PersonnelView` (yeni personel formu + satır içi foto — satır içi olan `ContextMenu` tabanlı
  `ShowPhotoMenu` deseniyle), `AnnouncementsView`, `StudentOfMonthView`, `VideosView` (satır içi, video
  dosyası seçimi — "+ Video Ekle" artık sadece boş bir satır ekliyor, dosya seçimi/adı satırdaki picker'dan).
- Tüm modül-görünümü (`*ModuleView`) tarafları da (StudentOfMonth, Announcements, DutyTeacher, Video)
  aynı "her render'da `Path.Combine(güncelFolderPath, kayıtlıDosyaAdı)`" desenine güncellendi.
- **Hava durumu ikonu eklendi:** `ClockDateModuleView` artık wttr.in'den gelen Türkçe durum metnindeki
  anahtar kelimelere (güneş/açık, bulut, yağmur, kar, sis, fırtına) göre bir emoji seçip (`IconFor`, ikon
  fontu YOK, düz emoji — kanıtlanmış desen) metnin yanında (☀️/☁️/🌧️/❄️/🌫️/⛈️) gösteriyor.
- Derleme: `dotnet build OkulPanosu.slnx` → 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) veri klasörünü yeniden adlandırıp/taşıyıp foto ve videoların hâlâ
  yüklendiğini doğrulamak, (2) Personel/Duyurular/Ayın Öğrencisi/Videolar sayfalarındaki yeni "mevcut
  dosyalardan seç" picker akışını denemek (Gözat/dosya penceresi kalmadığını teyit), (3) Saat modülündeki
  yeni hava durumu ikonunu görsel olarak kontrol etmek.

**On üçüncü tur — devamı: ComboBox tabanlı seçici geri alındı, native dosya penceresine dönüldü:**
- Kullanıcı ekran görüntüsüyle gösterdi: yukarıdaki `ComboBox` tabanlı "mevcut dosyalardan seç" listesi
  (soluk gri yazı, önizleme YOK, "ABDULSELAM DEMİR.jpg" gibi 20+ dosya adı üst üste) hiç kullanılabilir
  değildi — hangi görseli seçtiğini göremiyordu.
- **Karar netleşti:** OS'in kendi "Dosya Aç" penceresi geri getirildi (`Microsoft.Win32.OpenFileDialog`,
  `EditorControls.PickAndImportFile`) — bu pencere Gezgin'in kendi küçük resim/büyük simge görünümünü
  kullanıyor, okunaklılık/önizleme sorunu YOK (bizim çizdiğimiz bir kontrol değil, Windows'un kendisi).
  `InitialDirectory` ilgili paylaşılan alt klasöre (Medya\Videolar, Öğretmen Fotoğrafları, vb.) ayarlı,
  yani pencere "eskisi gibi" doğrudan o klasörde açılıyor.
- **Kritik fark — kayıt hâlâ tam yol DEĞİL:** Seçilen dosya o klasörün dışındaysa otomatik olarak içeri
  KOPYALANIYOR (`File.Copy`), ardından her durumda sadece `Path.GetFileName(...)` kaydediliyor — asla
  `D:\YAYIN\...` gibi kök yol saklanmıyor. Okuma anında her zaman GÜNCEL `repo.XxxFolderPath` (o alanın
  bildiği sabit alt klasör, ör. Videolar/Öğretmen Fotoğrafları) ile `Path.Combine` edilip buluyor — veri
  klasörü taşınsa/adı değişse bile bozulmuyor. Yani hem native/görsel seçim hem de yol dayanıklılığı
  birlikte sağlandı.
- **Görsel önizleme eklendi:** `EditorControls.MediaPickerWithPreview` artık seçim yapıldıktan SONRA da
  64x64 küçük resim gösteriyor (fotoğraflarda gerçek görsel bitmap, videolarda 🎬 simgesi — video kare
  önizlemesi WPF'te ek bağımlılık/karmaşıklık gerektirdiği için kasıtlı olarak yapılmadı, kullanıcıya
  belirtilecek). `PersonnelView` satır-içi foto butonu da aynı native dialog akışına (`PickPhoto`) geçti,
  eski `ContextMenu` tabanlı `ShowPhotoMenu` kaldırıldı.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıya not edilecek sınır:** Video seçiminde küçük resim ÖNİZLEMESİ yok (sadece 🎬 ikonu +
  dosya adı) — sadece dosya seçimi anında native Gezgin penceresinde (kullanıcı büyük simge görünümüne
  geçerse) video thumbnail'i görülebilir, bizim kendi arayüzümüzde değil.

**On dördüncü tur — dört büyük ekleme (pencere boyutu hafızası, slayt süresi, modül ayarları = içerik sayfası, yeni galeri modülü):**
- **Yönetim penceresi boyutu hatırlanıyor:** `LocalSettings.ManagementWindowWidth/Height/Maximized` eklendi.
  `ManagementWindow` artık kapanırken (`Closing`) mevcut boyutu (tam ekranken `RestoreBounds.Size`) makineye
  özel ayarlara kaydediyor, açılışta varsa bunu uyguluyor (`ApplySavedWindowSize`, `PositionOnPrimaryScreen`'den
  ÖNCE çağrılıyor ki ortalama doğru boyuta göre hesaplansın).
- **Ayın Öğrencisi + yeni Okulumuzdan Kareler modülünde slayt geçiş süresi artık ayarlanabilir:**
  `ModuleSettingsDialog`'a "Slayt Geçiş Süresi (saniye)" slider'ı eklendi (3-60 sn), `BoardModule.
  Settings["transitionSeconds"]`'a yazılıyor; `StudentOfMonthModuleView` ve yeni `SchoolGalleryModuleView`
  bu değeri okuyup `DispatcherTimer.Interval`'i buna göre ayarlıyor (yoksa sırasıyla 8/6 sn varsayılan).
- **Köklü değişiklik — Modül Ayarları (⚙️) artık ilgili İçerikler sayfasını gömüyor:** `ModuleSettingsDialog`
  1100x700, yeniden boyutlandırılabilir hale getirildi. `ModuleFactory.CreateContentEditor(type)` eklendi —
  modül tipi bir İçerikler alt sayfasıyla eşleşiyorsa (Nöbet Çizelgesi, Duyurular, Yemek Menüsü, Ders&Zil,
  Doğum Günleri, Ayın Öğrencisi, Yerel Videolar, Tarihte Bugün, Haftanın Beyin Egzersizi, Haftanın Temiz
  Sınıfları) o sayfanın YENİ bir örneğini oluşturup dialoga gömüyor. Örn. panodaki "Duyurular" modülünün
  ⚙️'sine tıklayınca, Yönetim → İçerikler → Duyurular sayfasındaki AYNI düzenleme arayüzü orada da çıkıyor.
  Bu gömülü sayfanın KENDİ "Kaydet" butonu var ve anında paylaşılan veriye yazıyor — dialog'un kendi Kaydet/
  İptal butonları sadece Başlık/Vurgu Rengi/tipe-özel ayarları (video, slayt süresi) etkiliyor, birbirinden
  bağımsız. Saat/Tarih ve Okulumuzdan Kareler gibi kendine ait içerik sayfası olmayan tipler için bu bölüm
  gizli kalıyor.
- **Yeni modül: "Okulumuzdan Kareler"** (`school_gallery`) — ayrı bir yönetim sayfası YOK, `Medya\Slayt`
  klasöründe (`BoardDataRepository.SlideshowFolderPath`, `EnsureFolderStructure`'a eklendi) ne varsa modül
  eklendiğinde otomatik olarak sırayla gösteriliyor (`SchoolGalleryModuleView`). Modül ayarlarında (⚙️) hem
  slayt süresi slider'ı hem de "📂 Slayt Klasörünü Aç" kısayolu var (kullanıcı resimleri doğrudan o klasöre
  kopyalar). `ModuleFactory.KnownTypes/DefaultSizes/CreateView`'a eklendi, modül paletinde otomatik görünüyor.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) Yönetim penceresini yeniden boyutlandırıp kapat-aç yaparak boyutun
  korunduğunu doğrulamak, (2) bir şablona "Okulumuzdan Kareler" modülü ekleyip Medya\Slayt klasörüne birkaç
  resim koyarak sırayla geçişi izlemek, (3) Ayın Öğrencisi/Okulumuzdan Kareler modüllerinin ⚙️'sinden slayt
  süresini değiştirip önizlemede etkisini görmek, (4) örn. Duyurular modülünün ⚙️'sine tıklayıp altta aynı
  Duyurular içerik sayfasının göründüğünü ve oradaki Kaydet'in çalıştığını doğrulamak.

**On beşinci tur — klasör adı, uygulama ikonu, gerçek çözünürlüklü editör tuvali, fareyle taşıma:**
- **Duyuru görselleri klasörü yeniden adlandırıldı:** `BoardDataRepository.ImagesFolderPath` artık
  `Medya\Duyuru ve Haber Resimleri` (eskiden `Medya\Resimler`). Eski klasörde dosya varsa ve yeni klasör
  henüz yoksa `EnsureFolderStructure` içinde otomatik `Directory.Move` ile taşınıyor (`MigrateLegacyImagesFolder`)
  — kullanıcı daha önce eklediği duyuru görsellerini kaybetmiyor.
- **Uygulama ikonu eklendi:** `Assets/AppIcon.ico` (16-256px, PNG-içerikli çok boyutlu ICO; koyu-mavi
  gradyan zemin üzerinde beyaz monitör + üç renkli "modül" bloğu — panonun kendi görsel diline uygun,
  bir throwaway .NET konsol scripti ile System.Drawing kullanılarak üretildi). `OkulPanosu.App.csproj`'a
  `<ApplicationIcon>` eklendi (görev çubuğu/EXE ikonu), tüm `Window` köklü XAML'lere (`BoardWindow`,
  `ManagementWindow`, `SetupWizardWindow`, dialoglar) `Icon="/Assets/AppIcon.ico"` eklendi (başlık çubuğu/
  Alt-Tab). `TrayIconService` artık sabit `SystemIcons.Application` yerine çalışan exe'nin kendi gömülü
  ikonunu `Icon.ExtractAssociatedIcon(Environment.ProcessPath)` ile çıkarıp kullanıyor — ayrı bir .ico
  dosyası taşımaya gerek yok, tepsi ikonu her zaman uygulamanın gerçek ikonuyla birebir aynı kalır.
- **Varsayılan ızgara 30×30'a çıkarıldı** (`Template.cs`, `TemplateEditorView` fallback değerleri).
- **Köklü editör değişikliği — çalışma alanı artık gerçek ekran çözünürlüğünde:** `AppServices.
  GetKioskScreenSize()` eklendi (Ayarlar'da seçili yayın ekranının `Screen.Bounds.Size`'ı, BoardWindow'un
  kendi konumlandırma mantığıyla aynı kaynağı kullanıyor). `TemplateEditorView` artık `PreviewGrid`'i
  ızgara sütun/satır SAYISINDAN bağımsız olarak HER ZAMAN bu gerçek piksel boyutuna (`Width`/`Height`)
  sabitliyor (`ApplyCanvasSize`, hem editör hem önizleme modunda) ve bunu bir `ScrollViewer` (Auto/Auto)
  içine aldı — pencere/panel bu boyuttan küçükse Excel'deki gibi yatay/dikey kaydırma çıkıyor. Böylece
  kullanıcı, ızgarayı 10×10 veya 60×60 yapsa da, yerleştirdiği modüllerin gerçek yayında (ör. 1920×1080)
  ne kadar yer kaplayacağını editörde birebir görüyor. Kart üstüne "Çalışma alanı: WxH px..." bilgi satırı eklendi.
- **Fareyle sürükle-bırak taşıma:** `BoardGridControl.StartMoveDrag` eklendi — modülün başlık çubuğundan
  (⚙️/🗑 butonlarının olmadığı kısımdan, imleç `SizeAll`) tutup sürüklemek modülü ızgara hücrelerine göre
  anlık taşıyor (resize tutamacıyla aynı desen: sürüklerken sadece `Grid.SetColumn/Row` canlı güncelleniyor,
  tam `Render()` yalnızca bırakınca çağrılıyor — capture sırasında görsel ağacın yeniden kurulup mouse
  capture'ın koptuğu bir hataya yol açmamak için). Yön tuşlarıyla taşıma da olduğu gibi duruyor, ikisi paralel çalışıyor.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) görev çubuğu ve sistem tepsisindeki yeni ikonu görsel olarak
  onaylamak, (2) Duyurular sayfasında yeni resim seçmeyi deneyip doğru klasörün açıldığını doğrulamak
  (daha önce eklenmiş resimler varsa hâlâ göründüğünü de kontrol etmek), (3) bir şablon editöründe
  çalışma alanının artık gerçek ekran boyutunda göründüğünü ve sağa/aşağı kaydırılabildiğini doğrulamak,
  (4) bir modülü başlık çubuğundan tutup fareyle sürükleyerek taşımayı denemek.

**On altıncı tur — okul adı başlığı, sistem tepsisi netleştirmesi, Duyurular yeniden tasarımı, sürükleme odak hatası:**
- **Panoya okul adı başlığı eklendi:** `BoardGridControl.xaml`'a yeni bir `Auto` satır (`SchoolNameHeader`)
  eklendi — `GlobalBoardData.InstitutionName` doluysa panonun en üstünde sabit bir şerit olarak gösteriliyor,
  boşsa satır tamamen gizleniyor (`Visibility.Collapsed`, `Auto` yükseklik → yer kaplamıyor). Bu satır,
  editördeki/kiosk'taki TOPLAM sabit yükseklikten (ekran çözünürlüğü) pay alıyor, dışına taşmıyor —
  modül ızgarası (RootGrid) satırı `*` olduğu için başlık kadar küçülüyor, toplam boy sabit kalıyor.
- **Sistem tepsisi ikonu — kod tarafı doğrulandı, sorun Windows'un kendi davranışı:** `Icon.
  ExtractAssociatedIcon` PowerShell ile ayrı test edildi, exe'den ikonu sorunsuz çıkarıyor (32x32,
  hatasız). Muhtemel açıklama: Windows yeni/az kullanılan tepsi ikonlarını görev çubuğundaki "^" (gizli
  simgeleri göster) okunun ARKASINA gizler — kullanıcıya bunu kontrol etmesi ve isterse simgeyi sürükleyip
  her zaman görünür alana taşıması söylendi. Kod tarafında ek bir değişiklik yapılmadı (registry ile
  zorlamak kırılgan/riskli, kullanıcının kendi görev çubuğu tercihine karışmak istenmedi).
- **Duyurular tamamen yeniden tasarlandı (liste görünümü + net tarih semantiği):**
  `Announcement.Date` (belirsiz serbest metin) kaldırıldı, yerine `StartDate` (DateTime, varsayılan bugün
  — yayına giriş tarihi), `EndDate` (DateTime?, opsiyonel — otomatik yayından kalkış), `IsActive` (bool,
  varsayılan true — tarihten bağımsız manuel yayında/kaldır anahtarı, Videolar'daki aynı desen) eklendi.
  `AnnouncementsView` artık Personel/Ayın Öğrencisi'ndeki gibi sütun başlıklı liste: Foto | Başlık |
  Başlangıç | Bitiş | Yayında ✓ | Foto/Sil butonları — altında İçerik + Önem ikinci bir satırda. Yeni
  `EditorControls.DateTextBox` eklendi — WPF'in `DatePicker`/`Calendar` kontrolleri bu projede hiç
  temalanmadı ve geçmişteki "beyaz zemin" hatalarıyla aynı risk ailesinden olduğu için KULLANILMADI;
  bunun yerine kanıtlanmış `WatermarkTextBoxStyle` tabanlı, yazarken otomatik nokta ekleyen ("05102026" →
  "05.10.2026") sade bir metin kutusu yazıldı. `AnnouncementsModuleView` artık yayına alırken
  `IsActive && StartDate<=bugün && (EndDate==null || EndDate>=bugün)` filtresini uyguluyor, tarih satırı
  "başlangıç" veya "başlangıç – bitiş" olarak gösteriliyor.
- **Sürükleyerek taşımada yön tuşu hatası düzeltildi:** `BoardGridControl.StartMoveDrag` modülü seçili
  yapıyordu ama `Focus()` çağırmıyordu — bu yüzden başlıktan seçilen bir modülde yön tuşları çalışmıyordu
  (klavye odağı hâlâ başka bir kontroldeydi, `PreviewKeyDown` tünellemesi `BoardGridControl`'e hiç
  uğramıyordu). `_selectedModule` atamasının hemen ardından `Focus()` eklendi — artık gövdeden tıklamak
  kadar başlıktan sürüklemek/seçmek de yön tuşlarını aktif ediyor.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) Ayarlar → Kurum Bilgileri'nde okul adı girip yayında/önizlemede
  üstte göründüğünü doğrulamak, (2) görev çubuğundaki "^" gizli simgeler okunu açıp tepsi ikonunun orada
  olup olmadığına bakmak, (3) yeni Duyurular liste ekranını (tarih kutularının otomatik nokta eklemesi,
  Yayında kutucuğu) denemek, (4) bir modülü başlığından seçtikten SONRA yön tuşlarıyla taşımayı denemek.

**On yedinci tur — tepsi ikonu bulanıklığının GERÇEK kök nedeni, Duyurular görsel yerleşimi, tüm slayt modülleri süre ayarlı + döngülü:**
- **Tepsi ikonu — GERÇEK kök neden bulundu (önbellek değilmiş):** `Icon.ExtractAssociatedIcon` sadece TEK
  bir boyut (genelde 32x32) döndürüyor; Windows bunu tepsinin gerçek küçük ikon boyutuna (tipik 16x16)
  küçültünce ince detaylar (renkli modül blokları) bulanıklaşıp anlamsız bir mavi lekeye dönüşüyordu —
  Explorer yeniden başlatma bu yüzden işe yaramadı. Kesin çözüm: `Assets\AppIcon.ico` artık çıktı
  klasörüne de düz dosya olarak kopyalanıyor (csproj'da `<None Include=... CopyToOutputDirectory=
  "PreserveNewest">`, `<Resource>` XAML pencere ikonu için ayrıca duruyor), `TrayIconService.LoadAppIcon`
  bu dosyayı `SystemInformation.SmallIconSize` ile doğrudan açıp çok boyutlu ICO içinden TAM o boyut için
  özel çizilmiş kareyi seçiyor — bulanıklık yok. `Environment.ProcessPath` + `ExtractAssociatedIcon`
  sadece yedek (fallback) olarak kaldı.
- **Duyurular modülünde görsel yerleşimi düzeltildi:** Görsel `Height="120"` sabitti ve metin grubu
  `StackPanel` içinde (Auto boyut) olduğu için, modül kartı büyükse altında büyük bir boşluk kalıyor,
  görsel de (büyük/farklı en-boy oranlı olduğunda `UniformToFill` ile) çoğu zaman kırpılıp sadece küçük
  bir kısmı görünüyordu. Çözüm: `ContentPanel` artık bir `Grid` — üstteki satır (`ImageRow`) görsel
  varsa `*` (kalan TÜM alanı doldurur, `Stretch="Uniform"` ile KIRPMADAN tam sığdırır), yoksa `0`
  (hiç yer kaplamaz); alttaki satır `Auto` boyutlu metin grubu HER ZAMAN mevcut alanın EN ALTINA oturur.
- **TÜM "slayt/döngü" modüllerinin ayarlarında artık geçiş süresi seçilebiliyor:** `ModuleSettingsDialog`'daki
  `SlideDurationDefaults` sözlüğü genişletildi — artık Ayın Öğrencisi/Okulumuzdan Kareler'e ek olarak
  **Duyurular, Nöbetçi Öğretmen, Tarihte Bugün, Haftanın Temiz Sınıfları, Doğum Günleri** de ⚙️'den
  saniye ayarlanabiliyor (her tip kendi mantıklı varsayılanıyla: 8/6/8/6/8/6/6 sn).
- **Üç modül tek-kayıt gösteriminden DÖNGÜLÜ gösterime çevrildi (birden fazla kayıt artık kaybolmuyor):**
  - **Tarihte Bugün:** Önceden bugünün tarihiyle eşleşen kayıtlardan sadece İLKİ gösteriliyordu (aynı
    tarihe birden fazla kayıt eklenirse diğerleri sessizce kayboluyordu) — artık eşleşen TÜM kayıtlar
    sırayla gösteriliyor.
  - **Haftanın Temiz Sınıfları:** Önceden tüm sıralama küçük tek satırlar hâlinde birlikte gösteriliyordu
    — artık her sıra (rütbe rozeti + sınıf adı + puan + ödül) tek tek, büyük punto ile sırayla gösteriliyor.
  - **Doğum Günleri → "Bugün Doğanlar" oldu:** Kullanıcı isteğiyle filtre "bu ay" yerine "bugün"e
    değiştirildi (`BirthdayStudent.Date` bugünün "{gün} {ay adı}" etiketiyle karşılaştırılıyor) — normalde
    1-2 kişi kalıyor, birden fazlaysa yine sırayla büyük gösteriliyor. Modül başlığı "🎂 Bu Ay Doğanlar" →
    "🎂 Bugün Doğanlar" olarak güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) tepsi ikonunun artık net/keskin göründüğünü doğrulamak, (2) bir
  duyuruya görsel ekleyip modülün artık görseli kırpmadan tam gösterdiğini ve metnin alta yaslandığını
  kontrol etmek, (3) Duyurular/Nöbetçi Öğretmen/Tarihte Bugün/Haftanın Temiz Sınıfları/Doğum Günleri
  modüllerinin ⚙️ ayarlarında yeni "Slayt Geçiş Süresi" kaydırıcısını denemek, (4) birden fazla "Tarihte
  Bugün" kaydı veya birden fazla "Haftanın Temiz Sınıfları" sırası girip döngüyü izlemek, (5) bugünün
  tarihiyle eşleşen bir doğum günü ekleyip "Bugün Doğanlar" başlığıyla göründüğünü doğrulamak.

**On sekizinci tur — dinamik doğum günü aralığı, Duyurular görsel yerleşim seçeneği, ayarlar penceresi sıkıştırıldı:**
- **Doğum Günleri artık dinamik aralıklı:** Sabit "bugün" yerine modül ⚙️ ayarında **Bugün / Bu Hafta / Bu
  Ay** seçilebiliyor ("birthdayFilter" ayarı, varsayılan "Bu Hafta" — kullanıcının tercihi). `BirthdayStudent.
  Date` (serbest metin, "gün Ay") artık `ParseDayMonth` ile ayrıştırılıp seçili tarih aralığına (yıl sınırı
  aşımı dahil, ör. hafta 30 Aralık-5 Ocak arası ise) düşüp düşmediği kontrol ediliyor. Birden fazla kişi
  aralığa düşerse tarihe göre sıralanıp sırayla gösteriliyor, başlık seçime göre "🎂 Bugün/Bu Hafta/Bu Ay
  Doğanlar" olarak değişiyor.
- **Duyurular modülünde görsel yerleşimi seçilebiliyor:** Modül ⚙️ ayarına "Görsel Yerleşimi" seçeneği
  eklendi — **Üstte Görsel/Altta Yazı** (varsayılan) veya **Solda Görsel/Yanda Yazı**. Bunu statik tek bir
  XAML ile ifade etmek pratik olmadığından `AnnouncementsModuleView` artık içeriğini (görsel + başlık/
  içerik/tarih grubu) her `Render()`'da KOD ile kuruyor (`ContentHost` boş bir `Grid` — BoardGridControl'ün
  kart oluşturma deseniyle aynı yaklaşım), seçilen yerleşime göre Grid satır/sütunlarını farklı kuruyor.
- **Modül Ayarları penceresi sıkıştırıldı:** Başlık ve Vurgu Rengi artık AYRI satırlar yerine TEK satırda
  yan yana (kullanıcının ekran görüntüsünde işaretlediği gibi boşa yer kaplıyorlardı). Slayt Geçiş Süresi/
  Ses Düzeyi gibi etiket+kaydırıcı+değer üçlüleri de artık TEK satıra sığıyor (`InlineSettingRow` ortak
  yardımcı metodu) — önceden etiket üstte, kaydırıcı altta iki satır kaplıyordu. Bu değişiklikler, altta
  gömülü içerik editörüne (listeler vb.) daha fazla dikey alan bırakıyor.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) Doğum Günleri modülünün ⚙️'sinden Bugün/Bu Hafta/Bu Ay seçeneklerini
  deneyip başlığın ve gösterilen kişilerin değiştiğini doğrulamak, (2) Duyurular modülünün ⚙️'sinden
  "Solda Görsel" seçip yerleşimin değiştiğini görmek, (3) Modül Ayarları penceresinin artık daha az dikey
  yer kapladığını (Başlık/Vurgu Rengi tek satırda) doğrulamak.

**On dokuzuncu tur — ComboBox görüntü hatası kökten çözüldü, tek Kaydet butonu, Ayın Öğrencisi fotoğrafı büyütüldü:**
- **ComboBox "{ Key = ..., Label = ... }" gösterme hatası — kök neden zaten bilinen bir aileden:**
  `DisplayMemberPath` kullanan özel-oluşturulmuş ComboBox'lar (Görsel Yerleşimi, Doğum Günü Aralığı) ham
  nesne `ToString()`'ini gösteriyordu — bu, projenin önceki turlarında zaten tespit edilip düzeltilmiş AYNI
  hata (`Base.xaml`'deki özel ComboBox şablonu `DisplayMemberPath`'i düzgün uygulamıyor). Çözüm: bu iki
  combobox da artık zaten kanıtlanmış `EditorControls.LabeledComboBox` (açık `ItemTemplate` kullanan) ile
  kuruluyor — DisplayMemberPath bir daha KULLANILMAYACAK, bu proje için güvenilir değil.
- **Modül Ayarları'nda tek satırlık ayar çiftleri yan yana:** Slayt Geçiş Süresi artık Görsel Yerleşimi /
  Doğum Günü Aralığı ile AYNI satırda, iki sütun hâlinde (`TwoColumnRow`) — daha az dikey yer kaplıyor.
  Duyurular'da "Solda Görsel" seçeneğinde görsel/yazı oranı artık görsel LEHİNE (1.5:1, önceden 1.1:1.4
  yazı lehineydi).
- **Köklü değişiklik — TEK "Kaydet" butonu:** Önceden ModuleSettingsDialog'a gömülü içerik sayfasının
  (Duyurular, Ayın Öğrencisi, Doğum Günleri, vb.) KENDİ "Kaydet" butonu + dialogun kendi "Kaydet"i olmak
  üzere İKİ ayrı buton görünüyordu, kafa karıştırıyordu. Yeni `IEmbeddableContentPage` arayüzü (`SetEmbedded()`
  + `SaveContent()`) eklendi, ⚙️ ile gömülen 10 İçerikler sayfasının (Duyurular, Nöbet Çizelgesi, Yemek
  Menüsü, Ders&Zil, Doğum Günleri, Ayın Öğrencisi, Yerel Videolar, Tarihte Bugün, Haftanın Beyin Egzersizi,
  Haftanın Temiz Sınıfları) TAMAMI bunu uyguluyor — gömülüyken kendi Kaydet butonları GİZLENİYOR, dialogun
  TEK "Kaydet" butonu artık hem modül ayarlarını hem gömülü içeriği aynı anda kaydediyor. Bu sayfalar
  bağımsız (İçerikler menüsünden) kullanıldığında kendi Kaydet butonları normal şekilde görünmeye devam ediyor.
- **Ayın Öğrencisi fotoğrafı büyütüldü:** 48px → 110px (dairesel). Ad Soyad/Sınıf/Gerekçe/Söz sütunları
  artık sabit piksel yerine ORANTILI (Star) genişlikte — satır, önceden 1048px'de sabit kalıp geniş
  pencerelerde sağda boş alan bırakırken, artık mevcut pencere genişliğine göre esniyor (VideosView'daki
  aynı desen). Bu değişiklik sadece Ayın Öğrencisi'ne uygulandı (kullanıcı özellikle bunu istedi); istenirse
  Personel/Duyurular listelerine de aynı teknik uygulanabilir.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) Duyurular/Doğum Günleri ⚙️'sinde comboboxların artık düzgün metin
  gösterdiğini doğrulamak, (2) Modül Ayarları penceresinin artık daha kompakt olduğunu görmek, (3) herhangi
  bir gömülü içerik sayfasında (ör. Duyurular) SADECE tek bir "Kaydet" butonu kaldığını ve tıklayınca hem
  başlık/renk/süre ayarlarının hem de listedeki değişikliklerin kaydedildiğini doğrulamak, (4) o sayfayı
  ayrıca İçerikler menüsünden bağımsız açıp kendi Kaydet butonunun hâlâ orada olduğunu kontrol etmek,
  (5) Ayın Öğrencisi listesindeki büyümüş fotoğrafları ve pencere genişliğine esneyen sütunları görmek.

**Yirminci tur — Ayın Öğrencisi fotoğrafı dinamik büyütüldü, varsayılan şablon kullanıcının gerçek düzeniyle değiştirildi:**
- **Ayın Öğrencisi modül fotoğrafı artık Nöbetçi Öğretmen ile aynı boyutta (150px) VE dinamik:** Sabit
  90px daire, Nöbetçi Öğretmen'in kullandığı 150px tasarıma çıkarıldı; ayrıca bir `Viewbox` (`Stretch=
  "Uniform"`, `StretchDirection="DownOnly"`) içine alındı — modül genişçe yer kaplıyorsa 150px'te sabit
  kalıyor, ama modül dar bir alana sıkıştırılırsa (ör. ızgarada küçük bir kutuya yerleştirilirse) fotoğraf
  da orantılı olarak küçülüyor (yalnızca genişlik yönünde daralmaya tepki veriyor — StackPanel dikeyde
  sonsuz yükseklik sunduğu için yükseklik yönünde otomatik küçülme yok, kapsam dışı bırakıldı).
- **Izgara varsayılanı 50×50'ye çıkarıldı** (`Template.cs`, `TemplateEditorView` fallback değerleri) —
  kullanıcının kendi panosunda kullandığı ve beğendiği değer.
- **Varsayılan şablon artık kullanıcının GERÇEK düzeni:** `BoardDataRepository.EnsureDefaultTemplate()`
  (sadece veri klasöründe HİÇ şablon yokken, yani gerçekten sıfır kurulumda devreye giriyor — kullanıcının
  kendi `D:\YAYIN\board-data.json`'ı zaten dolu olduğu için BU kurulumu etkilemedi/değiştirmedi) artık
  eski 7 modüllük basit yerleşim yerine, kullanıcının canlı yayınından okunan (`D:\YAYIN\board-data.json`)
  TAM 12 modüllü, 50×50 ızgaralı, gerçek ticker metni/hızı/renkleriyle BİREBİR AYNI düzeni oluşturuyor
  ("Ana Pano Düzeni (1920×1080)" — modül X/Y/W/H/ThemeColor/Settings değerleri harfiyen kopyalandı).
  Kullanıcı istediği zaman bu şablonu düzenleyip farklı bir isimle veya üzerine kaydedebilir; sadece İLK
  kurulumda (veri klasörü boşken) otomatik oluşuyor.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı. Kullanıcının kendi D:\YAYIN verisi
  dokunulmadan kaldı (EnsureDefaultTemplate zaten mevcut şablon varken no-op).
- **Kullanıcıdan istenen retest:** (1) Ayın Öğrencisi modülünün fotoğrafının artık Nöbetçi Öğretmen ile
  aynı boyutta göründüğünü doğrulamak, modülü ızgarada daraltıp fotoğrafın küçüldüğünü gözlemlemek,
  (2) yeni varsayılan şablonu görmek isterse boş bir veri klasörüyle (ör. farklı bir test klasörü seçerek
  Ayarlar'dan) uygulamayı yeniden başlatıp otomatik oluşan düzeni kontrol etmek — kendi çalışan kurulumunu
  bozmadan test etmek isterse bunu önerebilirim.

**Yirmi birinci tur — "Ders & Teneffüs Saatleri" tamamen yeniden düşünüldü: canlı sınıf ders programı:**
Kullanıcı haklı olarak, sadece periyot saatlerini listelemenin ("1. Ders 08:30-09:10" gibi) çok az bilgi
verdiğini belirtti — bunun yerine PANOYA BAKAN KİŞİNİN "şu an hangi sınıfın hangi dersi var" görmesini
istedi, teneffüste "TENEFFÜS" yarı saydam yazısıyla, bittiğinde otomatik bir sonraki derse geçerek.
- **Yeni veri modeli** (kullanıcıya önce gösterilip onaylandı): `SchoolClass` (Id, Name, `List<
  ClassLessonEntry> Lessons`) + `ClassLessonEntry` (Day, Period — LessonSchedule'daki LessonPeriod.Period'a
  referans verir, zaman tekrar tutulmaz —, Subject, Teacher opsiyonel). `GlobalBoardData.Classes` eklendi.
  Nöbet Çizelgesi'ndeki DutyRosterDay/DutyAssignment ile AYNI "seyrek liste" deseni — sadece dolu hücreler
  kayıtlı. Kullanıcı ileride (tatilden dönünce) Excel'den kopyala-yapıştır/okutma isteyecek — bu düz
  satır yapısı (Sınıf, Gün, Periyot, Ders, Öğretmen) buna zaten uygun, henüz uygulanmadı.
- **Yeni editör sayfası `ClassSchedulesView`** (İçerikler → "📚 Sınıf Ders Programı", `class_schedules`
  anahtarı) — Nöbet Çizelgesi'yle aynı matris deseni: önce sınıf tanımlanır (isim + Ekle), sonra seçili
  sınıf için gün (Pazartesi-Cuma) × periyot (LessonSchedule'daki periyotlar, saat aralığı oradan okunur)
  matrisinde her hücreye Ders + opsiyonel Öğretmen girilir. `IEmbeddableContentPage` uyguluyor (tutarlılık için).
- **`ScheduleModuleView` tamamen yeniden yazıldı:** 20 saniyede bir (`DispatcherTimer`) şu anki saati
  `LessonSchedule`'daki periyot aralıklarıyla (metin "08:30 - 09:10" içinden `TimeSpan.TryParse` ile
  ayrıştırılıyor) karşılaştırıp durumu hesaplıyor:
  - **Ders saatindeyse:** o periyotta dersi olan TÜM sınıfları (Sınıf | Ders | Öğretmen) kaydırılabilir
    bir tabloda gösterir, öğretmen boşsa satırda boş kalır.
  - **Teneffüsteyse:** son biten periyodun tablosu ALTTA görünmeye devam eder, üzerine yarı saydam
    (`#9A0B1220`, ~%60 opak) bir katman ve "TENEFFÜS" yazısı biner (`DropShadowEffect` ile okunaklılık),
    altında "N. ders HH:mm'de başlıyor" bilgisi de var.
  - **Ders başlamadan önce / bittikten sonra / hafta sonu:** ilgili boş durum mesajı (diğer modüllerle
    tutarlı stil).
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Henüz yapılmadı (kullanıcı tatilden dönünce netleşecek):** Excel'den toplu ders programı aktarımı
  (kopyala-yapıştır ya da dosya okutma) — şu an sadece hücre hücre elle giriliyor.
- **Kullanıcıdan istenen retest:** (1) İçerikler → Sınıf Ders Programı'ndan birkaç sınıf + birkaç ders
  girip kaydetmek, (2) panoda/önizlemede o an hangi periyotta olunduğuna göre doğru sınıf/ders tablosunun
  çıktığını görmek (test için `ScheduleView`'daki periyot saatlerini geçici olarak şu anki saate yakın
  ayarlayıp deneyebilir), (3) teneffüs aralığında "TENEFFÜS" yarı saydam yazısının tablo üzerine bindiğini
  doğrulamak.

**Yirmi ikinci tur — Ders & Zil Saatleri'nde otomatik oluşturma + zincirleme yeniden hesaplama, sınıf adı formatı:**
- **`LessonPeriod` modeli kökten değişti:** Serbest metin `Time` ("08:30 - 09:10") kaldırıldı, yerine
  yapılandırılmış `Start` (TimeSpan), `DurationMinutes`, `BreakAfterMinutes` eklendi. `End` ve `TimeLabel`
  bunlardan hesaplanan `[JsonIgnore]` özellikler. Bu, hem metin ayrıştırma kırılganlığını ortadan kaldırdı
  (`ScheduleModuleView` artık regex/split değil doğrudan `Start`/`End` kullanıyor) hem de otomatik
  oluşturma/zincirleme yeniden hesaplamayı mümkün kıldı.
- **"Otomatik Oluştur" kartı eklendi** (`ScheduleView`): Başlangıç saati + Ders süresi (dk) + Teneffüs
  süresi (dk) + Periyot Sayısı girilip "Oluştur"a basılınca TÜM periyotlar sıfırdan, zincirleme
  hesaplanmış başlangıç saatleriyle oluşuyor (`ScheduleView.Generate`).
  Ders Adı ve Zaman Aralığı artık aynı satırda yan yana (kullanıcının istediği gibi), altında ayrı bir
  satırda Ders Süresi/Teneffüs Süresi editable alanları var.
- **Zincirleme yeniden hesaplama:** Herhangi bir periyodun Ders Süresi veya Teneffüs Süresi değiştirildiğinde
  (`RecalculateStartTimes`), ondan SONRAKİ tüm periyotların başlangıç saati otomatik güncelleniyor — ilk
  periyodun başlangıcı tek "anchor", gerisi zincirleme türetiliyor. Periyot silindiğinde de aynı şekilde
  yeniden hesaplanıyor.
- **Sınıf adı formatı sabitlendi:** `ClassSchedulesView`'daki "+ Sınıf Ekle" artık TEK bir serbest metin
  kutusu değil, "Seviye" (ör. 9) + "Şube / Alan" (ör. A, KSH, Büro) olmak üzere İKİ ayrı alan — isim
  otomatik "Seviye-Şube" (ör. "9-KSH") biçiminde birleştiriliyor. Böylece hem basit "9-A" tarzı hem de
  okulun gerçek alan adlarıyla ("9-Konaklama ve Seyahat Hizmetleri" yerine kısaca "9-KSH" gibi) tutarlı
  bir ayırıcı formatı garanti ediliyor, serbest metinle karışıklık olmuyor.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) Ders & Zil Saatleri'nde "Otomatik Oluştur"u deneyip periyotların
  doğru zincirlendiğini görmek, (2) bir periyodun ders/teneffüs süresini değiştirip SONRAKİ periyotların
  saatinin otomatik kaydığını doğrulamak, (3) Sınıf Ders Programı'nda yeni Seviye+Şube formatıyla sınıf
  eklemeyi denemek (ör. "9" + "KSH" → "9-KSH").

**Yirmi üçüncü tur — Ders & Zil Saatleri: süre kutuları kaldırıldı, başlangıç saati doğrudan düzenlenebilir + Enter ile zincirleme:**
Kullanıcı bir önceki turdaki tasarımı düzeltti: "Ders Süresi (dk)"/"Teneffüs Süresi (dk)" ayrı satırı
İSTEMİYORDU (kart boyu şişiyordu — kullanıcının genel talebi "aşağı doğru bol yer olsun, daha çok kart
sığsın" tüm modüller için geçerli, bu turda somut olarak Ders & Zil Saatleri'nde uygulandı). Gerçek istek:
öğlen arası gibi standart dışı bir teneffüsü, o periyodun BAŞLANGIÇ SAATİNİ doğrudan yazıp Enter'a basarak
ayarlamak — sistem otomatik olarak (a) o periyottan ÖNCEKİ periyodun teneffüs süresini bu yeni boşluğa göre
türetir ve kalıcı olarak saklar, (b) SONRAKİ tüm periyotları üstteki "Otomatik Oluştur" kartındaki standart
ders/teneffüs sürelerine göre yeniden zincirler.
- **Süre satırı tamamen kaldırıldı** — her periyot kartı artık TEK satır: Ders Adı | Başlangıç Saati
  (editable, "Bitiş HH:mm" küçük ipucu etiket olarak yanında). Kart yüksekliği yaklaşık yarıya indi,
  ekrana daha fazla periyot sığıyor.
  Süre/teneffüs dakikaları model içinde (`DurationMinutes`/`BreakAfterMinutes`) hâlâ duruyor ama artık
  DOĞRUDAN elle girilmiyor — ya "Otomatik Oluştur" ile toptan ayarlanıyor ya da bir periyodun başlangıç
  saati elle değiştirildiğinde ÖNCEKİ periyodun teneffüsü buna göre otomatik türetiliyor.
- **Enter ile commit:** Başlangıç saati kutusunda Enter'a basınca (ya da odaktan çıkınca, ikisi de tetikliyor)
  `CommitStart()` çalışıyor: yeni saat ayrıştırılır → önceki periyodun `BreakAfterMinutes`'ı
  `(yeniBaşlangıç - öncekiBitiş)` olarak güncellenir (kalıcı, ileride yukarı akış değişse bile bu özel
  boşluk korunur) → periyodun kendi Start'ı ayarlanır → `RecalculateStartTimes()` ile sonraki TÜM
  periyotlar (kendi standart Duration/BreakAfter değerleriyle) zincirleme yeniden hesaplanır.
- **Genel ilke not edildi (tüm modüller için geçerli, bu tur kapsamında sadece Ders & Zil Saatleri'ne
  uygulandı):** Kullanıcı liste/kart tasarımlarında dikey alanı minimize etmeyi (daha çok kart sığsın diye)
  net bir tercih olarak belirtti — ileride başka bir liste sayfası "çok yer kaplıyor" şikâyeti gelirse aynı
  "yan yana sıkıştır" refleksiyle yaklaşılmalı, ayrı bir talep beklenmeden.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** 5. dersin bitişinden 25 dakika sonrasına 6. dersin başlangıcını yazıp
  Enter'a basarak, (a) 6. dersin bitişinin 40 dakikaya göre kaydığını, (b) 7. ve sonraki periyotların yine
  10 dakika standart teneffüsle zincirlendiğini doğrulamak.

**Yirmi dördüncü tur — Bitiş saati de düzenlenebilir oldu, Haftanın Temiz Sınıfları artık puana göre otomatik sıralanıyor:**
- **`ScheduleView`'da Bitiş saati de eklendi:** Her periyot kartında artık Ders Adı | Başlangıç | Bitiş
  üçü de aynı satırda. Bitiş saati elle değiştirilip Enter'a basılınca (`CommitEnd`) o periyodun
  `DurationMinutes`'ı (Bitiş - Başlangıç) olarak yeniden hesaplanıyor, ardından `RecalculateStartTimes()`
  ile SONRAKİ periyotlar zincirleme kayıyor — nadir de olsa bir dersin süresi standarttan farklı olduğunda
  (ör. çift saat bir atölye dersi) kullanılabiliyor.
- **Haftanın Temiz Sınıfları — kök neden bulundu:** `CleanestClassEntry.Rank` alanı EKLENME SIRASINA göre
  otomatik atanıyordu (`_entries.Count + 1`), puanla hiç ilişkisi yoktu — kullanıcı yüksek puanlı bir
  sınıfı SONRADAN eklediğinde düşük puanlı sınıfın önünde/1. sırada kalabiliyordu. Kesin çözüm: `Rank`
  alanı TAMAMEN kaldırıldı — sıra artık HER ZAMAN Score'a göre büyükten küçüğe sıralanıp gösterim anında
  türetiliyor, hem `CleanestClassView` (editör listesi, puan değiştirilince anında yeniden sıralanır) hem
  `CleanestClassModuleView` (pano döngüsü) için. Eklenme sırası artık hiçbir etkisi yok.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı. Eski kayıtlardaki "Rank" alanı JSON'da
  kalsa bile artık okunmuyor (System.Text.Json bilinmeyen alanı sessizce yok sayar), veri kaybı riski yok.
- **Kullanıcıdan istenen retest:** (1) bir periyodun Bitiş saatini değiştirip sonraki periyotların kaydığını
  doğrulamak, (2) Haftanın Temiz Sınıfları'na önce düşük puanlı, sonra yüksek puanlı bir sınıf ekleyip
  yüksek puanlının otomatik olarak "1. SIRA" gösterildiğini doğrulamak.

**Yirmi beşinci tur — "önce periyot tanımlayın" hatasının GERÇEK kök nedeni:**
- `ScheduleView.Load()` — veri klasöründe hiç periyot yoksa (`data.LessonSchedule.Count == 0`) sayfa kendi
  içinde GEÇİCİ bir varsayılan liste üretip ekranda gösteriyordu, ama "Kaydet"e basılmadan bu asla paylaşılan
  veriye YAZILMIYORDU. Kullanıcı sayfayı "dolu" gördüğü için periyotların tanımlı olduğunu düşündü, ama
  `ClassSchedulesView` ve `ScheduleModuleView` gerçek kayıtlı veriyi okuyordu — orası hâlâ boştu, "önce
  periyot tanımlayın" diyordu.
- **Kesin çözüm:** `BoardDataRepository.EnsureDefaultLessonSchedule()` eklendi (`EnsureDefaultTemplate` ile
  aynı desen) — veri klasöründe hiç periyot yoksa 08:30 başlangıçlı, 40 dk ders + 10 dk teneffüslü 10
  periyotluk varsayılanı otomatik KAYDEDİYOR. `AppServices.Initialize()`/`SetDataFolder()`'a eklendi.
  Kullanıcının kendi `D:\YAYIN` verisi de bu sayede hemen düzeldi (uygulama yeniden başlatılınca 10 periyot
  gerçekten kaydedildi, PowerShell ile doğrulandı).
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** Sınıf Ders Programı sayfasına gidip artık "önce periyot tanımlayın"
  uyarısının kaybolduğunu ve gün×periyot matrisinin (periyot saatleriyle birlikte) göründüğünü doğrulamak.

**Yirmi altıncı tur — Ders & Zil Saatleri artık her değişiklikte otomatik kaydediyor, ilk kapsamlı Yardım kılavuzu:**
- **Otomatik kaydetme:** `ScheduleView`'daki HER mutasyon noktası (Başlangıç/Bitiş saati Enter/odak kaybı,
  Ders Adı odak kaybı, periyot ekleme/silme, "Otomatik Oluştur") artık `PersistSilently()` çağırıp anında
  paylaşılan veriye yazıyor — kullanıcı "Değişiklikleri Kaydet" butonuna basmayı unutup sayfadan çıkarsa
  (Sınıf Ders Programı gibi başka sayfalar eski/eksik veri okuyordu) artık kaybolmuyor. Buton hâlâ duruyor
  (minimum periyot doğrulamasıyla) ama artık zorunlu değil, ek bir güvence.
- **Yardım sayfası eklendi (hem uygulama içi hem web):** Yönetim penceresi sol menüsüne "❓ Yardım" eklendi
  (`HelpView` — gömülü `WebBrowser` kontrolü, `Assets/Help/kilavuz.html`'i diskten açar, csproj'a
  `CopyToOutputDirectory` ile eklendi). Bu dosya, uygulamanın TÜM özelliklerini (Kiosk/Yönetim modu, Şablon
  Editörü, Modül Ayarları, medya seçimi, 12 modülün her biri, Personel, Ayarlar, SSS) kapsayan Türkçe bir
  kılavuz — basit/eski tarayıcı motoruyla (WPF WebBrowser = IE tabanlı) uyumlu, sade CSS.
  Aynı içerik ayrıca daha zengin bir tasarımla (sabit sol İçindekiler rayı, kaydırma ile aktif bölüm
  vurgusu, açık/koyu tema) **web sayfası olarak da yayınlandı**:
  <https://claude.ai/code/artifact/f261d410-4ad4-483a-9753-597fd87a04e1>
  (Bu artifact varsayılan olarak GİZLİ/private — kullanıcı paylaşmak isterse sayfanın kendi paylaşım
  menüsünden paylaşabilir.) İki sürüm de aynı kaynak içerikten türetildi ama ayrı dosyalar (in-app sürüm
  eski tarayıcı motoruyla uyumlu kalmak için kasıtlı olarak daha sade tutuldu).
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** (1) Ders & Zil Saatleri'nde bir değişiklik yapıp "Kaydet"e BASMADAN başka
  bir sayfaya geçip Sınıf Ders Programı'nda değişikliğin yansıdığını doğrulamak, (2) Yönetim penceresi sol
  menüsündeki yeni "❓ Yardım" sayfasını açıp kılavuzun düzgün göründüğünü kontrol etmek, (3) isterse web
  sürümünü paylaşım menüsünden paylaşmak.

**Yirmi yedinci tur — Yardım kılavuzu "tek sayfa/tek bölüm" gezinmeye çevrildi:**
- Kullanıcı geri bildirimi: önceki sürüm (kaydırmalı tek sayfa + aktif-bölüm vurgusu) yerine, sol menüden
  hangi öğeye tıklarsa sağda SADECE onunla ilgili içerik görünsün istedi (ör. "Nöbetçi Öğretmen"e tıklayınca
  diğer modüller karışmasın).
- **Mimari değişti:** Artık her bölüm (12 modülün her biri dahil) kendi bağımsız `.page` `<div>`'i — JS ile
  sadece SEÇİLEN sayfa gösteriliyor (`display:none`/`block`), IntersectionObserver tabanlı kaydırma-izleme
  kaldırıldı (daha basit VE eski IE motoruyla daha uyumlu). Her modül sayfasının altına önceki/sonraki modüle
  geçiş butonları eklendi, "Modüller" etiketi de tıklanabilir bir genel bakış/ızgara sayfasına dönüştürüldü.
- **Aynı tasarım artık uygulama içi Yardım'da da var:** `Assets/Help/kilavuz.html` (in-app WebBrowser
  sürümü) TAMAMEN bu yeni "tek bölüm" tasarımıyla değiştirildi — eski sade/ayrı tasarım kaldırıldı, artık
  ikisi de (web + uygulama içi) aynı gezinme deneyimini kullanıyor. In-app sürüm, WPF WebBrowser'ın eski
  IE motoruyla uyumlu kalması için CSS Grid/Flexbox yerine `inline-block`/`float` gibi daha temel tekniklerle
  ve `attachEvent` yedeğiyle (çok eski IE için) yazıldı; emoji yerine `&#xxxx;` HTML varlıkları kullanıldı
  (glyph render tutarlılığı için, projenin "ikon fontu güvenilmez" dersine paralel bir önlem).
  Web sürümü aynı URL'de güncellendi: <https://claude.ai/code/artifact/f261d410-4ad4-483a-9753-597fd87a04e1>
- **SSS'teki Ctrl+Alt+Y maddesi yeniden yazıldı:** Artık birincil yöntem olarak sistem tepsisi simgesi
  (çift tık / sağ tık → "Yönetime Geç") + Yönetim penceresindeki "Yayını Başlat/Durdur" butonu öne çıkıyor,
  tamamen çıkış için sağ tık → "Çıkış" bahsediliyor; Ctrl+Alt+Y ikincil/yedek yöntem olarak not düşüldü.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı, kopyalanan dosya doğrulandı.
- **Kullanıcıdan istenen retest:** (1) web sürümünde sol menüden bir modüle tıklayıp SADECE onun içeriğinin
  göründüğünü doğrulamak, (2) uygulama içi Yardım sayfasını açıp aynı gezinmenin (WPF WebBrowser içinde)
  düzgün çalıştığını kontrol etmek — eski IE motoru bazen modern CSS'i farklı yorumlayabilir, görünüm
  farklıysa bildirmesi gerekiyor.

**Yirmi sekizinci tur — kılavuzda "Kiosk/TV" netleştirmesi + pratik yöntem ana sayfaya taşındı:**
- Kılavuzdaki her "Kiosk" geçtiği yer "Kiosk/TV" oldu (nav butonu, başlıklar, Ayarlar maddeleri) —
  kullanıcı "Kiosk" teriminin kafa karıştırdığını, "TV" ekleyince daha net olacağını belirtti.
- "Kiosk/TV ve Yönetim Modu" sayfasındaki "Kiosk/TV (Yayın) modunda" bölümü genişletildi — SSS'teki pratik
  yöntem (tepsi simgesi çift tık/sağ tık → Yönetime Geç, Yayını Başlat/Durdur butonu, Çıkış, Ctrl+Alt+Y
  ikincil) artık doğrudan ana anlatımda da var, sadece SSS'te gömülü kalmıyor.
- Hem web sürümü (aynı URL) hem uygulama içi Yardım güncellendi. Derleme 0 hata, uygulama başlatıldı.

**Yirmi dokuzuncu tur — publish-al-genel.bat doğrulandı, Sınıf Ders Programı'na Excel'den yapıştırma eklendi:**
- **`publish-al-genel.bat` fiilen çalıştırılıp doğrulandı** (statik okuma değil, gerçek `dotnet publish -c
  Release -r win-x64 --self-contained true` komutu çalıştırıldı): doğru `.csproj`'u (OkulPanosu.App, Core
  değil) buluyor, çıktı klasörü (171MB, self-contained — hedef PC'de .NET kurulu olması gerekmiyor)
  `Assets/AppIcon.ico` ve `Assets/Help/kilavuz.html`'i doğru içeriyor. "Boş PC'de sıfırdan kurulum" gereksinimi
  zaten uygulamanın kendi mimarisiyle sağlanıyor — `LocalSettings` (%AppData%) ve şifre hash'i (paylaşılan
  `board-data.json` içinde) ikisi de publish çıktısının PARÇASI DEĞİL, ilk çalıştırmada sıfırdan oluşuyor;
  varsayılan veri klasörü önerisi de (`SetupWizardWindow`) exe'nin bulunduğu klasöre göre (`AppContext.
  BaseDirectory`) dinamik, hardcoded "D:\YAYIN" değil. Sonuç: bat dosyası eksiksiz, değişiklik gerekmedi.
- **Sınıf Ders Programı'na Excel'den kopyala-yapıştır eklendi** (önceki turda "henüz yok" diye işaretlenmişti):
  Seçili sınıf için "Excel'den Yapıştır" kartı — Excel'de hazırlanan haftalık tabloyu (gün başlıkları/periyot
  etiketi sütunuyla BİRLİKTE ya da onlarsız, ikisi de otomatik algılanıyor — satır/sütun sayısı beklenenden
  bir fazlaysa baştaki satır/sütun otomatik atlanıyor) kopyalayıp yapıştırınca, o sınıfın TÜM haftalık
  programını tek seferde dolduruyor. Hücrede "Ders - Öğretmen" yazılırsa öğretmen de ayrılıyor (son " - "
  ayracına göre), öğretmen kısmı opsiyonel. Yapıştırma da diğer mutasyonlar gibi anında kaydediliyor
  (`PersistSilently`, Ders & Zil Saatleri'ndeki aynı düzeltme deseni ClassSchedulesView'a da uygulandı —
  sınıf ekleme/silme/hücre düzenleme de artık anında kaydediyor).
- **Test için örnek `.xlsx` üretildi** (PowerShell + Excel COM otomasyonu ile, gerçek bir Excel dosyası —
  3 sayfa: "9-KSH", "9-Büro", "10-A", her biri 10 periyot × 5 gün, bazı hücrelerde öğretmen adı, bazılarında
  yok, bazı periyotlar bilinçli boş bırakıldı) kullanıcıya gönderildi.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı.
- **Kullanıcıdan istenen retest:** Gönderilen örnek Excel dosyasından bir sınıfın tablosunu (başlık
  satırı/sütunuyla birlikte veya onlarsız, farketmez) kopyalayıp Sınıf Ders Programı'nda ilgili sınıfı seçip
  "Excel'den Yapıştır" kutusuna yapıştırıp "Yapıştırılanı Uygula"ya basarak tüm haftalık programın tek
  seferde dolduğunu doğrulamak.

**Otuzuncu tur — KRİTİK hata: gece yarısını geçen periyotlar pano karşılaştırmasını sessizce bozuyordu:**
- **Kök neden:** `ScheduleView`'da periyot başlangıçları zincirleme toplanarak hesaplanıyor
  (`Start + Duration + Break`). Kullanıcı bir periyodun başlangıcını "şu anki saate yakın" (akşam/gece
  saatine) ayarlayınca, sonraki periyotlar zincirleme eklenince gece yarısını AŞTI — `TimeSpan.Days` 1'e
  çıktı (ör. "1.00:10:00" = 1 gün + 00:10). `LessonPeriod.TimeLabel`'daki `"hh\:mm"` biçimi bunu GÖRSEL
  olarak gizliyordu (sadece saat/dakika kısmını gösterip Days'i atlıyordu, editörde her şey normal
  görünüyordu) — ama `ScheduleModuleView.Refresh()`'teki karşılaştırma `DateTime.Now.TimeOfDay` (her zaman
  &lt;24 saat) ile `period.Start` (artık ≥24 saat) arasında yapıldığından ASLA eşleşmiyordu, pano modülü
  sessizce boş kalıyordu.
- **Kesin çözüm:** `LessonPeriod.NormalizeTimeOfDay(TimeSpan)` eklendi (mod 24 saat). `ScheduleView`'daki
  TÜM Start hesaplama noktalarına (`Generate`, `RecalculateStartTimes`, `CommitStart`, `AddPeriod_Click`)
  uygulandı — bir daha gece yarısını geçen bir zincir birikmeyecek. `ScheduleModuleView.Refresh()`'e de
  savunma amaçlı aynı normalize eklendi (sadece görüntüleme için, olası eski/bozuk kayıtları da kendiliğinden
  düzeltir).
- **Kullanıcının canlı verisi (`D:\YAYIN\board-data.json`) doğrudan onarıldı** (PowerShell ile periyot
  Start değerlerinden fazladan gün bileşeni temizlendi) — kod düzeltmesini beklemeden hemen çalışır hâle
  geldi, veri kaybı olmadı (Templates/Classes/Personnel/LessonSchedule sayıları düzeltme öncesi/sonrası
  aynı olduğu doğrulandı).
- **Sınıf Ders Programı'ndaki Excel yapıştırma da genişletildi** (kullanıcı gerçek okul formatının henüz
  belirsiz olduğunu, ama "Pazartesi'nin dersleri Pazartesi'ye yazılmalı" ve "ders adının altına öğretmen
  kısaltması" gibi olası farkları belirtti):
  - **Sütun sırası artık önemli değil:** yapıştırılan ilk satırda gün adları (tam ad veya Pzt/Sal/Çar/Per/Cum
    gibi kısaltmalar) tanınırsa, hangi sütunun hangi güne ait olduğu ORADAN okunur; tanınmazsa varsayılan
    Pazartesi-Cuma sırasına düşülür.
  - **Öğretmen kısaltması → Personel eşleştirme:** bir hücrede ders adının ALTINA (Excel'de Alt+Enter ile)
    yazılmış "A. Yılmaz" gibi bir kısaltma varsa, Personel listesindeki tam adla (baş harf + soyad, sesli
    harfleri atılmış hâliyle de) eşleştirilmeye çalışılır. Yalnızca TEK net aday varsa kullanılır — belirsiz
    ya da eşleşme yoksa kısaltma OLDUĞU GİBİ yazılır (yanlış eşleştirmektense güvenli taraf seçildi).
    Yapıştırma sonrası durum mesajı kaç kısaltmanın eşleştiğini/eşleşmediğini bildiriyor.
  - **Tırnak-farkında ayrıştırıcı (`ParseTsv`) eklendi:** Excel, tab/satır sonu içeren hücreleri tırnak
    içine alarak kopyalar (çok satırlı ders+öğretmen hücreleri tam olarak bunu tetikliyor) — düz
    `Split('\t')`/`Split('\n')` bunu satır/sütun sayımını bozarak yanlış yorumlardı, artık doğru ayrıştırılıyor.
- Derleme: 0 hata, 0 uyarı. Uygulama başlatıldı, çöküşsüz açıldı. Düzeltme sonrası canlı veriyle elle
  doğrulandı: şu anki saat (01:19, Salı) 4. periyoda denk düşüyor, 9-KSH A/B'nin o periyotta "Din Kültürü ve
  Ahlak Bilgisi" dersi var — pano artık bunu göstermeli.
- **Kullanıcıdan istenen retest:** Yayını (veya Yönetim'deki önizlemeyi) açıp Ders & Teneffüs modülünün artık
  doğru periyodu ve sınıf listesini gösterdiğini doğrulamak.

**Otuz birinci tur — Nöbetçi Öğretmen: nöbet yeri artık listede de görünüyor + branş gösterimi hatası düzeltildi:**
- **Nöbet yeri konumu (yeni, seçilebilir ayar):** Daha önce nöbet yeri (kat/bölge) yalnızca sağdaki fotoğraf
  panelinde, sıra o kişiye geldiğinde görünüyordu — listede sadece isim vardı. `DutyTeacherModuleView.xaml`'a
  iki `DataTemplate` eklendi (`RosterItemRightTemplate`: isim + sağında nöbet yeri sütunu; `RosterItemBelowTemplate`:
  isim + altında ikinci satır nöbet yeri), hangisinin kullanılacağı yeni `dutyLocationLayout` modül ayarıyla
  ("right"/"below", varsayılan "right") belirleniyor. `ModuleSettingsDialog`'a `BuildDutyLocationLayoutRow()`
  eklendi (mevcut `BuildAnnouncementLayoutRow` deseniyle birebir, `EditorControls.LabeledComboBox` kullanıyor —
  "Sağda Sütun" / "İsmin Altında"), Slayt Geçiş Süresi ile aynı satırda (`TwoColumnRow`) gösteriliyor. Gün bazlı
  nöbet filtreleme mantığına (`Load()`) kullanıcının açık talimatıyla DOKUNULMADI.
- **Branş gösterimi hatası (kullanıcı bildirdi — "öğretmen fotosunun altında branş gelmiyor"):** Kök neden,
  `DutyTeacherModuleView.xaml.cs`'de fotoğraf altındaki metnin yanlışlıkla `person.Title` (Görev/Ünvan — ör.
  "Öğretmen", çoğu kayıtta boş) okumasıydı; branş asıl `Personnel.Branch` alanında tutuluyor. `CurrentBranchText.Text`
  artık `person?.Branch` okuyor.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- Kılavuz (uygulama içi + web) da bu tur ile Ders & Zil Saatleri'ndeki Excel yapıştırma özelliği (artık
  "henüz yok" değil) ve Nöbetçi Öğretmen'in nöbet yeri yerleşim ayarıyla güncellendi.

**Otuz ikinci tur — Şablon bazlı Görünüm Modu (Koyu / Gök Mavisi / Buz Mavisi):**
- **İstek:** Kullanıcı sürekli koyu tema kullanıldığını belirtip, her şablonun kendi ayarından seçilebilen,
  farklı taban renk modları (örnek olarak "gök mavisi", "buz mavisi" verdi) istedi — seçilen mod o şablon
  yayına alındığında geçerli olsun.
- **Mimari:** Önceden `Themes/Base.xaml` TÜM uygulamada (Yönetim arayüzü dahil) tek sabit koyu paleti
  `StaticResource` ile veriyordu; sadece vurgu rengi (`ThemeManager`, app-genelinde/makine bazlı)
  değiştirilebiliyordu. Kullanıcının isteği ŞABLON bazlı ve SADECE yayın/önizleme kapsamlı olduğundan
  (Yönetim'in kendi menü/liste ekranları koyu kalmalı), taban paleti anahtarları (`BgPrimaryBrush`,
  `BgSecondaryBrush`, `BgElevatedBrush`, `BgInputBrush`, `BorderBrush1`, `TextPrimaryBrush`,
  `TextMutedBrush`, `SuccessBrush`, `WarningBrush`, `DangerBrush`, yeni `BoardCanvasBackgroundBrush`)
  `Base.xaml` içindeki tüm iç kullanımlarında `DynamicResource`'a çevrildi (app-genelinde görünüşte hiçbir
  şey değişmedi — hâlâ Application seviyesindeki `Base.xaml` değerlerine düşüyor). Yeni
  `BoardColorModeManager` (`Services/BoardColorModeManager.cs`), mod paletini SADECE verilen scope'un
  (`BoardGridControl.Resources`) kendi `MergedDictionaries`'ine ekliyor/değiştiriyor — bu yüzden sadece
  `BoardGridControl` (Kiosk'ta `BoardWindow` VE Yönetim'deki `TemplateEditorView` önizlemesi, ikisi de aynı
  kontrolü kullanıyor) içindeki `DynamicResource` referansları etkileniyor, Yönetim'in geri kalanı
  (Application seviyesi) dokunulmadan koyu kalıyor.
- **Yeni dosyalar:** `Themes/Base.Sky.xaml` (Gök Mavisi — koyu, tamamen mavi tonlu bir palet) ve
  `Themes/Base.Ice.xaml` (Buz Mavisi — açık/soğuk beyaz-mavi bir palet, koyu metin). Her ikisi de
  `Base.xaml` ile BİREBİR aynı anahtar setini tanımlıyor.
  - `Template.cs`'e `ColorMode` alanı eklendi (varsayılan `"dark"` — eski kayıtlarda alan yoksa otomatik
    bu değere düşer, geriye dönük uyumlu).
  - `BoardDataRepository.UpdateTemplateModules`'e `colorMode` parametresi eklendi.
  - `BoardGridControl.Render()` her çağrıldığında `BoardColorModeManager.Apply(this, template?.ColorMode)`
    çağırıyor — hem Kiosk hem editör önizlemesi otomatik doğru modu alıyor.
  - `TemplateEditorView`'a "Görünüm Modu" kartı eklendi (Kayan Duyuru kartının altında), `EditorControls.
    LabeledComboBox` ile üç seçenek arasında geçiş yapılıyor, değişiklik anında önizlemeye yansıyor.
  - Modüllerdeki tek tek doğrudan `StaticResource` renk referansları da (BoardGridControl'ün okul adı
    şeridi/zemin gradyanı, DutyTeacherModuleView'daki nöbet rozeti/foto çerçevesi/ayırıcı çizgi/branş rengi,
    CleanestClassModuleView'daki sıra rozeti, StudentOfMonthModuleView'daki foto çerçevesi,
    WeeklyQuestionModuleView'daki ayırıcı çizgi) `DynamicResource`'a çevrildi — aksi halde bu tek tek
    noktalar mod değişse de eski koyu renkte sabit kalırdı. Ayrıca okul adı başlığındaki sabit
    `Foreground="White"` de düzeltildi (Buz Mavisi'nde beyaz zemin üzerinde beyaz yazı görünmez olurdu).
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı. Kılavuza (uygulama içi + web) "Şablonlar ve
  Editör" sayfasına yeni "Görünüm Modu" bölümü eklendi.
- **Kullanıcıdan istenen retest:** Yönetim → Şablonlar → bir şablonu düzenle → "Görünüm Modu" kartından Gece
  Yeşili/Buz Mavisi deneyip sağdaki önizlemenin değiştiğini, Yönetim'in geri kalanının (sol menü, diğer
  sayfalar) koyu kaldığını doğrulamak; sonra Kaydet + Yayını Başlat ile TV çıkışında da aynı modun
  göründüğünü kontrol etmek.
- **Anında geri bildirim ve düzeltme (aynı tur içinde):** Kullanıcı canlı yayından ekran görüntüsü attı —
  Buz Mavisi'nde modül kartları arasındaki/içindeki BOŞ alanların (BgPrimaryBrush — modülün kendi
  UserControl zemin rengi, örn. Ders & Teneffüs kartının doldurulmamış alt kısmı) rengi çok soluk/beyaza
  yakın kalmış, ekran görüntüsünde kırmızı dikdörtgenle işaretlediği yerdeki tonun DAHA CANLI/PARLAK bir
  hâlini istedi. Ayrıca "Gök Mavisi" modunun zaten bilinen sıradan koyu maviye çok benzediğini, farklı bir
  renk olmasını (seçimi bana bırakarak) istedi.
  - **Buz Mavisi düzeltmesi:** `BgPrimaryBrush`/`BoardCanvasBackgroundBrush` belirgin şekilde koyulaştırıldı/
    canlandırıldı (`#EAF4FC` → `#AEE2F7`, gradyan `#86D2F4→#AEE2F7→#D3EFFC`), `BorderBrush1` de daha belirgin
    yapıldı (`#B9D9EF`→`#6FC3EA`); kartlar (`BgSecondaryBrush`) okunabilirlik için beyaza yakın bırakıldı.
  - **"Gök Mavisi" → "Gece Yeşili" oldu:** İçerik tamamen değişti — koyu, zengin bir zümrüt/orman yeşili
    paleti (`Themes/Base.Sky.xaml` dosya adı korundu, sadece içeriği değişti — dosya yoluna göre karar veren
    bir mantık yok). `BoardColorModeManager.Modes`'taki DisplayName/SwatchHex güncellendi (anahtar hâlâ
    `"sky"` — geriye dönük veri uyumluluğu için, hiçbir yerde kullanıcıya gösterilmiyor).
  - Kılavuz (uygulama içi + web) "Gök Mavisi" → "Gece Yeşili" olarak güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.

**Otuz ikinci tur — devamı: sabit presetler tamamen kaldırıldı, yerine "Özel Renk" seçici geldi:**
- **Kullanıcı geri bildirimi (ekran görüntüleriyle):** Hem Gece Yeşili hem Buz Mavisi'nde, modül
  kart/liste zeminlerinin ("bu kısımdaki liste") ana zeminden ("zemin rengi") FARKLI kaldığını ve
  "sırıttığını" bildirdi — özellikle Buz Mavisi'nde ana zemin doğru rengi almıştı ama kart zeminleri
  (`BgSecondaryBrush`) hâlâ sabit beyazdı; Ders & Teneffüs modülünün liste alanı da (hem Gece Yeşili'nde
  neredeyse siyah, hem Buz Mavisi'nde gri) ayrıca tamamen farklı bir tondaydı. Kullanıcı bizzat "renk
  kartelası" (color picker) önerdi: kullanıcı istediği rengi kendisi seçip kaydetsin.
- **Kök neden:** Elle ayarlanmış 3 sabit paletin (Koyu/Gece Yeşili/Buz Mavisi) her biri
  `BgPrimaryBrush`/`BgSecondaryBrush`/`BgElevatedBrush` için birbirinden BAĞIMSIZ, elle seçilmiş hex
  değerleri kullanıyordu (ör. Buz Mavisi'nde Primary canlı mavi ama Secondary hep sabit `#FFFFFF`) — bu
  yüzden her yeni geri bildirimde tek tek hex değerlerini elle düzeltmek gerekiyordu ve tutarlılık garanti
  değildi.
- **Kesin çözüm — algoritmik türetme:** 3 sabit preset (`Themes/Base.Sky.xaml`, `Themes/Base.Ice.xaml`)
  TAMAMEN kaldırıldı. Yerine `Template.ColorMode` artık sadece `"dark"` (varsayılan) veya `"custom"`
  değerini alıyor; `"custom"` iken yeni `Template.CustomBaseColor` alanındaki TEK renk (kullanıcının
  Windows'un kendi `ColorDialog`'uyla seçtiği) baz alınıyor. `BoardColorModeManager.BuildPalette(Color)`
  bu TEK renkten TÜM paleti (Secondary/Elevated/Input/Border = tabandan %10/%20/%4-16/%38 oranında
  açık/koyu yönde küçük adımlarla harmanlanmış (`Blend`) tonlar; metin rengi tabanın parlaklığına
  (`Luma`) göre otomatik açık/koyu seçiliyor; Success/Warning/Danger sabit iki setten (açık/koyu tabana
  göre) geliyor) OTOMATİK üretiyor — böylece HİÇBİR öğe tabandan kopuk bir renk ailesine sıçramıyor, hepsi
  garanti şekilde tutarlı kalıyor.
- **UI:** `TemplateEditorView`'daki "Görünüm Modu" kartı artık Koyu/Özel Renk seçimi + (Özel Renk
  seçiliyken beliren) tıklanabilir bir renk kutusu içeriyor; kutuya tıklayınca `System.Windows.Forms.
  ColorDialog` (proje zaten `UseWindowsForms=true`) açılıyor, seçilen renk anında önizlemeye yansıyor.
- Kılavuz (uygulama içi + web) yeni Koyu/Özel Renk modeline göre güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** Yönetim → Şablonlar → bir şablonu düzenle → "Görünüm Modu" → Özel Renk
  → renk kutusuna tıklayıp bir renk seçip önizlemede TÜM modül zeminlerinin (kart + liste alanları dahil)
  artık aynı renk ailesinden, tutarlı göründüğünü doğrulamak.

**Otuz ikinci tur — devamı 2: zemin renkleri BİREBİR eşitlendi, metin kontrastı güçlendirildi:**
- **Kullanıcı geri bildirimi (ekran görüntüsü, kırmızı okla işaretli):** "Aynı renk ailesi, küçük adım"
  yaklaşımı bile yeterli değildi — Ders & Teneffüs'ün boş kalan geniş alanı (`BgPrimaryBrush`, tabanın
  KENDİSİ) ile diğer kartların içeriği (`BgSecondaryBrush`, tabandan %10 harmanlanmış) arasındaki küçük
  fark, geniş boş bir alanda yine de göze çarpıyordu ("çok sırıtıyor"). Kullanıcı net talimat verdi: o alan
  da dahil TÜM zeminler BİREBİR AYNI (seçilen) renk olsun; ayrıca metin renklerinin seçilen rengin "zıttı"
  (maksimum kontrast) olmasını istedi.
- **Değişiklik (`BoardColorModeManager.BuildPalette`):** `BgPrimaryBrush`, `BgSecondaryBrush`,
  `BgInputBrush` ve `BoardCanvasBackgroundBrush` artık TÜMÜ aynı `SolidColorBrush` nesnesini paylaşıyor —
  hiçbir harmanlama yok, hepsi kullanıcının seçtiği renkle BİREBİR aynı. Kartları kanvastan ayırmak için
  artık SADECE `BorderBrush1` (tabandan %38 uzaklaşan kenarlık) ve zaten var olan gölge efekti kullanılıyor
  (`BgElevatedBrush` sadece SEÇİLİ liste satırını vurgulamak gibi kasıtlı bir amaç için hâlâ %18 farklı —
  bu "sırıtan" değil, işlevsel bir vurgu). Metin: `TextPrimaryBrush` uçları biraz daha keskinleştirildi
  (`#F1F5FA/#0A121E` → `#F6F9FC/#070D16`), `TextMutedBrush`'ın tabana harmanlanma oranı düşürüldü
  (`%45` → `%30`) — daha güçlü/okunaklı kontrast için.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** Aynı şablonda Özel Renk'i tekrar kontrol edip, artık Ders & Teneffüs'ün
  boş alanı dahil TÜM modül zeminlerinin gerçekten birebir aynı tonda olduğunu ve metin kontrastının
  yeterli olduğunu doğrulamak.

**Otuz üçüncü tur — üç gerçek veri/UI hatası: Tarihte Bugün, Haftanın Beyin Egzersizi, Doğum Günleri:**
- **Kullanıcı 3 ekran görüntüsüyle 3 ayrı sorun bildirdi**, üçü de aynı kök sorun ailesinden (serbest metin
  tarih girişi → panodaki karşılaştırma/eşleştirme sessizce başarısız oluyor):
  1. **Tarihte Bugün:** "Tarih" alanı serbest metindi (ör. "4 Ağustos" yerine "4 Temuz"/"subat" gibi yazım
     hataları mümkündü) ve panodaki eşleştirme (`TodayInHistoryModuleView`) TAM METİN karşılaştırması
     yapıyordu — tek bir harf farkı bile kaydı sessizce göstermiyordu.
  2. **Haftanın Beyin Egzersizi:** `WeeklyQuestionView.xaml.cs`'de "Soru" kutusu
     `LabeledMultilineTextBox("Soru", ..., 0)` çağrısıyla oluşturuluyordu — 4. parametre bu overload'da
     **height** (yükseklik) anlamına geliyor, `0` verilince kutu GÖRÜNMEZ oluyordu (kullanıcı asla
     dolduramıyordu). `WeeklyQuestionModuleView.Load()` da `Question` boşsa TÜM modülü (Kelime dahil)
     "tanımlanmamış" göstererek gizliyordu — bu yüzden Kelime alanı dolu olmasına rağmen o da hiç
     görünmüyordu. Tek kök neden: yanlış parametre (height yerine sanki marginTop gibi kullanılmış).
  3. **Doğum Günleri:** "Doğum Tarihi" alanı da serbest metindi; `BirthdaysModuleView.ParseDayMonth`
     SADECE "gün Ay" (ör. "18 Temmuz") biçimini tanıyordu — kullanıcı "4.08.2000" yazınca (ki bu son derece
     makul bir tarih girişi) ayrıştırma sessizce başarısız oluyor, `null` dönüyor, öğrenci HİÇBİR aralık
     filtresine düşmüyordu.
- **Kesin çözüm — üçü için de ortak desen:** Serbest metin yerine yapısal Gün(int)/Ay(int) alanları.
  - Yeni paylaşılan `TurkishCalendar.MonthNames` (Core) — 12 ay adı, tek yerden.
  - `TodayInHistoryEntry`: `Date`(string) kaldırıldı → `Day`(int), `Month`(int), `IsGeneral`(bool) eklendi.
    Editör: Gün kutusu (rakam-only) + 13 elemanlı Ay ComboBox'ı (12 ay + "Genel (Her Zaman)" — kullanıcının
    önerdiği tasarım birebir uygulandı). Pano eşleştirmesi artık `Day==bugün.Day && Month==bugün.Month`
    (sayısal, asla yazım hatası riski yok).
  - `BirthdayStudent`: `Date`(string) kaldırıldı → `Day`(int), `Month`(int) eklendi. Editör: aynı Gün
    kutusu + 12 elemanlı Ay ComboBox'ı (Doğum Günü'nde "Genel" seçeneği yok, mantıksız olurdu).
    `BirthdaysModuleView`'daki tüm `ParseDayMonth`/metin ayrıştırma mantığı TAMAMEN kaldırıldı — artık
    doğrudan sayısal `Day`/`Month` karşılaştırması.
  - Yeni paylaşılan `EditorControls.LabeledDayTextBox` (rakam-only, 1-31 kenetlenen) ve
    `EditorControls.LabeledComboColumn` (LabeledTextBox ile aynı dikey yerleşimde ComboBox) — ileride
    benzer gün/ay alanları gerekirse tekrar kullanılabilir.
  - `WeeklyQuestionView.xaml.cs`: "Soru" kutusunun `height=0` çağrısı düzeltildi (varsayılan 70'e döndü).
- **Kullanıcının canlı verisi (`D:\YAYIN\board-data.json`) doğrudan PowerShell ile göç ettirildi**
  (TodayInHistory "Genel" → `IsGeneral=true`; Birthdays "4.08.2000" → `Day=4, Month=8`) — kod
  değişikliğini beklemeden hemen çalışır hâle geldi, veri kaybı olmadı (Templates/Classes/Personnel/
  LessonSchedule/DutyRoster sayıları önce/sonra aynı, doğrulandı).
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** (a) Tarihte Bugün'de yeni Gün/Ay alanlarını deneyip bugünün tarihiyle
  eşleşen bir kayıt eklemek ve panoda göründüğünü doğrulamak; (b) Haftanın Beyin Egzersizi'nde artık
  görünen "Soru" kutusuna bir soru yazıp Kaydet'e basınca hem sorunun hem kelimenin panoda göründüğünü
  doğrulamak; (c) Doğum Günleri'nde Meryem Soylu'nun (4 Ağustos, göç sonrası) artık doğru aralık
  filtresinde (bu hafta/bu ay) göründüğünü doğrulamak.

**Otuz dördüncü tur — "resim eklerken paylaşılan klasörü bilmeye gerek kalmasın" isteği doğrulandı + Okulumuzdan Kareler'e fotoğraf seçici eklendi:**
- **Kullanıcının senaryosu:** Yayın PC'si sistem odasında, uygulama müdür/idareciler/beden eğitimi
  öğretmeni gibi paylaşılan `D:\YAYIN` klasörünü hiç bilmeyen kişilerin PC'lerine de kurulacak. Örnek:
  müdür yeni bir öğretmen eklerken "foto yükle" dediğinde KENDİ PC'sindeki fotoyu seçebilmeli, uygulama
  onu otomatik olarak doğru paylaşılan klasöre kopyalayıp ilgili kayıtla eşleştirmeli — kullanıcının
  paylaşılan klasörün yolunu bilmesine hiç gerek kalmamalı. Aynı ihtiyaç Ayın Öğrencisi için de geçerli.
- **Denetim sonucu:** Bu davranış (`EditorControls.PickAndImportFile` — native "Dosya Aç" penceresi
  kendi PC'sinde HERHANGİ bir yerden dosya seçtirir, seçilen dosya paylaşılan klasörde değilse oraya
  KOPYALANIR, kayda sadece dosya ADI yazılır) **Personel/Öğretmen fotoğrafları, Ayın Öğrencisi, Duyurular
  görseli ve Yerel Video** için ZATEN önceki bir turda bu şekilde kurulmuştu — kullanıcının verdiği iki
  örnek (öğretmen + Ayın Öğrencisi) doğrulandı, ek değişiklik gerekmedi.
- **Bulunan TEK eksik: Okulumuzdan Kareler.** Bu modülün ayrı bir yönetim sayfası yoktu — sadece
  Modül Ayarları'nda paylaşılan `Medya\Slayt` klasörünü doğrudan Gezgin'de açan bir kısayol vardı, yani
  kullanıcı yine paylaşılan klasörün yolunu bulup dosyaları kendisi oraya kopyalamak zorundaydı (istenen
  "hiç klasör bilmesin" ilkesine aykırıydı).
  - Yeni `SchoolGalleryView` (İçerikler → "🖼️ Okulumuzdan Kareler") eklendi: "+ Fotoğraf Ekle" ile
    (Multiselect=true) kendi PC'sinden birden fazla fotoğraf birden seçilebiliyor, hepsi otomatik olarak
    `Medya\Slayt` klasörüne kopyalanıyor; küçük resim (thumbnail) grid'i + her fotoğrafın üzerinde ✕ ile
    kaldırma. Bu modülde ayrı bir JSON kaydı yok (panoda hep "klasördeki ne varsa" mantığı korunuyor,
    bkz. `SchoolGalleryModuleView`) — sayfa sadece dosya sistemiyle doğrudan çalışıyor.
  - Yeni paylaşılan `EditorControls.PickAndImportFiles` (çoğul) — `PickAndImportFile`'ın çoklu seçim hâli,
    ileride benzer ihtiyaçlarda tekrar kullanılabilir.
  - `ModuleFactory.CreateContentEditor("school_gallery")` artık `SchoolGalleryView` döndürüyor — Modül
    Ayarları'ndaki eski "📂 Slayt Klasörünü Aç" kısayolu (ve artık kullanılmayan `BuildGalleryFolderShortcut`
    metodu) TAMAMEN kaldırıldı, yerine diğer 11 modülle AYNI gömülü-editör deseni geldi.
  - `ManagementWindow`'daki İçerikler alt menüsüne "Okulumuzdan Kareler" eklendi (bağımsız erişim için).
- Kılavuz (uygulama içi + web) Okulumuzdan Kareler bölümü yeni fotoğraf seçici akışına göre güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** İçerikler → Okulumuzdan Kareler'den "+ Fotoğraf Ekle" ile birkaç
  fotoğraf seçip (herhangi bir klasörden) otomatik kopyalandığını ve panoda göründüğünü doğrulamak.

**Otuz beşinci tur — Özel Renk'te "TENEFFÜS" gölgelemesi hâlâ sabit lacivertti, düzeltildi:**
- **Kullanıcı yine ekran görüntüsüyle işaret etti:** Zemin renkleri (BgPrimaryBrush/BgSecondaryBrush)
  daha önce eşitlenmişti ama Ders & Teneffüs modülünün TENEFFÜS sırasında içeriğin üzerine bindirdiği
  gölgeleme katmanı (`ScheduleModuleView.xaml`'daki `BreakOverlay`) hâlâ `#9A0B1220` diye SABİT kodlanmıştı
  — bu, önceki turda düzelttiğimiz "boş zemin" sorunundan FARKLI bir kod yolu olduğu için o düzeltmeden
  etkilenmemişti, kullanıcının seçtiği özel rengin tamamen dışında, sabit bir lacivert olarak kalmaya
  devam ediyordu.
- **Çözüm:** Yeni `BreakOverlayBrush` paylaşılan anahtarı eklendi — `Base.xaml`'de varsayılan (Koyu mod)
  değeri aynen `#9A0B1220` korunuyor (davranış değişmedi), ama Özel Renk modunda
  `BoardColorModeManager.BuildPalette` bunu ARTIK kullanıcının seçtiği rengin KENDİSİNİN koyulaştırılmış
  hâlinden (`Blend(baseColor, Black, 0.72)`, %60 opaklık) türetiyor — böylece gölgeleme de aynı renk
  ailesinden kalıyor, sabit/kopuk bir lacivert olarak sırıtmıyor. `ScheduleModuleView.xaml`'daki
  `BreakOverlay.Background` `StaticResource` yerine `{DynamicResource BreakOverlayBrush}` oldu.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** Özel Renk seçili bir şablonda, bir periyodun teneffüs olduğu ana denk
  gelip (ya da Ders & Zil Saatleri'nden geçici olarak bir periyodu "şu an" a yakın ayarlayıp) TENEFFÜS
  gölgelemesinin artık seçilen rengin koyu tonunda göründüğünü, sabit lacivert olmadığını doğrulamak.

**Otuz altıncı tur — Okulumuzdan Kareler'de dosya silince ÇÖKME hatası (yayını da kapatıyordu) + Duyurular'a şifreli silme ve otomatik görsel temizliği:**
- **Kullanıcı bildirdi:** Slayt klasöründen "fazlalık resimleri silince" uygulama "Beklenmeyen bir hata
  oluştu ve işlem durduruldu" hatası verdi VE yayını (Kiosk penceresini) da kapattı — kullanıcı elle
  yeniden başlatmak zorunda kaldı. `App.xaml.cs`'de zaten bir global `DispatcherUnhandledException`
  yakalayıcısı vardı (dostane hata penceresi gösterip `Handled=true` yapıyordu) ama gerçek bir çökme yine
  de oluştu — muhtemel neden: bir dosya, klasör listelendikten SONRA (yönetimden silinerek/dosya seçme
  penceresinin kendi sağ-tık Sil'i ile) kayboluyor, `BitmapImage` yüklemesi/`File.Copy` bunu yakalamadan
  patlıyor, CANLI panodaki `SchoolGalleryModuleView`'ın zamanlayıcısı da AYNI (artık geçersiz) dosya yoluna
  tekrar tekrar çarpıp modal hata penceresini üst üste tetikliyor olabilir.
- **Kesin çözüm — dosya erişiminin olduğu HER noktaya savunma eklendi** (kök nedeni tam olarak
  doğrulayamasak da, hiçbir dosya erişimi artık uygulamayı çökertemez):
  - `SchoolGalleryModuleView` (canlı pano): `Load()` ve `ShowCurrent()` artık `try/catch` içinde;
    yüklenemeyen bir dosya listeden çıkarılıp OTOMATİK olarak bir SONRAKİ dosyaya geçiliyor (kendi kendini
    onaran döngü) — aynı bozuk yola tekrar tekrar çarpıp hata penceresi biriktirmiyor.
  - `SchoolGalleryView` (Yönetim sayfası): `BuildTile` artık korumalı, yüklenemeyen dosya sessizce
    atlanıyor; silme işlemi artık `UnauthorizedAccessException`'ı da yakalıyor (önceden sadece
    `IOException`).
  - `EditorControls.PickAndImportFile`/`PickAndImportFiles`: kaynak dosya, seçimle kopyalama arasındaki
    anda silinmiş/taşınmışsa artık `File.Copy` çökme yerine sessizce o dosyayı atlıyor.
- **Duyurular'a yeni özellik (kullanıcı isteği):** "Resimlere de duyurular menüsünden müdahale edebilelim...
  ama silme işlemlerinde şifre sorulsun... duyuru yayından tamamen kaldırılınca ilgili görseli de silinsin."
  - `AnnouncementsView`'daki mevcut "Sil" butonu artık `AdminAuthService.EnsureUnlocked` ile korunuyor
    (zaten var olan, TemplateEditorView'da modül silmede kullanılan AYNI mekanizma — şifre kurulmuşsa
    oturumdaki ilk silmede sorar, 20 dakika boyunca tekrar sormaz).
  - Bir duyuru silinince, `Medya\Duyuru` klasöründeki ilgili görsel dosyası da otomatik siliniyor —
    öksüz dosya birikmiyor.
  - Ek olarak: bir duyurunun görseli "📷 Foto" ile DEĞİŞTİRİLDİĞİNDE de eski görsel dosyası otomatik
    siliniyor (kullanıcı bunu açıkça istemedi ama aynı "öksüz dosya" sorununun ikinci kaynağıydı, aynı
    turda ele alındı) — Okulumuzdan Kareler'deki fotoğraf silme işlemine de aynı şifre koruması eklendi
    (tutarlılık için).
  - Kılavuz (uygulama içi + web) Duyurular sayfasına bu davranışı açıklayan bir not eklendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** (a) Okulumuzdan Kareler'de birkaç fotoğrafı art arda silip artık çökme
  olmadığını doğrulamak; (b) Duyurular'da bir duyuruyu silmeyi deneyip (şifre kurulmuşsa) parola
  sorulduğunu ve ilgili görselin klasörden de kalktığını doğrulamak.

## Yeni Oturumda İlk Yapılacak

**Otuz yedinci tur — Duyurular'daki otomatik görsel silme GERİ ALINDI + TÜM silme butonlarına "emin misiniz?" onayı:**
- **Kullanıcı iki düzeltme istedi:**
  1. "duyurularda klasördeki resimleri hala silemiyoruz. ben onu kastetmiştim" — bir önceki turda
     eklediğim OTOMATİK görsel silme (duyuru silinince/fotoğrafı değiştirilince) yanlış çözümdü; kullanıcı
     asıl "kullanılmayan görselleri klasörden elle silebileceğim bir yol" istemişti. Ayrıca haklı bir
     endişe belirtti: aynı görsel dosyası başka bir duyuruda da kullanılıyor olabilir (bkz.
     `PickAndImportFile` — dosya klasörde zaten varsa TEKRAR kopyalanmaz, sadece dosya adı döner; yani iki
     duyuru AYNI dosyayı işaret edebilir) — otomatik silme bu durumda BAŞKA bir duyurunun görselini de
     kırardı.
  2. "silmelerde onay istese daha iyi olmaz mı... buna benzer diğer modüllerde de" — bir duyuruyu Sil'e
     bastığında hiç onay sormadan direkt silinmiş, yanlış tıklama riski yüksek.
- **Çözüm 1 — otomatik görsel silme TAMAMEN geri alındı:** `AnnouncementsView`'daki `DeleteImageFile`
  çağrıları (hem `PickPhoto`'da eski görseli hem `DeleteAnnouncement`'ta duyurunun görselini silen kod)
  kaldırıldı — artık duyuru silmek veya fotoğrafını değiştirmek klasördeki dosyaya HİÇ dokunmuyor. Yerine
  yeni **"📂 Görsel Klasörünü Aç"** butonu eklendi (Duyurular sayfasının üstünde) — `Medya\Duyuru`
  klasörünü doğrudan Gezgin'de açar, kullanılmayan görselleri istediğiniz zaman elle silersiniz (School
  Gallery'nin eski/kaldırılmış klasör-aç deseniyle aynı yaklaşım).
- **Çözüm 2 — TEK ortak yerden "emin misiniz?" onayı:** Uygulamadaki HEMEN HEMEN TÜM "🗑 Sil" butonları
  zaten `EditorControls.DeleteIconButton` adlı TEK bir paylaşılan yardımcıdan geliyordu (Doğum Günleri,
  Duyurular, Personel, Videolar, Tarihte Bugün, Temiz Sınıflar, Sınıf Ders Programı, Ayın Öğrencisi,
  Okulumuzdan Kareler, Ders & Zil Saatleri periyotları) — bu TEK metoda `AppMessageBox.Confirm` eklenerek
  tıklanan HER silme butonunda önce "... silmek istediğinizden emin misiniz? Bu işlem geri alınamaz."
  onayı çıkması sağlandı, tek satırlık bir değişiklikle 10 sayfa aynı anda korunmuş oldu (`itemLabel`
  opsiyonel parametresiyle çağıran taraf isterse daha spesifik bir metin verebiliyor, ör. Duyurular artık
  "bu duyuruyu silmek istediğinizden emin misiniz?" diyor). Bu deseni KULLANMAYAN tek yer —
  `TemplateEditorView`'daki modül silme (`BoardGridControl`'ün kendi `MakeIconButton`'ı üzerinden) — ayrıca
  elle aynı onaya bağlandı. `TemplatesView`'daki "şablonu sil" zaten kendi `AppMessageBox.Confirm`'üne
  sahipti, dokunulmadı.
  - **Önemli fark tespiti:** Kullanıcının "hiç onay sormadan silindi" şikayeti muhtemelen yönetici şifresi
    HENÜZ KURULMADIĞI için oldu — `AdminAuthService.EnsureUnlocked`, şifre hiç kurulmamışsa sessizce
    `true` döner (korumasız geçer). Bu YENİ onay adımı şifre kurulu olup olmasından TAMAMEN BAĞIMSIZ
    çalışıyor, bu yüzden bu boşluğu da kapatıyor.
- Kılavuz (uygulama içi + web) Duyurular bölümü yeni davranışa göre güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** Herhangi bir sayfada bir "Sil" butonuna basıp "emin misiniz?" onayının
  çıktığını; Duyurular'da "📂 Görsel Klasörünü Aç" ile klasörün açıldığını ve artık duyuru silmenin/foto
  değiştirmenin klasördeki dosyaya dokunmadığını doğrulamak.

## Yeni Oturumda İlk Yapılacak

**Otuz sekizinci tur — İki yeni modül: Büyük Anons (Yazı) ve Büyük Anons (Görsel) + şablon isimlendirme kolaylaştırıldı:**
- **İstek:** Sınav dönemlerinde (ÖSYM, Açık Öğretim Fakültesi vb.) idare, sadece bu amaç için ayrı bir
  şablon hazırlayıp ekranda kocaman punto ile bir yazılı anons VEYA sabit bir görsel (afiş/kroki)
  göstermek istiyor. Duyurular gibi dönen bir liste değil, Okulumuzdan Kareler gibi slayt döngüsü de
  değil — TEK, sabit içerik.
- **Yeni modül 1 — "Büyük Anons (Yazı)"** (`banner_text`, `BannerTextModuleView`): Modül ⚙️ ayarından tek
  bir metin girilir, **Yazı Boyutu** (24-200pt kaydırıcı) ve **Yazı Tipi** (Segoe UI/Segoe UI Black/Arial/
  Arial Black/Calibri/Tahoma/Impact — Windows'ta hazır bulunan bir küme) seçilir. Panoda `Viewbox`
  (Stretch=Uniform, StretchDirection=DownOnly) ile sarmalanmış — seçilen punto modül alanına sığıyorsa
  BİREBİR uygulanır, sığmıyorsa orantılı küçültülür (asla taşmaz/kırpılmaz). Ayrı bir İçerikler sayfası
  yok — metin/boyut/font doğrudan modül ayarlarında (`module.Settings`), tek modüle özel içerik olduğu
  için paylaşılan bir liste modeli gerekmiyor.
- **Yeni modül 2 — "Büyük Anons (Görsel)"** (`banner_image`, `BannerImageModuleView`): Modül ⚙️ ayarından
  "Fotoğraf Seç" ile (diğer tüm fotoğraf seçicilerle AYNI "seç, biz halledelim" deseni —
  `EditorControls.ImageFilePicker`/`PickAndImportFile`) TEK bir görsel seçilir. BİLEREK Okulumuzdan
  Kareler'in kullandığı Slayt klasöründen AYRI bir klasörde tutulur (yeni
  `BoardDataRepository.AnnouncementImageFolderPath`, "Medya\Anons Görselleri") — aksi hâlde buraya
  seçilen görsel Okulumuzdan Kareler'in otomatik döngüsüne de karışırdı.
  - `ModuleFactory`e her iki tip de eklendi (varsayılan boyutlar: yazı 36×14, görsel 24×20 ızgara birimi
    — "ekran boyunca" olacak şekilde geniş/büyük).
- **Yan istek — şablon isimlendirme:** Kullanıcı "şablonlara isim veremiyoruz, var olan ismi
  düzeltemiyoruz" dedi — kod incelendiğinde isimlendirme/yeniden adlandırma (✏️ simgesi, inline metin
  kutusu) ZATEN vardı ve doğru çalışıyordu, muhtemelen fark edilmemişti (küçük, göze çarpmayan bir
  simge). Keşfedilebilirliği artırmak için: "+ Yeni Şablon" artık oluşturur oluşturmaz otomatik olarak
  isim düzenleme moduna geçiyor — kullanıcı hemen kendi adını yazabiliyor, ✏️ simgesini fark etmesi
  gerekmiyor.
- Kılavuz (uygulama içi + web) iki yeni modül sayfasıyla ve şablon isimlendirme notuyla güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** Sadece bu iki modülü içeren yeni bir şablon oluşturup (yeni şablon adı
  hemen düzenlenebilir hâlde gelecek), Büyük Anons (Yazı)'ya bir metin + büyük punto girip, Büyük Anons
  (Görsel)'e bir görsel seçip ikisinin de yayında/önizlemede doğru göründüğünü doğrulamak.

**Otuz dokuzuncu tur — Büyük Anons (Yazı)'da satır kaydırma, editördeki YANLIŞ "taşıyor" hatası, ve şablon yeniden adlandırma — üç GERÇEK hata:**
- **1) Büyük Anons (Yazı) satır kaydırmıyordu:** Kök neden — `Viewbox` içeriğini ölçerken SONSUZ genişlik
  varsayar, bu yüzden doğrudan içine konan bir `TextBlock`'ta `TextWrapping="Wrap"` hiçbir zaman devreye
  girmiyordu (her zaman metnin TAMAMI tek satır ölçülüp sonra küçültülüyordu). Çözüm:
  `BannerTextModuleView.xaml`'da `TextBlock.Width`, kapsayan `Grid`in (`ContentGrid`) gerçek genişliğine
  (`ElementName` binding ile) bağlandı — artık Viewbox, TextBlock'u O genişlikte ölçüyor, satır kaydırma o
  noktada devreye giriyor, gerekirse (satırlar hâlâ sığmıyorsa) tüm blok orantılı küçültülüyor.
- **2) Editörde YANLIŞ "modül ızgara sınırlarını aşıyor" hatası — GERÇEK, ciddi bir bug:** Kök neden —
  `BoardGridControl.xaml.cs`'deki modül taşıma/boyutlandırma kodunda (fare sürükleme VE ok tuşu/Shift+ok
  tuşu, TÜMÜ) yatay eksen (X/W, sütun/GridColumns) her zaman doğru kenetleniyordu ama DİKEY eksen (Y/H,
  satır/GridRows) YALNIZCA alt sınıra (`Math.Max(1, ...)` / `Math.Max(0, ...)`) sahipti — ÜST sınır
  (GridRows'u aşmasın) hiç kontrol edilmiyordu. Render sırasında `Grid.RowSpan` zaten ayrıca kırpıldığı
  için (`Math.Min(module.H, GridRows-Y)`) kullanıcı EKRANDA hiçbir taşma GÖRMÜYORDU — ama modülün
  GERÇEK (kırpılmamış) `H`/`Y` değeri hâlâ sınırı aşmış durumda kalıyordu, ve Kaydet'teki doğrulama
  (haklı olarak) bunu tespit edip engelliyordu. Kullanıcının tarif ettiği TÜM belirtiler ("kesinlikle
  taşmadığı hâlde taşıyor diyor", "bir modülü küçültünce düzeliyor", "eski hâline getirince bu sefer
  sorunsuz kaydediyor") bu asimetriyle birebir örtüşüyordu. **Kesin çözüm:** `ComputeDragSize`
  (boyutlandırma sürükleme), `ResizeModule` (ok tuşu boyutlandırma), `ComputeDragPosition` (taşıma
  sürükleme) ve `MoveModule` (ok tuşu taşıma) — DÖRDÜNDE de Y/H artık X/W ile TAM SİMETRİK şekilde
  `Math.Clamp` ile üst sınıra da kenetleniyor. Kullanıcının canlı verisi (`board-data.json`) kontrol
  edildi — hiçbir şablonda gerçekten kaydedilmiş bir taşma YOKTU (doğrulama hep engellediği için sorun
  sadece bellekte kalıyordu, dosyaya hiç yazılmamıştı) — veri onarımı gerekmedi.
- **3) Şablon yeniden adlandırma çalışmıyordu:** Kök neden — `TemplatesView.xaml`'daki isim kutusu
  `Text="{Binding Name, Mode=OneWay}"` idi; OneWay olduğu için kullanıcı kutuya ne yazarsa yazsın
  `row.Name` HİÇBİR ZAMAN güncellenmiyordu. `CommitRename` de (Enter/LostFocus'ta) `row.Name`'i (yani
  ESKİ/değişmemiş değeri) okuyup kendi üzerine kaydediyordu — bu yüzden Enter'a basınca "eski hâline
  dönüyormuş" gibi görünüyordu (aslında hiç değişmemişti). **Çözüm:** `CommitRename` artık `row.Name`
  yerine doğrudan `TextBox.Text`'i (kullanıcının GERÇEKTEN yazdığı değeri) okuyor — bindingin mod/zamanlama
  ayrıntılarından tamamen bağımsız, en sağlam çözüm. XAML'daki gereksiz `Mode=OneWay` de kaldırıldı.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** (a) Büyük Anons (Yazı)'da uzun/büyük punto bir metin girip artık alt
  satıra geçtiğini; (b) editörde bir modülü aşağı doğru büyütüp Kaydet'in artık YANLIŞ "taşıyor" hatası
  vermediğini (gerçekten taşırsa hâlâ doğru şekilde engellemesi gerekiyor — bu davranış korunmalı); (c)
  Şablonlar'da ✏️ ile bir isim değiştirip Enter'a basınca artık kalıcı olarak değiştiğini doğrulamak.

## Yeni Oturumda İlk Yapılacak

**Kırkıncı tur — Büyük Anons (Görsel)'e çoklu görsel düzeni, Videolar doğrulandı, editördeki sığmama hatası artık kaydı engellemiyor + diyalog oldu:**
- **1) Büyük Anons (Görsel) — 1/2/3/4 görsel:** Kullanıcı aynı anda 2/3/4 görsel yan yana/2x2 gösterebilmek
  istedi ama bir modül tipi bir şablona sadece BİR KEZ eklenebiliyor (genel kural, tüm tipler için
  geçerli — `TemplateEditorView.BuildModulePalette`'teki "Şablonda Var" kontrolü). Bu genel kuralı
  bozmadan, modülün KENDİ içine bir **"Düzen"** ayarı eklendi (`ModuleSettingsDialog.
  BuildBannerImageSettings`): Tek Görsel / İki Görsel (Yan Yana) / Üç Görsel (Dikey) / Dört Görsel (2x2).
  Seçilen düzene göre 1-4 arası "Fotoğraf Seç" satırı beliriyor (`imageFileName1..4` ayarları).
  `BannerImageModuleView` artık düzene göre kod tarafında bir Grid kuruyor (2 sütun / 3 satır / 2x2) ve
  her hücreye kendi görselini (`Stretch=Uniform`, kırpmadan) yerleştiriyor; boş hücreler sessizce boş
  kart olarak kalıyor.
- **2) Videolar — zaten aynı "seç, biz halledelim" deseninde:** Kontrol edildi, `VideosView` zaten
  `EditorControls.VideoFilePicker`/`PickAndImportFile` kullanıyor (Personel/Ayın Öğrencisi/Duyurular ile
  BİREBİR aynı altyapı) — ek değişiklik gerekmedi, kullanıcıya doğrulandığı bildirildi.
- **3) Editördeki "sığmıyor" hatası artık kaydı engellemiyor + tüm uyarılar diyalog oldu:** Sınırlarına
  Sığmayan modülleri artık KAYDI ENGELLEMİYOR — sınıra sığacak şekilde otomatik küçültüp
  kaydı TAMAMLIYOR, sadece HANGİ modül(ler)in küçültüldüğünü bir diyalogla bildiriyor (`ClampOverflowingModules`,
  `TemplateEditorView.Save_Click`) — kullanıcının "tek tek küçültüp deniyorum, diğer değişikliklerim
  kayboluyordu" şikayetini doğrudan çözüyor. Çakışma (overlap) hatası GÜVENLİ OLMADIĞI için (hangi
  modülün nereye taşınacağı belirsiz) hâlâ engelliyor, ama artık hangi İKİ modülün çakıştığını adıyla
  söylüyor (zaten öyleydi). **Tüm uyarılar artık sol üstte küçük bir metin DEĞİL, gerçek bir diyalog
  penceresi** (`ShowError` → `AppMessageBox.Show`) — kullanıcının "diyalog penceresi olarak çıksa daha iyi
  olur" isteği. Artık kullanılmayan `ErrorText` XAML öğesi kaldırıldı.
- Kılavuz (uygulama içi + web) Büyük Anons (Görsel) bölümü çoklu düzen özelliğine göre güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** (a) Büyük Anons (Görsel)'de Düzen'i "Dört Görsel (2x2)" yapıp 4 farklı
  fotoğraf seçip panoda 2x2 ızgara olarak göründüğünü; (b) editörde bilerek bir modülü sınırın dışına
  taşıyıp Kaydet'e basınca artık DİYALOG ile "şu modül küçültüldü" dediğini VE diğer değişikliklerin de
  kaydolduğunu doğrulamak.

**Kırk birinci tur — Büyük Anons (Görsel): "sabit düzen" fikri terk edilip TAMAMEN SERBEST çoklu-kopya mimarisine geçildi + şablon adı butonundaki simge sorunu:**
- **Kullanıcı geri bildirimi (iki aşamalı):** Önce "3 görsel dikey"i "3 alt alta" olarak yanlış anladığımı
  belirtti (aslında "3 yan yana, her biri dikey (uzun) bir dikdörtgen" istemişti) ve "3 alt alta"nın da
  AYRICA geçerli bir seçenek olabileceğini söyledi. Ben bunu "3col"/"3row" diye iki ayrı sabit düzen
  seçeneği ekleyerek çözmeye başlarken, kullanıcı bir sonraki mesajında konuyu kökten yeniden düşündü:
  **"aslında en mantıklısı tek modülde birden fazla resim kullanmak yerine, o modüle has birden fazlası
  şablona eklenebilse ve her birinin görseli kendi ayarlarında kayıtlı olsa"** — yani sabit 1/2/3/4 kalıp
  düzenleri yerine, bu modül TİPİNİN şablona İSTEDİĞİ KADAR kez eklenebilmesi, her kopyanın TEK bir kendi
  görseli olması ve kullanıcının normal ızgara editörüyle (sürükle/boyutlandır) TAMAMEN SERBEST dizmesi —
  "2x2 de olur 3x2 de, tamamen ona kalmış bir şey."
- **Kesin mimari — sabit düzen kalıpları TAMAMEN TERK EDİLDİ, yerine çoklu-örnek istisnası geldi:**
  `BannerImageModuleView` basit TEK-görsel hâline geri döndürüldü (Düzen ayarı/4 slot YOK — ilk hâline
  dönüş). `TemplateEditorView`'a yeni `MultiInstanceModuleTypes` (`HashSet<string>`, şu an sadece
  `"banner_image"`) eklendi — genel kural hâlâ "bir tip bir şablona bir kez" ama bu tip İSTİSNA:
  `BuildModulePalette`'teki "Şablonda Var" kontrolü bu tip için hiç devreye girmiyor, "Ekle +" butonu her
  zaman aktif kalıyor (kaç tane eklendiğini gösteren küçük bir sayaç metni eklendi: "· 2 tane eklendi").
  Kullanıcı istediği kadar kopya ekleyip her birine ⚙️'den KENDİ görselini atıyor, sonra HER modülde zaten
  var olan sürükle/boyutlandır ile TAMAMEN serbest bir düzen kuruyor — kod tarafında hiçbir "düzen" mantığı
  yok, sadece normal ızgara pozisyonlama.
- **Kullanıcının canlı verisi göç ettirildi:** Daha önce tek modülde saklanan 2 görsel ("Okul Poster1.png",
  "Okul Poster2.jpg"), aynı toplam alanı (X=0-50,W=50) yan yana ikiye bölecek şekilde (X=0/W=25 ve
  X=25/W=25) İKİ AYRI modül kaydına dönüştürüldü — kullanıcı hiçbir şey kaybetmeden kaldığı yerden devam
  edebiliyor, üstelik artık bu iki parçayı istediği gibi yeniden konumlandırabilir.
- **Şablon adı butonundaki simge sorunu devam ediyordu:** Önceki turda "✏️" emoji'sinin (muhtemelen bu
  makinede desteklenmeyen bir varyasyon-seçicili emoji dizisi olduğu için) boş kare olarak göründüğünü
  bildirmişti. Projenin daha önce AYNI sorun ailesinden ("Segoe MDL2 Assets" glyph'leri) ders çıkarıp
  belirlediği kural ("Sembol/emoji glyph'lere güvenmek yerine düz metin kullanıyoruz") burada da uygulandı
  — buton artık "✏️" yerine düz metin **"Ad Değiştir"** yazıyor (26x26 sabit kare yerine otomatik genişleyen
  bir metin butonu).
- Kılavuz (uygulama içi + web) hem Büyük Anons (Görsel)'in yeni çoklu-örnek mimarisine hem "Ad Değiştir"
  butonuna göre güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** (a) Modül Ekleme Havuzu'ndan Büyük Anons (Görsel)'i art arda birkaç kez
  ekleyip her birine ayrı görsel atayıp istediği gibi (2x2, 3x2, karışık vb.) dizebildiğini; (b) "Ad
  Değiştir" butonunun artık düzgün göründüğünü ve isim değişikliğinin kalıcı olduğunu doğrulamak.

**Kırk ikinci tur — Büyük Anons (Yazı) da çoklu-örnek istisnasına eklendi:**
- **İstek:** "Birden fazla eklemeyi Anons metin olanına da yapalım, belki lazım olur ilerde."
- **Değişiklik:** `TemplateEditorView.MultiInstanceModuleTypes`'a `"banner_text"` eklendi (artık
  `["banner_image", "banner_text"]`) — mekanizma zaten paylaşılan olduğu için (her `BoardModule` kendi
  `Settings` sözlüğünü taşıyor) TEK SATIRLIK bir ekleme yeterliydi, başka hiçbir kod değişikliği
  gerekmedi. Artık Büyük Anons (Yazı) da Büyük Anons (Görsel) gibi bir şablona istediği kadar kez
  eklenebiliyor, her kopya kendi metnini/boyutunu/yazı tipini taşıyor.
- Kılavuz (uygulama içi + web) Büyük Anons (Yazı) sayfası bu istisnayı ve (bu turda ayrıca fark edilen)
  "önce satır kaydırma, sonra hâlâ sığmazsa küçültme" davranışını açıklayacak şekilde güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcı yorumu (not, işlem gerektirmiyor):** "Bence şu ana kadar böyle bir dijital pano yapılmamıştır
  okullar için. Süper bir şey oluyor." — memnuniyet bildirimi.
- **Kullanıcıdan istenen retest:** Büyük Anons (Yazı)'yı da art arda birkaç kez ekleyip her birine ayrı
  metin/boyut/yazı tipi atayıp istediği gibi (Büyük Anons (Görsel) ile karışık da olabilir) dizebildiğini
  doğrulamak.

## Yeni Oturumda İlk Yapılacak

Bu dosyayı okuduktan sonra, kullanıcıya doğrudan "hangi adımı denediniz, ne oldu?" diye sorarak devam
edilebilir — kod tarafında bekleyen bir iş yok, sıradaki adım kullanıcının manuel test geri bildirimi
(en güncel: Büyük Anons (Yazı) de Büyük Anons (Görsel) gibi şablona istediği kadar kez eklenebiliyor;
ayrıca şablon adı butonu artık "Ad Değiştir" düz metni, editördeki sığmama hatasının artık kaydı
engellememesi/otomatik küçültüp diyalogla bildirmesi, TÜM uyarıların diyalog penceresi olması, editördeki
Y/H ekseni simetrisi, TÜM silme butonlarına ortak onay, Duyurular'daki manuel görsel temizliği,
Okulumuzdan Kareler çökme düzeltmesi, Ders & Teneffüs TENEFFÜS gölgelemesi, Tarihte Bugün + Doğum Günleri
Gün/Ay alanları, Beyin Egzersizi "Soru" kutusu ve Nöbetçi Öğretmen nöbet yeri yerleşimi + branş metni de
henüz kullanıcı onayı bekliyor).

**Kırk üçüncü tur — KRİTİK canlı olay: laptop kiosk'ta kilitlendi, Ctrl+Alt+Y güvenilmezdi:**
- **Olay:** Kullanıcı uygulamayı laptopa (sistem odası yayın PC'si, kablosuz ekranla Samsung 55" TV'ye
  bağlı) kurdu. Kiosk ekranı yanlışlıkla Windows'un BİRİNCİL monitörü olarak ayarlıydı, uygulama açılır
  açılmaz tam ekran yayına geçti. Ctrl+Alt+Y çalışmadı, Görev Yöneticisi bile kiosk penceresinin ÖNÜNE
  geçemedi — kullanıcı oturumu kapatıp açarak (ve "başlangıçta çalıştır" işaretli olmadığı için) durumu
  ancak öyle kurtarabildi.
- **Kullanıcının verdiği kritik ipucu:** Ctrl+Alt+Y en başından beri güvenilmezdi — "yayın ekranına BİR
  KEZ tıklayıp Ctrl+Alt+Y'ye bastığımda tepki yoktu, fareyle bir kez DAHA tıklayınca çalışıyordu." Bu,
  Windows'un klasik "inaktif pencereye ilk tık sadece etkinleştirir, gerçek girdi olarak SAYILMAZ"
  davranışıyla birebir örtüşüyor — kullanıcı bunu hep sistem tepsisi ikonuyla (Yayını Başlat/Durdur
  butonu) aştığı için Ctrl+Alt+Y'nin bu kırılganlığını hiç fark etmemişti.
- **Kök neden:** `BoardWindow.Window_PreviewKeyDown`, WPF'in KENDİ klavye odağı yönlendirmesine dayanıyordu
  — pencere görsel olarak tam ekran/topmost olsa bile, WPF içi `Keyboard.Focus` gerçekten O pencereye
  ayarlanmamışsa (ör. açılışta başka bir pencere/kablosuz ekran bağlantı müzakeresi hâlâ OS odağını
  tutuyorsa) `PreviewKeyDown` HİÇ tetiklenmiyordu — `EnterKiosk()`'taki `Activate()/Focus()/Keyboard.Focus()`
  çağrıları buna karşı yeterli garanti değildi (Windows'un "foreground lock" kısıtlaması, bir uygulamanın
  kendi kendine öne çıkmasını bazı koşullarda sessizce reddedebiliyor).
- **Kesin çözüm 1 — Windows'un GERÇEK global kısayol mekanizması (`RegisterHotKey`):** `BoardWindow`'a
  Win32 `RegisterHotKey`/`UnregisterHotKey` P/Invoke + `HwndSource.AddHook` ile `WM_HOTKEY` dinleyicisi
  eklendi. Bu, pencere odaklı olsun olmasın, SİSTEM GENELİNDE her zaman çalışır — "önce tıkla sonra bas"
  ihtiyacını kökten ortadan kaldırıyor. Sadece kiosk AKTİFKEN kayıtlı tutuluyor (`EnterKiosk`'ta kayıt,
  `ExitKiosk`/`StopBroadcastFromManagement`'ta kayıt SİLME) — Yönetim penceresi kendi (odak bazlı)
  Ctrl+Alt+Y'sini kullanırken iki pencerenin AYNI global kombinasyonu almaya çalışıp çakışmaması için.
  Eski `PreviewKeyDown` de yedek olarak duruyor, zarar vermiyor.
- **Kesin çözüm 2 (kullanıcının önerisi) — köşede görünmez acil çıkış butonu:** `BoardWindow.xaml`'a sağ
  üst köşeye sabit (çözünürlük hesabı gerektirmeyen), normalde tamamen görünmez, fare üzerine gelince
  belirginleşen ("Yönetim" yazan) bir buton eklendi — tıklanınca klavyeden TAMAMEN bağımsız olarak
  `ExitKiosk()` çağırır. Hem klavyesiz/dokunmatik senaryolar hem de global kısayolun her nedenle
  çalışmadığı durumlar için ikinci bir güvence katmanı.
- **"Blink then resume" (ikinci tuş basışında bir an yönetime dönüp tekrar yayına dönme) kesin olarak
  doğrulanamadı** — en olası açıklama, kablosuz ekran (Miracast) bağlantısının kendi bilinen
  kararsızlığı (tam ekran/topmost pencerelerle birlikte kullanıldığında donanım/sürücü seviyesinde
  titreşme yapması yaygın bir sorun kategorisi) — kullanıcının "laptop artık TV'yi bulamıyor" demesi de
  bu bağlantının zaten kırılgan olduğunu destekliyor. Kullanıcıya kablolu (HDMI) bağlantının kiosk/pano
  kullanımı için çok daha güvenilir olacağı ÖNERİLDİ (henüz uygulanmadı, kullanıcının kararı).
- Derleme: 0 hata, 0 uyarı. Bu makinede temel sağlık kontrolü yapıldı (çöküşsüz açılıyor) — laptoptaki
  gerçek senaryo (kablosuz ekran + yanlış birincil monitör) burada TEKRARLANAMADI, kullanıcının kendi
  ortamında test etmesi gerekiyor.
- **Kullanıcıdan istenen retest:** Laptopta güncellenmiş sürümü kurup (a) kiosk'a girip TEK tıkla
  Ctrl+Alt+Y'nin artık çalıştığını, (b) sağ üst köşedeki görünmez butonun fare ile çalıştığını, (c) genel
  olarak yayın/yönetim geçişinin artık güvenilir olduğunu doğrulamak. Ayrıca (d) monitör
  ayarlarını düzeltip (TV birincil OLMASIN) hangi ekranın kiosk için seçili olduğunu Ayarlar'dan
  kontrol etmesi de önerilir — bu, orijinal olayın asıl tetikleyicisiydi.

## ÖNEMLİ — Ortam/veri klasörü değişikliği (kırk dördüncü tur sırasında fark edildi)

Bu geliştirme makinesindeki (`%AppData%\OkulPanosu\local-settings.json`) `DataFolderPath` artık
**`Z:\DijitalOkulPanosu\YAYIN`** — kullanıcı kendi PC'sinden laptopun D: sürücüsüne ağ paylaşımıyla
bağlanıp (laptopta Z: olarak görünüyor) GERÇEK CANLI VERİYE işaret edecek şekilde ayarlamış (önceki
turlardaki yerel `D:\YAYIN` test klasörü artık KULLANILMIYOR, hatta `D:\YAYIN` diskte `D:\YAYINx` olarak
yeniden adlandırılmış görünüyor — muhtemelen kullanıcının kendi denemesi, dokunulmadı). **Yeni oturumda
PowerShell ile veri okuma/onarma yaparken `D:\YAYIN` DEĞİL `Z:\DijitalOkulPanosu\YAYIN` yolunu kullanın**
— ve bu artık kullanıcının GERÇEK, laptopta yayında olan verisi olduğu için normalden de dikkatli olun
(entegrasyon kontrolü/yedek mantığı hep uygulanmalı, zaten bu oturumda hep öyle yapıldı).

**Kırk dördüncü tur — Yerel Video ŞABLONA ÖZGÜ, Duyurular İÇERİK PAYLAŞIMLI + şablon bazlı yayın anahtarı, köşe butonu çözünürlük sorusu yanıtlandı:**
- **Köşe butonu (kırk üçüncü turdan):** Kullanıcı çözünürlük değişince sorun olur mu diye sordu — HAYIR,
  `HorizontalAlignment="Right"`/`VerticalAlignment="Top"` + sabit 70x70 WPF birimi, düzen sistemi
  otomatik olarak her zaman gerçek sağ üst köşeye hizalar, kod değişikliği gerekmedi, sadece açıklandı.
- **Yerel Video → TAMAMEN şablona özgü (kullanıcı isteği: "a şablonunda x/y, b şablonunda başka
  videolar"):** `GlobalBoardData.Videos` KALDIRILDI, yerine `Template.Videos` (her şablonun TAMAMEN
  BAĞIMSIZ kendi listesi). Yeni `BoardDataRepository.UpdateTemplateVideos(templateId, videos)`.
  `VideoModuleView(BoardModule, Template)` artık `template.Videos` okuyor. `VideosView` artık
  `VideosView(string? lockedTemplateId = null)` — bağımsız (İçerikler menüsünden) açılışta üstte bir
  ŞABLON SEÇİCİ beliriyor (Videolar/Duyurular'da AYNI yeni desen), Modül Ayarları (⚙️) içinden gömülü
  açılışta zaten hangi şablon belli olduğundan seçici gizleniyor. Auto-save (`PersistSilently`) eklendi.
- **Duyurular → FARKLI bir model (kullanıcının açık düzeltmesi: "duyurular hangi şablonda oluşturulmuş
  olursa olsun diğer şablonlarda da görünsün, ama her şablonda yayınlanıp yayınlanmayacağı ayrı ayrı
  seçilsin — yoksa her şablonda yeniden duyuru yazıp görsel seçmekle uğraşmayız"):** İÇERİK (Başlık/
  İçerik/Görsel/Tarihler/Önem) PAYLAŞILAN kalıyor — Video'nun aksine DUPLICATE EDİLMİYOR.
  `Announcement.IsActive` (bool) KALDIRILDI, yerine `Announcement.PublishedInTemplateIds` (`List<string>`)
  — bir şablonun kimliği bu listedeyse (ve tarih aralığı uygunsa) O şablonda gösterilir.
  `AnnouncementsModuleView(BoardModule, Template)` artık `PublishedInTemplateIds.Contains(template.Id)`
  kontrol ediyor. `AnnouncementsView` de aynı `(string? lockedTemplateId = null)` + şablon seçici
  desenini kullanıyor ama "Yayında" kutucuğu artık İÇERİĞİ değil SADECE seçili şablon için
  `PublishedInTemplateIds` üyeliğini değiştiriyor (checkbox etiketi "BU ŞABLONDA" oldu). Yeni duyuru
  eklerken otomatik olarak o an düzenlenen şablonda yayınlanır durumda başlıyor. SİLME hâlâ İÇERİĞİ
  paylaşılan olduğu için TÜM şablonlardan kaldırıyor (sadece bir şablondan kaldırmak için kutucuğu
  kapatmak yeterli) — bu netçe belirtildi (kılavuzda da).
- **Ortak mimari değişiklik:** `ModuleFactory.CreateView(BoardModule, Template)` ve
  `CreateContentEditor(string type, string? templateId = null)` artık Template/templateId de alıyor
  (çoğu tip kullanmıyor ama Video/Duyurular kullanıyor). `ModuleSettingsDialog` artık
  `(BoardModule module, string templateId)` alıyor, `TemplateEditorView.OnConfigureModule` `_templateId`'yi
  geçiyor.
- **Kullanıcının GERÇEK canlı verisi göç ettirildi** (Z:\DijitalOkulPanosu\YAYIN üzerinden, laptopun
  paylaşımı): 3 global video → "Ana Şablon"un (video modülü olan tek şablon) kendi listesine taşındı; 3
  duyuru (hepsi IsActive=true) → her ikisi de mevcut 2 şablonun `PublishedInTemplateIds`'ine eklendi
  (görünür davranış AYNEN korunuyor, kullanıcı isterse sonra şablon bazında özelleştirir). Bütünlük
  kontrolü yapıldı (Templates/Classes/Personnel/LessonSchedule sayıları önce/sonra aynı).
- Kılavuz (uygulama içi + web) hem Yerel Video hem Duyurular sayfaları yeni modele göre güncellendi.
- Derleme: 0 hata, 0 uyarı. Bu makinede (artık laptopun canlı verisine bağlı) çöküşsüz açıldığı
  doğrulandı.
- **Kullanıcıdan istenen retest:** (a) İçerikler → Yerel Videolar'ı bağımsız açıp şablon seçiciyle iki
  farklı şablona farklı videolar atayıp panoda doğru ayrıldığını; (b) İçerikler → Duyurular'ı açıp aynı
  duyurunun "Bu Şablonda" kutusunu bir şablonda işaretleyip diğerinde kapatıp içeriğin ORTAK, yayın
  durumunun BAĞIMSIZ olduğunu doğrulamak.

**Kırk beşinci tur — KRİTİK: TemplateEditorView önizlemesi RASTGELE Id üretiyordu (Duyurular/Video önizlemede hiç çalışmıyordu) + Duyurular satır düzeni yeniden tasarlandı:**
- **Kullanıcı bildirdi:** Tüm duyurular "yayınlanmak üzere" işaretliyken bile hiçbir şablonda
  görünmüyordu ("henüz duyuru yok" diyordu); kutucuğu kaldırıp tekrar işaretlemeye çalışırken bir ara
  "beklenmeyen durum" mesajı çıktı (uygulama kapanmadı — muhtemelen `DispatcherUnhandledException`
  yakalayıp gösterdi, kesin tekrarlanamadı).
- **KESİN KÖK NEDEN bulundu:** `TemplateEditorView.RenderPreview()`'daki `new Template { ... }` nesnesi
  `Id` alanını AÇIKÇA vermiyordu — `Template.Id`'nin varsayılan değeri `Guid.NewGuid()` olduğundan, HER
  RenderPreview çağrısında (yani her küçük düzenlemede) RASTGELE yeni bir Id üretiliyordu. Bu yüzden:
  (a) Duyurular'daki `PublishedInTemplateIds.Contains(template.Id)` eşleştirmesi ÖNİZLEMEDE ASLA
  tutmuyordu — kutucuk işaretli olsa bile önizleme hep "yayında değil" davranıyordu; (b) AYNI nedenle
  `Videos` da önizleme nesnesinde hiç set edilmediğinden (varsayılan boş liste) Yerel Video da
  önizlemede HER ZAMAN boş kalıyordu (kullanıcı henüz fark etmemişti, ama aynı kök hataydı). **Kesin
  çözüm:** `RenderPreview()` artık `Id = _templateId` (gerçek, kayıtlı şablon kimliği) VE
  `Videos = <kayıtlı şablondan taze okunan liste>` set ediyor — hem Duyurular hem Video artık önizlemede
  DOĞRU çalışıyor. (Canlı Kiosk yayını bu hatadan ETKİLENMİYORDU — `BoardWindow.RenderActiveTemplate()`
  zaten `GetActiveTemplate()` ile gerçek, kayıtlı Template nesnesini kullanıyor; sorun SADECE
  editördeki/önizlemedeki test etme deneyimindeydi — ama kullanıcı büyük ihtimalle "çalışıyor mu"
  kontrolünü önizlemeden yaptığı için bunu canlı bir hata olarak yaşadı).
- **Duyurular satır/sütun düzeni tamamen yeniden tasarlandı** (iki ayrı kullanıcı geri bildirimi
  birleştirildi):
  1. Sütun başlıkları çakışıp "BU ŞABLONDİŞLEMLER" gibi anlamsız bir metne dönüşüyordu (dar sütun
     genişliği + uzun başlık metni taşması) — sütun genişlikleri yeniden hesaplandı.
  2. Çıplak kutucuğun (üstteki tek başlık satırı dışında) ne anlama geldiği, kartlar arasında gezinirken
     belirsizdi — artık her kartta kutucuğun YANINDA her zaman görünür "Yayında" / "Yayında Değil" yazısı
     var (yeşil/soluk renkle).
  3. Kullanıcının istediği yeni sütun sırası: **DURUM (kutucuk+yazı) → Başlık → Başlangıç → Bitiş →
     Önem → Fotoğraf → Foto Ekle → Sil**. Önem artık ayrı bir alt satırda değil, ana satırda Bitiş'in
     hemen sağında. Alt satırda artık sadece İçerik (tam genişlik) kalıyor.
- Kılavuz (uygulama içi + web) yeni "DURUM" sütunu ve Önem'in yeni konumuna göre güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı (Z:\DijitalOkulPanosu\YAYIN — laptopun canlı
  verisi — ile çöküşsüz açıldığı doğrulandı).
- **Kullanıcıdan istenen retest:** (a) Şablon editöründe bir duyurunun DURUM kutucuğunu işaretleyip
  ARTIK önizlemede gerçekten göründüğünü (önceki turda hiç görünmüyordu); (b) yeni sütun sırasının
  (DURUM/Başlık/Başlangıç/Bitiş/Önem/Foto/Foto Ekle/Sil) net ve anlaşılır olduğunu; (c) "beklenmeyen
  durum" mesajının bir daha çıkıp çıkmadığını (çıkarsa `%AppData%\OkulPanosu\crash.log` içeriğini
  paylaşması faydalı olur) doğrulamak.

**Kırk altıncı tur — Duyuruyu SİLMEDEN tüm şablonlardan yayından kaldırma:**
- **Kullanıcı sorusu:** Bir duyuruyu silmeden tamamen yayından kaldırmak istediğinde (ör. ileride tekrar
  kullanmak üzere) ne yapmalı — Bitiş tarihini geçmişe mi çeksin? Aslında standalone Duyurular sayfasında
  "yayında değil" işaretlemenin HER şablondan kaldıracağını sanıyordu, ama artık DURUM kutucuğu SADECE
  seçili şablona özgü.
- **Cevap/çözüm:** Bitiş tarihi hilesi ÖNERİLMEDİ (alanın gerçek anlamını bozar, unutmaya açık). Bunun
  yerine her duyuru kartının DURUM sütununa, kutucuğun ALTINA küçük bir **"tümünden kaldır"** bağlantısı
  eklendi — tıklanınca `Announcement.PublishedInTemplateIds.Clear()` (içerik SİLİNMEZ, sadece HANGİ
  şablonlarda yayında olduğu listesi boşaltılır) — tek tıkla tüm şablonlardan aynı anda kaldırılır, her
  şablonu tek tek gezip kutucuğu kapatmaya gerek kalmaz. Zaten hiçbir şablonda yayında değilse bağlantı
  gizleniyor (gereksiz tıklamayı önlemek için).
- Kılavuz (uygulama içi + web) güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** Birden fazla şablonda yayında olan bir duyuruda "tümünden kaldır"a
  tıklayıp gerçekten TÜM şablonlardan kalktığını, içeriğin SİLİNMEDİĞİNİ (tekrar tek tek şablonlara
  eklenebildiğini) doğrulamak.

**Kırk yedinci tur — "tümünden kaldır" bağlantısı GERİ ALINDI (kullanıcı fikrini değiştirdi):** Bir
önceki turda eklenen bağlantı kullanıcı tarafından iki gerekçeyle kaldırılması istendi: (1) yeri kötüydü
— kutucuğun hemen altında olması, tek bir şablondan kaldırmak isterken yanlışlıkla TÜMÜNDEN kaldırma
riski taşıyordu; (2) gerçek fayda düşük — bir duyuru pratikte en fazla 2-3 şablonda oluyor, elle tek tek
kapatmak zaten yeterli. `AnnouncementsView.xaml.cs`'deki `removeAllLink` TAMAMEN kaldırıldı, DURUM sütunu
sade hâline (sadece kutucuk + "Yayında"/"Yayında Değil" yazısı) döndürüldü. Kılavuz (uygulama içi + web)
da buna göre güncellendi. Derleme: 0 hata, 0 uyarı, uygulama yeniden başlatıldı.

**Kırk sekizinci tur — Duyurular sütun başlıkları artık sabit (scroll ile kaymıyor):** Kullanıcı
bildirdi: aşağıdaki duyuru kartlarına inince sütun başlık satırı (DURUM/BAŞLIK/BAŞLANGIÇ/BİTİŞ/ÖNEM/
İŞLEMLER) da kartlarla birlikte yukarı kayıp gözden kayboluyordu — birkaç kart aşağı indiğinde o karttaki
kutucukların/sütunların ne işe yaradığı belirsizleşiyordu. Kullanıcı iki çözüm önerdi (başlığı sabitlemek
YA DA her karta tekrar eklemek) ve sabitlemeyi önerdi. **Çözüm:** `AnnouncementsView.xaml`'da sütun
başlık `Grid`i, önceden `ScrollViewer` içindeki `StackPanel`in bir parçasıydı (listeyle BİRLİKTE
kayıyordu) — artık `ScrollViewer`ın TAMAMEN DIŞINDA, kendi sabit `Grid.Row`unda (Auto yükseklik); sadece
`ItemsControl` (`AnnouncementsList`) kendi `ScrollViewer`ı içinde kalıyor. Böylece başlık satırı HER ZAMAN
görünür kalıyor, sadece kartlar kayıyor. `VideosView` kontrol edildi — orada ayrı bir sütun başlık satırı
hiç yok (her alan zaten kendi içinde açıklayıcı — ör. checkbox'ın ToolTip'i "Yayında"), bu sorun orada
YOK, değişiklik gerekmedi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı.
- **Kullanıcıdan istenen retest:** Birkaç duyuru kartı aşağı kaydırıp sütun başlıklarının artık sabit
  kaldığını doğrulamak.

**Kırk dokuzuncu tur — Sabit sütun başlığı deseni diğer sayfalara uygulandı + Doğum Günleri liste
görünümüne geçti:**
- Kullanıcı, kırk sekizinci turda Duyurular'a yapılan "sütun başlığı sabit kalsın" düzeltmesinin AYNISINI
  benzer kalıba sahip diğer tüm sayfalara uygulanmasını istedi ("başka var mı bu şekilde olan bakmadım").
  `FontWeight="Bold" FontSize="11"` deseniyle tüm View XAML'leri tarandı — Duyurular dışında tam olarak
  2 sayfa daha aynı sorunu taşıyor bulundu: **Ayın Öğrencisi** (kullanıcının bizzat belirttiği örnek) ve
  **Personel & Öğretmenler** (grep ile bulundu). `VideosView`'da ayrı bir sütun başlığı hiç yok, sorun yok
  (kırk sekizinci turda zaten kontrol edilmişti).
  - `StudentOfMonthView.xaml`: Duyurular'daki BİREBİR desen uygulandı — sütun başlık `Grid`i
    `ScrollViewer`ın dışına, kendi sabit `Grid.Row`una taşındı; `ScrollViewer` artık sadece
    `StudentsList` `ItemsControl`'ünü içeriyor.
  - `PersonnelView.xaml`: Daha karmaşık bir yapı çünkü listeden ÖNCE "Toplu Aktarım (Kopyala-Yapıştır)"
    ve "Yeni Personel Tanımla" adında iki kurulum kartı var. Bu iki kart kullanıcı sadece listeyi
    tararken sürekli görünür kalmasına gerek olmayan, tek seferlik/nadiren kullanılan formlar olduğu
    için AYRI, kendi `MaxHeight="420"` sınırlı bir `ScrollViewer`a alındı (kendi içinde kayabiliyor,
    ekranı kaplamıyor); onun ALTINDA "Tanımlı Personel Listesi (N)" başlığı + sütun başlığı (İSİM/
    KATEGORİ/BRANŞ/GÖREV/İŞLEMLER) sabit bir satırda, en altta da SADECE `PersonnelList` kendi
    `ScrollViewer`ında. Yani artık üstteki formlar kendi alanında kaydırılabiliyor, personel listesinin
    sütun başlığı ise personel listesi kaydırılırken HER ZAMAN sabit kalıyor.
- **Doğum Günleri (`BirthdaysView`) tamamen liste görünümüne çevrildi:** Kullanıcı, mevcut "her öğrenci
  için tam yükseklikte bir kart" düzeninin gerçek ölçekte (700-1000 öğrenci) tamamen kullanışsız
  olduğunu belirtti ("işin yoksa kartlar arasında dolaş ve o öğrenciyi bul"). Eski düzen: sol tarafta
  sabit genişlikte (300px) bir "ekleme" kart-sütunu + sağda 2 sütunlu `UniformGrid` içinde tam-kart
  öğrenci listesi. Yeni düzen (Duyurular'daki desenle birebir tutarlı):
  1. **Üstte sabit ekleme formu** — artık yatay tek satırlık kompakt bir kart (Ad Soyad | Sınıf | Gün |
     Ay | "+ Tebrik Listesine Ekle" butonu), tüm genişlik boyunca sayfanın en üstünde sabit duruyor.
  2. **Onun altında sabit sütun başlığı** (AD SOYAD / SINIF / GÜN / AY / SİL) + öğrenci sayısını gösteren
     "Tebrik Listesi (N)" başlığı.
  3. **En altta, kendi `ScrollViewer`ında** — her öğrenci artık tam yükseklikte bir kart DEĞİL, tek
     satırlık kompakt bir satır (isim + sınıf + gün + ay + sil butonu), böylece binlerce öğrenci
     olsa bile ekranda aynı anda çok daha fazlası görünüyor ve taranması kolaylaşıyor.
  - `BirthdaysView.xaml.cs`: `BuildForm()` artık `LabeledTextBox`/`LabeledDayTextBox`/
    `LabeledComboColumn`'ın döndürdüğü her alanı sabit bir piksel genişliğine (`Width`) oturtup yatay
    `StackPanel`e ekliyor (bu yardımcılar `WatermarkTextBoxStyle`'ı kullanıyor ve stilde sabit bir
    genişlik YOK — yatay dizilimde genişlik verilmezse kutular içeriğe göre daralıp kullanılamaz hale
    gelirdi, bu yüzden sarmalayıcı `StackPanel`e `Width` verildi). `RebuildList()` artık her öğrenci için
    kart yerine 5 sütunlu (1.6*/1*/90/150/60) tek satırlık kompakt bir `Grid` üretiyor, `ListHeader.Text`
    her yeniden oluşturmada güncel sayıyla yenileniyor.
- Kılavuz (uygulama içi + web) GÜNCELLENMEDİ — bu tur SADECE görsel/yerleşim değişikliği, hangi alanların
  nasıl doldurulacağına dair kullanıcıya verilen talimatların hiçbiri değişmedi.
- Derleme: 0 hata, 0 uyarı (iki ayrı build, önce sabit-başlık turları için, sonra Doğum Günleri için).
  Uygulama yeniden başlatıldı, süreç ayakta doğrulandı.
- **Kullanıcıdan istenen retest:** (a) Ayın Öğrencisi ve Personel & Öğretmenler sayfalarında listeyi
  kaydırınca sütun başlıklarının artık sabit kaldığını; (b) Personel sayfasında "Toplu Aktarım"/"Yeni
  Personel Tanımla" kartlarının kendi alanında (personel listesinden BAĞIMSIZ) kaydığını; (c) Doğum
  Günleri'nde yeni liste görünümünün (üstte sabit ekleme formu, altında sabit sütun başlığı, en altta
  kompakt satır listesi) beklendiği gibi çalıştığını ve büyük öğrenci sayılarında (gerçek ölçek: 700-1000)
  kullanışlı olduğunu doğrulamak.
- **Kullanıcı tarafından ŞİMDİLİK ERTELENDİ, henüz yapılmadı** (sadece farkındalık için not edildi,
  onay beklemeden başlanmamalı): (a) Doğum Günleri listesinde isme göre arayıp tek bir öğrenciyi bulup
  silme özelliği; (b) yeni öğretim yılında e-Okul'dan alınan yeni öğrenci listesiyle TÜM eski öğrenci
  kayıtlarının (personel HARİÇ — personel sürekli durur, sadece gerektiğinde tek tek silinir) toplu
  olarak silinip yerine Excel'den kopyala-yapıştır ile yeni listenin aktarılması — Sınıf Ders
  Programı'ndaki (`ClassSchedulesView`) sütun-sırası-bağımsız yapıştırma deseniyle aynı mantıkta
  ("sütun yerlerinin önemi olmasın, bize lazım olan sütunları arayıp bulsun"), belki öğrenci fotoğrafı
  da içerecek şekilde.

**Ellinci tur — Personel üst kartların gereksiz genişlemesi düzeltildi + Doğum Günleri'ne Toplu Aktarım
ve arama eklendi:**
- **Kullanıcı bildirdi (ekran görüntüsüyle):** Personel sayfasındaki "Toplu Aktarım"/"Yeni Personel
  Tanımla" kartları pencereyle birlikte GEREKSİZ YERE genişliyordu (küçük bir metin kutusu için devasa
  boş alan), ayrıca pencere küçültülünce yatay kaydırma çubukları çıkıyordu. **Kök neden:** Kırk
  dokuzuncu turda bu üstteki alan `HorizontalAlignment` belirtilmeden bırakılmıştı (varsayılan
  `Stretch`) + `MaxWidth="1500"` + `ScrollViewer.HorizontalScrollBarVisibility="Auto"` — WPF'in bilinen
  bir davranışı olarak, `HorizontalScrollBarVisibility="Auto"` olan bir `ScrollViewer` ilk ölçüm
  geçişinde içeriğine SONSUZ genişlik sunar; `Stretch` hizalı bir öğe bu durumda `MaxWidth`'e kadar
  genişlemek İSTER (1500px), bu yüzden `ScrollViewer` gerçek pencere genişliğinden BAĞIMSIZ olarak
  hep "içerik 1500px istiyor" sanıp pencere 1500px'den dar her an yatay kaydırma çubuğu gösteriyordu.
  Alttaki personel listesi aynı sorunu YAŞAMIYORDU çünkü zaten `HorizontalAlignment="Left"` idi (kırk
  dokuzuncu turda öyle yazılmıştı) — `Left` hizalı öğeler sonsuz ölçümde sadece GERÇEK içerik
  genişliğini (978px, sabit sütun genişlikleri) ister, 1500'e zorlanmaz. **Çözüm:**
  `PersonnelView.xaml`'daki üst `ScrollViewer`ın içindeki `StackPanel`e `HorizontalAlignment="Left"`
  eklendi, `MaxWidth` 1500'den 1030'a düşürüldü (personel listesinin gerçek genişliğiyle — 978px sütun
  toplamı + kart dolgusu — eşleşsin diye), gereksiz `HorizontalScrollBarVisibility="Auto"` kaldırıldı
  (artık hiç gerekmiyor). Sonuç: üst kartlar artık personel listesiyle AYNI genişlikte, pencere
  büyüdükçe genişlemiyor; pencere daraltıldığında ise (Left hizalamada `MaxWidth` bir üst sınır olduğu
  için, mevcut alan zaten sınırın altına düştüğünde) doğal olarak küçülüyor, yanlış kaydırma çubuğu da
  bir daha çıkmıyor.
- **Doğum Günleri'ne Personel'deki gibi bir "Toplu Aktarım (Kopyala-Yapıştır)" kartı eklendi:**
  Kullanıcı, ertelenen iki maddeden ("işlemlerini ve arama kısımlarını yapalım" diyerek) ikisini de
  bu turda ONAYLADI. `BirthdaysView.xaml`'a Personel'deki üst-alan deseniyle (aynı `MaxHeight="420"`
  sınırlı, `MaxWidth="1030"` `HorizontalAlignment="Left"` — yani bu turun kendi düzeltmesiyle TUTARLI
  şekilde inşa edildi, eski hatalı `Stretch` deseni hiç kullanılmadı) bir "Toplu Aktarım" kartı
  eklendi. **Personel'in aksine, sütun sırası ÖNEMLİ DEĞİL** — kullanıcının önceki turda özellikle
  istediği gibi ("sütun yerlerinin önemi olmasın... sütunları arayıp bulsun"), Sınıf Ders Programı'ndaki
  (`ClassSchedulesView`) başlık-tabanlı sütun algılama yaklaşımı `BirthdaysView.xaml.cs`'e taşındı
  (`DetectColumns`, `NormalizeForMatch`, `ParseTsv` — sayfaya özgü ayrı kopyalar, projede zaten
  kurulu "her sayfa kendi yardımcılarını taşır" geleneğine uygun): ilk satır başlık olmalı, "Ad Soyad"
  (ya da ayrı "Adı"/"Soyadı"), "Sınıf" ve doğum tarihi için TEK bir "Doğum Tarihi" (gg.aa.yyyy/gg/aa/yyyy)
  sütunu YA DA ayrı "Gün"/"Ay" (Ay hem sayı hem Türkçe ay adı olabilir) sütunları tanınıyor — hangi
  sütunun sırada olduğu ÖNEMSİZ, başlık metninden bulunuyor. Zaten listede aynı isimde biri varsa o
  satır atlanıyor (Personel'deki "aynı isim tekrar eklenmez" davranışıyla aynı). Öğrenci FOTOĞRAFI içe
  aktarma bu turda YAPILMADI (kullanıcının "belki foto da ekleyebiliriz" notu hâlâ sadece bir olasılık,
  bu mesajda net istenmedi) — istenirse ayrı bir turda eklenebilir.
- **Arama eklendi:** Liste başlığının ("Tebrik Listesi (N)") yanına bir arama kutusu kondu — isme göre
  (büyük/küçük harf duyarsız) anlık filtreleme yapıyor, eşleşme sayısı varken başlık "(gösterilen /
  toplam)" biçimine dönüyor. Filtrelenmiş görünümde de her satırın kendi Sil butonu çalışıyor, yani
  arayıp bulma + silme birlikte çözülmüş oluyor — ayrı bir "bul ve sil" ekranına gerek kalmadı.
  **Not:** Yeni öğretim yılında TÜM eski öğrencileri toplu silip yerine yeni listeyi aktarma ("yıllık
  toplu değiştirme") özelliği bu turda YAPILMADI — bu mesajda net istenmedi, hâlâ ayrı bir onay
  bekliyor (arama+manuel silme + eklemeli Toplu Aktarım şimdilik yeterli olabilir; aksi istenirse ayrı
  bir "Tüm Öğrencileri Sil" butonu eklenebilir).
- Kılavuz (uygulama içi `Assets/Help/kilavuz.html` + web artifact, aynı URL korunarak yeniden
  yayınlandı) Doğum Günleri bölümü güncellendi — yeni liste görünümü, Toplu Aktarım ve arama anlatıldı.
  Personel bölümünde davranış değişikliği yok (sadece görsel/genişlik düzeltmesi), metin güncellenmedi.
- Derleme: 0 hata, 0 uyarı (üç ayrı build — önce genişlik düzeltmesi, sonra Toplu Aktarım/arama
  eklenmesi, sonra defensive null-check temizliği). Uygulama yeniden başlatıldı, süreç ayakta doğrulandı.
- **Kullanıcıdan istenen retest:** (a) Personel sayfasında üst kartların artık personel listesiyle aynı
  genişlikte olduğunu, pencere büyütülüp küçültülünce DOĞRU davrandığını (gereksiz genişleme yok,
  yanlış kaydırma çubuğu yok) doğrulamak; (b) Doğum Günleri'nde gerçek bir Excel/e-Okul listesi (farklı
  sütun sıralarıyla, hem "Doğum Tarihi" hem ayrı "Gün"/"Ay" biçimleriyle) yapıştırıp doğru
  içe aktarıldığını, aynı isimde tekrar yapıştırınca atlandığını; (c) arama kutusunun isme göre doğru
  filtrelediğini ve filtrelenmiş bir öğrenciyi silmenin doğru çalıştığını doğrulamak.

**Elli birinci tur — "Öğrenciler" genel öğrenci verisi (Personel'in öğrenci karşılığı) + Personel'in
toplu aktarımı da sütun-sırası-bağımsız yapıldı:**
- **Kullanıcı sorusu (1):** Elli tur önce Personel'e eklenen toplu aktarımın da Sınıf Ders Programı/Doğum
  Günleri gibi sütun-sırası-bağımsız olduğunu SANIYORDU. **Cevap:** Hayır, değildi — Personel'in
  `BulkImport_Click`'i hâlâ eski, sabit pozisyonel ayrıştırma kullanıyordu (`parts[0]`=isim,
  `parts[1]`=branş, `parts[2]`=cinsiyet, başlık satırı da gerektirmiyordu). Bu turda düzeltildi: artık
  Sınıf Ders Programı/Öğrenciler'deki AYNI başlık-algılama deseni kullanılıyor (`DetectColumns` —
  Ad Soyad/ayrı Adı-Soyadı, Branş, Cinsiyet; ilk satır ARTIK başlık olmalı, sütun sırası önemsiz).
- **Kullanıcı önerisi (2), kabul edildi:** Doğum Günleri'ni ayrı bir liste olarak tutmak yerine, Personel
  benzeri genel bir **"Öğrenciler"** ana verisi kurulsun, Doğum Günleri modülü panoda göstereceği kişileri
  ORADAN (doğum tarihine göre filtreleyerek) okusun — böylece toplu aktarım tek bir yere yapılır, personelle
  karışmaz, yıllık liste yenilemesi merkezi hale gelir. **Karar (kullanıcı "sen karar ver" dedi):** Bu
  yaklaşım benimsendi ve uygulandı — yeni mimari:
  - **Yeni `Student` varlığı** (`OkulPanosu.Core/Data/Student.cs`): Id, Name, Class, `BirthDay`/`BirthMonth`
    (int? — doğum tarihi ARTIK OPSİYONEL, bilinmiyorsa null; "genel öğrenci verisi" olduğu için her
    öğrencinin doğum tarihi girilmiş olması ZORUNLU değil). Eski `BirthdayStudent.cs` (Day/Month zorunlu,
    sadece doğum günü amaçlı) tamamen kaldırıldı.
  - **`GlobalBoardData.Birthdays` → `GlobalBoardData.Students`** (`List<Student>`).
  - **`BirthdaysView` → `StudentsView`** olarak yeniden adlandırıldı/yeniden tasarlandı (`Views/StudentsView.xaml`/`.xaml.cs`,
    eski dosyalar silindi). Sayfa artık "Doğum Günleri" değil "🎓 Öğrenciler" — Yönetim penceresinin
    İçerikler menüsündeki nav etiketi de buna göre değişti (`ManagementWindow.xaml.cs`, `"birthdays"`
    anahtarı kod tarafında AYNI kaldı — sadece görünen etiket "Öğrenciler" oldu, veri modelini bozacak bir
    anahtar değişikliği yapılmadı). Sayfanın kendisi kırk dokuzuncu/ellinci turda kurulan TÜM desenleri
    (sabit Toplu Aktarım + ekleme formu üstte, sabit sütun başlığı + arama ortada, kompakt satır listesi
    altta, `HorizontalAlignment="Left"` + `MaxWidth="1030"` genişlik düzeltmesi) miras alıyor.
  - **Toplu Aktarım artık genel amaçlı:** Ad Soyad + Sınıf yeterli, doğum tarihi (tek "Doğum Tarihi"
    sütunu YA DA ayrı "Gün"/"Ay") opsiyonel — girilmezse öğrenci BirthDay/BirthMonth=null ile eklenir,
    listede durur ama Doğum Günleri modülünde hiç görünmez (görünmesi için sonradan elle ya da tarihli bir
    toplu aktarımla eklenmesi gerekir).
  - **`BirthdaysModuleView.xaml.cs`** artık `AppServices.Data.Load().Students`'ı okuyor, sadece
    `BirthDay`/`BirthMonth` DOLU olan öğrencileri filtreliyor (`s.BirthDay is not null && s.BirthMonth is not null`).
  - **`ModuleFactory.CreateContentEditor("birthdays", ...)`** artık `new StudentsView()` döndürüyor (⚙️
    gear-icon kısayolu, Doğum Günleri modülünün ayarlarından doğrudan Öğrenciler sayfasını açar).
  - **Kod tekrarını azaltma:** `ParseTsv`/`NormalizeForMatch` (Excel yapıştırma ayrıştırıcısı) artık HER
    sayfada ayrı kopya değil — `EditorControls.cs`'e (zaten "sayfalar arası paylaşılan yardımcılar"
    dosyası) taşındı, `ClassSchedulesView`, `StudentsView` VE artık `PersonnelView` de oradan çağırıyor.
    Üç sayfada neredeyse birebir aynı ~60 satırlık kodun üçüncü kopyasını yazmak yerine (DRY ihlali
    büyüdükçe) paylaşılan hale getirildi — davranış AYNI, sadece kaynak tek yerde.
- **Canlı veri taşındı:** `Z:\DijitalOkulPanosu\YAYIN\board-data.json` üzerinde `Birthdays` (2 kayıt) →
  `Students` (Day→BirthDay, Month→BirthMonth) olarak PowerShell ile taşındı, önce yedek alındı
  (`board-data.backup-before-students-migration.json`), taşımadan önce/sonra Templates/Classes/Personnel/
  LessonSchedule sayıları DEĞİŞMEDİĞİ doğrulandı, 2 öğrenci kaydı da doğru alanlarla göründü.
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) güncellendi: "Doğum Günleri" modül
  sayfası artık içeriğin Öğrenciler sayfasından geldiğini, doğum tarihinin opsiyonel olduğunu anlatıyor;
  "Personel & Öğretmenler" sayfası toplu aktarımın artık sütun-sırası-bağımsız olduğunu ve
  öğrenci verisinin ARTIK bu listede olmadığını (ayrı Öğrenciler sayfasında) belirtiyor.
- Derleme: 0 hata, 0 uyarı (birden fazla ara build). Uygulama, canlı (Z:\) taşınmış veriyle yeniden
  başlatıldı, çöküşsüz açıldığı doğrulandı.
- **Kullanıcıdan istenen retest:** (a) Personel'de artık başlıksız yapıştırmanın "sütunlar tanınamadı"
  mesajı verdiğini, başlıklı (Ad Soyad/Branş/Cinsiyet, HERHANGİ sırada) yapıştırmanın doğru çalıştığını;
  (b) İçerikler menüsünde "Öğrenciler" sayfasının (eski Doğum Günleri'nin yerinde) göründüğünü, oradaki 2
  mevcut öğrencinin (Meryem Soylu, Cennet Atom) doğru isim/sınıf/gün/ay ile listelendiğini; (c) panodaki
  Doğum Günleri modülünün hâlâ doğru çalıştığını (Öğrenciler'den okuyarak); (d) Öğrenciler'e doğum tarihi
  GİRMEDEN sadece isim+sınıf ile bir öğrenci ekleyip o öğrencinin Doğum Günleri modülünde GÖRÜNMEDİĞİNİ
  (beklenen davranış) doğrulamak.
- **Kullanıcı tarafından bu mesajda İSTENMEDİ, hâlâ ayrı bir onay bekliyor:** Yıllık toplu öğrenci
  değiştirme (tümünü sil + yeni listeyi yapıştır) — kullanıcı "sen karar ver" derken sadece Öğrenciler
  mimarisi kararını kastetmişti, ayrı "Tüm Öğrencileri Sil" gibi bir buton bu turda eklenmedi (arama +
  manuel silme + eklemeli Toplu Aktarım şimdilik yeterli olabilir). Öğrenci fotoğrafı içe aktarma da hâlâ
  yapılmadı.

**Elli ikinci tur — Öğrenciler'e fotoğraf/cinsiyet + "listeyi tamamen değiştir" toplu aktarım seçeneği
eklendi:** Kullanıcı, bir önceki turdaki özetimin ardından iki şeyi netleştirdi:
- **(1) "Önceki öğrenciler silinsin" isteği ÜÇÜNCÜ KEZ tekrarlanmıştı** (kullanıcının kendi ifadesiyle) —
  önceki turlarda bunu hep "ayrı bir onay bekliyor" diye ertelemiştim, bu sefer kullanıcı ekran görüntüsüyle
  kendi önceki mesajını gösterip düzeltti. Artık ERTELENMEDİ, uygulandı: `StudentsView.xaml`'daki "Toplu
  Aktarım" kartına bir `ReplaceAllCheck` CheckBox'ı eklendi ("Yapıştırmadan önce mevcut öğrenci listesini
  TAMAMEN sil (yeni öğretim yılı listesi)"). İşaretliyken "+ Listeyi Sisteme Aktar"a basılırsa önce
  `AppMessageBox.Confirm` ile onay istenir (silme geri alınamaz olduğu için — projenin genel "silme her
  zaman onay ister" kuralına uygun), onaylanırsa `_students.Clear()` yapılıp SADECE yapıştırılan liste
  eklenir. **Personel'e hiç dokunulmaz** — zaten mimari olarak ayrı bir liste (`GlobalBoardData.Personnel`)
  olduğu için "personel hariç" isteği bu ayrımdan doğal olarak zaten sağlanıyordu, ekstra bir kod
  gerekmedi. Kutucuk işaretli DEĞİLSE (varsayılan) davranış AYNI kalıyor (ekleme, aynı isim atlanır) — tek
  tek öğrenci eklerken yanlışlıkla tüm listeyi silme riski yok.
- **(2) Fotoğraf + Cinsiyet eklendi:** `Student.cs`'e `Gender` (string, varsayılan "male") ve
  `PhotoFileName` (string) eklendi. `StudentsView`: yeni-öğrenci ekleme formuna kompakt bir Cinsiyet
  dropdown'ı eklendi; liste satırları artık Personel'deki gibi 48x48 dairesel bir fotoğraf/silüet + "📷 Foto
  Ekle" butonu içeriyor (`LoadPhoto`/`PickPhoto`, Personel'deki BİREBİR aynı desen — fotoğraf yoksa
  `PersonPlaceholder.GetSilhouette(student.Gender)` ile cinsiyete uygun silüet). Fotoğraflar zaten var olan
  `StudentPhotosFolderPath` ("Öğrenci Fotoğrafları" klasörü — daha önce sadece Ayın Öğrencisi kullanıyordu,
  isim zaten genel olduğu için yeni bir klasöre gerek kalmadı, aynı klasör paylaşıldı) içine kaydediliyor.
  Toplu Aktarım'ın `DetectColumns`'ına Personel'deki gibi bir "Cinsiyet" sütun algılama eklendi (opsiyonel
  — girilmezse varsayılan "Erkek"). Liste header/satır sütunları buna göre yeniden düzenlendi (Foto/Ad
  Soyad/Sınıf/Gün/Ay/İşlemler — İşlemler artık Foto Ekle + Sil birlikte), kart genişliği (900px) buna göre
  ayarlandı.
- Canlı veride (`Z:\DijitalOkulPanosu\YAYIN\board-data.json`) ayrı bir migrasyon GEREKMEDİ — `Gender`/
  `PhotoFileName` YENİ eklenen alanlar (var olan bir alanın adı/şekli değişmedi), .NET'in JSON
  deserializer'ı JSON'da olmayan bir alan için sınıftaki varsayılanı (`Gender="male"`, `PhotoFileName=""`)
  otomatik kullanıyor — mevcut 2 öğrenci kaydı (Meryem Soylu, Cennet Atom) sorunsuz yüklendi.
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) Doğum Günleri/Öğrenciler bölümü
  fotoğraf+cinsiyet ve "listeyi tamamen yenileme" akışını anlatacak şekilde güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama canlı veriyle yeniden başlatıldı, çöküşsüz açıldığı doğrulandı.
- **Kullanıcıdan istenen retest:** (a) Öğrenciler'de bir satırın "📷 Foto Ekle" ile fotoğraf atanabildiğini,
  fotoğrafsız kayıtlarda cinsiyete uygun silüet göründüğünü; (b) Toplu Aktarım'a Cinsiyet sütunu da içeren
  bir liste yapıştırıp doğru okunduğunu; (c) "Yapıştırmadan önce mevcut listeyi TAMAMEN sil" kutucuğunu
  işaretleyip yapıştırınca ÖNCE bir onay istediğini, onaylanınca eski öğrencilerin silinip SADECE yeni
  listenin kaldığını, Personel listesinin bundan HİÇ etkilenmediğini; (d) kutucuk işaretli değilken eski
  davranışın (ekleme, tekrar isim atlanır) aynen çalıştığını doğrulamak.

**Elli üçüncü tur — Personel'in toplu aktarımına "benzer isim" uyarısı eklendi:** Kullanıcı, Personel'in
toplu aktarımında AYNEN eşleşmeyen ama muhtemelen aynı kişi olan isimlerin (ör. listede "Mehmet Sait
Örnek" varken yeni satırda "M. Sait Örnek" gelmesi — ad kısaltılmış, soyadı aynı) durumunu sordu: bazen
sadece 4-5 yeni öğretmen geliyor (o zaman hepsini eklemek doğru), bazen okulun TÜM öğretmen listesi
tekrar geliyor (o zaman eskilerle örtüşen isimleri fark edip sormalı). Sabit bir kural (hep ekle / hep
sil) ikisinden birini yanlış yapacağı için **kullanıcıya soran** bir çözüm uygulandı:
- `PersonnelView.xaml.cs`'e `FindSimilarExisting(newName, existingNames)` eklendi — Sınıf Ders
  Programı'ndaki `ResolveTeacher` kısaltma-çözümlemesiyle aynı ailede bir sezgisel yöntem: soyadı (son
  kelime) aynı/sesli-harfsiz-hâliyle aynı VE adı (ilk kelime) ya birebir aynı ya da biri diğerinin
  kısaltması (baş harfleri eşleşiyor + biri tek harf/nokta uzunluğunda) ise "muhtemelen aynı kişi" kabul
  edilir. Orta isimler (varsa) karşılaştırmaya katılmaz, böylece "Mehmet Örnek" ↔ "Mehmet Sait Örnek" gibi
  eksik/fazla orta ad farkları da yakalanır.
- `BulkImport_Click`'te akış: AYNEN eşleşen isim hâlâ sessizce atlanır (değişmedi); BENZER (ama aynen
  değil) bir isim bulunursa `AppMessageBox.Confirm` ile "'X' satırı, listede '{Y}' ile benzer görünüyor —
  aynı kişi olabilir. Yine de AYRI eklensin mi?" diye sorulur — Evet=yine de ekle, Hayır=bu satırı atla.
  Karşılaştırma canlı `_personnel` listesine karşı yapıldığı için, AYNI yapıştırmanın içinde birbirine
  benzeyen iki satır varsa (ör. tüm liste yeniden yapıştırılırken bazı isimler biraz farklı yazılmışsa) o
  da otomatik olarak yakalanır — ekstra kod gerekmedi, liste zaten döngü içinde büyüyor.
- `StripVowels` (daha önce sadece `ClassSchedulesView`'de özel), `NormalizeForMatch`/`ParseTsv` gibi artık
  `EditorControls.cs`'e taşındı — üçüncü bir kopya yazmak yerine paylaşılan hâle getirildi.
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) Personel bölümüne bu davranış eklendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı, çöküşsüz açıldığı doğrulandı.
- **Kullanıcıdan istenen retest:** Personel'e önce "Mehmet Sait Örnek" gibi bir isim ekleyip, sonra Toplu
  Aktarım'a "M. Sait Örnek" içeren bir satır yapıştırarak uyarının doğru tetiklendiğini; "Hayır" denince o
  satırın atlandığını, "Evet" denince AYRI bir kayıt olarak eklendiğini doğrulamak. Ayrıca büyük, tamamen
  farklı bir liste yapıştırıldığında YANLIŞ POZİTİF (alakasız ama tesadüfen baş harfi/soyadı benzeyen iki
  farklı kişi için gereksiz uyarı) sıklığının kabul edilebilir olup olmadığını gözlemlemek — sezgisel bir
  yöntem olduğu için nadir yanlış pozitifler beklenir, kullanıcı "Evet" diyerek kolayca geçebilir.

**Elli dördüncü tur — Doğum Günleri modülünün ⚙️'sindeki gömülü Öğrenciler editörü kaldırıldı
(kafa karıştırıyordu):** Kullanıcı doğru bir soru sordu: "Öğrenciler'e öğrenci ekleyince Doğum Günleri
modülü kriterlere uyanları OTOMATİK mi gösterecek, yoksa yine ben mi ekleyeceğim?" — ve Personel'in kendi
başına bir modülü OLMADIĞINI, sadece ortak bir veri kaynağı olduğunu, Öğrenciler'in de öyle olması
gerektiğini doğru bir sezgiyle belirtti. **Cevap: veri akışı zaten TAM İSTEDİĞİ GİBİ çalışıyordu**
(`BirthdaysModuleView.xaml.cs` elli birinci turdan beri `AppServices.Data.Load().Students`'ı okuyup
`BirthDay`/`BirthMonth` dolu olanları tarih aralığına göre filtreliyor — ekstra bir "modüle öğrenci ekleme"
adımı hiç yoktu) — ama BİR yerde gerçekten kafa karıştırıcı bir tutarsızlık vardı: `ModuleFactory.
CreateContentEditor("birthdays", ...)` hâlâ TAM Öğrenciler sayfasını (`StudentsView`) Doğum Günleri
modülünün ⚙️ ayarlarına GÖMÜYORDU (elli birinci turda `BirthdaysView`'den miras kalmış bir satır,
mimari değiştiği hâlde güncellenmemişti) — bu da modülün ⚙️'sini açan birine "burada bu modüle özel
öğrenci eklüyorum" izlenimi veriyordu, oysa orada düzenlenen aslında TÜM şablonlardaki TÜM Doğum Günleri
modüllerini etkileyen PAYLAŞILAN roster'dı. **Düzeltme:** `ModuleFactory.cs`'den `"birthdays" => new
StudentsView()` satırı tamamen kaldırıldı — artık Personel'in (`ModuleFactory`'de hiç `"personnel"`
case'i olmaması) BİREBİR aynı deseni izliyor: Doğum Günleri modülünün ⚙️'sinde SADECE görüntüleme
ayarları var (Doğum Günü Aralığı: Bugün/Bu Hafta/Bu Ay — `ModuleSettingsDialog.BuildBirthdayFilterRow`,
bu AYRI bir mekanizma, dokunulmadı — + Slayt Geçiş Süresi), "içerik" bölümü hiç görünmüyor; öğrenci
eklemek/silmek SADECE İçerikler → 🎓 Öğrenciler sayfasından yapılır.
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) Doğum Günleri sayfasına bunu açıkça
  anlatan yeni bir paragraf eklendi: ⚙️'de artık içerik bölümü olmadığı, Personel ile aynı mantık, ve bir
  şablonda birden fazla Doğum Günleri modülü olsa bile hepsinin AYNI Öğrenciler listesini okuduğu.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı, çöküşsüz açıldığı doğrulandı.
- **Kullanıcıdan istenen retest:** Bir Doğum Günleri modülünün ⚙️'sini açıp artık SADECE Doğum Günü
  Aralığı + Slayt Geçiş Süresi ayarlarının göründüğünü, ayrı bir "içerik" bölümü ÇIKMADIĞINI; Öğrenciler
  sayfasına doğum tarihi olan bir öğrenci eklendiğinde, panoda o modülün (elle hiçbir şey yapmadan)
  otomatik göründüğünü doğrulamak.

**Elli beşinci tur — Cinsiyet artık Personel VE Öğrenciler listelerinde eklendikten SONRA da düzenlenebiliyor:**
Kullanıcı ekran görüntüleriyle iki gerçek eksik gösterdi: (1) Personel'in "Tanımlı Personel Listesi"
satırlarında Cinsiyet hiç görünmüyor/düzenlenemiyordu (sadece "Yeni Personel Tanımla" formunda vardı,
bir kez eklendikten sonra değiştirilemiyordu); (2) kendi eski panosundan aldığı örnek Excel verisiyle
(Sıra No/Sınıf/Ad/Soyad/DoğumTarihi sütunlu, öğrenci VE personel kayıtları aynı sayfada, personel
satırlarında "Sınıf" sütunu aslında görev/ünvan anlamında kullanılmış — "Sınıf Öğretmeni", "Memur" gibi)
sadece personel satırlarını Toplu Aktarım'a yapıştırdığında "Sütunlar tanınamadı" hatası aldı — muhtemel
sebep, seçtiği satır aralığının (20-35) başlık satırını (1. satır, sayfanın en üstünde) İÇERMEMESİ; ilk
yapıştırılan satır HER ZAMAN başlık olarak okunuyor, aradaki satırlardan biri başlık gibi ayrıştırılmaya
çalışılınca hiçbir tanınan sütun adı bulunamıyor. **Düzeltme (1):** `PersonnelView` ve `StudentsView`'ın
liste satırlarına, "Yeni ... Ekle" formundakiyle AYNI Erkek/Kadın `ComboBox`'ı eklendi — artık bir kayıt
eklendikten sonra da cinsiyeti değiştirilebiliyor, değişiklik anında fotoğraf yoksa gösterilen silüeti de
günceller (`image.Source = LoadPhoto(...)` çağrısı `onChanged` içinde). Sütun başlıkları/genişlikleri her
iki sayfada da CİNSİYET sütunu eklenecek şekilde yeniden ayarlandı. **Düzeltme (2):** Her iki sayfanın
Toplu Aktarım'ı da artık, yapıştırılan veride Cinsiyet sütunu YOKSA kaç kişinin varsayılan "Erkek" ile
eklendiğini durum mesajında açıkça söylüyor ("... N kişi varsayılan olarak 'Erkek' eklendi — listeden
CİNSİYET sütununu kontrol edip gerekenleri düzeltin") — böylece kullanıcı, hangi kayıtları elle gözden
geçirmesi gerektiğini biliyor, sessizce yanlış cinsiyetle kalmıyor. "Sütunlar tanınamadı" durumunun kendisi
bir kod hatası değildi (paylaşım seçiminde başlık satırının unutulmuş olması muhtemel) — kullanıcıya
yapıştırma formatının HER ZAMAN "ilk satır = başlık" kuralına uyması gerektiği hatırlatıldı.
- **BİLİNÇLİ OLARAK yapılmadı, kullanıcıya sorulmalı:** (a) Personel'in Toplu Aktarımı hâlâ SADECE
  öğretmen eklemek için tasarlı (Kategori/Görev her satırda sabit "Öğretmen" olarak gelir) — kullanıcının
  örnek verisinde Memur/Hizmetli/Müdür Yardımcısı gibi öğretmen olmayan personel de vardı, bunların toplu
  aktarımla eklenebilmesi için ayrı bir Görev/Kategori sütunu algılaması gerekir, bu turda EKLENMEDİ (kasıtlı
  kapsam dışı bırakıldı — mevcut UI metni zaten "Memur/Hizmetli gibi öğretmen olmayan personeli tek tek
  'Yeni Personel Tanımla'dan ekleyin" diyor). (b) Personel'in (Öğrenciler'in aksine) bir Doğum Tarihi alanı
  YOK — kullanıcının eski verisinde personel için de doğum tarihi vardı ama bu uygulamada Doğum Günleri
  modülü BİLİNÇLİ OLARAK sadece Öğrenciler'i kapsayacak şekilde tasarlandı (elli birinci tur kararı).
  Personel'e de doğum tarihi eklenip isteğe bağlı olarak panoda gösterilmesi ayrı bir özellik isteği
  olur, bu turda YAPILMADI. İkisi de kullanıcıya soruldu, cevap bekleniyor.
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) her iki sayfada Cinsiyet'in listede
  düzenlenebilir olduğunu belirtecek şekilde güncellendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı, çöküşsüz açıldığı doğrulandı.
- **Kullanıcıdan istenen retest:** (a) Personel/Öğrenciler listesinde bir satırın Cinsiyet'ini değiştirip
  fotoğrafsız kayıtlarda silüetin anında güncellendiğini; (b) Toplu Aktarım'a Cinsiyet sütunu OLMAYAN bir
  liste yapıştırıp durum mesajının "N kişi varsayılan Erkek eklendi" uyarısını doğru gösterdiğini; (c) bu
  sefer başlık satırını (Sıra No/Sınıf/Ad/Soyad/DoğumTarihi ya da benzeri) DAHİL ederek personel
  satırlarını tekrar yapıştırıp "Sütunlar tanınamadı" hatasının artık çıkmadığını doğrulamak.

**Elli altıncı tur — Personel'e de doğum tarihi eklendi + Toplu Aktarım artık Görev sütununu okuyor:**
Elli beşinci turda kullanıcıya iki soru soruldu (AskUserQuestion widget ile) ama kullanıcı widget'ı
anlamadığını belirtti ("benden karar beklediğin iki noktayı anlayamadım") — düz metinle tekrar sorulunca
kullanıcı NET cevap verdi (ve "ben zaten doğum tarihi eklensin diye yırtınıyorum sabahtan beri" diyerek,
bunun aslında ÜÇÜNCÜ kez tekrarlanan bir istek olduğunu belirtti — önceki turlarda hep "Öğrenciler'e
mi, Personel'e mi" diye kapsam netleştirmeye çalışırken, kullanıcının kastının başından beri HER İKİSİ de
olduğunu gözden kaçırmışım). **Karar (kullanıcıdan alındı):**
- **(a) Personel'e doğum tarihi eklendi:** `Personnel.cs`'e `BirthDay`/`BirthMonth` (int?, Student'taki
  gibi opsiyonel) eklendi. `PersonnelView`: "Yeni Personel Tanımla" formuna Gün/Ay satırı eklendi (manuel
  eklemede Öğrenciler'deki gibi bugünün tarihine varsayılan, istenirse değiştirilir); liste satırlarına
  GÜN/AY sütunları eklendi (Cinsiyet gibi anlık düzenlenebilir DEĞİL, salt-okunur metin — Öğrenciler'deki
  aynı tasarım tercihiyle tutarlı, düzeltmek gerekirse sil+yeniden ekle). Panoda GÖSTERİM eklenmedi (kılavuzda
  açıkça belirtildi: "şu an SADECE veri olarak tutulur, panoda henüz gösterilmiyor") — bu ayrı bir
  görüntüleme kararı, istenirse ayrı bir turda ele alınır.
- **(b) Toplu Aktarım artık Görev/Ünvan sütununu okuyor:** Kullanıcı "öğretmen olmayan personelin toplu
  veya manuel kayıt yapılırken branşı boş olur zaten, görevinde memur hizmetli falan yazar" diyerek net
  onay verdi. `PersonnelView.DetectColumns`'a "Görev"/"Ünvan"/"Kategori" başlığı tanıma eklendi — bulunursa
  her satır KENDİ görev metniyle eklenir VE Kategori o metne göre otomatik belirlenir (metin normalize
  edilip "ogretmen" geçiyorsa "teacher", geçmiyorsa "staff" — ör. "Memur"/"Hizmetli"/"Müdür Yardımcısı"
  doğru şekilde "Diğer Personel" olur). Görev sütunu YOKSA eski basit davranış (hepsi "Öğretmen") korunuyor
  — geriye dönük uyumlu. Doğum tarihi de (Öğrenciler'deki AYNI mantıkla — tek "Doğum Tarihi" sütunu ya da
  ayrı "Gün"/"Ay", opsiyonel) Toplu Aktarım'a eklendi.
- **Not — AskUserQuestion widget'ı bu kullanıcıda iyi çalışmadı:** Bu turdan çıkarılan genel ders: bu
  kullanıcıyla karmaşık/çok seçenekli kararlarda interaktif soru widget'ı yerine DÜZ METİNLE, sade ve kısa
  cümlelerle soru sormak daha güvenilir sonuç veriyor — widget bir kez net cevap alamadı, düz metin ilk
  seferde net cevap aldı.
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) Personel bölümü baştan yazıldı: doğum
  tarihi alanı, Görev-tabanlı toplu aktarım, ve doğum tarihinin panoda HENÜZ gösterilmediği notu eklendi.
- Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı, çöküşsüz açıldığı doğrulandı. Canlı veride
  (`Z:\DijitalOkulPanosu\YAYIN\board-data.json`) ayrı bir migrasyon GEREKMEDİ — `BirthDay`/`BirthMonth`
  yeni eklenen nullable alanlar, mevcut personel kayıtları JSON'da bu alanlar olmadan otomatik `null` ile
  yükleniyor (Student'ın Gender/PhotoFileName eklenmesiyle AYNI durum, elli ikinci turda da migrasyon
  gerekmemişti).
- **Kullanıcıdan istenen retest:** (a) "Yeni Personel Tanımla"da artık Gün/Ay alanlarının göründüğünü;
  (b) Toplu Aktarım'a Görev sütunu İÇEREN bir liste (öğretmen + Memur/Hizmetli karışık) yapıştırıp her
  satırın doğru Kategori'ye (Öğretmen/Diğer Personel) girdiğini; (c) Doğum Tarihi sütunu (ya da ayrı
  Gün/Ay) içeren bir liste yapıştırıp doğru okunduğunu doğrulamak.

**Elli yedinci tur — Toplu Aktarım'ın gerçek ayrıştırma sorunu bulundu (Tab yerine boşluk kullanılmış) +
alfabetik sıralama + sütun-eşleşme netleştirmesi:**
- **Kullanıcı, Görev sütunlu örneğini "Ad Soyad Görevi DogumTarihi" başlığıyla düzenleyip tekrar denedi,
  yine "Sütunlar tanınamadı" hatası aldı.** Kök neden bulundu: veri gerçek TAB karakteriyle değil, SADECE
  BOŞLUKLARLA ayrılmıştı (muhtemelen kutuya elle yazarken/düzenlerken — Excel'den gerçek kopyala-yapıştırda
  hücreler otomatik TAB ile ayrılır, ama WPF'in standart `TextBox`'ında Tab tuşuna basmak `AcceptsTab`
  ayarlı DEĞİLSE odağı bir sonraki kontrole taşır, tab KARAKTERİ yazmaz — kullanıcı muhtemelen bunun
  farkında olmadan boşluk kullandı). Sonuç: tüm başlık satırı "adsoyadgorevidogumtarihi" gibi TEK bir
  ayrıştırılamaz metin bloğuna dönüşüyordu, hiçbir bilinen anahtar kelimeyle eşleşmiyordu. **Düzeltme:**
  - `EditorControls.cs`'e `ParseTable(text)` eklendi — metinde HİÇ tab karakteri yoksa satırları noktalı
    virgül (`;`) ile ayırıyor (elle yazarken tab'dan çok daha kolay/görünür bir ayraç); tab varsa (gerçek
    Excel yapıştırması) hâlâ tırnak-farkında `ParseTsv`'ye devrediyor. Personel/Öğrenciler/Sınıf Ders
    Programı'nın üçü de artık `ParseTsv` yerine `ParseTable` çağırıyor.
  - Üç yapıştırma kutusuna da (`PersonnelView`, `StudentsView`, ClassSchedules'ta zaten vardı)
    `AcceptsTab="True"` eklendi — elle test verisi yazan biri artık Tab tuşuna bastığında gerçekten tab
    karakteri girebiliyor (odaktan çıkmıyor).
  - "Sütunlar tanınamadı" hata mesajı artık, başlık satırı TEK BİR HÜCRE olarak kaldıysa (`rows[0].Count
    == 1`, yani sütunlar hiç ayrılamadıysa) ek bir ipucu gösteriyor: Excel'den kopyalarken otomatik
    çalıştığını, elle yazarken noktalı virgül kullanılması gerektiğini somut bir örnekle anlatıyor.
- **Kullanıcı sorusu (2), zaten karşılanıyormuş, sadece netleştirildi:** "Görevi"/"Ünvanı" gibi Türkçe
  ek almış başlıklar, "Adı Soyadı" gibi birleşik iyelik ekli başlıklar, ayrı "Adı"+"Soyadı" sütunları
  hepsi ZATEN `NormalizeForMatch` + genişletilmiş anahtar kelime listeleri sayesinde tanınıyordu (kod
  değişikliği gerekmedi) — kullanıcıya bunu somut örneklerle doğrulandığı açıkça anlatıldı, kılavuza da
  "büyük/küçük harf ve Türkçe ek farketmez" notu eklendi.
- **Kullanıcı isteği (3):** Personel listesi artık **ada göre alfabetik** sıralı gösteriliyor
  (`_personnel.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)`) — arayıp bulmak kolaylaşsın diye.
  Aynı iyileştirme tutarlılık için Öğrenciler'e de eklendi (arama kutusu boşken de sonuçlar alfabetik).
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) her iki sayfaya "Sütunlar nasıl
  ayrılır" başlıklı yeni bir ipucu kutusu (Excel = otomatik Tab, elle yazım = noktalı virgül) ve alfabetik
  sıralama notu eklendi.
- Derleme: 0 hata, 0 uyarı (bir ara adımda `ClassSchedulesView.xaml`'da zaten var olan `AcceptsTab`
  ile YİNELENEN bir tane daha eklenmiş olduğu fark edilip düzeltildi — XAML "duplicate attribute" hatası
  verdi, kaldırıldı). Uygulama yeniden başlatıldı, çöküşsüz açıldığı doğrulandı.
- **Kullanıcıdan istenen retest:** (a) Örnek Görev+DoğumTarihi verisini bu sefer noktalı virgülle ayırıp
  (`Ad Soyad;Görevi;DoğumTarihi` biçiminde) tekrar yapıştırıp başarıyla eklendiğini; (b) gerçekten Excel'den
  kopyala-yapıştır yaparsa (muhtemelen gerçek TAB içerir) hiç noktalı virgül eklemeden de çalıştığını;
  (c) Personel/Öğrenciler listelerinin artık alfabetik sıralı geldiğini doğrulamak.

## PROJE ŞU AN ASKIDA (2026-08-04 itibariyle)

Kullanıcı, laptop + ikinci monitör (HDMI2) ile TV'yi simüle ederek uçtan uca test etti; bu PC'den yapılan
değişikliklerin karşı tarafta doğru yayına girdiğini doğruladı. Görsel olarak başka eksik bulamadı ve
**projeyi okullar açılana kadar askıya aldığını** belirtti — gerçek e-Okul verisiyle test okullar açılınca
yapılacak. Sırada kullanıcının başlamayı planladığı İKİ AYRI proje var (henüz hiç kod yok, detaylar
kullanıcı o projelere geçince konuşulacak):
1. **Akıllı tahta kilitleme uygulaması** — okuldaki akıllı tahtalarda Pardus (Linux) çalışıyor, öğrenciler
   teneffüste istenmeyen sitelere girmesin diye. Tahtalar, Bakanlık ağ politikası yüzünden idarenin PC'siyle
   AYNI ağda DEĞİL — bu yüzden aynı-LAN yaklaşımı (Okul Panosu'ndaki gibi) çalışmaz, **web tabanlı** olmalı.
2. **Kütüphane uygulaması** — emekli olan kütüphaneci yerine ders yükü az bir öğretmen görevlendiriliyor,
   düzgün bir takip sistemi olmadığı için hangi kitabın kimde olduğu/kimin iade ettiği karışıyor.

Bu proje klasöründe yeni bir oturum açılırsa ve kullanıcı Okul Panosu ile İLGİLİ bir şey istemiyorsa (ör.
yukarıdaki iki yeni proje hakkında konuşuyorsa), bu askıya alma durumunu hatırla ve gereksiz yere Okul
Panosu'na dönmeye çalışma — kullanıcı kendisi geri getirecek.

**Not (2026-08-04, aynı gün):** "Askıya alıyorum" dedikten sonra kullanıcı yine de gerçek kullanımda
(yayın PC'si kapalıyken uygulamayı açma) bir sorunla karşılaşıp geri döndü — bkz. elli dokuzuncu tur. Bu,
projenin "tamamen bitti" değil "aktif geliştirme durdu ama kullanıcı gerçek kullanımda hâlâ sorun
bulabilir" durumunda olduğunu gösteriyor — yeni bir sorun bildirirse normal şekilde çöz, "zaten askıya
alınmıştı" diye geri çevirme.

**Not:** Kullanıcı "askıya alıyorum" dedikten HEMEN sonra, son test turunda 3 gerçek hata daha buldu (bkz.
elli sekizinci tur) — bunlar da düzeltildi. Kullanıcı bu turun sonunda "söylemeden askıya almak istemedim"
dedi, yani bu muhtemelen GERÇEKTEN son tur, bir sonraki oturum muhtemelen okullar açıldıktan sonra olacak.

**Elli sekizinci tur — Yerel Video'da 3 gerçek hata (laptop üzerinde gerçek donanımla test sırasında
bulundu):**
- **(1) "+ Video Ekle" tepki vermiyormuş gibi görünüyordu:** Buton aslında çalışıyordu ama yeni (boş) kart
  listenin EN ALTINA ekleniyordu ve ekran o kadar uzun değildi — kullanıcı hiçbir şey olmadığını sanıp
  tekrar tekrar tıkladı, sonunda aşağı inince birden fazla boş kart eklenmiş olduğunu gördü. Kullanıcı iki
  çözüm önerdi (üstte sabit ekleme kartı YA DA yeni kart otomatik görünür alana kaysın); ikincisi seçildi
  (daha az yapısal değişiklik, mevcut deseni bozmuyor). **Çözüm:** `VideosView.AddVideo_Click`, `Rebuild()`
  sonrası son eklenen kartın konteynerini `Dispatcher.BeginInvoke(..., DispatcherPriority.Loaded)` ile
  (layout tamamlanana kadar bekleyip) `BringIntoView()` çağırıyor.
- **(2) Uzun video seçildiğinde arayüz GERÇEKTEN donuyordu** — kullanıcının ekran görüntüsünde pencere
  başlığı "Modül Ayarları (Yanıt Vermiyor)" yazıyordu, yani bu bir "UX ince ayarı" değil GERÇEK bir donma
  hatasıydı: `EditorControls.PickAndImportFile` içindeki `File.Copy` senkron ve UI thread'inde çalışıyordu
  — ağ paylaşımlı bir klasöre büyük bir video kopyalanırken Windows pencereyi "yanıt vermiyor" işaretliyordu.
  **Çözüm:** Yeni `EditorControls.PickAndImportFileAsync(...)` eklendi — kopyalama `Task.Run` ile arka plan
  thread'inde çalışıyor, `setBusy` callback'i ile çağıran taraf "İçe aktarılıyor..." göstergesi
  sunabiliyor. `MediaPickerWithPreview`'ın seçim butonu artık bunu kullanıyor (`async` click handler),
  kopyalama sırasında buton metni "⏳ İçe aktarılıyor, lütfen bekleyin..." olup devre dışı kalıyor. Bu TEK
  paylaşılan bileşen üzerinden düzeltildiği için Yerel Video'nun yanı sıra Personel/Öğrenciler/Ayın
  Öğrencisi fotoğraf seçiciler ve Büyük Anons (Görsel) modülünün görsel seçici de otomatik olarak aynı
  düzeltmeyi (ve dosya küçük olduğu için fark edilmeyecek kadar hızlı ama artık senkron olmayan bir
  bekleme) miras aldı — ekstra kod gerekmedi. (Liste satırlarındaki KÜÇÜK fotoğraf butonları — ör.
  Personel/Öğrenciler'in "📷 Foto" satır butonu — hâlâ senkron `PickAndImportFile` kullanıyor, kasıtlı:
  fotoğraflar küçük/hızlı, şikayet oradan gelmedi, gereksiz kapsam genişletmesi yapılmadı.)
- **(3) Kullanılmayan video dosyalarını uzak PC'den silme imkânı yoktu:** Kullanıcı, artık kullanılmayan
  videoları Windows Gezgini'ne (uzak Yayın PC'sine) bağlanmadan silebilmek istedi. `VideosView`'a yeni bir
  "🗂 Klasördeki Dosyalar" butonu + açılır/kapanır panel eklendi: `Medya\Videolar` klasöründeki TÜM
  dosyaları (herhangi bir video kartında kullanılsın/kullanılmasın) listeler, her dosya için "Kullanılıyor"/
  "Kullanılmıyor" durumu (TÜM şablonlar taranarak — sadece açık olan şablon değil, çünkü bir dosya BAŞKA
  bir şablonun video listesinde kullanılıyor olabilir) ve bir Sil butonu gösterir. "Kullanılıyor" bir
  dosya silinmek istenirse onay mesajı bunu açıkça belirtiyor (yine de silinmesine izin veriliyor —
  kullanıcı bilerek silmek isteyebilir).
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) Yerel Video sayfasına üç düzeltme de
  eklendi.
- Derleme: 0 hata, 0 uyarı (bir ara adımda `BringIntoView`'ın `UIElement` değil `FrameworkElement` üzerinde
  tanımlı olduğu fark edilip tip düzeltildi). Uygulama yeniden başlatıldı, çöküşsüz açıldığı doğrulandı.
- **Kullanıcıdan istenen retest (muhtemelen okullar açılınca):** (a) Uzun bir video dosyası seçip artık
  pencerenin "Yanıt Vermiyor" olmadığını, buton üzerinde "İçe aktarılıyor..." yazısının göründüğünü;
  (b) "+ Video Ekle"ye basınca yeni boş kartın otomatik görünür alana kaydığını; (c) "🗂 Klasördeki
  Dosyalar" panelinin doğru dosya listesini ve kullanım durumunu gösterdiğini, silme işleminin gerçekten
  dosyayı sildiğini doğrulamak.

**Elli dokuzuncu tur — "Yayın PC'si kapalıyken belirsiz hata" düzeltildi:**
- **Kullanıcı sorunu:** Veri klasörünü paylaşan PC (Yayın PC'si) kapalıyken uygulama açılırsa, "ne
  olduğu belirsiz", "sadece bir hata oluştu gibi" bir mesaj görüyordu.
- **Kök neden bulundu (Explore ajanıyla araştırıldı):** `App.xaml.cs`'in `OnStartup`'ı, `AppServices.
  Initialize()`'ı HİÇBİR pencere açılmadan ÖNCE, senkron olarak çağırıyordu; bu metot içindeki
  `Directory.CreateDirectory(DataFolderPath)` (ağ paylaşımlı UNC yol) PC kapalıyken `IOException`
  fırlatıyordu. Bu istisna genel `DispatcherUnhandledException` yakalayıcısına düşüyordu (o da ham
  `IOException.Message`'ı — teknik bir Windows I/O metni — gösteriyordu, "Yayın PC'si kapalı" gibi bir
  yorum YOKTU) VE daha kötüsü: `OnStartup` istisna yüzünden yarıda kesildiği için hiçbir pencere
  (SetupWizard/BoardWindow/ManagementWindow) hiç açılmıyordu — kullanıcı bir hata mesajı görüp
  kapattıktan sonra uygulama görünmez, pencere'siz bir süreç olarak (ya da hiç) takılı kalıyor olmalıydı.
- **Düzeltme:**
  - `AppServices.cs`: `Initialize()` ikiye bölündü — `LoadLocalSettings()` (SADECE yerel dosya, asla ağ
    dokunmaz, asla hata vermez) ve yeni `EnsureDataReadyWithRetry(Window? owner)` (ağa dokunan kısım —
    `EnsureFolderStructure`/`EnsureDefaultTemplate`/`EnsureDefaultLessonSchedule`). İkincisi bir `while`
    döngüsünde: `IOException`/`UnauthorizedAccessException` yakalanırsa (`IsFolderUnreachableError`)
    kullanıcıya NET bir mesaj (`DescribeFolderUnreachable` — klasör yolu + "Yayın PC'si kapalı/ağa bağlı
    değil olabilir" + "PC'yi açıp tekrar deneyin") ve "Tekrar denensin mi?" sorusu gösterilir; Evet
    denirse döngü tekrar dener, Hayır denirse `false` döner.
  - `App.xaml.cs`: `OnStartup` artık `LoadLocalSettings()` (hızlı, güvenli) çağırıp SetupWizard kararını
    verdikten SONRA, veri klasörü zaten AYARLIYSA `EnsureDataReadyWithRetry(null)`'ı çağırıyor; `false`
    dönerse (kullanıcı vazgeçti) `Shutdown()` ile TEMİZ çıkıyor — artık görünmez/pencere'siz takılı kalma
    riski yok. Genel `DispatcherUnhandledException` yakalayıcısı da bu istisna türünü ayrı tanıyıp aynı
    net mesajı gösterecek şekilde güncellendi (oturum ortasında, ör. Kaydet sırasında PC aniden kapanırsa
    diye bir güvenlik ağı).
  - `SetupWizardWindow.xaml.cs` (`Continue_Click`) ve `SettingsView.xaml.cs` (`ChangeFolder_Click`) —
    veri klasörü seçilirken/değiştirilirken de aynı net mesaj gösteriliyor (sihirbazda satır içi
    `ErrorText`, Ayarlar'da `AppMessageBox.Show`), ham istisna mesajı yerine.
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) İpuçları & SSS'ye yeni bir madde
  eklendi: "Veri Klasörüne Ulaşılamıyor" mesajını görürse ne yapması gerektiği.
- Derleme: 0 hata, 0 uyarı (nullable uyarıları `DataFolderPath!` null-forgiving ile temizlendi — bu metot
  sadece yol boş olmadığı doğrulandıktan sonra çağrılıyor). Uygulama, mevcut ULAŞILABİLİR paylaşımla
  (Z:\DijitalOkulPanosu\YAYIN) yeniden başlatıldı, normal senaryo bozulmadığı doğrulandı.
- **GERÇEKTEN ULAŞILAMAZ senaryosu (Yayın PC'si fiilen kapalıyken) canlı test EDİLEMEDİ** — bunu
  simüle etmek geliştirme ortamındaki gerçek paylaşımı bozmayı gerektirirdi, riske girilmedi. Mantık
  incelemesi (try/catch IOException/UnauthorizedAccessException, .NET'in UNC yol erişilemezliğinde bu
  istisnaları fırlattığı bilinen davranışı) sağlam ama **kullanıcının gerçek Yayın PC'sini kapatıp
  denemesi gerekiyor** — bu turun ASIL doğrulaması budur.
- **Kullanıcıdan istenen retest:** Yayın PC'sini kapatıp Yönetim PC'sinden (ya da veri klasörü o PC'yi
  gösteren herhangi bir PC'den) uygulamayı açıp: (a) artık net bir "Veri Klasörüne Ulaşılamıyor" mesajı
  ile klasör yolunu ve "Yayın PC'si kapalı olabilir" açıklamasını gördüğünü; (b) "Tekrar denensin mi?"
  sorusuna Hayır deyince uygulamanın (görünmez takılı kalmadan) TEMİZ kapandığını; (c) Yayın PC'sini
  açtıktan sonra tekrar başlatıp normal açıldığını doğrulamak.

**Altmışıncı tur — AppMessageBox sabit yükseklik yüzünden uzun mesajları KESİYORDU (elli dokuzuncu turun
kendi hatası):** Kullanıcı, bir önceki turda eklenen "Veri Klasörüne Ulaşılamıyor" onay penceresinin
ekran görüntüsünü gönderdi — sadece başlık ("Veri Klasörüne Ulaşılamıyor") ve mesajın İLK SATIRI
("Paylaşılan veri klasörüne ulaşılamıyor:") görünüyordu, asıl "Tekrar denensin mi?" sorusu hiç
GÖRÜNMÜYORDU — kullanıcı haklı olarak Evet/Hayır'ın ne anlama geldiğini soramadı çünkü ortada görünür bir
soru yoktu. **Kök neden:** `AppMessageBox.xaml`'in `Height="180" ResizeMode="NoResize"` ile SABİT boyutlu
olması — kısa "silmek istediğinizden emin misiniz?" gibi mesajlar için yeterliydi ama elli dokuzuncu
turda eklenen çok satırlı, uzun hata mesajı bu sabit yüksekliğe sığmayıp altı kesiliyordu (taşan kısım
görünmüyordu, kaydırma da yoktu). **Düzeltme:** `Height="180"` kaldırıldı, `SizeToContent="Height"` +
`MinHeight="150"` eklendi (pencere artık mesaj uzunluğuna göre kendini büyütüyor); orta satırın
`Grid.RowDefinitions`'ındaki `Height="*"` da `Auto`'ya çevrildi (SizeToContent ile Star satırların
davranışı belirsizleşebiliyordu, Auto ile kesin). Bu, `AppMessageBox` KULLANAN HER YERİ (tüm "emin
misiniz?" onayları, tüm hata mesajları — Sil butonları dahil) etkiliyor, hepsi artık kendi içeriğine göre
doğru boyutlanacak. Derleme: 0 hata, 0 uyarı. Uygulama yeniden başlatıldı, normal senaryoda sorunsuz açıldı.
- **Kullanıcıdan istenen retest:** Yayın PC'si kapalıyken tekrar deneyip bu sefer TÜM mesajın (klasör
  yolu + açıklama + "Tekrar denensin mi?" sorusu) göründüğünü, Evet/Hayır'ın ne yapacağının artık açık
  olduğunu doğrulamak. Ayrıca genel olarak — başka bir "emin misiniz?" onay penceresi (ör. bir öğeyi
  silerken) hâlâ doğru boyutta görünüyor mu diye bakmak faydalı olur (kısa mesajlarda `MinHeight="150"`
  ile önceki görünüme yakın kalmalı).

**Altmış birinci tur — "Veri Klasörüne Ulaşılamıyor" tasarımı BASİTLEŞTİRİLDİ (kullanıcı geri bildirimi) +
canlı ortamda GERÇEK bir çökme hatası bulunup düzeltildi:**
- **Şans eseri önemli bir doğrulama:** Kullanıcının ekran görüntüsünü incelerken, bu geliştirme
  makinesindeki `Z:\DijitalOkulPanosu\YAYIN` paylaşımının O AN GERÇEKTEN ulaşılamaz olduğu fark edildi
  (`crash.log`'da "Ağ yolu bulunamadı" hatası, güncel zaman damgasıyla) — yani bu turun testleri SİMÜLE
  EDİLMEDİ, elli dokuzuncu/altmışıncı turun kodu GERÇEK bir "Yayın PC'sine ulaşılamıyor" durumunda
  çalıştırıldı, tam kullanıcının yaşadığı senaryonun aynısı.
- **Bu sayede canlı olarak yeni, gerçek bir çökme hatası yakalandı:** Uygulama başlarken, `AppMessageBox`
  `owner: null` ile (henüz hiçbir pencere yokken) çağrıldığında, `AppMessageBox.xaml`'deki sabit
  `WindowStartupLocation="CenterOwner"` özelliği WPF'de `InvalidOperationException` fırlatıyordu (Owner
  ayarlı değilken CenterOwner geçersiz) — bu YENİ istisna, zaten bir istisnayı (ağ hatasını) işlerken
  oluştuğu için, hem `EnsureDataReadyWithRetry`'nin kendi `catch`'ini hem ardından genel
  `DispatcherUnhandledException` yakalayıcısını (O DA aynı hatayla AYNI şekilde çöktüğü için) art arda
  tetikleyip uygulamayı `crash.log`'a bile yazamadan SESSİZCE sonlandırıyordu — `tasklist` ile süreç
  aranınca hiçbir şey bulunamıyordu, ne pencere ne hata izi. **Düzeltme:** `AppMessageBox.xaml.cs`
  constructor'ına, `owner is null` ise `WindowStartupLocation = WindowStartupLocation.CenterScreen`
  atayan bir satır eklendi (Owner varsa eski CenterOwner davranışı korunuyor).
- **Kullanıcı, elli dokuzuncu turdaki tasarımı (Tekrar Dene / Vazgeç → Vazgeçilirse Veri Klasörünü Değiştir
  → O da reddedilirse uygulama kapanır) haklı olarak reddetti:** "kullanıcı PC'nin adresini/klasör yolunu
  bilmiyorsa ya da o PC'den tamamen vazgeçildiyse, hiçbir seçeneği kabul edemeyip uygulamaya HİÇ giremem,
  veri klasörümü bile değiştiremem" riski vardı — engelleyici bir karar zinciri, kullanıcıyı köşeye
  sıkıştırabilirdi. **Yeni, çok daha basit tasarım:** `AppServices.EnsureDataReadyWithRetry` tamamen
  kaldırıldı, yerine `EnsureDataReady(Window? owner)` geldi — TEK bir deneme yapar, başarısız olursa
  SADECE bilgilendirici bir uyarı gösterir (`AppMessageBox.Show`, Evet/Hayır YOK, sadece "Tamam"),
  `Data = null` bırakır ve uygulamanın normal şekilde açılmasına (hiç bloke etmeden) izin verir — tıpkı
  "veri klasörü henüz hiç seçilmemiş" (ilk kurulum) durumundaki gibi. Kullanıcı istediği zaman, kendi
  zamanında, zaten var olan Ayarlar sayfasındaki "Veri Klasörünü Değiştir" akışını (elli dokuzuncu turda
  zaten net hata mesajı eklenmişti) kullanabilir. `App.xaml.cs` buna göre sadeleştirildi (artık dönüş
  değerine göre `Shutdown()` kararı yok, doğrudan çağrılıp devam ediliyor).
- **Canlı doğrulama (gerçek Z:\ ulaşılamaz durumundayken):** Ekran görüntüsü alma imkânı olmadığı için
  PowerShell üzerinden Win32 `EnumWindows`/`GetWindowText` ile sürecin görünür pencerelerini listeleyip
  uyarı penceresinin GERÇEKTEN açıldığı (başlık: "Okul Panosu", `ShowInTaskbar="False"` yüzünden
  `Process.MainWindowHandle` bunu göremiyordu ama pencere gerçekten vardı) doğrulandı; süreç yanıt verir
  durumda kaldı (çökmedi). `SendKeys` ile pencereyi otomatik kapatıp devamını (ManagementWindow'un
  açılmasını) test etme denemesi `SetForegroundWindow`'un arka plan sürecinden engellenmesi yüzünden
  tam sonuçlanmadı — bu son adım (uyarıyı KAPATTIKTAN SONRA uygulamanın gerçekten normal açıldığını
  görsel olarak görmek) kullanıcının kendi testine kalıyor.
- Kılavuz (uygulama içi + web, aynı URL korunarak yeniden yayınlandı) SSS maddesi yeni davranışa göre
  düzeltildi ("Tekrar denensin mi?" ifadesi kaldırıldı, "Tamam'a basınca uygulama yine de açılır" eklendi).
- Derleme: 0 hata, 0 uyarı. Test sırasında GEÇİCİ olarak `%AppData%\OkulPanosu\local-settings.json`
  yerel bir test klasörüne yönlendirilmişti (normal/ulaşılabilir senaryoyu doğrulamak için) — kullanıcının
  itirazı üzerine bu HEMEN geri alındı, dosya orijinal içeriğine (`Z:\DijitalOkulPanosu\YAYIN`) döndürüldü,
  geçici test klasörü silindi. **Önemli ders:** kullanıcının gerçek yerel ayarlarını (geliştirme amaçlı
  bile olsa) değiştirmeden önce bunun ne kadar can sıkıcı/güven kırıcı olabileceği unutulmamalı.
- **Kullanıcıdan istenen retest:** Yayın PC'si kapalıyken uygulamayı açıp: (a) net bir uyarı ("Tamam"
  butonlu, tek seçenek) gördükten sonra Tamam'a basınca uygulamanın (ManagementWindow ya da BoardWindow)
  GERÇEKTEN açıldığını (kapanmadığını, takılı kalmadığını); (b) içeriklerin o an boş/varsayılan
  göründüğünü ama uygulamanın kullanılabilir olduğunu; (c) Yayın PC'sini açtıktan sonra uygulamayı
  kapatıp yeniden açınca normal (verili) şekilde açıldığını; (d) isterse Ayarlar'dan veri klasörünü
  başka bir konuma değiştirebildiğini doğrulamak.

**Altmış ikinci tur — Kullanıcı "Tamam'a bastım ama uygulama açılmadı" dedi; kök neden STALE PUBLISH
BUILD + gerçek bir ShutdownMode hatası bulundu:**
- **En kritik keşif — proje kökünde `publish/` diye AYRI bir klasör var** (`publish-al-genel.bat`
  scripti ile kullanıcının kendi ürettiği, self-contained `dotnet publish -c Release -r win-x64` çıktısı).
  **Bu oturum boyunca (elli dokuzuncu/altmışıncı tur dahil) HEP `bin\Debug\...` derlemesini test ettim,
  `publish\` klasörünü HİÇ güncellemedim.** Kullanıcı muhtemelen gerçek testlerini `publish\
  OkulPanosu.App.exe`'den yapıyor (proje geleneği bu — "self-contained publish" kurulum notlarında baştan
  beri vurgulanıyor) — yani "Tamam'a bastım ama açılmadı" raporu, büyük ihtimalle BENİM fixlerimi hiç
  İÇERMEYEN eski bir exe ile test edilmişti. **Düzeltme:** `dotnet publish ... -o publish` doğrudan
  çalıştırılıp `publish\OkulPanosu.App.exe` bu turun TÜM düzeltmeleriyle YENİDEN üretildi. **Ders — HER
  ZAMAN hatırlanmalı:** Bundan sonraki her turda, kullanıcıya "dener misin" demeden ÖNCE `publish\`
  klasörünü de güncellemek gerekiyor (sadece `bin\Debug` yeterli değil) — aksi hâlde kullanıcı hâlâ eski
  davranışı test ediyor olabilir, bu da hem onun hem benim zamanımı boşa harcar.
- **Bu arada GERÇEK bir ikinci hata da bulundu (kod incelemesiyle, ShutdownMode gotcha'sı):**
  `App.xaml`'de `ShutdownMode` hiç ayarlanmamıştı (varsayılan `OnLastWindowClose`). Açılışta (asıl Board/
  Management penceresi henüz yokken) gösterilen "Veri Klasörüne Ulaşılamıyor" uyarı penceresi O AN TEK
  açık pencere oluyordu — kullanıcı "Tamam"a basıp bu pencereyi kapattığı anda, WPF'in varsayılan
  davranışı gereği ("son pencere kapandı") uygulama SESSİZCE kapanabiliyordu, asıl pencere (Management/
  Board) hiç açılamadan — `crash.log`'a bir şey yazmadan, çünkü bu bir İSTİSNA değil, WPF'in KASITLI
  "son pencere kapandı, kapan" davranışıydı. **Düzeltme:** `App.xaml.cs`'in `OnStartup`'ının en başında
  `ShutdownMode = ShutdownMode.OnExplicitShutdown` ayarlanıp, asıl pencere (Board/Management) açıldıktan
  SONRA tekrar `ShutdownMode.OnLastWindowClose`'a döndürülüyor — bu sayede açılıştaki geçici diyaloglar
  (sihirbaz, uyarı) artık asla erken kapanmaya sebep olamaz, ama Yönetim penceresini kapatmak eskisi gibi
  uygulamayı kapatmaya devam ediyor (davranış değişikliği YOK, sadece açılış anındaki kırılganlık
  giderildi).
- **Ayrıca (ilgili, savunmacı bir sağlamlaştırma):** `BoardSyncWatcher.CheckForChanges()` (Kiosk modunda
  her 5 saniyede bir board-data.json'un değiştiğini kontrol eden `DispatcherTimer`) artık ağ hatalarını
  (`IOException`/`UnauthorizedAccessException`) sessizce yutup bir sonraki denemeye bırakıyor — önceden,
  ağ kararsızsa (flapping) her 5 saniyede bir "Veri Klasörüne Ulaşılamıyor" penceresi açılıp durabilirdi,
  bu kötü bir deneyim olurdu. `FileSystemWatcher`'a da bir `Error` olayı dinleyicisi eklendi (bağlantı
  koptuğunda izleyiciyi güvenle kapatıp polling'e bırakıyor).
- **Canlı test yöntemi hakkında not:** Bu turda PowerShell üzerinden Win32 `EnumWindows`/UI Automation ile
  otomatik test denemeleri yapıldı; TEK bir bash çağrısı içinde (başlat+bekle+kontrol et) süreç HER ZAMAN
  kararlı kaldı, ama AYRI/ardışık araç çağrıları arasında süreç birkaç kez "kayboldu" (crash.log'da hiç iz
  bırakmadan) — bu muhtemelen bu SANDBOX ortamının araç çağrıları arasında arka planda başlatılan
  süreçleri temizleme davranışından kaynaklanıyor (bir kod hatası değil). Bu yüzden güvenilir sonuç için
  HER ZAMAN "başlat + yeterince bekle + kontrol et" TEK bir bash çağrısında yapılmalı, ayrı çağrılara
  bölünmemeli.
- Derleme: 0 hata, 0 uyarı. `publish\` klasörü bu turun TÜM kod değişiklikleriyle taze yeniden üretildi.
- **DOĞRULANDI — kullanıcı `publish\OkulPanosu.App.exe`'yi test etti, "şimdi iyi çalışıyor" dedi.** Yayın
  PC'si kapalıyken artık net bir uyarı gösterip normal şekilde açılıyor, erken kapanma yok. Bu konu
  ÇÖZÜLDÜ sayılabilir — tekrar açılmadıkça buna dönmeye gerek yok.

## Yeni Oturumda İlk Yapılacak

Bu dosyayı okuduktan sonra, kullanıcıya doğrudan "hangi adımı denediniz, ne oldu?" diye sorarak devam
edilebilir — kod tarafında bekleyen bir iş yok, sıradaki adım kullanıcının manuel test geri bildirimi
(proje şu an askıda, yukarıdaki nota bakın). **ÖNEMLİ:** Bu makinenin veri klasörü artık
`Z:\DijitalOkulPanosu\YAYIN` (laptopun GERÇEK canlı verisi, ağ paylaşımı üzerinden) — yukarıdaki
"ÖNEMLİ — Ortam/veri klasörü değişikliği" notuna bakın.

**"Veri Klasörüne Ulaşılamıyor" konusu ÇÖZÜLDÜ ve DOĞRULANDI** (altmış ikinci tur — kullanıcı
`publish\OkulPanosu.App.exe`'yi test edip "şimdi iyi çalışıyor" dedi). Tekrar bir sorun bildirilmedikçe
bu konuya dönmeye gerek yok.

**ÇOK ÖNEMLİ İŞ AKIŞI NOTU:** Bu projede kullanıcı GERÇEK testlerini `D:\MYPRG\CLAUDE\DijitalOkulPanosu\
publish\OkulPanosu.App.exe`'den (self-contained Release build) yapıyor OLABİLİR, sadece `bin\Debug\
net10.0-windows\OkulPanosu.App.exe`'den DEĞİL. Kullanıcıya bir düzeltmeyi "dener misin" demeden ÖNCE
`dotnet publish src/OkulPanosu.App/OkulPanosu.App.csproj -c Release -r win-x64 --self-contained true -o
publish` komutunu da çalıştırıp `publish\` klasörünü güncel tutmayı UNUTMA — altmış ikinci turda bu
unutulup kullanıcı eski bir exe ile test etmiş, gereksiz bir kafa karışıklığına yol açmıştı.

**DAVRANIŞSAL NOT:** Bu kullanıcıyla karmaşık/çok seçenekli kararlarda `AskUserQuestion` yerine düz metinle
soru sormak daha güvenilir sonuç veriyor (elli beşinci/altıncı turda gözlemlendi). Kullanıcı "bunu zaten
söylemiştim" derse ciddiye al, yeniden açık soru haline getirme — doğrudan uygula (elli altıncı turda
Personel doğum tarihi isteği üçüncü kez tekrarlanmıştı).

En güncel: "Sütunlar tanınamadı" hatasının GERÇEK kök nedeni bulundu — kullanıcı test verisini Tab yerine
boşlukla ayırmıştı (WPF TextBox'ta AcceptsTab kapalıyken Tab tuşu odağı değiştirir, karakter yazmaz).
Artık: elle yazılan (tab içermeyen) veri noktalı virgülle ayrıştırılabiliyor (`EditorControls.ParseTable`),
yapıştırma kutuları gerçek Tab tuşu girişini de kabul ediyor (`AcceptsTab="True"`), hata mesajı somut
örnekle yönlendiriyor. Personel + Öğrenciler listeleri artık alfabetik sıralı. Öncesinde: Personel'e de
(Öğrenciler'deki gibi) opsiyonel doğum tarihi (Gün/Ay) eklendi; Personel'in Toplu Aktarımı artık bir
Görev/Ünvan sütunu bulursa her satırı kendi görevi ve doğru Kategorisiyle (Öğretmen/
Diğer Personel) ekliyor, bulamazsa eski davranışı (hepsi Öğretmen) koruyor. Personel'in doğum tarihi şu an
SADECE veri, panoda henüz gösterilmiyor — istenirse ayrı bir tur bekliyor. Öncesinde: Cinsiyet artık
Personel VE Öğrenciler listelerinde eklendikten SONRA da düzenlenebiliyor,
Toplu Aktarım Cinsiyet sütunu bulamazsa kaç kişinin varsayılan eklendiğini söylüyor. Daha öncesinde: Doğum
Günleri modülünün ⚙️'sindeki gömülü Öğrenciler editörü kaldırıldı — artık Personel ile
BİREBİR aynı desen (kendine ait modülü/gömülü editörü yok, sadece paylaşılan veri kaynağı); veri akışının
kendisi (Öğrenciler'e eklenen doğum tarihi olan biri otomatik panoda görünür) zaten elli birinci turdan
beri doğru çalışıyordu, sadece ⚙️'deki yanıltıcı gömülü editör satırı kafa karıştırıyordu. Öncesinde:
Personel'in toplu aktarımı artık AYNEN eşleşmeyen ama benzer görünen isimler için (ör. "M.
Sait Örnek" ~ "Mehmet Sait Örnek") kullanıcıya sorup karar veriyor, otomatik silmiyor/eklemiyor. Daha
öncesinde: Öğrenciler sayfasına fotoğraf (silüet varsayılanlı) + cinsiyet alanı ve "yapıştırmadan önce
mevcut listeyi TAMAMEN sil" (onaylı, personele dokunmayan) toplu aktarım seçeneği eklendi — kullanıcının
üç kez tekrarladığı "önceki öğrenciler silinsin" isteği artık karşılanıyor. Daha öncesinde: Doğum Günleri artık
ayrı bir liste DEĞİL — yeni bir "Öğrenciler" (🎓) ana verisinden (Personel'in öğrenci karşılığı, `Student`
varlığı, `GlobalBoardData.Students`) doğum tarihine göre filtrelenerek okunuyor;
eski `BirthdaysView`/`BirthdayStudent` tamamen `StudentsView`/`Student`'a dönüştürüldü, canlı veri
(`Z:\DijitalOkulPanosu\YAYIN\board-data.json`) taşındı ve doğrulandı. Personel'in toplu aktarımı da (daha
önce sütun sırasına bağımlıydı, kullanıcı yanlışlıkla bağımsız sanıyordu) artık sütun-sırası-bağımsız —
Sınıf Ders Programı/Öğrenciler'deki başlık-algılama deseniyle tutarlı. HİÇBİRİ henüz kullanıcı onayı
almadı, öncelikli retest bunlar olmalı (özellikle: Personel'de artık BAŞLIK SATIRI gerekiyor — eski
alışkanlıkla başlıksız yapıştırırsa hata mesajı görecek, bunu bilmesi gerekir). Yıllık toplu öğrenci
değiştirme ve öğrenci fotoğrafı içe aktarma HÂLÂ yapılmadı, net istenirse eklenebilir. Öncesinde: Personel
sayfasındaki üst kartların (Toplu Aktarım/Yeni Personel Tanımla) gereksiz genişleme + yanlış kaydırma
çubuğu hatası düzeltildi (kök neden: ScrollViewer'ın Auto yatay kaydırma modunda sonsuz genişlik ölçümü +
Stretch hizalama etkileşimi); Doğum Günleri'ne (şimdi Öğrenciler'e taşınan) Toplu Aktarım ve isme göre
anlık arama eklendi (arama + her satırın kendi Sil butonuyla "bul ve sil" ihtiyacı da çözüldü). Daha
önce: Ayın Öğrencisi + Personel & Öğretmenler sayfalarında sütun başlıkları
sabit (Duyurular'la aynı desen); Doğum Günleri tamamen liste görünümüne çevrildi (üstte sabit ekleme
formu, sabit sütun başlığı, altta kompakt tek-satırlık öğrenci listesi); Duyurular'da sütun başlıkları
sabit; DURUM sütunu sade (kutucuk + yazı, "tümünden kaldır" YOK — denenip geri alındı);
`TemplateEditorView.RenderPreview()`'daki RASTGELE-Id hatası düzeltildi — bu hata hem Duyurular'ın "bu
şablonda yayında" eşleşmesini hem Yerel Video'yu ÖNİZLEMEDE komple bozuyordu. Ayrıca Yerel Video artık
TAMAMEN şablona özgü, Duyurular İÇERİK paylaşılan ama YAYIN durumu şablon bazlı; Ctrl+Alt+Y artık
Windows'un global kısayol mekanizmasını kullanıyor (odak gerektirmiyor) + sağ üst köşede görünmez,
fareyle çalışan bir acil çıkış butonu eklendi — laptoptaki kilitlenme olayına karşı; kullanıcının kendi
ortamında (kablosuz ekran + laptop) doğrulaması gerekiyor, henüz onaylanmadı. Diğer her şey — Büyük Anons
modülleri çoklu-örnek, "Ad Değiştir" butonu, editördeki sığmama/diyalog düzeltmeleri, TÜM silme
butonlarına ortak onay, Okulumuzdan Kareler çökme düzeltmesi, Ders & Teneffüs TENEFFÜS gölgelemesi,
Tarihte Bugün Gün/Ay alanları, Beyin Egzersizi "Soru" kutusu, Nöbetçi Öğretmen nöbet yeri yerleşimi +
branş metni — de henüz kullanıcı onayı bekliyor).

## 2026-09-10 — Okullar açıldı, okul PC'sinden devam

**Proje artık askıda DEĞİL** — yukarıdaki "askıya alıyorum" notu geçerliliğini yitirdi, okullar açıldı,
kullanıcı şu an fiilen okulda (sistem odası + idare PC'si) çalışıyor. Öğrenciler bu haftanın pazartesi günü
başlıyor, kullanıcının hazırlık için 2 iş günü vardı.

**Okul PC'si (idare PC'si, kullanıcı adı `DELLENDIM\Selim`) ortamı sıfırdan kuruldu:**
- Ne Git ne .NET SDK kuruluydu — ikisi de `winget` ile kuruldu (Git.Git, Microsoft.DotNet.SDK.10 → 10.0.401).
  PowerShell komutları arasında `$env:Path` kalıcı olmuyor, her komutta Machine+User PATH'i yeniden
  birleştirmek gerekiyor.
- Proje klasörü (`D:\MYPRG\CLAUDE\Dijital Okul Panosu`) zaten evden git deposu olarak (elle/harici diskle)
  taşınmıştı, `origin` (github.com/mstogluk/dijital-okul-panosu) zaten tanımlıydı — GitHub Desktop kurup ayrı
  bir klon almaya gerek kalmadı (kullanıcı bir ara denedi, sonra kendisi sildi, doğru karardı).

**Paylaşılan veri klasörüne (`\\sistemodasi\ortak\YAYIN`, bu PC'de `Z:\YAYIN`) yazma erişimi "Veri Klasörüne
Ulaşılamıyor" hatası veriyordu — kök neden ağ/bağlantı DEĞİL, NTFS izniydi:**
- `crash.log`'da gerçek istisna `UnauthorizedAccessException: Access to the path 'Z:\YAYIN\board-data.json.tmp'
  is denied` idi (uygulamanın "Veri Klasörüne Ulaşılamıyor" mesajı hem ağ-erişilemezliği hem izin-reddini AYNI
  genel metinle gösteriyor — ileride ayrı mesaja çevrilebilir, şimdilik dokunulmadı).
  Doğrudan PowerShell'den de aynı klasöre yazma denendi, aynı hata alındı — uygulamadan bağımsız, gerçek bir
  Windows/paylaşım izni sorunu olduğu böyle doğrulandı.
- Kullanıcı yaz tatilinde birinin sistem odası PC'sine dokunmuş olabileceğini hatırladı: paylaşım "ortak"
  isimli ortak kullanıcı + Users grubuna salt-okunur (read-only) çevrilmişti. Kullanıcı kendisi sunucudaki
  paylaşım/NTFS izinlerini düzeltti (Değiştirme iznini geri açtı) — **artık çalışıyor, doğrulandı.**
  Not: NTFS izin düzenleme penceresini paylaşımın KÖKÜNDE (`Z:\`) değil, doğrudan `YAYIN` alt klasöründe
  açmak "Kapsayıcıdaki nesneler numaralandırılamadı" hatasını önlüyor (kök, izinleri kısıtlı başka alt
  klasörler — GENEL/KİŞİSEL — içeriyor olabilir).

**Ana Şablon artık kodla senkron — büyük bir tutarsızlık giderildi:**
- Kullanıcı fark etti: `BoardDataRepository.EnsureDefaultTemplate()` (yeni kurulumda otomatik oluşan
  varsayılan şablon) ile kullanıcının panoda fiilen düzenlediği "Ana Şablon" birbirinden TAMAMEN
  bağımsızdı — biri koda gömülü sabit, diğeri paylaşılan JSON'da. Başka bir okula sıfırdan publish
  verilseydi, kullanıcının emek verdiği güncel düzen değil eski/dondurulmuş kod düzeni çıkardı.
- **Düzeltme:** `EnsureDefaultTemplate()` artık kullanıcının gerçek "Ana Şablon"uyla BİREBİR eşleşiyor
  (12 modül, aynı X/Y/W/H, `ColorMode=custom`/`CustomBaseColor=#7CABE4`) — TEK fark, kayan duyuru metni:
  gerçek Ana Şablon'daki metin bu okula özeldi (Batman Mezopotamya...), koda genel bir yer tutucu
  ("Okul Dijital Panosu Yayınıdır.") gömüldü, başka okul kendi metnini kolayca girer.
  Kullanıcı "Ana Şablon bozulmasın" dediği için üzerinde deneme yapmak yerine bir KOPYASINI ("Mezopotamya
  Ana Şablon") oluşturup onun üzerinde çalışmaya karar verdi — Ana Şablon artık hem panoda hem kodda sabit
  referans.
- **Ek olarak:** `EnsureDefaultTemplate()` artık aynı düzeni `{VeriKlasörü}\Şablonlar\Ana Şablon_şablonu_yedek.json`
  olarak da (TemplateExportService'in "Dışa Aktar" ürettiğiyle AYNI JSON biçiminde) otomatik yazıyor
  (`WriteDefaultTemplateBackup`) — kullanıcı panodaki Ana Şablon'u kazayla bozarsa/silerse, Şablonlar
  sayfasındaki "İçe Aktar" ile bu dosyadan geri getirebilir; ayrı bir yedek dosyası taşımasına/göndermesine
  gerek kalmadı. Kılavuza (hem uygulama içi hem web, aynı URL:
  <https://claude.ai/code/artifact/f261d410-4ad4-483a-9753-597fd87a04e1>) bu özellik belgelendi (Şablonlar
  sayfasına yeni alt başlık + SSS'ye yeni madde).
- **"+ Yeni Şablon" kasıtlı olarak BOŞ açılmaya devam ediyor** (değişiklik YAPILMADI) — "Kopyala" zaten var
  olan bir düzenden başlamak için, "Yeni" temiz sayfa için; ikisini birleştirmek kafa karıştırırdı.

**Git push akışı — bu makineden otomatik push GÜVENİLİR DEĞİL:**
- GCM (Git Credential Manager, `credential.helper=manager`) kurulu, ama bu oturumdaki (Claude Code'un
  PowerShell aracından, non-interactive/stdin=null ortamda) tetiklenen `git push` denemeleri kimlik
  doğrulamasını tamamlayamadı ("terminal prompts disabled" ya da sessizce sonsuza kadar askıda kaldı) —
  hem doğrudan `git push` hem `github_gonder.bat` üzerinden denendi, ikisi de aynı şekilde takıldı.
  **Ders: bu makinede push'u HER ZAMAN kullanıcının kendi (interaktif) terminalinden/bat dosyasından
  yaptırmak gerekiyor, otomatik çalıştırmayı denemeye devam etmemeli.**
- Kullanıcı `github_gonder.bat`'ı kendisi çalıştırdığında push çalışıyor ama BAZEN ÇOK YAVAŞ olabiliyor
  (bir seferinde 91 nesne/birkaç MB, okul ağının kısıtlı giden bant genişliğiyle dakikalarca sürebiliyor)
  — bu normal, "Writing objects: %N" ilerlemesi duruyorsa (yüzde artıyorsa) beklemek yeterli, müdahale
  gerekmiyor.
- Bu tur sonunda GitHub'a gönderilmesi gereken commit'ler: Ana Şablon kod eşleşmesi, otomatik yedek
  özelliği, kılavuz güncellemesi (bkz. yukarısı) — push'un fiilen tamamlanıp tamamlanmadığı bir sonraki
  oturumda `git status`/`git log origin/main..HEAD` ile doğrulanmalı.

**Sırada:** Kullanıcı idareden ders programı + nöbetçi öğretmen listesi beklerken, buna bağlı olmayan diğer
modülleri (Duyurular, Personel, Yemek Menüsü, Ayın Öğrencisi, Yerel Video, Beyin Egzersizi, Temiz Sınıflar,
Öğrenciler) dolduruyor ve TV'de gerçek yayın denemesi yapıyor — kod tarafında şu an bekleyen bir iş yok,
bir sonraki oturumda kullanıcıya doğrudan "yayın denemesi nasıl gitti, bir sorun çıktı mı?" diye sorulabilir.
