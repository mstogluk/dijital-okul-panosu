using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

/// <summary>Panonun en altında sabit duran kayan duyuru şeridi — bir modül değil, aktif şablonun
/// (TickerText/TickerSpeed/TickerBgColor/TickerTextColor) doğrudan yansıması. Hem Kiosk hem
/// Yönetim'deki önizlemede <see cref="BoardGridControl"/> tarafından kullanılır.</summary>
public partial class TickerBar : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private bool _positioned;
    private int _speedSeconds = 25;

    public TickerBar()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Advance();
        _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    public void SetTemplate(Template? template)
    {
        if (template is null || string.IsNullOrWhiteSpace(template.TickerText))
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        Visibility = Visibility.Visible;
        _speedSeconds = Math.Max(5, template.TickerSpeed);
        RootBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(template.TickerBgColor)!);
        TickerText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(template.TickerTextColor)!);
        TickerText.Text = template.TickerText;

        _positioned = false;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Canvas.SetLeft(TickerText, TickerCanvas.ActualWidth);
            _positioned = true;
        }), DispatcherPriority.Loaded);
    }

    private void TickerCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Canvas.SetLeft(TickerText, TickerCanvas.ActualWidth);
        _positioned = true;
    }

    private void Advance()
    {
        if (!_positioned || TickerText.ActualWidth <= 0 || TickerCanvas.ActualWidth <= 0) return;

        var totalDistance = TickerCanvas.ActualWidth + TickerText.ActualWidth;
        var ticksForFullLoop = _speedSeconds * 1000.0 / _timer.Interval.TotalMilliseconds;
        var pixelsPerTick = totalDistance / Math.Max(1, ticksForFullLoop);

        var left = Canvas.GetLeft(TickerText) - pixelsPerTick;
        if (left + TickerText.ActualWidth < 0)
            left = TickerCanvas.ActualWidth;

        Canvas.SetLeft(TickerText, left);
    }
}
