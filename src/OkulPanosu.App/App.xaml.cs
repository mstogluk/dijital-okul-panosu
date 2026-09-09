using System.Windows;
using OkulPanosu.App.Services;
using OkulPanosu.App.Views;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Açılışta (asıl Board/Management penceresi henüz açılmadan önce) gösterilebilecek geçici
        // diyaloglar (kurulum sihirbazı, veri klasörü uyarısı) o an TEK açık pencere oluyor — varsayılan
        // ShutdownMode="OnLastWindowClose" ile bu diyalog KAPANDIĞI an WPF "son pencere kapandı" sanıp
        // uygulamayı erkenden kapatabiliyordu, asıl pencere HİÇ açılamadan (kullanıcı "Tamam"a bastı ama
        // uygulama açılmadı şikâyeti buradan geliyor olabilir). Asıl pencere açılana kadar kapanma SADECE
        // elle (Shutdown()) tetiklenir; asıl pencere açıldıktan sonra normal davranışa dönülüyor (ör.
        // Yönetim penceresini kapatmak uygulamayı kapatmaya devam etsin diye).
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, args) =>
        {
            CrashLogger.Log(args.Exception);
            if (AppServices.IsFolderUnreachableError(args.Exception))
            {
                AppMessageBox.Show(null, AppServices.DescribeFolderUnreachable(AppServices.LocalSettings?.DataFolderPath ?? "?"),
                    "Veri Klasörüne Ulaşılamıyor");
            }
            else
            {
                AppMessageBox.Show(null,
                    $"Beklenmeyen bir hata oluştu ve işlem durduruldu:\n\n{args.Exception.Message}\n\nAyrıntılar %AppData%\\OkulPanosu\\crash.log dosyasına yazıldı.",
                    "Okul Panosu — Hata");
            }
            args.Handled = true;
        };

        AppServices.LoadLocalSettings();
        ThemeManager.Apply(AppServices.LocalSettings.ThemeKey);

        if (!AppServices.LocalSettingsRepo.Exists || string.IsNullOrEmpty(AppServices.LocalSettings.DataFolderPath))
        {
            var wizard = new SetupWizardWindow();
            if (wizard.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }
        else
        {
            // Veri klasörüne (ağ paylaşımlı olabilir) ulaşılamazsa kullanıcı ASLA bloke edilmez — sadece
            // uyarılır, uygulama normal şekilde açılmaya devam eder (bkz. EnsureDataReady'nin açıklaması).
            AppServices.EnsureDataReady(null);
        }

        if (AppServices.LocalSettings.StartupMode == StartupModes.Kiosk)
        {
            var board = new BoardWindow();
            board.Show();
            TrayIconService.Initialize("Okul Panosu — Yayında", board.OpenManagementKeepingBroadcast, Shutdown);
        }
        else
        {
            var management = new ManagementWindow();
            management.Show();
            TrayIconService.Initialize("Okul Panosu — Yönetim", () =>
            {
                management.Show();
                management.WindowState = WindowState.Normal;
                management.Activate();
            }, Shutdown);
        }

        // Asıl pencere artık açık — eskisi gibi, o pencere kapatılınca uygulama da kapansın.
        ShutdownMode = ShutdownMode.OnLastWindowClose;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TrayIconService.Dispose();
        base.OnExit(e);
    }
}
