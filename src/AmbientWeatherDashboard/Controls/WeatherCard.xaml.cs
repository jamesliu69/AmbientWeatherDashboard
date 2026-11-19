using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AmbientWeatherDashboard.Controls;

[ContentProperty(nameof(Body))]
public partial class WeatherCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(WeatherCard));

    public static readonly DependencyProperty BodyProperty =
        DependencyProperty.Register(nameof(Body), typeof(object), typeof(WeatherCard));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(Geometry), typeof(WeatherCard));

    public static readonly DependencyProperty DetailProperty =
        DependencyProperty.Register(nameof(Detail), typeof(string), typeof(WeatherCard));

    public static readonly DependencyProperty AnimateProperty =
        DependencyProperty.Register(nameof(Animate), typeof(bool), typeof(WeatherCard),
            new PropertyMetadata(false, (sender, args) =>
            {
                if (args.NewValue is false)
                    ((WeatherCard)sender).BeginAnimation(OpacityProperty, null);
            }));

    public bool Animate { get => (bool)GetValue(AnimateProperty); set => SetValue(AnimateProperty, value); }
    public string Detail { get => (string)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object Body { get => GetValue(BodyProperty); set => SetValue(BodyProperty, value); }
    public Geometry Glyph { get => (Geometry)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

    public WeatherCard()
    {
        InitializeComponent();
        Loaded += (_, _) => FadeIn();
        IsVisibleChanged += (_, _) =>
        {
            if (IsLoaded)
                FadeIn();
        };
    }

    private void FadeIn()
    {
        if (IsVisible && Animate)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                FillBehavior = FillBehavior.Stop
            });
        }
        else
            BeginAnimation(OpacityProperty, null);
    }

    private void OpenMenu(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }

    private void ShowDetails(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is { } window)
            MessageBox.Show(window, Detail, Title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
