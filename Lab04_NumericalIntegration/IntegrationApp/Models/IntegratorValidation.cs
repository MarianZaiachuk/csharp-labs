namespace IntegrationApp.Models;

internal static class IntegratorValidation
{
    public static void ValidateArguments(Func<double, double> f, double a, double b, int intervals)
    {
        ArgumentNullException.ThrowIfNull(f);

        if (!double.IsFinite(a) || !double.IsFinite(b))
        {
            throw new ArgumentException("Межі інтегрування a та b мають бути скінченними числами.");
        }

        if (intervals <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervals),
                "Кількість інтервалів n має бути додатним цілим числом.");
        }
    }
}
