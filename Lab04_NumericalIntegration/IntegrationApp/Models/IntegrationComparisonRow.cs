namespace IntegrationApp.Models;

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
