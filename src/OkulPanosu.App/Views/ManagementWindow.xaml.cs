using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Models;
using OkulPanosu.App.Services;

namespace OkulPanosu.App.Views;

public partial class ManagementWindow : Window
{
    private static readonly NavItem[] ContentSubItems =
    [
        new("duty_roster", "🗓️", "Nöbet Çizelgesi"),
        new("announcements", "📢", "Duyurular"),
        new("personnel", "👥", "Personel & Öğretmenler"),
        new("food_menu", "🍽️", "Yemek Menüsü"),
        new("schedule", "📖", "Ders & Zil Saatleri"),
        new("class_schedules", "📚", "Sınıf Ders Programı"),
        new("birthdays", "🎓", "Öğrenciler"),
        new("student_of_month", "🏆", "Ayın Öğrencisi"),
        new("videos", "🎬", "Yerel Videolar"),
        new("today_in_history", "📜", "Tarihte Bugün"),
        new("weekly_question", "🧠", "Haftanın Beyin Egzersizi"),
        new("cleanest_class", "🧹", "Haftanın Temiz Sınıfları"),
        new("school_gallery", "🖼️", "Okulumuzdan Kareler"),
    ];

    private readonly Dictionary<string, UserControl> _pages = new();
    private BoardWindow? _boardWindow;
    private double _navExpandedWidth = 200;
    private bool _navCollapsed;
    private const double SubNavWidth = 210;

    public ManagementWindow()
    {
        InitializeComponent();
        ApplySavedWindowSize();
        PositionOnPrimaryScreen();

        NavList.ItemsSource = new[]
        {
            new NavItem("templates", "📋", "Şablonlar"),
            new NavItem("content", "🗂️", "İçerikler"),
            new NavItem("settings", "⚙️", "Ayarlar"),
            new NavItem("help", "❓", "Yardım"),
        };
        SubNavList.ItemsSource = ContentSubItems;
        NavList.SelectedIndex = 0;

        Closing += (_, _) => SaveWindowSize();
    }

    /// <summary>Pencere boyutu (ve tam ekran durumu) makineye özel ayarlarda saklanır — kullanıcı
    /// pencereyi büyütüp küçültünce, bir dahaki açılışta aynı boyutta gelsin diye.</summary>
    private void ApplySavedWindowSize()
    {
        var settings = AppServices.LocalSettings;
        if (settings.ManagementWindowWidth is { } w && w > 400) Width = w;
        if (settings.ManagementWindowHeight is { } h && h > 300) Height = h;
        if (settings.ManagementWindowMaximized) WindowState = WindowState.Maximized;
    }

    private void SaveWindowSize()
    {
        var settings = AppServices.LocalSettings;
        settings.ManagementWindowMaximized = WindowState == WindowState.Maximized;

        var size = WindowState == WindowState.Maximized ? RestoreBounds.Size : new Size(ActualWidth, ActualHeight);
        if (size.Width > 0 && size.Height > 0)
        {
            settings.ManagementWindowWidth = size.Width;
            settings.ManagementWindowHeight = size.Height;
        }

        AppServices.SaveLocalSettings();
    }

    /// <summary>Kiosk PC'sinde Ctrl+Alt+Y ile tam ekrandan çıkıldığında, mevcut (gizlenmiş) BoardWindow'u
    /// yeni bir tane açmak yerine yeniden kullanmak için — bkz. BoardWindow.ExitKiosk.</summary>
    public ManagementWindow(BoardWindow existingBoardWindow) : this()
    {
        _boardWindow = existingBoardWindow;
        _boardWindow.Closed += (_, _) => _boardWindow = null;
        _boardWindow.BroadcastStopped += () => StartBoardButton.Content = "📺 Yayını Başlat";

        // Yayın hâlâ sürüyorsa (ör. sistem tepsisinden yayını durdurmadan Yönetim'e geçildiyse)
        // buton baştan "Durdur" durumunu göstersin.
        if (_boardWindow.IsBroadcasting) StartBoardButton.Content = "⏹ Yayını Durdur";
    }

