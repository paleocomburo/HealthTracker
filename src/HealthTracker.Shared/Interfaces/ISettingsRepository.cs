namespace HealthTracker.Shared.Interfaces;

using HealthTracker.Shared.Dtos;

public interface ISettingsRepository
{
    Task<AppSettings> Load(CancellationToken ct = default);
    Task Save(AppSettings settings, CancellationToken ct = default);
}
