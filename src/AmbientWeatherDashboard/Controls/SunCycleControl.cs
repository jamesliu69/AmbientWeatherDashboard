using System.Windows;
using System.Windows.Media;
using static AmbientWeatherDashboard.Controls.DialDrawing;

namespace AmbientWeatherDashboard.Controls;

/// <summary>以圓弧顯示日照區間和目前進度，並標示四個時刻方向。</summary>
public sealed class SunCycleControl : VectorControl
{
    /// <summary>日照圓弧與文字標籤共用的中心座標。</summary>
    private static readonly Point Center = new(132, 114);
    /// <summary>日照時間外側背景圓環的描邊。</summary>
    private static readonly Pen Outside = Pen(Track, 10);
    /// <summary>目前進度內側背景圓環的描邊。</summary>
    private static readonly Pen Inside = Pen(Brush("#26496B"), 6);
    /// <summary>代表完整日照區間的金色描邊。</summary>
    private static readonly Pen Day = Pen(Gold, 11);
    /// <summary>代表日照區間中已經過進度的描邊。</summary>
    private static readonly Pen Progress = Pen(Ice, 6);

    /// <summary>供繫結設定日照圓弧起始角度的相依屬性。</summary>
    public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
        nameof(StartAngle), typeof(double), typeof(SunCycleControl), Render(0d));

    /// <summary>供繫結設定日照圓弧角度範圍的相依屬性。</summary>
    public static readonly DependencyProperty DaySweepProperty = DependencyProperty.Register(
        nameof(DaySweep), typeof(double), typeof(SunCycleControl), Render(0d));

    /// <summary>供繫結設定圓心日照時長文字的相依屬性。</summary>
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration), typeof(string), typeof(SunCycleControl), Render("—"));

    /// <summary>供繫結切換圓周時刻標籤語言的相依屬性。</summary>
    public static readonly DependencyProperty ChineseLabelsProperty = DependencyProperty.Register(
        nameof(ChineseLabels), typeof(bool), typeof(SunCycleControl), Render(false));

    public bool ChineseLabels { get => (bool)GetValue(ChineseLabelsProperty); set => SetValue(ChineseLabelsProperty, value); }
    public double StartAngle { get => (double)GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }
    public double DaySweep { get => (double)GetValue(DaySweepProperty); set => SetValue(DaySweepProperty, value); }
    public string Duration { get => (string)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    protected override Size DrawingSize => new(264, 228);

    /// <summary>將進度限制在零到一之間。</summary>
    protected override double Target(double value) => Math.Clamp(value, 0, 1);

    /// <summary>繪製日照區間、其經過進度、時刻標籤及日照時長。</summary>
    protected override void Draw(DrawingContext context)
    {
        context.DrawEllipse(null, Outside, Center, 85, 85);
        context.DrawEllipse(null, Inside, Center, 66, 66);
        Arc(context, Center, 85, StartAngle, DaySweep, Day);
        Arc(context, Center, 66, StartAngle, DaySweep * DisplayValue, Progress);
        Label(context, ChineseLabels ? "中午 12" : "12 PM", new(132, 11), 12.5, White, true);
        Label(context, ChineseLabels ? "下午 6" : "6 PM", new(242, 114), 11.5, White, true);
        Label(context, ChineseLabels ? "午夜 12" : "12 AM", new(132, 217), 12.5, White, true);
        Label(context, ChineseLabels ? "早上 6" : "6 AM", new(22, 114), 11.5, White, true);
        Label(context, Duration, Center, 15, Muted, true);
    }
}
