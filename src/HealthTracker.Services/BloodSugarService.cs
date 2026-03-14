namespace HealthTracker.Services;

using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;

public class BloodSugarService(IBloodSugarRepository repository)
{
    private const string FastingContext = "fasting";

    public Task<IReadOnlyList<BloodSugarEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        repository.GetEntries(from, to, ct);

    public async Task<BloodSugarEntry> AddEntry(
        DateOnly date,
        IReadOnlyList<decimal> readings,
        CancellationToken ct = default)
    {
        Validate(date, readings);
        var entry = new BloodSugarEntry(Guid.NewGuid(), date, readings, FastingContext);
        return await repository.Add(entry, ct);
    }

    public async Task UpdateEntry(BloodSugarEntry entry, CancellationToken ct = default)
    {
        Validate(entry.Date, entry.Readings);
        await repository.Update(entry, ct);
    }

    public Task DeleteEntry(Guid id, DateOnly entryDate, CancellationToken ct = default) =>
        repository.Delete(id, entryDate, ct);

    public async Task<(DateOnly From, DateOnly To)> GetLast10DateRange(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var all = await repository.GetEntries(today.AddYears(-10), today, ct);

        if (all.Count == 0)
            return (today.AddMonths(-1), today);

        var last10 = all.OrderByDescending(e => e.Date).Take(10).ToList();
        return (last10[^1].Date, last10[0].Date);
    }

    private static void Validate(DateOnly date, IReadOnlyList<decimal> readings)
    {
        if (date > DateOnly.FromDateTime(DateTime.Today))
            throw new ValidationException("Entry date cannot be in the future.");

        if (readings.Count == 0 || readings.Count > 3)
            throw new ValidationException("A blood sugar entry must have between 1 and 3 readings.");

        foreach (var r in readings)
        {
            if (r < 0.5m || r > 30.0m)
                throw new ValidationException($"Blood sugar value {r} is out of range (0.5–30.0 mmol/L).");
        }
    }
}
