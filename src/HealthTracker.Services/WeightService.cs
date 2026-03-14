namespace HealthTracker.Services;

using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;

public class WeightService(IWeightRepository repository)
{
    public Task<IReadOnlyList<WeightEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        repository.GetEntries(from, to, ct);

    public async Task<WeightEntry> AddEntry(DateOnly date, decimal weightKg, CancellationToken ct = default)
    {
        Validate(date, weightKg);
        var entry = new WeightEntry(Guid.NewGuid(), date, weightKg);
        return await repository.Add(entry, ct);
    }

    public async Task UpdateEntry(WeightEntry entry, CancellationToken ct = default)
    {
        Validate(entry.Date, entry.WeightKg);
        await repository.Update(entry, ct);
    }

    public Task DeleteEntry(Guid id, DateOnly entryDate, CancellationToken ct = default) =>
        repository.Delete(id, entryDate, ct);

    /// <summary>
    /// Returns the date range that spans the last 10 entries, used to set the default view on open.
    /// </summary>
    public async Task<(DateOnly From, DateOnly To)> GetLast10DateRange(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        // Query a broad range (10 years back) to find the last 10 entries regardless of how spread out they are.
        var all = await repository.GetEntries(today.AddYears(-10), today, ct);

        if (all.Count == 0)
            return (today.AddMonths(-1), today);

        var last10 = all.OrderByDescending(e => e.Date).Take(10).ToList();
        return (last10[^1].Date, last10[0].Date);
    }

    private static void Validate(DateOnly date, decimal weightKg)
    {
        if (weightKg <= 0 || weightKg >= 500)
            throw new ValidationException("Weight must be between 0 and 500 kg.");

        if (date > DateOnly.FromDateTime(DateTime.Today))
            throw new ValidationException("Entry date cannot be in the future.");
    }
}
