using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Sınıf sıralamasındaki her kaydı, sırayla, tek tek büyük gösterir (birden fazla sınıf olabileceği için).</summary>
public partial class CleanestClassModuleView : UserControl
{
    private const int DefaultSeconds = 6;

    private readonly DispatcherTimer _timer = new();
    private List<CleanestClassEntry> _entries = [];
    private int _index;

    public CleanestClassModuleView(BoardModule module)
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
        _entries = (AppServices.Data?.Load().CleanestClasses ?? []).OrderByDescending(e => e.Score).ToList();

        if (_entries.Count == 0)
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Bu hafta için sınıf sıralaması tanımlanmamış";
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
        if (_entries.Count == 0) return;
        _index = (_index + 1) % _entries.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var entry = _entries[_index];
        RankText.Text = $"{_index + 1}. SIRA";
        ClassNameText.Text = entry.ClassName;
        ScoreText.Text = $"{entry.Score} puan";
        AwardText.Text = entry.Award;
        AwardText.Visibility = string.IsNullOrWhiteSpace(entry.Award) ? Visibility.Collapsed : Visibility.Visible;
    }
}
