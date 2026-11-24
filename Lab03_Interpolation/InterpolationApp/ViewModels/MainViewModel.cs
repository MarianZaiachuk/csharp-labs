using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using InterpolationApp.Commands;
using InterpolationApp.Models;

namespace InterpolationApp.ViewModels;

/// <summary>
/// ViewModel головного вікна: координує параметри табулювання, валідацію h1/h2,
/// поліморфне обчислення інтерполяції та побудову графіків функції й похибки.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private const double MarkerRadius = 4.0;

    private readonly Interpolation[] _interpolators =
    [
        new LinearInterpolation(),
        new HermiteInterpolation()
    ];

    private TestFunctionOption _selectedFunction;
    private string _h1Text = InterpolationExperiment.DefaultH1.ToString(CultureInfo.InvariantCulture);
    private string _h2Text = InterpolationExperiment.DefaultH2.ToString(CultureInfo.InvariantCulture);
    private string _h1Error = string.Empty;
    private string _h2Error = string.Empty;
    private bool _useNonUniformKnots;

    private bool _showReference = true;
    private bool _showSupportPoints = true;
    private bool _showLinear = true;
    private bool _showHermite = true;

    private double _mainCanvasWidth = 640.0;
    private double _mainCanvasHeight = 360.0;
    private double _mainZeroAxisY = 180.0;
    private double _errorCanvasWidth = 640.0;
    private double _errorCanvasHeight = 200.0;
    private double _errorBaselineY = 176.0;

    private PointCollection _referencePoints = new();
    private PointCollection _linearPoints = new();
    private PointCollection _hermitePoints = new();
    private PointCollection _linearErrorPoints = new();
    private PointCollection _hermiteErrorPoints = new();
    private string _errorSummaryText = string.Empty;
    private string _statusText = string.Empty;

    private IReadOnlyList<PointD> _modelSupportPoints = Array.Empty<PointD>();
    private IReadOnlyList<PointD> _modelReferencePoints = Array.Empty<PointD>();
    private InterpolationSeriesResult? _linearResult;
    private InterpolationSeriesResult? _hermiteResult;
    private double _rangeMinX;
    private double _rangeMaxX;

    public MainViewModel()
    {
        Functions =
        [
            new TestFunctionOption("sin(x) на [0, 2π]", 0.0, 2.0 * Math.PI, Math.Sin),
            new TestFunctionOption(
                "cos(x) + 0.5·sin(2x) на [0, 2π]",
                0.0,
                2.0 * Math.PI,
                x => Math.Cos(x) + 0.5 * Math.Sin(2.0 * x)),
            new TestFunctionOption(
                "x·sin(x) на [0, 2π]",
                0.0,
                2.0 * Math.PI,
                x => x * Math.Sin(x))
        ];
        _selectedFunction = Functions[0];

        SupportMarkers = [];
        BuildCommand = new RelayCommand(ValidateAndCompute);

        ValidateAndCompute();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<TestFunctionOption> Functions { get; }

    public ObservableCollection<SupportMarker> SupportMarkers { get; }

    public ICommand BuildCommand { get; }

    public TestFunctionOption SelectedFunction
    {
        get => _selectedFunction;
        set
        {
            if (SetField(ref _selectedFunction, value))
            {
                ValidateAndCompute();
            }
        }
    }

    public string H1Text
    {
        get => _h1Text;
        set
        {
            if (SetField(ref _h1Text, value))
            {
                ValidateAndCompute();
            }
        }
    }

    public string H2Text
    {
        get => _h2Text;
        set
        {
            if (SetField(ref _h2Text, value))
            {
                ValidateAndCompute();
            }
        }
    }

    public string H1Error
    {
        get => _h1Error;
        private set => SetField(ref _h1Error, value);
    }

    public string H2Error
    {
        get => _h2Error;
        private set => SetField(ref _h2Error, value);
    }

    public bool UseNonUniformKnots
    {
        get => _useNonUniformKnots;
        set
        {
            if (SetField(ref _useNonUniformKnots, value))
            {
                ValidateAndCompute();
            }
        }
    }

    public bool ShowReference
    {
        get => _showReference;
        set => SetField(ref _showReference, value);
    }

    public bool ShowSupportPoints
    {
        get => _showSupportPoints;
        set => SetField(ref _showSupportPoints, value);
    }

    public bool ShowLinear
    {
        get => _showLinear;
        set => SetField(ref _showLinear, value);
    }

    public bool ShowHermite
    {
        get => _showHermite;
        set => SetField(ref _showHermite, value);
    }

    public double MainCanvasWidth
    {
        get => _mainCanvasWidth;
        private set => SetField(ref _mainCanvasWidth, value);
    }

    public double MainCanvasHeight
    {
        get => _mainCanvasHeight;
        private set => SetField(ref _mainCanvasHeight, value);
    }

    public double MainZeroAxisY
    {
        get => _mainZeroAxisY;
        private set => SetField(ref _mainZeroAxisY, value);
    }

    public double ErrorCanvasWidth
    {
        get => _errorCanvasWidth;
        private set => SetField(ref _errorCanvasWidth, value);
    }

    public double ErrorCanvasHeight
    {
        get => _errorCanvasHeight;
        private set => SetField(ref _errorCanvasHeight, value);
    }

    public double ErrorBaselineY
    {
        get => _errorBaselineY;
        private set => SetField(ref _errorBaselineY, value);
    }

    public PointCollection ReferencePoints
    {
        get => _referencePoints;
        private set => SetField(ref _referencePoints, value);
    }

    public PointCollection LinearPoints
    {
        get => _linearPoints;
        private set => SetField(ref _linearPoints, value);
    }

    public PointCollection HermitePoints
    {
        get => _hermitePoints;
        private set => SetField(ref _hermitePoints, value);
    }

    public PointCollection LinearErrorPoints
    {
        get => _linearErrorPoints;
        private set => SetField(ref _linearErrorPoints, value);
    }

    public PointCollection HermiteErrorPoints
    {
        get => _hermiteErrorPoints;
        private set => SetField(ref _hermiteErrorPoints, value);
    }

    public string ErrorSummaryText
    {
        get => _errorSummaryText;
        private set => SetField(ref _errorSummaryText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public void UpdateMainCanvasSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        MainCanvasWidth = width;
        MainCanvasHeight = height;
        ProjectMainPlot();
    }

    public void UpdateErrorCanvasSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        ErrorCanvasWidth = width;
        ErrorCanvasHeight = height;
        ProjectErrorPlot();
    }

    private void ValidateAndCompute()
    {
        double intervalLength = SelectedFunction.EndX - SelectedFunction.StartX;

        bool h1Valid = TryParseDouble(_h1Text, out double h1) &&
                       h1 >= InterpolationExperiment.MinH1 &&
                       h1 <= InterpolationExperiment.MaxH1 &&
                       h1 < intervalLength;

        H1Error = h1Valid
            ? string.Empty
            : $"Крок h₁ має бути в межах [{InterpolationExperiment.MinH1}; {Math.Min(InterpolationExperiment.MaxH1, intervalLength - 0.01):0.##}].";

        bool h2Valid = false;
        string h2Error = string.Empty;

        if (!TryParseDouble(_h2Text, out double h2) || h2 < InterpolationExperiment.MinStep)
        {
            h2Error = $"Крок h₂ має бути не менше {InterpolationExperiment.MinStep}.";
        }
        else if (h1Valid && h2 >= h1)
        {
            h2Error = "Крок h₂ має бути строго меншим за h₁ (h₂ < h₁).";
        }
        else if (Math.Ceiling(intervalLength / h2) + 2 > InterpolationExperiment.MaxPoints)
        {
            h2Error = $"Занадто малий крок: перевищено ліміт {InterpolationExperiment.MaxPoints} точок.";
        }
        else
        {
            h2Valid = true;
        }

        H2Error = h2Error;

        if (!h1Valid || !h2Valid)
        {
            StatusText = "Виправте некоректні кроки h₁ або h₂ для побудови графіків.";
            return;
        }

        try
        {
            _rangeMinX = SelectedFunction.StartX;
            _rangeMaxX = SelectedFunction.EndX;

            _modelSupportPoints = InterpolationExperiment.GenerateSupportPoints(
                SelectedFunction.Function,
                _rangeMinX,
                _rangeMaxX,
                h1,
                UseNonUniformKnots);

            IReadOnlyList<double> fineGrid = InterpolationExperiment.GenerateFineGrid(
                _rangeMinX,
                _rangeMaxX,
                h2);

            var refPoints = new List<PointD>(fineGrid.Count);
            for (int i = 0; i < fineGrid.Count; i++)
            {
                double x = fineGrid[i];
                refPoints.Add(new PointD(x, SelectedFunction.Function(x)));
            }
            _modelReferencePoints = refPoints;

            IReadOnlyList<InterpolationSeriesResult> results = InterpolationExperiment.EvaluateAll(
                _interpolators,
                _modelSupportPoints,
                fineGrid,
                SelectedFunction.Function);

            _linearResult = results[0];
            _hermiteResult = results[1];

            ProjectMainPlot();
            ProjectErrorPlot();

            string knotsType = UseNonUniformKnots ? "нерівновіддалені" : "рівновіддалені";
            ErrorSummaryText =
                $"Макс. похибка — Лінійна: {_linearResult.MaxAbsoluteError:0.#####} | Ерміт: {_hermiteResult.MaxAbsoluteError:0.#####}";
            StatusText =
                $"Опорних точок A1: {_modelSupportPoints.Count} ({knotsType}) | Точок A2: {fineGrid.Count} | " +
                $"{ErrorSummaryText}";
        }
        catch (ArgumentException ex)
        {
            H2Error = ex.Message;
            StatusText = ex.Message;
        }
    }

    private void ProjectMainPlot()
    {
        if (_modelReferencePoints.Count == 0 || _linearResult is null || _hermiteResult is null)
        {
            return;
        }

        (double minY, double maxY) = ComputeYRange(
            _modelReferencePoints,
            _linearResult.CurvePoints,
            _hermiteResult.CurvePoints,
            _modelSupportPoints);

        double span = maxY - minY;
        double margin = span > 1e-9 ? span * 0.05 : 1.0;
        minY -= margin;
        maxY += margin;

        MainZeroAxisY = InterpolationExperiment.MapYToScreen(0.0, minY, maxY, MainCanvasHeight);

        ReferencePoints = ToPointCollection(InterpolationExperiment.MapToScreen(
            _modelReferencePoints, _rangeMinX, _rangeMaxX, minY, maxY, MainCanvasWidth, MainCanvasHeight));

        LinearPoints = ToPointCollection(InterpolationExperiment.MapToScreen(
            _linearResult.CurvePoints, _rangeMinX, _rangeMaxX, minY, maxY, MainCanvasWidth, MainCanvasHeight));

        HermitePoints = ToPointCollection(InterpolationExperiment.MapToScreen(
            _hermiteResult.CurvePoints, _rangeMinX, _rangeMaxX, minY, maxY, MainCanvasWidth, MainCanvasHeight));

        IReadOnlyList<PointD> screenKnots = InterpolationExperiment.MapToScreen(
            _modelSupportPoints, _rangeMinX, _rangeMaxX, minY, maxY, MainCanvasWidth, MainCanvasHeight);

        SupportMarkers.Clear();
        for (int i = 0; i < screenKnots.Count; i++)
        {
            SupportMarkers.Add(new SupportMarker(
                screenKnots[i].X - MarkerRadius,
                screenKnots[i].Y - MarkerRadius));
        }
    }

    private void ProjectErrorPlot()
    {
        if (_linearResult is null || _hermiteResult is null)
        {
            return;
        }

        double maxErr = Math.Max(
            Math.Max(_linearResult.MaxAbsoluteError, _hermiteResult.MaxAbsoluteError),
            1e-4);

        ErrorBaselineY = InterpolationExperiment.MapYToScreen(0.0, 0.0, maxErr, ErrorCanvasHeight);

        LinearErrorPoints = ToPointCollection(InterpolationExperiment.MapToScreen(
            _linearResult.ErrorPoints, _rangeMinX, _rangeMaxX, 0.0, maxErr, ErrorCanvasWidth, ErrorCanvasHeight));

        HermiteErrorPoints = ToPointCollection(InterpolationExperiment.MapToScreen(
            _hermiteResult.ErrorPoints, _rangeMinX, _rangeMaxX, 0.0, maxErr, ErrorCanvasWidth, ErrorCanvasHeight));
    }

    private static (double MinY, double MaxY) ComputeYRange(params IReadOnlyList<PointD>[] seriesList)
    {
        double minY = 0.0;
        double maxY = 0.0;

        foreach (IReadOnlyList<PointD> series in seriesList)
        {
            for (int i = 0; i < series.Count; i++)
            {
                double y = series[i].Y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        return (minY, maxY);
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
