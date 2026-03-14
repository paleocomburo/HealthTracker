namespace HealthTracker.Shared.Interfaces;

using HealthTracker.Shared.Dtos;

public interface IBloodPressureRepository
{
    Task<IReadOnlyList<BloodPressureEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<BloodPressureEntry> Add(BloodPressureEntry entry, CancellationToken ct = default);
    Task Update(BloodPressureEntry entry, CancellationToken ct = default);
    Task Delete(Guid id, DateOnly entryDate, CancellationToken ct = default);
}
