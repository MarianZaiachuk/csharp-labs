namespace IntegrationApp.Models;

public interface IIntegrator
{
    string Name { get; }

    double Integrate(Func<double, double> f, double a, double b, int intervals);
}
