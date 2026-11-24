namespace InterpolationApp.Models;

/// <summary>
/// Результат обчислення інтерпольованої кривої та її абсолютної похибки.
/// </summary>
public sealed record InterpolationSeriesResult(
    string MethodName,
    IReadOnlyList<PointD> CurvePoints,
    IReadOnlyList<PointD> ErrorPoints,
    double MaxAbsoluteError);
