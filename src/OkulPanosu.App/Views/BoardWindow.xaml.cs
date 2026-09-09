using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using OkulPanosu.App.Services;

namespace OkulPanosu.App.Views;

/// <summary>Yayın (Kiosk) penceresi — TV/panoda tam ekran çalışır, aktif şablonu render eder.</summary>
public partial class BoardWindow : Window
{
    // Ctrl+Alt+Y'yi Windows'un GLOBAL kısayol mekanizmasıyla (RegisterHotKey) yakalıyoruz — WPF'in kendi
    // PreviewKeyDown'ı SADECE pencere klavye odağındaysa çalışır, ve kiosk penceresi görsel olarak tam
    // ekran/topmost olsa bile OS seviyesinde odağı gerçekten ALMAMIŞ olabilir (ör. açılışta başka bir
    // pencere/kablosuz ekran bağlantısı hâlâ odaktaysa). Kullanıcı canlı ortamda TAM OLARAK bunu yaşadı:
    // "bir kez tıklayıp Ctrl+Alt+Y'ye bastığımda tepki yoktu, ikinci kez tıklayınca çalıştı" — klasik
    // Windows "ilk tık sadece etkinleştirir" davranışı. RegisterHotKey, pencere odaklı olsun olmasın,
    // sistem genelinde HER ZAMAN çalışır. Eski PreviewKeyDown de (aşağıda) yedek olarak duruyor —
    // zarar vermiyor, sadece global kısayol her nedenle kayıt olamazsa (ör. başka bir uygulama aynı
    // kombinasyonu almışsa) bir ikinci şans sağlıyor.
    private const int ExitHotkeyId = 0x4F4B59;
    private const uint ModControl = 0x0002;
    private const uint ModAlt = 0x0001;
    private const uint VkY = 0x59;
    private const int WmHotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;
    private bool _hotkeyRegistered;

    private BoardSyncWatcher? _syncWatcher;
    private bool _isKiosk;
    private ManagementWindow? _managementWindow;
    private bool _managementWindowIsOwnedByUs;

    public BoardWindow()
    {
        InitializeComponent();

        SourceInitialized += (_, _) =>
        {
            _hwndSource = (HwndSource)PresentationSource.FromVisual(this)!;
            _hwndSource.AddHook(WndProc);
            RegisterExitHotkey();
        };

        PositionOnConfiguredMonitor();
        RenderActiveTemplate();
        EnterKiosk();

        if (AppServices.Data is { } repo)
            _syncWatcher = new BoardSyncWatcher(repo, RenderActiveTemplate);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == ExitHotkeyId)
        {
            if (_isKiosk) ExitKiosk();
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>Sadece kiosk AKTİFKEN kayıtlı tutuluyor — Yönetim penceresi açıkken zaten kendi
    /// PreviewKeyDown'ı (odaklıyken) Ctrl+Alt+Y'yi yakalıyor; aynı kombinasyonu aynı anda İKİ pencere
    /// global olarak almaya çalışırsa ikincisi başarısız olur, bu yüzden sadece gerekliyken kayıtlı.</summary>
    private void RegisterExitHotkey()
    {
        if (_hotkeyRegistered || _hwndSource is null) return;
        _hotkeyRegistered = RegisterHotKey(_hwndSource.Handle, ExitHotkeyId, ModControl | ModAlt, VkY);
    }

    private void UnregisterExitHotkey()
    {
        if (!_hotkeyRegistered || _hwndSource is null) return;
        UnregisterHotKey(_hwndSource.Handle, ExitHotkeyId);
        _hotkeyRegistered = false;
    }

    private void PositionOnConfiguredMonitor()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        var target = screens.FirstOrDefault(s => s.DeviceName == AppServices.LocalSettings.KioskMonitorDeviceName)
                     ?? System.Windows.Forms.Screen.PrimaryScreen
                     ?? screens[0];

        Left = target.Bounds.Left;
        Top = target.Bounds.Top;
        Width = target.Bounds.Width;
        Height = target.Bounds.Height;
    }

    public void RenderActiveTemplate()
    {
        var template = AppServices.Data?.GetActiveTemplate();
        BoardGrid.Render(template);
    }

    public bool IsBroadcasting => _isKiosk && IsVisible;

    /// <summary>Yayın hangi yoldan durdurulursa durdurulsun (TV'de Ctrl+Alt+Y, Yönetim'den durdur,
    /// pencere kapanması) tetiklenir — Yönetim penceresi "Yayını Başlat" butonunun metnini buna göre günceller.</summary>
    public event Action? BroadcastStopped;

    /// <summary>Yönetim penceresindeki "Yayını Durdur" butonu veya Ctrl+Alt+Y (Yönetim penceresi
    /// odaktayken) tarafından çağrılır. Yayını fiilen durduran, geri alınması TV başında fiziksel
    /// müdahale gerektiren bir eylem olduğu için burada şifre isteniyor — dialog, kiosk ekranındaki
    /// (görülemeyen) BoardWindow yerine <paramref name="owner"/> (Yönetim penceresi) üzerinde açılır.</summary>
    public bool StopBroadcastFromManagement(Window owner)
    {
        if (!_isKiosk) return true;
        if (!AdminAuthService.EnsureUnlocked(owner)) return false;

        _isKiosk = false;
        Hide();
        UnregisterExitHotkey();
        BroadcastStopped?.Invoke();
        return true;
    }

