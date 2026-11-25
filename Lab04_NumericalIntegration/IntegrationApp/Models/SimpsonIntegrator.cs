namespace IntegrationApp.Models;

public sealed class SimpsonIntegrator : IIntegrator
{
    public string Name => "Метод Сімпсона";

    public double Integrate(Func<double, double> f, double a, double b, int intervals)
    {
        IntegratorValidation.ValidateArguments(f, a, b, intervals);

        if (intervals < 2 || intervals % 2 != 0)
        {
            throw new ArgumentException(
                "Метод Сімпсона вимагає парної кількості інтервалів (n ≥ 2).",
                nameof(intervals));
        }

        if (a == b)
        {
            return 0.0;
        }

        double h = (b - a) / intervals;
        double sum = f(a) + f(b);

        for (int i = 1; i < intervals; i++)
        {
            double weight = i % 2 == 1 ? 4.0 : 2.0;
            sum += weight * f(a + i * h);
        }

        return (h / 3.0) * sum;
    }
}
