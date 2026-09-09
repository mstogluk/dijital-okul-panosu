using System.Windows;

namespace OkulPanosu.App.Views;

public partial class SetPasswordDialog : Window
{
    public string NewPassword { get; private set; } = string.Empty;

    public SetPasswordDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NewPasswordInput.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (NewPasswordInput.Password.Length < 4)
        {
            ShowError("Şifre en az 4 karakter olmalı.");
            return;
        }

        if (NewPasswordInput.Password != ConfirmPasswordInput.Password)
        {
            ShowError("Şifreler eşleşmiyor.");
            return;
        }

        NewPassword = NewPasswordInput.Password;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
