using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LissajousApp.Commands;
using LissajousApp.Models;

namespace LissajousApp.ViewModels;

/// <summary>
/// ViewModel головного вікна: керує параметрами фігури Ліссажу,
/// валідацією введених даних та перетворенням точок у координати полотна.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private const double MinSliderTimeStep = 0.0005;
    private const double StepComparisonTolerance = 1e-12;

    private readonly LissajousModel _model = new();

    private double _ax = 3.0;
    private double _ay = 3.0;
    private double _fx = 2.0;
    private double _fy = 3.0;
    private double _phiX = 0.0;
    private double _phiY = 90.0;
    private double _timeStep = 0.003;

    private string _axText = "3";
    private string _ayText = "3";
    private string _fxText = "2";
    private string _fyText = "3";
    private string _phiXText = "0";
    private string _phiYText = "90";
    private string _timeStepText = "0.003";

    private string _axError = string.Empty;
    private string _ayError = string.Empty;
    private string _fxError = string.Empty;
    private string _fyError = string.Empty;
    private string _phiXError = string.Empty;
    private string _phiYError = string.Empty;
    private string _timeStepError = string.Empty;

    private double _canvasWidth = 560.0;
    private double _canvasHeight = 480.0;
    private PointCollection _curvePoints = new();
    private string _statusText = string.Empty;

    public MainViewModel()
    {
        PlotCommand = new RelayCommand(ExecutePlot);
        AutoStepCommand = new RelayCommand(ExecuteAutoStep);
        ApplyPresetCommand = new RelayCommand(ExecutePreset);

        ValidateAndDraw();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand PlotCommand { get; }
    public ICommand AutoStepCommand { get; }
    public ICommand ApplyPresetCommand { get; }

    public string AxText
    {
        get => _axText;
        set => SetTextField(ref _axText, value, nameof(AxText), nameof(AxSlider), ValidateAndDraw);
    }

    public double AxSlider
    {
        get => _ax;
        set => SetSliderField(ref _ax, ref _axText, value, 2, nameof(AxSlider), nameof(AxText));
    }

    public string AxError
    {
        get => _axError;
        private set => SetField(ref _axError, value);
    }

    public string AyText
    {
        get => _ayText;
        set => SetTextField(ref _ayText, value, nameof(AyText), nameof(AySlider), ValidateAndDraw);
    }

    public double AySlider
    {
        get => _ay;
        set => SetSliderField(ref _ay, ref _ayText, value, 2, nameof(AySlider), nameof(AyText));
    }

    public string AyError
    {
        get => _ayError;
        private set => SetField(ref _ayError, value);
    }

    public string FxText
    {
        get => _fxText;
        set => SetTextField(ref _fxText, value, nameof(FxText), nameof(FxSlider), () =>
        {
            OnPropertyChanged(nameof(MaxTimeStepHint));
            ValidateAndDraw();
        });
    }

    public double FxSlider
    {
        get => _fx;
        set => SetSliderField(ref _fx, ref _fxText, value, 2, nameof(FxSlider), nameof(FxText), () =>
            OnPropertyChanged(nameof(MaxTimeStepHint)));
    }

    public string FxError
    {
        get => _fxError;
        private set => SetField(ref _fxError, value);
    }

    public string FyText
    {
        get => _fyText;
        set => SetTextField(ref _fyText, value, nameof(FyText), nameof(FySlider), () =>
        {
            OnPropertyChanged(nameof(MaxTimeStepHint));
            ValidateAndDraw();
        });
    }

    public double FySlider
    {
        get => _fy;
        set => SetSliderField(ref _fy, ref _fyText, value, 2, nameof(FySlider), nameof(FyText), () =>
            OnPropertyChanged(nameof(MaxTimeStepHint)));
    }

    public string FyError
    {
        get => _fyError;
        private set => SetField(ref _fyError, value);
    }

    public string PhiXText
    {
        get => _phiXText;
        set => SetTextField(ref _phiXText, value, nameof(PhiXText), nameof(PhiXSlider), ValidateAndDraw);
    }

    public double PhiXSlider
    {
        get => _phiX;
        set => SetSliderField(ref _phiX, ref _phiXText, value, 1, nameof(PhiXSlider), nameof(PhiXText));
    }

    public string PhiXError
    {
        get => _phiXError;
        private set => SetField(ref _phiXError, value);
    }

    public string PhiYText
    {
        get => _phiYText;
        set => SetTextField(ref _phiYText, value, nameof(PhiYText), nameof(PhiYSlider), ValidateAndDraw);
    }

    public double PhiYSlider
    {
        get => _phiY;
        set => SetSliderField(ref _phiY, ref _phiYText, value, 1, nameof(PhiYSlider), nameof(PhiYText));
    }

    public string PhiYError
    {
        get => _phiYError;
        private set => SetField(ref _phiYError, value);
    }

    public string TimeStepText
    {
        get => _timeStepText;
        set => SetTextField(ref _timeStepText, value, nameof(TimeStepText), nameof(TimeStepSlider), ValidateAndDraw);
    }

    public double TimeStepSlider
    {
        get => _timeStep;
        set => SetSliderField(ref _timeStep, ref _timeStepText, value, 4, nameof(TimeStepSlider), nameof(TimeStepText));
    }

    public string TimeStepError
    {
        get => _timeStepError;
        private set => SetField(ref _timeStepError, value);
    }

    public string MaxTimeStepHint
    {
        get
        {
            if (!TryParseDouble(_fxText, out double fx) || fx < LissajousParameters.MinFrequency || fx > LissajousParameters.MaxFrequency ||
                !TryParseDouble(_fyText, out double fy) || fy < LissajousParameters.MinFrequency || fy > LissajousParameters.MaxFrequency)
            {
                return "Допустимий крок залежить від коректних частот fx та fy";
            }

            double maxStep = LissajousModel.GetMaxAllowedTimeStep(fx, fy);
            double recStep = LissajousModel.GetRecommendedTimeStep(fx, fy);
            return $"Макс. Δt ≤ {maxStep:0.####} с (рек. {recStep:0.####} с)";
        }
    }

    public double CanvasWidth
    {
        get => _canvasWidth;
        private set
        {
            if (SetField(ref _canvasWidth, value))
            {
                OnPropertyChanged(nameof(AxisCenterX));
            }
        }
    }

    public double CanvasHeight
    {
        get => _canvasHeight;
        private set
        {
            if (SetField(ref _canvasHeight, value))
            {
                OnPropertyChanged(nameof(AxisCenterY));
            }
        }
    }

    public double AxisCenterX => CanvasWidth / 2.0;
    public double AxisCenterY => CanvasHeight / 2.0;

    public PointCollection CurvePoints
    {
        get => _curvePoints;
        private set => SetField(ref _curvePoints, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public bool HasErrors =>
        !string.IsNullOrEmpty(AxError) ||
        !string.IsNullOrEmpty(AyError) ||
        !string.IsNullOrEmpty(FxError) ||
        !string.IsNullOrEmpty(FyError) ||
        !string.IsNullOrEmpty(PhiXError) ||
        !string.IsNullOrEmpty(PhiYError) ||
        !string.IsNullOrEmpty(TimeStepError);

    public void UpdateCanvasSize(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        CanvasWidth = width;
        CanvasHeight = height;

        if (!HasErrors)
        {
            RedrawCurrentFigure();
        }
    }

    private void ExecutePlot() => ValidateAndDraw();

    private void ExecuteAutoStep()
    {
        if (!TryValidateFrequency(_fxText, out double fx, out _) ||
            !TryValidateFrequency(_fyText, out double fy, out _))
        {
            ValidateAndDraw();
            return;
        }

        double maxStep = LissajousModel.GetMaxAllowedTimeStep(fx, fy);
        double recommended = Math.Round(LissajousModel.GetRecommendedTimeStep(fx, fy), 4);
        recommended = Math.Clamp(recommended, MinSliderTimeStep, maxStep);

        _timeStep = recommended;
        _timeStepText = FormatNumber(recommended);
        OnPropertyChanged(nameof(TimeStepSlider));
        OnPropertyChanged(nameof(TimeStepText));

        ValidateAndDraw();
    }

    private void ExecutePreset(object? parameter)
    {
        string? preset = parameter as string;
        if (string.IsNullOrWhiteSpace(preset))
        {
            return;
        }

        (double ax, double ay, double fx, double fy, double phiX, double phiY) = preset switch
        {
            "1:1-line" => (3.0, 3.0, 2.0, 2.0, 0.0, 0.0),
            "1:1-circle" => (3.0, 3.0, 2.0, 2.0, 0.0, 90.0),
            "1:2" => (3.0, 3.0, 1.0, 2.0, 0.0, 90.0),
            "2:3" => (3.0, 3.0, 2.0, 3.0, 0.0, 90.0),
            "3:4" => (3.0, 3.0, 3.0, 4.0, 0.0, 90.0),
            _ => (_ax, _ay, _fx, _fy, _phiX, _phiY)
        };

        double maxStep = LissajousModel.GetMaxAllowedTimeStep(fx, fy);
        double recommended = Math.Clamp(
            Math.Round(LissajousModel.GetRecommendedTimeStep(fx, fy), 4),
            MinSliderTimeStep,
            maxStep);

        SetNumericAndText(ref _ax, ref _axText, ax, nameof(AxSlider), nameof(AxText));
        SetNumericAndText(ref _ay, ref _ayText, ay, nameof(AySlider), nameof(AyText));
        SetNumericAndText(ref _fx, ref _fxText, fx, nameof(FxSlider), nameof(FxText));
        SetNumericAndText(ref _fy, ref _fyText, fy, nameof(FySlider), nameof(FyText));
        SetNumericAndText(ref _phiX, ref _phiXText, phiX, nameof(PhiXSlider), nameof(PhiXText));
        SetNumericAndText(ref _phiY, ref _phiYText, phiY, nameof(PhiYSlider), nameof(PhiYText));
        SetNumericAndText(ref _timeStep, ref _timeStepText, recommended, nameof(TimeStepSlider), nameof(TimeStepText));

        OnPropertyChanged(nameof(MaxTimeStepHint));
        ValidateAndDraw();
    }

    private void ValidateAndDraw()
    {
        bool axValid = TryValidateAmplitude(_axText, out double ax, out string axErr);
        bool ayValid = TryValidateAmplitude(_ayText, out double ay, out string ayErr);
        bool fxValid = TryValidateFrequency(_fxText, out double fx, out string fxErr);
        bool fyValid = TryValidateFrequency(_fyText, out double fy, out string fyErr);
        bool phiXValid = TryValidatePhase(_phiXText, out double phiX, out string phiXErr);
        bool phiYValid = TryValidatePhase(_phiYText, out double phiY, out string phiYErr);

        AxError = axErr;
        AyError = ayErr;
        FxError = fxErr;
        FyError = fyErr;
        PhiXError = phiXErr;
        PhiYError = phiYErr;

        bool stepValid = TryValidateTimeStep(_timeStepText, fxValid ? fx : null, fyValid ? fy : null, out double timeStep, out string stepErr);
        TimeStepError = stepErr;

        if (axValid) _ax = ax;
        if (ayValid) _ay = ay;
        if (fxValid) _fx = fx;
        if (fyValid) _fy = fy;
        if (phiXValid) _phiX = phiX;
        if (phiYValid) _phiY = phiY;
        if (stepValid) _timeStep = timeStep;

        OnPropertyChanged(nameof(HasErrors));

        if (!axValid || !ayValid || !fxValid || !fyValid || !phiXValid || !phiYValid || !stepValid)
        {
            StatusText = "Виправте некоректні параметри для побудови фігури.";
            return;
        }

        RedrawCurrentFigure();
    }

    private void RedrawCurrentFigure()
    {
        try
        {
            var parameters = new LissajousParameters(_ax, _ay, _fx, _fy, _phiX, _phiY, _timeStep);
            IReadOnlyList<PointD> modelPoints = _model.GeneratePoints(parameters);
            IReadOnlyList<PointD> screenPoints = LissajousModel.MapToScreen(
                modelPoints,
                _ax,
                _ay,
                CanvasWidth,
                CanvasHeight);

            var collection = new PointCollection(screenPoints.Count);
            for (int i = 0; i < screenPoints.Count; i++)
            {
                collection.Add(new Point(screenPoints[i].X, screenPoints[i].Y));
            }

            CurvePoints = collection;

            double fastestPeriod = LissajousModel.GetFastestPeriod(_fx, _fy);
            double pointsPerFastestPeriod = fastestPeriod / _timeStep;
            double totalDuration = LissajousModel.CalculateClosedDuration(_fx, _fy);
            double deltaPhi = Math.Abs(_phiY - _phiX);

            StatusText =
                $"Точок: {modelPoints.Count} | Тмін = {fastestPeriod:0.###} с | " +
                $"Точок/період: {pointsPerFastestPeriod:0.#} | Δφ = {deltaPhi:0.#}° | Tзаг = {totalDuration:0.##} с";
        }
        catch (ArgumentException ex)
        {
            TimeStepError = ex.Message;
            OnPropertyChanged(nameof(HasErrors));
            StatusText = ex.Message;
        }
    }

    private static bool TryValidateAmplitude(string? text, out double value, out string error)
    {
        if (!TryParseDouble(text, out value) ||
            value < LissajousParameters.MinAmplitude ||
            value > LissajousParameters.MaxAmplitude)
        {
            error = $"Амплітуда має бути додатним числом ({LissajousParameters.MinAmplitude}–{LissajousParameters.MaxAmplitude}).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateFrequency(string? text, out double value, out string error)
    {
        if (!TryParseDouble(text, out value) ||
            value < LissajousParameters.MinFrequency ||
            value > LissajousParameters.MaxFrequency)
        {
            error = $"Частота має бути додатним числом ({LissajousParameters.MinFrequency}–{LissajousParameters.MaxFrequency} Гц).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidatePhase(string? text, out double value, out string error)
    {
        if (!TryParseDouble(text, out value) ||
            value < LissajousParameters.MinPhaseDeg ||
            value > LissajousParameters.MaxPhaseDeg)
        {
            error = $"Фаза має бути числом від {LissajousParameters.MinPhaseDeg:0}° до {LissajousParameters.MaxPhaseDeg:0}°.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateTimeStep(
        string? text,
        double? fx,
        double? fy,
        out double value,
        out string error)
    {
        if (!TryParseDouble(text, out value) || value <= 0)
        {
            error = "Крок часу Δt має бути додатним числом.";
            return false;
        }

        if (fx.HasValue && fy.HasValue)
        {
            double maxAllowed = LissajousModel.GetMaxAllowedTimeStep(fx.Value, fy.Value);
            if (value > maxAllowed + StepComparisonTolerance)
            {
                error = $"Завеликий крок: для ≥ {LissajousParameters.MinPointsPerPeriod} точок/період потрібно Δt ≤ {maxAllowed:0.####} с.";
                return false;
            }

            double duration = LissajousModel.CalculateClosedDuration(fx.Value, fy.Value);
            if (Math.Ceiling(duration / value) > LissajousParameters.MaxTotalPoints)
            {
                error = $"Занадто малий крок: перевищено ліміт {LissajousParameters.MaxTotalPoints} точок.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseDouble(string? text, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
               && double.IsFinite(result);
    }

    private static string FormatNumber(double value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    private void SetTextField(
        ref string backingText,
        string newText,
        string textPropertyName,
        string sliderPropertyName,
        Action afterChange)
    {
        if (string.Equals(backingText, newText, StringComparison.Ordinal))
        {
            return;
        }

        backingText = newText;
        OnPropertyChanged(textPropertyName);
        afterChange();
        OnPropertyChanged(sliderPropertyName);
    }

    private void SetSliderField(
        ref double backingNumeric,
        ref string backingText,
        double newValue,
        int decimals,
        string sliderPropertyName,
        string textPropertyName,
        Action? extraNotify = null)
    {
        double rounded = Math.Round(newValue, decimals);
        if (Math.Abs(backingNumeric - rounded) < 1e-9 &&
            string.Equals(backingText, FormatNumber(rounded), StringComparison.Ordinal))
        {
            return;
        }

        backingNumeric = rounded;
        backingText = FormatNumber(rounded);
        OnPropertyChanged(sliderPropertyName);
        OnPropertyChanged(textPropertyName);
        extraNotify?.Invoke();
        ValidateAndDraw();
    }

    private void SetNumericAndText(
        ref double backingNumeric,
        ref string backingText,
        double value,
        string sliderPropertyName,
        string textPropertyName)
    {
        backingNumeric = value;
        backingText = FormatNumber(value);
        OnPropertyChanged(sliderPropertyName);
        OnPropertyChanged(textPropertyName);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
