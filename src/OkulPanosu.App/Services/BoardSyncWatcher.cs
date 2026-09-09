using System.IO;
using System.Windows;
using System.Windows.Threading;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Services;

/// <summary>
/// Paylaşılan veri klasöründeki board-data.json değişikliklerini izler. UNC ağ paylaşımlarında
/// FileSystemWatcher güvenilir olmayabildiğinden, 5 saniyede bir son yazma zamanını karşılaştıran
/// bir polling fallback da paralel çalışır — ikisinden hangisi önce yakalarsa o tetikler.
/// </summary>
public sealed class BoardSyncWatcher : IDisposable
{
    private readonly BoardDataRepository _repo;
    private readonly Action _onChanged;
    private readonly DispatcherTimer _pollTimer;
    private DispatcherTimer? _debounceTimer;
    private FileSystemWatcher? _watcher;
    private DateTime? _lastKnownWriteUtc;

    public BoardSyncWatcher(BoardDataRepository repo, Action onChanged)
    {
        _repo = repo;
        _onChanged = onChanged;
        _lastKnownWriteUtc = repo.GetLastWriteTimeUtc();

        TrySetupFileSystemWatcher();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _pollTimer.Tick += (_, _) => CheckForChanges();
        _pollTimer.Start();
    }

    private void TrySetupFileSystemWatcher()
    {
        try
        {
            Directory.CreateDirectory(_repo.DataFolderPath);
            _watcher = new FileSystemWatcher(_repo.DataFolderPath, "board-data.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            };
            _watcher.Changed += (_, _) => ScheduleDebouncedCheck();
            _watcher.Created += (_, _) => ScheduleDebouncedCheck();
            // FileSystemWatcher, izlediği ağ paylaşımlı klasörle bağlantı koptuğunda kendi iç thread'inde
            // bir Error olayı doğurabiliyor — bu olay dinlenmezse (özellikle UNC yollarda) süreci
            // etkileyebilecek belirsiz bir durum bırakabiliyor. Burada sadece izleyiciyi güvenle
            // devre dışı bırakıyoruz; 5 saniyelik polling zaten paralel çalıştığı için senkronizasyon
            // kesintiye uğramıyor, bağlantı geri geldiğinde polling üzerinden değişiklik yine yakalanır.
            _watcher.Error += (_, _) =>
            {
                try { _watcher!.EnableRaisingEvents = false; } catch { /* zaten kopmuş olabilir */ }
            };
            _watcher.EnableRaisingEvents = true;
        }
        catch
        {
            // Klasör henüz erişilemez olabilir (ör. ağ paylaşımı geçici kopuk) — polling zaten devrede.
        }
    }

    private void ScheduleDebouncedCheck()
    {
        // FileSystemWatcher olayları arka plan thread'inden gelir, DispatcherTimer UI thread'i ister.
        Application.Current.Dispatcher.Invoke(() =>
        {
            _debounceTimer?.Stop();
            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer!.Stop();
                CheckForChanges();
            };
            _debounceTimer.Start();
        });
    }

    /// <summary>Her 5 saniyede bir (ya da FileSystemWatcher bir değişiklik bildirdiğinde) çalışır.
    /// Ağ paylaşımı bu esnada geçici olarak kopuk olabilir — File.Exists bunu sessizce yutuyor ama
    /// GetLastWriteTimeUtc'nin kendisi (ör. bağlantı TAM O anda koparsa) yine de IOException
    /// fırlatabiliyordu; bu, DispatcherTimer üzerinden geldiği için genel hata penceresine düşüyor ve
    /// ağ kararsızsa her 5 saniyede bir "Veri Klasörüne Ulaşılamıyor" penceresi açılıp duruyordu — kötü
    /// bir deneyim. Artık böyle geçici hatalar sessizce yutulup bir sonraki denemeye bırakılıyor.</summary>
    private void CheckForChanges()
    {
        DateTime? writeTime;
        try
        {
            writeTime = _repo.GetLastWriteTimeUtc();
        }
        catch (Exception ex) when (AppServices.IsFolderUnreachableError(ex))
        {
            return;
        }

        if (writeTime is null || (_lastKnownWriteUtc is not null && writeTime <= _lastKnownWriteUtc)) return;

        _lastKnownWriteUtc = writeTime;
        _onChanged();
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _pollTimer.Stop();
        _debounceTimer?.Stop();
    }
}
