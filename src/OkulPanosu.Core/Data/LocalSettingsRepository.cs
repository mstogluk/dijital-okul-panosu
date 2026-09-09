using System.Text.Json;

namespace OkulPanosu.Core.Data;

public sealed class LocalSettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public LocalSettingsRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OkulPanosu", "local-settings.json");
    }

    public bool Exists => File.Exists(_filePath);

    public LocalSettings Load()
    {
        if (!File.Exists(_filePath)) return new LocalSettings();
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<LocalSettings>(json) ?? new LocalSettings();
    }

    public void Save(LocalSettings settings)
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
