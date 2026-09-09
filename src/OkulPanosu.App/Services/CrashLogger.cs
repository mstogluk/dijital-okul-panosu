using System.IO;

namespace OkulPanosu.App.Services;

/// <summary>Beklenmeyen bir hata olduğunda (App.xaml.cs'teki DispatcherUnhandledException) uygulamanın
/// sessizce kapanması yerine hatayı bir dosyaya yazar ve kullanıcıya bir bilgi penceresi gösterir.</summary>
public static class CrashLogger
{
    private static string LogPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OkulPanosu", "crash.log");

    public static void Log(Exception ex)
    {
        try
        {
            var path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n");
        }
        catch
        {
            // Loglama başarısız olsa bile kullanıcıya gösterilen mesaj kutusu zaten devrede.
        }
    }
}
