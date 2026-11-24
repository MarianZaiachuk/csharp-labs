namespace IntegrationApp.Models;

/// <summary>
/// Контракт стратегії чисельного обчислення визначеного інтеграла.
/// </summary>
public interface IIntegrator
{
    string Name { get; }

    double Integrate(Func<double, double> f, double a, double b, int intervals);
}
