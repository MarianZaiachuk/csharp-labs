namespace LissajousApp.Models;

/// <summary>
/// Параметри побудови фігури Ліссажу та допустимі межі значень.
/// </summary>
public sealed record LissajousParameters(
    double Ax,
    double Ay,
    double Fx,
    double Fy,
    double PhiXDeg,
    double PhiYDeg,
    double TimeStep)
{
    public const double MinAmplitude = 0.1;
    public const double MaxAmplitude = 10.0;
    public const double MinFrequency = 0.1;
    public const double MaxFrequency = 20.0;
    public const double MinPhaseDeg = 0.0;
    public const double MaxPhaseDeg = 360.0;
    public const int MinPointsPerPeriod = 20;
    public const int RecommendedPointsPerPeriod = 100;
    public const int MaxTotalPoints = 50_000;
    public const double MaxSimulationDuration = 20.0;
}
