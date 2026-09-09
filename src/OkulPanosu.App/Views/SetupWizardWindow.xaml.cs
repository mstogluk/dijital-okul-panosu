using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class SetupWizardWindow : Window
{
    public SetupWizardWindow()
    {
        InitializeComponent();
        FolderText.Text = Path.Combine(AppContext.BaseDirectory, "YAYIN");
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Veri Klasörünü Seçin" };
        if (dialog.ShowDialog(this) == true)
        {
            FolderText.Text = dialog.FolderName;
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FolderText.Text))
        {
            ErrorText.Text = "Devam etmek için önce bir veri klasörü seçmelisiniz.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            AppServices.SetDataFolder(FolderText.Text);
        }
        catch (Exception ex) when (AppServices.IsFolderUnreachableError(ex))
        {
            ErrorText.Text = AppServices.DescribeFolderUnreachable(FolderText.Text);
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        AppServices.LocalSettings.StartupMode = KioskRadio.IsChecked == true ? StartupModes.Kiosk : StartupModes.Management;
        AppServices.SaveLocalSettings();

        DialogResult = true;
    }
}
