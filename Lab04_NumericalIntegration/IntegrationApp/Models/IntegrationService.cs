using System.Globalization;

namespace IntegrationApp.Models;

/// <summary>
/// Сервіс порівняння стратегій чисельного інтегрування та аналізу збіжності.
/// Працює виключно через контракт IIntegrator і не залежить від WPF.
/// </summary>
public static class IntegrationService
{
    public const int DefaultIntervals = 20;
    public const int MaxIntervals = 1_000_000;
    public const double DefaultPadding = 24.0;
    public const double LogFloorError = 1e-15;

    public static readonly int[] ConvergenceSteps = [10, 20, 40, 80, 160];

    public static IntegrationComparisonRow EvaluateSingle(
        IIntegrator integrator,
        KnownFunction function,
        double a,
        double b,
        int intervals)
    {
        ArgumentNullException.ThrowIfNull(integrator);
        ArgumentNullException.ThrowIfNull(function);

        double exact = function.ExactIntegral(a, b);
        string exactText = exact.ToString("0.00000000", CultureInfo.InvariantCulture);

        try
        {
            double approx = integrator.Integrate(function.Function, a, b, intervals);
            double absError = Math.Abs(approx - exact);

            string relText = Math.Abs(exact) > 1e-12
                ? $"{absError / Math.Abs(exact) * 100.0:0.0000}%"
                : "—";

            string note = a > b
                ? "a > b: знак інтеграла інвертовано"
                : a == b
                    ? "a = b: інтеграл дорівнює 0"
                    : "OK";

            return new IntegrationComparisonRow(
                integrator.Name,
                approx.ToString("0.00000000", CultureInfo.InvariantCulture),
                exactText,
                absError.ToString("E2", CultureInfo.InvariantCulture),
                relText,
                note,
                true,
                approx,
                absError);
        }
        catch (ArgumentException ex)
        {
            return new IntegrationComparisonRow(
                integrator.Name,
                "—",
                exactText,
                "—",
                "—",
                ex.Message,
                false,
                null,
                null);
        }
    }

    public static IReadOnlyList<IntegrationComparisonRow> CompareAll(
        IReadOnlyList<IIntegrator> integrators,
        KnownFunction function,
        double a,
        double b,
        int intervals)
    {
        ArgumentNullException.ThrowIfNull(integrators);

        var rows = new List<IntegrationComparisonRow>(integrators.Count);
        for (int i = 0; i < integrators.Count; i++)
        {
            rows.Add(EvaluateSingle(integrators[i], function, a, b, intervals));
        }

        return rows;
    }

    public static IReadOnlyList<ConvergenceSeries> ComputeConvergence(
        IReadOnlyList<IIntegrator> integrators,
        KnownFunction function,
        double a,
        double b,
        IReadOnlyList<int> stepCounts)
    {
        ArgumentNullException.ThrowIfNull(integrators);
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(stepCounts);

        double exact = function.ExactIntegral(a, b);
        var seriesList = new List<ConvergenceSeries>(integrators.Count);

        for (int i = 0; i < integrators.Count; i++)
        {
            IIntegrator integrator = integrators[i];
            var points = new List<(int Intervals, double AbsoluteError)>(stepCounts.Count);

            for (int j = 0; j < stepCounts.Count; j++)
            {
                int n = stepCounts[j];
                double approx = integrator.Integrate(function.Function, a, b, n);
                double error = Math.Abs(approx - exact);
                points.Add((n, error));
            }

            seriesList.Add(new ConvergenceSeries(integrator.Name, points));
        }

        return seriesList;
    }

    public static IReadOnlyList<PointD> MapConvergenceToScreen(
        IReadOnlyList<(int Intervals, double AbsoluteError)> points,
        double maxLinearError,
        double minLogError,
        double maxLogError,
        bool useLogScale,
        double canvasWidth,
        double canvasHeight,
        double padding = DefaultPadding)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count == 0 ||
            canvasWidth <= 2.0 * padding ||
            canvasHeight <= 2.0 * padding)
        {
            return Array.Empty<PointD>();
        }

        double usableWidth = canvasWidth - 2.0 * padding;
        double minY = useLogScale ? minLogError : 0.0;
        double maxY = useLogScale ? maxLogError : Math.Max(maxLinearError, 1e-12);

        var mapped = new List<PointD>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            double xFraction = points.Count == 1 ? 0.5 : (double)i / (points.Count - 1);
            double sx = padding + xFraction * usableWidth;

            double rawValue = useLogScale
                ? Math.Log10(Math.Max(points[i].AbsoluteError, LogFloorError))
                : points[i].AbsoluteError;

            double sy = MapYToScreen(rawValue, minY, maxY, canvasHeight, padding);
            mapped.Add(new PointD(sx, sy));
        }

        return mapped;
    }

    public static double MapYToScreen(
        double value,
        double minValue,
        double maxValue,
        double canvasHeight,
        double padding = DefaultPadding)
    {
        if (canvasHeight <= 2.0 * padding)
        {
            return canvasHeight / 2.0;
        }

        double usableHeight = canvasHeight - 2.0 * padding;
        double span = maxValue - minValue;
        if (span < 1e-15)
        {
            return padding + usableHeight;
        }

        return padding + (maxValue - value) / span * usableHeight;
    }
}
