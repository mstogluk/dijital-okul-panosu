using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Yayında (IsActive) işaretli öğrencileri sırayla gösterir — tek kişi varsa sabit kalır, birden
/// fazlaysa Nöbetçi Öğretmen modülündeki gibi birkaç saniyede bir sıradakine geçer.</summary>
public partial class StudentOfMonthModuleView : UserControl
{
    private const int DefaultSeconds = 8;

    private readonly DispatcherTimer _timer = new();
    private List<StudentOfTheMonth> _students = [];
    private int _index;

    public StudentOfMonthModuleView(BoardModule module)
    {
        InitializeComponent();

        var seconds = module.Settings.TryGetValue("transitionSeconds", out var raw) && int.TryParse(raw, out var s) && s > 0
            ? s
            : DefaultSeconds;
        _timer.Interval = TimeSpan.FromSeconds(seconds);

        Load();

        _timer.Tick += (_, _) => Advance();
        _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Load()
    {
        _students = (AppServices.Data?.Load().StudentsOfTheMonth ?? [])
            .Where(s => s.IsActive && !string.IsNullOrWhiteSpace(s.Name))
            .ToList();

        if (_students.Count == 0)
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Ayın öğrencisi tanımlanmamış";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        ContentPanel.Visibility = Visibility.Visible;
        EmptyText.Visibility = Visibility.Collapsed;
        _index = 0;
        ShowCurrent();
    }

    private void Advance()
    {
        if (_students.Count == 0) return;
        _index = (_index + 1) % _students.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var student = _students[_index];

        NameText.Text = student.Name;
        ClassText.Text = student.Class;
        ReasonText.Text = student.Reason;

        if (!string.IsNullOrWhiteSpace(student.Quote))
        {
            QuoteText.Text = $"“{student.Quote}”";
            QuoteText.Visibility = Visibility.Visible;
        }
        else
        {
            QuoteText.Visibility = Visibility.Collapsed;
        }

        var photoPath = !string.IsNullOrWhiteSpace(student.ImageFileName) && AppServices.Data is { } repo
            ? Path.Combine(repo.StudentPhotosFolderPath, student.ImageFileName)
            : null;
        PhotoImage.Source = photoPath is not null && File.Exists(photoPath)
            ? new BitmapImage(new Uri(photoPath, UriKind.Absolute))
            : PersonPlaceholder.GetSilhouette("female");
    }
}
