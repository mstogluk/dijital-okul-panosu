using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class CleanestClassView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private readonly List<CleanestClassEntry> _entries = new();

    public CleanestClassView()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        _entries.Clear();
        _entries.AddRange(repo.Load().CleanestClasses);
        Rebuild();
    }

    public void Reload() => Load();

    private void Rebuild()
    {
        EntriesList.Items.Clear();
        var rank = 0;
        foreach (var entry in _entries.OrderByDescending(e => e.Score))
        {
            rank++;
            var panel = new StackPanel();

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Children.Add(new TextBlock
            {
                Text = $"🏆 {rank}. TEMİZ SINIF",
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["WarningBrush"],
                VerticalAlignment = VerticalAlignment.Center,
            });
            var delete = EditorControls.DeleteIconButton(() => { _entries.Remove(entry); Rebuild(); });
            Grid.SetColumn(delete, 1);
            header.Children.Add(delete);
            panel.Children.Add(header);

            panel.Children.Add(EditorControls.LabeledTextBox("Sınıf Şubesi", entry.ClassName, v => entry.ClassName = v));
            panel.Children.Add(EditorControls.LabeledTextBox("Değerlendirme Puanı", entry.Score.ToString(Turkish),
                v => { entry.Score = int.TryParse(v, out var s) ? s : entry.Score; Rebuild(); }));
            panel.Children.Add(EditorControls.LabeledTextBox("Ödül / Ünvan", entry.Award, v => entry.Award = v));

            EntriesList.Items.Add(EditorControls.CardWith(panel, new Thickness(0, 0, 10, 10)));
        }
    }

    private void AddEntry_Click(object sender, RoutedEventArgs e)
    {
        _entries.Add(new CleanestClassEntry());
        Rebuild();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.CleanestClasses = _entries);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
