namespace InterpolationApp.Models;

/// <summary>
/// Опис тестової функції та інтервалу табулювання.
/// </summary>
public sealed record TestFunctionOption(
    string DisplayName,
    double StartX,
    double EndX,
    Func<double, double> Function);
