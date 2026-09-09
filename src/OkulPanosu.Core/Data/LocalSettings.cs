namespace OkulPanosu.Core.Data;

public static class StartupModes
{
    public const string Kiosk = "Kiosk";
    public const string Management = "Management";
}

/// <summary>
/// Bu makineye özel ayarlar — paylaşılan veri klasöründe DEĞİL, her PC'nin kendi
/// %AppData%'ında tutulur (hangi PC hangi modda açılsın, hangi ekranda tam ekran olsun gibi
/// bilgiler makineden makineye farklı olmalı).
/// </summary>
public sealed class LocalSettings
{
    public string? DataFolderPath { get; set; }

    public string StartupMode { get; set; } = StartupModes.Kiosk;

    public string? KioskMonitorDeviceName { get; set; }

    public bool AutoStartWithWindows { get; set; }

    public string? ThemeKey { get; set; }

    /// <summary>Yönetim penceresinin son kapatıldığındaki boyutu — tekrar açıldığında aynı boyutta gelsin diye.</summary>
    public double? ManagementWindowWidth { get; set; }

    public double? ManagementWindowHeight { get; set; }

    public bool ManagementWindowMaximized { get; set; }
}
