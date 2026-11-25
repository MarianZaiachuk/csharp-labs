namespace InterpolationApp.Models;

public sealed record TestFunctionOption(
    string DisplayName,
    double StartX,
    double EndX,
    Func<double, double> Function);
