namespace HealthTracker.Shared.Dtos;

public record ThresholdSettings(
    int? BpSystolicUpperMmhg,
    int? BpDiastolicUpperMmhg,
    decimal? BloodSugarWarningMmolL,
    decimal? BloodSugarDangerMmolL,
    decimal? BloodSugarLowerMmolL)
{
    public static ThresholdSettings Empty { get; } = new(null, null, null, null, null);
}
