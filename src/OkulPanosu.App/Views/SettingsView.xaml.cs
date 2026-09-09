using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class SettingsView : UserControl
{
    private bool _loaded;

    public SettingsView()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        _loaded = false;

        if (AppServices.Data is { } repo)
        {
            var data = repo.Load();
            InstitutionNameBox.Text = data.InstitutionName;
            InstitutionCityBox.Text = data.InstitutionCity;
        }

        DataFolderText.Text = AppServices.LocalSettings.DataFolderPath;

        var screens = System.Windows.Forms.Screen.AllScreens
            .Select((s, i) => new MonitorItem(
                s.DeviceName,
                $"Ekran {i + 1} - {s.Bounds.Width}x{s.Bounds.Height}{(s.Primary ? " (Birincil)" : "")}"))
            .ToList();
        MonitorCombo.ItemsSource = screens;
        MonitorCombo.SelectedValue = AppServices.LocalSettings.KioskMonitorDeviceName ?? screens.FirstOrDefault()?.DeviceName;

        AutoStartCheck.IsChecked = AutoStartService.IsEnabled();

        KioskRadio.IsChecked = AppServices.LocalSettings.StartupMode == StartupModes.Kiosk;
        ManagementRadio.IsChecked = AppServices.LocalSettings.StartupMode == StartupModes.Management;

        BuildThemeSwatches();

        _loaded = true;
    }

    private void InstitutionInfo_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loaded || AppServices.Data is not { } repo) return;

        var data = repo.Load();
        data.InstitutionName = InstitutionNameBox.Text;
        data.InstitutionCity = InstitutionCityBox.Text;
        repo.Save(data);
    }

    private void ChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;

        var dialog = new OpenFolderDialog { Title = "Veri Klasörünü Seçin" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            AppServices.SetDataFolder(dialog.FolderName);
        }
        catch (Exception ex) when (AppServices.IsFolderUnreachableError(ex))
        {
            AppMessageBox.Show(Window.GetWindow(this), AppServices.DescribeFolderUnreachable(dialog.FolderName), "Veri Klasörüne Ulaşılamıyor");
            return;
        }

        DataFolderText.Text = dialog.FolderName;
    }

    private void MonitorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || MonitorCombo.SelectedValue is not string deviceName) return;

        AppServices.LocalSettings.KioskMonitorDeviceName = deviceName;
        AppServices.SaveLocalSettings();
    }

    private void AutoStartCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;

        var enabled = AutoStartCheck.IsChecked == true;
        if (enabled) AutoStartService.Enable(); else AutoStartService.Disable();

        AppServices.LocalSettings.AutoStartWithWindows = enabled;
        AppServices.SaveLocalSettings();
    }

    private void StartupMode_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;

        AppServices.LocalSettings.StartupMode = KioskRadio.IsChecked == true ? StartupModes.Kiosk : StartupModes.Management;
        AppServices.SaveLocalSettings();
    }

    private void AdminPassword_Click(object sender, RoutedEventArgs e)
    {
        if (!AdminAuthService.EnsureUnlocked(Window.GetWindow(this))) return;

        var dialog = new SetPasswordDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;

        AdminAuthService.SetPassword(dialog.NewPassword);
        AppMessageBox.Show(Window.GetWindow(this), "Yönetici şifresi güncellendi.", "Yönetici Şifresi");
    }

    private void BuildThemeSwatches()
    {
        ThemeSwatchPanel.Children.Clear();
        foreach (var theme in ThemeManager.Themes)
        {
            var button = new Button
            {
                Style = (Style)FindResource("ThemeSwatchStyle"),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.SwatchHex)!),
                ToolTip = theme.DisplayName,
                Tag = theme.Key == ThemeManager.CurrentThemeKey ? "Selected" : null,
            };
            button.Click += (_, _) =>
            {
                ThemeManager.Apply(theme.Key);
                AppServices.LocalSettings.ThemeKey = theme.Key;
                AppServices.SaveLocalSettings();
                foreach (var child in ThemeSwatchPanel.Children.OfType<Button>())
                    child.Tag = ReferenceEquals(child, button) ? "Selected" : null;
            };
            ThemeSwatchPanel.Children.Add(button);
        }
    }

    private sealed record MonitorItem(string DeviceName, string Label);
}
