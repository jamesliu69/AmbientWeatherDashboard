using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AmbientWeatherDashboard.Controls;

public abstract class VectorControl : FrameworkElement
{
    private static readonly DependencyProperty DisplayValueProperty = DependencyProperty.Register(
        nameof(DisplayValue), typeof(double), typeof(VectorControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double?), typeof(VectorControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, AnimateValue));

    public static readonly DependencyProperty AnimateProperty = DependencyProperty.Register(
        nameof(Animate), typeof(bool), typeof(VectorControl),
        new FrameworkPropertyMetadata(false, AnimateValue));

    protected double DisplayValue => (double)GetValue(DisplayValueProperty);
    public double? Value { get => (double?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public bool Animate { get => (bool)GetValue(AnimateProperty); set => SetValue(AnimateProperty, value); }

    protected override AutomationPeer OnCreateAutomationPeer() => new VectorAutomationPeer(this);

    protected virtual double Target(double value) => value;

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

    protected static FrameworkPropertyMetadata Render(object? value = null) =>
        new(value, FrameworkPropertyMetadataOptions.AffectsRender);

    protected abstract Size DrawingSize { get; }

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

    protected abstract void Draw(DrawingContext context);

    protected void Label(DrawingContext context, string text, Point center, double size, Brush color, bool bold = false)
    {
        var formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyles.Normal,
                bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal), size, color,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
    }

    private static readonly FontFamily FontFamily = new("Segoe UI, Microsoft JhengHei");
}
