using System.Windows;
using System.Windows.Input;

namespace OkulPanosu.App.Views;

public partial class AdminPasswordDialog : Window
{
    public string EnteredPassword { get; private set; } = string.Empty;

    public AdminPasswordDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordInput.Focus();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        EnteredPassword = PasswordInput.Password;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Confirm_Click(sender, e);
    }
}
