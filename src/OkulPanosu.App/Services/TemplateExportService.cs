using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Services;

/// <summary>Şartnamedeki "Konum Seçmeli Dışa Aktarma" akışı: SaveFileDialog + JSON, varsayılan ad "{Şablon}_şablonu_yedek.json".</summary>
public static class TemplateExportService
{
    private sealed record ExportDto(DateTimeOffset ExportedAt, Template Template);

    public static void ExportTemplate(Template template, BoardDataRepository repo, System.Windows.Window? owner)
    {
        var safeName = string.Join("_", template.Name.Split(Path.GetInvalidFileNameChars()));
        var defaultDir = repo.TemplatesFolderPath;
        Directory.CreateDirectory(defaultDir);

        var dialog = new SaveFileDialog
        {
            Filter = "JSON Dosyası (*.json)|*.json",
            FileName = $"{safeName}_şablonu_yedek.json",
            Title = "Şablonu Kaydedeceğiniz Konumu Seçin",
            InitialDirectory = defaultDir,
        };

        if (dialog.ShowDialog(owner) != true) return;

        var dto = new ExportDto(DateTimeOffset.Now, template);
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>İçe aktarılan şablon her zaman yeni bir Id ile eklenir — mevcut şablonların üzerine yazılmaz.</summary>
    public static Template? ImportTemplate(BoardDataRepository repo, System.Windows.Window? owner)
    {
        var dialog = new OpenFileDialog { Filter = "JSON Dosyası (*.json)|*.json", Title = "Şablon Yedeğini Seçin" };
        if (dialog.ShowDialog(owner) != true) return null;

        using var doc = JsonDocument.Parse(File.ReadAllText(dialog.FileName));
        if (!doc.RootElement.TryGetProperty("Template", out var templateEl)) return null;

        var imported = templateEl.Deserialize<Template>();
        if (imported is null) return null;

        var data = repo.Load();
        imported.Id = Guid.NewGuid().ToString("N");
        imported.IsActive = false;
        imported.Name = data.Templates.Any(t => t.Name == imported.Name) ? imported.Name + " (içe aktarılan)" : imported.Name;
        data.Templates.Add(imported);
        repo.Save(data);

        return imported;
    }
}
