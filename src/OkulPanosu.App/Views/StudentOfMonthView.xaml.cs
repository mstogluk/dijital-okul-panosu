using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

/// <summary>Ayın Örnek Öğrencisi artık tek bir kayıt değil, dinamik bir liste — yılın her ayı için ayrı
/// bir öğrenci eklenebilir, panoda sadece "Yayında" işaretli olanlar sırayla gösterilir (Videolar
/// sayfasındaki IsActive deseniyle aynı).</summary>
public partial class StudentOfMonthView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private readonly List<StudentOfTheMonth> _students = new();

    public StudentOfMonthView()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        _students.Clear();
        _students.AddRange(repo.Load().StudentsOfTheMonth);
        Rebuild();
    }

    public void Reload() => Load();

    private void AddStudent_Click(object sender, RoutedEventArgs e)
    {
        _students.Add(new StudentOfTheMonth { IsActive = true });
        Rebuild();
    }

    private void Rebuild()
    {
        StudentsList.Items.Clear();

        foreach (var student in _students)
        {
            var row = new Grid { Margin = new Thickness(4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });

            var photo = new Border
            {
                Width = 110,
                Height = 110,
                CornerRadius = new CornerRadius(55),
                ClipToBounds = true,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = (Brush)Application.Current.Resources["BgElevatedBrush"],
            };
            var image = new Image { Stretch = Stretch.UniformToFill, Source = LoadPhoto(student) };
            photo.Child = image;
            row.Children.Add(photo);

            var nameBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = student.Name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Ad Soyad" };
            nameBox.LostFocus += (_, _) => student.Name = nameBox.Text;
            Grid.SetColumn(nameBox, 1);
            row.Children.Add(nameBox);

            var classBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = student.Class, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Sınıf" };
            classBox.LostFocus += (_, _) => student.Class = classBox.Text;
            Grid.SetColumn(classBox, 2);
            row.Children.Add(classBox);

            var reasonBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = student.Reason, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Seçilme Gerekçesi" };
            reasonBox.LostFocus += (_, _) => student.Reason = reasonBox.Text;
            Grid.SetColumn(reasonBox, 3);
            row.Children.Add(reasonBox);

            var quoteBox = new TextBox { Style = EditorControls.TextBoxStyle, Text = student.Quote, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Söz (opsiyonel)" };
            quoteBox.LostFocus += (_, _) => student.Quote = quoteBox.Text;
            Grid.SetColumn(quoteBox, 4);
            row.Children.Add(quoteBox);

            var activeCheck = new CheckBox { IsChecked = student.IsActive, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Yayında" };
            activeCheck.Checked += (_, _) => student.IsActive = true;
            activeCheck.Unchecked += (_, _) => student.IsActive = false;
            Grid.SetColumn(activeCheck, 5);
            row.Children.Add(activeCheck);

            var buttonsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var photoButton = new Button
            {
                Content = string.IsNullOrWhiteSpace(student.ImageFileName) ? "📷 Foto Ekle" : "📷 Foto",
                Style = EditorControls.SecondaryButton,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Fotoğraf seç",
            };
            photoButton.Click += (_, _) => PickPhoto(photoButton, student, image, photoButton);
            buttonsStack.Children.Add(photoButton);
            buttonsStack.Children.Add(EditorControls.DeleteIconButton(() => { _students.Remove(student); Rebuild(); }));
            Grid.SetColumn(buttonsStack, 6);
            row.Children.Add(buttonsStack);

            StudentsList.Items.Add(EditorControls.CardWith(row));
        }
    }

    private void PickPhoto(Button anchor, StudentOfTheMonth student, Image image, Button photoButton)
    {
        var folder = AppServices.Data?.StudentPhotosFolderPath;
        if (folder is null) return;

        var picked = EditorControls.PickAndImportFile(Window.GetWindow(anchor), folder, EditorControls.ImageExtensions, "Görsel Dosyaları");
        if (picked is null) return;

        student.ImageFileName = picked;
        image.Source = LoadPhoto(student);
        photoButton.Content = "📷 Foto";
    }

    private static BitmapSource LoadPhoto(StudentOfTheMonth student)
    {
        var folder = AppServices.Data?.StudentPhotosFolderPath;
        if (!string.IsNullOrWhiteSpace(student.ImageFileName) && folder is not null)
        {
            var fullPath = Path.Combine(folder, student.ImageFileName);
            if (File.Exists(fullPath))
                return new BitmapImage(new Uri(fullPath, UriKind.Absolute));
        }

        return PersonPlaceholder.GetSilhouette("female");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.StudentsOfTheMonth = _students);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
