namespace HealthTracker.Services;

using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;

public class SettingsService(ISettingsRepository repository)
{
    public Task<AppSettings> Load(CancellationToken ct = default) =>
        repository.Load(ct);

    public async Task Save(AppSettings settings, CancellationToken ct = default)
    {
        ValidateThresholds(settings.Thresholds);
        await repository.Save(settings, ct);
    }

    private static void ValidateThresholds(ThresholdSettings t)
    {
        if (t.BpSystolicUpperMmhg is <= 0)
            throw new ValidationException("Systolic threshold must be a positive value.");

        if (t.BpDiastolicUpperMmhg is <= 0)
            throw new ValidationException("Diastolic threshold must be a positive value.");

        if (t.BloodSugarWarningMmolL is <= 0)
            throw new ValidationException("Blood sugar warning threshold must be a positive value.");

        if (t.BloodSugarDangerMmolL is <= 0)
            throw new ValidationException("Blood sugar danger threshold must be a positive value.");

        if (t.BloodSugarLowerMmolL is <= 0)
            throw new ValidationException("Blood sugar lower limit must be a positive value.");
    }
}
