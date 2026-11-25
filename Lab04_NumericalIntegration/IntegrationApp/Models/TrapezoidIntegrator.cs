namespace IntegrationApp.Models;

public sealed class TrapezoidIntegrator : IIntegrator
{
    public string Name => "Метод трапецій";

    public double Integrate(Func<double, double> f, double a, double b, int intervals)
    {
        IntegratorValidation.ValidateArguments(f, a, b, intervals);

        if (a == b)
        {
            return 0.0;
        }

        double h = (b - a) / intervals;
        double sum = 0.5 * (f(a) + f(b));

        for (int i = 1; i < intervals; i++)
        {
            sum += f(a + i * h);
        }

        return h * sum;
    }
}
