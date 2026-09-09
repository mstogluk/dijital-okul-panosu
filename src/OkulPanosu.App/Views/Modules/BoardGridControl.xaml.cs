using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>
/// Işgara matris motoru — bir Template'i GridColumns x GridRows boyutunda bir Grid'e,
/// her modülü X/Y/W/H'sine göre yerleştirerek render eder. Hem Kiosk (BoardWindow) hem
/// Yönetim tarafındaki düzen önizlemesi (TemplateEditorView) tarafından kullanılır.
///
/// <see cref="EditorMode"/> açıkken: hücre çizgileri görünür, her modülün başlık çubuğunda
/// ayar/sil butonları belirir, modül tıklanıp seçilebilir ve seçili modül yön tuşlarıyla
/// taşınır / Shift+yön tuşlarıyla boyutlandırılır (aynı Template.Modules listesindeki nesneler
/// doğrudan mutasyona uğrar — ayrı bir "kaydet" adımı gerekmez, çağıran taraf zaten aynı listeyi tutuyor).
/// </summary>
public partial class BoardGridControl : UserControl
{
    private Template? _template;
    private BoardModule? _selectedModule;

    public bool EditorMode { get; set; }

    public event Action<BoardModule>? ConfigureRequested;
    public event Action<BoardModule>? DeleteRequested;

    public BoardGridControl()
    {
        InitializeComponent();
        Focusable = true;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public void Render(Template? template)
    {
        _template = template;
        BoardColorModeManager.Apply(this, template);
        RootGrid.ColumnDefinitions.Clear();
        RootGrid.RowDefinitions.Clear();
        RootGrid.Children.Clear();

        var institutionName = AppServices.Data?.Load().InstitutionName;
        if (!string.IsNullOrWhiteSpace(institutionName))
        {
            SchoolNameText.Text = institutionName;
            SchoolNameHeader.Visibility = Visibility.Visible;
        }
        else
        {
            SchoolNameHeader.Visibility = Visibility.Collapsed;
        }

        Ticker.SetTemplate(template);

        if (template is null) return;

        for (var c = 0; c < template.GridColumns; c++)
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var r = 0; r < template.GridRows; r++)
            RootGrid.RowDefinitions.Add(new RowDefinition());

        if (EditorMode)
            DrawGridLines(template);

        foreach (var module in template.Modules)
            RootGrid.Children.Add(BuildModuleCard(module, template));
    }