    /// <summary>Yönetim penceresi her zaman ANA ekranda açılır — "CenterScreen" imleç konumuna göre
    /// (kiosk ekranındaysa oraya) merkezleyebiliyordu, kullanıcı TV ekranına erişemediği için pencereyi
    /// göremiyordu. Burada açıkça birincil ekranın ortasına konumlandırıyoruz.</summary>
    private void PositionOnPrimaryScreen()
    {
        var area = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea
                   ?? System.Windows.Forms.Screen.AllScreens[0].WorkingArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not NavItem item) return;

        if (item.Key == "content")
        {
            SubNavBorder.Visibility = Visibility.Visible;
            SubNavColumn.Width = new GridLength(SubNavWidth);
            if (SubNavList.SelectedIndex < 0) SubNavList.SelectedIndex = 0;
            else MainContent.Content = GetOrCreatePage(((NavItem)SubNavList.SelectedItem).Key);
            return;
        }

        SubNavBorder.Visibility = Visibility.Collapsed;
        SubNavColumn.Width = new GridLength(0);
        MainContent.Content = GetOrCreatePage(item.Key);
    }

    private void SubNavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubNavList.SelectedItem is not NavItem item) return;
        MainContent.Content = GetOrCreatePage(item.Key);
    }

    private UserControl GetOrCreatePage(string key)
    {
        if (_pages.TryGetValue(key, out var existing))
        {
            if (existing is IReloadablePage reloadable) reloadable.Reload();
            return existing;
        }

        UserControl page = key switch
        {
            "templates" => CreateTemplatesView(),
            "duty_roster" => new DutyRosterView(),
            "announcements" => new AnnouncementsView(),
            "personnel" => new PersonnelView(),
            "food_menu" => new FoodMenuView(),
            "schedule" => new ScheduleView(),
            "class_schedules" => new ClassSchedulesView(),
            "birthdays" => new StudentsView(),
            "student_of_month" => new StudentOfMonthView(),
            "videos" => new VideosView(),
            "today_in_history" => new TodayInHistoryView(),
            "weekly_question" => new WeeklyQuestionView(),
            "cleanest_class" => new CleanestClassView(),
            "school_gallery" => new SchoolGalleryView(),
            "settings" => new SettingsView(),
            "help" => new HelpView(),
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };

        _pages[key] = page;
        return page;
    }

    private TemplatesView CreateTemplatesView()
    {
        var view = new TemplatesView();
        view.EditModulesRequested += ShowEditor;
        return view;
    }

    private void ShowEditor(string templateId)
    {
        var editor = new TemplateEditorView(templateId);
        editor.BackRequested += (_, _) =>
        {
            NavList.SelectedIndex = 0;
            MainContent.Content = GetOrCreatePage("templates");
            _boardWindow?.RenderActiveTemplate();
        };
        MainContent.Content = editor;
    }

    private void ToggleNav_Click(object sender, RoutedEventArgs e)
    {
        _navCollapsed = !_navCollapsed;
        if (_navCollapsed)
        {
            if (NavColumn.ActualWidth > 0) _navExpandedWidth = NavColumn.ActualWidth;
            NavColumn.Width = new GridLength(0);
            ToggleNavButton.Content = "▶";
        }
        else
        {
            NavColumn.Width = new GridLength(_navExpandedWidth);
            ToggleNavButton.Content = "◀";
        }
    }

    private void StartBoard_Click(object sender, RoutedEventArgs e) => ToggleBoard();

    /// <summary>Yayın TV ekranında ise fareyle oraya gidip Ctrl+Alt+Y'ye basmak pratik değil — bu yüzden
    /// Yönetim penceresi odaktayken de aynı kısayol çalışır ve yayını başlatır/durdurur.</summary>
    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Y ||
            System.Windows.Input.Keyboard.Modifiers != (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt))
            return;

        ToggleBoard();
    }

    private void ToggleBoard()
    {
        if (_boardWindow is { IsBroadcasting: true })
        {
            if (!_boardWindow.StopBroadcastFromManagement(this)) return;
            StartBoardButton.Content = "📺 Yayını Başlat";
            return;
        }

        if (_boardWindow is null)
        {
            _boardWindow = new BoardWindow();
            _boardWindow.AttachManagementWindow(this);
            _boardWindow.BroadcastStopped += () => StartBoardButton.Content = "📺 Yayını Başlat";
            _boardWindow.Closed += (_, _) => _boardWindow = null;
        }

        _boardWindow.ShowKiosk();
        StartBoardButton.Content = "⏹ Yayını Durdur";
    }
}
