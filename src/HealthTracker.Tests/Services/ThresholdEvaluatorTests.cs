namespace HealthTracker.Tests.Services;

using AwesomeAssertions;
using HealthTracker.Services;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Enums;
using Xunit;

public class ThresholdEvaluatorTests
{
    private static ThresholdSettings NoThresholds => new(null, null, null, null, null);

    [Fact]
    public void EvaluateBloodPressure_NoThresholds_ReturnsNone()
    {
        var result = ThresholdEvaluator.EvaluateBloodPressure(180, 110, NoThresholds);

        result.Should().Be(ThresholdLevel.None);
    }

    [Fact]
    public void EvaluateBloodPressure_BelowBothThresholds_ReturnsNone()
    {
        var thresholds = new ThresholdSettings(140, 90, null, null, null);

        var result = ThresholdEvaluator.EvaluateBloodPressure(120, 80, thresholds);

        result.Should().Be(ThresholdLevel.None);
    }

    [Fact]
    public void EvaluateBloodPressure_SystolicExceedsThreshold_ReturnsDanger()
    {
        var thresholds = new ThresholdSettings(140, 90, null, null, null);

        var result = ThresholdEvaluator.EvaluateBloodPressure(150, 80, thresholds);

        result.Should().Be(ThresholdLevel.Danger);
    }

    [Fact]
    public void EvaluateBloodPressure_DiastolicExceedsThreshold_ReturnsDanger()
    {
        var thresholds = new ThresholdSettings(140, 90, null, null, null);

        var result = ThresholdEvaluator.EvaluateBloodPressure(130, 95, thresholds);

        result.Should().Be(ThresholdLevel.Danger);
    }

    [Fact]
    public void EvaluateBloodSugar_NoThresholds_ReturnsNone()
    {
        var result = ThresholdEvaluator.EvaluateBloodSugar(15m, NoThresholds);

        result.Should().Be(ThresholdLevel.None);
    }

    [Fact]
    public void EvaluateBloodSugar_BelowLowerLimit_ReturnsBelowLower()
    {
        var thresholds = new ThresholdSettings(null, null, 7.0m, 11.0m, 4.0m);

        var result = ThresholdEvaluator.EvaluateBloodSugar(3.5m, thresholds);

        result.Should().Be(ThresholdLevel.BelowLower);
    }

    [Fact]
    public void EvaluateBloodSugar_AboveDangerThreshold_ReturnsDanger()
    {
        var thresholds = new ThresholdSettings(null, null, 7.0m, 11.0m, 4.0m);

        var result = ThresholdEvaluator.EvaluateBloodSugar(12.0m, thresholds);

        result.Should().Be(ThresholdLevel.Danger);
    }

    [Fact]
    public void EvaluateBloodSugar_AboveWarningBelowDanger_ReturnsWarning()
    {
        var thresholds = new ThresholdSettings(null, null, 7.0m, 11.0m, 4.0m);

        var result = ThresholdEvaluator.EvaluateBloodSugar(8.5m, thresholds);

        result.Should().Be(ThresholdLevel.Warning);
    }

    [Fact]
    public void EvaluateBloodSugar_WithinNormalRange_ReturnsNone()
    {
        var thresholds = new ThresholdSettings(null, null, 7.0m, 11.0m, 4.0m);

        var result = ThresholdEvaluator.EvaluateBloodSugar(5.5m, thresholds);

        result.Should().Be(ThresholdLevel.None);
    }
}
