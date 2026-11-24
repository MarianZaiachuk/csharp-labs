namespace IntegrationApp.Models;

/// <summary>
/// Послідовність значень абсолютної похибки для різних розбиттів n.
/// </summary>
public sealed record ConvergenceSeries(
    string MethodName,
    IReadOnlyList<(int Intervals, double AbsoluteError)> Points);
