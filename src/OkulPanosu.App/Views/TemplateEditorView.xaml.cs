using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OkulPanosu.App.Services;
using OkulPanosu.App.Views.Modules;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class TemplateEditorView : UserControl
{
    public event EventHandler? BackRequested;

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Genel kural: her modül tipi bir şablona sadece BİR kez eklenebilir. "Büyük Anons
    /// (Görsel)" için kullanıcı istisna istedi — birden fazla afiş/kroki'yi TEK bir kalıp düzene (2x2 vb.)
    /// zorlamak yerine, bu tipten istediği kadar kopya ekleyip her birine ayrı görsel atayıp normal
    /// ızgara editörüyle (sürükle/boyutlandır) tamamen serbestçe dizebiliyor. "Büyük Anons (Yazı)" için de
    /// aynı istisna (ileride birden fazla ayrı anons metni gerekebilir diye) — mekanizma zaten paylaşılan
    /// (BoardModule.Settings her modül örneğine özel), tek satırlık ek yeterli.</summary>
    private static readonly HashSet<string> MultiInstanceModuleTypes = ["banner_image", "banner_text"];

    private readonly string _templateId;
    private readonly List<BoardModule> _modules = new();
    private double _editorPanelExpandedWidth = 420;
    private bool _editorPanelCollapsed;
    private bool _previewing;
    private string _colorMode = BoardColorModeManager.DefaultModeKey;
    private string _customBaseColor = BoardColorModeManager.DefaultCustomColorHex;

    public TemplateEditorView(string templateId)
    {
        InitializeComponent();
        _templateId = templateId;

        PreviewGrid.ConfigureRequested += OnConfigureModule;
        PreviewGrid.DeleteRequested += OnDeleteModule;
        PreviewGrid.EditorMode = true;
        ApplyCanvasSize();

        Load();
        BuildModulePalette();
    }

    /// <summary>Çalışma alanı, ızgara sütun/satır sayısından BAĞIMSIZ olarak her zaman seçili yayın
    /// ekranının gerçek çözünürlüğünde çizilir (bkz. AppServices.GetKioskScreenSize) — kullanıcı Excel'deki
    /// gibi sağa/aşağı kaydırarak dolaşır, böylece yerleştirilen modüllerin gerçek yayında ne kadar yer
    /// kapladığı editörde de birebir bellidir.</summary>
    private void ApplyCanvasSize()
    {
        var size = AppServices.GetKioskScreenSize();
        PreviewGrid.Width = size.Width;
        PreviewGrid.Height = size.Height;
        CanvasSizeText.Text = $"Çalışma alanı: {size.Width}×{size.Height} px (Ayarlar'da seçili yayın ekranının gerçek çözünürlüğü)";
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        var data = repo.Load();
        var template = data.Templates.FirstOrDefault(t => t.Id == _templateId);
        if (template is null) return;

        TitleText.Text = $"Düzen Editörü — {template.Name}";
        GridColumnsBox.Text = template.GridColumns.ToString(Turkish);
        GridRowsBox.Text = template.GridRows.ToString(Turkish);
        TickerTextBox.Text = template.TickerText;
        TickerSpeedBox.Text = template.TickerSpeed.ToString(Turkish);

        _colorMode = template.ColorMode;
        _customBaseColor = BoardColorModeManager.TryParseHex(template.CustomBaseColor, out _)
            ? template.CustomBaseColor
            : BoardColorModeManager.DefaultCustomColorHex;

        ColorModeComboHost.Content = EditorControls.LabeledComboBox(
            BoardColorModeManager.Modes, "DisplayName", "Key", _colorMode,
            v =>
            {
                _colorMode = v as string ?? BoardColorModeManager.DefaultModeKey;
                CustomColorPanel.Visibility = _colorMode == BoardColorModeManager.CustomModeKey ? Visibility.Visible : Visibility.Collapsed;
                RenderPreview();
            });
        CustomColorPanel.Visibility = _colorMode == BoardColorModeManager.CustomModeKey ? Visibility.Visible : Visibility.Collapsed;
        UpdateCustomColorDisplay();

        _modules.Clear();
        _modules.AddRange(template.Modules);

        RenderPreview();
    }

    private void OnConfigureModule(BoardModule module)
    {
        var dialog = new ModuleSettingsDialog(module, _templateId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;
        RenderPreview();
    }

    private void OnDeleteModule(BoardModule module)
    {
        var title = string.IsNullOrWhiteSpace(module.Title) ? ModuleFactory.DisplayNameFor(module.Type) : module.Title;
        if (!AppMessageBox.Confirm(Window.GetWindow(this), $"\"{title}\" modülünü panodan kaldırmak istediğinizden emin misiniz?", "Silme Onayı")) return;
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;

        _modules.Remove(module);
        RenderPreview();
        BuildModulePalette();
    }

    private void BuildModulePalette()
    {
        ModulePalette.Items.Clear();
        foreach (var (type, displayName) in ModuleFactory.KnownTypes)
        {
            var (w, h) = ModuleFactory.DefaultSizeFor(type);
            var count = _modules.Count(m => m.Type == type);
            var isMultiInstance = MultiInstanceModuleTypes.Contains(type);
            var alreadyAdded = !isMultiInstance && count > 0;

            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var sizeLabel = $"Boyut: {w}×{h} ızgara birimi";
            if (isMultiInstance && count > 0) sizeLabel += $" · {count} tane eklendi";

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textStack.Children.Add(new TextBlock { Text = displayName, FontSize = 13, FontWeight = FontWeights.SemiBold });
            textStack.Children.Add(new TextBlock { Text = sizeLabel, Style = (Style)FindResource("MutedTextStyle") });
            row.Children.Add(textStack);

            if (alreadyAdded)
            {
                var badge = new Border
                {
                    Background = (Brush)FindResource("SuccessBrush"),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = "Şablonda Var", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.White },
                };
                Grid.SetColumn(badge, 1);
                row.Children.Add(badge);
            }
            else
            {
                var addButton = new Button { Content = "Ekle +", Style = (Style)FindResource("SecondaryButtonStyle"), Padding = new Thickness(10, 4, 10, 4) };
                addButton.Click += (_, _) => AddModule(type, displayName);
                Grid.SetColumn(addButton, 1);
                row.Children.Add(addButton);
            }

            ModulePalette.Items.Add(new Border
            {
                Background = alreadyAdded ? (Brush)FindResource("BgElevatedBrush") : (Brush)FindResource("BgSecondaryBrush"),
                BorderBrush = (Brush)FindResource("BorderBrush1"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 6),
                Child = row,
            });
        }
    }

    private void AddModule(string type, string displayName)
    {
        var (w, h) = ModuleFactory.DefaultSizeFor(type);
        _modules.Add(new BoardModule { Type = type, Title = displayName, W = w, H = h, ThemeColor = "Blue" });
        RenderPreview();
        BuildModulePalette();
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        _previewing = !_previewing;
        PreviewGrid.EditorMode = !_previewing;
        PreviewButton.Content = _previewing ? "Düzenlemeye Dön" : "Önizle";
        RenderPreview();
    }

    private void UpdateCustomColorDisplay()
    {
        BoardColorModeManager.TryParseHex(_customBaseColor, out var color);
        CustomColorSwatch.Background = new SolidColorBrush(color);
        CustomColorHexText.Text = _customBaseColor.ToUpperInvariant();
    }

    private void PickCustomColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BoardColorModeManager.TryParseHex(_customBaseColor, out var current);
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B),
            FullOpen = true,
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        _customBaseColor = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        UpdateCustomColorDisplay();
        RenderPreview();
    }

    private void TickerFields_Changed(object sender, RoutedEventArgs e) => RenderPreview();

    private void GridSize_Changed(object sender, RoutedEventArgs e) => RenderPreview();

    private void GridSize_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter) RenderPreview();
    }

    private void RenderPreview()
    {
        if (!int.TryParse(GridColumnsBox.Text, out var cols) || cols <= 0) cols = 50;
        if (!int.TryParse(GridRowsBox.Text, out var rows) || rows <= 0) rows = 50;
        if (!int.TryParse(TickerSpeedBox.Text, NumberStyles.Integer, Turkish, out var tickerSpeed) || tickerSpeed <= 0) tickerSpeed = 25;

        // ÖNEMLİ: Id BURADA açıkça verilmezse `new Template { ... }` her RenderPreview çağrısında
        // (yani her küçük düzenlemede) RASTGELE yeni bir Id üretiyordu (Template.Id'nin varsayılan
        // değeri Guid.NewGuid()) — bu yüzden Duyurular'daki "bu şablonda yayında" eşleştirmesi
        // (PublishedInTemplateIds.Contains(template.Id)) ÖNİZLEMEDE hiçbir zaman tutmuyordu, kullanıcı
        // kutucuğu işaretlese de önizleme hep "yayında değil" gibi davranıyordu. Aynı nedenle Videos da
        // (kaydedilmiş şablondan gelmediği için) önizlemede hep BOŞ kalıyordu. Video listesi de gerçek
        // şablondan (kayıtlı hâlinden) taze okunuyor — Yerel Video ayarları ⚙️ içinden kaydedilir kaydedilmez
        // önizlemeye yansısın diye.
        var savedVideos = AppServices.Data?.Load().Templates.FirstOrDefault(t => t.Id == _templateId)?.Videos ?? [];

        var preview = new Template
        {
            Id = _templateId,
            GridColumns = cols,
            GridRows = rows,
            Modules = _modules,
            TickerText = TickerTextBox.Text,
            TickerSpeed = tickerSpeed,
            ColorMode = _colorMode,
            CustomBaseColor = _customBaseColor,
            Videos = savedVideos,
        };
        PreviewGrid.Render(preview);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;

        if (!int.TryParse(GridColumnsBox.Text, out var cols) || cols <= 0 ||
            !int.TryParse(GridRowsBox.Text, out var rows) || rows <= 0)
        {
            ShowError("Sütun/Satır sayısı geçerli bir pozitif sayı olmalı.");
            return;
        }

        if (!int.TryParse(TickerSpeedBox.Text, NumberStyles.Integer, Turkish, out var tickerSpeed) || tickerSpeed <= 0)
        {
            ShowError("Ticker hızı geçerli bir pozitif sayı olmalı.");
            return;
        }

        if (FindOverlap(_modules) is { } conflict)
        {
            ShowError($"\"{conflict.A}\" ile \"{conflict.B}\" modülleri çakışıyor — kaydetmeden önce konumlarını düzeltin.");
            return;
        }

        // Işgara sınırlarını aşan modülleri artık KAYDI ENGELLEMİYORUZ — sınıra sığacak şekilde otomatik
        // küçültüp kaydı tamamlıyoruz, sadece HANGİ modül(ler)in küçültüldüğünü bir diyalogla bildiriyoruz.
        // Böylece bir modülün sığmaması yüzünden diğer TÜM değişiklikler kaybolmuyordu (kullanıcı bunu
        // "tek tek küçültüp deniyorum" diye tarif etmişti).
        var shrunk = ClampOverflowingModules(_modules, cols, rows);

        repo.UpdateTemplateModules(_templateId, cols, rows, _modules, TickerTextBox.Text, tickerSpeed, _colorMode, _customBaseColor);

        if (shrunk.Count > 0)
        {
            RenderPreview();
            AppMessageBox.Show(Window.GetWindow(this),
                $"Işgara sınırlarına sığmayan şu modüllerin boyutu otomatik olarak sınıra sığacak şekilde küçültüldü, diğer tüm değişiklikleriniz kaydedildi:\n\n{string.Join("\n", shrunk.Select(s => "• " + s))}",
                "Bazı Modüller Küçültüldü");
        }

        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>X/Y konumunu aynen bırakıp W/H'yi ızgara sınırına sığacak şekilde küçültür — modülü hiç
    /// silmez/taşımaz, sadece taşan kısmını keser. Küçültülen modüllerin (başlık varsa başlık, yoksa tip
    /// adı) listesini döner.</summary>
    private static List<string> ClampOverflowingModules(List<BoardModule> modules, int cols, int rows)
    {
        var shrunk = new List<string>();
        foreach (var module in modules)
        {
            var newW = Math.Min(module.W, Math.Max(1, cols - module.X));
            var newH = Math.Min(module.H, Math.Max(1, rows - module.Y));
            if (newW == module.W && newH == module.H) continue;

            shrunk.Add(string.IsNullOrWhiteSpace(module.Title) ? ModuleFactory.DisplayNameFor(module.Type) : module.Title);
            module.W = newW;
            module.H = newH;
        }
        return shrunk;
    }

    private static (string A, string B)? FindOverlap(List<BoardModule> modules)
    {
        for (var i = 0; i < modules.Count; i++)
        {
            for (var j = i + 1; j < modules.Count; j++)
            {
                var a = modules[i];
                var b = modules[j];
                var overlapsX = a.X < b.X + b.W && b.X < a.X + a.W;
                var overlapsY = a.Y < b.Y + b.H && b.Y < a.Y + a.H;
                if (overlapsX && overlapsY)
                    return (string.IsNullOrWhiteSpace(a.Title) ? ModuleFactory.DisplayNameFor(a.Type) : a.Title,
                            string.IsNullOrWhiteSpace(b.Title) ? ModuleFactory.DisplayNameFor(b.Type) : b.Title);
            }
        }
        return null;
    }

    /// <summary>Önceden sol üstte küçük, gözden kaçabilen bir metin olarak gösteriliyordu — kullanıcı
    /// bunu fark etmeyip aynı hatayı tekrar tekrar aldığını sanmıştı. Artık gerçek bir diyalog penceresi.</summary>
    private void ShowError(string message) =>
        AppMessageBox.Show(Window.GetWindow(this), message, "Şablon Kaydedilemedi");

    private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    private void ToggleEditorPanel_Click(object sender, RoutedEventArgs e)
    {
        _editorPanelCollapsed = !_editorPanelCollapsed;
        if (_editorPanelCollapsed)
        {
            if (EditorPanelColumn.ActualWidth > 0) _editorPanelExpandedWidth = EditorPanelColumn.ActualWidth;
            EditorPanelColumn.Width = new GridLength(0);
            ToggleEditorPanelButton.Content = "▶";
        }
        else
        {
            EditorPanelColumn.Width = new GridLength(_editorPanelExpandedWidth);
            ToggleEditorPanelButton.Content = "◀";
        }
    }
}
