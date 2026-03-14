namespace HealthTracker.Services;

using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Enums;

/// <summary>
/// Pure evaluation logic for health threshold breaches.
/// No I/O, no state — trivially testable and reusable across layers.
/// </summary>
public static class ThresholdEvaluator
{
    public static ThresholdLevel EvaluateBloodPressure(int systolic, int diastolic, ThresholdSettings thresholds)
    {
        var systolicBreached = thresholds.BpSystolicUpperMmhg is not null && systolic > thresholds.BpSystolicUpperMmhg;
        var diastolicBreached = thresholds.BpDiastolicUpperMmhg is not null && diastolic > thresholds.BpDiastolicUpperMmhg;

        return systolicBreached || diastolicBreached ? ThresholdLevel.Danger : ThresholdLevel.None;
    }

    public static ThresholdLevel EvaluateBloodSugar(decimal averageReading, ThresholdSettings thresholds)
    {
        if (thresholds.BloodSugarLowerMmolL is not null && averageReading < thresholds.BloodSugarLowerMmolL)
            return ThresholdLevel.BelowLower;

        if (thresholds.BloodSugarDangerMmolL is not null && averageReading >= thresholds.BloodSugarDangerMmolL)
            return ThresholdLevel.Danger;

        if (thresholds.BloodSugarWarningMmolL is not null && averageReading >= thresholds.BloodSugarWarningMmolL)
            return ThresholdLevel.Warning;

        return ThresholdLevel.None;
    }
}
