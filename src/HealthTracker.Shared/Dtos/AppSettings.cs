namespace HealthTracker.Shared.Dtos;

public record AppSettings(decimal? TargetWeightKg, ThresholdSettings Thresholds)
{
    public static AppSettings Default { get; } = new(null, ThresholdSettings.Empty);
}
