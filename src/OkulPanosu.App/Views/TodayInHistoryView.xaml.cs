using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class TodayInHistoryView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private sealed record MonthChoice(int? Month, string Label);

    private static readonly MonthChoice[] MonthChoices =
    [
        new(null, "Genel (Her Zaman)"),
        .. Enumerable.Range(1, 12).Select(m => new MonthChoice(m, TurkishCalendar.MonthNames[m - 1])),
    ];

    private readonly List<TodayInHistoryEntry> _entries = new();

    public TodayInHistoryView()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        _entries.Clear();
        _entries.AddRange(repo.Load().TodayInHistory);
        Rebuild();
    }

    public void Reload() => Load();

    private void Rebuild()
    {
        EntriesList.Items.Clear();
        foreach (var entry in _entries)
        {
            var panel = new StackPanel();

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Children.Add(new TextBlock { Text = "Tarih Kartı", Style = EditorControls.Heading, VerticalAlignment = VerticalAlignment.Center });
            var delete = EditorControls.DeleteIconButton(() => { _entries.Remove(entry); Rebuild(); });
            Grid.SetColumn(delete, 1);
            header.Children.Add(delete);
            panel.Children.Add(header);

            var dateRow = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            dateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            dateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            dateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            dateRow.Children.Add(EditorControls.LabeledDayTextBox("Gün", entry.Day, v => entry.Day = v, 0));

            var monthCombo = EditorControls.LabeledComboColumn("Ay", MonthChoices, "Label", "Month",
                entry.IsGeneral ? null : entry.Month,
                v =>
                {
                    if (v is int m) { entry.Month = m; entry.IsGeneral = false; }
                    else { entry.IsGeneral = true; }
                }, 0);
            Grid.SetColumn(monthCombo, 2);
            dateRow.Children.Add(monthCombo);

            panel.Children.Add(dateRow);
            panel.Children.Add(EditorControls.LabeledMultilineTextBox("Olaylar (her satır bir olay)", string.Join('\n', entry.Events),
                v => entry.Events = v.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(), 90));

            EntriesList.Items.Add(EditorControls.CardWith(panel));
        }
    }

    private void AddEntry_Click(object sender, RoutedEventArgs e)
    {
        _entries.Add(new TodayInHistoryEntry());
        Rebuild();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.TodayInHistory = _entries);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
