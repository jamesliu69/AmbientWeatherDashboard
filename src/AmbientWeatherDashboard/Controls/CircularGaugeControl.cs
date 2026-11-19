using System.Windows;
using System.Windows.Media;
using static AmbientWeatherDashboard.Controls.DialDrawing;

namespace AmbientWeatherDashboard.Controls;

public sealed class CircularGaugeControl : VectorControl
{
    private static readonly Point Center = new(100, 100);
    private static readonly Pen OuterTrack = Pen(Brush("#3A315C"), 6);
    private static readonly Pen InnerTrack = Pen(Track, 5);
    private static readonly Pen Outer = Pen(Violet, 6);
    private static readonly Pen Inner = Pen(Aqua, 5);

    public static readonly DependencyProperty ReadoutProperty = DependencyProperty.Register(
        nameof(Readout), typeof(string), typeof(CircularGaugeControl), Render("—"));

    public string Readout { get => (string)GetValue(ReadoutProperty); set => SetValue(ReadoutProperty, value); }

    protected override Size DrawingSize => new(200, 200);

    protected override double Target(double value) => Math.Clamp(value, 0, 100);

    protected override void Draw(DrawingContext context)
    {
        context.DrawEllipse(null, OuterTrack, Center, 91, 91);
        context.DrawEllipse(null, InnerTrack, Center, 72, 72);
        var sweep = AmbientWeatherDashboard.Core.Models.WeatherMath.HumiditySweep(DisplayValue);
        Arc(context, Center, 91, 0, sweep, Outer);
        Arc(context, Center, 72, 0, sweep, Inner);
        Label(context, Readout, Center, 43, White);
    }
}
