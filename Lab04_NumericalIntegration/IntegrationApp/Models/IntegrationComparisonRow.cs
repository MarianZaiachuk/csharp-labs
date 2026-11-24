namespace IntegrationApp.Models;

/// <summary>
/// Рядок порівняльної таблиці результатів інтегрування для одного методу.
/// </summary>
public sealed record IntegrationComparisonRow(
    string MethodName,
    string ApproximationText,
    string ExactValueText,
    string AbsoluteErrorText,
    string RelativeErrorText,
    string StatusNote,
    bool IsSuccess,
    double? Approximation,
    double? AbsoluteError);
