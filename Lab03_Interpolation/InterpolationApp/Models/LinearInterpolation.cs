namespace InterpolationApp.Models;

/// <summary>
/// Кусочно-лінійна інтерполяція між сусідніми опорними точками.
/// </summary>
public sealed class LinearInterpolation : Interpolation
{
    public override string Name => "Лінійна інтерполяція";

    public override double Calculate(IReadOnlyList<PointD> points, double x)
    {
        ValidateSupportPoints(points);
        int i = FindSegmentIndex(points, x);
        double clampedX = Math.Clamp(x, points[0].X, points[^1].X);

        PointD p0 = points[i];
        PointD p1 = points[i + 1];
        double t = (clampedX - p0.X) / (p1.X - p0.X);

        return p0.Y + t * (p1.Y - p0.Y);
    }
}
