using System.Text.Json.Serialization;

namespace GameLogic;

public enum ShapeKind { Rect, Ellipse, Arc, Triangle }
public readonly record struct ShapePoint(double X, double Y);

public record Obstacle(int X, int Y, int Width, int Height)
{
    public ShapeKind Shape { get; init; }
    public string Color { get; init; } = "#646b50";
    public double StartAngle { get; init; }
    public double SweepAngle { get; init; } = 180;
    public double InnerRatio { get; init; } = 0.55;

    // The same polygon is used for drawing and collision, including the hollow arc.
    [JsonIgnore]
    public IReadOnlyList<ShapePoint> Outline
    {
        get
        {
            var key = (X, Y, Width, Height, Shape, StartAngle, SweepAngle, InnerRatio);
            if (outline is null || outlineKey != key)
            {
                outline = BuildOutline();
                svgPoints = null;
                outlineKey = key;
            }
            return outline;
        }
    }
    private IReadOnlyList<ShapePoint>? outline;
    private (int, int, int, int, ShapeKind, double, double, double) outlineKey;
    private string? svgPoints;
    [JsonIgnore]
    public string SvgPoints
    {
        get
        {
            var points = Outline;
            return svgPoints ??= string.Join(" ", points.Select(p =>
                FormattableString.Invariant($"{p.X:0.###},{p.Y:0.###}")));
        }
    }

    private IReadOnlyList<ShapePoint> BuildOutline()
    {
        if (Shape == ShapeKind.Rect)
            return [new(X, Y), new(X + Width, Y), new(X + Width, Y + Height), new(X, Y + Height)];
        if (Shape == ShapeKind.Triangle)
            return [new(X + Width / 2.0, Y), new(X + Width, Y + Height), new(X, Y + Height)];
        var points = new List<ShapePoint>();
        var sweep = Shape == ShapeKind.Ellipse ? 360 : SweepAngle;
        var start = Shape == ShapeKind.Ellipse ? 0 : StartAngle;
        var segments = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweep) / 5));
        ShapePoint Point(double angle, double radius)
        {
            var radians = angle * Math.PI / 180;
            return new(X + Width / 2.0 * (1 + radius * Math.Cos(radians)),
                       Y + Height / 2.0 * (1 + radius * Math.Sin(radians)));
        }
        for (var i = 0; i <= segments; i++)
            points.Add(Point(start + sweep * i / segments, 1));
        if (Shape == ShapeKind.Arc)
            for (var i = segments; i >= 0; i--)
                points.Add(Point(start + sweep * i / segments, InnerRatio));
        return points;
    }

    public bool ContainsPoint(int x, int y) => Contains(new ShapePoint(x, y));

    private bool Contains(ShapePoint point)
    {
        if (point.X < X || point.X > X + Width || point.Y < Y || point.Y > Y + Height)
            return false;
        var inside = false;
        for (var i = 0; i < Outline.Count; i++)
        {
            var a = Outline[i];
            var b = Outline[(i + 1) % Outline.Count];
            if (OnSegment(a, b, point)) return true;
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    public bool Intersects(RectangleArea area)
    {
        if (X >= area.X + area.Width || X + Width <= area.X ||
            Y >= area.Y + area.Height || Y + Height <= area.Y) return false;
        if (Shape == ShapeKind.Rect) return true;
        ShapePoint[] corners = [new(area.X, area.Y), new(area.X + area.Width, area.Y),
            new(area.X + area.Width, area.Y + area.Height), new(area.X, area.Y + area.Height)];
        if (corners.Any(Contains) || Outline.Any(p => p.X >= area.X && p.X <= area.X + area.Width &&
            p.Y >= area.Y && p.Y <= area.Y + area.Height)) return true;
        for (var i = 0; i < Outline.Count; i++)
            for (var j = 0; j < 4; j++)
                if (Crosses(Outline[i], Outline[(i + 1) % Outline.Count], corners[j], corners[(j + 1) % 4]))
                    return true;
        return false;
    }

    private static double Cross(ShapePoint a, ShapePoint b, ShapePoint p) =>
        (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
    private static bool OnSegment(ShapePoint a, ShapePoint b, ShapePoint p) =>
        Math.Abs(Cross(a, b, p)) < 0.000001 &&
        p.X >= Math.Min(a.X, b.X) - 0.000001 && p.X <= Math.Max(a.X, b.X) + 0.000001 &&
        p.Y >= Math.Min(a.Y, b.Y) - 0.000001 && p.Y <= Math.Max(a.Y, b.Y) + 0.000001;
    private static bool Crosses(ShapePoint a, ShapePoint b, ShapePoint c, ShapePoint d) =>
        (Cross(a, b, c) * Cross(a, b, d) < 0 && Cross(c, d, a) * Cross(c, d, b) < 0) ||
        OnSegment(a, b, c) || OnSegment(a, b, d) || OnSegment(c, d, a) || OnSegment(c, d, b);
}
