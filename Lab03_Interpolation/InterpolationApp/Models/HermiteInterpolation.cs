namespace InterpolationApp.Models;

public sealed class HermiteInterpolation : Interpolation
{
    public override string Name => "Кубічна інтерполяція Ерміта";

    public override double Calculate(IReadOnlyList<PointD> points, double x)
    {
        ValidateSupportPoints(points);
        int i = FindSegmentIndex(points, x);
        double clampedX = Math.Clamp(x, points[0].X, points[^1].X);

        PointD p0 = points[i];
        PointD p1 = points[i + 1];
        double dx = p1.X - p0.X;
        double t = (clampedX - p0.X) / dx;
        double t2 = t * t;
        double t3 = t2 * t;

        double m0 = EstimateSlope(points, i);
        double m1 = EstimateSlope(points, i + 1);

        double h00 = 2.0 * t3 - 3.0 * t2 + 1.0;
        double h10 = t3 - 2.0 * t2 + t;
        double h01 = -2.0 * t3 + 3.0 * t2;
        double h11 = t3 - t2;

        return h00 * p0.Y + h10 * dx * m0 + h01 * p1.Y + h11 * dx * m1;
    }

    private static double EstimateSlope(IReadOnlyList<PointD> points, int index)
    {
        if (index == 0)
        {
            return (points[1].Y - points[0].Y) / (points[1].X - points[0].X);
        }

        if (index == points.Count - 1)
        {
            return (points[^1].Y - points[^2].Y) / (points[^1].X - points[^2].X);
        }

        return (points[index + 1].Y - points[index - 1].Y) / (points[index + 1].X - points[index - 1].X);
    }
}
