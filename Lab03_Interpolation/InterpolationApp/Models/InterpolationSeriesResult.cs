namespace InterpolationApp.Models;

public sealed record InterpolationSeriesResult(
    string MethodName,
    IReadOnlyList<PointD> CurvePoints,
    IReadOnlyList<PointD> ErrorPoints,
    double MaxAbsoluteError);
