namespace InterpolationApp.Models;

public abstract class Interpolation
{
    protected const double EndpointTolerance = 1e-12;

    public abstract string Name { get; }

    public abstract double Calculate(IReadOnlyList<PointD> points, double x);

    protected static void ValidateSupportPoints(IReadOnlyList<PointD> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count < 2)
        {
            throw new ArgumentException(
                "Для інтерполяції потрібно щонайменше 2 опорні точки.",
                nameof(points));
        }

        if (!double.IsFinite(points[0].X) || !double.IsFinite(points[0].Y))
        {
            throw new ArgumentException(
                "Координати опорних точок мають бути скінченними числами.",
                nameof(points));
        }

        for (int i = 1; i < points.Count; i++)
        {
            if (!double.IsFinite(points[i].X) || !double.IsFinite(points[i].Y))
            {
                throw new ArgumentException(
                    "Координати опорних точок мають бути скінченними числами.",
                    nameof(points));
            }

            if (points[i].X <= points[i - 1].X)
            {
                throw new ArgumentException(
                    "Координати X опорних точок мають строго зростати (однакові X заборонені).",
                    nameof(points));
            }
        }
    }

    protected static int FindSegmentIndex(IReadOnlyList<PointD> points, double x)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Аргумент x має бути скінченним числом.");
        }

        double minX = points[0].X;
        double maxX = points[^1].X;
        if (x < minX - EndpointTolerance || x > maxX + EndpointTolerance)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                $"Значення x = {x} виходить за межі діапазону опорних точок [{minX}; {maxX}].");
        }

        if (x <= minX)
        {
            return 0;
        }

        if (x >= maxX)
        {
            return points.Count - 2;
        }

        int low = 0;
        int high = points.Count - 2;
        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (x < points[mid].X)
            {
                high = mid - 1;
            }
            else if (x > points[mid + 1].X)
            {
                low = mid + 1;
            }
            else
            {
                return mid;
            }
        }

        return points.Count - 2;
    }
}
