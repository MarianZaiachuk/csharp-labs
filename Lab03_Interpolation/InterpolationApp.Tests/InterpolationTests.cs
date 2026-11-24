using InterpolationApp.Models;

namespace InterpolationApp.Tests;

public class InterpolationTests
{
    private static Interpolation[] CreatePolymorphicMethods() =>
    [
        new LinearInterpolation(),
        new HermiteInterpolation()
    ];

    [Fact]
    public void BothInterpolators_ReturnExactValuesAtKnots_Equidistant()
    {
        IReadOnlyList<PointD> knots = InterpolationExperiment.GenerateSupportPoints(
            Math.Sin,
            0.0,
            2.0 * Math.PI,
            h1: 0.8,
            nonUniform: false);

        foreach (Interpolation method in CreatePolymorphicMethods())
        {
            foreach (PointD knot in knots)
            {
                double interpolated = method.Calculate(knots, knot.X);
                Assert.True(Math.Abs(interpolated - knot.Y) < 1e-12);
            }
        }
    }

    [Fact]
    public void BothInterpolators_ReturnExactValuesAtKnots_NonEquidistant()
    {
        PointD[] nonUniformKnots =
        [
            new PointD(0.0, Math.Sin(0.0)),
            new PointD(0.45, Math.Sin(0.45)),
            new PointD(1.3, Math.Sin(1.3)),
            new PointD(2.9, Math.Sin(2.9)),
            new PointD(4.2, Math.Sin(4.2)),
            new PointD(2.0 * Math.PI, Math.Sin(2.0 * Math.PI))
        ];

        foreach (Interpolation method in CreatePolymorphicMethods())
        {
            foreach (PointD knot in nonUniformKnots)
            {
                double interpolated = method.Calculate(nonUniformKnots, knot.X);
                Assert.True(Math.Abs(interpolated - knot.Y) < 1e-12);
            }

            double interior = method.Calculate(nonUniformKnots, 1.8);
            Assert.True(double.IsFinite(interior));
        }
    }

    [Fact]
    public void LinearInterpolation_CalculatesExactMidpointOnSegment()
    {
        Interpolation linear = new LinearInterpolation();
        PointD[] knots =
        [
            new PointD(1.0, 2.0),
            new PointD(3.0, 6.0),
            new PointD(7.0, -2.0)
        ];

        Assert.Equal(4.0, linear.Calculate(knots, 2.0), 12);
        Assert.Equal(2.0, linear.Calculate(knots, 5.0), 12);
    }

    [Fact]
    public void HermiteInterpolation_HasLowerMaxErrorThanLinear_OnSinFunction()
    {
        IReadOnlyList<PointD> supportPoints = InterpolationExperiment.GenerateSupportPoints(
            Math.Sin,
            0.0,
            2.0 * Math.PI,
            h1: 0.8,
            nonUniform: false);
        IReadOnlyList<double> fineGrid = InterpolationExperiment.GenerateFineGrid(0.0, 2.0 * Math.PI, h2: 0.05);

        IReadOnlyList<InterpolationSeriesResult> results = InterpolationExperiment.EvaluateAll(
            CreatePolymorphicMethods(),
            supportPoints,
            fineGrid,
            Math.Sin);

        Assert.Equal(2, results.Count);
        double linearMaxErr = results[0].MaxAbsoluteError;
        double hermiteMaxErr = results[1].MaxAbsoluteError;

        Assert.True(hermiteMaxErr < linearMaxErr);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void BothInterpolators_RejectDuplicateX(int methodIndex)
    {
        Interpolation method = CreatePolymorphicMethods()[methodIndex];
        PointD[] duplicateXPoints =
        [
            new PointD(0.0, 0.0),
            new PointD(1.0, 2.0),
            new PointD(1.0, 3.0),
            new PointD(2.0, 4.0)
        ];

        Assert.Throws<ArgumentException>(() => method.Calculate(duplicateXPoints, 0.5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void BothInterpolators_RejectUnsortedX(int methodIndex)
    {
        Interpolation method = CreatePolymorphicMethods()[methodIndex];
        PointD[] unsortedPoints =
        [
            new PointD(0.0, 0.0),
            new PointD(2.0, 1.0),
            new PointD(1.5, 2.0)
        ];

        Assert.Throws<ArgumentException>(() => method.Calculate(unsortedPoints, 1.0));
    }

    [Fact]
    public void BothInterpolators_RejectTooFewPointsAndOutOfRangeX()
    {
        PointD[] singlePoint = [new PointD(0.0, 1.0)];
        PointD[] validPoints = [new PointD(0.0, 0.0), new PointD(2.0, 4.0)];

        foreach (Interpolation method in CreatePolymorphicMethods())
        {
            Assert.Throws<ArgumentException>(() => method.Calculate(singlePoint, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => method.Calculate(validPoints, -0.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => method.Calculate(validPoints, 2.1));
        }
    }

    [Fact]
    public void EvaluateAll_RunsPolymorphicallyAcrossInterpolationArray()
    {
        IReadOnlyList<PointD> supportPoints = InterpolationExperiment.GenerateSupportPoints(
            Math.Sin,
            0.0,
            Math.PI,
            h1: 0.7,
            nonUniform: true);
        IReadOnlyList<double> fineGrid = InterpolationExperiment.GenerateFineGrid(0.0, Math.PI, h2: 0.05);

        IReadOnlyList<InterpolationSeriesResult> results = InterpolationExperiment.EvaluateAll(
            CreatePolymorphicMethods(),
            supportPoints,
            fineGrid,
            Math.Sin);

        Assert.Equal(2, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal(fineGrid.Count, r.CurvePoints.Count);
            Assert.Equal(fineGrid.Count, r.ErrorPoints.Count);
            Assert.True(r.MaxAbsoluteError >= 0.0);
        });
    }
}
