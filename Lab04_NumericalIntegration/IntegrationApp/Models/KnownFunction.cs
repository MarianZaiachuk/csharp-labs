namespace IntegrationApp.Models;

/// <summary>
/// Підінтегральна функція з відомою первісною для обчислення еталонного значення.
/// </summary>
public sealed record KnownFunction(
    string DisplayName,
    double DefaultA,
    double DefaultB,
    Func<double, double> Function,
    Func<double, double> Antiderivative)
{
    public double ExactIntegral(double a, double b) => Antiderivative(b) - Antiderivative(a);
}
