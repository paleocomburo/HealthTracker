namespace HealthTracker.Shared.Interfaces;

using HealthTracker.Shared.Dtos;

public interface IWeightRepository
{
    Task<IReadOnlyList<WeightEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<WeightEntry> Add(WeightEntry entry, CancellationToken ct = default);
    Task Update(WeightEntry entry, CancellationToken ct = default);
    Task Delete(Guid id, DateOnly entryDate, CancellationToken ct = default);
}
