using System.Windows;
using System.Windows.Media;

namespace AmbientWeatherDashboard.Controls;

internal static class DialDrawing
{
    internal static readonly Brush White = Brush("#F5F9FF");
    internal static readonly Brush Muted = Brush("#9AB3C8");
    internal static readonly Brush Ice = Brush("#9BD5FF");
    internal static readonly Brush Aqua = Brush("#87E0D3");
    internal static readonly Brush Violet = Brush("#9B6ADD");
    internal static readonly Brush Gold = Brush("#FFC72B");
    internal static readonly Brush Track = Brush("#254764");

    internal static Brush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    internal static Pen Pen(Brush brush, double width)
    {
        var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        return pen;
    }

    internal static Point PointAt(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(center.X + radius * Math.Sin(radians), center.Y - radius * Math.Cos(radians));
    }

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

    internal static Geometry Geometry(string path)
    {
        var geometry = System.Windows.Media.Geometry.Parse(path);
        geometry.Freeze();
        return geometry;
    }
}
