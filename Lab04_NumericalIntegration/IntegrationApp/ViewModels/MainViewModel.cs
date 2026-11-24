using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using IntegrationApp.Commands;
using IntegrationApp.Models;

namespace IntegrationApp.ViewModels;

/// <summary>
/// ViewModel головного вікна. Працює виключно через контракт IIntegrator
/// без розгалужень обчислення для конкретних реалізацій.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IIntegrator[] _strategies;

    private KnownFunction _selectedFunction;
    private IIntegrator _selectedStrategy;
    private string _aText = string.Empty;
    private string _bText = string.Empty;
    private string _intervalsText = IntegrationService.DefaultIntervals.ToString(CultureInfo.InvariantCulture);
    private string _aError = string.Empty;
    private string _bError = string.Empty;
    private string _intervalsError = string.Empty;
    private bool _useLogScale;

    private string _selectedStrategySummary = string.Empty;
    private string _statusText = string.Empty;
    private string _chartScaleHint = string.Empty;

    private double _canvasWidth = 640.0;
    private double _canvasHeight = 250.0;
    private double _baselineY = 226.0;

    private PointCollection _rectangleErrorPoints = new();
    private PointCollection _trapezoidErrorPoints = new();
    private PointCollection _simpsonErrorPoints = new();

    private IReadOnlyList<ConvergenceSeries> _lastConvergenceSeries = Array.Empty<ConvergenceSeries>();
    private bool _suppressAutoCalculate;

    public MainViewModel()
    {
        _strategies =
        [
            new RectangleIntegrator(),
            new TrapezoidIntegrator(),
            new SimpsonIntegrator()
        ];
        _selectedStrategy = _strategies[2];

        Functions =
        [
            new KnownFunction("sin(x)  [еталон на [0, π] = 2]", 0.0, Math.PI, Math.Sin, x => -Math.Cos(x)),
            new KnownFunction("x  [еталон на [0, 1] = 0.5]", 0.0, 1.0, x => x, x => 0.5 * x * x),
            new KnownFunction("x²  [еталон на [0, 3] = 9]", 0.0, 3.0, x => x * x, x => x * x * x / 3.0),
            new KnownFunction("eˣ  [еталон на [0, 1] = e − 1]", 0.0, 1.0, Math.Exp, Math.Exp)
        ];
        _selectedFunction = Functions[0];

        _aText = FormatBound(_selectedFunction.DefaultA);
        _bText = FormatBound(_selectedFunction.DefaultB);

        ComparisonRows = [];
        ConvergenceStepLabels = IntegrationService.ConvergenceSteps
            .Select(n => $"n = {n}")
            .ToArray();

        CalculateCommand = new RelayCommand(ValidateAndCalculate);
        SwapBoundsCommand = new RelayCommand(SwapBounds);

        ValidateAndCalculate();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<IIntegrator> Strategies => _strategies;

    public IReadOnlyList<KnownFunction> Functions { get; }

    public ObservableCollection<IntegrationComparisonRow> ComparisonRows { get; }

    public IReadOnlyList<string> ConvergenceStepLabels { get; }

    public ICommand CalculateCommand { get; }

    public ICommand SwapBoundsCommand { get; }

    public KnownFunction SelectedFunction
    {
        get => _selectedFunction;
        set
        {
            if (SetField(ref _selectedFunction, value))
            {
                _suppressAutoCalculate = true;
                AText = FormatBound(value.DefaultA);
                BText = FormatBound(value.DefaultB);
                _suppressAutoCalculate = false;
                ValidateAndCalculate();
            }
        }
    }

    public IIntegrator SelectedStrategy
    {
        get => _selectedStrategy;
        set
        {
            if (SetField(ref _selectedStrategy, value))
            {
                UpdateSelectedStrategySummary();
            }
        }
    }

    public string AText
    {
        get => _aText;
        set
        {
            if (SetField(ref _aText, value) && !_suppressAutoCalculate)
            {
                ValidateAndCalculate();
            }
        }
    }

    public string BText
    {
        get => _bText;
        set
        {
            if (SetField(ref _bText, value) && !_suppressAutoCalculate)
            {
                ValidateAndCalculate();
            }
        }
    }

    public string IntervalsText
    {
        get => _intervalsText;
        set
        {
            if (SetField(ref _intervalsText, value))
            {
                ValidateAndCalculate();
            }
        }
    }

    public string AError
    {
        get => _aError;
        private set => SetField(ref _aError, value);
    }

    public string BError
    {
        get => _bError;
        private set => SetField(ref _bError, value);
    }

    public string IntervalsError
    {
        get => _intervalsError;
        private set => SetField(ref _intervalsError, value);
    }

    public bool UseLogScale
    {
        get => _useLogScale;
        set
        {
            if (SetField(ref _useLogScale, value))
            {
                ProjectConvergenceChart();
            }
        }
    }

    public string SelectedStrategySummary
    {
        get => _selectedStrategySummary;
        private set => SetField(ref _selectedStrategySummary, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string ChartScaleHint
    {
        get => _chartScaleHint;
        private set => SetField(ref _chartScaleHint, value);
    }

    public double CanvasWidth
    {
        get => _canvasWidth;
        private set => SetField(ref _canvasWidth, value);
    }

    public double CanvasHeight
    {
        get => _canvasHeight;
        private set => SetField(ref _canvasHeight, value);
    }

    public double BaselineY
    {
        get => _baselineY;
        private set => SetField(ref _baselineY, value);
    }

    public PointCollection RectangleErrorPoints
    {
        get => _rectangleErrorPoints;
        private set => SetField(ref _rectangleErrorPoints, value);
    }

    public PointCollection TrapezoidErrorPoints
    {
        get => _trapezoidErrorPoints;
        private set => SetField(ref _trapezoidErrorPoints, value);
    }

    public PointCollection SimpsonErrorPoints
    {
        get => _simpsonErrorPoints;
        private set => SetField(ref _simpsonErrorPoints, value);
    }

    public void UpdateCanvasSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        CanvasWidth = width;
        CanvasHeight = height;
        ProjectConvergenceChart();
    }

    private void SwapBounds()
    {
        _suppressAutoCalculate = true;
        string oldA = AText;
        AText = BText;
        BText = oldA;
        _suppressAutoCalculate = false;
        ValidateAndCalculate();
    }

    private void ValidateAndCalculate()
    {
        bool aValid = TryParseDouble(_aText, out double a);
        AError = aValid ? string.Empty : "Нижня межа a має бути скінченним числом.";

        bool bValid = TryParseDouble(_bText, out double b);
        BError = bValid ? string.Empty : "Верхня межа b має бути скінченним числом.";

        bool nValid = TryParsePositiveInt(_intervalsText, out int n) && n <= IntegrationService.MaxIntervals;
        IntervalsError = nValid
            ? string.Empty
            : $"Кількість інтервалів n має бути цілим числом від 1 до {IntegrationService.MaxIntervals}.";

        if (!aValid || !bValid || !nValid)
        {
            StatusText = "Виправте некоректні параметри інтегрування.";
            return;
        }

        IReadOnlyList<IntegrationComparisonRow> rows = IntegrationService.CompareAll(
            _strategies,
            SelectedFunction,
            a,
            b,
            n);

        ComparisonRows.Clear();
        for (int i = 0; i < rows.Count; i++)
        {
            ComparisonRows.Add(rows[i]);
        }

        UpdateSelectedStrategySummary();

        _lastConvergenceSeries = IntegrationService.ComputeConvergence(
            _strategies,
            SelectedFunction,
            a,
            b,
            IntegrationService.ConvergenceSteps);

        ProjectConvergenceChart();

        double exact = SelectedFunction.ExactIntegral(a, b);
        string directionNote = a > b ? " (a > b: зміна знаку інтеграла)" : string.Empty;
        StatusText =
            $"Межі: [{a:0.####}; {b:0.####}]{directionNote} | n = {n} | Еталон = {exact:0.########}";
    }

    private void UpdateSelectedStrategySummary()
    {
        if (!TryParseDouble(_aText, out double a) ||
            !TryParseDouble(_bText, out double b) ||
            !TryParsePositiveInt(_intervalsText, out int n) ||
            n > IntegrationService.MaxIntervals)
        {
            SelectedStrategySummary = "Некоректні вхідні параметри.";
            return;
        }

        IntegrationComparisonRow row = IntegrationService.EvaluateSingle(
            SelectedStrategy,
            SelectedFunction,
            a,
            b,
            n);

        SelectedStrategySummary = row.IsSuccess
            ? $"{row.MethodName}: I ≈ {row.ApproximationText}\n" +
              $"Еталон: {row.ExactValueText}\n" +
              $"Абс. похибка: {row.AbsoluteErrorText} (відн.: {row.RelativeErrorText})"
            : $"{row.MethodName}: {row.StatusNote}";
    }

    private void ProjectConvergenceChart()
    {
        if (_lastConvergenceSeries.Count < 3)
        {
            return;
        }

        double maxLinearError = 0.0;
        double minLogError = double.PositiveInfinity;
        double maxLogError = double.NegativeInfinity;

        foreach (ConvergenceSeries series in _lastConvergenceSeries)
        {
            foreach ((_, double error) in series.Points)
            {
                if (error > maxLinearError) maxLinearError = error;

                double logErr = Math.Log10(Math.Max(error, IntegrationService.LogFloorError));
                if (logErr < minLogError) minLogError = logErr;
                if (logErr > maxLogError) maxLogError = logErr;
            }
        }

        if (maxLogError - minLogError < 1.0)
        {
            minLogError -= 0.5;
            maxLogError += 0.5;
        }

        BaselineY = UseLogScale
            ? CanvasHeight - IntegrationService.DefaultPadding
            : IntegrationService.MapYToScreen(0.0, 0.0, Math.Max(maxLinearError, 1e-12), CanvasHeight);

        ChartScaleHint = UseLogScale
            ? $"log₁₀(похибка): [{minLogError:0.#}; {maxLogError:0.#}]"
            : $"Лінійна шкала: [0; {maxLinearError:E2}]";

        RectangleErrorPoints = ToPointCollection(IntegrationService.MapConvergenceToScreen(
            _lastConvergenceSeries[0].Points, maxLinearError, minLogError, maxLogError, UseLogScale, CanvasWidth, CanvasHeight));

        TrapezoidErrorPoints = ToPointCollection(IntegrationService.MapConvergenceToScreen(
            _lastConvergenceSeries[1].Points, maxLinearError, minLogError, maxLogError, UseLogScale, CanvasWidth, CanvasHeight));

        SimpsonErrorPoints = ToPointCollection(IntegrationService.MapConvergenceToScreen(
            _lastConvergenceSeries[2].Points, maxLinearError, minLogError, maxLogError, UseLogScale, CanvasWidth, CanvasHeight));
    }

    private static PointCollection ToPointCollection(IReadOnlyList<PointD> screenPoints)
    {
        var collection = new PointCollection(screenPoints.Count);
        for (int i = 0; i < screenPoints.Count; i++)
        {
            collection.Add(new Point(screenPoints[i].X, screenPoints[i].Y));
        }

        return collection;
    }

    private static string FormatBound(double value) =>
        Math.Abs(value - Math.PI) < 1e-12
            ? value.ToString("0.########", CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);

    private static bool TryParseDouble(string? text, out double result)
    {
        result = 0.0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
               && double.IsFinite(result);
    }

    private static bool TryParsePositiveInt(string? text, out int result)
    {
        result = 0;
        return !string.IsNullOrWhiteSpace(text) &&
               int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) &&
               result > 0;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
