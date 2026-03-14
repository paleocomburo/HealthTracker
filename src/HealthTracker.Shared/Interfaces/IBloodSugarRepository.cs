namespace HealthTracker.Shared.Interfaces;

using HealthTracker.Shared.Dtos;

public interface IBloodSugarRepository
{
    Task<IReadOnlyList<BloodSugarEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<BloodSugarEntry> Add(BloodSugarEntry entry, CancellationToken ct = default);
    Task Update(BloodSugarEntry entry, CancellationToken ct = default);
    Task Delete(Guid id, DateOnly entryDate, CancellationToken ct = default);
}
