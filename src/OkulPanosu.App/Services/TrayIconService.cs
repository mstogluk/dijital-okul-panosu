using System.IO;

namespace OkulPanosu.App.Services;

/// <summary>
/// Uygulama her modda (Kiosk veya Yönetim) çalışırken görev çubuğunda bir simge tutar. Amacı: TV/kiosk
/// ekranına fiziksel olarak erişilemediğinde (uzak masaüstü/RDP ile bağlanıldığında Ctrl+Alt+Y bazen
/// uzak oturuma değil yerel makineye gidebiliyor, ya da ekran gerçekten erişilemez bir yerdeyse) yine de
/// çift tıkla Yönetim'e geçilebilsin. Kiosk PC'sinin klavyesine hiç dokunmadan da çalışır.
/// </summary>
public static class TrayIconService
{
    private static System.Windows.Forms.NotifyIcon? _icon;

    public static void Initialize(string tooltip, Action onOpenRequested, Action onExitRequested)
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Yönetime Geç / Göster", null, (_, _) => onOpenRequested());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Çıkış", null, (_, _) => onExitRequested());

        _icon = new System.Windows.Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = tooltip,
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => onOpenRequested();
    }

    /// <summary>Tepsi ikonunu yükler. Not: `Icon.ExtractAssociatedIcon(exePath)` denenmişti ama sadece TEK
    /// (genelde 32x32) boyut döndürüyor — Windows bunu tepsinin gerçek boyutuna (16x16) küçültünce ince
    /// detaylar (modül renk blokları) bulanıklaşıp anlamsız bir mavi lekeye dönüşüyordu. Bunun yerine
    /// Assets\AppIcon.ico (çıktı klasörüne kopyalanıyor, bkz. csproj) doğrudan, sistemin gerçek küçük ikon
    /// boyutu istenerek açılıyor — çok boyutlu ICO içinden TAM O boyut için özel çizilmiş kare seçiliyor,
    /// bulanıklık olmuyor.</summary>
    private static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                var size = System.Windows.Forms.SystemInformation.SmallIconSize;
                return new System.Drawing.Icon(iconPath, size);
            }
        }
        catch
        {
            // Beklenmeyen bir sorun olursa (ör. dosya bulunamadı/bozuk) aşağıdaki yedeklere düş.
        }

        try
        {
            var path = Environment.ProcessPath;
            if (path is not null)
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon is not null) return icon;
            }
        }
        catch
        {
            // Yedek de başarısız olursa sistem ikonuna düş.
        }

        return System.Drawing.SystemIcons.Application;
    }

    public static void Dispose()
    {
        if (_icon is null) return;
        _icon.Visible = false;
        _icon.Dispose();
        _icon = null;
    }
}
