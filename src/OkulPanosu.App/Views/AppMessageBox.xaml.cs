using System.Windows;

namespace OkulPanosu.App.Views;

/// <summary>Native <c>System.Windows.MessageBox</c> her zaman Windows'un açık/klasik temasıyla açılır ve
/// uygulamanın koyu temasına hiç uymaz — bu yüzden bilgi/onay pencereleri için bunun yerine kullanılır.</summary>
public partial class AppMessageBox : Window
{
    private bool _result;

    private AppMessageBox(Window? owner, string message, string title)
    {
        InitializeComponent();
        Owner = owner;
        // XAML'de WindowStartupLocation="CenterOwner" sabit — Owner NULL olduğunda (ör. uygulama başlarken,
        // henüz hiçbir pencere açılmadan önce gösterilen "Veri Klasörüne Ulaşılamıyor" diyaloğu) WPF bunu
        // InvalidOperationException ile reddediyor; bu istisna zaten bir hata işleme sırasında (üstelik
        // genel DispatcherUnhandledException yakalayıcısının KENDİSİ de aynı hatayla AYNI şekilde
        // AppMessageBox.Show(null, ...) çağırdığı için) art arda tetiklenip uygulamayı hiçbir iz
        // bırakmadan (crash.log'a bile yazamadan) sessizce sonlandırıyordu. Owner yoksa ekranı ortala.
        if (owner is null) WindowStartupLocation = WindowStartupLocation.CenterScreen;
        TitleText.Text = title;
        MessageText.Text = message;
    }

    public static void Show(Window? owner, string message, string title)
    {
        var dialog = new AppMessageBox(owner, message, title);
        dialog.ShowDialog();
    }

    public static bool Confirm(Window? owner, string message, string title)
    {
        var dialog = new AppMessageBox(owner, message, title);
        dialog.NoButton.Visibility = Visibility.Visible;
        dialog.YesButton.Content = "Evet";
        dialog.ShowDialog();
        return dialog._result;
    }

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
        _result = true;
        Close();
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        _result = false;
        Close();
    }
}
