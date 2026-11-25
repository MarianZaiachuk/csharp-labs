namespace IntegrationApp.Models;

public sealed class RectangleIntegrator : IIntegrator
{
    public string Name => "Середні прямокутники";

    public double Integrate(Func<double, double> f, double a, double b, int intervals)
    {
        IntegratorValidation.ValidateArguments(f, a, b, intervals);

        if (a == b)
        {
            return 0.0;
        }

        double h = (b - a) / intervals;
        double sum = 0.0;

        for (int i = 0; i < intervals; i++)
        {
            double xMid = a + (i + 0.5) * h;
            sum += f(xMid);
        }

        return h * sum;
    }
}
