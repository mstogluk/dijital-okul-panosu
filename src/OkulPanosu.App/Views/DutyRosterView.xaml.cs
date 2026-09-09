using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

/// <summary>Gerçek bir nöbet çizelgesi gibi: satırlar kat/alan, sütunlar gün. Her hücrede birden
/// fazla öğretmen (çip + çarpı) olabilir, Personel listesinden seçilerek eklenir.</summary>
public partial class DutyRosterView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly DayOfWeek[] WorkDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private readonly List<DutyRosterDay> _roster = new();
    private List<Personnel> _personnel = new();

    public DutyRosterView()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        var data = repo.Load();

        _personnel = data.Personnel;
        _roster.Clear();
        _roster.AddRange(Enum.GetValues<DayOfWeek>().Select(d =>
            data.DutyRoster.FirstOrDefault(r => r.Day == d) ?? new DutyRosterDay { Day = d }));

        BuildMatrix();
    }

    /// <summary>Sayfa önbellekten yeniden gösterildiğinde çağrılır — Personel sayfasında az önce
    /// eklenen öğretmenler buradaki "+ Öğretmen" listesinde hemen görünsün diye.</summary>
    public void Reload() => Load();

    private DutyRosterDay DayRow(DayOfWeek day) => _roster.First(r => r.Day == day);

    private void BuildMatrix()
    {
        MatrixGrid.RowDefinitions.Clear();
        MatrixGrid.ColumnDefinitions.Clear();
        MatrixGrid.Children.Clear();

        MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        foreach (var _ in WorkDays)
            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

        var rowCount = 2 + DutyRosterDay.DefaultFloors.Length;
        for (var i = 0; i < rowCount; i++)
            MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Gün başlıkları
        AddCell(0, 0, HeaderCell(""));
        for (var c = 0; c < WorkDays.Length; c++)
            AddCell(0, c + 1, HeaderCell(Turkish.DateTimeFormat.GetDayName(WorkDays[c]).ToUpper(Turkish)));

        // Müdür yardımcısı satırı
        AddCell(1, 0, HeaderCell("👑 NÖBETÇİ MD. YRD."));
        for (var c = 0; c < WorkDays.Length; c++)
        {
            var day = DayRow(WorkDays[c]);
            var combo = PersonnelCombo(day.Deputy, name => day.Deputy = name ?? "");
            AddCell(1, c + 1, Pad(combo));
        }

        // Kat satırları
        for (var f = 0; f < DutyRosterDay.DefaultFloors.Length; f++)
        {
            var floor = DutyRosterDay.DefaultFloors[f];
            AddCell(f + 2, 0, HeaderCell(floor.ToUpper(Turkish)));
            for (var c = 0; c < WorkDays.Length; c++)
            {
                var day = DayRow(WorkDays[c]);
                AddCell(f + 2, c + 1, Pad(BuildFloorCell(day, floor)));
            }
        }
    }

    private UIElement BuildFloorCell(DutyRosterDay day, string floor)
    {
        var stack = new StackPanel();

        void Rebuild()
        {
            stack.Children.Clear();
            foreach (var assignment in day.Assignments.Where(a => a.Floor == floor).ToList())
            {
                var chip = new Border
                {
                    Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 4, 6, 4),
                    Margin = new Thickness(0, 0, 0, 4),
                };
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Children.Add(new TextBlock { Text = assignment.TeacherName, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
                var remove = new Button { Content = "x", Style = EditorControls.SecondaryButton, Width = 22, Height = 22, Padding = new Thickness(0), FontSize = 11, ToolTip = "Kaldır" };
                remove.Click += (_, _) => { day.Assignments.Remove(assignment); Rebuild(); };
                Grid.SetColumn(remove, 1);
                row.Children.Add(remove);
                chip.Child = row;
                stack.Children.Add(chip);
            }

            var addCombo = new ComboBox
            {
                ItemsSource = _personnel.Where(p => p.Category == "teacher").Select(p => p.Name)
                    .Where(n => !day.Assignments.Any(a => a.Floor == floor && a.TeacherName == n)).ToList(),
                SelectedIndex = -1,
                ToolTip = "+ Öğretmen ekle",
            };
            addCombo.SelectionChanged += (_, _) =>
            {
                if (addCombo.SelectedItem is not string name) return;
                day.Assignments.Add(new DutyAssignment { Floor = floor, TeacherName = name });
                Rebuild();
            };
            stack.Children.Add(addCombo);
        }

        Rebuild();
        return stack;
    }

    private ComboBox PersonnelCombo(string currentName, Action<string?> onChanged)
    {
        var names = _personnel.Select(p => p.Name).ToList();
        if (!string.IsNullOrWhiteSpace(currentName) && !names.Contains(currentName)) names.Insert(0, currentName);
        names.Insert(0, "— Seçiniz —");

        var combo = new ComboBox { ItemsSource = names, SelectedItem = string.IsNullOrWhiteSpace(currentName) ? "— Seçiniz —" : currentName };
        combo.SelectionChanged += (_, _) =>
        {
            var value = combo.SelectedItem as string;
            onChanged(value == "— Seçiniz —" ? null : value);
        };
        return combo;
    }

    private static Border HeaderCell(string text) => new()
    {
        Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x5F)),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(10, 10, 10, 10),
        Margin = new Thickness(2),
        Child = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
        },
    };

    private static Border Pad(UIElement content) => new() { Padding = new Thickness(6), Child = content };

    private void AddCell(int row, int col, UIElement element)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, col);
        MatrixGrid.Children.Add(element);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.DutyRoster = _roster);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
