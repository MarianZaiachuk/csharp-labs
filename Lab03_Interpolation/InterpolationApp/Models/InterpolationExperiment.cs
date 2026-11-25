namespace InterpolationApp.Models;

public static class InterpolationExperiment
{
    public const double DefaultH1 = 0.8;
    public const double DefaultH2 = 0.05;
    public const double MinH1 = 0.05;
    public const double MaxH1 = 5.0;
    public const double MinStep = 0.005;
    public const int MaxPoints = 20_000;
    public const double DefaultPadding = 24.0;

    private const double NonUniformShortFactor = 0.65;
    private const double NonUniformLongFactor = 1.35;

    public static IReadOnlyList<PointD> GenerateSupportPoints(
        Func<double, double> function,
        double startX,
        double endX,
        double h1,
        bool nonUniform)
    {
        ArgumentNullException.ThrowIfNull(function);

        if (endX <= startX || h1 <= 0 || h1 >= endX - startX)
        {
            throw new ArgumentException("Некоректний інтервал або крок h1 для опорних точок.");
        }

        var points = new List<PointD>();
        double x = startX;
        int stepIndex = 0;

        while (x < endX - 1e-6 * h1)
        {
            points.Add(new PointD(x, function(x)));
            double step = nonUniform
                ? h1 * (stepIndex % 2 == 0 ? NonUniformShortFactor : NonUniformLongFactor)
                : h1;
            x += step;
            stepIndex++;
        }

        if (points.Count == 0 || endX - points[^1].X > 1e-9)
        {
            points.Add(new PointD(endX, function(endX)));
        }
        else
        {
            points[^1] = new PointD(endX, function(endX));
        }

        return points;
    }

    public static IReadOnlyList<double> GenerateFineGrid(double startX, double endX, double h2)
    {
        if (endX <= startX || h2 <= 0)
        {
            throw new ArgumentException("Некоректні параметри сітки h2.");
        }

        int estimatedCount = (int)Math.Ceiling((endX - startX) / h2) + 2;
        if (estimatedCount > MaxPoints)
        {
            throw new ArgumentException($"Кількість точок перевищує ліміт ({MaxPoints}).");
        }

        var grid = new List<double>(estimatedCount);
        double x = startX;
        while (x < endX - 1e-6 * h2)
        {
            grid.Add(x);
            x += h2;
        }

        if (grid.Count == 0 || endX - grid[^1] > 1e-9)
        {
            grid.Add(endX);
        }
        else
        {
            grid[^1] = endX;
        }

        return grid;
    }

    public static IReadOnlyList<InterpolationSeriesResult> EvaluateAll(
        IReadOnlyList<Interpolation> interpolators,
        IReadOnlyList<PointD> supportPoints,
        IReadOnlyList<double> fineGrid,
        Func<double, double> referenceFunction)
    {
        ArgumentNullException.ThrowIfNull(interpolators);
        ArgumentNullException.ThrowIfNull(supportPoints);
        ArgumentNullException.ThrowIfNull(fineGrid);
        ArgumentNullException.ThrowIfNull(referenceFunction);

        var referenceValues = new double[fineGrid.Count];
        for (int i = 0; i < fineGrid.Count; i++)
        {
            referenceValues[i] = referenceFunction(fineGrid[i]);
        }

        var results = new List<InterpolationSeriesResult>(interpolators.Count);
        for (int methodIndex = 0; methodIndex < interpolators.Count; methodIndex++)
        {
            Interpolation interpolator = interpolators[methodIndex];
            var curvePoints = new List<PointD>(fineGrid.Count);
            var errorPoints = new List<PointD>(fineGrid.Count);
            double maxError = 0.0;

            for (int i = 0; i < fineGrid.Count; i++)
            {
                double x = fineGrid[i];
                double yInterp = interpolator.Calculate(supportPoints, x);
                double absError = Math.Abs(yInterp - referenceValues[i]);
                if (absError > maxError)
                {
                    maxError = absError;
                }

                curvePoints.Add(new PointD(x, yInterp));
                errorPoints.Add(new PointD(x, absError));
            }

            results.Add(new InterpolationSeriesResult(
                interpolator.Name,
                curvePoints,
                errorPoints,
                maxError));
        }

        return results;
    }

    public static IReadOnlyList<PointD> MapToScreen(
        IReadOnlyList<PointD> points,
        double minX,
        double maxX,
        double minY,
        double maxY,
        double canvasWidth,
        double canvasHeight,
        double padding = DefaultPadding)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count == 0 ||
            canvasWidth <= 2.0 * padding ||
            canvasHeight <= 2.0 * padding ||
            maxX - minX < 1e-12)
        {
            return Array.Empty<PointD>();
        }

        double usableWidth = canvasWidth - 2.0 * padding;
        double spanX = maxX - minX;

        var mapped = new List<PointD>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            double sx = padding + (points[i].X - minX) / spanX * usableWidth;
            double sy = MapYToScreen(points[i].Y, minY, maxY, canvasHeight, padding);
            mapped.Add(new PointD(sx, sy));
        }

        return mapped;
    }

    public static double MapYToScreen(
        double y,
        double minY,
        double maxY,
        double canvasHeight,
        double padding = DefaultPadding)
    {
        if (canvasHeight <= 2.0 * padding)
        {
            return canvasHeight / 2.0;
        }

        double usableHeight = canvasHeight - 2.0 * padding;
        double spanY = maxY - minY;
        if (spanY < 1e-9)
        {
            return padding + usableHeight / 2.0;
        }

        return padding + (maxY - y) / spanY * usableHeight;
    }
}
