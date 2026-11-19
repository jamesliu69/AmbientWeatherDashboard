using System.Windows;
using System.Windows.Media;
using static AmbientWeatherDashboard.Controls.DialDrawing;

namespace AmbientWeatherDashboard.Controls;

public sealed class WindCompassControl : VectorControl
{
    private static readonly Point Center = new(120, 120);
    private static readonly Pen Inner = Pen(Brush("#80BCD7"), 4);
    private static readonly Pen Tick = Pen(Brush("#4679A9"), 3.6);
    private static readonly Drawing Dial = CreateDial();
    private static readonly Geometry Pointer = Geometry("M120,29 L105,3 Q104,0 108,0 L132,0 Q136,0 135,3 Z");

    public static readonly DependencyProperty SpeedProperty = DependencyProperty.Register(
        nameof(Speed), typeof(string), typeof(WindCompassControl), Render("—"));

    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(WindCompassControl), Render("KM/H"));

    public string Speed { get => (string)GetValue(SpeedProperty); set => SetValue(SpeedProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    protected override Size DrawingSize => new(240, 240);

    protected override double Target(double value)
    {
        var delta = AmbientWeatherDashboard.Core.Models.WeatherMath.ShortestTurn(DisplayValue, value);
        return DisplayValue + delta;
    }

    protected override void Draw(DrawingContext context)
    {
        context.DrawDrawing(Dial);
        Label(context, "N", new(120, 13), 16, White, true);
        Label(context, "E", new(227, 120), 16, White, true);
        Label(context, "S", new(120, 227), 16, White, true);
        Label(context, "W", new(13, 120), 16, White, true);
        Label(context, Speed, new(120, 112), 47, White, true);
        Label(context, Unit, new(120, 154), 16, Muted, true);
        if (Value.HasValue)
        {
            context.PushTransform(new RotateTransform(DisplayValue, Center.X, Center.Y));
            context.PushTransform(new TranslateTransform(0, 20));
            context.DrawGeometry(Gold, null, Pointer);
            context.Pop();
            context.Pop();
        }
    }

    private static Drawing CreateDial()
    {
        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
        {
            context.DrawEllipse(Brush("#0A2138"), Inner, Center, 66, 66);
            for (var degree = 0; degree < 360; degree += 10)
                context.DrawLine(Tick, PointAt(Center, degree % 30 == 0 ? 83 : 86, degree), PointAt(Center, 89, degree));
        }
        drawing.Freeze();
        return drawing;
    }
}
