namespace LissajousApp.Models;

public sealed class LissajousModel
{
    private const double StepTolerance = 1e-12;
    private const double DefaultCanvasPadding = 24.0;

    public static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    public static double GetFastestPeriod(double fx, double fy) => 1.0 / Math.Max(fx, fy);

    public static double GetMaxAllowedTimeStep(double fx, double fy) =>
        GetFastestPeriod(fx, fy) / LissajousParameters.MinPointsPerPeriod;

    public static double GetRecommendedTimeStep(double fx, double fy) =>
        GetFastestPeriod(fx, fy) / LissajousParameters.RecommendedPointsPerPeriod;

    public static double CalculateClosedDuration(double fx, double fy)
    {
        long nx = Math.Max(1, (long)Math.Round(fx * 100.0));
        long ny = Math.Max(1, (long)Math.Round(fy * 100.0));
        long gcd = Gcd(nx, ny);

        double duration = 100.0 / gcd;
        duration = Math.Max(duration, 1.0 / Math.Min(fx, fy));

        return Math.Min(duration, LissajousParameters.MaxSimulationDuration);
    }

    public static void Validate(LissajousParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (!double.IsFinite(parameters.Ax) ||
            parameters.Ax < LissajousParameters.MinAmplitude ||
            parameters.Ax > LissajousParameters.MaxAmplitude)
        {
            throw new ArgumentException(
                $"Амплітуда Ax має бути в межах від {LissajousParameters.MinAmplitude} до {LissajousParameters.MaxAmplitude}.",
                nameof(parameters));
        }

        if (!double.IsFinite(parameters.Ay) ||
            parameters.Ay < LissajousParameters.MinAmplitude ||
            parameters.Ay > LissajousParameters.MaxAmplitude)
        {
            throw new ArgumentException(
                $"Амплітуда Ay має бути в межах від {LissajousParameters.MinAmplitude} до {LissajousParameters.MaxAmplitude}.",
                nameof(parameters));
        }

        if (!double.IsFinite(parameters.Fx) ||
            parameters.Fx < LissajousParameters.MinFrequency ||
            parameters.Fx > LissajousParameters.MaxFrequency)
        {
            throw new ArgumentException(
                $"Частота fx має бути в межах від {LissajousParameters.MinFrequency} до {LissajousParameters.MaxFrequency} Гц.",
                nameof(parameters));
        }

        if (!double.IsFinite(parameters.Fy) ||
            parameters.Fy < LissajousParameters.MinFrequency ||
            parameters.Fy > LissajousParameters.MaxFrequency)
        {
            throw new ArgumentException(
                $"Частота fy має бути в межах від {LissajousParameters.MinFrequency} до {LissajousParameters.MaxFrequency} Гц.",
                nameof(parameters));
        }

        if (!double.IsFinite(parameters.PhiXDeg) || !double.IsFinite(parameters.PhiYDeg))
        {
            throw new ArgumentException("Початкові фази мають бути скінченними числами.", nameof(parameters));
        }

        if (!double.IsFinite(parameters.TimeStep) || parameters.TimeStep <= 0)
        {
            throw new ArgumentException("Крок часу має бути додатним числом.", nameof(parameters));
        }

        double maxAllowedStep = GetMaxAllowedTimeStep(parameters.Fx, parameters.Fy);
        if (parameters.TimeStep > maxAllowedStep + StepTolerance)
        {
            throw new ArgumentException(
                $"Крок часу завеликий: для забезпечення не менше {LissajousParameters.MinPointsPerPeriod} точок на найменший період потрібно Δt ≤ {maxAllowedStep:0.####} с.",
                nameof(parameters));
        }

        double duration = CalculateClosedDuration(parameters.Fx, parameters.Fy);
        double estimatedSteps = Math.Ceiling(duration / parameters.TimeStep);
        if (estimatedSteps > LissajousParameters.MaxTotalPoints)
        {
            throw new ArgumentException(
                $"Крок часу занадто малий: кількість точок перевищує ліміт ({LissajousParameters.MaxTotalPoints}).",
                nameof(parameters));
        }
    }

    public IReadOnlyList<PointD> GeneratePoints(LissajousParameters parameters)
    {
        Validate(parameters);

        double phiX = DegreesToRadians(parameters.PhiXDeg);
        double phiY = DegreesToRadians(parameters.PhiYDeg);
        double duration = CalculateClosedDuration(parameters.Fx, parameters.Fy);
        int steps = (int)Math.Ceiling(duration / parameters.TimeStep);

        var points = new List<PointD>(steps + 1);
        for (int i = 0; i <= steps; i++)
        {
            double t = i == steps ? duration : i * parameters.TimeStep;
            double x = parameters.Ax * Math.Sin(2.0 * Math.PI * parameters.Fx * t + phiX);
            double y = parameters.Ay * Math.Sin(2.0 * Math.PI * parameters.Fy * t + phiY);
            points.Add(new PointD(x, y));
        }

        return points;
    }

    public static IReadOnlyList<PointD> MapToScreen(
        IReadOnlyList<PointD> modelPoints,
        double maxAmplitudeX,
        double maxAmplitudeY,
        double canvasWidth,
        double canvasHeight,
        double padding = DefaultCanvasPadding)
    {
        ArgumentNullException.ThrowIfNull(modelPoints);

        if (modelPoints.Count == 0 ||
            maxAmplitudeX <= 0 ||
            maxAmplitudeY <= 0 ||
            canvasWidth <= 2.0 * padding ||
            canvasHeight <= 2.0 * padding)
        {
            return Array.Empty<PointD>();
        }

        double usableWidth = canvasWidth - 2.0 * padding;
        double usableHeight = canvasHeight - 2.0 * padding;
        double scale = Math.Min(
            usableWidth / (2.0 * maxAmplitudeX),
            usableHeight / (2.0 * maxAmplitudeY));

        double centerX = canvasWidth / 2.0;
        double centerY = canvasHeight / 2.0;

        var screenPoints = new List<PointD>(modelPoints.Count);
        for (int i = 0; i < modelPoints.Count; i++)
        {
            PointD point = modelPoints[i];
            screenPoints.Add(new PointD(
                centerX + point.X * scale,
                centerY - point.Y * scale));
        }

        return screenPoints;
    }

    private static long Gcd(long a, long b)
    {
        while (b != 0)
        {
            long remainder = a % b;
            a = b;
            b = remainder;
        }

        return a;
    }
}
