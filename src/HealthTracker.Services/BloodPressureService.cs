namespace HealthTracker.Services;

using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Enums;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;

public class BloodPressureService(IBloodPressureRepository repository)
{
    public Task<IReadOnlyList<BloodPressureEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        repository.GetEntries(from, to, ct);

    public async Task<BloodPressureEntry> AddEntry(
        DateOnly date,
        TimeOfDay timeOfDay,
        IReadOnlyList<BloodPressureReading> readings,
        CancellationToken ct = default)
    {
        Validate(date, readings);
        var entry = new BloodPressureEntry(Guid.NewGuid(), date, timeOfDay, readings);
        return await repository.Add(entry, ct);
    }

    public async Task UpdateEntry(BloodPressureEntry entry, CancellationToken ct = default)
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

    private static void Validate(DateOnly date, IReadOnlyList<BloodPressureReading> readings)
    {
        if (date > DateOnly.FromDateTime(DateTime.Today))
            throw new ValidationException("Entry date cannot be in the future.");

        if (readings.Count == 0 || readings.Count > 5)
            throw new ValidationException("A blood pressure entry must have between 1 and 5 readings.");

        foreach (var r in readings)
        {
            if (r.SystolicMmhg < 60 || r.SystolicMmhg > 250)
                throw new ValidationException($"Systolic value {r.SystolicMmhg} is out of range (60–250).");

            if (r.DiastolicMmhg < 40 || r.DiastolicMmhg > 150)
                throw new ValidationException($"Diastolic value {r.DiastolicMmhg} is out of range (40–150).");
        }
    }
}
