using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AmbientWeatherDashboard.Controls;

/// <summary>以標題、圖示、內容和詳細說明組成可選擇淡入的天氣卡片。</summary>
[ContentProperty(nameof(Body))]
public partial class WeatherCard : UserControl
{
    /// <summary>供繫結設定卡片標題的相依屬性。</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(WeatherCard));

    /// <summary>供內容屬性語法設定卡片主體的相依屬性。</summary>
    public static readonly DependencyProperty BodyProperty =
        DependencyProperty.Register(nameof(Body), typeof(object), typeof(WeatherCard));

    /// <summary>供繫結設定卡片標題圖示的相依屬性。</summary>
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(Geometry), typeof(WeatherCard));

    /// <summary>供詳細說明對話框顯示內容的相依屬性。</summary>
    public static readonly DependencyProperty DetailProperty =
        DependencyProperty.Register(nameof(Detail), typeof(string), typeof(WeatherCard));

    /// <summary>控制卡片淡入動畫的相依屬性，停用時會清除透明度動畫。</summary>
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

    /// <summary>載入卡片並在載入或重新顯示時更新淡入效果。</summary>
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

    /// <summary>依可見狀態與動畫設定淡入卡片，否則清除透明度動畫。</summary>
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

    /// <summary>將卡片內容的快顯選單錨定至觸發按鈕並開啟。</summary>
    private void OpenMenu(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }

    /// <summary>以卡片標題和詳細說明開啟資訊對話框。</summary>
    private void ShowDetails(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is { } window)
            MessageBox.Show(window, Detail, Title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
