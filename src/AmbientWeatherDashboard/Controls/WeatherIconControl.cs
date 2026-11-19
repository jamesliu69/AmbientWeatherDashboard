using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static AmbientWeatherDashboard.Controls.DialDrawing;

namespace AmbientWeatherDashboard.Controls;

/// <summary>依天氣代碼、日夜狀態與風況繪製天氣圖示。</summary>
public sealed class WeatherIconControl : VectorControl
{
    /// <summary>保存雲層動畫水平位移的相依屬性。</summary>
    private static readonly DependencyProperty CloudOffsetProperty = DependencyProperty.Register(
        "CloudOffset", typeof(double), typeof(WeatherIconControl), Render(0d));

    /// <summary>供繫結設定天氣代碼並更新圖示的相依屬性。</summary>
    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code), typeof(int?), typeof(WeatherIconControl), new FrameworkPropertyMetadata(
            null, FrameworkPropertyMetadataOptions.AffectsRender, UpdateCloud));

    /// <summary>供繫結設定日夜狀態並更新圖示的相依屬性。</summary>
    public static readonly DependencyProperty IsDayProperty = DependencyProperty.Register(
        nameof(IsDay), typeof(bool?), typeof(WeatherIconControl), Render());

    /// <summary>供繫結設定是否顯示風線的相依屬性。</summary>
    public static readonly DependencyProperty IsWindyProperty = DependencyProperty.Register(
        nameof(IsWindy), typeof(bool), typeof(WeatherIconControl), Render(false));

    /// <summary>表示雲層外形的共用幾何圖形。</summary>
    private static readonly Geometry CloudShape = Geometry("M31,101 C15,101 7,92 7,81 C7,68 17,59 29,58 C31,39 44,27 62,27 C79,27 92,37 97,52 C113,54 123,64 123,78 C123,92 112,101 98,101 Z");
    /// <summary>表示月牙的共用幾何圖形。</summary>
    private static readonly Geometry Crescent = Geometry("M65,14 C85,12 105,28 104,52 C103,76 86,90 65,88 C47,87 33,75 30,61 C50,74 71,66 78,48 C82,36 76,21 65,14 Z");
    /// <summary>表示雷電的共用幾何圖形。</summary>
    private static readonly Geometry Bolt = Geometry("M73,94 L61,111 H69 L64,125 L84,105 H75 L80,94 Z");
    /// <summary>表示風線的共用幾何圖形。</summary>
    private static readonly Geometry WindShape = Geometry("M101,105 H122 Q128,105 128,101 Q128,98 124,98 M95,112 H119 M106,119 H125 Q130,119 130,123 Q130,126 126,126");
    /// <summary>為雲層底部投影使用的半透明畫筆。</summary>
    private static readonly Brush CloudShadow = Brush("#50244465");
    /// <summary>為雲層外形填入的柔光漸層畫筆。</summary>
    private static readonly Brush CloudGlow = CreateGlow();
    /// <summary>非雷暴雲層使用的漸層畫筆。</summary>
    private static readonly Brush Cloud = Blend("#F2FAFF", "#81CFEE");
    /// <summary>雷暴雲層使用的漸層畫筆。</summary>
    private static readonly Brush StormCloud = Blend("#D8C9F8", "#7D83BC");
    /// <summary>太陽使用的漸層畫筆。</summary>
    private static readonly Brush Sun = Blend("#FFF5BA", "#FFAB21");
    /// <summary>月牙使用的漸層畫筆。</summary>
    private static readonly Brush Moon = Blend("#D9B4FF", "#594ACB");
    /// <summary>繪製太陽光芒的描邊。</summary>
    private static readonly Pen Ray = Pen(Gold, 3);
    /// <summary>繪製雨線的描邊。</summary>
    private static readonly Pen Rain = Pen(Ice, 3.7);
    /// <summary>繪製霧線的描邊。</summary>
    private static readonly Pen Fog = Pen(Muted, 3);
    /// <summary>繪製風線的描邊。</summary>
    private static readonly Pen Wind = Pen(Aqua, 2.7);
    /// <summary>預先繪製並重複使用的太陽圖形。</summary>
    private static readonly Drawing Sunshine = CreateSun();

    /// <summary>在可見或載入狀態改變時更新雲層動畫，卸載時清除動畫。</summary>
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

    /// <summary>設定天氣圖示使用的動畫預設值及代碼變更回呼。</summary>
    static WeatherIconControl()
    {
        AnimateProperty.OverrideMetadata(typeof(WeatherIconControl), new FrameworkPropertyMetadata(false, UpdateCloud));
    }

    /// <summary>天氣代碼變更時轉交給控制項更新雲層動畫。</summary>
    private static void UpdateCloud(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((WeatherIconControl)sender).UpdateCloudAnimation();

    /// <summary>只在動畫已啟用且圖示可見、已載入並有雲層時播放雲層位移。</summary>
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

    /// <summary>依天氣代碼和日夜狀態繪製雲、日月、降水及風線。</summary>
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

    /// <summary>在風況標記為有風時繪製風線。</summary>
    private void DrawWind(DrawingContext context)
    {
        if (IsWindy)
            context.DrawGeometry(null, Wind, WindShape);
    }

    /// <summary>建立並凍結雲層填色使用的放射狀柔光畫筆。</summary>
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

    /// <summary>依兩個色碼建立並凍結線性漸層畫筆。</summary>
    private static Brush Blend(string top, string bottom)
    {
        var brush = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(top),
            (Color)ColorConverter.ConvertFromString(bottom), 55);
        brush.Freeze();
        return brush;
    }

    /// <summary>建立並凍結包含太陽圓盤和光芒的共用圖形。</summary>
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
