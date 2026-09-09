using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

public partial class FoodMenuModuleView : UserControl
{
    public FoodMenuModuleView(BoardModule module)
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        var today = DateTime.Now.DayOfWeek;
        var menu = AppServices.Data?.Load().FoodMenu ?? [];
        var day = menu.FirstOrDefault(m => m.Day == today);

        if (day is null || day.Items.Count == 0)
        {
            ItemsList.Visibility = Visibility.Collapsed;
            CaloriesText.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Bugün için yemek menüsü tanımlanmamış";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        ItemsList.ItemsSource = day.Items;
        ItemsList.Visibility = Visibility.Visible;
        EmptyText.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrWhiteSpace(day.Calories))
        {
            CaloriesText.Text = day.Calories;
            CaloriesText.Visibility = Visibility.Visible;
        }
    }
}
