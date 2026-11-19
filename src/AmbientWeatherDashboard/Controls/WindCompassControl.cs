using System.Windows;
using System.Windows.Media;
using static AmbientWeatherDashboard.Controls.DialDrawing;

namespace AmbientWeatherDashboard.Controls;

/// <summary>以羅盤刻度和指針呈現風向，並在中心顯示風速與單位。</summary>
public sealed class WindCompassControl : VectorControl
{
    /// <summary>羅盤刻度盤的中心座標。</summary>
    private static readonly Point Center = new(120, 120);
    /// <summary>羅盤內圈的描邊。</summary>
    private static readonly Pen Inner = Pen(Brush("#80BCD7"), 4);
    /// <summary>羅盤周圍刻度線的描邊。</summary>
    private static readonly Pen Tick = Pen(Brush("#4679A9"), 3.6);
    /// <summary>預先繪製的羅盤圓環與刻度。</summary>
    private static readonly Drawing Dial = CreateDial();
    /// <summary>表示目前風向的指針外形。</summary>
    private static readonly Geometry Pointer = Geometry("M120,29 L105,3 Q104,0 108,0 L132,0 Q136,0 135,3 Z");

    /// <summary>供繫結設定羅盤中心風速文字的相依屬性。</summary>
    public static readonly DependencyProperty SpeedProperty = DependencyProperty.Register(
        nameof(Speed), typeof(string), typeof(WindCompassControl), Render("—"));

    /// <summary>供繫結設定風速單位文字的相依屬性。</summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(WindCompassControl), Render("KM/H"));

    public string Speed { get => (string)GetValue(SpeedProperty); set => SetValue(SpeedProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    protected override Size DrawingSize => new(240, 240);

    /// <summary>讓指針沿角度差最短的方向轉到新的風向。</summary>
    protected override double Target(double value)
    {
        var delta = AmbientWeatherDashboard.Core.Models.WeatherMath.ShortestTurn(DisplayValue, value);
        return DisplayValue + delta;
    }

    /// <summary>繪製羅盤、風速文字，以及目前風向指針。</summary>
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

    /// <summary>建立並凍結包含圓環和方向刻度的羅盤底圖。</summary>
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
