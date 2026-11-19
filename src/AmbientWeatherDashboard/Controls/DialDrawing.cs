using System.Windows;
using System.Windows.Media;

namespace AmbientWeatherDashboard.Controls;

/// <summary>提供自訂儀表共用的色彩、描邊、座標與幾何繪製工具。</summary>
internal static class DialDrawing
{
    /// <summary>儀表文字與高亮細節共用的淡白色畫筆。</summary>
    internal static readonly Brush White = Brush("#F5F9FF");
    /// <summary>次要標籤與弱化線條共用的藍灰色畫筆。</summary>
    internal static readonly Brush Muted = Brush("#9AB3C8");
    /// <summary>進度與降雨等圖形共用的淺藍色畫筆。</summary>
    internal static readonly Brush Ice = Brush("#9BD5FF");
    /// <summary>風向和其他圖形細節共用的青綠色畫筆。</summary>
    internal static readonly Brush Aqua = Brush("#87E0D3");
    /// <summary>圓形儀表強調線條共用的紫色畫筆。</summary>
    internal static readonly Brush Violet = Brush("#9B6ADD");
    /// <summary>日照、指針和其他圖形細節共用的金色畫筆。</summary>
    internal static readonly Brush Gold = Brush("#FFC72B");
    /// <summary>刻度盤背景圓環共用的深藍色畫筆。</summary>
    internal static readonly Brush Track = Brush("#254764");

    /// <summary>將十六進位色碼轉成凍結的純色畫筆。</summary>
    internal static Brush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    /// <summary>建立端點為圓角且已凍結的描邊筆。</summary>
    internal static Pen Pen(Brush brush, double width)
    {
        var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        return pen;
    }

    /// <summary>依中心、半徑和自正上方順時針起算的角度取得圓周座標。</summary>
    internal static Point PointAt(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(center.X + radius * Math.Sin(radians), center.Y - radius * Math.Cos(radians));
    }

    /// <summary>繪製指定起點和角度跨度的圓弧，完整跨度改繪整圓。</summary>
    internal static void Arc(DrawingContext context, Point center, double radius, double start, double sweep, Pen pen)
    {
        if (sweep <= 0)
            return;
        if (sweep >= 359.999)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            path.BeginFigure(PointAt(center, radius, start), false, false);
            path.ArcTo(PointAt(center, radius, start + sweep), new Size(radius, radius), 0,
                sweep > 180, SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        context.DrawGeometry(null, pen, geometry);
    }

    /// <summary>解析幾何路徑字串並回傳已凍結的圖形。</summary>
    internal static Geometry Geometry(string path)
    {
        var geometry = System.Windows.Media.Geometry.Parse(path);
        geometry.Freeze();
        return geometry;
    }
}