    private void EnterKiosk()
    {
        _isKiosk = true;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        PositionOnConfiguredMonitor();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
        Keyboard.Focus(this);
        RegisterExitHotkey();
    }

    /// <summary>Yönetim penceresinden "Yayını Başlat" ile açıldığında çağrılır — Ctrl+Alt+Y ile çıkışta
    /// yeni bir Yönetim penceresi açmak yerine bu var olanı yeniden kullanır (iki pencere birden
    /// açılmasını önler). Bu pencere kullanıcı tarafından zaten açılmış olduğundan, kapatılması
    /// "yayına otomatik dön" davranışını TETİKLEMEZ — kapatma normal pencere kapatma gibi davranır.</summary>
    public void AttachManagementWindow(ManagementWindow window)
    {
        _managementWindow = window;
        _managementWindowIsOwnedByUs = false;
    }

    /// <summary>Yayın PC'sindeki tek örneği tekrar tam ekrana getirir — pencere gizliyse önce gösterir.
    /// ManagementWindow'daki "Yayını Başlat" butonu bu örneği yeniden kullanmak için bunu çağırır.</summary>
    public void ShowKiosk()
    {
        if (!IsVisible) Show();
        EnterKiosk();
    }

    /// <summary>Sistem tepsisi ikonundan veya Ctrl+Alt+Y'den çağrılır — kiosk PC'sine fiziksel erişim
    /// olmadan (fare/klavyeye uzaktan erişim, ör. RDP ile) da Yönetim'e geçilebilsin diye. Yönetim'i
    /// AÇMANIN kendisi artık şifre istemiyor (kullanıcı isteği: "yönetim penceresi açılmak istendiğinde
    /// şifre istemesin, direkt açılsın") — şifre koruması, gerçek değişiklik yapan eylemlerde
    /// (yayını durdurma, modül silme) devreye giriyor, açmak serbest.</summary>
    public void RequestManagementAccess() => ExitKiosk();

    /// <summary>Sistem tepsisi ikonuna çift tıklanınca çağrılır — yayını durdurmadan (TV'de göstermeye
    /// devam ederken) Yönetim penceresini açar/öne getirir. Tek PC'de aynı anda hem yayın hem yönetimle
    /// çalışmak isteyen kullanıcı için: "Yayını Başlat" durumu değişmez, sadece Yönetim erişilebilir olur.</summary>
    public void OpenManagementKeepingBroadcast() => OpenManagement();

    private void ExitKiosk()
    {
        if (!_isKiosk) return;

        _isKiosk = false;
        Hide();
        UnregisterExitHotkey();
        BroadcastStopped?.Invoke();
        OpenManagement();
    }

    /// <summary>Klavye kısayolunun (Ctrl+Alt+Y — bkz. yukarıdaki global RegisterHotKey açıklaması) yanı
    /// sıra, tamamen odak/klavyeden bağımsız çalışan bir FARE yedeği — kullanıcının isteği: "yayın
    /// ekranının bir köşesinde şeffaf bir buton olsun, fare gidince belirginleşsin, tıklayınca yayın
    /// kesinlikle dursun". Sabit bir köşeye (sağ üst) hizalı olduğu için çözünürlük hesabı gerekmiyor.</summary>
    private void ExitCorner_MouseEnter(object sender, MouseEventArgs e) => ExitCornerIcon.Opacity = 1;

    private void ExitCorner_MouseLeave(object sender, MouseEventArgs e) => ExitCornerIcon.Opacity = 0;

    private void ExitCorner_Click(object sender, MouseButtonEventArgs e)
    {
        if (_isKiosk) ExitKiosk();
    }

    private void OpenManagement()
    {
        if (_managementWindow is null)
        {
            _managementWindow = new ManagementWindow(this);
            _managementWindowIsOwnedByUs = true;
            _managementWindow.Closed += (_, _) =>
            {
                var wasOwnedByUs = _managementWindowIsOwnedByUs;
                _managementWindow = null;
                // Sadece BİZİM açtığımız (kiosk'tan Ctrl+Alt+Y ile çıkışta oluşturulan) pencere
                // kapatılınca otomatik yayına dönülür. Kullanıcının "Yayını Başlat" ile önceden
                // açtığı asıl Yönetim penceresi kapatılırsa bu normal uygulama kapanışıdır.
                if (wasOwnedByUs && !IsVisible) ShowKiosk();
            };
        }

        _managementWindow.Show();
        _managementWindow.Activate();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Y || Keyboard.Modifiers != (ModifierKeys.Control | ModifierKeys.Alt)) return;

        if (_isKiosk) ExitKiosk();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _syncWatcher?.Dispose();
        UnregisterExitHotkey();
        _hwndSource?.RemoveHook(WndProc);
    }
}
