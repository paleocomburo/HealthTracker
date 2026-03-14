namespace HealthTracker.Infrastructure.Repositories;

using HealthTracker.Infrastructure.Json;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Enums;
using HealthTracker.Shared.Interfaces;

public class BloodPressureRepository(YearPartitionedStore<BloodPressureEntryJson> store) : IBloodPressureRepository
{
    public async Task<IReadOnlyList<BloodPressureEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var results = new List<BloodPressureEntry>();

        for (var year = from.Year; year <= to.Year; year++)
        {
            var raw = await store.ReadYear(year, ct);
            results.AddRange(
                raw.Select(ToDto)
                   .Where(e => e.Date >= from && e.Date <= to));
        }

        return results.OrderBy(e => e.Date).ThenBy(e => e.TimeOfDay).ToList();
    }

    public async Task<BloodPressureEntry> Add(BloodPressureEntry entry, CancellationToken ct = default)
    {
        var entries = await store.ReadYear(entry.Date.Year, ct);
        entries.Add(ToJson(entry));
        await store.WriteYear(entry.Date.Year, entries, ct);
        return entry;
    }

    public async Task Update(BloodPressureEntry entry, CancellationToken ct = default)
    {
        var entries = await store.ReadYear(entry.Date.Year, ct);
        var idx = entries.FindIndex(e => e.Id == entry.Id.ToString());
        if (idx >= 0)
            entries[idx] = ToJson(entry);
        await store.WriteYear(entry.Date.Year, entries, ct);
    }

    public async Task Delete(Guid id, DateOnly entryDate, CancellationToken ct = default)
    {
        var entries = await store.ReadYear(entryDate.Year, ct);
        entries.RemoveAll(e => e.Id == id.ToString());
        await store.WriteYear(entryDate.Year, entries, ct);
    }

    private static BloodPressureEntry ToDto(BloodPressureEntryJson j) =>
        new(
            Guid.Parse(j.Id),
            DateOnly.Parse(j.Date),
            Enum.Parse<TimeOfDay>(j.TimeOfDay, ignoreCase: true),
            j.Readings.Select(r => new BloodPressureReading(r.SystolicMmhg, r.DiastolicMmhg)).ToList());

    private static BloodPressureEntryJson ToJson(BloodPressureEntry e) =>
        new(
            e.Id.ToString(),
            e.Date.ToString("yyyy-MM-dd"),
            e.TimeOfDay.ToString().ToLowerInvariant(),
            e.Readings.Select(r => new BloodPressureReadingJson(r.SystolicMmhg, r.DiastolicMmhg)).ToList());
}

public record BloodPressureReadingJson(int SystolicMmhg, int DiastolicMmhg);

public record BloodPressureEntryJson(
    string Id,
    string Date,
    string TimeOfDay,
    List<BloodPressureReadingJson> Readings);
