using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AmbientWeatherDashboard.Controls;

/// <summary>提供固定繪圖座標縮放、數值動畫與文字繪製功能的向量控制項基底。</summary>
public abstract class VectorControl : FrameworkElement
{
    /// <summary>保存供繪圖使用且可動畫更新的目前數值。</summary>
    private static readonly DependencyProperty DisplayValueProperty = DependencyProperty.Register(
        nameof(DisplayValue), typeof(double), typeof(VectorControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>供繫結設定控制項輸入數值並觸發顯示更新的相依屬性。</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double?), typeof(VectorControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, AnimateValue));

    /// <summary>供繫結控制數值變更時是否播放動畫的相依屬性。</summary>
    public static readonly DependencyProperty AnimateProperty = DependencyProperty.Register(
        nameof(Animate), typeof(bool), typeof(VectorControl),
        new FrameworkPropertyMetadata(false, AnimateValue));

    protected double DisplayValue => (double)GetValue(DisplayValueProperty);
    public double? Value { get => (double?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public bool Animate { get => (bool)GetValue(AnimateProperty); set => SetValue(AnimateProperty, value); }

    /// <summary>建立此向量控制項的自動化影像節點。</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new VectorAutomationPeer(this);

    /// <summary>將輸入值轉換為繪圖使用的目標值，子類別可覆寫此轉換。</summary>
    protected virtual double Target(double value) => value;

    /// <summary>在輸入值或動畫設定變更時更新顯示值，並視設定播放過渡動畫。</summary>
    private static void AnimateValue(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (VectorControl)sender;
        var from = control.DisplayValue;
        var to = control.Value is { } value && double.IsFinite(value) ? control.Target(value) : 0;
        control.BeginAnimation(DisplayValueProperty, null);
        control.SetValue(DisplayValueProperty, to);
        if (control.Animate && control.Value.HasValue && Math.Abs(from - to) > 0.001)
        {
            control.BeginAnimation(DisplayValueProperty, new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(420))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            });
        }
    }

    /// <summary>建立變更時會要求控制項重新繪製的相依屬性中繼資料。</summary>
    protected static FrameworkPropertyMetadata Render(object? value = null) =>
        new(value, FrameworkPropertyMetadataOptions.AffectsRender);

    /// <summary>依控制項的繪圖座標尺寸回報設計尺寸。</summary>
    protected abstract Size DrawingSize { get; }

    /// <summary>將設計尺寸等比例置中縮放至實際控制項大小後呼叫繪圖實作。</summary>
    protected sealed override void OnRender(DrawingContext context)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
            return;

        var size = DrawingSize;
        var scale = Math.Min(ActualWidth / size.Width, ActualHeight / size.Height);
        context.PushTransform(new TranslateTransform((ActualWidth - size.Width * scale) / 2, (ActualHeight - size.Height * scale) / 2));
        context.PushTransform(new ScaleTransform(scale, scale));
        Draw(context);
        context.Pop();
        context.Pop();
    }

    /// <summary>由子類別實作控制項的具體圖形繪製。</summary>
    protected abstract void Draw(DrawingContext context);

    /// <summary>以目前 UI 語系建立置中的文字並繪製於指定座標。</summary>
    protected void Label(DrawingContext context, string text, Point center, double size, Brush color, bool bold = false)
    {
        var formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyles.Normal,
                bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal), size, color,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
    }

    /// <summary>供向量儀表文字共用的字型設定。</summary>
    private static readonly FontFamily FontFamily = new("Segoe UI, Microsoft JhengHei");
}
