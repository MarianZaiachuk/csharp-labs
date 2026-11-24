using IntegrationApp.Models;

namespace IntegrationApp.Tests;

public class IntegratorTests
{
    private static IIntegrator[] CreateStrategies() =>
    [
        new RectangleIntegrator(),
        new TrapezoidIntegrator(),
        new SimpsonIntegrator()
    ];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AllIntegrators_ComputeLinearFromZeroToOne_EqualsHalf(int strategyIndex)
    {
        IIntegrator integrator = CreateStrategies()[strategyIndex];
        double result = integrator.Integrate(x => x, 0.0, 1.0, intervals: 10);

        Assert.Equal(0.5, result, 12);
    }

    [Theory]
    [InlineData(0, 1e-3)]
    [InlineData(1, 1e-3)]
    [InlineData(2, 1e-6)]
    public void AllIntegrators_ComputeSinFromZeroToPi_ApproximatesTwo(int strategyIndex, double tolerance)
    {
        IIntegrator integrator = CreateStrategies()[strategyIndex];
        double result = integrator.Integrate(Math.Sin, 0.0, Math.PI, intervals: 100);

        Assert.True(Math.Abs(result - 2.0) < tolerance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AllIntegrators_ReturnNegativeIntegral_WhenBoundsAreSwapped(int strategyIndex)
    {
        IIntegrator integrator = CreateStrategies()[strategyIndex];

        double forwardSin = integrator.Integrate(Math.Sin, 0.0, Math.PI, intervals: 40);
        double reversedSin = integrator.Integrate(Math.Sin, Math.PI, 0.0, intervals: 40);
        double reversedLinear = integrator.Integrate(x => x, 1.0, 0.0, intervals: 10);

        Assert.Equal(-forwardSin, reversedSin, 12);
        Assert.True(reversedSin < 0.0);
        Assert.Equal(-0.5, reversedLinear, 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AllIntegrators_ReturnZero_WhenBoundsAreEqual(int strategyIndex)
    {
        IIntegrator integrator = CreateStrategies()[strategyIndex];
        Assert.Equal(0.0, integrator.Integrate(Math.Sin, 1.5, 1.5, intervals: 10), 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void DoublingIntervals_ReducesError_ForAllMethods(int strategyIndex)
    {
        IIntegrator integrator = CreateStrategies()[strategyIndex];
        int[] steps = [10, 20, 40, 80, 160];
        double previousError = double.MaxValue;

        foreach (int n in steps)
        {
            double approx = integrator.Integrate(Math.Sin, 0.0, Math.PI, n);
            double error = Math.Abs(approx - 2.0);
            Assert.True(error < previousError);
            previousError = error;
        }
    }

    [Fact]
    public void SimpsonIntegrator_IsExactForCubicPolynomial()
    {
        IIntegrator simpson = new SimpsonIntegrator();
        double result = simpson.Integrate(x => x * x * x, 0.0, 2.0, intervals: 2);

        Assert.Equal(4.0, result, 12);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(15)]
    public void SimpsonIntegrator_RejectsOddIntervals(int oddIntervals)
    {
        IIntegrator simpson = new SimpsonIntegrator();
        Assert.Throws<ArgumentException>(() => simpson.Integrate(Math.Sin, 0.0, Math.PI, oddIntervals));
    }

    [Fact]
    public void AllIntegrators_RejectInvalidInputs()
    {
        foreach (IIntegrator integrator in CreateStrategies())
        {
            Assert.Throws<ArgumentNullException>(() => integrator.Integrate(null!, 0.0, 1.0, 10));
            Assert.Throws<ArgumentException>(() => integrator.Integrate(Math.Sin, double.NaN, 1.0, 10));
            Assert.Throws<ArgumentException>(() => integrator.Integrate(Math.Sin, 0.0, double.PositiveInfinity, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => integrator.Integrate(Math.Sin, 0.0, 1.0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => integrator.Integrate(Math.Sin, 0.0, 1.0, -4));
        }
    }

    [Fact]
    public void IntegrationService_CompareAll_HandlesOddIntervalsGracefully()
    {
        var sinFunc = new KnownFunction("sin(x)", 0.0, Math.PI, Math.Sin, x => -Math.Cos(x));
        IReadOnlyList<IntegrationComparisonRow> rows = IntegrationService.CompareAll(
            CreateStrategies(),
            sinFunc,
            0.0,
            Math.PI,
            intervals: 15);

        Assert.Equal(3, rows.Count);
        Assert.True(rows[0].IsSuccess);
        Assert.True(rows[1].IsSuccess);
        Assert.False(rows[2].IsSuccess);
        Assert.Contains("парної", rows[2].StatusNote);
    }
}
