using System.Windows;
using System.Windows.Media;
using static AmbientWeatherDashboard.Controls.DialDrawing;

namespace AmbientWeatherDashboard.Controls;

public sealed class SunCycleControl : VectorControl
{
    private static readonly Point Center = new(132, 114);
    private static readonly Pen Outside = Pen(Track, 10);
    private static readonly Pen Inside = Pen(Brush("#26496B"), 6);
    private static readonly Pen Day = Pen(Gold, 11);
    private static readonly Pen Progress = Pen(Ice, 6);

    public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
        nameof(StartAngle), typeof(double), typeof(SunCycleControl), Render(0d));

    public static readonly DependencyProperty DaySweepProperty = DependencyProperty.Register(
        nameof(DaySweep), typeof(double), typeof(SunCycleControl), Render(0d));

    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration), typeof(string), typeof(SunCycleControl), Render("—"));

    public static readonly DependencyProperty ChineseLabelsProperty = DependencyProperty.Register(
        nameof(ChineseLabels), typeof(bool), typeof(SunCycleControl), Render(false));

    public bool ChineseLabels { get => (bool)GetValue(ChineseLabelsProperty); set => SetValue(ChineseLabelsProperty, value); }
    public double StartAngle { get => (double)GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }
    public double DaySweep { get => (double)GetValue(DaySweepProperty); set => SetValue(DaySweepProperty, value); }
    public string Duration { get => (string)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    protected override Size DrawingSize => new(264, 228);

    protected override double Target(double value) => Math.Clamp(value, 0, 1);

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
