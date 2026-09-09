using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class FoodMenuView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private const int MealSlots = 4;
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly DayOfWeek[] WorkDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private readonly List<FoodMenuDay> _menu = new();

    public FoodMenuView()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        var data = repo.Load();

        _menu.Clear();
        _menu.AddRange(Enum.GetValues<DayOfWeek>().Select(d =>
            data.FoodMenu.FirstOrDefault(m => m.Day == d) ?? new FoodMenuDay { Day = d }));

        Rebuild();
    }

    public void Reload() => Load();

    private void Rebuild()
    {
        DaysList.Items.Clear();
        foreach (var day in WorkDays)
        {
            var menu = _menu.First(m => m.Day == day);
            while (menu.Items.Count < MealSlots) menu.Items.Add("");

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = Turkish.DateTimeFormat.GetDayName(day).ToUpper(Turkish),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["WarningBrush"],
                Margin = new Thickness(0, 0, 0, 8),
            });

            for (var i = 0; i < MealSlots; i++)
            {
                var slot = i;
                panel.Children.Add(EditorControls.LabeledTextBox($"{i + 1}. Yemek", menu.Items[i], v => menu.Items[slot] = v));
            }

            panel.Children.Add(EditorControls.LabeledTextBox("Kalori (kcal)", menu.Calories, v => menu.Calories = v));

            DaysList.Items.Add(EditorControls.CardWith(panel, new Thickness(0, 0, 8, 0)));
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        foreach (var day in _menu) day.Items.RemoveAll(string.IsNullOrWhiteSpace);
        repo.UpdateContent(data => data.FoodMenu = _menu);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
