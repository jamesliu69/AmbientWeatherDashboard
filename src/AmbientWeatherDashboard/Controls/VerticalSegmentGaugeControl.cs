using System.Windows;
using System.Windows.Media;

namespace AmbientWeatherDashboard.Controls;

/// <summary>以垂直圓角分段及透明度呈現指定範圍內的數值。</summary>
public sealed class VerticalSegmentGaugeControl : VectorControl
{
    /// <summary>供繫結設定儀表數值下限的相依屬性。</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(VerticalSegmentGaugeControl), Render(0d));

    /// <summary>供繫結設定儀表數值上限的相依屬性。</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(VerticalSegmentGaugeControl), Render(100d));

    /// <summary>供繫結設定儀表分段數量的相依屬性。</summary>
    public static readonly DependencyProperty SegmentCountProperty = DependencyProperty.Register(
        nameof(SegmentCount), typeof(int), typeof(VerticalSegmentGaugeControl), Render(9));

    /// <summary>供繫結設定各分段強調色的相依屬性。</summary>
    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(VerticalSegmentGaugeControl), Render(DialDrawing.Gold));

    /// <summary>供自動化說明數值單位的相依屬性。</summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(VerticalSegmentGaugeControl), Render(string.Empty));

    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public int SegmentCount { get => (int)GetValue(SegmentCountProperty); set => SetValue(SegmentCountProperty, value); }
    public Brush Accent { get => (Brush)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    protected override Size DrawingSize => new(50, 130);

    /// <summary>依數值在上下限間的位置繪製填入程度不同的分段。</summary>
    protected override void Draw(DrawingContext context)
    {
        var count = Math.Clamp(SegmentCount, 1, 24);
        var range = Maximum - Minimum;
        var filled = Value.HasValue && range > 0 ? Math.Clamp((DisplayValue - Minimum) / range, 0, 1) * count : 0;
        var spacing = 130d / count;
        for (var index = 0; index < count; index++)
        {
            var bar = new Rect(8, (count - 1 - index) * spacing + 2, 34, spacing * 0.57);
            context.PushOpacity(0.22 + 0.78 * Math.Clamp(filled - index, 0, 1));
            context.DrawRoundedRectangle(Accent, null, bar, 4, 4);
            context.Pop();
        }
    }
}
