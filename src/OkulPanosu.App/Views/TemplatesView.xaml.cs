using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OkulPanosu.App.Services;

namespace OkulPanosu.App.Views;

public partial class TemplatesView : UserControl, IReloadablePage
{
    public event Action<string>? EditModulesRequested;

    private readonly ObservableCollection<TemplateRow> _rows = new();

    public TemplatesView()
    {
        InitializeComponent();
        TemplateList.ItemsSource = _rows;
        Reload();
    }

    public void Reload()
    {
        _rows.Clear();
        if (AppServices.Data is not { } repo) return;

        var data = repo.Load();
        foreach (var template in data.Templates)
        {
            _rows.Add(new TemplateRow
            {
                Id = template.Id,
                Name = template.Name,
                IsActive = template.Id == data.ActiveTemplateId,
                ModuleCountText = $"{template.Modules.Count} modül · {template.GridColumns}x{template.GridRows} ızgara",
            });
        }
    }

    private void NewTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;

        var template = repo.AddTemplate($"Yeni Şablon {DateTime.Now:HH:mm}");
        Reload();

        // Yeni şablon otomatik oluşturulan bir isimle geliyor — kullanıcı "isim veremiyoruz" diye
        // şikayet etti, isim düzenleme kutusunun (✏️) varlığını fark etmemiş olabilir. Oluşturur
        // oluşturmaz doğrudan isim düzenleme moduna geçerek kendi adını yazmasını kolaylaştırıyoruz.
        if (_rows.FirstOrDefault(r => r.Id == template.Id) is { } row)
            row.IsEditingName = true;
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;
        if (RowFromSender(sender) is not { } row) return;

        repo.SetActiveTemplate(row.Id);
        Reload();
    }

    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;
        if (RowFromSender(sender) is not { } row) return;

        repo.DuplicateTemplate(row.Id);
        Reload();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;
        if (RowFromSender(sender) is not { } row) return;

        if (!AppMessageBox.Confirm(Window.GetWindow(this), $"\"{row.Name}\" şablonu silinsin mi?", "Şablonu Sil")) return;

        repo.DeleteTemplate(row.Id);
        Reload();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;
        if (RowFromSender(sender) is not { } row) return;

        var template = repo.Load().Templates.FirstOrDefault(t => t.Id == row.Id);
        if (template is null) return;

        TemplateExportService.ExportTemplate(template, repo, Window.GetWindow(this));
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;

        TemplateExportService.ImportTemplate(repo, Window.GetWindow(this));
        Reload();
    }

    private void EditModules_Click(object sender, RoutedEventArgs e)
    {
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;
        if (RowFromSender(sender) is not { } row) return;

        EditModulesRequested?.Invoke(row.Id);
    }

    private void EditName_Click(object sender, RoutedEventArgs e)
    {
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;
        if (RowFromSender(sender) is not { } row) return;

        row.IsEditingName = true;
    }

    private void RenameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        CommitRename(sender);
    }

    private void RenameBox_LostFocus(object sender, RoutedEventArgs e) => CommitRename(sender);

    private void CommitRename(object sender)
    {
        if (AppServices.Data is not { } repo) return;
        // Kutunun bağlı olduğu row.Name'i DEĞİL, kutunun KENDİ Text'ini okuyoruz — Text="{Binding Name,
        // Mode=OneWay}" olduğu için row.Name kullanıcının yazdığıyla ASLA güncellenmiyordu, bu yüzden
        // Enter'a basınca eski ada geri dönüyor gibi görünüyordu (aslında hiç değişmemişti).
        if (sender is not TextBox { DataContext: TemplateRow row } textBox) return;
        if (!row.IsEditingName) return;

        var newName = textBox.Text.Trim();
        if (!string.IsNullOrEmpty(newName))
            repo.RenameTemplate(row.Id, newName);

        row.IsEditingName = false;
        Reload();
    }

    private TemplateRow? RowFromSender(object sender) =>
        (sender as FrameworkElement)?.DataContext as TemplateRow;

    private sealed class TemplateRow : INotifyPropertyChanged
    {
        public required string Id { get; init; }
        public required string Name { get; set; }
        public required bool IsActive { get; init; }
        public required string ModuleCountText { get; init; }

        private bool _isEditingName;
        public bool IsEditingName
        {
            get => _isEditingName;
            set { _isEditingName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
