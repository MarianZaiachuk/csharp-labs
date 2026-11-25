namespace IntegrationApp.Models;

public sealed record ConvergenceSeries(
    string MethodName,
    IReadOnlyList<(int Intervals, double AbsoluteError)> Points);