    private void DrawGridLines(Template template)
    {
        for (var r = 0; r < template.GridRows; r++)
        {
            for (var c = 0; c < template.GridColumns; c++)
            {
                var cell = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromArgb(45, 148, 163, 184)),
                    BorderThickness = new Thickness(0.5),
                    Margin = new Thickness(1),
                    IsHitTestVisible = false,
                };
                Grid.SetColumn(cell, c);
                Grid.SetRow(cell, r);
                RootGrid.Children.Add(cell);
            }
        }
    }

    private UIElement BuildModuleCard(BoardModule module, Template template)
    {
        var isSelected = EditorMode && ReferenceEquals(module, _selectedModule);
        var accentColor = (Color)ColorConverter.ConvertFromString(ThemeManager.GetSwatchHex(module.ThemeColor))!;
        var accentBrush = new SolidColorBrush(accentColor);
        var opaqueCardBackground = (Brush)FindResource("BgSecondaryBrush");
        var headerBackground = new SolidColorBrush(Color.FromArgb(210, accentColor.R, accentColor.G, accentColor.B));

        const double cornerRadius = 10;

        var card = new Border
        {
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(cornerRadius),
            Background = opaqueCardBackground,
            BorderBrush = isSelected ? Brushes.White : new SolidColorBrush(Color.FromArgb(180, accentColor.R, accentColor.G, accentColor.B)),
            BorderThickness = new Thickness(isSelected ? 2 : 1),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = 3,
                BlurRadius = 12,
                Opacity = 0.4,
            },
        };

        // Border.ClipToBounds SADECE dikdörtgen sınıra göre kırpar, kartın kendi yuvarlak köşesine göre
        // DEĞİL — bu yüzden köşeli header köşelerde kartın yuvarlak kenarından taşıp küçük bir boşluk
        // bırakıyordu. Kesin çözüm: kartın kendisine, boyutu belli olduğunda AYNI radius'lu bir
        // RectangleGeometry Clip veriyoruz — böylece TÜM içerik (header dahil) birebir aynı köşeye kırpılır.
        card.SizeChanged += (_, _) =>
        {
            if (card.ActualWidth <= 0 || card.ActualHeight <= 0) return;
            card.Clip = new RectangleGeometry(new Rect(0, 0, card.ActualWidth, card.ActualHeight), cornerRadius, cornerRadius);
        };

        var content = new DockPanel();

        var headerBorder = new Border { Background = headerBackground };
        var header = new Grid();
        headerBorder.Child = header;
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 5, 6, 5) };
        titleStack.Children.Add(new Ellipse
        {
            Width = 8,
            Height = 8,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Fill = accentBrush,
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(module.Title) ? ModuleFactory.DisplayNameFor(module.Type) : module.Title,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        header.Children.Add(titleStack);

        if (EditorMode)
        {
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 4, 0) };
            buttons.Children.Add(MakeIconButton("⚙️ Ayar", "Ayarlar", () => ConfigureRequested?.Invoke(module)));
            buttons.Children.Add(MakeIconButton("🗑 Sil", "Sil", () => DeleteRequested?.Invoke(module)));
            Grid.SetColumn(buttons, 1);
            header.Children.Add(buttons);

            headerBorder.Cursor = Cursors.SizeAll;
            headerBorder.ToolTip = "Sürükleyerek taşı";
            headerBorder.MouseLeftButtonDown += (_, e) => StartMoveDrag(module, card, headerBorder, e);
        }

        DockPanel.SetDock(headerBorder, Dock.Top);
        content.Children.Add(headerBorder);

        if (isSelected)
        {
            var footer = BuildSizeToolbar(module, template);
            DockPanel.SetDock(footer, Dock.Bottom);
            content.Children.Add(footer);
        }

        content.Children.Add(ModuleFactory.CreateView(module, template));

        var overlay = new Grid();
        overlay.Children.Add(content);

        if (isSelected)
        {
            var grip = new Border
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 3, 3),
                Background = accentBrush,
                CornerRadius = new CornerRadius(3),
                Cursor = Cursors.SizeNWSE,
                ToolTip = "Sürükleyerek boyutlandır",
            };
            grip.MouseLeftButtonDown += (_, e) => StartResizeDrag(module, card, grip, e);
            overlay.Children.Add(grip);
        }

        card.Child = overlay;

        if (EditorMode)
        {
            card.Cursor = Cursors.Hand;
            card.MouseLeftButtonDown += (_, e) =>
            {
                _selectedModule = module;
                Focus();
                Render(template);
                e.Handled = true;
            };
        }

        Grid.SetColumn(card, Math.Clamp(module.X, 0, Math.Max(0, template.GridColumns - 1)));
        Grid.SetRow(card, Math.Clamp(module.Y, 0, Math.Max(0, template.GridRows - 1)));
        Grid.SetColumnSpan(card, Math.Max(1, Math.Min(module.W, template.GridColumns - module.X)));
        Grid.SetRowSpan(card, Math.Max(1, Math.Min(module.H, template.GridRows - module.Y)));

        return card;
    }

    private UIElement BuildSizeToolbar(BoardModule module, Template template)
    {
        var bar = new Grid
        {
            Background = (Brush)FindResource("BgElevatedBrush"),
        };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sizeLabel = new TextBlock
        {
            Text = $"{module.W}×{module.H}  ({module.X},{module.Y})",
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = (Brush)FindResource("TextMutedBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 4, 4, 4),
        };
        bar.Children.Add(sizeLabel);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 2, 4, 2) };
        buttons.Children.Add(MakeIconButton("◀", "Sola taşı", () => MoveModule(module, -1, 0)));
        buttons.Children.Add(MakeIconButton("▶", "Sağa taşı", () => MoveModule(module, 1, 0)));
        buttons.Children.Add(MakeIconButton("▲", "Yukarı taşı", () => MoveModule(module, 0, -1)));
        buttons.Children.Add(MakeIconButton("▼", "Aşağı taşı", () => MoveModule(module, 0, 1)));
        buttons.Children.Add(new Border { Width = 1, Margin = new Thickness(4, 2, 4, 2), Background = (Brush)FindResource("BorderBrush1") });
        buttons.Children.Add(MakeIconButton("−W", "Daralt", () => ResizeModule(module, -1, 0)));
        buttons.Children.Add(MakeIconButton("+W", "Genişlet", () => ResizeModule(module, 1, 0)));
        buttons.Children.Add(MakeIconButton("−H", "Alçalt", () => ResizeModule(module, 0, -1)));
        buttons.Children.Add(MakeIconButton("+H", "Yükselt", () => ResizeModule(module, 0, 1)));
        Grid.SetColumn(buttons, 1);
        bar.Children.Add(buttons);

        return bar;
    }

    private void StartResizeDrag(BoardModule module, Border card, Border grip, MouseButtonEventArgs e)
    {
        if (_template is null || RootGrid.ActualWidth <= 0 || RootGrid.ActualHeight <= 0) return;

        var template = _template;
        var startPoint = e.GetPosition(RootGrid);
        var startW = module.W;
        var startH = module.H;
        var cellWidth = RootGrid.ActualWidth / template.GridColumns;
        var cellHeight = RootGrid.ActualHeight / template.GridRows;

        grip.CaptureMouse();
        e.Handled = true;

        MouseEventHandler onMove = null!;
        MouseButtonEventHandler onUp = null!;

        onMove = (_, moveArgs) =>
        {
            var (w, h) = ComputeDragSize(moveArgs.GetPosition(RootGrid));
            Grid.SetColumnSpan(card, w);
            Grid.SetRowSpan(card, h);
        };

        onUp = (_, upArgs) =>
        {
            grip.ReleaseMouseCapture();
            grip.MouseMove -= onMove;
            grip.MouseLeftButtonUp -= onUp;

            var (w, h) = ComputeDragSize(upArgs.GetPosition(RootGrid));
            module.W = w;
            module.H = h;
            Render(template);
        };

        grip.MouseMove += onMove;
        grip.MouseLeftButtonUp += onUp;

        (int W, int H) ComputeDragSize(Point pos)
        {
            var dCols = (int)Math.Round((pos.X - startPoint.X) / cellWidth);
            var dRows = (int)Math.Round((pos.Y - startPoint.Y) / cellHeight);
            var w = Math.Clamp(startW + dCols, 1, Math.Max(1, template.GridColumns - module.X));
            var h = Math.Clamp(startH + dRows, 1, Math.Max(1, template.GridRows - module.Y));
            return (w, h);
        }
    }

    /// <summary>Modül başlık çubuğundan fareyle tutup sürükleyerek taşıma — yön tuşlarıyla taşımanın
    /// fare karşılığı. Sürükleme sırasında (performans/görsel süreklilik için) tam bir Render() TETİKLENMEZ,
    /// sadece kartın Grid.Column/Row'u canlı güncellenir; bırakınca modül gerçekten mutasyona uğrar ve
    /// temiz bir Render() ile seçili/araç çubuklu hâli gösterilir (StartResizeDrag ile aynı desen).</summary>
    private void StartMoveDrag(BoardModule module, Border card, UIElement handle, MouseButtonEventArgs e)
    {
        if (_template is null || RootGrid.ActualWidth <= 0 || RootGrid.ActualHeight <= 0) return;

        var template = _template;
        _selectedModule = module;
        Focus();

        var startPoint = e.GetPosition(RootGrid);
        var startX = module.X;
        var startY = module.Y;
        var cellWidth = RootGrid.ActualWidth / template.GridColumns;
        var cellHeight = RootGrid.ActualHeight / template.GridRows;

        handle.CaptureMouse();
        e.Handled = true;

        MouseEventHandler onMove = null!;
        MouseButtonEventHandler onUp = null!;

        onMove = (_, moveArgs) =>
        {
            var (x, y) = ComputeDragPosition(moveArgs.GetPosition(RootGrid));
            Grid.SetColumn(card, x);
            Grid.SetRow(card, y);
        };

        onUp = (_, upArgs) =>
        {
            handle.ReleaseMouseCapture();
            handle.MouseMove -= onMove;
            handle.MouseLeftButtonUp -= onUp;

            var (x, y) = ComputeDragPosition(upArgs.GetPosition(RootGrid));
            module.X = x;
            module.Y = y;
            Render(template);
        };

        handle.MouseMove += onMove;
        handle.MouseLeftButtonUp += onUp;

        (int X, int Y) ComputeDragPosition(Point pos)
        {
            var dCols = (int)Math.Round((pos.X - startPoint.X) / cellWidth);
            var dRows = (int)Math.Round((pos.Y - startPoint.Y) / cellHeight);
            var x = Math.Clamp(startX + dCols, 0, Math.Max(0, template.GridColumns - module.W));
            var y = Math.Clamp(startY + dRows, 0, Math.Max(0, template.GridRows - module.H));
            return (x, y);
        }
    }

    private void MoveModule(BoardModule module, int dx, int dy)
    {
        if (_template is null) return;
        module.X = Math.Clamp(module.X + dx, 0, Math.Max(0, _template.GridColumns - module.W));
        module.Y = Math.Clamp(module.Y + dy, 0, Math.Max(0, _template.GridRows - module.H));
        Render(_template);
    }

    private void ResizeModule(BoardModule module, int dw, int dh)
    {
        if (_template is null) return;
        module.W = Math.Clamp(module.W + dw, 1, Math.Max(1, _template.GridColumns - module.X));
        module.H = Math.Clamp(module.H + dh, 1, Math.Max(1, _template.GridRows - module.Y));
        Render(_template);
    }

    private Button MakeIconButton(string glyph, string tooltip, Action action)
    {
        var button = new Button
        {
            Content = glyph,
            ToolTip = tooltip,
            MinWidth = 20,
            Height = 20,
            FontSize = 10,
            Padding = new Thickness(3, 0, 3, 0),
            Margin = new Thickness(2, 0, 0, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = (Brush)FindResource("TextMutedBrush"),
            Cursor = Cursors.Hand,
        };
        button.Click += (_, e) =>
        {
            action();
            e.Handled = true;
        };
        return button;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!EditorMode || _selectedModule is not { } module || _template is null) return;

        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        switch (e.Key)
        {
            case Key.Left when shift: ResizeModule(module, -1, 0); break;
            case Key.Left: MoveModule(module, -1, 0); break;
            case Key.Right when shift: ResizeModule(module, 1, 0); break;
            case Key.Right: MoveModule(module, 1, 0); break;
            case Key.Up when shift: ResizeModule(module, 0, -1); break;
            case Key.Up: MoveModule(module, 0, -1); break;
            case Key.Down when shift: ResizeModule(module, 0, 1); break;
            case Key.Down: MoveModule(module, 0, 1); break;
            case Key.Escape:
                _selectedModule = null;
                Render(_template);
                break;
            default:
                return;
        }

        e.Handled = true;
    }
}
