using LissajousApp.Models;

namespace LissajousApp.Tests;

public class LissajousModelTests
{
    private readonly LissajousModel _model = new();

    [Fact]
    public void SameFrequencyAndPhase_ProducesDiagonalLine()
    {
        var parameters = new LissajousParameters(
            Ax: 3.0,
            Ay: 3.0,
            Fx: 2.0,
            Fy: 2.0,
            PhiXDeg: 0.0,
            PhiYDeg: 0.0,
            TimeStep: 0.005);

        IReadOnlyList<PointD> points = _model.GeneratePoints(parameters);

        Assert.NotEmpty(points);
        Assert.All(points, p => Assert.True(Math.Abs(p.X - p.Y) < 1e-9));
    }

    [Fact]
    public void SameFrequencyAnd90DegreeShift_EqualAmplitudes_ProducesCircle()
    {
        var parameters = new LissajousParameters(
            Ax: 3.0,
            Ay: 3.0,
            Fx: 2.0,
            Fy: 2.0,
            PhiXDeg: 0.0,
            PhiYDeg: 90.0,
            TimeStep: 0.005);

        IReadOnlyList<PointD> points = _model.GeneratePoints(parameters);

        Assert.NotEmpty(points);
        Assert.All(points, p =>
        {
            double radius = Math.Sqrt(p.X * p.X + p.Y * p.Y);
            Assert.True(Math.Abs(radius - 3.0) < 1e-9);
        });
    }

    [Fact]
    public void MapToScreen_UsesUniformScalingAndFlipsY()
    {
        PointD[] cardinalPoints =
        [
            new PointD(3.0, 0.0),
            new PointD(-3.0, 0.0),
            new PointD(0.0, 3.0),
            new PointD(0.0, -3.0)
        ];

        IReadOnlyList<PointD> screenPoints = LissajousModel.MapToScreen(
            cardinalPoints,
            maxAmplitudeX: 3.0,
            maxAmplitudeY: 3.0,
            canvasWidth: 800.0,
            canvasHeight: 400.0,
            padding: 20.0);

        Assert.Equal(4, screenPoints.Count);

        double spanX = screenPoints[0].X - screenPoints[1].X;
        double spanY = screenPoints[3].Y - screenPoints[2].Y;

        Assert.True(Math.Abs(spanX - spanY) < 1e-9);
        Assert.True(screenPoints[2].Y < 200.0);
        Assert.True(screenPoints[3].Y > 200.0);
    }

    [Fact]
    public void DecreasingTimeStep_ImprovesSmoothnessWithoutChangingShape()
    {
        var coarseParams = new LissajousParameters(3.0, 4.0, 2.0, 3.0, 0.0, 90.0, 0.01);
        var fineParams = new LissajousParameters(3.0, 4.0, 2.0, 3.0, 0.0, 90.0, 0.002);

        IReadOnlyList<PointD> coarse = _model.GeneratePoints(coarseParams);
        IReadOnlyList<PointD> fine = _model.GeneratePoints(fineParams);

        Assert.True(fine.Count > coarse.Count);
        Assert.All(fine, p =>
        {
            Assert.True(Math.Abs(p.X) <= 3.0 + 1e-9);
            Assert.True(Math.Abs(p.Y) <= 4.0 + 1e-9);
        });

        for (int i = 0; i < coarse.Count - 1; i++)
        {
            PointD expected = coarse[i];
            PointD actual = fine[i * 5];
            Assert.True(Math.Abs(expected.X - actual.X) < 1e-9);
            Assert.True(Math.Abs(expected.Y - actual.Y) < 1e-9);
        }
    }

    [Fact]
    public void BoundaryMinPointsPerPeriod_IsAccepted()
    {
        var boundaryParams = new LissajousParameters(1.0, 1.0, 1.0, 1.0, 0.0, 90.0, 0.05);
        IReadOnlyList<PointD> points = _model.GeneratePoints(boundaryParams);

        Assert.Equal(21, points.Count);
    }

    [Theory]
    [InlineData(0.0, 3.0, 2.0, 3.0, 0.01)]
    [InlineData(3.0, -1.0, 2.0, 3.0, 0.01)]
    [InlineData(3.0, 3.0, 0.0, 3.0, 0.01)]
    [InlineData(3.0, 3.0, 2.0, -2.0, 0.01)]
    [InlineData(3.0, 3.0, 2.0, 3.0, 0.0)]
    [InlineData(3.0, 3.0, 2.0, 3.0, -0.01)]
    [InlineData(3.0, 3.0, 1.0, 1.0, 0.06)]
    public void InvalidParameters_ThrowArgumentException(
        double ax,
        double ay,
        double fx,
        double fy,
        double dt)
    {
        var parameters = new LissajousParameters(ax, ay, fx, fy, 0.0, 90.0, dt);
        Assert.Throws<ArgumentException>(() => _model.GeneratePoints(parameters));
    }

    [Theory]
    [InlineData(1.0, 1.0, 1.0)]
    [InlineData(1.0, 2.0, 1.0)]
    [InlineData(2.0, 3.0, 1.0)]
    [InlineData(3.0, 4.0, 1.0)]
    [InlineData(0.5, 1.0, 2.0)]
    [InlineData(0.4, 0.6, 5.0)]
    public void CalculateClosedDuration_ReturnsExpectedCommonPeriod(
        double fx,
        double fy,
        double expectedDuration)
    {
        double duration = LissajousModel.CalculateClosedDuration(fx, fy);
        Assert.Equal(expectedDuration, duration, 6);
    }
}
