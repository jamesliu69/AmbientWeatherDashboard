using System.Windows;
using System.Windows.Media;
using static AmbientWeatherDashboard.Controls.DialDrawing;

namespace AmbientWeatherDashboard.Controls;

/// <summary>以雙層圓弧顯示百分比數值及其讀值文字。</summary>
public sealed class CircularGaugeControl : VectorControl
{
    /// <summary>百分比圓弧的繪圖中心座標。</summary>
    private static readonly Point Center = new(100, 100);
    /// <summary>外側百分比進度軌道的描邊。</summary>
    private static readonly Pen OuterTrack = Pen(Brush("#3A315C"), 6);
    /// <summary>內側百分比進度軌道的描邊。</summary>
    private static readonly Pen InnerTrack = Pen(Track, 5);
    /// <summary>外側百分比進度弧的描邊。</summary>
    private static readonly Pen Outer = Pen(Violet, 6);
    /// <summary>內側百分比進度弧的描邊。</summary>
    private static readonly Pen Inner = Pen(Aqua, 5);

    /// <summary>供繫結設定圓心讀值文字的相依屬性。</summary>
    public static readonly DependencyProperty ReadoutProperty = DependencyProperty.Register(
        nameof(Readout), typeof(string), typeof(CircularGaugeControl), Render("—"));

    public string Readout { get => (string)GetValue(ReadoutProperty); set => SetValue(ReadoutProperty, value); }

    protected override Size DrawingSize => new(200, 200);

    /// <summary>將輸入百分比限制在零到一百之間。</summary>
    protected override double Target(double value) => Math.Clamp(value, 0, 100);

    /// <summary>繪製百分比軌道、進度弧和中心讀值。</summary>
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
