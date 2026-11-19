using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static AmbientWeatherDashboard.Controls.DialDrawing;

namespace AmbientWeatherDashboard.Controls;

public sealed class WeatherIconControl : VectorControl
{
    private static readonly DependencyProperty CloudOffsetProperty = DependencyProperty.Register(
        "CloudOffset", typeof(double), typeof(WeatherIconControl), Render(0d));

    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code), typeof(int?), typeof(WeatherIconControl), new FrameworkPropertyMetadata(
            null, FrameworkPropertyMetadataOptions.AffectsRender, UpdateCloud));

    public static readonly DependencyProperty IsDayProperty = DependencyProperty.Register(
        nameof(IsDay), typeof(bool?), typeof(WeatherIconControl), Render());

    public static readonly DependencyProperty IsWindyProperty = DependencyProperty.Register(
        nameof(IsWindy), typeof(bool), typeof(WeatherIconControl), Render(false));

    private static readonly Geometry CloudShape = Geometry("M31,101 C15,101 7,92 7,81 C7,68 17,59 29,58 C31,39 44,27 62,27 C79,27 92,37 97,52 C113,54 123,64 123,78 C123,92 112,101 98,101 Z");
    private static readonly Geometry Crescent = Geometry("M65,14 C85,12 105,28 104,52 C103,76 86,90 65,88 C47,87 33,75 30,61 C50,74 71,66 78,48 C82,36 76,21 65,14 Z");
    private static readonly Geometry Bolt = Geometry("M73,94 L61,111 H69 L64,125 L84,105 H75 L80,94 Z");
    private static readonly Geometry WindShape = Geometry("M101,105 H122 Q128,105 128,101 Q128,98 124,98 M95,112 H119 M106,119 H125 Q130,119 130,123 Q130,126 126,126");
    private static readonly Brush CloudShadow = Brush("#50244465");
    private static readonly Brush CloudGlow = CreateGlow();
    private static readonly Brush Cloud = Blend("#F2FAFF", "#81CFEE");
    private static readonly Brush StormCloud = Blend("#D8C9F8", "#7D83BC");
    private static readonly Brush Sun = Blend("#FFF5BA", "#FFAB21");
    private static readonly Brush Moon = Blend("#D9B4FF", "#594ACB");
    private static readonly Pen Ray = Pen(Gold, 3);
    private static readonly Pen Rain = Pen(Ice, 3.7);
    private static readonly Pen Fog = Pen(Muted, 3);
    private static readonly Pen Wind = Pen(Aqua, 2.7);
    private static readonly Drawing Sunshine = CreateSun();

    public WeatherIconControl()
    {
        IsVisibleChanged += (_, _) => UpdateCloudAnimation();
        Loaded += (_, _) => UpdateCloudAnimation();
        Unloaded += (_, _) => BeginAnimation(CloudOffsetProperty, null);
    }

    public int? Code { get => (int?)GetValue(CodeProperty); set => SetValue(CodeProperty, value); }
    public bool? IsDay { get => (bool?)GetValue(IsDayProperty); set => SetValue(IsDayProperty, value); }

    public bool IsWindy { get => (bool)GetValue(IsWindyProperty); set => SetValue(IsWindyProperty, value); }

    protected override Size DrawingSize => new(132, 130);

    static WeatherIconControl()
    {
        AnimateProperty.OverrideMetadata(typeof(WeatherIconControl), new FrameworkPropertyMetadata(false, UpdateCloud));
    }

    private static void UpdateCloud(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((WeatherIconControl)sender).UpdateCloudAnimation();

    private void UpdateCloudAnimation()
    {
        if (!Animate || !IsVisible || !IsLoaded || Code is null or 0)
        {
            BeginAnimation(CloudOffsetProperty, null);
            return;
        }

        BeginAnimation(CloudOffsetProperty, new DoubleAnimation(-2, 2, TimeSpan.FromSeconds(4))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
    }

    protected override void Draw(DrawingContext context)
    {
        if (Code is not { } code)
        {
            Label(context, "—", new Point(66, 65), 29, Muted);
            DrawWind(context);
            return;
        }

        if (code <= 2)
        {
            if (IsDay is { } day)
            {
                if (day)
                    context.DrawDrawing(Sunshine);
                else
                {
                    context.DrawGeometry(Moon, null, Crescent);
                    context.DrawEllipse(White, null, new Point(45, 24), 2, 2);
                    context.DrawEllipse(Ice, null, new Point(34, 40), 1.5, 1.5);
                }
            }

            if (code == 0)
            {
                if (IsDay is null)
                    context.DrawEllipse(null, Fog, new Point(66, 59), 21, 21);
                DrawWind(context);
                return;
            }
        }

        context.PushTransform(new TranslateTransform((double)GetValue(CloudOffsetProperty), 0));
        if (code <= 2 && IsDay.HasValue)
            context.PushTransform(new MatrixTransform(0.72, 0, 0, 0.72, 29, 32));
        context.DrawEllipse(CloudShadow, null, new Point(68, 98), 47, 7);
        context.DrawGeometry(code >= 95 ? StormCloud : Cloud, null, CloudShape);
        context.DrawGeometry(CloudGlow, null, CloudShape);
        if (code <= 2 && IsDay.HasValue)
            context.Pop();
        context.Pop();

        if (code is 45 or 48)
        {
            context.DrawLine(Fog, new(24, 111), new(103, 111));
            context.DrawLine(Fog, new(40, 120), new(90, 120));
        }
        else if (code is >= 71 and <= 77 or 85 or 86)
        {
            for (var index = 0; index < 3; index++)
                context.DrawEllipse(White, null, new Point(42 + 24 * index, 113 + index % 2 * 6), 3.5, 3.5);
        }
        else if (code >= 51)
        {
            context.DrawLine(Rain, new(45, 106), new(39, 117));
            context.DrawLine(Rain, new(93, 106), new(87, 117));
            if (code >= 95)
                context.DrawGeometry(Gold, null, Bolt);
            else
                context.DrawLine(Rain, new(70, 112), new(64, 123));
        }

        DrawWind(context);
    }

    private void DrawWind(DrawingContext context)
    {
        if (IsWindy)
            context.DrawGeometry(null, Wind, WindShape);
    }

    private static Brush CreateGlow()
    {
        var brush = new RadialGradientBrush(Color.FromArgb(110, 255, 255, 255), Colors.Transparent)
        {
            Center = new Point(0.26, 0.32),
            GradientOrigin = new Point(0.26, 0.32),
            RadiusX = 0.43,
            RadiusY = 0.56
        };
        brush.Freeze();
        return brush;
    }

    private static Brush Blend(string top, string bottom)
    {
        var brush = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(top),
            (Color)ColorConverter.ConvertFromString(bottom), 55);
        brush.Freeze();
        return brush;
    }

    private static Drawing CreateSun()
    {
        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
        {
            var center = new Point(65, 54);
            context.DrawEllipse(Sun, null, center, 27, 27);
            for (var degree = 0; degree < 360; degree += 45)
                context.DrawLine(Ray, PointAt(center, 34, degree), PointAt(center, 42, degree));
        }
        drawing.Freeze();
        return drawing;
    }
}
